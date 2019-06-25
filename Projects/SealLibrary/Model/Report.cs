//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
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

namespace Seal.Model
{
    public interface ReportExecutionLog
    {
        void LogMessage(string message, params object[] args);
    }

    public class Report : ReportExecutionLog
    {
        private string _GUID;
        public string GUID
        {
            get { return _GUID; }
            set { _GUID = value; }
        }

        private List<ReportSource> _sources = new List<ReportSource>();
        public List<ReportSource> Sources
        {
            get { return _sources; }
            set { _sources = value; }
        }
        public bool ShouldSerializeSources() { return _sources.Count > 0; }

        private List<ReportModel> _models = new List<ReportModel>();
        public List<ReportModel> Models
        {
            get { return _models; }
            set { _models = value; }
        }
        public bool ShouldSerializeModels() { return _models.Count > 0; }

        private List<ReportOutput> _outputs = new List<ReportOutput>();
        public List<ReportOutput> Outputs
        {
            get { return _outputs; }
            set { _outputs = value; }
        }
        public bool ShouldSerializeOutputs() { return _outputs.Count > 0; }

        private List<ReportTask> _taks = new List<ReportTask>();
        public List<ReportTask> Tasks
        {
            get { return _taks; }
            set { _taks = value; }
        }
        public bool ShouldSerializeTasks() { return _taks.Count > 0; }

        List<CommonScript> _commonScripts = new List<CommonScript>();
        public List<CommonScript> CommonScripts
        {
            get { return _commonScripts; }
            set { _commonScripts = value; }
        }
        public bool ShouldSerializeCommonScripts() { return _commonScripts.Count > 0; }

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

        private string _tasksScript = "";
        public string TasksScript
        {
            get { return _tasksScript; }
            set { _tasksScript = value; }
        }
        public bool ShouldSerializeTasksScript() { return !string.IsNullOrEmpty(_tasksScript); }


        //Input Values
        private List<ReportRestriction> _inputValues = new List<ReportRestriction>();
        public List<ReportRestriction> InputValues
        {
            get { return _inputValues; }
            set { _inputValues = value; }
        }
        public bool ShouldSerializeInputValues() { return _inputValues.Count > 0; }

        public ReportRestriction GetInputValueByName(string name)
        {
            return InputValues.FirstOrDefault(i => i.DisplayNameEl.ToLower() == name.ToLower());
        }


        [XmlIgnore]
        public List<ReportRestriction> ExecutionReportRestrictions
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

        private List<ReportView> _views = new List<ReportView>();
        public List<ReportView> Views
        {
            get { return _views; }
            set { _views = value; }
        }

        private string _displayName = "";
        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }
        public bool ShouldSerializeDisplayName() { return !string.IsNullOrEmpty(_displayName); }

