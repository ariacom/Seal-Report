//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using Seal.Helpers;
using System.ComponentModel;
using Seal.Converter;
using Seal.Forms;
using System.Drawing.Design;
using DynamicTypeDescriptor;
using RazorEngine.Templating;
using System.Globalization;
using System.Web;
using System.Data;

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
                GetProperty("ModelGUID").SetIsBrowsable(Template.ForReportModel);
                GetProperty("TemplateName").SetIsBrowsable(true);
                GetProperty("TemplateDescription").SetIsBrowsable(true);

                //Set culture only on master view
                GetProperty("CultureName").SetIsBrowsable(IsRootView);
                GetProperty("UseCustomTemplate").SetIsBrowsable(true);
                GetProperty("CustomTemplate").SetIsBrowsable(true);
                GetProperty("PartialTemplates").SetIsBrowsable(PartialTemplates.Count > 0);

                GetProperty("TemplateConfiguration").SetIsBrowsable(Parameters.Count > 0);
                GetProperty("PdfConverter").SetIsBrowsable(Template.Name == ReportViewTemplate.ReportName);
                PdfConverter.InitEditor();

                GetProperty("ExcelConverter").SetIsBrowsable(true);
                ExcelConverter.InitEditor();

                GetProperty("WidgetDefinition").SetIsBrowsable(Template.Name != ReportViewTemplate.ReportName); //No widget from the root view...
                GetProperty("WebExec").SetIsBrowsable(Template.Name == ReportViewTemplate.ReportName);

                //Read only
                GetProperty("TemplateName").SetIsReadOnly(true);
                GetProperty("CustomTemplate").SetIsReadOnly(!UseCustomTemplate);

                //Helpers
                GetProperty("HelperReloadConfiguration").SetIsBrowsable(true);
                GetProperty("HelperResetParameters").SetIsBrowsable(true);
                GetProperty("HelperResetPDFConfigurations").SetIsBrowsable(true);
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
        }

        #endregion

        public static ReportView Create(ReportViewTemplate template)
        {
            ReportView result = new ReportView() { GUID = Guid.NewGuid().ToString(), TemplateName = template.Name };
            return result;
        }

        public void InitReferences()
        {
            AddDefaultModelViews();
            foreach (var childView in Views)
            {
                childView.ParentView = this;
                childView.Report = Report;
                childView.InitReferences();

                //backward compatibility for CSV before 4.0...
                if (childView.TemplateName == "Model CSV Excel" && TemplateName == ReportViewTemplate.ReportName)
                {
                    InitParameters(false);
                    SetParameter(Parameter.ReportFormatParameter, ReportFormat.csv.ToString());
                }
            }
        }

        public void InitParameters(List<Parameter> configParameters, List<Parameter> parameters, bool resetValues)
        {
            var initialParameters = parameters.ToList();
            parameters.Clear();
            foreach (var configParameter in configParameters)
            {
                Parameter parameter = initialParameters.FirstOrDefault(i => i.Name == configParameter.Name);
                if (parameter == null) parameter = new Parameter() { Name = configParameter.Name, Value = configParameter.Value };

                parameters.Add(parameter);
                if (resetValues) parameter.Value = configParameter.Value;
                parameter.Enums = configParameter.Enums;
                parameter.Description = configParameter.Description;
                parameter.Type = configParameter.Type;
                parameter.UseOnlyEnumValues = configParameter.UseOnlyEnumValues;
                parameter.DisplayName = configParameter.DisplayName;
                parameter.ConfigValue = configParameter.Value;
                parameter.EditorLanguage = configParameter.EditorLanguage;
                parameter.TextSamples = configParameter.TextSamples;
            }

            if (TemplateName == "Report")
            {
                //backward compatibility for format before 4.0...
                if (initialParameters.Exists(i => i.Name == "excel_layout" && i.Value == "True")) SetParameter(Parameter.ReportFormatParameter, "excel");
                else if (initialParameters.Exists(i => i.Name == "pdf_layout" && i.Value == "True")) SetParameter(Parameter.ReportFormatParameter, "pdf");
                else if (initialParameters.Exists(i => i.Name == "print_layout" && i.Value == "True")) SetParameter(Parameter.ReportFormatParameter, "print");
                if (initialParameters.Exists(i => i.Name == "display_messages" && i.Value == "True")) SetParameter("messages_mode", "enabledshown");
            }
        }

        //Temporary variables to help for report serialization...
        private List<Parameter> _tempParameters;

        public void BeforeSerialization()
        {
            _tempParameters = Parameters.ToList();
            //Remove parameters identical to config
            Parameters.RemoveAll(i => i.Value == null || i.Value == i.ConfigValue);

            //Remove empty custom template
            _partialTemplates.RemoveAll(i => string.IsNullOrWhiteSpace(i.Text));

            foreach (var view in Views) view.BeforeSerialization();
        }

        public void AfterSerialization()
        {
            Parameters = _tempParameters;
            InitPartialTemplates();

            foreach (var view in Views) view.AfterSerialization();
        }

        public void ReloadConfiguration()
        {
            _template = null;
            var t = Template;
            _information = "Configuration has been reloaded.";
        }

        public void InitParameters(bool resetValues)
        {
            if (Report == null || Template == null) return;

            InitParameters(Template.Parameters, _parameters, resetValues);
            _error = Template.Error;
            _information = "";
            if (resetValues) _information += "Values have been reset";
            if (!string.IsNullOrEmpty(_information)) _information = Helper.FormatMessage(_information);
        }

        public void AddDefaultModelViews()
        {
            if (TemplateName == ReportViewTemplate.ModelName && Views.Count == 0)
            {
                var containerView = Report.AddChildView(this, ReportViewTemplate.ModelContainerName);
                Report.AddChildView(containerView, ReportViewTemplate.PageTableName);
                Report.AddChildView(containerView, ReportViewTemplate.ChartJSName);
                Report.AddChildView(containerView, ReportViewTemplate.ChartNVD3Name);
                Report.AddChildView(containerView, ReportViewTemplate.ChartPlotlyName);
                Report.AddChildView(containerView, ReportViewTemplate.DataTableName);
            }
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

        public string GetValueOrDefault(string name)
        {
            Parameter parameter = _parameters.FirstOrDefault(i => i.Name == name);
            if (parameter != null)
            {
                if (string.IsNullOrEmpty(parameter.Value)) return parameter.ConfigValue;
                else return parameter.Value;
            }
            return "";
        }

        public string AddAttribute(string attrName, string paramName)
        {
            return Helper.AddAttribute(attrName, GetValue(paramName));
        }

        public string ChartTitle
        {
            get
            {
                var view = Report.FindViewFromTemplate(Views, ReportViewTemplate.ChartJSName);
                if (view != null) return view.GetValue("chartjs_title");
                view = Report.FindViewFromTemplate(Views, ReportViewTemplate.ChartNVD3Name);
                if (view != null) return view.GetValue("nvd3_chart_title");
                view = Report.FindViewFromTemplate(Views, ReportViewTemplate.ChartPlotlyName);
                if (view != null) return view.GetValue("plotly_title");
                return "";
            }
        }

        public string GetTranslatedMappedLabel(string text)
        {
            string result = text;
            if (!string.IsNullOrEmpty(text) && text.Count(i => i == '%') > 1)
            {
                List<ReportElement> values = new List<ReportElement>();
                foreach (var element in Model.Elements)
                {
                    if (result.Contains("%" + element.DisplayNameEl + "%"))
                    {
                        result = result.Replace("%" + element.DisplayNameEl + "%", string.Format("%{0}%", values.Count));
                        values.Add(element);
                    }
                }
                //Translate it
                result = Report.TranslateGeneral(result);
                int i = 0;
                foreach (var element in values.OrderBy(j => j.DisplayNameEl))
                {
                    result = result.Replace(string.Format("%{0}%", i++), element.DisplayNameElTranslated);
                }
            }
            else
            {
                result = Report.TranslateGeneral(text);
            }
            return result;
        }

        public void ReplaceInParameterValues(string paramName, string pattern, string text)
        {
            foreach (var param in Parameters.Where(i => i.Name == paramName && !string.IsNullOrEmpty(i.Value)))
            {
                param.Value = param.Value.Replace(pattern, text);
            }

            foreach (var child in Views)
            {
                child.ReplaceInParameterValues(paramName, pattern, text);
            }
        }

        public Parameter GetParameter(string name)
        {
            var result = _parameters.FirstOrDefault(i => i.Name == name);
            return result;
        }

        public void SetParameter(string name, string value)
        {
            var result = _parameters.FirstOrDefault(i => i.Name == name);
            if (result != null) result.Value = value;
        }

        public void SetParameter(string name, bool value)
        {
            var result = _parameters.FirstOrDefault(i => i.Name == name);
            if (result != null) result.BoolValue = value;
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

        public bool GetBoolValue(string name, bool defaultValue)
        {
            Parameter parameter = _parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? defaultValue : parameter.BoolValue;
        }

        public int GetNumericValue(string name)
        {
            Parameter parameter = _parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? 0 : parameter.NumericValue;
        }

        public bool IsRootView
        {
            get { return Template.ParentNames.Count == 0; }
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

        public string ViewName
        {
            get
            {
                if (Report != null) return Report.TranslateViewName(Name);
                return Name;
            }
        }

        string _templateName;
        [DisplayName("Template name"), Description("The name of the view template. View templates are defined in the repository Views folder."), Category("Definition"), Id(1, 1)]
        public string TemplateName
        {
            get
            {
                if (_templateName.EndsWith(" HTML")) return _templateName.Replace(" HTML", ""); //backward compatibility
                return _templateName;
            }
            set { _templateName = value; }
        }

        [DisplayName("Template Description"), Description("The description of the template."), Category("Definition"), Id(2, 1)]
        public string TemplateDescription
        {
            get
            {
                return Template != null ? Template.Description : "";
            }
        }

        string _modelGUID;
        [DisplayName("Model"), Description("The data model used for the view."), Category("Definition"), Id(3, 1)]
        [TypeConverter(typeof(ReportModelConverter))]
        public string ModelGUID
        {
            get { return _modelGUID; }
            set
            {
                _modelGUID = value;
                UpdateEditorAttributes();
            }
        }
        public bool ShouldSerializeModelGUID() { return !string.IsNullOrEmpty(_modelGUID); }


        public void InitPartialTemplates()
        {
            //Init partial templates
            foreach (var partialPath in Template.PartialTemplatesPath)
            {
                var partialName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(partialPath));
                var pt = _partialTemplates.FirstOrDefault(i => i.Name == partialName);
                if (pt == null)
                {
                    pt = new ReportViewPartialTemplate() { Name = partialName, UseCustom = false, Text = "" };
                    _partialTemplates.Add(pt);
                }
                pt.View = this;
            }
            //Remove unused
            _partialTemplates.RemoveAll(i => !Template.PartialTemplatesPath.Exists(j => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(j)) == i.Name));
        }

        ReportViewTemplate _template = null;
        public ReportViewTemplate Template
        {
            get
            {
                if (_template == null)
                {
                    _template = RepositoryServer.GetViewTemplate(TemplateName);
                    if (_template == null)
                    {
                        _template = new ReportViewTemplate() { Name = TemplateName };
                        _error = string.Format("Unable to find template named '{0}'. Check your repository Views folder.", TemplateName);
                    }
                    else
                    {
                        _error = _template.Error;
                    }
                    InitPartialTemplates();
                    InitParameters(false);
                }
                return _template;
            }
        }

        public List<ReportView> Views = new List<ReportView>();
        public bool ShouldSerializeViews() { return Views.Count > 0; }


        bool _useCustomTemplate = false;
        [DisplayName("Use custom template text"), Description("If true, the template text can be modified."), Category("Custom template texts"), Id(2, 3)]
        [DefaultValue(false)]
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
        [DisplayName("Custom template"), Description("The custom template text used instead of the template defined by the template name."), Category("Custom template texts"), Id(3, 3)]
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

        List<ReportViewPartialTemplate> _partialTemplates = new List<ReportViewPartialTemplate>();
        [DisplayName("Custom Partial Templates"), Description("The custom partial template texts for the view."), Category("Custom template texts"), Id(4, 3)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<ReportViewPartialTemplate> PartialTemplates
        {
            get { return _partialTemplates; }
            set { _partialTemplates = value; }
        }
        public bool ShouldSerializePartialTemplates() { return _partialTemplates.Count > 0; }


        List<Parameter> _parameters = new List<Parameter>();
        public List<Parameter> Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }
        public bool ShouldSerializeParameters() { return _parameters.Count > 0; }


        string _cultureName = "";
        [DisplayName("Culture name"), Description("The language and culture used to display the report. If empty, the default culture is used."), Category("View parameters"), Id(2, 4)]
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
        public bool ShouldSerializeCultureName() { return !string.IsNullOrEmpty(_cultureName); }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("Template configuration"), Description("The view configuration values."), Category("View parameters"), Id(3, 4)]
        [XmlIgnore]
        public ParametersEditor TemplateConfiguration
        {
            get
            {
                var editor = new ParametersEditor();
                editor.Init(_parameters);
                return editor;
            }
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

        CultureInfo _cultureInfo = null;
        public CultureInfo CultureInfo
        {
            get
            {
                //Culture from the view if specified
                if (_cultureInfo == null && !string.IsNullOrEmpty(_cultureName))
                {
                    var culture = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(i => i.EnglishName == _cultureName);
                    if (culture != null) _cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(i => i.EnglishName == _cultureName).Clone() as CultureInfo;
                }
                //Culture from the execution view
                if (_cultureInfo == null && Report.ExecutionView != this && Report.ExecutionView != null) _cultureInfo = Report.CultureInfo.Clone() as CultureInfo;
                //Culture from the repository
                if (_cultureInfo == null) _cultureInfo = Report.Repository.CultureInfo.Clone() as CultureInfo;
                return _cultureInfo;
            }
        }

        public void SetAdvancedConfigurations()
        {
            //Pdf & Excel
            if (PdfConverterEdited)
            {
                _pdfConfigurations = PdfConverter.GetConfigurations();
            }
            if (ExcelConverterEdited)
            {
                _excelConfigurations = ExcelConverter.GetConfigurations();
            }

            foreach (var view in Views) view.SetAdvancedConfigurations();
        }

        #region Web Report Server and Dashboard Widgets

        private bool _webExec = true;
        [Category("Web Report Server and Dashboard"), DisplayName("Web execution"), Description("For the Web Report Server: If true, the view can be executed from the report list."), Id(2, 6)]
        [DefaultValue(true)]
        public bool WebExec
        {
            get { return _webExec; }
            set { _webExec = value; }
        }


        public bool ShouldSerializeWidgetDefinition() {
            return _widgetDefinition.IsPublished || !string.IsNullOrEmpty(_widgetDefinition.GUID);
        }

        private DashboardWidget _widgetDefinition = new DashboardWidget();
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("Widget definition"), Description("Settings to publish the view as a dashboard widget. If a name is specified, the widget can be selected to build dashboards from the Web Report Server."), Category("Web Report Server and Dashboard"), Id(3, 6)]
        public DashboardWidget WidgetDefinition
        {
            get { return _widgetDefinition; }
            set { _widgetDefinition = value; }
        }


        #endregion


        [XmlIgnore]
        public ReportModel Model
        {
            get
            {
                if (string.IsNullOrEmpty(_modelGUID)) return null;
                return _report.Models.FirstOrDefault(i => i.GUID == _modelGUID);
            }
        }
        [XmlIgnore]
        public ReportView ParentView { get; set; } = null;

        #region PDF and Excel Converters

        private List<string> _pdfConfigurations = new List<string>();
        public List<string> PdfConfigurations
        {
            get { return _pdfConfigurations; }
            set { _pdfConfigurations = value; }
        }
        public bool ShouldSerializePdfConfigurations() {
            bool result = false;
            if (Report.Repository.Configuration.PdfConfigurations.Count == 0)
            {
                result = _pdfConfigurations.Count > 0;
            }
            else
            {
                for (int i = 0; i < _pdfConfigurations.Count && i < Report.Repository.Configuration.PdfConfigurations.Count; i++)
                {
                    if (_pdfConfigurations[i].Trim().Replace("\r\n", "\n") != Report.Repository.Configuration.PdfConfigurations[i].Trim().Replace("\r\n", "\n"))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        private SealPdfConverter _pdfConverter = null;
        [XmlIgnore]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("PDF Configuration"), Description("All the options applied to the PDF conversion from the HTML result."), Category("View parameters"), Id(7, 4)]
        public SealPdfConverter PdfConverter
        {
            get
            {
                if (_pdfConverter == null)
                {
                    _pdfConverter = SealPdfConverter.Create(Report.Repository.ApplicationPath);
                    if (PdfConfigurations.Count == 0) PdfConfigurations = Report.Repository.Configuration.PdfConfigurations.ToList();
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
        public bool ShouldSerializeExcelConfigurations() {
            bool result = false;

            if (Report.Repository.Configuration.ExcelConfigurations.Count == 0)
            {
                result = _excelConfigurations.Count > 0;
            }
            else
            {
                for (int i = 0; i < _excelConfigurations.Count && i < Report.Repository.Configuration.ExcelConfigurations.Count; i++)
                {
                    if (_excelConfigurations[i].Trim().Replace("\r\n", "\n") != Report.Repository.Configuration.ExcelConfigurations[i].Trim().Replace("\r\n", "\n"))
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        private SealExcelConverter _excelConverter = null;
        [XmlIgnore]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("Excel Configuration"), Description("All the options applied to the Excel conversion from the view."), Category("View parameters"), Id(8, 4)]
        public SealExcelConverter ExcelConverter
        {
            get
            {
                if (_excelConverter == null)
                {
                    _excelConverter = SealExcelConverter.Create(Report.Repository.ApplicationPath);
                    if (ExcelConfigurations.Count == 0) ExcelConfigurations = Report.Repository.Configuration.ExcelConfigurations.ToList();

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
        [Category("Helpers"), DisplayName("Reload template configuration"), Description("Load the template configuration file."), Id(2, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperReloadConfiguration
        {
            get { return "<Click to reload the template configuration and refresh the parameters>"; }
        }

        [Category("Helpers"), DisplayName("Reset template parameter values"), Description("Reset all template parameters to their default values."), Id(3, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetParameters
        {
            get { return "<Click to reset the view parameters to their default values>"; }
        }

        [Category("Helpers"), DisplayName("Reset PDF configurations"), Description("Reset PDF configuration values to their default values."), Id(7, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetPDFConfigurations
        {
            get { return "<Click to reset the PDF configuration values to their default values>"; }
        }

        [Category("Helpers"), DisplayName("Reset Excel configurations"), Description("Reset Excel configuration values to their default values."), Id(8, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetExcelConfigurations
        {
            get { return "<Click to reset the Excel configuration values to their default values>"; }
        }

        string _information;
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(9, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        string _error;
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(10, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        #endregion


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


        public string GetPartialTemplateKey(string name, Object model)
        {
            var path = Template.GetPartialTemplatePath(name);
            var partial = PartialTemplates.FirstOrDefault(i => i.Name == name);
            if (!File.Exists(path) || partial == null) throw new Exception(string.Format("Unable to find partial template named '{0}'. Check the name and the file (.partial.cshtml) in the Views folder...", name));

            string key, text = null;
            if (partial.UseCustom && !string.IsNullOrWhiteSpace(partial.Text))
            {
                //custom template
                key = string.Format("REP:{0}_{1}_{2}_{3}", Report.FilePath, GUID, partial.Name, partial.LastTemplateModification.ToString("s"));
                text = partial.Text;
            }
            else
            {
                key = string.Format("TPL:{0}_{1}", path, File.GetLastWriteTime(path).ToString("s"));
            }

            try
            {
                if (string.IsNullOrEmpty(text)) text = Template.GetPartialTemplateText(name);
                RazorHelper.Compile(text, model.GetType(), key);
            }
            catch (Exception ex)
            {
                var message = (ex is TemplateCompilationException ? Helper.GetExceptionMessage((TemplateCompilationException)ex) : ex.Message);
                _error += string.Format("Execution error when compiling the partial view template '{0}({1})':\r\n{2}\r\n", Name, name, message);
                if (ex.InnerException != null) _error += "\r\n" + ex.InnerException.Message;
                Report.ExecutionErrors += _error;
                throw ex;
            }
            return key;
        }

        public string Parse()
        {
            string result = "";
            string phase = "compiling";
            _error = "";

            try
            {
                Report.CurrentView = this;
                string key = "";
                if (!UseCustomTemplate || string.IsNullOrWhiteSpace(CustomTemplate))
                {
                    //template -> file path + last modification
                    key = string.Format("TPL:{0}_{1}", Template.FilePath, File.GetLastWriteTime(Template.FilePath).ToString("s"));
                }
                else
                {
                    //view -> report path + last modification
                    key = string.Format("REP:{0}_{1}_{2}", Report.FilePath, GUID, _lastTemplateModification.ToString("s"));
                }

                if (Template.ForReportModel && Model == null)
                {
                    Report.ExecutionMessages += string.Format("Warning for view '{0}': Model has been lost for the view. Switching to the first model of the report...", Name);
                    _modelGUID = Report.Models[0].GUID;
                }
                phase = "executing";
                result = RazorHelper.CompileExecute(ViewTemplateText, Report, key);

                //For CSV, add just one new line
                if (Report.Format == ReportFormat.csv) {
                    result = result.Trim() + "\r\n" + (result.EndsWith("\r\n") ? "\r\n" : "");
                }
            }
            catch (Exception ex)
            {
                var message = (ex is TemplateCompilationException ? Helper.GetExceptionMessage((TemplateCompilationException)ex) : ex.Message);
                _error += string.Format("Error got when {0} the view '{1}({2})':\r\n{3}\r\n", phase, Name, Template.Name, message);
                if (ex.InnerException != null) _error += "\r\n" + ex.InnerException.Message;
            }
            if (!string.IsNullOrEmpty(_error))
            {
                Report.ExecutionErrors += _error;
                result = Helper.ToHtml(_error);
            }

            return result;
        }

        public string ParseChildren()
        {
            string result = "";
            foreach (ReportView view in Views.OrderBy(i => i.SortOrder))
            {
                if (view.Report.Format == ReportFormat.csv && !view.Template.ForReportModel)
                {
                    //add result for csv only for model
                    result += view.ParseChildren();
                }
                else
                {
                    result += view.Parse();
                }
            }
            return result;
        }

        public string[] GetGridLayoutRows(string gridLayout)
        {
            if (string.IsNullOrEmpty(gridLayout)) return new string[] { "" };
            return gridLayout.Replace("\r\n", "\n").Split('\n').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
        }
        public string[] GetGridLayoutColumns(string rowLayout)
        {
            if (string.IsNullOrEmpty(rowLayout)) return new string[] { "" };
            return rowLayout.Trim().Split(';').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
        }

        public string GetGridLayoutColumnClass(string column)
        {
            if (!IsGridLayoutColumnForModel(column)) return column.Substring(1, column.Length - 2);
            return column;
        }

        public bool IsGridLayoutColumnForModel(string column)
        {
            return !(column.StartsWith("(") && column.EndsWith(")"));
        }

        public void InitTemplates(ReportView view, ref string errors)
        {
            view.InitParameters(false);
            if (!string.IsNullOrEmpty(view.Error)) errors += string.Format("Error in view template '{0}': {1}\r\n", view.Name, view.Error);
            foreach (var child in view.Views) InitTemplates(child, ref errors);
        }

        private void initAxisProperties(ResultPage page, List<ResultCell[]> XDimensions)
        {
            bool hasPie = Model.Elements.Exists(i => (i.Nvd3Serie == NVD3SerieDefinition.PieChart || i.ChartJSSerie == ChartJSSerieDefinition.Pie || i.ChartJSSerie == ChartJSSerieDefinition.PolarArea || i.PlotlySerie == PlotlySerieDefinition.Pie) && i.PivotPosition == PivotPosition.Data);
            var dimensions = XDimensions.FirstOrDefault();
            if (dimensions != null)
            {
                //One value -> set the raw value, several values -> concat the display value
                if (dimensions.Length == 1)
                {
                    if (!dimensions[0].Element.IsEnum && dimensions[0].Element.AxisUseValues && !hasPie)
                    {
                        Model.ExecChartIsNumericAxis = dimensions[0].Element.IsNumeric;
                        Model.ExecChartIsDateTimeAxis = dimensions[0].Element.IsDateTime;
                        Model.ExecD3XAxisFormat = dimensions[0].Element.GetD3Format(CultureInfo, Model.ExecNVD3ChartType);
                        Model.ExecMomentJSXAxisFormat = dimensions[0].Element.GetMomentJSFormat(CultureInfo);
                    }
                }
            }
        }

        private Dictionary<object, object> initXValues(ResultPage page, List<ResultCell[]> XDimensions)
        {
            Dictionary<object, object> result = new Dictionary<object, object>();
            foreach (var dimensions in XDimensions)
            {
                //One value -> set the raw value, several values -> concat the display value
                if (dimensions.Length == 1)
                {

                    if (!dimensions[0].Element.IsEnum && dimensions[0].Element.AxisUseValues)
                    {
                        result.Add(dimensions, dimensions[0].Value);
                    }
                    else
                    {
                        result.Add(dimensions, dimensions[0].DisplayValue);
                    }
                }
                else result.Add(dimensions, Helper.ConcatCellValues(dimensions, ","));
            }

            return result;
        }

        private void initChartXValues(ResultPage page)
        {
            //Build list of X Values
            page.PrimaryXValues = initXValues(page, page.PrimaryXDimensions);
            page.SecondaryXValues = initXValues(page, page.SecondaryXDimensions);
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

        private void buildChartSeries(ResultPage page)
        {
            if (page.ChartInitDone) return;

            initAxisProperties(page, page.PrimaryXDimensions);
            //Sort series if necessary, only one serie is used for sorting...
            if (!Model.ExecChartIsNumericAxis && !Model.ExecChartIsDateTimeAxis)
            {
                var rootSerie = page.Series.FirstOrDefault(i => i.Element.SerieSortType != SerieSortType.None);
                if (rootSerie != null)
                {
                    _serieForSort = new ResultSerie() { Element = rootSerie.Element };
                    if (_serieForSort.Element.SerieSortType == SerieSortType.Y)
                    {
                        foreach (var dimension in page.PrimaryXDimensions)
                        {
                            //add the values of all series of this element for the sort
                            foreach (var serie in page.Series.Where(i => i.Element == rootSerie.Element))
                            {
                                ResultSerieValue sortValue = _serieForSort.Values.FirstOrDefault(i => i.XDimensionValues == dimension);
                                if (sortValue == null)
                                {
                                    sortValue = new ResultSerieValue() { XDimensionValues = dimension };
                                    sortValue.Yvalue = new ResultTotalCell() { Element = rootSerie.Element, IsSerie = true };
                                    _serieForSort.Values.Add(sortValue);
                                }
                                ResultSerieValue serieValue = serie.Values.FirstOrDefault(i => i.XDimensionValues == dimension);
                                if (serieValue != null) sortValue.Yvalue.Cells.Add(new ResultCell() { Element = rootSerie.Element, Value = serieValue.Yvalue.Value });
                            }
                        }
                        foreach (var serieValue in _serieForSort.Values)
                        {
                            //Classic calculation
                            serieValue.Yvalue.Calculate();
                        }
                        page.PrimaryXDimensions.Sort(CompareXDimensionsWithSeries);
                    }
                    else page.PrimaryXDimensions.Sort(CompareXDimensionsWithAxis);
                }
            }
            initChartXValues(page);

            page.AxisXLabelMaxLen = 0;
            page.AxisYPrimaryMaxLen = 0;
            page.AxisYSecondaryMaxLen = 0;

            StringBuilder result = new StringBuilder(), navs = new StringBuilder();
            //Build X labels
            foreach (var key in page.PrimaryXValues.Keys)
            {
                ResultCell[] dimensions = key as ResultCell[];
                if (result.Length != 0) result.Append(",");
                var xval = (dimensions.Length == 1 ? dimensions[0].DisplayValue : page.PrimaryXValues[key].ToString());
                result.Append(Helper.QuoteSingle(HttpUtility.JavaScriptStringEncode(xval)));
                if (xval.Length > page.AxisXLabelMaxLen) page.AxisXLabelMaxLen = xval.Length;

                var navigation = Model.GetNavigation(((ResultCell[])key)[0]);
                if (!string.IsNullOrEmpty(navigation))
                {
                    if (navs.Length != 0) navs.Append(",");
                    navs.Append(navigation);
                }
            }

            page.ChartXLabels = result.ToString();
            page.ChartNavigations = navs.ToString();

            foreach (ResultSerie resultSerie in page.Series)
            {
                if (Report.Cancel) break;

                if (string.IsNullOrEmpty(Model.ExecD3PrimaryYAxisFormat) && resultSerie.Element.YAxisType == AxisType.Primary)
                {
                    Model.ExecD3PrimaryYAxisFormat = resultSerie.Element.GetD3Format(CultureInfo, Model.ExecNVD3ChartType);
                    Model.ExecAxisPrimaryYIsDateTime = resultSerie.Element.IsDateTime;
                }
                else if (string.IsNullOrEmpty(Model.ExecD3SecondaryYAxisFormat) && resultSerie.Element.YAxisType == AxisType.Secondary)
                {
                    Model.ExecD3SecondaryYAxisFormat = resultSerie.Element.GetD3Format(CultureInfo, Model.ExecNVD3ChartType);
                    Model.ExecAxisSecondaryYIsDateTime = resultSerie.Element.IsDateTime;
                }

                //Fill Serie
                StringBuilder chartXResult = new StringBuilder(), chartXDateTimeResult = new StringBuilder(), chartYResult = new StringBuilder(), chartYDateResult = new StringBuilder();
                StringBuilder chartXYResult = new StringBuilder(), chartYXResult = new StringBuilder(), chartYDisplayResult = new StringBuilder();
                int index = 0;
                foreach (var xDimensionKey in page.PrimaryXValues.Keys)
                {
                    string xValue = (index++).ToString(CultureInfo.InvariantCulture.NumberFormat);
                    DateTime xValueDT = DateTime.MinValue, yValueDT = DateTime.MinValue;

                    //Find the corresponding serie value...
                    ResultSerieValue value = resultSerie.Values.FirstOrDefault(i => i.XDimensionValues == xDimensionKey);
                    object yValue = (value != null ? value.Yvalue.Value : null);
                    if (resultSerie.Element.YAxisType == AxisType.Primary && value != null && value.Yvalue.DisplayValue.Length > page.AxisYPrimaryMaxLen) page.AxisYPrimaryMaxLen = value.Yvalue.DisplayValue.Length;
                    if (resultSerie.Element.YAxisType == AxisType.Secondary && value != null && value.Yvalue.DisplayValue.Length > page.AxisYSecondaryMaxLen) page.AxisYSecondaryMaxLen = value.Yvalue.DisplayValue.Length;

                    if (Model.ExecChartIsNumericAxis)
                    {
                        Double db = 0;
                        if (value == null) Double.TryParse(page.PrimaryXValues[xDimensionKey].ToString(), out db);
                        else if (value.XDimensionValues[0].DoubleValue != null) db = value.XDimensionValues[0].DoubleValue.Value;
                        xValue = db.ToString(CultureInfo.InvariantCulture.NumberFormat);
                    }
                    else if (Model.ExecChartIsDateTimeAxis)
                    {
                        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                        if (value == null) dt = ((DateTime)page.PrimaryXValues[xDimensionKey]);
                        else if (value.XDimensionValues[0].DateTimeValue != null) dt = value.XDimensionValues[0].DateTimeValue.Value;
                        xValueDT = dt;
                        TimeSpan diff = dt.ToUniversalTime() - (new DateTime(1970, 1, 1, 0, 0, 0, 0));
                        xValue = string.Format("{0}000", Math.Floor(diff.TotalSeconds));
                    }

                    if (yValue is DateTime)
                    {
                        yValueDT = (DateTime)yValue;
                        TimeSpan diff = yValueDT.ToUniversalTime() - (new DateTime(1970, 1, 1, 0, 0, 0, 0));
                        yValue = string.Format("{0}000", Math.Floor(diff.TotalSeconds));
                    }
                    else if (yValue is Double)
                    {
                        yValue = ((Double)yValue).ToString(CultureInfo.InvariantCulture.NumberFormat);
                    }

                    if (yValue == null && GetBoolValue(Parameter.NVD3AddNullPointParameter))
                    {
                        yValue = "0";
                    }
                    if (yValue != null)
                    {
                        if (chartXYResult.Length != 0) chartXYResult.Append(",");
                        chartXYResult.AppendFormat("{{x:{0},y:{1}}}", xValue, yValue);

                        if (chartYXResult.Length != 0) chartYXResult.Append(",");
                        chartYXResult.AppendFormat("{{x:{0},y:{1}}}", yValue, xValue);

                        if (chartXResult.Length != 0) chartXResult.Append(",");
                        chartXResult.AppendFormat("{0}", xValue);

                        if (Model.ExecChartIsDateTimeAxis)
                        {
                            if (chartXDateTimeResult.Length != 0) chartXDateTimeResult.Append(",");
                            chartXDateTimeResult.AppendFormat("\"{0:yyyy-MM-dd HH:mm:ss}\"", xValueDT);
                        }

                        if (chartYResult.Length != 0) chartYResult.Append(",");
                        chartYResult.AppendFormat("{0}", yValue);

                        if (yValueDT != DateTime.MinValue)
                        {
                            if (chartYDateResult.Length != 0) chartYDateResult.Append(",");
                            chartYDateResult.AppendFormat("\"{0:yyyy-MM-dd HH:mm:ss}\"", yValueDT);
                        }
                    }
                }
                resultSerie.ChartXYSerieValues = chartXYResult.ToString();
                resultSerie.ChartYXSerieValues = chartYXResult.ToString();
                resultSerie.ChartXSerieValues = chartXResult.ToString();
                resultSerie.ChartXDateTimeSerieValues = chartXDateTimeResult.ToString();
                resultSerie.ChartYSerieValues = chartYResult.ToString();
                resultSerie.ChartYDateSerieValues = chartYDateResult.ToString();
            }
            page.ChartInitDone = true;
        }

        public bool InitPageChart(ResultPage page)
        {
            if (Model != null)
            {
                try
                {
                    if (Model.HasSerie && !page.ChartInitDone)
                    {
                        Model.CheckNVD3ChartIntegrity();
                        Model.CheckPlotlyChartIntegrity();
                        Model.CheckChartJSIntegrity();
                        buildChartSeries(page);
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
                if (!string.IsNullOrEmpty(child.WidgetDefinition.GUID)) child.WidgetDefinition.GUID = Guid.NewGuid().ToString();
                child.ReinitGUIDChildren();
            }
        }


        List<string> _columnsHidden = null;
        public bool IsColumnHidden(int col)
        {
            if (_columnsHidden == null) _columnsHidden = GetValue(Parameter.ColumnsHiddenParameter).Split(';').ToList();
            return _columnsHidden.Contains((col + 1).ToString());
        }
    }
}
