//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Xml;
using System.ComponentModel.Design;
using Seal.Helpers;
using RazorEngine.Templating;

namespace Seal.Model
{
    /// <summary>
    /// Main configuration of the Seal Server
    /// </summary>
    public class SealServerConfiguration : RootComponent
    {
        /// <summary>
        /// Current file path
        /// </summary>
        [XmlIgnore]
        public string FilePath;

        /// <summary>
        /// Current repository
        /// </summary>
        [XmlIgnore]
        public Repository Repository;


        /// <summary>
        /// True if the configuration is used for Web Site publication on IIS
        /// </summary>
        [XmlIgnore]
        public bool ForPublication = false;

        /// <summary>
        /// The logo file name used by the report templates
        /// </summary>
        public string LogoName { get; set; } = "logo.png";

        /// <summary>
        /// True if a logo is defined and exists
        /// </summary>
        [XmlIgnore]
        public bool HasLogo
        {
            get
            {
                if (string.IsNullOrEmpty(LogoName)) return false;
                return File.Exists(Path.Combine(Repository.ViewImagesFolder, LogoName));
            }
        }

        /// <summary>
        /// Number of days of log files to keep in the repository 'Logs' subfolder. If 0, the log feature is disabled.
        /// </summary>
        public int LogDays { get; set; } = 30;

        /// <summary>
        /// The name of the product displayed on the Web site
        /// </summary>
        public string WebProductName { get; set; } = "Seal Report";

        /// <summary>
        /// List of cultures available in the user profile of the Web Report Server. If nothing is selected, the translation cultures installed in the repository are proposed by default.
        /// </summary>
        public List<string> WebCultures { get; set; } = new List<string>();
        public bool ShouldSerializeWebCultures() { return WebCultures.Count > 0; }

        /// <summary>
        /// If true, the programs will not access to Internet for external resources. All JavaScript's will be loaded locally (no use of CDN path).
        /// </summary>
        public bool IsLocal { get; set; } = true;

        /// <summary>
        /// List of strings to replace when the report result is generated in a single HTML file (case of View Report Result or Output generation). This allow to specify the new font location in a CSS.
        /// </summary>
        public List<FileReplacePattern> FileReplacePatterns { get; set; } = new List<FileReplacePattern>();
        public bool ShouldSerializeFileReplacePatterns() { return FileReplacePatterns.Count > 0; }

        /// <summary>
        /// Additional CSS files to be included in the HTML report result. One per line or separated by semi-column.
        /// </summary>
        public string CssFiles { get; set; } = null;

        /// <summary>
        /// Additional JavaScript files to be included in the HTML report result. One per line or separated by semi-column.
        /// </summary>
        public string ScriptFiles { get; set; } = null;

        bool _useSealScheduler = false;
        /// <summary>
        /// If true, the Web Report Server Scheduler is used instead of the Windows Task Scheduler. The schedules are stored in the 'SpecialFolders\\Schedules' repository folder (one file per schedule). This allows schedules for .NETCore or Azure installations.
        /// </summary>
        public bool UseSealScheduler
        {
            get
            {
                return _useSealScheduler;
            }
            set
            {
                _useSealScheduler = value;
            }
        }

        /// <summary>
        /// Name of the Task Scheduler folder containg the schedules of the reports if the Windows Task Scheduler is used
        /// </summary>
        public string TaskFolderName { get; set; } = Repository.SealRootProductName + " Report";

       bool _auditEnabled = false;
        /// <summary>
        /// If true, the Audit script is executed for the follwing events: login, logout, report execution and management, folder management, file management, dashboard management.
        /// </summary>
        public bool AuditEnabled
        {
            get
            {
                return _auditEnabled;
            }
            set
            {
                _auditEnabled = value;
            }
        }

        /// <summary>
        /// If set, the script is executed to log events. The default implementation is to insert a record into a database table.
        /// </summary>
        public string AuditScript { get; set; } = null;

        /// <summary>
        /// If set, the script is executed when a report is initialized for an execution. Default values for report execution can be set here.
        /// </summary>
        public string InitScript { get; set; } = null;

        /// <summary>
        /// If set, the script is executed when a new report is created. Default values for report creation can be set here.
        /// </summary>
        public string ReportCreationScript { get; set; } = null;

        /// <summary>
        /// List of scripts added to all scripts executed during a report execution (including tasks). This may be useful to defined common functions for the reports. To include the script, an @Include("common script name") directive must be inserted at the beginning of the script.
        /// </summary>
        public List<CommonScript> CommonScripts { get; set; } = new List<CommonScript>();
        public bool ShouldSerializeCommonScripts() { return CommonScripts.Count > 0; }

