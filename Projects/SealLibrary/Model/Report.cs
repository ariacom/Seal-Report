//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.IO;
using Seal.Helpers;
using System.Web;
using System.Globalization;
using Microsoft.Win32.TaskScheduler;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Xml;
using RazorEngine.Templating;
using System.ComponentModel;
using Seal.Forms;
using System.Drawing.Design;
using DynamicTypeDescriptor;

namespace Seal.Model
{
    /// <summary>
    /// Main Seal Report Objects: Repository (Sources, MetaData), Reports (Models, Views)   
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Interface dedicated to log execution messages
    /// </summary>
    public interface ReportExecutionLog
    {
        /// <summary>
        /// Log a message displayed in the messages panel of the report result
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        void LogMessage(string message, params object[] args);
    }

    /// <summary>
    /// The main Report class to store a report definition, plus extra properties for execution 
    /// </summary>
    public class Report : RootEditor, ReportExecutionLog, ITreeSort
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("DisplayName").SetIsBrowsable(true);
                GetProperty("ViewGUID").SetIsBrowsable(true);
                GetProperty("InputValues").SetIsBrowsable(true);
                GetProperty("WidgetCache").SetIsBrowsable(true);

                GetProperty("CommonScripts").SetIsBrowsable(true);
                GetProperty("InitScript").SetIsBrowsable(true);
                GetProperty("NavigationScript").SetIsBrowsable(true);
                //GetProperty("CommonScripts").SetDisplayName("Common Scripts: " + (Report.CommonScripts.Count == 0 ? "None" : Report.CommonScripts.Count.ToString() + " Items(s)"));
                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

        public int GetSort() { return 0; }