        private string _displayNameEx = null;
        [XmlIgnore]
        public string DisplayNameEx
        {
            get
            {
                if (!string.IsNullOrEmpty(_displayNameEx)) return _displayNameEx;
                if (!string.IsNullOrEmpty(_displayName))
                {
                    try
                    {
                        _displayNameEx = RazorHelper.CompileExecute(_displayName, this);
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

        private string _initScript = "";
        public string InitScript
        {
            get { return _initScript; }
            set { _initScript = value; }
        }
        public bool ShouldSerializeInitScript() { return !string.IsNullOrEmpty(_initScript); }

        private string _viewGUID;
        public string ViewGUID
        {
            get { return _viewGUID; }
            set { _viewGUID = value; }
        }

        public string CurrentViewGUID;

        private List<ReportSchedule> _schedules = new List<ReportSchedule>();
        public List<ReportSchedule> Schedules
        {
            get { return _schedules; }
            set { _schedules = value; }
        }
        public bool ShouldSerializeSchedules() { return _schedules.Count > 0; }

        private int _widgetCache = 60;
        public int WidgetCache
        {
            get { return _widgetCache; }
            set { _widgetCache = value; }
        }
        public bool ShouldSerializeWidgetCache() { return _widgetCache != 60; }

        [XmlIgnore]
        public Repository Repository = null;

        [XmlIgnore]
        public string FilePath = "";

        [XmlIgnore]
        public DateTime LastModification;

        [XmlIgnore]
        public string ResultFilePath;

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
        [XmlIgnore]
        public string HTMLDisplayFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_HTMLDisplayFilePath)) _HTMLDisplayFilePath = FileHelper.GetUniqueFileName(Path.Combine(GenerationFolder, "result.htm"));
                return _HTMLDisplayFilePath;
            }
            set { _HTMLDisplayFilePath = value; }
        }

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
                            if (!Directory.Exists(result)) Directory.CreateDirectory(result);
                        }
                    }
                }
                return result;
            }
        }

        [XmlIgnore]
        public string WebUrl = "";

        [XmlIgnore]
        public bool IsMobileDevice = false;

        [XmlIgnore]
        public string ExecutionGUID = Guid.NewGuid().ToString();

        [XmlIgnore]
        public string GenerationFolder
        {
            get
            {
                return FileHelper.TempApplicationDirectory;
            }
        }

        [XmlIgnore]
        public string ResultFileName
        {
            get
            {
                string fileName = DisplayNameEx.ToString();
                if (ForOutput)
                {
                    if (OutputToExecute.Device is OutputFolderDevice)
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
                fileName = Helper.CleanFileName(fileName) + ".htm";
                if (!ForOutput) fileName = fileName.Replace(" ", "_");
                return fileName;
            }
        }

        public void InitForExecution()
        {
            string fileName = "", fileFolder = "";
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
                var template = ExecutionView.Template; //This force to init parameters
                fileName = ResultFileName;
                fileFolder = FileHelper.TempApplicationDirectory;

                if (ForOutput && OutputToExecute.Device is OutputFolderDevice)
                {
                    if (Format != ReportFormat.pdf && Format != ReportFormat.excel) fileFolder = OutputFolderDeviceResultFolder;
                    //For folder output, we do not need a unique file name
                    ResultFilePath = Path.Combine(fileFolder, Path.GetFileNameWithoutExtension(fileName)) + "." + ResultExtension;
                }
                else
                {
                    //get unique file name in the result folder
                    ResultFilePath = FileHelper.GetUniqueFileName(Path.Combine(fileFolder, fileName), "." + ResultExtension);
                }
                //Display path is always an HTML one...
                HTMLDisplayFilePath = FileHelper.GetUniqueFileName(Path.Combine(GenerationFolder, FileHelper.GetResultFilePrefix(ResultFilePath) + ".htm"));

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
        }

        [XmlIgnore]
        public bool HasValidationErrors = false;
        [XmlIgnore]
        public string ExecutionMessages;
        [XmlIgnore]
        public string ExecutionErrors;
        [XmlIgnore]
        public string ExecutionErrorStackTrace;
        [XmlIgnore]
        public string WebExecutionErrors
        {
            get
            {
                return !string.IsNullOrEmpty(WebUrl) && !HasValidationErrors && !string.IsNullOrEmpty(ExecutionErrors) ? Translate("This report has execution errors. Please check details in the Windows Event Viewer...") : ExecutionErrors;
            }
        }

        [XmlIgnore]
        public bool ShowExecutionMessages
        {
            get
            {
                return ExecutionView.GetValue("messages_mode") == "enabledshown" || (ExecutionView.GetValue("messages_mode") == "enabled" && !string.IsNullOrEmpty(WebExecutionErrors));
            }
        }

        [XmlIgnore]
        public string LoadErrors = "";

        [XmlIgnore]
        public string UpgradeWarnings = "";

        [XmlIgnore]
        public string TemplateParsingErrors;

        [XmlIgnore]
        public ReportStatus Status = ReportStatus.NotExecuted;

        [XmlIgnore]
        public string ExecutionName
        {
            get { return TranslateDisplayName((string.IsNullOrEmpty(DisplayNameEx) ? ExecutionView.Name : DisplayNameEx)) + (OutputToExecute != null && Status != ReportStatus.RenderingResult ? string.Format(" - {0}", TranslateOutputName(OutputToExecute.Name)) : ""); }
        }

        [XmlIgnore]
        public List<ReportTask> ExecutionTasks
        {
            get
            {
                return Tasks.Where(i => i.Enabled).ToList();
            }
        }

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

        //Progression values and messages
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

        [XmlIgnore]
        public int ExecutionProgressionModels
        {
            get
            {
                return ExecutionModels.Count == 0 ? 100 : (100 * ExecutionModels.Count(i => i.Progression >= 100)) / ExecutionModels.Count;
            }
        }

        [XmlIgnore]
        public string ExecutionProgressionModelsMessage
        {
            get
            {
                return string.Format("{0}/{1} {2}", ExecutionModels.Count(i => i.Progression >= 100), ExecutionModels.Count, ExecutionModels.Count > 1 ? Translate("Models loaded...") : Translate("Model loaded..."));
            }
        }

        [XmlIgnore]
        public int ExecutionProgressionTasks
        {
            get
            {
                return ExecutionTasks.Count == 0 ? 100 : (100 * ExecutionTasks.Count(i => i.Progression >= 100)) / ExecutionTasks.Count;
            }
        }

        [XmlIgnore]
        public string ExecutionProgressionTasksMessage
        {
            get
            {
                return string.Format("{0}/{1} {2}", ExecutionTasks.Count(i => i.Progression >= 100), ExecutionTasks.Count, ExecutionTasks.Count > 1 ? Translate("Tasks executed...") : Translate("Task executed..."));
            }
        }

        [XmlIgnore]
        public bool CheckingExecution = false;

        [XmlIgnore]
        public bool SchedulesModified = false;

        [XmlIgnore]
        public bool SchedulesWithCurrentUser = false;

        [XmlIgnore]
        public bool IsExecuting
        {
            get { return (Status != ReportStatus.NotExecuted && Status != ReportStatus.Executed); }
        }
        [XmlIgnore]
        public bool Cancel = false;
        [XmlIgnore]
        public bool RenderOnly = false;
        [XmlIgnore]
        public DateTime ExecutionStartDate;
        [XmlIgnore]
        public DateTime ExecutionRenderingDate;
        [XmlIgnore]
        public DateTime ExecutionEndDate;
        [XmlIgnore]
        public TimeSpan ExecutionModelDuration
        {
            get { return (ExecutionRenderingDate - ExecutionStartDate); }
        }

        [XmlIgnore]
        public bool IsNavigating = false; //If false, do evaluate restrictions prompted...

        [XmlIgnore]
        public bool HasNavigation = false; //If true, navigation must be activated...

        //Output management
        [XmlIgnore]
        public ReportOutput OutputToExecute = null;
        [XmlIgnore]
        public bool ForOutput
        {
            get { return OutputToExecute != null; }
        }

        //One task only
        [XmlIgnore]
        public ReportTask TaskToExecute = null;

        [XmlIgnore]
        public bool GenerateHTMLDisplay
        {
            get
            {
                return Status == ReportStatus.RenderingDisplay || Status == ReportStatus.NotExecuted;
            }
        }

        [XmlIgnore]
        public bool IsBasicHTMLWithNoOutput //Indicates that the report is not for an output and has no external viewer
        {
            get
            {
                return !ForOutput && !HasExternalViewer;
            }
        }

        [XmlIgnore]
        public ReportExecutionContext ExecutionContext = ReportExecutionContext.DesignerReport;

        [XmlIgnore]
        public SecurityUser SecurityContext = null;

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

        [XmlIgnore]
        public TimeSpan ExecutionFullDuration
        {
            get { return (ExecutionEndDate - ExecutionStartDate); }
        }

        [XmlIgnore]
        public Dictionary<string, string> PreInputRestrictions = new Dictionary<string, string>();
        [XmlIgnore]
        public Dictionary<string, string> InputRestrictions = new Dictionary<string, string>();

        public string GetInputRestriction(string key)
        {
            if (InputRestrictions.ContainsKey(key)) return InputRestrictions[key];
            return "";
        }

        [XmlIgnore]
        public bool HasErrors
        {
            get { return !string.IsNullOrEmpty(ExecutionErrors); }
        }

        TaskFolder _taskFolder = null;
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

        [XmlIgnore]
        public bool HasRestrictions
        {
            get { return ExecutionCommonRestrictions.Count > 0; }
        }

        [XmlIgnore]
        public bool HasChart
        {
            get { return HasNVD3Chart || HasChartJSChart || HasPlotlyChart; }
        }

        [XmlIgnore]
        public bool HasNVD3Chart
        {
            get { return Models.Exists(i => i.HasNVD3Serie); }
        }

        [XmlIgnore]
        public bool HasChartJSChart
        {
            get { return Models.Exists(i => i.HasChartJSSerie); }
        }

        [XmlIgnore]
        public bool HasPlotlyChart
        {
            get { return Models.Exists(i => i.HasPlotlySerie); }
        }

        [XmlIgnore]
        public Encoding ResultFileEncoding
        {
            get
            {
                //Utf8 by default, except for CSV if specified
                return (Format == ReportFormat.csv && !ExecutionView.GetBoolValue(Parameter.CSVUtf8Parameter)) ? Encoding.Default : Encoding.UTF8;
            }
        }

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


        static public Report LoadFromFile(string path, Repository repository)
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

                if (result.Views.Count == 0)
                {
                    var view = result.AddRootView();
                    view.Name = "View";
                }
                result.InitReferences();

                //Refresh enums
                foreach (ReportSource source in result.Sources) source.RefreshEnumsOnDbConnection();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to read the file '{0}'.\r\n{1}\r\n", path, ex.Message, ex.StackTrace));
            }
            return result;
        }

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

            //and a 2 models
            if (result.Models.Count == 0)
            {
                result.AddModel(false);
                var model = result.AddModel(true);
                model.Name = "SQL Model";
            }
            //Add default views
            ReportView view = result.AddModelHTMLView();
            if (view == null) throw new Exception(string.Format("Unable to find any view in your repository. Check that your repository folder '{0}' contains all the default sub-folders and files...", repository.RepositoryPath));
            result.ViewGUID = view.GUID;

            view = result.AddModelHTMLView();
            view.Name = "SQL view";

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

        public void SaveToFile()
        {
            SaveToFile(FilePath);
        }

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
            catch { }
        }


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
            if (SchedulesModified) SynchronizeTasks();
            SchedulesModified = false;
        }

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

        public void Serialize(string path)
        {
            try
            {
                foreach (var output in Outputs) output.BeforeSerialization();
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
                    source.TempJoins = source.MetaData.Joins.ToList();
                    source.TempEnums = source.MetaData.Enums.ToList();
                    source.Connections.RemoveAll(i => !i.IsEditable);
                    source.MetaData.Tables.RemoveAll(i => !i.IsEditable);
                    source.MetaData.Joins.RemoveAll(i => !i.IsEditable);
                    source.MetaData.Enums.RemoveAll(i => !i.IsEditable);
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
                    source.Connections = source.TempConnections;
                    source.MetaData.Tables = source.TempTables;
                    source.MetaData.Joins = source.TempJoins;
                    source.MetaData.Enums = source.TempEnums;
                }
                foreach (var view in Views) view.AfterSerialization();
                foreach (var output in Outputs) output.AfterSerialization();
            }
        }

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
            return result;
        }

        public void RemoveSource(ReportSource source)
        {
            if (Sources.Count == 1) throw new Exception("The report must contain at least a Data Source");

            foreach (var model in Models.Where(i => i.SourceGUID == source.GUID))
            {
                if (model.Elements.Count > 0 || !string.IsNullOrEmpty(model.RestrictionText)) throw new Exception(string.Format("The source '{0}' is already used by a model.", source.Name));
                model.SourceGUID = Sources.First(i => i.GUID != source.GUID).GUID;
            }
            Sources.Remove(source);
        }

        public ReportModel AddModel(bool sqlModel)
        {
            if (Sources.Count == 0) throw new Exception("Unable to create a model: No source available.\r\nPlease create a source first.");
            ReportModel result = ReportModel.Create();
            result.Name = Helper.GetUniqueName("Model", (from i in Models select i.Name).ToList());
            if (sqlModel)
            {
                result.Table = MetaTable.Create();
                result.Table.DynamicColumns = true;
            }
            ReportSource source = Sources.FirstOrDefault(i => i.IsDefault);
            if (source == null) source = Sources[0];
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

        public void RemoveModel(ReportModel model)
        {
            if (isModelUsedInViews(Views, model)) throw new Exception(string.Format("The model '{0}' is already used by a view.", model.Name));
            if (Models.Count == 1) throw new Exception("Unable to remove the model: The report must contain at least one Model.");

            Models.Remove(model);
        }

        public ReportOutput AddOutput(OutputDevice device)
        {
            ReportOutput result = ReportOutput.Create();
            result.Name = Helper.GetUniqueName(string.Format("output ({0})", device.Name), (from i in Outputs select i.Name).ToList());
            if (device is OutputFolderDevice)
            {
                result.FolderPath = string.IsNullOrEmpty(FilePath) ? Repository.SealRepositoryKeyword + "\\Reports\\" : Path.GetDirectoryName(FilePath).Replace(Repository.RepositoryPath, Repository.SealRepositoryKeyword);
                result.FileName = Repository.SealReportDisplayNameKeyword;
            }

            result.Report = this;
            result.OutputDeviceGUID = device.GUID;
            result.ViewGUID = ViewGUID;
            result.InitReferences();
            Outputs.Add(result);
            return result;
        }

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

        public void RemoveTask(ReportTask task)
        {
            Tasks.Remove(task);
        }

        public ReportView AddModelHTMLView()
        {
            return AddView(ReportViewTemplate.ModelName);
        }

        public ReportView AddRootView()
        {
            ReportViewTemplate reportTemplate = RepositoryServer.GetViewTemplate("Report");
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

        public ReportView AddView(string name)
        {
            ReportView view = null;
            ReportViewTemplate modelTemplate = RepositoryServer.GetViewTemplate(name);
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

        public ReportView AddChildView(ReportView parent, string templateName)
        {
            return AddChildView(parent, RepositoryServer.GetViewTemplate(templateName));
        }

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

        public void RemoveView(ReportView parent, ReportView view)
        {
            if (parent == null)
            {
                foreach (ReportOutput output in Outputs)
                {
                    if (output.ViewGUID == view.GUID) throw new Exception(string.Format("Unable to remove the view '{0}': This view is used by the output '{1}'.", view.Name, output.Name));
                }

                if (Views.Count == 1) throw new Exception("Unable to remove the view: The report must contain at least one View.");
                Views.Remove(view);
                //Change the default view if necessary
                if (view.GUID == ViewGUID) ViewGUID = Views[0].GUID;
            }
            else parent.Views.Remove(view);
        }

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

        public void RemoveSchedule(ReportSchedule schedule)
        {
            Schedules.Remove(schedule);
            SchedulesModified = true;
            //Do not sync taks here, it will be done when report is really saved...
        }


        public string GetImageFile(string fileName)
        {
            if (ExecutionContext == ReportExecutionContext.WebReport || ExecutionContext == ReportExecutionContext.WebOutput)
            {
                return string.Format("{0}Images/{1}", WebUrl, fileName);
            }

            string result = Path.Combine(Repository.ViewImagesFolder, fileName);
            return "file:///" + result.Replace("\\", "/");
        }

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


        string GetAttachedFileContent(string fileName)
        {
            string result = File.ReadAllText(fileName);
            foreach (var item in Repository.Configuration.FileReplacePatterns.Where(i => i.FileName == Path.GetFileName(fileName)))
            {
                result = result.Replace(item.OldValue, item.NewValue);
            }
            return result;
        }

        public string AttachScriptFile(string fileName, string cdnPath = "")
        {
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

            result += GetAttachedFileContent(sourceFilePath);
            result += "\r\n</script>\r\n";
            return result;
        }

        public string AttachCSSFile(string fileName, string cdnPath = "")
        {
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
            result += GetAttachedFileContent(sourceFilePath);
            result += "\r\n</style>\r\n";
            return result;
        }

        private List<ReportRestriction> _executionCommonRestrictions = null;
        [XmlIgnore]
        public List<ReportRestriction> ExecutionCommonRestrictions
        {
            get
            {
                if (_executionCommonRestrictions == null)
                {

                    int index = 0;
                    _executionCommonRestrictions = new List<ReportRestriction>();
                    foreach (ReportRestriction restriction in AllExecutionRestrictions.Where(i => i.Prompt != PromptType.None))
                    {
                        if (
                            restriction.IsInputValue ||
                            !_executionCommonRestrictions.Exists(i => (i.IsCommonRestrictionValue && i.Name == restriction.Name) || (!i.IsCommonRestrictionValue && i.MetaColumnGUID == restriction.MetaColumnGUID && i.DisplayNameEl == restriction.DisplayNameEl))
                            )
                        {
                            restriction.HtmlIndex = index.ToString();
                            _executionCommonRestrictions.Add(restriction);
                            index++;
                        }
                    }

                    //Set index in all models
                    foreach (ReportModel model in ExecutionModels)
                    {
                        foreach (ReportRestriction restriction in _executionCommonRestrictions)
                        {
                            ReportRestriction modelRestriction = model.Restrictions.Union(model.AggregateRestrictions).Union(model.CommonRestrictions).FirstOrDefault(i => (i.IsCommonRestrictionValue && i.Name == restriction.Name) || (!i.IsCommonRestrictionValue && i.MetaColumnGUID == restriction.MetaColumnGUID && i.DisplayNameEl == restriction.DisplayNameEl));
                            if (modelRestriction != null) modelRestriction.HtmlIndex = restriction.HtmlIndex;
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

        [XmlIgnore]
        public List<ReportRestriction> AllExecutionRestrictions
        {
            get
            {
                List<ReportRestriction> result = new List<ReportRestriction>();
                result.AddRange(ExecutionReportRestrictions);
                foreach (ReportModel model in ExecutionModels)
                {
                    result.AddRange(model.ExecutionRestrictions.Union(model.ExecutionAggregateRestrictions).Union(model.ExecutionCommonRestrictions));
                }
                return result;
            }
        }


        public void GetModelsToExecute(ReportView view, List<ReportModel> result)
        {
            if (view.Model != null && view.Model.Elements.Count > 0 && !result.Contains(view.Model)) result.Add(view.Model);
            foreach (var child in view.Views) GetModelsToExecute(child, result);
        }


        [XmlIgnore]
        public ReportView CurrentView; //Current view used when parsing
        [XmlIgnore]
        public ReportView CurrentModelView; //Current model view used when parsing
        [XmlIgnore]
        public ResultPage CurrentPage; //Current result page used when parsing

        //Translations
        public string Translate(string reference)
        {
            if (ExecutionView == null) return reference;
            return Repository.Translate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "Report", reference);
        }

        public string TranslateToJS(string reference)
        {
            return Helper.ToJS(Translate(reference));
        }

        public string ContextTranslate(string context, string reference)
        {
            if (ExecutionView == null) return reference;
            return Repository.Translate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, context, reference);
        }

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

        public string TranslateDateKeywords(string value)
        {
            string result = value;
            foreach (string name in Enum.GetNames(typeof(DateRestrictionKeyword)))
            {
                result = result.Replace(name, Translate(name));
            }
            return result;
        }

        public string TranslateDateKeywordsToEnglish(string value)
        {
            string result = value;
            foreach (string name in Enum.GetNames(typeof(DateRestrictionKeyword)))
            {
                result = result.Replace(Translate(name), name);
            }
            return result;
        }

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

        [XmlIgnore]
        public CultureInfo CultureInfo
        {
            get { return ExecutionView.CultureInfo; }
        }

        [XmlIgnore]
        public bool HasExternalViewer
        {
            get
            {
                return Format == ReportFormat.pdf || Format == ReportFormat.excel || Format == ReportFormat.csv || Format == ReportFormat.custom;
            }
        }


        [XmlIgnore]
        public bool IsDrillEnabled
        {
            get
            {
                return ExecutionView.GetBoolValue(Parameter.DrillEnabledParameter);
            }
        }

        [XmlIgnore]
        public bool IsSubReportsEnabled
        {
            get
            {
                return ExecutionView.GetBoolValue(Parameter.SubReportsEnabledParameter);
            }
        }

        [XmlIgnore]
        public bool IsServerPaginationEnabled
        {
            get
            {
                return ExecutionView.GetBoolValue(Parameter.ServerPaginationParameter) && !PrintLayout && Format != ReportFormat.csv && !ForOutput;
            }
        }

        [XmlIgnore]
        public bool PdfConversion = false;

        [XmlIgnore]
        public ReportFormat Format
        {
            get { return (ReportFormat)Enum.Parse(typeof(ReportFormat), ExecutionView.GetValue(Parameter.ReportFormatParameter)); }
            set { ExecutionView.SetParameter(Parameter.ReportFormatParameter, value.ToString()); }
        }

        [XmlIgnore]
        public string ResultExtension
        {
            get
            {
                var format = Format;
                if (format == ReportFormat.csv) return "csv";
                if (format == ReportFormat.excel) return "xlsx";
                if (format == ReportFormat.pdf) return "htm"; //converter to pdf 
                return "htm";
            }
        }


        [XmlIgnore]
        //Indicates if we use the print layout
        public bool PrintLayout
        {
            get { return Format == ReportFormat.print || Format == ReportFormat.pdf; }
        }


        [XmlIgnore]
        public List<string> DrillParents = new List<string>();

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

        [XmlIgnore]
        public Object Tag;

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

        public void CancelExecution()
        {
            int cnt = 30;
            while (--cnt >= 0 && IsExecuting)
            {
                Cancel = true;
                Thread.Sleep(1000);
            }
        }

        //Log Interface implementation
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
                    //string are supposed to be thread-safe...
                    Debug.WriteLine(string.Format("{0} {1}\r\n", DateTime.Now.ToLongTimeString(), string.Format(message, args)));
                    ExecutionMessages += string.Format("{0} {1}\r\n", DateTime.Now.ToLongTimeString(), string.Format(message, args));
                }
            }
            catch (Exception ex)
            {
                ExecutionMessages += string.Format("Error logging {0}\r\n{1}\r\n", message, ex.Message);
            }
        }

        public ReportSource GetReportSource(string sourceName)
        {
            return _sources.FirstOrDefault(i => i.Name == sourceName);
        }

        public ReportModel GetReportModel(string modelName)
        {
            return _models.FirstOrDefault(i => i.Name == modelName);
        }

        #region Translation Helpers

        string TranslationFilePath
        {
            get
            {
                return FilePath.Replace(Repository.ReportsFolder, "").Replace(Repository.SubReportsFolder, string.Format("\\..\\{0}", Path.GetFileNameWithoutExtension(Repository.SubReportsFolder)));
            }
        }

        //Helpers for translations
        public string TranslateDisplayName(string displayName)
        {
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "ReportDisplayName", TranslationFilePath, displayName);
        }

        public string TranslateViewName(string viewName)
        {
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "ReportViewName", TranslationFilePath, viewName);
        }

        public string TranslateOutputName(string outputName)
        {
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "ReportOutputName", TranslationFilePath, outputName);
        }

        public string TranslateGeneral(string reference)
        {
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "ReportGeneral", TranslationFilePath, reference);
        }

        public string TranslateElement(ReportElement element, string reference)
        {
            if (string.IsNullOrEmpty(element.MetaColumnGUID)) return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "Element", element.DisplayNameEl, reference);
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "Element", element.MetaColumn.Category + '.' + element.DisplayNameEl, reference);
        }

        public string EnumDisplayValue(MetaEnum instance, string id, bool forRestriction = false)
        {
            string result = instance.GetDisplayValue(id, forRestriction);
            if (instance.Translate) result = Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "Enum", instance.Name, result);
            return result;
        }

        public string EnumMessage(MetaEnum instance)
        {
            if (!instance.HasFilters && !instance.HasDependencies) return "";
            return Repository.RepositoryTranslate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "EnumMessage", instance.Name, instance.Message);
        }
        #endregion

        #region Log

        static bool PurgeIsDone = false;
        public void LogExecution()
        {
            try
            {
                if (Repository.Configuration.LogDays <= 0) return;

                if (!Directory.Exists(Repository.LogsFolder)) Directory.CreateDirectory(Repository.LogsFolder);

                string logFileName = Path.Combine(Repository.LogsFolder, string.Format("log_{0:yyyy_MM_dd}.txt", DateTime.Now));
                var message = ExecutionMessages;
                if (string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(ExecutionErrors)) message = ExecutionErrors;
                if (!Cancel && !string.IsNullOrEmpty(ExecutionErrorStackTrace)) message += string.Format("\r\nError Stack Trace:\r\n{0}\r\n", ExecutionErrorStackTrace);
                string log = string.Format("********************\r\nExecution of '{0}' on {1} {2}\r\n{3}********************\r\n", FilePath, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), message);
                File.AppendAllText(logFileName, log);

                if (!PurgeIsDone)
                {
                    PurgeIsDone = true;
                    foreach (var file in Directory.GetFiles(Repository.LogsFolder, "log_*"))
                    {
                        //purge old files...
                        if (File.GetLastWriteTime(file).AddDays(Repository.Configuration.LogDays) < DateTime.Now)
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch { };
                        }
                    }
                }
            }
            catch { }
        }

        #endregion
    }
}
