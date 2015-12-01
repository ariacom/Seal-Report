//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RazorEngine;
using System.Xml.Serialization;
using Seal.Helpers;
using System.ComponentModel;
using Seal.Converter;
using Seal.Forms;
using System.Drawing.Design;
using DynamicTypeDescriptor;
using RazorEngine.Templating;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.Collections;
using System.Threading;
using System.Web;
using System.Data;
using System.Windows.Forms;
using System.Reflection;

namespace Seal.Model
{

    public class ReportView : ReportComponent, ITreeSort
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("ModelGUID").SetIsBrowsable(Template.ForModel);
                GetProperty("TemplateName").SetIsBrowsable(true);
                GetProperty("ThemeName").SetIsBrowsable(Template.UseThemeValues);
                GetProperty("CSS").SetIsBrowsable(Template.CSS.Count > 0);
                GetProperty("GeneralParameters").SetIsBrowsable(GeneralParameters.Count > 0);
                GetProperty("DataTableParameters").SetIsBrowsable(Model != null && DataTableParameters.Count > 0);

                //Set culture only on master view
                GetProperty("CultureName").SetIsBrowsable(IsRootView);
                GetProperty("UseCustomTemplate").SetIsBrowsable(true);
                GetProperty("CustomTemplate").SetIsBrowsable(true);
                GetProperty("CustomConfiguration").SetIsBrowsable(true);
                GetProperty("UseCustomConfiguration").SetIsBrowsable(true);

                //PDF only on root view generating HTML...
                if (AllowPDFConversion)
                {
                    GetProperty("PdfConverter").SetIsBrowsable(true);
                    PdfConverter.InitEditor();
                }
                GetProperty("ExcelConverter").SetIsBrowsable(true);
                ExcelConverter.InitEditor();

                bool hasMSChartConfig = (Model != null && Model.HasMicrosoftSerie && !Model.HasNVD3Serie) && !string.IsNullOrEmpty(Template.ChartConfigurationXML);
                GetProperty("ChartConfiguration").SetIsBrowsable(hasMSChartConfig);
                GetProperty("ChartAxisX").SetIsBrowsable(hasMSChartConfig);
                GetProperty("ChartAxisX2").SetIsBrowsable(hasMSChartConfig);
                GetProperty("ChartAxisY").SetIsBrowsable(hasMSChartConfig);
                GetProperty("ChartAxisY2").SetIsBrowsable(hasMSChartConfig);
                GetProperty("ChartSeries").SetIsBrowsable(hasMSChartConfig);
                GetProperty("ChartArea").SetIsBrowsable(hasMSChartConfig);
                bool hasNVD3Config = (Model != null && Model.HasNVD3Serie && NVD3Parameters.Count > 0);
                GetProperty("NVD3Parameters").SetIsBrowsable(hasNVD3Config);

                //Read only
                GetProperty("TemplateName").SetIsReadOnly(true);
                GetProperty("CustomTemplate").SetIsReadOnly(!UseCustomTemplate);
                GetProperty("CustomConfiguration").SetIsReadOnly(!UseCustomConfiguration);

