//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using Seal.Helpers;
using System.Reflection;
using System.Globalization;
using Twilio.Rest.Trunking.V1;
using DocumentFormat.OpenXml.Vml;
#if WINDOWS
using System.Drawing.Design;
using DynamicTypeDescriptor;
using Seal.Forms;
using System.Windows.Forms.Design;
using System.ComponentModel.Design;
#endif

namespace Seal.Model
{
    /// <summary>
    /// Main configuration of the Seal Server
    /// </summary>
    public class SealServerConfiguration : RootComponent
    {
        public const string ApplicationKeysKeyName = "Application Keys";
        public const string ApplicationKeysKeyValue = "1*çéàèüwien42feäöü!???**";

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

#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("SchedulerMode").SetIsBrowsable(!ForPublication);
                GetProperty("TaskFolderName").SetIsBrowsable(!ForPublication);
                GetProperty("TaskFolderName").SetIsReadOnly(_schedulerMode != SchedulerMode.Windows);
                GetProperty("OuterProcess").SetIsBrowsable(!ForPublication);
                GetProperty("OuterProcess").SetIsReadOnly(_schedulerMode == SchedulerMode.Windows);

                GetProperty("DefaultCulture").SetIsBrowsable(!ForPublication);
                GetProperty("NumberGroupSeparator").SetIsBrowsable(!ForPublication);
                GetProperty("NumberDecimalSeparator").SetIsBrowsable(!ForPublication);
                GetProperty("DateSeparator").SetIsBrowsable(!ForPublication);
                GetProperty("TimeSeparator").SetIsBrowsable(!ForPublication);
                GetProperty("LogoName").SetIsBrowsable(!ForPublication);
                GetProperty("WebProductName").SetIsBrowsable(!ForPublication);
                GetProperty("WebHelpLink").SetIsBrowsable(!ForPublication);
                GetProperty("WebCultures").SetIsBrowsable(!ForPublication);
                GetProperty("LogDays").SetIsBrowsable(!ForPublication);
                GetProperty("CsvSeparator").SetIsBrowsable(!ForPublication);
                GetProperty("NumericFormat").SetIsBrowsable(!ForPublication);
                GetProperty("DateTimeFormat").SetIsBrowsable(!ForPublication);
                GetProperty("AuditEnabled").SetIsBrowsable(!ForPublication);
                GetProperty("AuditScript").SetIsBrowsable(!ForPublication);
                GetProperty("AuditScript").SetIsReadOnly(!AuditEnabled);

                GetProperty("WebSessionInitScript").SetIsBrowsable(!ForPublication);
                GetProperty("InitScript").SetIsBrowsable(!ForPublication);
                GetProperty("ReportCreationScript").SetIsBrowsable(!ForPublication);
                GetProperty("RepositoryTranslationsScript").SetIsBrowsable(!ForPublication);
                GetProperty("IsLocal").SetIsBrowsable(!ForPublication);
                GetProperty("HostForPersonalFolder").SetIsBrowsable(!ForPublication);
                GetProperty("FileReplacePatterns").SetIsBrowsable(!ForPublication);
                GetProperty("CssFiles").SetIsBrowsable(!ForPublication);
                GetProperty("ScriptFiles").SetIsBrowsable(!ForPublication);
                GetProperty("WebCssFiles").SetIsBrowsable(!ForPublication);
                GetProperty("WebScriptFiles").SetIsBrowsable(!ForPublication);
                GetProperty("AlternateTempDirectory").SetIsBrowsable(!ForPublication);
                GetProperty("EnableRazorCache").SetIsBrowsable(!ForPublication);
                GetProperty("EnableDownloadUpload").SetIsBrowsable(!ForPublication);
                GetProperty("ReportFormats").SetIsBrowsable(!ForPublication);

                GetProperty("EncryptionMode").SetIsBrowsable(!ForPublication);
                GetProperty("KeyValues").SetIsBrowsable(!ForPublication && EncryptionMode == EncryptionMode.Default);
                GetProperty("ApplicationKeys").SetIsBrowsable(!ForPublication);

                GetProperty("WebApplicationPoolName").SetIsBrowsable(ForPublication);
                GetProperty("WebApplicationName").SetIsBrowsable(ForPublication);
                GetProperty("WebPublicationDirectory").SetIsBrowsable(ForPublication);