        /// <summary>
        /// Unique identifier of the report
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// The report name displayed in the result. If empty, the report file name is used. The display name may contain a Razor script  if it starts with '@'.
        /// </summary>
        [Category("Definition"), DisplayName("Display name"), Description("The report name displayed in the result. If empty, the report file name is used. The display name may contain a Razor script  if it starts with '@'."), Id(1, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string DisplayName { get; set; } = "";
        public bool ShouldSerializeDisplayName() { return !string.IsNullOrEmpty(DisplayName); }

        private string _displayNameEx = null;
        /// <summary>
        /// The final display name of the report. It may include script execution defined in DisplayName
        /// </summary>
        [XmlIgnore]
        public string DisplayNameEx
        {
            get
            {
                if (!string.IsNullOrEmpty(_displayNameEx)) return _displayNameEx;
                if (!string.IsNullOrEmpty(DisplayName))
                {
                    try
                    {
                        _displayNameEx = RazorHelper.CompileExecute(DisplayName, this).Trim();
                    }
                    catch { }
                }
                else
                {
                    _displayNameEx = Path.GetFileNameWithoutExtension(FilePath);
                }
                return _displayNameEx;
            }
        }

        [DefaultValue(null)]
        [Category("Definition"), DisplayName("Current view"), Description("The current view used to execute the report."), Id(2, 1)]
        [TypeConverter(typeof(ReportViewConverter))]
        public string ViewGUID { get; set; }

        /// <summary>
        /// GUID of the view to being executed
        /// </summary>
        public string CurrentViewGUID;

        /// <summary>
        /// Definition of additional report input values (actually a restriction used as value only that may be prompted). Input values can then be used in the task scripts or any scripts used to generate the report.
        /// </summary>
        [Category("Definition"), DisplayName("Report Input Values"), Description("Definition of additional report input values (actually a restriction used as value only that may be prompted). Input values can then be used in the task scripts or any scripts used to generate the report."), Id(3, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<ReportRestriction> InputValues { get; set; } = new List<ReportRestriction>();
        public bool ShouldSerializeInputValues() { return InputValues.Count > 0; }

        /// <summary>
        /// For dashboards, the duration in seconds the report execution is kept by the Web Report Server to render the widgets defined in the report.
        /// </summary>
        [Category("Definition"), DisplayName("Widgets cache duration"), Description("For dashboards, the duration in seconds the report execution is kept by the Web Report Server to render the widgets defined in the report."), Id(5, 1)]
        [DefaultValue(60)]
        public int WidgetCache { get; set; } = 60;
        public bool ShouldSerializeWidgetCache() { return WidgetCache != 60; }

        /// <summary>
        /// List of data sources of the report (either from repository or defined in the report itself)
        /// </summary>
        public List<ReportSource> Sources { get; set; } = new List<ReportSource>();

        public bool ShouldSerializeSources() { return Sources.Count > 0; }

        /// <summary>
        /// List of models of the report
        /// </summary>
        public List<ReportModel> Models { get; set; } = new List<ReportModel>();


        public bool ShouldSerializeModels() { return Models.Count > 0; }

        /// <summary>
        /// List of outputs of the report
        /// </summary>
        public List<ReportOutput> Outputs { get; set; } = new List<ReportOutput>();
        public bool ShouldSerializeOutputs() { return Outputs.Count > 0; }

        /// <summary>
        /// List of tasks of the report
        /// </summary>
        public List<ReportTask> Tasks { get; set; } = new List<ReportTask>();
        public bool ShouldSerializeTasks()
        {
            return Tasks.Count > 0;
        }

        /// <summary>
        /// List of scripts added to all scripts executed for the report (including tasks). This may be useful to defined common functions for the report.
        /// </summary>
        [Category("Scripts"), DisplayName("Common Scripts"), Description("List of scripts added to all scripts executed for the report (including tasks). This may be useful to defined common functions for the report."), Id(1, 2)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<CommonScript> CommonScripts { get; set; } = new List<CommonScript>();
        public bool ShouldSerializeCommonScripts() { return CommonScripts.Count > 0; }

        /// <summary>
        /// The header to include in razor scripts executed for this report
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
        /// The header to include in razor scripts executed for this report, except the one being edited
        /// </summary>
        public string GetCommonScriptsHeader(CommonScript scriptBeingEdited)
        {
            var result = "";
            foreach (var script in CommonScripts.Where(i => i != scriptBeingEdited)) result += script.Script + "\r\n";
            return result;
        }

        /// <summary>
        /// Returns a common script key from a given name and model
        /// </summary>      
        public string GetReportCommonScriptKey(string name, object model)
        {
            var script = CommonScripts.FirstOrDefault(i => i.Name == name);
            if (script == null) throw new Exception(string.Format("Unable to find a report common script  named '{0}'...", name));

            string key = string.Format("REPCS:{0}_{1}_{2}_{3}", FilePath, GUID, name, File.GetLastWriteTime(FilePath).ToString("s"));
            try
            {
                RazorHelper.Compile(script.Script, model.GetType(), key);
            }
            catch (Exception ex)
            {
                var message = (ex is TemplateCompilationException ? Helper.GetExceptionMessage((TemplateCompilationException)ex) : ex.Message);
                ExecutionErrors += string.Format("Execution error when compiling the common script '{0}':\r\n{1}\r\n", name, message);
                if (ex.InnerException != null) ExecutionErrors += "\r\n" + ex.InnerException.Message;
                throw ex;
            }
            return key;
        }

        /// <summary>
        /// Returns an input value (Report Restriction) from a given name
        /// </summary>
        public ReportRestriction GetInputValueByName(string name)
        {
            return InputValues.FirstOrDefault(i => i.DisplayNameEl.ToLower() == name.ToLower());
        }

        /// <summary>
        /// The current list of input values restrictions of the report at execution time
        /// </summary>
        [XmlIgnore]
        public List<ReportRestriction> ExecutionInputValues
        {
            get
            {
                List<ReportRestriction> result = new List<ReportRestriction>();
                foreach (ReportRestriction restriction in InputValues)
                {
                    ReportRestriction newRestriction = restriction;
                    if (ForOutput && OutputToExecute.UseCustomRestrictions)
                    {
                        newRestriction = OutputToExecute.Restrictions.FirstOrDefault(i => i.GUID == restriction.GUID);
                        if (newRestriction == null) newRestriction = restriction;
                    }
                    result.Add(newRestriction);
                }
                return result;
            }
        }

        /// <summary>
        /// List of views of the report
        /// </summary>
        public List<ReportView> Views { get; set; } = new List<ReportView>();

        /// <summary>
        /// A Razor script executed when the report is initialized for the execution. The script can be used to modify the report definition (e.g. set default values in restrictions). 
        /// </summary>
        [Category("Scripts"), DisplayName("Report Execution Init script"), Description("A Razor script executed when the report is initialized for the execution. The script can be used to modify the report definition (e.g. set default values in restrictions)."), Id(2, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string InitScript { get; set; } = "";
        public bool ShouldSerializeInitScript() { return !string.IsNullOrEmpty(InitScript); }

        /// <summary>
        /// Optional Razor Script executed if script navigation links have been added in the CellScript
        /// </summary>
        [Category("Scripts"), DisplayName("Report Navigation Script"), Description("Optional Razor Script executed if script navigation links have been added to the report (e.g. in a dedicated task)."), Id(3, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
        public string NavigationScript { get; set; }
        public bool ShouldSerializeNavigationScript() { return !string.IsNullOrEmpty(NavigationScript); }


        /// <summary>
        /// Get the hyperlink string to execute the report navigation script.
        /// </summary>
        public string GetReportNavigationScriptLink(string text = "", string linkTag = "")
        {
            var guid = Guid.NewGuid().ToString();
            var link = new NavigationLink() { Type = NavigationType.ReportScript, Href = guid, Text = text, Report = this, Tag = linkTag };
            NavigationLinks.Add(guid, link);
            return link.FullHref;
        }

        /// <summary>
        /// Get the hyperlink string to execute the report navigation script to download a file.
        /// </summary>
        public string GetReportNavigationFileDownloadLink(string text = "", string linkTag = "")
        {
            var guid = Guid.NewGuid().ToString();
            var link = new NavigationLink() { Type = NavigationType.FileDownload, Href = guid, Text = text, Report = this, Tag = linkTag };
            NavigationLinks.Add(guid, link);
            return link.FullHref;
        }

        /// <summary>
        /// List of schedules of the report
        /// </summary>
        public List<ReportSchedule> Schedules { get; set; } = new List<ReportSchedule>();
        public bool ShouldSerializeSchedules() { return Schedules.Count > 0; }

        /// <summary>
        /// Current repository of the report
        /// </summary>
        [XmlIgnore]
        public Repository Repository = null;

        /// <summary>
        /// Current file path of the report
        /// </summary>
        [XmlIgnore]
        public string FilePath = "";

        /// <summary>
        /// Last modification date of the report file 
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification;

        /// <summary>
        /// Path of the result file after a report execution 
        /// </summary>
        [XmlIgnore]
        public string ResultFilePath;

        /// <summary>
        /// File path displayed to the user
        /// </summary>
        [XmlIgnore]
        public string DisplayResultFilePath
        {
            get
            {
                string result = ResultFilePath;
                if (ResultFilePath.StartsWith(Repository.ReportsFolder)) result = ResultFilePath.Substring(Repository.ReportsFolder.Length);
                else if (ResultFilePath.StartsWith(Repository.PersonalFolder))
                {
                    if (SecurityContext != null && ResultFilePath.StartsWith(Repository.GetPersonalFolder(SecurityContext)))
                    {
                        result = Repository.GetPersonalFolderName(SecurityContext) + ResultFilePath.Substring(Repository.GetPersonalFolder(SecurityContext).Length);
                    }
                    else result = ResultFilePath.Substring(Repository.PersonalFolder.Length);
                }
                return result;
            }
        }

        string _HTMLDisplayFilePath = "";
        /// <summary>
        /// Path of the HTML intermediate result file
        /// </summary>
        [XmlIgnore]
        public string HTMLDisplayFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_HTMLDisplayFilePath)) _HTMLDisplayFilePath = FileHelper.GetUniqueFileName(Path.Combine(GenerationFolder, "result.html"));
                return _HTMLDisplayFilePath;
            }
            set { _HTMLDisplayFilePath = value; }
        }

        /// <summary>
        /// Path of the folder when executed to an output device
        /// </summary>
        [XmlIgnore]
        public string OutputFolderDeviceResultFolder
        {
            get
            {
                string result = FileHelper.TempApplicationDirectory;
                if (ForOutput)
                {
                    if (OutputToExecute.Device is OutputFolderDevice)
                    {
                        if (!string.IsNullOrEmpty(OutputToExecute.FolderPath))
                        {
                            result = Repository.ReplaceRepositoryKeyword(OutputToExecute.FolderPath);
#if NETCOREAPP
                if (Path.DirectorySeparatorChar == '/' && result.Contains("\\")) result = result.Replace("\\", "/");
                else if (Path.DirectorySeparatorChar == '\\' && result.Contains("/")) result = result.Replace("/", "\\");
#endif 
                            if (!Directory.Exists(result)) Directory.CreateDirectory(result);
                        }
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// URL when executed from the Web Report Server 
        /// </summary>
        [XmlIgnore]
        public string WebUrl = "";

        /// <summary>
        /// Current identifier of the report's execution
        /// </summary>
        [XmlIgnore]
        public string ExecutionGUID = Guid.NewGuid().ToString();

        /// <summary>
        /// Current folder use for the file generation during execution
        /// </summary>
        [XmlIgnore]
        public string GenerationFolder
        {
            get
            {
                return FileHelper.TempApplicationDirectory;
            }
        }

        /// <summary>
        /// Current file name of the result file
        /// </summary>
        [XmlIgnore]
        public string ResultFileName
        {
            get
            {
                string fileName = DisplayNameEx.ToString();
                if (ForOutput)
                {
                    if (OutputToExecute.Device is OutputFolderDevice || OutputToExecute.Device is OutputFileServerDevice)
                    {
                        fileName = OutputToExecute.FileName.Replace(Repository.SealReportDisplayNameKeyword, FileHelper.CleanFilePath(DisplayNameEx));
                        try
                        {
                            fileName = string.Format(fileName, DateTime.Now);
                        }
                        catch { }
                    }
                }
                if (string.IsNullOrEmpty(fileName)) fileName = "result";
                fileName = Helper.CleanFileName(fileName) + ".html";
                if (!ForOutput) fileName = fileName.Replace(" ", "_");
                return fileName;
            }
        }

        /// <summary>
        /// Procedure executed before any execution: set default parameters values and executes init scripts.
        /// </summary>
        public void InitForExecution()
        {
            string fileName = "", fileFolder = "";

            //Init all execution view parameters...
            foreach (var view in Views) view.InitParameters(false);

            //Copy values from reference views
            foreach (var view in AllViews.Where(i => i.ReferenceView != null))
            {
                view.InitFromReferenceView();
            }

            foreach (var model in Models)
            {
                model.ExecResultTableLoaded = false;
                model.ExecResultPagesBuilt = false;
            }

            if (ForOutput)
            {
                //Check custom Output Parameters 
                OutputToExecute.CopyParameters(OutputToExecute.View.Parameters);

                //Add the security context for the output if specified
                if (!string.IsNullOrWhiteSpace(OutputToExecute.UserName) || !string.IsNullOrWhiteSpace(OutputToExecute.UserGroups))
                {
                    SecurityContext = new SecurityUser(Repository.Security) { Name = OutputToExecute.UserName };
                    string[] groups = OutputToExecute.UserGroups.Replace(";", "\r").Replace("\n", "").Split('\r');
                    foreach (string group in groups) SecurityContext.AddSecurityGroup(group);
                }

                if (!string.IsNullOrEmpty(OutputToExecute.UserCulture))
                {
                    SecurityContext.SetDefaultCulture(OutputToExecute.UserCulture);
                }
            }

            try
            {
                fileName = ResultFileName;
                fileFolder = FileHelper.TempApplicationDirectory;

                if (ForOutput && OutputToExecute.Device is OutputFolderDevice && !OutputToExecute.ZipResult)
                {
                    //no need to get unique file name
                    if (Format != ReportFormat.pdf && Format != ReportFormat.excel)
                    {
                        fileFolder = OutputFolderDeviceResultFolder;
                    }
                    ResultFilePath = Path.Combine(fileFolder, Path.GetFileNameWithoutExtension(fileName)) + "." + ResultExtension;
                }
                else
                {
                    //get unique file name in the result folder
                    ResultFilePath = FileHelper.GetUniqueFileName(Path.Combine(fileFolder, fileName), "." + ResultExtension);
                }
                //Display path is always an HTML one...
                HTMLDisplayFilePath = FileHelper.GetUniqueFileName(Path.Combine(GenerationFolder, FileHelper.GetResultFilePrefix(ResultFilePath) + ".html"));

                //Clear some cache values...
                _displayNameEx = null;

            }
            catch (Exception ex)
            {
                Cancel = true;
                if (string.IsNullOrEmpty(fileFolder) && OutputToExecute != null && !string.IsNullOrEmpty(OutputToExecute.FolderPath)) fileFolder = OutputToExecute.FolderPath;
                ExecutionErrors += string.Format("Error initializing report Path, check your report execution or output Path '{0}'\r\n{1}\r\n", Path.Combine(fileFolder, fileName), ex.Message);
                ExecutionErrorStackTrace = ex.StackTrace;
            }

            //Init scripts

            //Load converter assembly
            if (ExecutionView != null)
            {
                var converter = ExecutionView.PdfConverter;
            }

            //First config
            if (!string.IsNullOrEmpty(Repository.Configuration.InitScript))
            {
                try
                {
                    RazorHelper.CompileExecute(Repository.Configuration.InitScript, this);
                }
                catch (Exception ex2)
                {
                    ExecutionErrors += string.Format("Error executing configuration init script:\r\n{0}\r\n", ex2.Message);
                    ExecutionErrorStackTrace = ex2.StackTrace;
                }
            }

            //Then source
            foreach (var source in Sources.Where(i => !string.IsNullOrEmpty(i.InitScript)))
            {
                if (Models.Exists(i => i.SourceGUID == source.GUID))
                {
                    try
                    {
                        RazorHelper.CompileExecute(source.InitScript, source);
                    }
                    catch (Exception ex2)
                    {
                        ExecutionErrors += string.Format("Error executing source init script for '{0}'\r\n{1}\r\n", source.Name, ex2.Message);
                        ExecutionErrorStackTrace = ex2.StackTrace;
                    }
                }
            }

            //Finally report
            if (!string.IsNullOrEmpty(InitScript))
            {
                try
                {
                    RazorHelper.CompileExecute(InitScript, this);
                }
                catch (Exception ex2)
                {
                    ExecutionErrors += string.Format("Error executing report init script:\r\n{0}\r\n", ex2.Message);
                    ExecutionErrorStackTrace = ex2.StackTrace;
                }
            }

            //Init enum values
            foreach (var restriction in AllRestrictions.Where(i => i.IsEnumRE))
            {
                restriction.SetEnumHtmlIds();
            }

            //First selection for enum values
            foreach (var restriction in AllRestrictions.Where(i => i.IsEnumRE && i.FirstSelection != FirstEnumSelection.None))
            {
                restriction.EnumValues.Clear();
                if (restriction.FirstSelection == FirstEnumSelection.All)
                {
                    restriction.EnumValues.AddRange(from v in restriction.EnumRE.Values select v.Id);
                }
                else if (restriction.FirstSelection == FirstEnumSelection.First && restriction.EnumRE.Values.Count > 0)
                {
                    restriction.EnumValues.Add(restriction.EnumRE.Values.First().Id);
                }
                if (restriction.FirstSelection == FirstEnumSelection.Last && restriction.EnumRE.Values.Count > 0)
                {
                    restriction.EnumValues.Add(restriction.EnumRE.Values.Last().Id);
                }
                restriction.FirstSelection = FirstEnumSelection.None;
            }
        }

        /// <summary>
        /// After execution, indicates if the report has validation errors
        /// </summary>
        [XmlIgnore]
        public bool HasValidationErrors = false;

        /// <summary>
        ///Execution messages after execution
        /// </summary>
        [XmlIgnore]
        public string ExecutionMessages;

        /// <summary>
        ///Execution errors after execution
        /// </summary>
        [XmlIgnore]
        public string ExecutionErrors;

        /// <summary>
        ///Execution errors stack trace after execution
        /// </summary>
        [XmlIgnore]
        public string ExecutionErrorStackTrace;

        /// <summary>
        ///Execution errors after execution used by the Web Report Server
        /// </summary>
        [XmlIgnore]
        public string WebExecutionErrors
        {
            get
            {
                return !string.IsNullOrEmpty(WebUrl) && !HasValidationErrors && !string.IsNullOrEmpty(ExecutionErrors) ? Translate("This report has execution errors. Please check details in the Repository Logs Files or in the Event Viewer...") : ExecutionErrors;
            }
        }

        /// <summary>
        /// Indicates if the execution messages are shown in the report
        /// </summary>
        [XmlIgnore]
        public bool ShowExecutionMessages
        {
            get
            {
                return ((ExecutionView.GetValue("messages_mode") == "enabledshown")
                    || (ExecutionView.GetValue("messages_mode") == "enabledshownexec" && (Status == ReportStatus.NotExecuted || Status == ReportStatus.Executing))
                    || (ExecutionView.GetValue("messages_mode") == "enabled" && !string.IsNullOrEmpty(WebExecutionErrors))
                    );
            }
        }

        /// <summary>
        /// Error messages got during the load of the report
        /// </summary>
        [XmlIgnore]
        public string LoadErrors = "";

        /// <summary>
        /// Warning messages in case of product upgrade
        /// </summary>
        [XmlIgnore]
        public string UpgradeWarnings = "";

        /// <summary>
        /// Error messages got during the parsing of the templates
        /// </summary>
        [XmlIgnore]
        public string TemplateParsingErrors;

        /// <summary>
        /// Execution status of the report
        /// </summary>
        [XmlIgnore]
        public ReportStatus Status = ReportStatus.NotExecuted;

        /// <summary>
        /// Name of the report during its execution
        /// </summary>
        [XmlIgnore]
        public string ExecutionName
        {
            get { return TranslateDisplayName((string.IsNullOrEmpty(DisplayNameEx) ? ExecutionView.Name : DisplayNameEx)) + (OutputToExecute != null && Status != ReportStatus.RenderingResult ? string.Format(" - {0}", TranslateOutputName(OutputToExecute.Name)) : ""); }
        }

        /// <summary>
        /// List of tasks to be executed (actually tasks enabled)
        /// </summary>
        [XmlIgnore]
        public List<ReportTask> ExecutionTasks
        {
            get
            {
                return Tasks.Where(i => i.Enabled).ToList();
            }
        }

        /// <summary>
        /// List of model to process during the report execution. By default, only models involved in displayed views are executed, unless they have the ForceModelsLoad flag set to true.
        /// </summary>
        [XmlIgnore]
        public List<ReportModel> ExecutionModels
        {
            get
            {
                List<ReportModel> result = new List<ReportModel>();
                if (ExecutionView.GetBoolValue(Parameter.ForceModelsLoad)) result = Models.ToList();
                else GetModelsToExecute(ExecutionView, result);
                return result;
            }
        }

        /// <summary>
        /// List of all views to parse during the report execution.
        /// </summary>
        [XmlIgnore]
        public List<ReportView> ExecutionViews
        {
            get
            {
                List<ReportView> result = new List<ReportView>() { ExecutionView };
                fillFullViewList(ExecutionView.Views, result);
                return result;
            }
        }

        /// <summary>
        /// Execution progression in percentage
        /// </summary>
        [XmlIgnore]
        public int ExecutionProgression
        {
            get
            {
                int overall = ExecutionTasks.Count + ExecutionModels.Count;
                int progression = 0;
                foreach (var task in ExecutionTasks) progression += task.Progression;
                foreach (var model in ExecutionModels) progression += model.Progression;
                return overall == 0 ? 100 : progression / overall;
            }
        }

        /// <summary>
        /// Execution progression message
        /// </summary>
        [XmlIgnore]
        public string ExecutionProgressionMessage
        {
            get
            {
                TimeSpan duration = DateTime.Now - ExecutionStartDate;
                StringBuilder message = new StringBuilder("");
                if (duration.Hours > 0) message.AppendFormat("{0:00}:", Convert.ToInt32(duration.Hours));
                message.Append(string.Format("{0:00}:{1:00} {2}", duration.Minutes, duration.Seconds, Cancel ? Translate("Cancelling report...") : Translate("Executing report...")));
                return message.ToString();
            }
        }

        /// <summary>
        /// Execution progression percentage for the models processing
        /// </summary>
        [XmlIgnore]
        public int ExecutionProgressionModels
        {
            get
            {
                return ExecutionModels.Count == 0 ? 100 : (100 * ExecutionModels.Count(i => i.Progression >= 100)) / ExecutionModels.Count;
            }
        }

        /// <summary>
        /// Execution progression percentage message for the models processing
        /// </summary>
        [XmlIgnore]
        public string ExecutionProgressionModelsMessage
        {
            get
            {
                return string.Format("{0}/{1} {2}", ExecutionModels.Count(i => i.Progression >= 100), ExecutionModels.Count, ExecutionModels.Count > 1 ? Translate("Models loaded...") : Translate("Model loaded..."));
            }
        }

        /// <summary>
        /// Execution progression percentage for the tasks processing
        /// </summary>
        [XmlIgnore]
        public int ExecutionProgressionTasks
        {
            get
            {
                return ExecutionTasks.Count == 0 ? 100 : (100 * ExecutionTasks.Count(i => i.Progression >= 100)) / ExecutionTasks.Count;
            }
        }

        /// <summary>
        /// Execution progression percentage for the tasks processing
        /// </summary>
        [XmlIgnore]
        public string ExecutionProgressionTasksMessage
        {
            get
            {
                return string.Format("{0}/{1} {2}", ExecutionTasks.Count(i => i.Progression >= 100), ExecutionTasks.Count, ExecutionTasks.Count > 1 ? Translate("Tasks executed...") : Translate("Task executed..."));
            }
        }

        /// <summary>
        /// True if the report is being tested for execution (from the Server Manager)
        /// </summary>
        [XmlIgnore]
        public bool CheckingExecution = false;

        /// <summary>
        /// True if the schedules have been modified
        /// </summary>
        [XmlIgnore]
        public bool SchedulesModified = false;

        /// <summary>
        /// True if the schedules have to be defined with the current user, otherwise SYSTEM is used
        /// </summary>
        [XmlIgnore]
        public bool SchedulesWithCurrentUser = false;

        /// <summary>
        /// True if the report is being executed
        /// </summary>
        [XmlIgnore]
        public bool IsExecuting
        {
            get { return (Status != ReportStatus.NotExecuted && Status != ReportStatus.Executed); }
        }

        /// <summary>
        /// True if the report has been cancelled
        /// </summary>
        [XmlIgnore]
        private bool _cancel = false;
        public bool Cancel { get => _cancel; set => _cancel = value; }

        /// <summary>
        /// True if the report has only to be rendered
        /// </summary>
        [XmlIgnore]
        public bool RenderOnly = false;

        /// <summary>
        /// Execution start date time
        /// </summary>
        [XmlIgnore]
        public DateTime ExecutionStartDate;

        /// <summary>
        /// Rendering date time
        /// </summary>
        [XmlIgnore]
        public DateTime ExecutionRenderingDate;

        /// <summary>
        /// Execution end date time
        /// </summary>
        [XmlIgnore]
        public DateTime ExecutionEndDate;

        /// <summary>
        /// Duration of the model execution
        /// </summary>
        [XmlIgnore]
        public TimeSpan ExecutionModelDuration
        {
            get { return (ExecutionRenderingDate - ExecutionStartDate); }
        }

        /// <summary>
        /// True is the report execution occured after an navigation (sub-report or drill)
        /// </summary>
        [XmlIgnore]
        public bool IsNavigating = false; //If false, do evaluate restrictions prompted...

        /// <summary>
        /// True if the report has navigation links in the result
        /// </summary>
        [XmlIgnore]
        public bool HasNavigation = false; //If true, navigation must be activated...

        //Output management
        /// <summary>
        /// Current report output to execute
        /// </summary>
        [XmlIgnore]
        public ReportOutput OutputToExecute = null;

        /// <summary>
        /// True if the execution is for a report output
        /// </summary>
        [XmlIgnore]
        public bool ForOutput
        {
            get { return OutputToExecute != null; }
        }

        /// <summary>
        /// Task set if only one task has to be executed
        /// </summary>
        [XmlIgnore]
        public ReportTask TaskToExecute = null;

        /// <summary>
        /// True if the html display result is being generated
        /// </summary>
        [XmlIgnore]
        public bool GenerateHTMLDisplay
        {
            get
            {
                return Status == ReportStatus.RenderingDisplay || Status == ReportStatus.NotExecuted;
            }
        }

        /// <summary>
        /// True if the report is not for an output and has no external viewer
        /// </summary>
        [XmlIgnore]
        public bool IsBasicHTMLWithNoOutput
        {
            get
            {
                return !ForOutput && !HasExternalViewer;
            }
        }

        /// <summary>
        /// Context of the execution:  DesignerReport, DesignerOutput, TaskScheduler, WebReport, WebOutput
        /// </summary>
        [XmlIgnore]
        public ReportExecutionContext ExecutionContext = ReportExecutionContext.DesignerReport;

        /// <summary>
        /// Current result format generated durin a View Result: html, print, csv, pdf, excel
        /// </summary>
        [XmlIgnore]
        public string ExecutionViewResultFormat = "";

        /// <summary>
        /// Current security user of the report execution
        /// </summary>
        [XmlIgnore]
        public SecurityUser SecurityContext = null;

        /// <summary>
        /// Root view being executed
        /// </summary>
        [XmlIgnore]
        public ReportView ExecutionView
        {
            get
            {
                ReportView result = Views.FirstOrDefault(i => i.GUID == CurrentViewGUID);
                if (result == null) result = Views.FirstOrDefault(i => i.GUID == ViewGUID);
                if (result == null)
                {
                    result = Views.FirstOrDefault();
                    ViewGUID = result.GUID;
                }
                return result;
            }
        }

        /// <summary>
        /// Full execution duration
        /// </summary>
        [XmlIgnore]
        public TimeSpan ExecutionFullDuration
        {
            get { return (ExecutionEndDate - ExecutionStartDate); }
        }

        /// <summary>
        /// Helper Dictionary to manage restrictions in the Web Report Server
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, string> PreInputRestrictions = new Dictionary<string, string>();

        /// <summary>
        /// Helper Dictionary to manage restrictions in the Web Report Server
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, string> InputRestrictions = new Dictionary<string, string>();

        /// <summary>
        /// Input restriction value for a given key
        /// </summary>
        public string GetInputRestriction(string key)
        {
            if (InputRestrictions.ContainsKey(key)) return InputRestrictions[key];
            return "";
        }

        /// <summary>
        /// True if the execution has errors
        /// </summary>
        [XmlIgnore]
        public bool HasErrors
        {
            get { return !string.IsNullOrEmpty(ExecutionErrors); }
        }

        TaskFolder _taskFolder = null;
        /// <summary>
        /// Task Folder used to store the schedules of the report
        /// </summary>
        public TaskFolder TaskFolder
        {
            get
            {
                if (_taskFolder == null)
                {
                    TaskService taskService = new TaskService();
                    _taskFolder = taskService.RootFolder.SubFolders.FirstOrDefault(i => i.Name == Repository.Configuration.TaskFolderName);
                    if (_taskFolder == null) _taskFolder = taskService.RootFolder.CreateFolder(Repository.Configuration.TaskFolderName);
                }
                return _taskFolder;
            }
        }

        /// <summary>
        /// True if the report has restrictions to prompt
        /// </summary>
        [XmlIgnore]
        public bool HasRestrictions
        {
            get { return ExecutionCommonRestrictions.Count > 0; }
        }

        /// <summary>
        /// True if the report has chart
        /// </summary>
        [XmlIgnore]
        public bool HasChart
        {
            get { return HasNVD3Chart || HasChartJSChart || HasPlotlyChart; }
        }

        /// <summary>
        /// True if the report has NVD3 chart
        /// </summary>
        [XmlIgnore]
        public bool HasNVD3Chart
        {
            get { return Models.Exists(i => i.HasNVD3Serie); }
        }

        /// <summary>
        /// True if the report has JS chart
        /// </summary>
        [XmlIgnore]
        public bool HasChartJSChart
        {
            get { return Models.Exists(i => i.HasChartJSSerie); }
        }

        /// <summary>
        /// True if the report has Plotly chart
        /// </summary>
        [XmlIgnore]
        public bool HasPlotlyChart
        {
            get { return Models.Exists(i => i.HasPlotlySerie); }
        }

        /// <summary>
        /// Encoding of the result file
        /// </summary>
        [XmlIgnore]
        public Encoding ResultFileEncoding
        {
            get
            {
                //Utf8 by default, except for CSV if specified
                return (Format == ReportFormat.csv && !ExecutionView.GetBoolValue(Parameter.CSVUtf8Parameter)) ? Encoding.Default : Encoding.UTF8;
            }
        }

        /// <summary>
        /// Init all references of the report: Sources, Models, Views, Taks, InputValues
        /// </summary>
        public void InitReferences()
        {
            //init report references in objects
            int i = Sources.Count;
            while (--i >= 0)
            {
                Sources[i].Report = this;
                if (Sources[i].MetaData == null)
                {
                    //metadata has gone...
                    LoadErrors += string.Format("No Metadata found, removing the source '{0}' (GUID {1})\r\n", Sources[i].Name, Sources[i].MetaSourceGUID);
                    Sources.RemoveAt(i);
                    continue;
                }
                Sources[i].InitReferences(Repository);
            }

            foreach (var model in Models)
            {
                model.Report = this;
                model.InitReferences();
            }


            foreach (var view in Views)
            {
                view.Report = this;
                view.InitReferences();
            }

            foreach (var task in Tasks)
            {
                task.Report = this;
                task.InitReferences();
            }

            foreach (var restriction in InputValues)
            {
                restriction.Report = this;
            }

            i = Outputs.Count;
            while (--i >= 0)
            {
                Outputs[i].Report = this;
                if (Outputs[i].Device == null)
                {
                    LoadErrors += string.Format("No Device found, removing the output '{0}' (GUID {1}). Check the device files in the repository folder.\r\n", Outputs[i].Name, Outputs[i].OutputDeviceGUID);
                    Outputs.RemoveAt(i);
                    continue;
                }
                Outputs[i].InitReferences();
            }

            i = Schedules.Count;
            while (--i >= 0)
            {
                Schedules[i].Report = this;
                if (!Schedules[i].IsTasksSchedule && Schedules[i].Output == null)
                {
                    LoadErrors += string.Format("No Output found, removing the schedule '{0}' (GUID {1})\r\n", Schedules[i].Name, Schedules[i].OutputGUID);
                    Schedules.RemoveAt(i);
                    continue;
                }
            }
        }

        /// <summary>
        /// Load a report from a file
        /// </summary>
        /// <returns>the report loaded and initialized</returns>
        static public Report LoadFromFile(string path, Repository repository, bool refreshEnums = true)
        {
            Report result = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Report));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (Report)serializer.Deserialize(xr);
                }
                result.FilePath = path;
                result.Repository = repository;
                result.LastModification = File.GetLastWriteTime(path);

                result.LoadErrors = "";
                foreach (ReportSource source in result.Sources)
                {
                    source.Report = result;
                    source.LoadRepositoryMetaSources(repository);
                }

                result.CheckLinkedTablesSources();

                if (result.Views.Count == 0)
                {
                    var view = result.AddRootView();
                    view.Name = "View";
                }
                result.InitReferences();

                //Refresh enums
                if (refreshEnums)
                {
                    foreach (ReportSource source in result.Sources) source.RefreshEnumsOnDbConnection();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to read the file '{0}'.\r\n{1}\r\n", path, ex.Message, ex.StackTrace));
            }
            return result;
        }

        /// <summary>
        /// Create an empty report
        /// </summary>
        /// <returns>the new report</returns>
        static public Report Create(Repository repository)
        {
            Report result = new Report() { GUID = Guid.NewGuid().ToString() };
            result.FilePath = repository.TranslateReport("New report") + "." + Repository.SealReportFileExtension;
            result.Repository = repository;
            foreach (MetaSource source in repository.Sources)
            {
                result.AddSource(source);
            }
            if (result.Sources.Count == 0) result.AddSource(null);
            foreach (ReportSource source in result.Sources)
            {
                source.LoadRepositoryMetaSources(repository);
                //Remove the connection added
                if (source.Connections.Count > 1) source.Connections.RemoveAll(i => i.IsEditable);
                //And master table added for NoSQL
                if (source.IsNoSQL && source.MetaData.Tables.Count > 1) source.MetaData.Tables.RemoveAll(i => i.IsEditable);
            }

            //and a model
            if (result.Models.Count == 0)
            {
                result.AddModel(false);
            }
            //Add default views
            ReportView view = result.AddModelHTMLView();
            if (view == null) throw new Exception(string.Format("Unable to find any view in your repository. Check that your repository folder '{0}' contains all the default sub-folders and files...", repository.RepositoryPath));
            result.ViewGUID = view.GUID;

            //Creation script
            if (!string.IsNullOrEmpty(repository.Configuration.ReportCreationScript))
            {
                try
                {
                    RazorHelper.CompileExecute(repository.Configuration.ReportCreationScript, result);
                }
                catch (Exception ex)
                {
                    result.ExecutionErrors = string.Format("Error executing configuration report creation script:\r\n{0}\r\n", ex.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// Save the current report to its file
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(FilePath);
        }

        /// <summary>
        /// Init view GUIDs and clear schedule before a report Copy
        /// </summary>
        public void InitGUIDAndSchedules()
        {
            GUID = Guid.NewGuid().ToString();

            var newValues = new Dictionary<string, string>();

            foreach (var view in AllViews)
            {
                var newGUID = Guid.NewGuid().ToString();
                newValues.Add(view.GUID, newGUID);
                //Set new GUIDs
                view.GUID = newGUID;
                if (!string.IsNullOrEmpty(view.WidgetDefinition.GUID)) view.WidgetDefinition.GUID = Guid.NewGuid().ToString();
            }

            //Reference views
            foreach (var view in AllViews.Where(i => !string.IsNullOrEmpty(i.ReferenceViewGUID)))
            {
                view.ReferenceViewGUID = newValues[view.ReferenceViewGUID];
                if (!string.IsNullOrEmpty(view.WidgetDefinition.ExecViewGUID)) view.WidgetDefinition.ExecViewGUID = newValues[view.WidgetDefinition.ExecViewGUID];
            }

            //Current view of the report
            ViewGUID = newValues[ViewGUID];
            CurrentViewGUID = ViewGUID;

            //Output views
            foreach (var output in Outputs)
            {
                output.ViewGUID = newValues[output.ViewGUID];
            }

            //No schedule
            Schedules.Clear();
        }

        /// <summary>
        /// Synchronize all report schedules defined in the report with the Windows Task Scheduler.
        /// </summary>
        public void SynchronizeTasks()
        {
            try
            {
                //Synchronize all schedules...
                foreach (ReportSchedule schedule in Schedules)
                {
                    try
                    {
                        schedule.SynchronizeTask();
                    }
                    catch { }
                }

                //Clear unused tasks
                if (Repository.UseWebScheduler)
                {
                    foreach (var schedule in SealReportScheduler.Instance.GetSchedules().Where(i => i.ReportGUID == GUID).ToList())
                    {
                        if (!Schedules.Exists(i => i.GUID == schedule.GUID))
                        {
                            SealReportScheduler.Instance.DeleteSchedule(schedule.GUID);
                        }
                    }
                }
                else
                {
                    foreach (Task task in TaskFolder.GetTasks().Where(i => i.Definition.RegistrationInfo.Source.StartsWith(FilePath + "\n")))
                    {
                        try
                        {
                            ReportSchedule schedule = Schedules.FirstOrDefault(i => i.TaskSource == task.Definition.RegistrationInfo.Source);
                            if (schedule == null)
                            {
                                TaskFolder.DeleteTask(task.Name);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Save report to a given file
        /// </summary>
        public void SaveToFile(string path)
        {
            //Check last modification
            if (LastModification != DateTime.MinValue && File.Exists(path))
            {
                DateTime lastDateTime = File.GetLastWriteTime(path);
                if (LastModification != lastDateTime)
                {
                    throw new Exception("Unable to save the report file. The file has been modified by another user.");
                }
            }
            try
            {
                Serialize(path);
            }
            finally
            {
                FilePath = path;
                LastModification = File.GetLastWriteTime(path);
            }
            //Clear and synchronize tasks
            if (SchedulesModified || Repository.UseWebScheduler) SynchronizeTasks();
            SchedulesModified = false;
        }

        /// <summary>
        /// Clone a report
        /// </summary>
        public Report Clone()
        {
            foreach (var view in Views)
            {
                view.SetAdvancedConfigurations();
            }
            Report report = (Report)Helper.Clone(this);
            report.Repository = Repository;
            report.InitReferences();
            report.FilePath = FilePath;
            report.LastModification = LastModification;
            return report;
        }

        private void Serialize(string path)
        {
            try
            {
                foreach (var model in Models)
                {
                    foreach (var table in model.LINQSubTables) table.BeforeSerialization();
                }

                foreach (var output in Outputs)
                {
                    output.BeforeSerialization();
                }

                foreach (var view in Views)
                {
                    view.SetAdvancedConfigurations();
                    view.BeforeSerialization();
                }
                //serialize only not readonly metadata
                foreach (ReportSource source in Sources)
                {
                    source.TempConnections = source.Connections.ToList();
                    source.TempTables = source.MetaData.Tables.ToList();
                    source.TempLinks = source.MetaData.TableLinks.ToList();
                    source.TempJoins = source.MetaData.Joins.ToList();
                    source.TempEnums = source.MetaData.Enums.ToList();
                    source.Connections.RemoveAll(i => !i.IsEditable);
                    source.MetaData.Tables.RemoveAll(i => !i.IsEditable);
                    source.MetaData.TableLinks.RemoveAll(i => !i.IsEditable);
                    source.MetaData.Joins.RemoveAll(i => !i.IsEditable);
                    source.MetaData.Enums.RemoveAll(i => !i.IsEditable);

                    foreach (var table in source.MetaData.Tables) table.BeforeSerialization();
                }

                XmlSerializer serializer = new XmlSerializer(typeof(Report));
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.NewLineHandling = NewLineHandling.Entitize;
                using (XmlWriter xw = XmlWriter.Create(path, ws))
                {
                    serializer.Serialize(xw, this);
                }
            }
            finally
            {
                foreach (ReportSource source in Sources)
                {

                    foreach (var table in source.MetaData.Tables) table.AfterSerialization();

                    source.Connections = source.TempConnections;
                    source.MetaData.Tables = source.TempTables;
                    source.MetaData.TableLinks = source.TempLinks;
                    source.MetaData.Joins = source.TempJoins;
                    source.MetaData.Enums = source.TempEnums;
                }
                foreach (var view in Views)
                {
                    view.AfterSerialization();
                }

                foreach (var output in Outputs)
                {
                    output.AfterSerialization();
                }

                foreach (var model in Models)
                {
                    foreach (var table in model.LINQSubTables) table.AfterSerialization();
                }
            }
        }

        /// <summary>
        /// Check report sources to have all sources referenced by linked tables 
        /// </summary>
        public void CheckLinkedTablesSources()
        {
            foreach (var source in Sources.Where(i => i.IsNoSQL).ToList())
            {
                //Add linked sources referenced if necessary
                foreach (var sourceGUID in (from s in source.MetaData.TableLinks select s.SourceGUID).Distinct())
                {
                    if (!Sources.Exists(j => j.GUID == sourceGUID || j.MetaSourceGUID == sourceGUID))
                    {
                        var newSource = AddSource(Repository.Sources.FirstOrDefault(i => i.GUID == sourceGUID));
                        newSource.LoadRepositoryMetaSources(Repository);
                        if (newSource.IsNoSQL) CheckLinkedTablesSources();
                    }
                }
            }
        }

        /// <summary>
        /// Add a default source to the report
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public ReportSource AddSource(MetaSource source)
        {
            ReportSource result = ReportSource.Create(Repository, (source == null));
            result.Report = this;
            if (source != null)
            {
                result.ConnectionGUID = ReportSource.DefaultRepositoryConnectionGUID;
                result.MetaSourceGUID = source.GUID;
                result.Name = source.Name + " (Repository)";
            }
            result.Name = Helper.GetUniqueName(result.Name, (from i in Sources select i.Name).ToList());
            Sources.Add(result);

            if (source != null && source.IsNoSQL) CheckLinkedTablesSources();
            return result;
        }

        /// <summary>
        /// Remove a report source from the report
        /// </summary>
        /// <param name="source"></param>
        public void RemoveSource(ReportSource source)
        {
            if (Sources.Count == 1) throw new Exception("The report must contain at least a Data Source");

            foreach (var model in Models.Where(i => i.SourceGUID == source.GUID))
            {
                if (model.Elements.Count > 0 || !string.IsNullOrEmpty(model.RestrictionText) || model.IsSQLModel) throw new Exception(string.Format("The source '{0}' is already used by a model.", source.Name));
                model.SourceGUID = Sources.First(i => i.GUID != source.GUID).GUID;
            }

            foreach (var reportSource in Sources.Where(i => i != source))
            {
                if (reportSource.MetaData.TableLinks.Exists(i => i.SourceGUID == source.GUID || i.SourceGUID == source.MetaSourceGUID)) throw new Exception(string.Format("The source '{0}' is referenced by a table link in '{1}'.", source.Name, reportSource.Name));
            }

            Sources.Remove(source);
        }

        /// <summary>
        /// Add a default model to the report (either SQL or Standard)
        /// </summary>
        public ReportModel AddModel(bool sqlModel)
        {
            if (Sources.Count == 0) throw new Exception("Unable to create a model: No source available.\r\nPlease create or add a source first.");
            ReportSource source = Sources.FirstOrDefault(i => i.IsDefault);
            if (source == null) source = Sources[0];
            if (sqlModel && !source.IsSQL) 
            {
                source = Sources.FirstOrDefault(i => i.IsSQL);
                if (source == null) throw new Exception("Unable to create a SQL model: No SQL source available.\r\nPlease create or add a SQL source first.");
            }

            ReportModel result = ReportModel.Create();
            result.Name = Helper.GetUniqueName("Model", (from i in Models select i.Name).ToList());
            if (sqlModel)
            {
                result.Table = MetaTable.Create();
                result.Table.DynamicColumns = true;
            }
            result.SourceGUID = source.GUID;
            result.Report = this;
            Models.Add(result);
            return result;
        }


        bool isModelUsedInViews(List<ReportView> views, ReportModel model)
        {
            if (views.Exists(i => i.ModelGUID == model.GUID)) return true;
            foreach (var childView in views)
            {
                if (isModelUsedInViews(childView.Views, model)) return true;
            }
            return false;
        }

        /// <summary>
        /// Remove a report model from the report
        /// </summary>
        /// <param name="model"></param>
        public void RemoveModel(ReportModel model)
        {
            if (isModelUsedInViews(Views, model)) throw new Exception(string.Format("The model '{0}' is already used by a view.", model.Name));
            if (Models.Count == 1) throw new Exception("Unable to remove the model: The report must contain at least one Model.");

            Models.Remove(model);
        }

        /// <summary>
        /// Add a default output to the report
        /// </summary>
        public ReportOutput AddOutput(OutputDevice device)
        {
            ReportOutput result = ReportOutput.Create();
            result.Name = Helper.GetUniqueName(string.Format("output ({0})", device.Name), (from i in Outputs select i.Name).ToList());
            if (device is OutputFolderDevice)
            {
                result.FolderPath = string.IsNullOrEmpty(FilePath) ? Repository.SealRepositoryKeyword + string.Format("{0}Reports{0}", Path.DirectorySeparatorChar) : Path.GetDirectoryName(FilePath).Replace(Repository.RepositoryPath, Repository.SealRepositoryKeyword);
                result.FileName = Repository.SealReportDisplayNameKeyword;
            }
            else if (device is OutputFileServerDevice)
            {
                result.FolderPath = "/";
                if (!string.IsNullOrEmpty(((OutputFileServerDevice)device).Directories)) result.FolderPath = ((OutputFileServerDevice)device).DirectoriesArray[0];
                result.FileName = Repository.SealReportDisplayNameKeyword;
            }

            result.Report = this;
            result.OutputDeviceGUID = device.GUID;
            result.ViewGUID = ViewGUID;
            result.InitReferences();
            Outputs.Add(result);
            return result;
        }

        /// <summary>
        /// Remove an output from the report
        /// </summary>
        public void RemoveOutput(ReportOutput output)
        {
            var schedules = Schedules.Where(i => i.OutputGUID == output.GUID).ToList();
            int j = schedules.Count;
            while (--j >= 0)
            {
                RemoveSchedule(schedules[j]);
            }
            Outputs.Remove(output);
        }

        /// <summary>
        /// Add a task to the report
        /// </summary>
        public ReportTask AddTask()
        {
            ReportTask result = ReportTask.Create();
            result.Name = Helper.GetUniqueName("Task", (from i in Tasks select i.Name).ToList());
            ReportSource source = Sources.FirstOrDefault(i => i.IsDefault);
            if (source == null) source = Sources[0];
            result.SourceGUID = source.GUID;
            result.Report = this;
            result.SortOrder = Tasks.Count > 0 ? Tasks.Max(i => i.SortOrder) + 1 : 1;
            Tasks.Add(result);
            return result;
        }

        /// <summary>
        /// Remove a task from the report
        /// </summary>
        /// <param name="task"></param>
        public void RemoveTask(ReportTask task)
        {
            Tasks.Remove(task);
        }

        /// <summary>
        /// Add a model view to the report
        /// </summary>
        /// <returns></returns>
        public ReportView AddModelHTMLView()
        {
            return AddView(ReportViewTemplate.ModelName);
        }

        /// <summary>
        /// Add a root report view to the report
        /// </summary>
        /// <returns></returns>
        public ReportView AddRootView()
        {
            ReportViewTemplate reportTemplate = RepositoryServer.GetViewTemplate(ReportViewTemplate.ReportName);
            ReportView view = null;
            if (reportTemplate != null)
            {
                view = ReportView.Create(reportTemplate);
                view.ModelGUID = "";
                view.Report = this;
                view.InitReferences();
                Views.Add(view);
            }
            return view;
        }

        /// <summary>
        /// Add a view with a template name
        /// </summary>
        public ReportView AddView(string templateName)
        {
            ReportView view = null;
            ReportViewTemplate modelTemplate = RepositoryServer.GetViewTemplate(templateName);
            if (modelTemplate != null)
            {
                view = AddRootView();
                view.SortOrder = Views.Count > 0 ? Views.Max(i => i.SortOrder) + 1 : 1;
                if (view != null)
                {
                    view.Name = Helper.GetUniqueName("View", (from i in Views select i.Name).ToList());
                    var child = AddChildView(view, modelTemplate);
                    if (child.TemplateName == ReportViewTemplate.ModelName) child.AddDefaultModelViews();
                }
            }
            return view;
        }

        /// <summary>
        /// From a parent view, add a child view with a template name
        /// </summary>
        public ReportView AddChildView(ReportView parent, string templateName)
        {
            return AddChildView(parent, RepositoryServer.GetViewTemplate(templateName));
        }

        /// <summary>
        /// From a parent view, add a child view with a template
        /// </summary>
        public ReportView AddChildView(ReportView parent, ReportViewTemplate template)
        {
            if (Models.Count == 0) throw new Exception("Unable to create a view: No model available.\r\nPlease create a model first.");

            ReportView result = ReportView.Create(template);
            result.Name = Helper.GetUniqueName(template.Name, (from i in parent.Views select i.Name).ToList());
            result.Report = this;
            result.InitReferences();
            result.SortOrder = parent.Views.Count > 0 ? parent.Views.Max(i => i.SortOrder) + 1 : 1;
            if (template.ForReportModel)
            {
                //take a model not used in views
                result.ModelGUID = Models[0].GUID;
                foreach (var model in Models)
                {
                    if (!isModelUsedInViews(Views, model))
                    {
                        result.ModelGUID = model.GUID;
                        break;
                    }
                }
            }
            parent.Views.Add(result);
            if (result.TemplateName == ReportViewTemplate.ModelName) result.AddDefaultModelViews();
            return result;
        }

        /// <summary>
        /// Remove a view from its parent 
        /// </summary>
        public void RemoveView(ReportView parent, ReportView view)
        {
            foreach (var refView in AllViews.Where(i => !string.IsNullOrEmpty(i.ReferenceViewGUID)))
            {
                var v1 = FindView(view.Views, refView.GUID);
                if (v1 == null)
                {
                    //This view has a reference and is not part of the children of the deleted view
                    var v2 = FindView(view.Views, refView.ReferenceViewGUID);
                    if (v2 != null)
                    {
                        throw new Exception(string.Format("Unable to remove the view '{0}': This view or one of its children named '{2}' is referenced by the view '{1}'.", view.Name, refView.Name, v2.Name));
                    }
                }
            }

            if (parent == null)
            {
                //Delete a root view
                foreach (ReportOutput output in Outputs)
                {
                    if (output.ViewGUID == view.GUID) throw new Exception(string.Format("Unable to remove the view '{0}': This view is used by the output '{1}'.", view.Name, output.Name));
                }

                foreach (var refView in AllViews)
                {
                    if (refView.WidgetDefinition.IsPublished && refView.WidgetDefinition.ExecViewGUID == view.GUID) throw new Exception(string.Format("Unable to remove the view '{0}': This view is referenced by the Widget in the view '{1}'.", view.Name, refView.Name));
                }

                if (Views.Count == 1) throw new Exception("Unable to remove the view: The report must contain at least one View.");
                Views.Remove(view);
                //Change the default view if necessary
                if (view.GUID == ViewGUID) ViewGUID = Views[0].GUID;
            }
            else
            {
                parent.Views.Remove(view);
            }
        }

        /// <summary>
        /// Add a schedule to the report
        /// </summary>
        public ReportSchedule AddSchedule(ReportOutput output)
        {
            ReportSchedule result = ReportSchedule.Create();
            string name = output != null ? string.Format("Schedule ({0})", output.Name) : "schedule for Tasks";
            result.Name = Helper.GetUniqueName(name, (from i in Schedules select i.Name).ToList());
            result.Report = this;
            if (output != null) result.OutputGUID = output.GUID;
            Schedules.Add(result);
            SchedulesModified = true;
            return result;
        }

        /// <summary>
        /// Remove a schedule from the report
        /// </summary>
        public void RemoveSchedule(ReportSchedule schedule)
        {
            Schedules.Remove(schedule);
            SchedulesModified = true;
            //Do not sync taks here, it will be done when report is really saved...
        }

        /// <summary>
        /// The image file name of the HTML result according to execution context 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetImageFile(string fileName)
        {
            if (ExecutionContext == ReportExecutionContext.WebReport || ExecutionContext == ReportExecutionContext.WebOutput)
            {
                return string.Format("{0}Images/{1}", WebUrl, fileName);
            }

            string result = Path.Combine(Repository.ViewImagesFolder, fileName);
            return "file:///" + result.Replace(Path.DirectorySeparatorChar.ToString(), "/");
        }

        /// <summary>
        /// The image file name or source of the HTML result according to execution context 
        /// </summary>
        public string AttachImageFile(string fileName)
        {
            if (ExecutionContext == ReportExecutionContext.WebReport)
            {
                return string.Format("{0}Images/{1}", WebUrl, fileName);
            }

            if (GenerateHTMLDisplay)
            {
                //Rendering the display, we return full path with file:///
                return GetImageFile(fileName);
            }

            //generating result file
            return Helper.HtmlMakeImageSrcData(Path.Combine(Repository.ViewImagesFolder, fileName));
        }

        /// <summary>
        /// For Script Files, insert the attached file names or their contents according to execution context 
        /// </summary>
        public string AttachScriptFiles(string fileNames)
        {
            string result = "";
            foreach (var fileName in Helper.GetStringList(fileNames))
            {
                if (!string.IsNullOrEmpty(fileName)) result += AttachScriptFile(fileName) + "\r\n";
            }
            return result;
        }

        /// <summary>
        /// For a Script File, insert the attached script file name or it content according to execution context 
        /// </summary>
        public string AttachScriptFile(string fileName, string cdnPath = "")
        {
            fileName = FileHelper.ConvertOSFilePath(fileName);
            string sourceFilePath = Path.Combine(Repository.ViewScriptsFolder, fileName);
            if (!File.Exists(sourceFilePath)) return "";

            if (!string.IsNullOrEmpty(cdnPath) && !Repository.Configuration.IsLocal) return string.Format("<script type='text/javascript' src='{0}'></script>", cdnPath);

            if (GenerateHTMLDisplay)
            {
                if (ExecutionContext == ReportExecutionContext.WebReport || ExecutionContext == ReportExecutionContext.WebOutput)
                {
                    return string.Format("<script type='text/javascript' src='{0}Scripts/{1}'></script>", WebUrl, fileName);
                }
                else
                {
                    //reference local file
                    string fileReference = "file:///" + HttpUtility.HtmlEncode(Path.Combine(Repository.ViewScriptsFolder, fileName));
                    return string.Format("<script type='text/javascript' src='{0}'></script>", fileReference);
                }
            }

            //generating result file, set the script directly in the result
            string result = "<script type='text/javascript'>\r\n";
            result += Repository.Configuration.GetAttachedFileContent(sourceFilePath);
            result += "\r\n</script>\r\n";
            return result;
        }

        /// <summary>
        /// For CSS Files, insert the attached file names or their contents according to execution context 
        /// </summary>
        public string AttachCSSFiles(string fileNames)
        {
            string result = "";
            foreach (var fileName in Helper.GetStringList(fileNames))
            {
                if (!string.IsNullOrEmpty(fileName)) result += AttachCSSFile(fileName) + "\r\n";
            }
            return result;
        }

        /// <summary>
        /// For a CSS File, insert the attached file name or its content according to execution context 
        /// </summary>
        public string AttachCSSFile(string fileName, string cdnPath = "")
        {
            fileName = FileHelper.ConvertOSFilePath(fileName);
            string sourceFilePath = Path.Combine(Repository.ViewContentFolder, fileName);
            if (!File.Exists(sourceFilePath)) return "";

            if (!string.IsNullOrEmpty(cdnPath) && !Repository.Configuration.IsLocal) return string.Format("<link type='text/css' href='{0}' rel='stylesheet'/>", cdnPath);

            if (GenerateHTMLDisplay)
            {
                if (ExecutionContext == ReportExecutionContext.WebReport || ExecutionContext == ReportExecutionContext.WebOutput)
                {
                    return string.Format("<link type='text/css' href='{0}Content/{1}' rel='stylesheet'/>", WebUrl, fileName);
                }
                else
                {
                    //reference local file
                    string fileReference = "file:///" + HttpUtility.HtmlEncode(Path.Combine(Repository.ViewContentFolder, fileName));
                    return string.Format("<link type='text/css' href='{0}' rel='stylesheet'/>", fileReference);
                }
            }

            //generating result file, set the CSS directly in the result
            string result = "<style type='text/css'>\r\n";
            result += Repository.Configuration.GetAttachedFileContent(sourceFilePath);
            result += "\r\n</style>\r\n";
            return result;
        }


        private List<ReportRestriction> _executionCommonRestrictions = null;
        /// <summary>
        /// List of common restrictions prompted at execution
        /// </summary>
        [XmlIgnore]
        public List<ReportRestriction> ExecutionCommonRestrictions
        {
            get
            {
                if (_executionCommonRestrictions == null)
                {
                    _executionCommonRestrictions = new List<ReportRestriction>();
                    foreach (ReportRestriction restriction in AllExecutionRestrictions.Where(i => i.Prompt != PromptType.None || i.AllowAPI).OrderBy(i => i.DisplayOrder))
                    {
                        if (restriction.IsInputValue || !_executionCommonRestrictions.Exists(i => i.IsIdenticalForPrompt(restriction)))
                        {
                            //Check that the restriction is not displayed in a restriction view
                            if (AllViews.Exists(i => i.Template.ForViewRestrictions && i.RestrictionsGUID.Contains(restriction.GUID)))
                            {
                                continue;
                            }

                            _executionCommonRestrictions.Add(restriction);
                        }
                    }

                    //Set same index in all models
                    foreach (ReportModel model in ExecutionModels)
                    {
                        foreach (ReportRestriction restriction in _executionCommonRestrictions)
                        {
                            ReportRestriction modelRestriction = model.Restrictions.Union(model.AggregateRestrictions).Union(model.CommonRestrictions).FirstOrDefault(i => i != restriction && i.IsIdenticalForPrompt(restriction));
                            if (modelRestriction != null)
                            {
                                modelRestriction.HtmlIndex = restriction.HtmlIndex;
                            }
                        }
                    }
                }
                return _executionCommonRestrictions;
            }
            set
            {
                _executionCommonRestrictions = value;
            }
        }

        /// <summary>
        /// List of restrictions prompted at execution from the Report Result
        /// </summary>
        [XmlIgnore]
        public List<ReportRestriction> ExecutionPromptedRestrictions
        {
            get
            {
                return ExecutionCommonRestrictions.Where(i => i.Prompt != PromptType.None).OrderBy(i => i.DisplayOrder).ToList();
            }
        }

        private List<ReportRestriction> _executionViewRestrictions = null;
        /// <summary>
        /// List of all view restrictions prompted at execution
        /// </summary>
        [XmlIgnore]
        public List<ReportRestriction> ExecutionViewRestrictions
        {
            get
            {
                if (_executionViewRestrictions == null)
                {
                    _executionViewRestrictions = new List<ReportRestriction>();

                    foreach (var view in AllViews.Where(i => i.Template.ForViewRestrictions))
                    {
                        foreach (ReportRestriction restriction in view.Restrictions)
                        {
                            if (restriction.IsInputValue || !_executionViewRestrictions.Exists(i => i.IsIdenticalForPrompt(restriction)))
                            {
                                //Force prompt if the restriction is involved in a view
                                if (restriction.Prompt == PromptType.None) restriction.Prompt = PromptType.Prompt;
                                _executionViewRestrictions.Add(restriction);
                            }
                        }

                    }

                    //Copy similar restriction in all models
                    foreach (ReportModel model in ExecutionModels)
                    {
                        foreach (ReportRestriction restriction in _executionViewRestrictions)
                        {
                            ReportRestriction modelRestriction = model.Restrictions.Union(model.AggregateRestrictions).Union(model.CommonRestrictions).FirstOrDefault(i => i != restriction && i.IsIdenticalForPrompt(restriction));
                            if (modelRestriction != null)
                            {
                                modelRestriction.CopyForPrompt(restriction);
                            }
                        }

                        if (model.IsLINQ)
                        {
                            foreach (ReportModel subModel in model.LINQSubModels)
                            {
                                foreach (ReportRestriction restriction in _executionViewRestrictions)
                                {
                                    ReportRestriction modelRestriction = subModel.Restrictions.Union(subModel.AggregateRestrictions).Union(subModel.CommonRestrictions).FirstOrDefault(i => i != restriction && i.IsIdenticalForPrompt(restriction));
                                    if (modelRestriction != null)
                                    {
                                        modelRestriction.CopyForPrompt(restriction);
                                    }
                                }

                            }
                        }
                    }
                }
                return _executionViewRestrictions;
            }
            set
            {
                _executionViewRestrictions = value;
            }
        }

        /// <summary>
        /// List of all restrictions of all models of the report, plus the input values
        /// </summary>
        [XmlIgnore]
        public List<ReportRestriction> AllRestrictions
        {
            get
            {
                List<ReportRestriction> result = new List<ReportRestriction>();
                result.AddRange(InputValues);
                foreach (ReportModel model in Models)
                {
                    result.AddRange(model.Restrictions.Union(model.AggregateRestrictions).Union(model.CommonRestrictions));
                }
                return result;
            }
        }

        /// <summary>
        /// List of all restrictions involved in the execution
        /// </summary>
        [XmlIgnore]
        public List<ReportRestriction> AllExecutionRestrictions
        {
            get
            {
                List<ReportRestriction> result = new List<ReportRestriction>();
                result.AddRange(ExecutionInputValues);
                foreach (ReportModel model in ExecutionModels)
                {
                    result.AddRange(model.ExecutionRestrictions.Union(model.ExecutionAggregateRestrictions).Union(model.ExecutionCommonRestrictions));
                    if (model.IsLINQ)
                    {
                        foreach (var subModel in model.LINQSubModels) result.AddRange(subModel.ExecutionRestrictions.Union(subModel.ExecutionAggregateRestrictions).Union(subModel.ExecutionCommonRestrictions));
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// For a given view, fill the list of models to execute
        /// </summary>
        public void GetModelsToExecute(ReportView view, List<ReportModel> result)
        {
            if (view.Model != null && view.Model.Elements.Count > 0 && !result.Contains(view.Model)) result.Add(view.Model);
            foreach (var child in view.Views) GetModelsToExecute(child, result);
        }

        /// <summary>
        /// Current view used when parsing
        /// </summary>
        [XmlIgnore]
        public ReportView CurrentView;

        /// <summary>
        /// Current model view used when parsing
        /// </summary>
        [XmlIgnore]
        public ReportView CurrentModelView;

        /// <summary>
        /// Current page used when parsing
        /// </summary>
        [XmlIgnore]
        public ResultPage CurrentPage;

        /// <summary>
        /// Translate a reference text from the Report context
        /// </summary>
        public string Translate(string reference)
        {
            if (ExecutionView == null) return reference;
            return Repository.Translate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "Report", reference);
        }

        /// <summary>
        /// Translate in a JavaScript format a reference text from the Report context 
        /// </summary>
        public string TranslateToJS(string reference)
        {
            return Helper.ToJS(Translate(reference));
        }

        /// <summary>
        /// Translate a reference text from a given context
        /// </summary>
        public string ContextTranslate(string context, string reference)
        {
            if (ExecutionView == null) return reference;
            return Repository.Translate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, context, reference);
        }

        /// <summary>
        /// Translate a reference text from the Report context with args parameter
        /// </summary>
        public string Translate(string reference, params object[] args)
        {
            try
            {
                return string.Format(Translate(reference), args);
            }
            catch
            {
                return reference;
            };
        }

        /// <summary>
        /// Translate a date keyword
        /// </summary>
        public string TranslateDateKeywords(string value)
        {
            string result = value;
            foreach (string name in Enum.GetNames(typeof(DateRestrictionKeyword)))
            {
                result = result.Replace(name, Translate(name));
            }
            return result;
        }

        /// <summary>
        /// Translate a date keyword in english
        /// </summary>
        public string TranslateDateKeywordsToEnglish(string value)
        {
            string result = value;
            foreach (string name in Enum.GetNames(typeof(DateRestrictionKeyword)))
            {
                result = result.Replace(Translate(name), name);
            }
            return result;
        }

        /// <summary>
        /// List of date keywords in english
        /// </summary>
        [XmlIgnore]
        public string DateKeywordsList
        {
            get
            {
                string result = "";
                foreach (string name in Enum.GetNames(typeof(DateRestrictionKeyword)))
                {
                    Helper.AddValue(ref result, ",", Translate(name));
                }
                return result;
            }
        }

        /// <summary>
        /// Current culture of the execution
        /// </summary>
        [XmlIgnore]
        public CultureInfo CultureInfo
        {
            get { return ExecutionView.CultureInfo; }
        }

        /// <summary>
        /// True if the report result has an external viewer (for PDF, CSV or Excel)
        /// </summary>
        [XmlIgnore]
        public bool HasExternalViewer
        {
            get
            {
                return Format == ReportFormat.pdf || Format == ReportFormat.excel || Format == ReportFormat.csv || Format == ReportFormat.custom;
            }
        }

        /// <summary>
        /// True if the drill navigation is enabled
        /// </summary>
        [XmlIgnore]
        public bool IsDrillEnabled
        {
            get
            {
                return ExecutionView.GetBoolValue(Parameter.DrillEnabledParameter);
            }
        }

        /// <summary>
        /// True if the sub-reports navigation is enabled
        /// </summary>
        [XmlIgnore]
        public bool IsSubReportsEnabled
        {
            get
            {
                return ExecutionView.GetBoolValue(Parameter.SubReportsEnabledParameter);
            }
        }

        /// <summary>
        /// True if the server pagination for DataTables is enabled
        /// </summary>
        [XmlIgnore]
        public bool IsServerPaginationEnabled
        {
            get
            {
                return ExecutionView.GetBoolValue(Parameter.ServerPaginationParameter) && !PrintLayout && Format != ReportFormat.csv && !ForOutput;
            }
        }

        /// <summary>
        /// True if a PDF conversion will be done after the HTML generation
        /// </summary>
        [XmlIgnore]
        public bool PdfConversion = false;

        /// <summary>
        /// Report format
        /// </summary>
        [XmlIgnore]
        public ReportFormat Format
        {
            get { return (ReportFormat)Enum.Parse(typeof(ReportFormat), ExecutionView.GetValue(Parameter.ReportFormatParameter)); }
            set { ExecutionView.SetParameter(Parameter.ReportFormatParameter, value.ToString()); }
        }

        /// <summary>
        /// File extension of the result file
        /// </summary>
        [XmlIgnore]
        public string ResultExtension
        {
            get
            {
                var format = Format;
                if (format == ReportFormat.csv) return "csv";
                if (format == ReportFormat.excel) return "xlsx";
                if (format == ReportFormat.pdf) return "html"; //converter to pdf 
                return "html";
            }
        }

        /// <summary>
        /// True if the print layout should be used for the HTML generation 
        /// </summary>
        [XmlIgnore]
        //Indicates if we use the print layout
        public bool PrintLayout
        {
            get { return Format == ReportFormat.print || Format == ReportFormat.pdf; }
        }

        /// <summary>
        /// List of drill parents at execution
        /// </summary>
        [XmlIgnore]
        public List<string> DrillParents = new List<string>();


        /// <summary>
        /// List of links used for Script navigation
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, NavigationLink> NavigationLinks = new Dictionary<string, NavigationLink>();


        /// <summary>
        /// Helper to update a view parameter
        /// </summary>
        public void UpdateViewParameter(string viewId, string parameterName, string parameterValue)
        {
            ReportView view = ExecutionView.GetView(viewId);
            if (view != null)
            {
                Parameter parameter = view.Parameters.FirstOrDefault(i => i.Name == parameterName);
                if (parameter != null)
                {
                    parameter.Value = parameterValue;
                }
            }
        }

        /// <summary>
        /// Object that can be used at run-time for any purpose
        /// </summary>
        [XmlIgnore]
        public object Tag;

        /// <summary>
        /// Object that can be used at run-time for any purpose
        /// </summary>
        [XmlIgnore]
        public object Tag2;

        /// <summary>
        /// Object that can be used at run-time for any purpose
        /// </summary>
        [XmlIgnore]
        public object Tag3;

        /// <summary>
        /// Helper to find a view from its identifier
        /// </summary>
        public ReportView FindView(List<ReportView> views, string guid)
        {
            ReportView result = null;
            foreach (var view in views)
            {
                if (view.GUID == guid)
                {
                    result = view;
                    break;
                }
                result = FindView(view.Views, guid);

                if (result != null) break;
            }
            return result;
        }

        /// <summary>
        /// Helper to find a view from its name
        /// </summary>
        public ReportView FindViewFromName(List<ReportView> views, string name)
        {
            ReportView result = null;
            foreach (var view in views)
            {
                if (view.Name == name)
                {
                    result = view;
                    break;
                }
                result = FindViewFromName(view.Views, name);

                if (result != null) break;
            }
            return result;
        }


        void fillFullViewList(List<ReportView> views, List<ReportView> result)
        {
            foreach (var view in views)
            {
                result.Add(view);
                fillFullViewList(view.Views, result);
            }
        }

        /// <summary>
        /// Helper to list of all the views of the report
        /// </summary>
        [XmlIgnore]
        public List<ReportView> AllViews
        {
            get
            {
                var result = new List<ReportView>();
                fillFullViewList(Views, result);
                return result;
            }
        }
        /// <summary>
        /// Helper to get the root view from a child view
        /// </summary>
        public ReportView GetRootView(ReportView child)
        {
            ReportView result = null;
            foreach (var view in Views)
            {
                if (FindView(view.Views, child.GUID) == child)
                {
                    result = view;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Helper to get the a view from its execution id
        /// </summary>
        public ReportView GetViewFromExecId(string viewId)
        {
            ReportView result = null;
            foreach (var view in Views)
            {
                result = view.GetView(viewId);
                if (result != null) break;
            }
            return result;
        }


        /// <summary>
        /// Get the widget view from the widgetGUID
        /// </summary>
        public void GetWidgetViewToParse(List<ReportView> views, string widgetGUID, ref ReportView widgetView, ref ReportView modelView)
        {
            foreach (var view in views)
            {
                if (view.WidgetDefinition.GUID == widgetGUID)
                {
                    widgetView = view;

                    var lastView = widgetView;
                    while (lastView.ParentView != null)
                    {
                        if (lastView.ParentView.Model != null)
                        {
                            modelView = lastView.ParentView;
                            break;
                        }
                        else lastView = lastView.ParentView;
                    }
                }
                if (widgetView != null) break;

                GetWidgetViewToParse(view.Views, widgetGUID, ref widgetView, ref modelView);
            }
        }

        /// <summary>
        /// List of widget views of the report
        /// </summary>
        public List<ReportView> GetWidgetViews()
        {
            List<ReportView> result = new List<ReportView>();
            foreach (var view in Views.OrderBy(i => i.SortOrder))
            {
                getWidgetViews(result, view);
            }
            return result;
        }

        void getWidgetViews(List<ReportView> widgetViews, ReportView view)
        {
            if (view.WidgetDefinition.IsPublished) widgetViews.Add(view);
            foreach (var subview in view.Views.OrderBy(i => i.SortOrder))
            {
                getWidgetViews(widgetViews, subview);
            }
        }

        /// <summary>
        /// Helper to find a view from its template name
        /// </summary>
        public ReportView FindViewFromTemplate(List<ReportView> views, string templateName)
        {
            ReportView result = null;
            foreach (var view in views)
            {
                if (view.TemplateName == templateName)
                {
                    result = view;
                    break;
                }
                result = FindViewFromTemplate(view.Views, templateName);

                if (result != null) break;
            }
            if (result != null) result.InitParameters(false);
            return result;
        }

        /// <summary>
        /// Cancel the report execution
        /// </summary>
        public void CancelExecution()
        {
            int cnt = 30;
            while (--cnt >= 0 && IsExecuting)
            {
                Cancel = true;
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Log Interface implementation
        /// </summary>
        public void LogMessage(string message, params object[] args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message.Trim()) && args.Length == 0)
                {
                    Debug.WriteLine(message);
                    ExecutionMessages += message;
                }
                else
                {
                    var msg = string.Format("{0} {1}\r\n", DateTime.Now.ToLongTimeString(), (args.Length == 0 ? message : string.Format(message, args)));
                    Debug.WriteLine(msg);
                    ExecutionMessages += msg;
                }
            }
            catch (Exception ex)
            {
                ExecutionMessages += string.Format("Error logging {0}\r\n{1}\r\n", message, ex.Message);
            }
        }

        /// <summary>
        /// Helper to get a report source from its name
        /// </summary>
        public ReportSource GetReportSource(string sourceName)
        {
            return Sources.FirstOrDefault(i => i.Name == sourceName);
        }

        /// <summary>
        /// Helper to get a report model from its name
        /// </summary>
        public ReportModel GetReportModel(string modelName)
        {
            return Models.FirstOrDefault(i => i.Name == modelName);
        }

        #region Translation Helpers

        string TranslationFilePath
        {
            get
            {
                return FilePath.Replace(Repository.ReportsFolder, "").Replace(Repository.SubReportsFolder, string.Format("{1}..{1}{0}", Path.GetFileNameWithoutExtension(Repository.SubReportsFolder), Path.DirectorySeparatorChar));
            }
        }

        //Helpers for translations

        /// <summary>
        /// Repository Translate using the ReportViewName context
        /// </summary>
        public string TranslateRepository(string context, string instance, string reference)
        {
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, context, instance, reference);
        }

        /// <summary>
        /// Translate using the ReportDisplayName context
        /// </summary>
        public string TranslateDisplayName(string displayName)
        {
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "ReportDisplayName", TranslationFilePath, displayName);
        }

        /// <summary>
        /// Translate using the ReportViewName context
        /// </summary>
        public string TranslateViewName(string viewName)
        {
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "ReportViewName", TranslationFilePath, viewName);
        }

        /// <summary>
        /// Translate using the ReportOutputName context
        /// </summary>
        public string TranslateOutputName(string outputName)
        {
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "ReportOutputName", TranslationFilePath, outputName);
        }

        /// <summary>
        /// Translate using the ReportGeneral context
        /// </summary>
        public string TranslateGeneral(string reference)
        {
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "ReportGeneral", TranslationFilePath, reference);
        }

        /// <summary>
        /// Translate using the Element context
        /// </summary>
        public string TranslateElement(ReportElement element, string reference)
        {
            if (string.IsNullOrEmpty(element.MetaColumnGUID)) return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "Element", element.DisplayNameEl, reference);
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "Element", element.MetaColumn.Category + '.' + element.DisplayNameEl, reference);
        }

        /// <summary>
        /// Translate the enum using the Enum context
        /// </summary>
        public string EnumDisplayValue(MetaEnum instance, string id, bool forRestriction = false)
        {
            string result = instance.GetDisplayValue(id, forRestriction);
            if (instance.Translate) result = Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "Enum", instance.Name, result);
            return result;
        }

        /// <summary>
        /// Translate the enum message using the EnumMessage context
        /// </summary>
        public string EnumMessage(MetaEnum instance)
        {
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "EnumMessage", instance.Name, instance.Message);
        }
        #endregion

        #region Log

        /// <summary>
        /// Log the report execution in the Log repository folder
        /// </summary>
        public void LogExecution()
        {
            try
            {
                var message = ExecutionMessages;
                if (!string.IsNullOrEmpty(ExecutionErrors)) message += string.Format("\r\nError Message:\r\n{0}\r\n", ExecutionErrors);
                if (!Cancel && !string.IsNullOrEmpty(ExecutionErrorStackTrace)) message += string.Format("\r\nError Stack Trace:\r\n{0}\r\n", ExecutionErrorStackTrace);
                string log = string.Format("********************\r\nExecution of '{0}' on {1} {2}\r\n{3}********************\r\n", FilePath, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), message);
                Helper.WriteDailyLog("executions", Repository.LogsFolder, Repository.Configuration.LogDays, log);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion
    }
}