        /// <summary>
        /// Replace the @Include by the common script in the current script
        /// </summary>
        public string SetConfigurationCommonScripts(string script)
        {
            var result = script;
            bool checkResult = true;
            while (checkResult)
            {
                checkResult = false;
                foreach (var cs in CommonScripts)
                {
                    var pattern = string.Format("@Include(\"{0}\")", cs.Name);
                    var index = result.IndexOf(pattern);
                    if (index >= 0)
                    {
                        checkResult = true;
                        result = result.Replace(pattern, "");
                        result = result + "\r\n" + cs.Script;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a common script key form a given name and model
        /// </summary>      
        public string GetConfigurationCommonScriptKey(string name, object model)
        {
            var script = CommonScripts.FirstOrDefault(i => i.Name == name);

            if (script == null) throw new Exception(string.Format("Unable to find a configuration common script  named '{0}'...", name));

            string key = string.Format("CFGCS:{0}_{1}_{2}_{3}", FilePath, GUID, name, File.GetLastWriteTime(FilePath).ToString("s"));
            RazorHelper.Compile(script.Script, model.GetType(), key);

            return key;
        }


        /// <summary>
        /// If true, the client library is used to perform the HTML to PDF conversion (mainly useful for .NETCore distribution). This requires the installation of the HTML to PDF Server on a Windows machine or on Azur Services.
        /// </summary>
        public bool PdfUseClient { get; set; } = false;

        /// <summary>
        /// If the client library is used, the HTML to PDF server IP or name.
        /// </summary>
        public string PdfServer { get; set; } = "127.0.0.1";

        /// <summary>
        /// If the client library is used, the HTML to PDF server IP or name.
        /// </summary>
        public uint PdfServerPort { get; set; } = 45001;

        /// <summary>
        /// If the client library is used, optional HTML to PDF converter service password.
        /// </summary>
        public string PdfServicePassword { get; set; } = "";

        /// <summary>
        /// If true, the client library will call the Web service instead of the TCP service to perform the HTML to PDF conversion.
        /// </summary>
        public bool PdfUseWebService { get; set; } = false;

        /// <summary>
        /// If the client library is used, the HTML to PDF web service URL.
        /// </summary>
        public string PdfWebServiceURL { get; set; } = "";

        /// <summary>
        /// Current default configuration values for Pdf converter
        /// </summary>
        public List<string> PdfConfigurations { get; set; } = new List<string>();
        public bool ShouldSerializePdfConfigurations() { return PdfConfigurations.Count > 0; }

        private SealPdfConverter _pdfConverter = null;
        /// <summary>
        /// Editor Helper: All the default options applied to the PDF conversion from the HTML result.
        /// </summary>
        [XmlIgnore]
        public SealPdfConverter PdfConverter
        {
            get
            {
                if (_pdfConverter == null)
                {
                    _pdfConverter = SealPdfConverter.Create();
                    _pdfConverter.SetConfigurations(PdfConfigurations, null);
                    
                }
                return _pdfConverter;
            }
            set { _pdfConverter = value; }
        }

        /// <summary>
        /// True if the Pdf configurations were edited
        /// </summary>
        public bool PdfConverterEdited
        {
            get { return _pdfConverter != null; }
        }

        /// <summary>
        /// Current default configuration values for Excel converter
        /// </summary>
        public List<string> ExcelConfigurations { get; set; } = new List<string>();
        public bool ShouldSerializeExcelConfigurations() { return ExcelConfigurations.Count > 0; }

        private SealExcelConverter _excelConverter = null;
        /// <summary>
        /// Editor Helper: All the default options applied to the Excel conversion from the view
        /// </summary>
        [XmlIgnore]
        public SealExcelConverter ExcelConverter
        {
            get
            {
                if (_excelConverter == null)
                {
                    _excelConverter = SealExcelConverter.Create();
                    _excelConverter.SetConfigurations(ExcelConfigurations, null);
                    
                }
                return _excelConverter;
            }
            set { _excelConverter = value; }
        }

        /// <summary>
        /// True if the Excel configurations were edited
        /// </summary>
        public bool ExcelConverterEdited
        {
            get { return _excelConverter != null; }
        }

        /// <summary>
        /// Editor Helper: Reset PDF configuration values to their default values
        /// </summary>
        public string HelperResetPDFConfigurations
        {
            get { return "<Click to reset the PDF configuration values to their default values>"; }
        }

        /// <summary>
        /// Editor Helper: Reset Excel configuration values to their default values
        /// </summary>
        public string HelperResetExcelConfigurations
        {
            get { return "<Click to reset the Excel configuration values to their default values>"; }
        }

        /// <summary>
        /// Current default configuration values for Dashboard Pdf converter
        /// </summary>
        public List<string> DashboardPdfConfigurations { get; set; } = new List<string>();
        public bool ShouldSerializeDashboardPdfConfigurations() { return DashboardPdfConfigurations.Count > 0; }

        private SealPdfConverter _dashboardPdfConverter = null;
        /// <summary>
        /// Editor Helper: All the default options applied to the PDF conversion from the HTML result.
        /// </summary>
        [XmlIgnore]
        public SealPdfConverter DashboardPdfConverter
        {
            get
            {
                if (_dashboardPdfConverter == null)
                {
                    _dashboardPdfConverter = SealPdfConverter.Create();
                    _dashboardPdfConverter.SetConfigurations(DashboardPdfConfigurations, null);
                    
                }
                return _dashboardPdfConverter;
            }
            set { _dashboardPdfConverter = value; }
        }

        /// <summary>
        /// True if the Pdf configurations were edited
        /// </summary>
        public bool DashboardPdfConverterEdited
        {
            get { return _dashboardPdfConverter != null; }
        }

        /// <summary>
        /// Current default configuration values for Excel converter
        /// </summary>
        public List<string> DashboardExcelConfigurations { get; set; } = new List<string>();
        public bool ShouldSerializeDashboardExcelConfigurations() { return DashboardExcelConfigurations.Count > 0; }

        private SealExcelConverter _dashboardExcelConverter = null;
        /// <summary>
        /// Editor Helper: All the default options applied to the Excel conversion from the view
        /// </summary>
        [XmlIgnore]
        public SealExcelConverter DashboardExcelConverter
        {
            get
            {
                if (_dashboardExcelConverter == null)
                {
                    _dashboardExcelConverter = SealExcelConverter.Create();
                    _dashboardExcelConverter.ForDashboard = true;
                    _dashboardExcelConverter.SetConfigurations(DashboardExcelConfigurations, null);
                    
                }
                return _dashboardExcelConverter;
            }
            set { _dashboardExcelConverter = value; }
        }

        /// <summary>
        /// True if the Excel configurations were edited
        /// </summary>
        public bool DashboardExcelConverterEdited
        {
            get { return _dashboardExcelConverter != null; }
        }

        /// <summary>
        /// Editor Helper: Reset PDF configuration values to their default values
        /// </summary>
        public string HelperResetDashboardPDFConfigurations
        {
            get { return "<Click to reset the Dashboard PDF configuration values to their default values>"; }
        }

        /// <summary>
        /// Editor Helper: Reset Excel configuration values to their default values
        /// </summary>
        public string HelperResetDashboardExcelConfigurations
        {
            get { return "<Click to reset the Dashboard Excel configuration values to their default values>"; }
        }

        /// <summary>
        /// All common scripts
        /// </summary>
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

        /// <summary>
        /// Returns all common scripts not being edited
        /// </summary>
        public string GetCommonScriptsHeader(CommonScript scriptBeingEdited)
        {
            var result = "";
            foreach (var script in CommonScripts.Where(i => i != scriptBeingEdited)) result += script.Script + "\r\n";
            return result;
        }

        /// <summary>
        /// The name of the culture used when a report is created. If not specified, the current culture of the server is used.
        /// </summary>
        public string DefaultCulture { get; set; } = "";

        /// <summary>
        /// The numeric format used for numeric column having the default format
        /// </summary>
        public string NumericFormat { get; set; } = "N0";

        /// <summary>
        /// The date time format used for date time column having the default format
        /// </summary>
        public string DateTimeFormat { get; set; } = "d";

        /// <summary>
        /// If not specified in the report, separator used for the CSV template
        /// </summary>
        public string CsvSeparator { get; set; } = "";

        /// <summary>
        /// If true, the Web site is published with the NET Core distribution.
        /// </summary>
        public bool WebNETCore { get; set; } = false;

        /// <summary>
        /// The name of the IIS Web application. Use '/' to publish on 'Default Web Site'
        /// </summary>
        public string WebApplicationName { get; set; } = "/Seal";

        /// <summary>
        /// The name of the IIS Application pool used by the web application
        /// </summary>
        public string WebApplicationPoolName { get; set; } = Repository.SealRootProductName + " Application Pool";

        /// <summary>
        /// The directory were the web site files are published
        /// </summary>
        public string WebPublicationDirectory { get; set; } = "";

        //Set by the server manager...
        string _installationDirectory = "";
        /// <summary>
        /// Installation directory
        /// </summary>
        public string InstallationDirectory
        {
            get { return _installationDirectory; }
            set { _installationDirectory = value; }
        }

        /// <summary>
        /// Last modifcation date time
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification;

        /// <summary>
        /// Load configuration from a file
        /// </summary>
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

        /// <summary>
        /// Save to current file
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(FilePath);
        }

        /// <summary>
        /// Save to a given file path
        /// </summary>
        /// <param name="path"></param>
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
            if (DashboardPdfConverterEdited)
            {
                DashboardPdfConfigurations = DashboardPdfConverter.GetConfigurations();
            }
            if (ExcelConverterEdited)
            {
                ExcelConfigurations = ExcelConverter.GetConfigurations();
            }
            if (DashboardExcelConverterEdited)
            {
                DashboardExcelConfigurations = DashboardExcelConverter.GetConfigurations();
            }
#if !DEBUG && !NETCOREAPP
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

        /// <summary>
        /// Read the file content and replace the configuration keywords
        /// </summary>
        public string GetAttachedFileContent(string fileName)
        {
            string result = File.ReadAllText(fileName);
            foreach (var item in FileReplacePatterns.Where(i => i.FileName == Path.GetFileName(fileName)))
            {
                result = result.Replace(item.OldValue, item.NewValue);
            }
            return result;
        }

        /// <summary>
        /// Defines a pattern to replace in a file 
        /// </summary>
        public class FileReplacePattern
        {
            public override string ToString()
            {
                return FileName + " " + OldValue;
            }

            public string FileName { get; set; } = "filename.css";
            public string OldValue { get; set; }
            public string NewValue { get; set; }
        }
    }
}