                GetProperty("DefaultCulture").SetDisplayName($"Culture (Default is '{CultureInfo.CurrentCulture.EnglishName}')");

                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

#endif
        /// <summary>
        /// True if the configuration is used for Web Site publication on IIS
        /// </summary>
        [XmlIgnore]
        public bool ForPublication = false;

        /// <summary>
        /// The logo file name used by the report templates
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Logo file name"), Description("The logo file name used by the report templates. The file must be located in the Repository folder '<Repository Path>\\Views\\Images' and in the \\Images sub-folder of the Web publication directory. If empty, the Web Product Name is used as prefix."), Id(1, 1)]
        [DefaultValue("logo.svg")]
#endif
        public string LogoName { get; set; } = "logo.svg";

        /// <summary>
        /// True if a logo is defined and exists
        /// </summary>
        [XmlIgnore]
        public bool HasLogo
        {
            get
            {
                if (string.IsNullOrEmpty(LogoName)) return false;
                return File.Exists(LogoFilePath);
            }
        }

        /// <summary>
        /// Logo file path from the repository
        /// </summary>
        [XmlIgnore]
        public string LogoFilePath
        {
            get
            {
                return System.IO.Path.Combine(Repository.ViewImagesFolder, LogoName);
            }
        }

        /// <summary>
        /// Number of days of log files to keep in the repository 'Logs' subfolder. If 0, the log feature is disabled.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Log days to keep"), Description("Number of days of log files to keep in the repository 'Logs' subfolder. If 0, the log feature is disabled."), Id(6, 1)]
        [DefaultValue(30)]
#endif
        public int LogDays { get; set; } = 30;

        /// <summary>
        /// The name of the product displayed on the Web site
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Web Product Name"), Description("The name of the product displayed on the Web site."), Id(7, 1)]
#endif
        public string WebProductName { get; set; } = "Seal Report";

        /// <summary>
        /// Optional Help link for the Web Report Server
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Web Help Link"), Description("Optional Help link for the Web Report Server."), Id(7, 1)]
#endif
        public string WebHelpLink { get; set; }

        /// <summary>
        /// List of cultures available in the user profile of the Web Report Server. If nothing is selected, the translation cultures installed in the repository are proposed by default.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Web Cultures"), Description("List of cultures available in the user profile of the Web Report Server. If nothing is selected, the translation cultures installed in the repository are proposed by default."), Id(8, 1)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
#endif
        public List<string> WebCultures { get; set; } = new List<string>();
        public bool ShouldSerializeWebCultures() { return WebCultures.Count > 0; }

        /// <summary>
        /// If true, the programs will not access to Internet for external resources. All JavaScript's will be loaded locally (no use of CDN path).
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Server is local (No internet)"), Description("If true, the programs will not access to Internet for external resources. All JavaScript's will be loaded locally (no use of CDN path)."), Id(9, 1)]
        [DefaultValue(true)]
#endif
        public bool IsLocal { get; set; } = true;

        /// <summary>
        /// If true, the User Personal Folder (located in the 'SpecialFolders\\Personal' repository sub-folder) containing the profile and personal files is built with the host name. This allows multiple Web sites on the same installation.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Use host name to define the Personal Folder name"), Description("If true, the User Personal Folder (located in the 'SpecialFolders\\Personal' repository sub-folder) containing the profile and personal files is built with the host name. This allows multiple Web sites on the same installation."), Id(10, 1)]
        [DefaultValue(false)]
#endif
        public bool HostForPersonalFolder { get; set; } = false;

        /// <summary>
        /// List of strings to replace when the report result is generated in a single HTML file (case of View Report Result or Output generation). This allow to specify the new font location in a CSS.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Patterns to replace in CSS or JScript"), Description("List of strings to replace when the report result is generated in a single HTML file (case of View Report Result or Output generation). This allow to specify the new font location in a CSS."), Id(11, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<FileReplacePattern> FileReplacePatterns { get; set; } = new List<FileReplacePattern>();
        public bool ShouldSerializeFileReplacePatterns() { return FileReplacePatterns.Count > 0; }

        /// <summary>
        /// Additional CSS files to be included in the HTML report result. One per line or separated by semi-column.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("CSS Files"), Description("Additional CSS files to be included in the HTML report result. One per line or separated by semi-column."), Id(12, 1)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
#endif
        public string CssFiles { get; set; } = "";