                //Helpers
                GetProperty("HelperReloadConfiguration").SetIsBrowsable(true);
                GetProperty("HelperResetParameters").SetIsBrowsable(true);
                GetProperty("HelperResetChartConfiguration").SetIsBrowsable(hasMSChartConfig);
                GetProperty("HelperResetPDFConfigurations").SetIsBrowsable(AllowPDFConversion);
                GetProperty("HelperResetExcelConfigurations").SetIsBrowsable(true);
                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);

                GetProperty("Information").SetIsReadOnly(true);
                GetProperty("Error").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }

        public override void InitEditor()
        {
            base.InitEditor();
            //init series
            initMicrosoftChartConfigurationSeries();
        }

        #endregion

        public static ReportView Create(ReportViewTemplate template)
        {
            ReportView result = new ReportView() { GUID = Guid.NewGuid().ToString(), TemplateName = template.Name };
            return result;
        }


        public void InitReferences()
        {
            foreach (var childView in Views)
            {
                childView.Report = Report;
                childView.InitReferences();
            }
        }


        public void InitParameters(List<Parameter> configParameters, List<Parameter> parameters, bool resetValues)
        {
            foreach (var configParameter in configParameters)
            {
                Parameter parameter = parameters.FirstOrDefault(i => i.Name == configParameter.Name);
                if (parameter == null)
                {
                    parameter = new Parameter() { Name = configParameter.Name };
                    parameter.Value = configParameter.Value;
                    parameters.Add(parameter);
                }
                if (resetValues && parameter.Name != Parameter.NVD3ConfigurationParameter) parameter.Value = configParameter.Value;
                parameter.Enums = configParameter.Enums;
                parameter.Description = configParameter.Description;
                parameter.Type = configParameter.Type;
                parameter.UseOnlyEnumValues = configParameter.UseOnlyEnumValues;
                parameter.DisplayName = configParameter.DisplayName;
                parameter.ConfigValue = configParameter.Value;
                parameter.EditorLanguage = configParameter.EditorLanguage;
                parameter.Category = configParameter.Category;
            }
            //remove undefined parameters
            int index = parameters.Count;
            while (--index >= 0) if (!configParameters.Exists(i => i.Name == parameters[index].Name)) parameters.RemoveAt(index);

        }

        //Temporary variables to help for report serialization...
        private List<Parameter> _tempParameters;
        private List<Parameter> _tempCSS;
        private string _tempChartConfigurationXML;

        public void BeforeSerialization()
        {
            _tempParameters = Parameters.ToList();
            _tempCSS = CSS.ToList();
            _tempChartConfigurationXML = ChartConfigurationXml;

            //Remove parameters and CSS identical to config
            if (string.IsNullOrEmpty(Template.ChartConfigurationXML)) _chartConfigurationXml = null;
            else if (!string.IsNullOrEmpty(_chartConfigurationXml) && _chartConfigurationXml.Replace("\r\n", "").Replace(" ", "") == Template.ChartConfigurationXML.Replace(" ", "")) _chartConfigurationXml = "";
            Parameters.RemoveAll(i => i.Value == null || i.Value == i.ConfigValue);
            CSS.RemoveAll(i => i.Value == i.ConfigValue);

            foreach (var view in Views) view.BeforeSerialization();
        }

        public void AfterSerialization()
        {
            Parameters = _tempParameters;
            CSS = _tempCSS;
            ChartConfigurationXml = _tempChartConfigurationXML;

            foreach (var view in Views) view.AfterSerialization();
        }


        public void InitParameters(bool resetValues)
        {
            if (Report == null || Template == null) return;

            _error = "";
            //Parse the configuration file to init the view template
            Template.ParseConfiguration(ViewTemplateConfiguration);

            if (string.IsNullOrEmpty(_error) && !string.IsNullOrEmpty(Template.Error)) _error = string.Format("Error parsing configuration of template '{0}':\r\nError:{1}\r\nPath:{2}", _template.Name, Template.Error, Template.ConfigurationPath);

            InitParameters(Template.Parameters, _parameters, resetValues);
            InitParameters(Template.CSS, _css, resetValues);

            if (!string.IsNullOrEmpty(_error)) _information = "Error loading the configuration";
            else
            {
                _information = "Configuration has been loaded successfully";
                if (resetValues) _information += " and values have been reset";
            }
            _information = Helper.FormatMessage(_information);
        }

        public void ResetChartConfiguration()
        {
            _chartConfigurationXml = "";
            _chartConfiguration = null;
            _information = Helper.FormatMessage("Chart configuration has been reset");
        }

        public bool HasValue(string name)
        {
            return !string.IsNullOrEmpty(GetValue(name));
        }

        public string GetValue(string name)
        {
            Parameter parameter = _parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? "" : parameter.Value;
        }

        public string GetHtmlValue(string name)
        {
            return Helper.ToHtml(GetValue(name));
        }

        public bool GetBoolValue(string name)
        {
            Parameter parameter = _parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? false : parameter.BoolValue;
        }

        public int GetNumericValue(string name)
        {
            Parameter parameter = _parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? 0 : parameter.NumericValue;
        }

        public string GetCSS(string name)
        {
            Parameter css = _css.FirstOrDefault(i => i.Name == name);
            return css == null ? "" : css.Value;
        }

        public string GetThemeValue(string name)
        {
            Parameter parameter = Theme.Values.FirstOrDefault(i => i.Name == name);
            return parameter == null ? "" : parameter.Value;
        }

        public Color GetThemeColor(string name)
        {
            Color result = Color.Red;
            string colorText = GetThemeValue(name);
            if (colorText.StartsWith("#") && colorText.Length == 7)
            {
                try
                {
                    result = Color.FromArgb(int.Parse(colorText.Substring(1, 2), System.Globalization.NumberStyles.HexNumber), int.Parse(colorText.Substring(3, 2), System.Globalization.NumberStyles.HexNumber), int.Parse(colorText.Substring(5, 2), System.Globalization.NumberStyles.HexNumber));
                }
                catch { }
            }
            return result;
        }

        public bool IsRootView
        {
            get { return Template.ParentNames.Count == 0; }
        }

        public bool AllowPDFConversion
        {
            get { return IsRootView && string.IsNullOrEmpty(ExternalViewerExtension); }
        }

        public bool IsAncestorOf(ReportView view)
        {
            bool result = false;
            foreach (ReportView child in Views)
            {
                if (child == view) return true;
                result = child.IsAncestorOf(view);
                if (result) break;
            }
            return result;
        }

        public ReportView RootView
        {
            get
            {
                ReportView result = this;
                foreach (var view in Report.Views)
                {
                    if (view.IsAncestorOf(this)) return view;
                }
                return this;
            }
        }

        public bool DisplaySummaryRow(int row)
        {
            bool result = false;
            if (Model != null && Model.SummaryTable != null && row < Model.SummaryTable.Lines.Count)
            {
                ResultCell[] line = Model.SummaryTable.Lines[row];
                if ((!GetBoolValue("add_summary_totals_totals") || !GetBoolValue("display_summary_totals") || !Model.HasTotals) && line[0].IsTotal && row == Model.SummaryTable.Lines.Count - 1)
                {
                    result = false;
                }
                else
                {
                    result = true;
                }
            }
            return result;
        }

        public string ViewName
        {
            get
            {
                if (Report != null) return Report.TranslateViewName(Name);
                return Name;
            }
        }

        string _modelGUID;
        [DisplayName("Model"), Description("The data model used for the view."), Category("Definition"), Id(1, 1)]
        [TypeConverter(typeof(ReportModelConverter))]
        public string ModelGUID
        {
            get { return _modelGUID; }
            set
            {
                if (_modelGUID != value) _chartConfiguration = null;
                _modelGUID = value;
                UpdateEditorAttributes();
            }
        }

        string _templateName;
        [DisplayName("Template name"), Description("The name of the view template. View templates are defined in the repository Views folder."), Category("Definition"), Id(2, 1)]
        public string TemplateName
        {
            get { return _templateName; }
            set { _templateName = value; }
        }

        ReportViewTemplate _template = null;
        public ReportViewTemplate Template
        {
            get
            {
                if (_template == null)
                {
                    _template = _report.Repository.ViewTemplates.FirstOrDefault(i => i.Name == TemplateName);
                    if (_template == null)
                    {
                        _template = new ReportViewTemplate() { Name = TemplateName };
                        _error = string.Format("Unable to find template named '{0}'. Check your repository Views folder.", TemplateName);
                    }
                    else
                    {
                        _error = _template.Error;
                    }
                    InitParameters(false);
                }
                return _template;
            }
        }

        string _themeName = "";
        [DisplayName("Theme name"), Description("The name of the theme used for the view. If empty, the default theme is used. Themes are defined in the repository Themes folder."), Category("Definition"), Id(1, 1)]
        [TypeConverter(typeof(ThemeConverter))]
        public string ThemeName
        {
            get { return _themeName; }
            set { _themeName = value; }
        }

        private Theme _theme = null;
        public Theme Theme
        {
            get
            {
                if (_theme == null)
                {
                    if (string.IsNullOrEmpty(ThemeName)) _theme = _report.Repository.Themes.FirstOrDefault(i => i.IsDefault);
                    else _theme = _report.Repository.Themes.FirstOrDefault(i => i.Name == ThemeName);
                    if (_theme == null)
                    {
                        _theme = new Theme() { Name = ThemeName };
                        _error = string.Format("Unable to find theme named '{0}'. Check your repository Themes folder.", ThemeName);
                    }
                    else
                    {
                        _error = _theme.Error;
                    }
                }
                return _theme;
            }
        }


        public List<ReportView> Views = new List<ReportView>();

        bool _useCustomConfiguration = false;
        [DisplayName("Use custom configuration text"), Description("If true, the configuration text can be modified."), Category("Custom template configuration"), Id(1, 2)]
        public bool UseCustomConfiguration
        {
            get { return _useCustomConfiguration; }
            set
            {
                _useCustomConfiguration = value;
                InitParameters(false);
                UpdateEditorAttributes();
            }
        }

        string _customConfiguration;
        [DisplayName("Custom configuration"), Description("The custom configuration text used instead of the configuration of the template."), Category("Custom template configuration"), Id(2, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string CustomConfiguration
        {
            get { return _customConfiguration; }
            set
            {
                _customConfiguration = value;
                InitParameters(false);
            }
        }

        bool _useCustomTemplate = false;
        [DisplayName("Use custom template text"), Description("If true, the template text can be modified."), Category("Custom template text"), Id(1, 3)]
        public bool UseCustomTemplate
        {
            get { return _useCustomTemplate; }
            set
            {
                _useCustomTemplate = value;
                UpdateEditorAttributes();
            }
        }

        DateTime _lastTemplateModification = DateTime.Now;
        string _customTemplate;
        [DisplayName("Custom template"), Description("The custom template text used instead of the template defined by the template name."), Category("Custom template text"), Id(2, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string CustomTemplate
        {
            get { return _customTemplate; }
            set
            {
                _lastTemplateModification = DateTime.Now;
                _customTemplate = value;
            }
        }

        List<Parameter> _parameters = new List<Parameter>();
        public List<Parameter> Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        [DisplayName("General Parameters"), Description("The view parameters values."), Category("View parameters"), Id(1, 4)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        [XmlIgnore]
        public List<Parameter> GeneralParameters
        {
            get { return _parameters.Where(i => i.Category == ViewParameterCategory.General).ToList(); }
            set { }
        }

        [DisplayName("Data Table Configuration"), Description("The configuration values for the Data Table."), Category("View parameters"), Id(2, 4)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        [XmlIgnore]
        public List<Parameter> DataTableParameters
        {
            get { return _parameters.Where(i => i.Category == ViewParameterCategory.DataTables).ToList(); }
            set { }
        }

        [DisplayName("NVD3 Chart Configuration"), Description("The configuration values for the NV3 Chart."), Category("View parameters"), Id(4, 4)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        [XmlIgnore]
        public List<Parameter> NVD3Parameters
        {
            get { return _parameters.Where(i => i.Category == ViewParameterCategory.NVD3).ToList(); }
            set { }
        }

        List<Parameter> _css = new List<Parameter>();
        [DisplayName("CSS"), Description("The custom CSS values for the view."), Category("View parameters"), Id(5, 4)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<Parameter> CSS
        {
            get { return _css; }
            set { _css = value; }
        }

        public int GetSort()
        {
            return _sortOrder;
        }

        int _sortOrder = 0;
        public int SortOrder
        {
            get { return _sortOrder; }
            set { _sortOrder = value; }
        }

        public string SortOrderFull
        {
            get { return string.Format("{0:D5}_{1}", _sortOrder, Name); }
        }

        string _cultureName = "";
        [DisplayName("Culture name"), Description("The language and culture used to display the report. If empty, the culture of the server configuration is used."), Category("View parameters"), Id(3, 4)]
        [TypeConverter(typeof(CultureNameConverter))]
        public string CultureName
        {
            get { return _cultureName; }
            set
            {
                _cultureInfo = null;
                _cultureName = value;
            }
        }


        CultureInfo _cultureInfo = null;
        public CultureInfo CultureInfo
        {
            get
            {
                if (_cultureInfo == null && !string.IsNullOrEmpty(_cultureName)) _cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(i => i.EnglishName == _cultureName);
                if (_cultureInfo == null && Report.ExecutionView != this && Report.ExecutionView != null) _cultureInfo = Report.CultureInfo;
                if (_cultureInfo == null) _cultureInfo = Report.Repository.CultureInfo;
                return _cultureInfo;
            }
        }

        public void SetAdvancedConfigurations()
        {
            //Chart
            if (Template.ForModel && ChartConfiguration != null && Model != null)
            {
                //clear points of dynamic series...
                foreach (var serieElement in Model.Elements.Where(i => i.SerieDefinition == SerieDefinition.Serie))
                {
                    Series serie = ChartConfiguration.Series.FirstOrDefault(i => i.Name.Contains(serieElement.GUID));
                    if (serie != null) serie.Points.Clear();
                }

                StringWriter sw = new StringWriter();
                ChartConfiguration.Serializer.Save(sw);
                ChartConfigurationXml = sw.ToString();
            }
            //Pdf & Excel
            if (AllowPDFConversion && PdfConverterEdited)
            {
                _pdfConfigurations = PdfConverter.GetConfigurations();
            }
            if (ExcelConverterEdited)
            {
                _excelConfigurations = ExcelConverter.GetConfigurations();
            }

            foreach (var view in Views) view.SetAdvancedConfigurations();
        }

        string _chartConfigurationXml;
        public string ChartConfigurationXml
        {
            get { return _chartConfigurationXml; }
            set { _chartConfigurationXml = value; }
        }

        Chart _chartConfiguration = null;
        [DisplayName("Full configuration"), Description("The configuration of the chart if the model defines chart series."), Category("Microsoft Chart configuration"), Id(1, 6)]
        [XmlIgnore]
        public Chart ChartConfiguration
        {
            get
            {
                if (Template.ForModel && _chartConfiguration == null && !string.IsNullOrEmpty(Template.ChartConfigurationXML))
                {
                    _chartConfiguration = new Chart() { Width = 600, Height = 400 };
                    //Add a legend by default
                    _chartConfiguration.Legends.Add(new Legend());
                    string configuration = Template.ChartConfigurationXML;
                    if (!string.IsNullOrEmpty(_chartConfigurationXml)) configuration = _chartConfigurationXml;
                    if (!string.IsNullOrEmpty(configuration))
                    {
                        try
                        {
                            StringReader sr = new StringReader(configuration);
                            _chartConfiguration.Serializer.Load(sr);
                        }
                        catch (Exception ex)
                        {
                            if (!string.IsNullOrEmpty(_chartConfigurationXml))
                            {
                                _error = "Invalid chart configuration, reset it." + ex.Message;
                                _chartConfigurationXml = "";
                            }
                            else
                            {
                                _error = "Invalid chart configuration in template." + ex.Message;
                            }
                        }
                    }

                    //Check basics chart elements
                    //Area
                    if (_chartConfiguration.ChartAreas.Count == 0)
                    {
                        ChartArea area = new ChartArea();
                        _chartConfiguration.ChartAreas.Add(area);
                    }
                    //Title 
                    if (_chartConfiguration.Titles.Count == 0)
                    {
                        Title title = new Title() { Visible = false };
                        _chartConfiguration.Titles.Add(title);
                    }
                    initMicrosoftChartConfigurationSeries();
                }
                return _chartConfiguration;
            }
        }

        [DisplayName("Chart X axis"), Description("The primary X axis of the chart."), Category("Microsoft Chart configuration"), Id(4, 6)]
        [XmlIgnore]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Axis ChartAxisX
        {
            get
            {
                if (ChartConfiguration != null && ChartConfiguration.ChartAreas.Count > 0) return ChartConfiguration.ChartAreas[0].AxisX;
                return null;
            }
        }

        [DisplayName("Chart X2 axis"), Description("The secondary X axis of the chart."), Category("Microsoft Chart configuration"), Id(5, 6)]
        [XmlIgnore]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Axis ChartAxisX2
        {
            get
            {
                if (ChartConfiguration != null && ChartConfiguration.ChartAreas.Count > 0) return ChartConfiguration.ChartAreas[0].AxisX2;
                return null;
            }
        }


        [DisplayName("Chart Y axis"), Description("The primary Y axis of the chart."), Category("Microsoft Chart configuration"), Id(6, 6)]
        [XmlIgnore]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Axis ChartAxisY
        {
            get
            {
                if (ChartConfiguration != null && ChartConfiguration.ChartAreas.Count > 0) return ChartConfiguration.ChartAreas[0].AxisY;
                return null;
            }
        }

        [DisplayName("Chart Y2 axis"), Description("The secondary Y axis of the chart."), Category("Microsoft Chart configuration"), Id(7, 6)]
        [XmlIgnore]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Axis ChartAxisY2
        {
            get
            {
                if (ChartConfiguration != null && ChartConfiguration.ChartAreas.Count > 0) return ChartConfiguration.ChartAreas[0].AxisY2;
                return null;
            }
        }

        [DisplayName("Chart series"), Description("The chart series."), Category("Microsoft Chart configuration"), Id(3, 6)]
        [XmlIgnore]
        public SeriesCollection ChartSeries
        {
            get
            {
                if (ChartConfiguration != null) return ChartConfiguration.Series;
                return null;
            }
        }

        [DisplayName("Chart area"), Description("The chart area."), Category("Microsoft Chart configuration"), Id(2, 6)]
        [XmlIgnore]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public ChartArea ChartArea
        {
            get
            {
                if (ChartConfiguration != null && ChartConfiguration.ChartAreas.Count > 0) return ChartConfiguration.ChartAreas[0];
                return null;
            }
        }

        [XmlIgnore]
        public ReportModel Model
        {
            get
            {
                if (string.IsNullOrEmpty(_modelGUID)) return null;
                return _report.Models.FirstOrDefault(i => i.GUID == _modelGUID);
            }
        }


        #region PDF and Excel Converters

        private List<string> _pdfConfigurations = new List<string>();
        public List<string> PdfConfigurations
        {
            get { return _pdfConfigurations; }
            set { _pdfConfigurations = value; }
        }

        private SealPdfConverter _pdfConverter = null;
        [XmlIgnore]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("PDF Configuration"), Description("All the options applied to the PDF conversion from the HTML result."), Category("View parameters"), Id(5, 4)]
        public SealPdfConverter PdfConverter
        {
            get
            {
                if (AllowPDFConversion && _pdfConverter == null)
                {
                    _pdfConverter = SealPdfConverter.Create(Report.Repository.ApplicationPath);
                    _pdfConverter.SetConfigurations(PdfConfigurations, this);
                    _pdfConverter.EntityHandler = HelperEditor.HandlerInterface;
                    UpdateEditorAttributes();
                }
                return _pdfConverter;
            }
            set { _pdfConverter = value; }
        }

        public bool PdfConverterEdited
        {
            get { return _pdfConverter != null; }
        }

        private List<string> _excelConfigurations = new List<string>();
        public List<string> ExcelConfigurations
        {
            get { return _excelConfigurations; }
            set { _excelConfigurations = value; }
        }

        private SealExcelConverter _excelConverter = null;
        [XmlIgnore]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("Excel Configuration"), Description("All the options applied to the Excel conversion from the view."), Category("View parameters"), Id(6, 4)]
        public SealExcelConverter ExcelConverter
        {
            get
            {
                if (_excelConverter == null)
                {
                    _excelConverter = SealExcelConverter.Create(Report.Repository.ApplicationPath);
                    _excelConverter.SetConfigurations(ExcelConfigurations, this);
                    _excelConverter.EntityHandler = HelperEditor.HandlerInterface;
                    UpdateEditorAttributes();
                }
                return _excelConverter;
            }
            set { _excelConverter = value; }
        }

        public bool ExcelConverterEdited
        {
            get { return _excelConverter != null; }
        }

        public string ConvertToExcel(string destination)
        {
            return ExcelConverter.ConvertToExcel(destination);
        }

        #endregion

        #region Helpers
        [Category("Helpers"), DisplayName("Reload template configuration"), Description("Load the template configuration file."), Id(1, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperReloadConfiguration
        {
            get { return "<Click to reload the template configuration and refresh the parameters>"; }
        }

        [Category("Helpers"), DisplayName("Reset parameter values"), Description("Reset parameters to their default values."), Id(2, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetParameters
        {
            get { return "<Click to reset the view parameters to their default values>"; }
        }

        [Category("Helpers"), DisplayName("Reset Microsoft Chart configuration"), Description("Reset the Microsoft Chart configuration to default values."), Id(3, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetChartConfiguration
        {
            get { return "<Click to reset the Microsoft Chart configuration to default values>"; }
        }

        [Category("Helpers"), DisplayName("Reset PDF configurations"), Description("Reset PDF configuration values to default values."), Id(5, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetPDFConfigurations
        {
            get { return "<Click to reset the PDF configuration values to default values>"; }
        }

        [Category("Helpers"), DisplayName("Reset Excel configurations"), Description("Reset Excel configuration values to default values."), Id(6, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetExcelConfigurations
        {
            get { return "<Click to reset the Excel configuration values to default values>"; }
        }

        string _information;
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(7, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        string _error;
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(8, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        #endregion

        [XmlIgnore]
        public string ViewTemplateConfiguration
        {
            get
            {
                if (UseCustomConfiguration)
                {
                    if (string.IsNullOrWhiteSpace(CustomConfiguration)) return Template.Configuration;
                    return CustomConfiguration;
                }
                return Template.Configuration;
            }
        }


        [XmlIgnore]
        public string ViewTemplateCacheKey
        {
            get
            {
                string result;
                if (!UseCustomTemplate || string.IsNullOrWhiteSpace(CustomTemplate))
                {
                    //template -> file path + last modification
                    result = string.Format("TPL:{0}_{1}", Template.FilePath, File.GetLastWriteTime(Template.FilePath).ToString("s"));
                }
                else
                {
                    //view -> report path + last modification
                    result = string.Format("REP:{0}_{1}_{2}", Report.FilePath, GUID, _lastTemplateModification.ToString("s"));
                }
                return result;
            }
        }

        [XmlIgnore]
        public string ViewTemplateText
        {
            get
            {
                if (UseCustomTemplate)
                {
                    if (string.IsNullOrWhiteSpace(CustomTemplate)) return Template.Text;
                    return CustomTemplate;
                }
                return Template.Text;
            }
        }
        string _viewId = null;
        [XmlIgnore]
        public string ViewId
        {
            get
            {
                if (string.IsNullOrEmpty(_viewId)) _viewId = Guid.NewGuid().ToString();
                return _viewId;
            }
        }

        static HtmlString dummy = null;
        static DataTable dummy2 = null;
        static Control dummy3 = null;

        public string Parse()
        {
            string result = "";
            _error = "";

            try
            {
                Report.View = this;

                if (dummy == null) dummy = new HtmlString(""); //Force the load of System.Web
                if (dummy2 == null) dummy2 = new DataTable(); //Force the load of System.Data
                if (dummy3 == null) dummy3 = new Control(); //Force load of System.Windows.Forms

                if (Template.ForModel)
                {
                    if (Model == null)
                    {
                        Report.ExecutionMessages += string.Format("Warning for view '{0}': Model has been lost for the view. Switching to the first model of the report...", Name);
                        _modelGUID = Report.Models[0].GUID;
                    }
                    if (!ReportExecution.IsViewCompiled(ViewTemplateCacheKey))
                    {
                        Helper.CompileRazor(ViewTemplateText, typeof(ReportModel), ViewTemplateCacheKey);
                        ReportExecution.CompiledViewAdd(ViewTemplateCacheKey);
                    }
                    result = Razor.Run(ViewTemplateCacheKey, Model);
                }
                else
                {
                    if (!ReportExecution.IsViewCompiled(ViewTemplateCacheKey))
                    {
                        Helper.CompileRazor(ViewTemplateText, typeof(Report), ViewTemplateCacheKey);
                        ReportExecution.CompiledViewAdd(ViewTemplateCacheKey);
                    }
                    result = Razor.Run(ViewTemplateCacheKey, Report);
                }
            }
            catch (TemplateCompilationException ex)
            {
                _error = string.Format("Compilation error when parsing the view '{0}({1})':\r\n{2}", Name, Template.Name, Helper.GetExceptionMessage(ex));
                if (ex.InnerException != null) _error += "\r\n" + ex.InnerException.Message;
            }
            catch (Exception ex)
            {
                _error = string.Format("Execution error when parsing the view '{0} ({1})':\r\n{2}", Name, Template.Name, ex.Message);
                if (ex.InnerException != null) _error += "\r\n" + ex.InnerException.Message;
            }
            if (!string.IsNullOrEmpty(_error))
            {
                Report.ExecutionErrors += _error;
                result = _error;
            }

            return result;
        }

        public string ParseChildren()
        {
            string result = "";
            foreach (ReportView view in Views.OrderBy(i => i.SortOrder))
            {
                result += view.Parse();
            }
            return result;
        }


        public void InitTemplates(ReportView view, ref string errors)
        {
            view.InitParameters(false);
            if (!string.IsNullOrEmpty(view.Error)) errors += string.Format("Error in view template '{0}': {1}\r\n", view.Name, view.Error);
            foreach (var child in view.Views) InitTemplates(child, ref errors);
        }


        void initMicrosoftChartConfigurationSeries()
        {
            if (Model != null && !string.IsNullOrEmpty(Template.ChartConfigurationXML) && ChartConfiguration != null && Model.HasSerie)
            {
                foreach (var serieElement in Model.Elements.Where(i => i.SerieDefinition == SerieDefinition.Serie))
                {
                    Series serie = ChartConfiguration.Series.FirstOrDefault(i => i.Name.Contains(serieElement.GUID));
                    if (serie == null)
                    {
                        serie = new Series();
                        serie.IsValueShownAsLabel = true;
                        ChartConfiguration.Series.Add(serie);
                    }
                    serie.Name = string.Format("{0}({1})", serieElement.DisplayNameEl, serieElement.GUID);
                    serie.XAxisType = serieElement.XAxisType;
                    serie.YAxisType = serieElement.YAxisType;

                    serie.ChartType = serieElement.SerieType;
                    serie.LabelFormat = serieElement.FormatEl;

                    if (ChartConfiguration.ChartAreas.Count > 0 && string.IsNullOrEmpty(ChartConfiguration.ChartAreas[0].AxisY.LabelStyle.Format) && serieElement.YAxisType == AxisType.Primary)
                    {
                        ChartConfiguration.ChartAreas[0].AxisY.LabelStyle.Format = serieElement.FormatEl;
                    }
                    if (ChartConfiguration.ChartAreas.Count > 0 && string.IsNullOrEmpty(ChartConfiguration.ChartAreas[0].AxisY2.LabelStyle.Format) && serieElement.YAxisType == AxisType.Secondary)
                    {
                        ChartConfiguration.ChartAreas[0].AxisY2.LabelStyle.Format = serieElement.FormatEl;
                    }
                }

                //Cleanup lost series...
                int index = ChartConfiguration.Series.Count;
                while (--index >= 0)
                {
                    if (Model.Elements.FirstOrDefault(i => ChartConfiguration.Series[index].Name.Contains(i.GUID)) == null)
                    {
                        ChartConfiguration.Series.RemoveAt(index);
                    }
                }
            }
        }

        private void initAxisProperties(ResultPage page, List<ResultCell[]> XDimensions)
        {
            bool hasNVD3Pie = Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.PieChart && i.PivotPosition == PivotPosition.Data);
            var dimensions = XDimensions.FirstOrDefault();
            if (dimensions != null)
            {
                //One value -> set the raw value, several values -> concat the display value
                if (dimensions.Length == 1)
                {
                    if (!dimensions[0].Element.IsEnum && dimensions[0].Element.AxisUseValues && !hasNVD3Pie)
                    {
                        if (page.Chart != null) page.Chart.ChartAreas[0].AxisX.LabelStyle.Format = dimensions[0].Element.FormatEl;
                        page.NVD3IsNumericAxis = dimensions[0].Element.IsNumeric;
                        page.NVD3IsDateTimeAxis = dimensions[0].Element.IsDateTime;
                        page.NVD3XAxisFormat = dimensions[0].Element.GetNVD3Format(CultureInfo, page.NVD3ChartType);
                    }
                }
            }
        }


        private Dictionary<object, object> initXValues(ResultPage page, List<ResultCell[]> XDimensions)
        {
            Dictionary<object, object> result = new Dictionary<object, object>();
            bool hasNVD3Pie = Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.PieChart && i.PivotPosition == PivotPosition.Data);
            bool orderAsc = true;
            foreach (var dimensions in XDimensions)
            {
                //One value -> set the raw value, several values -> concat the display value
                if (dimensions.Length == 1)
                {

                    if (!dimensions[0].Element.IsEnum && dimensions[0].Element.AxisUseValues && !hasNVD3Pie)
                    {
                        result.Add(dimensions, dimensions[0].Value);
                    }
                    else
                    {
                        result.Add(dimensions, dimensions[0].ValueNoHTML);
                    }
                }
                else result.Add(dimensions, Helper.ConcatCellValues(dimensions, ","));

                if (dimensions.Length > 0 && dimensions[0].Element.SortOrder.Contains(SortOrderConverter.kAutomaticDescSortKeyword)) orderAsc = false;
            }

            return orderAsc ? result.OrderBy(i => i.Value).ToDictionary(i => i.Key, i => i.Value) : result.OrderByDescending(i => i.Value).ToDictionary(i => i.Key, i => i.Value);
        }

        private void initChartXValues(ResultPage page)
        {
            //Build list of X Values
            page.PrimaryXValues = initXValues(page, page.PrimaryXDimensions);
            page.SecondaryXValues = initXValues(page, page.SecondaryXDimensions);
        }

        private void buildMicrosoftSeries(ResultPage page)
        {
            initAxisProperties(page, page.PrimaryXDimensions);
            initAxisProperties(page, page.SecondaryXDimensions);
            initChartXValues(page);

            foreach (ResultSerie resultSerie in page.Series)
            {
                if (Report.Cancel) break;

                Series serieConfiguration = ChartConfiguration.Series.FirstOrDefault(i => i.Name.Contains(resultSerie.Element.GUID));
                if (serieConfiguration != null)
                {
                    Series serie = Helper.CloneSeries(serieConfiguration);
                    serie.Tag = 1;
                    serie.Name = resultSerie.SerieDisplayName;
                    page.Chart.Series.Add(serie);

                    //Fill Serie
                    Dictionary<object, object> XValues = (resultSerie.Element.XAxisType == AxisType.Primary ? page.PrimaryXValues : page.SecondaryXValues);
                    foreach (var xDimensionKey in XValues.Keys)
                    {
                        //Find the corresponding serie value...
                        ResultSerieValue value = resultSerie.Values.FirstOrDefault(i => i.XDimensionValues == xDimensionKey);
                        object yValue = (value != null ? value.Yvalue.Value : null);
                        serie.Points.AddXY(XValues[xDimensionKey], yValue);
                    }

                    //Sort serie
                    if (resultSerie.Element.SerieSortType != SerieSortType.None) serie.Sort(resultSerie.Element.SerieSortOrder, resultSerie.Element.SerieSortType.ToString());
                }
            }

            //clear the series used for configurations
            int index = page.Chart.Series.Count;
            while (--index >= 0) if (page.Chart.Series[index].Tag == null) page.Chart.Series.RemoveAt(index);
        }

        ResultSerie _serieForSort = null;
        private int CompareXDimensionsWithSeries(ResultCell[] a, ResultCell[] b)
        {
            ResultSerieValue va = _serieForSort.Values.FirstOrDefault(i => i.XDimensionValues == a);
            ResultSerieValue vb = _serieForSort.Values.FirstOrDefault(i => i.XDimensionValues == b);
            if (va != null && vb != null)
            {
                return (_serieForSort.Element.SerieSortOrder == PointSortOrder.Ascending ? 1 : -1) * CompareResultSerieValues(va, vb);
            }
            return 0;
        }

        private static int CompareResultSerieValues(ResultSerieValue a, ResultSerieValue b)
        {
            if (a.Yvalue.Element.IsNumeric && a.Yvalue.DoubleValue != null && b.Yvalue.DoubleValue != null) return a.Yvalue.DoubleValue.Value.CompareTo(b.Yvalue.DoubleValue.Value);
            if (a.Yvalue.Element.IsDateTime && a.Yvalue.DateTimeValue != null && b.Yvalue.DateTimeValue != null) return a.Yvalue.DateTimeValue.Value.CompareTo(b.Yvalue.DateTimeValue.Value);
            return 0;
        }

        private int CompareXDimensionsWithAxis(ResultCell[] a, ResultCell[] b)
        {
            return (_serieForSort.Element.SerieSortOrder == PointSortOrder.Ascending ? 1 : -1) * ResultCell.CompareCells(a, b);
        }

        private void buildNVD3Series(ResultPage page)
        {
            initAxisProperties(page, page.PrimaryXDimensions);
            //Sort series if necessary, only one serie is used for sorting...
            if (!page.NVD3IsNumericAxis && !page.NVD3IsDateTimeAxis)
            {
                _serieForSort = page.Series.FirstOrDefault(i => i.Element.SerieSortType != SerieSortType.None);
                if (_serieForSort != null)
                {
                    if (_serieForSort.Element.SerieSortType == SerieSortType.Y) page.PrimaryXDimensions.Sort(CompareXDimensionsWithSeries);
                    else page.PrimaryXDimensions.Sort(CompareXDimensionsWithAxis);
                }
            }
            initChartXValues(page);

            StringBuilder result = new StringBuilder();
            if (!page.NVD3IsNumericAxis && !page.NVD3IsDateTimeAxis)
            {
                //Build X labels
                foreach (var key in page.PrimaryXValues.Keys)
                {
                    if (result.Length != 0) result.Append(",");
                    result.Append(Helper.QuoteSingle(HttpUtility.JavaScriptStringEncode(page.PrimaryXValues[key].ToString())));
                }

                page.NVD3XLabels = result.ToString();
            }

            foreach (ResultSerie resultSerie in page.Series)
            {
                if (Report.Cancel) break;

                if (string.IsNullOrEmpty(page.NVD3PrimaryYAxisFormat) && resultSerie.Element.YAxisType == AxisType.Primary)
                {
                    page.NVD3PrimaryYAxisFormat = resultSerie.Element.GetNVD3Format(CultureInfo, page.NVD3ChartType);
                    page.NVD3PrimaryYIsDateTime = resultSerie.Element.IsDateTime;
                }
                else if (string.IsNullOrEmpty(page.NVD3SecondaryYAxisFormat) && resultSerie.Element.YAxisType == AxisType.Secondary)
                {
                    page.NVD3SecondaryYAxisFormat = resultSerie.Element.GetNVD3Format(CultureInfo, page.NVD3ChartType);
                    page.NVD3SecondaryYIsDateTime = resultSerie.Element.IsDateTime;
                }
                //Fill Serie
                result = new StringBuilder();
                int index = 0;
                foreach (var xDimensionKey in page.PrimaryXValues.Keys)
                {
                    string xValue = (index++).ToString(CultureInfo.InvariantCulture.NumberFormat);

                    //Find the corresponding serie value...
                    ResultSerieValue value = resultSerie.Values.FirstOrDefault(i => i.XDimensionValues == xDimensionKey);
                    object yValue = (value != null ? value.Yvalue.Value : null);
                    if (result.Length != 0) result.Append(",");

                    if (page.NVD3IsNumericAxis)
                    {
                        Double db = 0;
                        if (value == null) Double.TryParse(page.PrimaryXValues[xDimensionKey].ToString(), out db);
                        else if (value.XDimensionValues[0].DoubleValue != null) db = value.XDimensionValues[0].DoubleValue.Value;
                        xValue = db.ToString(CultureInfo.InvariantCulture.NumberFormat);
                    }
                    else if (page.NVD3IsDateTimeAxis)
                    {
                        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                        if (value == null) dt = ((DateTime)page.PrimaryXValues[xDimensionKey]);
                        else if (value.XDimensionValues[0].DateTimeValue != null) dt = value.XDimensionValues[0].DateTimeValue.Value;
                        TimeSpan diff = dt.ToUniversalTime() - (new DateTime(1970, 1, 1, 0, 0, 0, 0));
                        xValue = string.Format("{0}000", Math.Floor(diff.TotalSeconds));
                    }

                    if (yValue is DateTime)
                    {
                        TimeSpan diff = ((DateTime)yValue).ToUniversalTime() - (new DateTime(1970, 1, 1, 0, 0, 0, 0));
                        yValue = string.Format("{0}000", Math.Floor(diff.TotalSeconds));
                    }
                    else if (yValue is Double)
                    {
                        yValue = ((Double)yValue).ToString(CultureInfo.InvariantCulture.NumberFormat);
                    }

                    result.AppendFormat("{{x:{0},y:{1}}}", xValue, yValue == null ? "0" : yValue);
                }
                resultSerie.NVD3SerieValues = result.ToString();
            }
        }


        void checkChartIntegrity(ResultPage page)
        {
            if (Model.HasNVD3Serie && Model.HasMicrosoftSerie) throw new Exception("Invalid chart configuration: Cannot mix HTML5 and Microsoft Series.");

            if (Model.HasNVD3Serie)
            {
                bool hasArea = false, hasBar = false, hasLine = false;
                page.NVD3ChartType = "";

                //Check and choose the right chart
                if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.ScatterChart))
                {
                    if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie != NVD3SerieDefinition.ScatterChart)) throw new Exception("Invalid chart configuration: Cannot mix NVD3 Point Serie with another type.");
                    page.NVD3ChartType = "scatterChart";
                }
                else if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.PieChart))
                {
                    if (Model.Elements.Count(i => i.SerieDefinition == SerieDefinition.NVD3Serie) > 1) throw new Exception("Invalid chart configuration: Only one Pie Serie can be defined.");
                    page.NVD3ChartType = "pieChart";
                }
                else if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.MultiBarHorizontalChart))
                {
                    if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie != NVD3SerieDefinition.MultiBarHorizontalChart)) throw new Exception("Invalid chart configuration: Cannot mix NVD3 Horizontal Bar Serie with another type.");
                    page.NVD3ChartType = "multiBarHorizontalChart";
                }
                else if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.LineWithFocusChart))
                {
                    if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie != NVD3SerieDefinition.LineWithFocusChart)) throw new Exception("Invalid chart configuration: Cannot mix NVD3 Line with focus Serie with another type.");
                    page.NVD3ChartType = "lineWithFocusChart";
                }
                else if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.DiscreteBarChart))
                {
                    if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie != NVD3SerieDefinition.DiscreteBarChart)) throw new Exception("Invalid chart configuration: Cannot mix NVD3 Discrete Bar Serie with another type.");
                    page.NVD3ChartType = "discreteBarChart";
                }
                else if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.CumulativeLineChart))
                {
                    if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie != NVD3SerieDefinition.CumulativeLineChart)) throw new Exception("Invalid chart configuration: Cannot mix NVD3 Cumulative Line Serie with another type.");
                    page.NVD3ChartType = "cumulativeLineChart";
                }
                else if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.StackedAreaChart))
                {
                    hasArea = true;
                    if (!Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie != NVD3SerieDefinition.StackedAreaChart))
                    {
                        //if primary and secondary axis are used, keep the multi chart 
                        if (!(Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.StackedAreaChart && i.YAxisType == AxisType.Primary) &&
                            Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.StackedAreaChart && i.YAxisType == AxisType.Secondary)))
                            page.NVD3ChartType = "stackedAreaChart";
                    }
                }
                else if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.Line))
                {
                    hasLine = true;
                    if (!Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie != NVD3SerieDefinition.Line))
                    {
                        //if primary and secondary axis are used, keep the multi chart 
                        if (!(Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.Line && i.YAxisType == AxisType.Primary) &&
                            Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.Line && i.YAxisType == AxisType.Secondary)))
                            page.NVD3ChartType = "lineChart";
                    }
                }
                else if (Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.MultiBarChart))
                {
                    hasBar = true;
                    if (!Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie != NVD3SerieDefinition.MultiBarChart))
                    {
                        //if primary and secondary axis are used, keep the multi chart 
                        if (!(Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.MultiBarChart && i.YAxisType == AxisType.Primary) &&
                            Model.Elements.Exists(i => i.SerieDefinition == SerieDefinition.NVD3Serie && i.Nvd3Serie == NVD3SerieDefinition.MultiBarChart && i.YAxisType == AxisType.Secondary)))
                            page.NVD3ChartType = "multiBarChart";
                    }
                }

                //If mix of Line, Bar and Area -> we go for multiChart
                if (string.IsNullOrEmpty(page.NVD3ChartType) && (hasArea || hasBar || hasLine)) page.NVD3ChartType = "multiChart";
            }

        }
        public bool InitPageChart(ResultPage page)
        {
            if (Model != null)
            {
                try
                {
                    checkChartIntegrity(page);
                    if (Model.HasMicrosoftSerie)
                    {
                        Thread.CurrentThread.CurrentCulture = Report.ExecutionView.CultureInfo;
                        //reload it from configuration
                        _chartConfiguration = null;
                        initMicrosoftChartConfigurationSeries();
                        page.Chart = ChartConfiguration;
                        buildMicrosoftSeries(page);
                    }
                    else if (Model.HasNVD3Serie)
                    {
                        buildNVD3Series(page);
                    }
                }
                catch (Exception ex)
                {
                    _error = "Error got when generating chart:\r\n" + ex.Message;
                    return false;
                }
            }
            return true;
        }

        public bool GenerateMicrosoftChartImage(ResultPage page)
        {
            if (page.Chart != null)
            {
                CultureInfo initialCulture = Thread.CurrentThread.CurrentCulture;
                try
                {
                    Thread.CurrentThread.CurrentCulture = Report.ExecutionView.CultureInfo;
                    page.ChartPath = Report.GetChartFileName();
                    page.ChartFileName = Path.GetFileName(page.ChartPath);
                    page.Chart.SaveImage(page.ChartPath, ChartImageFormat.Png);
                }
                catch (Exception ex)
                {
                    _error = "Error got when generating chart:\r\n" + ex.Message;
                    return false;
                }
                finally
                {
                    Thread.CurrentThread.CurrentCulture = initialCulture;
                }
            }
            return true;
        }

        public ReportView GetView(string viewId)
        {
            if (ViewId == viewId) return this;

            ReportView result = null;
            foreach (var view in Views)
            {
                if (view.ViewId == viewId) return view;
                result = view.GetView(viewId);
                if (result != null) break;
            }
            return result;
        }

        public void ReinitGUIDChildren()
        {
            foreach (ReportView child in Views)
            {
                child.GUID = Guid.NewGuid().ToString();
                child.ReinitGUIDChildren();
            }
        }


        [XmlIgnore]
        public bool HasExternalViewer
        {
            get
            {
                return !string.IsNullOrEmpty(ExternalViewerExtension);
            }
        }

        [XmlIgnore]
        public string ExternalViewerExtension
        {
            get
            {
                return Views.Where(i => !string.IsNullOrEmpty(i.Template.ExternalViewerExtension)).Max(i => i.Template.ExternalViewerExtension);
            }
        }

    }
}
