//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Converter;
using System.Drawing.Design;
using DynamicTypeDescriptor;
using System.IO;
using Seal.Forms;
using System.Windows.Forms.Design;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace Seal.Model
{
    public class SealServerConfiguration : RootComponent
    {
        [XmlIgnore]
        public string FilePath;

        [XmlIgnore]
        public Repository Repository;

        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("DefaultConnectionString").SetIsBrowsable(!ForPublication);
                GetProperty("TaskFolderName").SetIsBrowsable(!ForPublication);
                GetProperty("DefaultCulture").SetIsBrowsable(!ForPublication);
                GetProperty("LogoName").SetIsBrowsable(!ForPublication);
                GetProperty("WebProductName").SetIsBrowsable(!ForPublication);
                GetProperty("LogDays").SetIsBrowsable(!ForPublication);
                GetProperty("CsvSeparator").SetIsBrowsable(!ForPublication);
                GetProperty("NumericFormat").SetIsBrowsable(!ForPublication);
                GetProperty("DateTimeFormat").SetIsBrowsable(!ForPublication);
                GetProperty("InitScript").SetIsBrowsable(!ForPublication);
                GetProperty("TasksScript").SetIsBrowsable(!ForPublication);
                GetProperty("CommonScripts").SetIsBrowsable(!ForPublication);
                //GetProperty("CommonScripts").SetDisplayName("Common Scripts: " + (_commonScripts.Count == 0 ? "None" : _commonScripts.Count.ToString() + " Items(s)"));
                GetProperty("ReportCreationScript").SetIsBrowsable(!ForPublication);
                GetProperty("IsLocal").SetIsBrowsable(!ForPublication);
                GetProperty("FileReplacePatterns").SetIsBrowsable(!ForPublication);

                GetProperty("ExcelConverter").SetIsBrowsable(!ForPublication);
                GetProperty("PdfConverter").SetIsBrowsable(!ForPublication);
                GetProperty("HelperResetPDFConfigurations").SetIsBrowsable(!ForPublication);
                GetProperty("HelperResetExcelConfigurations").SetIsBrowsable(!ForPublication);
                if (!ForPublication)
                {
                    ExcelConverter.InitEditor();
                    PdfConverter.InitEditor();
                }

                GetProperty("WebApplicationPoolName").SetIsBrowsable(ForPublication);
                GetProperty("WebApplicationName").SetIsBrowsable(ForPublication);
                GetProperty("WebPublicationDirectory").SetIsBrowsable(ForPublication);

                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

        [XmlIgnore]
        public bool ForPublication = false;
        [Category("Server Settings"), DisplayName("Default Connection String"), Description("The OLE DB Default Connection String used when a new Data Source is created. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Id(1, 1)]
        public string DefaultConnectionString { get; set; } = "Provider=SQLOLEDB;data source=localhost;initial catalog=adb;Integrated Security=SSPI;";

        [Category("Server Settings"), DisplayName("Task Folder Name"), Description("The name of the Task Scheduler folder containg the schedules of the reports. Warning: Changing this name will affect all existing schedules !"), Id(2, 1)]
        public string TaskFolderName { get; set; } = Repository.SealRootProductName + " Report";

        [Category("Server Settings"), DisplayName("Logo file name"), Description("The logo file name used by the report templates. The file must be located in the Repository folder '<Repository Path>\\Views\\Images' and in the \\Images sub-folder of the Web publication directory. If empty, the Web Product Name is used as prefix."), Id(5, 1)]
        [DefaultValue("logo.png")]
        public string LogoName { get; set; } = "logo.png";

        [XmlIgnore]
        public bool HasLogo
        {
            get {
                if (string.IsNullOrEmpty(LogoName)) return false;
                return File.Exists(Path.Combine(Repository.ViewImagesFolder, LogoName));
            }
        }

        [Category("Server Settings"), DisplayName("Log days to keep"), Description("Number of days of log files to keep in the repository 'Logs' subfolder. If 0, the log feature is disabled."), Id(6, 1)]
        [DefaultValue(30)]
        public int LogDays { get; set; } = 30;

        [Category("Server Settings"), DisplayName("Web Product Name"), Description("The name of the product displayed on the Web site."), Id(7, 1)]
        public string WebProductName { get; set; } = "Seal Report";

        [Category("Server Settings"), DisplayName("Server is local (No internet)"), Description("If true, the programs will not access to Internet for external resources. All JavaScript's will be loaded locally (no use of CDN path)."), Id(8, 1)]
        [DefaultValue(true)]
        public bool IsLocal { get; set; } = true;

        [Category("Server Settings"), DisplayName("Patterns to replace in CSS or JScript"), Description("List of strings to replace when the report result is generated in a single HTML file (case of View Report Result or Output generation). This allow to specify the new font location in a CSS."), Id(9, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<FileReplacePattern> FileReplacePatterns { get; set; } = new List<FileReplacePattern>();
        public bool ShouldSerializeFileReplacePatterns() { return FileReplacePatterns.Count > 0; }

        [Category("Scripts"), DisplayName("Report Execution Init Script"), Description("If set, the script is executed when a report is initialized for an execution. Default values for report execution can be set here."), Id(4, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string InitScript { get; set; } = "";

        [Category("Scripts"), DisplayName("Report Creation Script"), Description("If set, the script is executed when a new report is created. Default values for report creation can be set here."), Id(5, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string ReportCreationScript { get; set; } = "";

        [Category("Scripts"), DisplayName("Tasks Script"), Description("If set, the script is added to all task scripts executed. This may be useful to defined common functions."), Id(6, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string TasksScript { get; set; } = "";

        [Category("Scripts"), DisplayName("Common Scripts"), Description("List of scripts added to all scripts executed during a report execution (not only for tasks). This may be useful to defined common functions for the report."), Id(7, 3)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<CommonScript> CommonScripts { get; set; } = new List<CommonScript>();

        public List<string> PdfConfigurations { get; set; } = new List<string>();
        public bool ShouldSerializePdfConfigurations() { return PdfConfigurations.Count > 0; }

        private SealPdfConverter _pdfConverter = null;
        [XmlIgnore]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("Default PDF Configuration"), Description("All the default options applied to the PDF conversion from the HTML result."), Category("PDF and Excel Converter"), Id(1, 4)]
        public SealPdfConverter PdfConverter
        {
            get
            {
                if (_pdfConverter == null)
                {
                    _pdfConverter = SealPdfConverter.Create(Repository.Instance.ApplicationPath);
                    _pdfConverter.SetConfigurations(PdfConfigurations, null);
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

        public List<string> ExcelConfigurations { get; set; } = new List<string>();
        public bool ShouldSerializeExcelConfigurations() { return ExcelConfigurations.Count > 0; }

        private SealExcelConverter _excelConverter = null;
        [XmlIgnore]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("Default Excel Configuration"), Description("All the default options applied to the Excel conversion from the view."), Category("PDF and Excel Converter"), Id(2, 4)]
        public SealExcelConverter ExcelConverter
        {
            get
            {
                if (_excelConverter == null)
                {
                    _excelConverter = SealExcelConverter.Create(Repository.Instance.ApplicationPath);
                    _excelConverter.SetConfigurations(ExcelConfigurations, null);
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

        [Category("PDF and Excel Converter"), DisplayName("Reset PDF configurations"), Description("Reset PDF configuration values to their default values."), Id(3, 4)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetPDFConfigurations
        {
            get { return "<Click to reset the PDF configuration values to their default values>"; }
        }

        [Category("PDF and Excel Converter"), DisplayName("Reset Excel configurations"), Description("Reset Excel configuration values to their default values."), Id(4, 4)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetExcelConfigurations
        {
            get { return "<Click to reset the Excel configuration values to their default values>"; }
        }
      

        [XmlIgnore]
        public string CommonScriptsHeader
        {
            get
            {
                var result = "";
                foreach (var script in CommonScripts) result += script.Script + "\r\n";
                return result;
            }
        }

        public string GetCommonScriptsHeader(CommonScript scriptBeingEdited)
        {
            var result = "";
            foreach (var script in CommonScripts.Where(i => i != scriptBeingEdited)) result += script.Script + "\r\n";
            return result;
        }


        string _defaultCulture = "";
        [Category("Formats"), DisplayName("Culture"), Description("The name of the culture used when a report is created. If not specified, the current culture of the server is used."), Id(1, 2)]
        [TypeConverter(typeof(Seal.Converter.CultureInfoConverter))]
        public string DefaultCulture
        {
            get { return _defaultCulture; }
            set { _defaultCulture = value; }
        }
 
        string _numericFormat = "N0";
        [Category("Formats"), DisplayName("Numeric Format"), Description("The numeric format used for numeric column having the default format"), Id(2, 2)]
        [TypeConverter(typeof(CustomFormatConverter))]
        [DefaultValue("N0")]
        public string NumericFormat
        {
            get { return _numericFormat; }
            set { _numericFormat = value; }
        }

        string _dateFormat = "d";
        [Category("Formats"), DisplayName("Date Time Format"), Description("The date time format used for date time column having the default format"), Id(3, 2)]
        [TypeConverter(typeof(CustomFormatConverter))]
        [DefaultValue("d")]
        public string DateTimeFormat
        {
            get { return _dateFormat; }
            set { _dateFormat = value; }
        }

        string _csvSeparator = "";
        [Category("Formats"), DisplayName("CSV Separator"), Description("If not specified in the report, separator used for the CSV template. If empty, the separator of the user culture is used."), Id(4, 2)]
        public string CsvSeparator
        {
            get { return _csvSeparator; }
            set { _csvSeparator = value; }
        }

        string _webApplicationPoolName = Repository.SealRootProductName + " Application Pool";
        [Category("Web Server IIS Publication"), DisplayName("Application Pool Name"), Description("The name of the IIS Application pool used by the web application."), Id(2, 1)]
        public string WebApplicationPoolName
        {
            get { return _webApplicationPoolName; }
            set { _webApplicationPoolName = value; }
        }

        string _webApplicationName = "/Seal";
        [Category("Web Server IIS Publication"), DisplayName("Application Name"), Description("The name of the IIS Web application. Use '/' to publish on 'Default Web Site'"), Id(1, 1)]
        public string WebApplicationName
        {
            get { return _webApplicationName; }
            set { _webApplicationName = value; }
        }

        string _webPublicationDirectory = "";
        [Category("Web Server IIS Publication"), DisplayName("Publication directory"), Description("The directory were the web site files are published."), Id(3, 1)]
        [EditorAttribute(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string WebPublicationDirectory
        {
            get { return _webPublicationDirectory; }
            set { _webPublicationDirectory = value; }
        }

        //Set by the server manager...
        string _installationDirectory = "";
        public string InstallationDirectory
        {
            get { return _installationDirectory;}
            set {_installationDirectory = value;}
        }

        [XmlIgnore]
        public DateTime LastModification;
        static public SealServerConfiguration LoadFromFile(string path, bool ignoreException)
        {
            SealServerConfiguration result = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SealServerConfiguration));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (SealServerConfiguration)serializer.Deserialize(xr);
                }
                result.FilePath = path;
                result.LastModification = File.GetLastWriteTime(path);
            }
            catch (Exception ex)
            {
                if (!ignoreException) throw new Exception(string.Format("Unable to read the configuration file '{0}'.\r\n{1}", path, ex.Message));
            }
            return result;
        }


        public void SaveToFile()
        {
            SaveToFile(FilePath);
        }

        public void SaveToFile(string path)
        {
            //Check last modification
            if (LastModification != DateTime.MinValue && File.Exists(path))
            {
                DateTime lastDateTime = File.GetLastWriteTime(path);
                if (LastModification != lastDateTime)
                {
                    throw new Exception("Unable to save the Server Configuration file. The file has been modified by another user.");
                }
            }
            var xmlOverrides = new XmlAttributeOverrides();
            XmlAttributes attrs = new XmlAttributes();
            attrs.XmlIgnore = true;
            xmlOverrides.Add(typeof(RootComponent), "Name", attrs);
            xmlOverrides.Add(typeof(RootComponent), "GUID", attrs);

            //Pdf & Excel
            if (PdfConverterEdited)
            {
                PdfConfigurations = PdfConverter.GetConfigurations();
            }
            if (ExcelConverterEdited)
            {
                ExcelConfigurations = ExcelConverter.GetConfigurations();
            }

#if !DEBUG
            //Set installation path, used by, to define schedules
            if (Path.GetFileName(Application.ExecutablePath).ToLower() == Repository.SealServerManager.ToLower() || Path.GetFileName(Application.ExecutablePath).ToLower() == Repository.SealReportDesigner.ToLower())
            {
                _installationDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            }
#endif
            XmlSerializer serializer = new XmlSerializer(typeof(SealServerConfiguration), xmlOverrides);
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.NewLineHandling = NewLineHandling.Entitize;
            using (XmlWriter xw = XmlWriter.Create(path, ws))
            {
                serializer.Serialize(xw, this);
            }
            FilePath = path;
            LastModification = File.GetLastWriteTime(path);
        }

        public class FileReplacePattern
        {
            public override string ToString()
            {
                return FileName + " " + OldValue;
            }

            [Category("Pattern Definition"),DisplayName("\tFile Name"), Description("The name of the attached file.")]
            public string FileName { get; set; } = "filename.css";
            [Category("Pattern Definition"),DisplayName("\tOld Value"), Description("The pattern to replace.")]
            public string OldValue { get; set; }
            [Category("Pattern Definition"),DisplayName("New Value"), Description("The new value.")]
            public string NewValue { get; set; }
        }
    }
}