        /// <summary>
        /// Additional CSS files to be included in the Web Report Server application. One per line or separated by semi-column.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Web CSS Files"), Description("Additional CSS files to be included in the Web Report Server application. One per line or separated by semi-column."), Id(14, 1)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
#endif
        public string WebCssFiles { get; set; } = "";

        /// <summary>
        /// Additional JavaScript files to be included in the HTML report result. One per line or separated by semi-column.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("JavaScript Files"), Description("Additional Script files to be included in the HTML report result. One per line or separated by semi-column."), Id(15, 1)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
#endif
        public string ScriptFiles { get; set; } = null;

        /// <summary>
        /// Additional JavaScript files to be included in the Web Report Server application. One per line or separated by semi-column.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Web JavaScript Files"), Description("Additional JavaScript files to be included in the Web Report Server application. One per line or separated by semi-column."), Id(16, 1)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
#endif
        public string WebScriptFiles { get; set; } = null;

        /// <summary>
        /// If set, the directory is used instead of the standard Temp directory for compiling Razor Scripts and generating report results.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Alternate Temp Directory"), Description("If set, the directory is used instead of the standard Temp directory for compiling Razor Scripts and generating report results. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder (e.g. '%SEALREPOSITORY%\\Temp')."), Id(20, 1)]
#endif
        public string AlternateTempDirectory { get; set; } = null;

        /// <summary>
        /// If true, standard razor script used for templates (Views, Tables, Tasks and Security Providers) are compiled and stored in the 'Assemblies\\RazorCache' repository folder. This speed-up the application start-up.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Enable Razor Cache"), Description("If true, standard razor script used for templates (Views, Tables, Tasks and Security Providers) are compiled and stored in the 'Assemblies\\RazorCache' repository folder. This speed-up the application start-up."), Id(21, 1)]
        [DefaultValue(true)]
#endif
        public bool EnableRazorCache { get; set; } = true;
        public bool ShouldSerializeEnableRazorCache() { return !EnableRazorCache; }

        /// <summary>
        /// If true, download and upload of files and reports are allowed through the Web Report Server if the user belongs to a group having this right.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Enable Download and Upload"), Description("If true, download and upload of files and reports are allowed through the Web Report Server if the user belongs to a group having this right."), Id(22, 1)]
        [DefaultValue(true)]
#endif
        public bool EnableDownloadUpload { get; set; } = true;
        public bool ShouldSerializeEnableDownloadUpload() { return !EnableDownloadUpload; }


        /// <summary>
        /// List of report format allowed in view result. If empty, all formats are taken.
        /// </summary>
#if WINDOWS
        [Category("Server Settings"), DisplayName("Result Report Formats"), Description("List of report format allowed in view result. If empty, all formats are taken."), Id(23, 1)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
#endif
        public List<string> ReportFormats { get; set; } = new List<string>();
        public bool ShouldSerializeReportFormats() { return ReportFormats.Count > 0; }


        SchedulerMode _schedulerMode = SchedulerMode.Windows;
        /// <summary>
        /// How the Report Scheduler is started on the server. Windows Task Scheduler (Windows only), Windows Service (Windows only, requires the service installation) or Worker (All platforms), Web Server (All platforms, requires to keep the Web Server up).
        /// </summary>
#if WINDOWS
        [Category("Report Scheduler Settings"), DisplayName("Report Scheduler Mode"), Description("How the Report Scheduler is started on the server. Windows Task Scheduler (Windows only), Windows Service (Windows only, requires the service installation) or Worker (All platforms), Web Server (All platforms, requires to keep the Web Server up)."), Id(2, 2)]
        [DefaultValue(SchedulerMode.Windows)]
        [TypeConverter(typeof(NamedEnumConverter))]
#endif
        public SchedulerMode SchedulerMode
        {
            get
            {
                return _schedulerMode;
            }
            set
            {
                _schedulerMode = value;
#if WINDOWS
                UpdateEditor();
#endif
            }
        }

        /// <summary>
        /// Name of the Task Scheduler folder containing the schedules of the reports if the Windows Task Scheduler is used
        /// </summary>
#if WINDOWS
        [Category("Report Scheduler Settings"), DisplayName("Task Folder Name"), Description("Name of the Task Scheduler folder containing the schedules of the reports if the Windows Task Scheduler is used. Warning: Changing this name will affect all existing schedules !"), Id(3, 2)]
#endif
        public string TaskFolderName { get; set; } = Repository.SealRootProductName + " Report";

        /// <summary>
        /// If true and the scheduler in executed in Service, Worker or Web Server, schedules is executed in an outer process forked by the initiator process. If false, the schedule is executed in a dedicated thread of the initiator.
        /// </summary>
#if WINDOWS
        [Category("Report Scheduler Settings"), DisplayName("Execute in Outer Process"), Description("If true and the scheduler in executed in Service, Worker or Web Server, schedules are executed in an outer process (SealTaskScheduler) forked by the initiator process. If false, the schedule is executed in a dedicated thread of the initiator."), Id(4, 2)]
        [DefaultValue(true)]
#endif
        public bool OuterProcess { get; set; } = true;



        bool _auditEnabled = false;
        /// <summary>
        /// If true, the Audit script is executed for the following events: login, logout, report execution and management, folder management, file management.
        /// </summary>
#if WINDOWS
        [Category("Audit Settings"), DisplayName("Audit Enabled"), Description("If true, the Audit script is executed for the following events: login, logout, report execution and management, folder management, file management."), Id(2, 3)]
        [DefaultValue(false)]
#endif
        public bool AuditEnabled
        {
            get
            {
                return _auditEnabled;
            }
            set
            {
                _auditEnabled = value;
#if WINDOWS
                UpdateEditor();
#endif
            }
        }

        /// <summary>
        /// If set, the script is executed to log events. The default implementation is to insert a record into a database table.
        /// </summary>
#if WINDOWS
        [Category("Audit Settings"), DisplayName("Audit Script"), Description("If set, the script is executed to log events. The default implementation is to insert a record into a database table."), Id(3, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string AuditScript { get; set; } = null;

        /// <summary>
        /// If set, the script is executed when a report is initialized for an execution. Default values for report execution can be set here.
        /// </summary>
#if WINDOWS
        [Category("Scripts"), DisplayName("Web Server Session Init Script"), Description("If set, the script is executed when a Web Server Session is started."), Id(3, 4)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string WebSessionInitScript { get; set; } = null;

        /// <summary>
        /// If set, the script is executed when a report is initialized for an execution. Default values for report execution can be set here.
        /// </summary>
#if WINDOWS
        [Category("Scripts"), DisplayName("Report Execution Init Script"), Description("If set, the script is executed when a report is initialized for an execution. Default values for report execution can be set here."), Id(4, 4)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string InitScript { get; set; } = null;

        /// <summary>
        /// If set, the script is executed when a new report is created. Default values for report creation can be set here.
        /// </summary>
#if WINDOWS
        [Category("Scripts"), DisplayName("Report Creation Script"), Description("If set, the script is executed when a new report is created. Default values for report creation can be set here."), Id(5, 4)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ReportCreationScript { get; set; } = null;

        /// <summary>
        /// If set, the script is executed when the repository translations are loaded. This allows to load dynamically translations from a database or any source.
        /// </summary>
#if WINDOWS
        [Category("Scripts"), DisplayName("\tRepository Translations Script"), Description("If set, the script is executed when the repository translations are loaded. This allows to load dynamically translations from a database or any source."), Id(8, 4)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string RepositoryTranslationsScript { get; set; } = null;

        EncryptionMode _encryptionMode = EncryptionMode.Default;
        /// <summary>
        /// Storage and encryption mode of encryption keys used for Passwords, SendGrid Keys and Application Keys/Passwords. If Machine RSA Container is chosen, only the users of the machine will have access to the keys. If User RSA Container is chosen, only the Windows User will have access to the keys. Warning: You must retype the used password after changing mode or key values.
        /// </summary>
#if WINDOWS
        [DisplayName("Storage and encryption mode"), Description("Storage and encryption mode of encryption keys used for Passwords, SendGrid Keys and Application Keys/Passwords. If Machine RSA Container is chosen, only the users of the machine will have access to the keys. If User RSA Container is chosen, only the Windows user will have access to the keys to decrypt the password. Warning: You must retype the used password after changing mode or key values."), Category("Server Keys"), Id(1, 6)]
        [DefaultValue(EncryptionMode.Default)]
        [TypeConverter(typeof(NamedEnumConverter))]
#endif
        public EncryptionMode EncryptionMode
        {
            get
            {
                return _encryptionMode;
            }
            set
            {
                _encryptionMode = value;
                UpdateEditorAttributes();
            }
        }

        List<KeyValue> _keyValues = new List<KeyValue>();
        /// <summary>
        /// If default mode is chosen, the key values used by the server for different operations (e.g. for encrypting/decrypting connection password, SMTP or FTP passwords).
        /// </summary>
#if WINDOWS
        [DisplayName("Key values"), Description("If default mode is chosen, the key values used by the server for different operations (e.g. for encrypting/decrypting Connection password, SMTP or FTP passwords)."), Category("Server Keys"), Id(2, 6)]
        [DefaultValue(false)]
#endif
        public List<KeyValue> KeyValues
        {
            get
            {
                return _keyValues;
            }
            set { _keyValues = value; }
        }

 #if WINDOWS
        [DisplayName("Application keys and passwords"), Description("Keys or passwords used in the application (e.g. keys used in scripts in reports and tasks using the Repository.GetApplicationKey(\"KeyName\") method). Values are encrypted using the encryption mode defined above."), Category("Server Keys"), Id(3, 6)]
#endif       
        /// <summary>
        /// Keys or passwords used in the application (report and tasks). Values are encrypted using the encryption mode.
        /// </summary>
        public List<KeyValue> ApplicationKeys { get; set; } = new List<KeyValue>();

        /// <summary>
        /// The name of the culture used for the user session. It defines the language and the number and date formats. If not specified, the current culture of the server is used.
        /// </summary>
#if WINDOWS
        [Category("Formats"), DisplayName("Culture"), Description("The name of the culture used for the user session. It defines the language and the number and date formats. If not specified, the current culture of the server is used."), Id(2, 3)]
        [TypeConverter(typeof(Forms.CultureInfoConverter))]
#endif
        public string DefaultCulture { get; set; } = "";

        /// <summary>
        /// The numeric format used for numeric column having the default format
        /// </summary>
#if WINDOWS
        [Category("Formats"), DisplayName("Numeric Format"), Description("The numeric format used for numeric column having the default format."), Id(3, 3)]
        [TypeConverter(typeof(CustomFormatConverter))]
        [DefaultValue("N0")]
#endif
        public string NumericFormat { get; set; } = "N0";

        /// <summary>
        /// The date time format used for date time column having the default format
        /// </summary>
#if WINDOWS
        [Category("Formats"), DisplayName("Date Time Format"), Description("The date time format used for date time column having the default format."), Id(4, 3)]
        [TypeConverter(typeof(CustomFormatConverter))]
        [DefaultValue("d")]
#endif
        public string DateTimeFormat { get; set; } = "d";

        /// <summary>
        /// If set, overrides the Group Separator for numbers of the current culture
        /// </summary>
#if WINDOWS
        [Category("Formats"), DisplayName("Number Group Separator"), Description("If set, overrides the Group Separator for numbers of the current culture."), Id(5, 3)]
#endif
        public string NumberGroupSeparator { get; set; } = null;

        /// <summary>
        /// If set, overrides the Decimal Separator for numbers of the current culture
        /// </summary>
#if WINDOWS
        [Category("Formats"), DisplayName("Number Decimal Separator"), Description("If set, overrides the Decimal Separator for numbers of the current culture."), Id(6, 3)]
#endif
        public string NumberDecimalSeparator { get; set; } = null;

        /// <summary>
        /// If set, overrides the Date Separator for dates of the current culture
        /// </summary>
#if WINDOWS
        [Category("Formats"), DisplayName("Date Separator"), Description("If set, overrides the Date Separator for dates of the current culture."), Id(7, 3)]
#endif
        public string DateSeparator { get; set; } = null;

        /// <summary>
        /// If set, overrides the Time Separator for times of the current culture
        /// </summary>
#if WINDOWS
        [Category("Formats"), DisplayName("Time Separator"), Description("If set, overrides the Time Separator for times of the current culture."), Id(8, 3)]
#endif
        public string TimeSeparator { get; set; } = null;

        /// <summary>
        /// If not specified in the report, separator used for the CSV template
        /// </summary>
#if WINDOWS
        [Category("Formats"), DisplayName("CSV Separator"), Description("If not specified in the report, separator used for the CSV template. If empty, the separator of the user culture is used."), Id(9, 3)]
#endif
        public string CsvSeparator { get; set; } = "";

        /// <summary>
        /// The name of the IIS Web application. Use '/' to publish on 'Default Web Site'
        /// </summary>
#if WINDOWS
        [Category("Web Server IIS Publication"), DisplayName("Application Name"), Description("The name of the IIS Web application. Use '/' to publish on 'Default Web Site'."), Id(2, 3)]
#endif
        public string WebApplicationName { get; set; } = "/Seal";

        /// <summary>
        /// The name of the IIS Application pool used by the web application
        /// </summary>
#if WINDOWS
        [Category("Web Server IIS Publication"), DisplayName("Application Pool Name"), Description("The name of the IIS Application pool used by the web application."), Id(3, 3)]
#endif
        public string WebApplicationPoolName { get; set; } = Repository.SealRootProductName + " Application Pool";

        /// <summary>
        /// The directory were the web site files are published
        /// </summary>
#if WINDOWS
        [Category("Web Server IIS Publication"), DisplayName("Publication Directory"), Description("The directory were the web site files are published."), Id(4, 3)]
        [EditorAttribute(typeof(FolderNameEditor), typeof(UITypeEditor))]
#endif
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
        /// Last modification date time
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
                    xr.Close();
                }

                result.InitDefaultKeyValues();
                result.InitApplicationKeys();
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

            var originalApplicationsKey = (List<KeyValue>) Helper.Clone(ApplicationKeys);
            try
            {
                foreach (var k in ApplicationKeys)
                {
                    try
                    {
                        k.Value = EncryptValue(k.Value, ApplicationKeysKeyName);
                    }
                    catch (Exception ex)
                    {
                        Helper.WriteEventLogEntry("SealServerConfiguration", ex);
                    }
                }

                var xmlOverrides = new XmlAttributeOverrides();
                XmlAttributes attrs = new XmlAttributes();
                attrs.XmlIgnore = true;
                xmlOverrides.Add(typeof(RootComponent), "Name", attrs);
                xmlOverrides.Add(typeof(RootComponent), "GUID", attrs);

                Helper.Serialize(path, this, new XmlSerializer(typeof(SealServerConfiguration), xmlOverrides));
                FilePath = path;
                LastModification = File.GetLastWriteTime(path);
            }
            finally
            {
                ApplicationKeys = originalApplicationsKey;
            }
        }


        /// <summary>
        /// True if the executable is using Windows libraries (ReportDesigner and ServerManager)
        /// </summary>
        public bool IsUsingSealLibraryWin
        {
            get
            {
                var exe = Assembly.GetExecutingAssembly().Location;
                return System.IO.Path.GetFileName(exe).ToLower() == "seallibrarywin.dll";
            }
        }

        /// <summary>
        /// Read the file content and replace the configuration keywords
        /// </summary>
        public string GetAttachedFileContent(string fileName)
        {
            string result = File.ReadAllText(fileName);
            foreach (var item in FileReplacePatterns.Where(i => i.FileName == System.IO.Path.GetFileName(fileName)))
            {
                result = result.Replace(item.OldValue, item.NewValue);
            }
            return result;
        }

        /// <summary>
        /// Init and decode application keys
        /// </summary>
        public void InitApplicationKeys()
        {
            foreach (var key in ApplicationKeys)
            {
                try
                {
                    key.Value = DecryptValue(key.Value, ApplicationKeysKeyName);
                }
                catch (Exception ex)
                {
                    Helper.WriteEventLogEntry("SealServerConfiguration", ex);
                }
            }
        }

        /// <summary>
        /// Init default encryption keys
        /// </summary>
        public void InitDefaultKeyValues()
        {
            lock (_keyValues)
            {
                if (!_keyValues.Exists(i => i.Name == MetaConnection.PasswordKeyName))
                {
                    _keyValues.Add(new KeyValue() { Name = MetaConnection.PasswordKeyName, Value = MetaConnection.PasswordKeyValue });
                }
                if (!_keyValues.Exists(i => i.Name == OutputEmailDevice.PasswordKeyName))
                {
                    _keyValues.Add(new KeyValue() { Name = OutputEmailDevice.PasswordKeyName, Value = OutputEmailDevice.PasswordKeyValue });
                }
                if (!_keyValues.Exists(i => i.Name == OutputEmailDevice.SendGridKeyName))
                {
                    _keyValues.Add(new KeyValue() { Name = OutputEmailDevice.SendGridKeyName, Value = OutputEmailDevice.SendGridKeyValue });
                }
                if (!_keyValues.Exists(i => i.Name == OutputEmailDevice.AzureSecretKeyName))
                {
                    _keyValues.Add(new KeyValue() { Name = OutputEmailDevice.AzureSecretKeyName, Value = OutputEmailDevice.AzureSecretKeyValue });
                }
                if (!_keyValues.Exists(i => i.Name == OutputFileServerDevice.PasswordKeyName))
                {
                    _keyValues.Add(new KeyValue() { Name = OutputFileServerDevice.PasswordKeyName, Value = OutputFileServerDevice.PasswordKeyValue });
                }
                if (!_keyValues.Exists(i => i.Name == ApplicationKeysKeyName))
                {
                    _keyValues.Add(new KeyValue() { Name = ApplicationKeysKeyName, Value = ApplicationKeysKeyValue });
                }
            }
        }

        /// <summary>
        /// Decrypt a value using the key name and the encryption mode
        /// </summary>
        public string DecryptValue(string value, string keyName, bool useAES = false)
        {
            if (string.IsNullOrEmpty(value)) return "";

            var key = KeyValues.FirstOrDefault(i => i.Name == keyName);
            var result = "";

            if (EncryptionMode == EncryptionMode.Default)
            {
                result = useAES ? CryptoHelper.DecryptAES(value, key.Value) : CryptoHelper.DecryptTripleDES(value, key.Value);
            }
            else if (EncryptionMode == EncryptionMode.MachineRSAContainer)
            {
                result = CryptoHelper.DecryptWithRSAContainer(value, keyName, true);
            }
            else if (EncryptionMode == EncryptionMode.UserRSAContainer)
            {
                result = CryptoHelper.DecryptWithRSAContainer(value, keyName, false);
            }
            return result;
        }

        /// <summary>
        /// Encrypt a value using the key name and the encryption mode
        /// </summary>
        public string EncryptValue(string value, string keyName, bool useAES = false)
        {
            if (string.IsNullOrEmpty(value)) return "";

            var key = KeyValues.FirstOrDefault(i => i.Name == keyName);
            var result = "";

            if (key != null)
            {
                if (EncryptionMode == EncryptionMode.Default)
                {
                    result = useAES ? CryptoHelper.EncryptAES(value, key.Value) : CryptoHelper.EncryptTripleDES(value, key.Value);
                }
                else if (EncryptionMode == EncryptionMode.MachineRSAContainer)
                {
                    result = CryptoHelper.EncryptWithRSAContainer(value, keyName, true);
                }
                else if (EncryptionMode == EncryptionMode.UserRSAContainer)
                {
                    result = CryptoHelper.EncryptWithRSAContainer(value, keyName, false);
                }
            }

            return result;
        }

        /// <summary>
        /// Get an application key or password
        /// </summary>
        public string GetApplicationKey(string keyName)
        {
            var key = ApplicationKeys.FirstOrDefault(i => i.Name == keyName);
            var result = "";
            if (key != null) result = key.Value;           
            return result;
        }

        /// <summary>
        /// Retruns the build date of the assembly
        /// </summary>
        public static DateTime GetBuildDate()
        {
            var assembly = Assembly.GetAssembly(typeof(SealServerConfiguration));
            var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
            return attribute?.DateTime ?? default(DateTime);
        }


        /// <summary>
        /// Key name and values used by the application and stored at server level
        /// </summary>
        public class KeyValue
        {
            public override string ToString()
            {
                return Name;
            }

#if WINDOWS
            [Category("Definition"), DisplayName("Key name"), Description("The name of the key.")]
#endif
            public string Name { get; set; }
#if WINDOWS
            [Category("Definition"), DisplayName("Key value"), Description("The value of the key.")]
#endif
            public string Value { get; set; }
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

#if WINDOWS
            [Category("Pattern Definition"), DisplayName("\tFile Name"), Description("The name of the attached file.")]
#endif
            public string FileName { get; set; } = "filename.css";
#if WINDOWS
            [Category("Pattern Definition"), DisplayName("\tOld Value"), Description("The pattern to replace.")]
#endif
            public string OldValue { get; set; }
#if WINDOWS
            [Category("Pattern Definition"), DisplayName("New Value"), Description("The new value.")]
#endif
            public string NewValue { get; set; }
        }
    }
}

