//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.ComponentModel;
using RazorEngine;
using DynamicTypeDescriptor;
using Seal.Helpers;
using System.Drawing;
using System.Web;
using System.Windows.Forms;
using System.Globalization;
using Microsoft.Win32.TaskScheduler;
using System.Threading;
using System.Diagnostics;

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

        private List<ReportModel> _models = new List<ReportModel>();
        public List<ReportModel> Models
        {
            get { return _models; }
            set { _models = value; }
        }

        private List<ReportOutput> _outputs = new List<ReportOutput>();
        public List<ReportOutput> Outputs
        {
            get { return _outputs; }
            set { _outputs = value; }
        }

        private List<ReportTask> _taks = new List<ReportTask>();
        public List<ReportTask> Tasks
        {
            get { return _taks; }
            set { _taks = value; }
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
                        _displayNameEx = Helper.ParseRazor(_displayName, this);
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

        [XmlIgnore]
        public Repository Repository = null;

        [XmlIgnore]
        public string FilePath;

        [XmlIgnore]
        public DateTime LastModification;

        [XmlIgnore]
        public const string ResultFileStaticSuffix = "_seal_attach";

        public static bool IsSealAttachedFile(string path)
        {
            return Path.GetFileNameWithoutExtension(path).EndsWith(ResultFileStaticSuffix);
        }

        public static bool IsSealReportFile(string path)
        {
            return path.EndsWith("." + Repository.SealReportFileExtension);
        }

        public static string CopySealFile(string path, string destinationFolder)
        {
            string newPath = FileHelper.GetUniqueFileName(Path.Combine(destinationFolder, Path.GetFileName(path)));
            File.Copy(path, newPath, true);
            File.SetLastWriteTimeUtc(path, DateTime.Now);
            foreach (string attachedPath in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "*" + ResultFileStaticSuffix + ".*"))
            {
                File.Copy(attachedPath, Path.Combine(destinationFolder, Path.GetFileName(attachedPath)), true);
                File.SetLastWriteTimeUtc(attachedPath, DateTime.Now);
            }
            return newPath;
        }

        [XmlIgnore]
        public string ResultFilePrefix;

        [XmlIgnore]
        public string ResultFilePath;

        [XmlIgnore]
        public string DisplayResultFilePath
        {
            get
            {
                return ResultFilePath.Replace(Repository.ReportsFolder, "");
            }
        }

        string _HTMLDisplayFilePath = "";
        [XmlIgnore]
        public string HTMLDisplayFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_HTMLDisplayFilePath)) _HTMLDisplayFilePath = FileHelper.GetUniqueFileName(Path.Combine(DisplayFolder, "result.htm"));
                return _HTMLDisplayFilePath;
            }
            set { _HTMLDisplayFilePath = value; }
        }

        [XmlIgnore]
        public string ResultFolder
        {
            get
            {
                string result = (ExecutionContext == ReportExecutionContext.WebReport ? Repository.WebPublishFolder : FileHelper.TempApplicationDirectory);

                if (ForOutput)
                {
                    if (OutputToExecute.Device is OutputFolderDevice && !ForPDFConversion && !ForExcelConversion)
                    {
                        result = OutputFolderDeviceResultFolder;
                    }
                }
                return result;
            }
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
        public string WebTempUrl
        {
            get { return WebUrl + "temp/"; }
        }

        [XmlIgnore]
        public string WebExecutionGUID = "";

        [XmlIgnore]
        public string DisplayFolder
        {
            get
            {
                string result = FileHelper.TempApplicationDirectory;
                if (ExecutionContext == ReportExecutionContext.WebOutput || ExecutionContext == ReportExecutionContext.WebReport)
                {
                    result = Repository.WebPublishFolder;
                }
                return result;
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
                        fileName = OutputToExecute.FileName;
                        try
                        {
                            fileName = string.Format(OutputToExecute.FileName, DateTime.Now);
                        }
                        catch { }
                    }
                }
                if (string.IsNullOrEmpty(fileName)) fileName = "result";
                fileName = Helper.CleanFileName(fileName) + ".htm";
                if (ExecutionContext == ReportExecutionContext.WebReport)
                {
                    //add salt to the file name for web security...
                    string salt = Path.GetRandomFileName().Replace(".", "").Substring(0, 5);
                    fileName = Path.GetFileNameWithoutExtension(fileName) + "_" + salt + Path.GetExtension(fileName);
                }

                if (!ForOutput) fileName = fileName.Replace(" ", "_");
                return fileName;
            }
        }

        public void InitForExecution()
        {
            string fileName = "", fileFolder = "";
            if (ForOutput)
            {
                //Check custom Output Parameters and CSS
                Parameter.CopyParameters(OutputToExecute.ViewParameters, OutputToExecute.View.Parameters);
                Parameter.CopyParameters(OutputToExecute.ViewCSS, OutputToExecute.View.CSS);
            }

            try
            {
                var template = ExecutionView.Template; //This force to init parameters
                fileName = ResultFileName;
                fileFolder = ResultFolder;
                if (ForOutput && OutputToExecute.Device is OutputFolderDevice)
                {
                    //For folder output, we do not need a unique file name
                    ResultFilePath = Path.Combine(ResultFolder, Path.GetFileNameWithoutExtension(fileName)) + (!HasExternalViewer || string.IsNullOrEmpty(ExecutionView.ExternalViewerExtension) ? Path.GetExtension(fileName) : "." + ExecutionView.ExternalViewerExtension);
                }
                else
                {
                    //get unique file name in the result folder
                    ResultFilePath = FileHelper.GetUniqueFileName(Path.Combine(ResultFolder, fileName), "." + ExecutionView.ExternalViewerExtension);
                }
                ResultFilePrefix = Path.GetFileNameWithoutExtension(ResultFilePath) + "_" + Path.GetExtension(ResultFilePath).Replace(".", "");
                if (Repository.Configuration.IsLocal && !Directory.Exists(Path.Combine(ResultFolder, "images")) && !ExecutionView.Views.Exists(i => i.Template.Name == ReportViewTemplate.ModelCSVExcelName))
                {
                    try
                    {
                        //copy images folder (for jquery images)
                        FileHelper.CopyDirectory(Path.Combine(Repository.ViewScriptsFolder, "images"), Path.Combine(ResultFolder, "images"), false);
                    }
                    catch { }
                }

                //Display path is always an HTML one...
                HTMLDisplayFilePath = FileHelper.GetUniqueFileName(Path.Combine(DisplayFolder, ResultFilePrefix + ".htm"));

                //Clear some cache values...
                _displayNameEx = null;

            }
            catch (Exception ex)
            {
                Cancel = true;
                if (string.IsNullOrEmpty(fileFolder) && OutputToExecute != null && !string.IsNullOrEmpty(OutputToExecute.FolderPath)) fileFolder = OutputToExecute.FolderPath;
                ExecutionErrors += string.Format("Error initializing report Path, check your report execution or output Path '{0}'\r\n{1}\r\n", Path.Combine(fileFolder, fileName), ex.Message);
            }
        }


        [XmlIgnore]
        public bool HasValidationErrors = false;
        [XmlIgnore]
        public string ExecutionMessages;
        [XmlIgnore]
        public string ExecutionErrors;
        [XmlIgnore]
        public string WebExecutionErrors
        {
            get
            {
                return !string.IsNullOrEmpty(WebUrl) && !HasValidationErrors ? Translate("This report has execution errors. Please check details in the Windows Event Viewer...") : ExecutionErrors;
            }
        }
        [XmlIgnore]
        public string LoadErrors = "";

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
        public string ExecutionHeader
        {
            get 
            {
                TimeSpan duration = DateTime.Now - ExecutionStartDate;
                string message = "";
                if (duration.Hours > 0) message = string.Format("{0:00}:", Convert.ToInt32(duration.TotalHours));
                message += string.Format("{0:00}:{1:00} {2}", duration.Minutes, duration.Seconds, Cancel ? Translate("Cancelling report...") : Translate("Executing report..."));
                return message;
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
        public List<string> ExecutionAttachedFiles = new List<string>();

        //Output management
        [XmlIgnore]
        public ReportOutput OutputToExecute = null;
        [XmlIgnore]
        public bool ForOutput
        {
            get { return OutputToExecute != null; }
        }

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
                if (result == null) result = Views.FirstOrDefault();
                return result;
            }
        }

        [XmlIgnore]
        public TimeSpan ExecutionFullDuration
        {
            get { return (ExecutionEndDate - ExecutionStartDate); }
        }

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
        public bool HasNVD3Chart
        {
            get { return Models.Exists(i => i.HasNVD3Serie); }
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
                StreamReader sr = new StreamReader(path);
                XmlSerializer serializer = new XmlSerializer(typeof(Report));
                result = (Report)serializer.Deserialize(sr);
                result.FilePath = path;
                result.Repository = repository;
                sr.Close();
                result.LastModification = File.GetLastWriteTime(path);

                result.LoadErrors = "";
                foreach (ReportSource source in result.Sources)
                {
                    source.Report = result;
                    source.LoadRepositoryMetaSources(repository);
                }
                result.InitReferences();

                //Refresh enums
                foreach (ReportSource source in result.Sources) source.RefreshEnumsOnDbConnection();

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to read the file '{0}'.\r\n{1}", path, ex.Message));
            }
            return result;
        }

        static public Report Create(Repository repository)
        {
            Report result = new Report() { GUID = Guid.NewGuid().ToString() };
            result.FilePath = "NewReport." + Repository.SealReportFileExtension;
            result.Repository = repository;
            foreach (MetaSource source in repository.Sources)
            {
                ReportSource reportSource = result.AddSource(source);
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

            //and a first model
            if (result.Models.Count == 0) result.AddModel();
            //Add default views
            ReportView defaultView = result.AddModelHTMLView();
            result.ViewGUID = defaultView.GUID;
            result.AddModelCSVView();

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
                //Synchronze all schedules...
                foreach (ReportSchedule schedule in Schedules)
                {
                    schedule.SynchronizeTask();
                }

                //Clear unused tasks
                foreach (Task task in TaskFolder.GetTasks().Where(i => i.Definition.RegistrationInfo.Source.StartsWith(FilePath + "\n")))
                {
                    ReportSchedule schedule = Schedules.FirstOrDefault(i => i.TaskSource == task.Definition.RegistrationInfo.Source);
                    if (schedule == null)
                    {
                        TaskFolder.DeleteTask(task.Name);
                    }
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
            Serialize(path);
            FilePath = path;
            LastModification = File.GetLastWriteTime(path);

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
                StreamWriter sw = new StreamWriter(path);
                serializer.Serialize(sw, this);
                sw.Close();
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

        public ReportModel AddModel()
        {
            if (Sources.Count == 0) throw new Exception("Unable to create a model: No source available.\r\nPlease create a source first.");
            ReportModel result = ReportModel.Create();
            result.Name = Helper.GetUniqueName("model", (from i in Models select i.Name).ToList());
            ReportSource source = Sources.FirstOrDefault(i => i.IsDefault);
            if (source == null) source = Sources[0];
            result.SourceGUID = source.GUID;
            result.Report = this;
            Models.Add(result);
            return result;
        }


        void checkRemoveModel(ReportView view, ReportModel model)
        {
            foreach (var childView in view.Views)
            {
                if (view.Views.Exists(i => i.ModelGUID == model.GUID)) throw new Exception(string.Format("The model '{0}' is already used by a view.", model.Name));
                checkRemoveModel(childView, model);
            }
        }

        public void RemoveModel(ReportModel model)
        {
            foreach (ReportView view in Views)
            {
                checkRemoveModel(view, model);
            }
            if (Models.Count == 1) throw new Exception("Unable to remove the model: The report must contain at least one Model.");

            Models.Remove(model);
        }

        public ReportOutput AddOutput(OutputDevice device)
        {
            ReportOutput result = ReportOutput.Create();
            result.Name = Helper.GetUniqueName(string.Format("output ({0})", device.Name), (from i in Outputs select i.Name).ToList());
            if (device is OutputFolderDevice)
            {
                result.FolderPath = Repository.SealRepositoryKeyword + "\\Reports\\";
                result.FileName = FileHelper.CleanFilePath(DisplayNameEx);
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
            return AddView(ReportViewTemplate.ModelHTMLName);
        }

        public ReportView AddModelCSVView()
        {
            return AddView(ReportViewTemplate.ModelCSVExcelName);
        }

        public ReportView AddRootView()
        {
            ReportViewTemplate reportTemplate = Repository.ViewTemplates.FirstOrDefault(i => i.ParentNames.Count == 0 && i.Name == "Report");
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

        public ReportView AddView(string modelName)
        {
            ReportViewTemplate modelTemplate = Repository.ViewTemplates.FirstOrDefault(i => i.Name == modelName);
            ReportView view = AddRootView();
            if (view != null && modelTemplate != null)
            {
                view.Name = Helper.GetUniqueName("view" + (modelName == ReportViewTemplate.ModelCSVExcelName ? " Excel" : ""), (from i in Views select i.Name).ToList());
                AddChildView(view, modelTemplate);
            }
            return view;
        }

        public ReportView AddChildView(ReportView parent, ReportViewTemplate template)
        {
            if (Models.Count == 0) throw new Exception("Unable to create a view: No model available.\r\nPlease create a model first.");

            ReportView result = ReportView.Create(template);
            result.Name = Helper.GetUniqueName(template.Name + " View", (from i in parent.Views select i.Name).ToList());
            result.Report = this;
            result.InitReferences();
            result.SortOrder = parent.Views.Count > 0 ? parent.Views.Max(i => i.SortOrder) + 1 : 1;
            if (template.ForModel) result.ModelGUID = Models[0].GUID;
            parent.Views.Add(result);
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
            return "file:///" + HttpUtility.UrlEncode(result).Replace("+", "%20");
        }


        public string GetChartFileName()
        {
            return FileHelper.GetUniqueFileName(Path.Combine(ResultFolder, ResultFilePrefix + Guid.NewGuid().ToString() + ResultFileStaticSuffix + ".png"));
        }

        public string AttachImageFile(string fileName)
        {
            if (ExecutionContext == ReportExecutionContext.WebReport || ExecutionContext == ReportExecutionContext.WebOutput)
            {
                return string.Format("{0}Images/{1}", WebUrl, fileName);
            }

            if (GenerateHTMLDisplay || SkipImageAttachment || IsBasicHTMLWithNoOutput || ForPDFConversion)
            {
                //Rendering the display, we return full path with file:///
                return GetImageFile(fileName);
            }

            //generating result file
            string sourceFilePath = Path.Combine(Repository.ViewImagesFolder, fileName);
            if (ForOutput)
            {
                //execution to output, rename the file and copy them in the target directory
                string targetFilePath = FileHelper.GetUniqueFileName(Path.Combine(ResultFolder, ResultFilePrefix + Path.GetFileNameWithoutExtension(fileName) + ResultFileStaticSuffix + Path.GetExtension(fileName)));
                File.Copy(sourceFilePath, targetFilePath);
                sourceFilePath = targetFilePath;
            }

            ExecutionAttachedFiles.Add(sourceFilePath);
            return Path.GetFileName(sourceFilePath);
        }

        public string AttachScriptFile(string fileName, string cdnPath = "")
        {
            if (!string.IsNullOrEmpty(cdnPath) && !Repository.Configuration.IsLocal) return string.Format("<script type='text/javascript' src='{0}'></script>", cdnPath);

            if (GenerateHTMLDisplay || ForPDFConversion)
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
            string sourceFilePath = Path.Combine(Repository.ViewScriptsFolder, fileName);
            result += File.ReadAllText(sourceFilePath);
            result += "\r\n</script>\r\n";
            return result;
        }

        public string AttachCSSFile(string fileName, string cdnPath = "")
        {
            if (!string.IsNullOrEmpty(cdnPath) && !Repository.Configuration.IsLocal) return string.Format("<link type='text/css' href='{0}' rel='stylesheet'/>", cdnPath);

            if (GenerateHTMLDisplay)
            {
                if (ExecutionContext == ReportExecutionContext.WebReport || ExecutionContext == ReportExecutionContext.WebOutput)
                {
                    return string.Format("<link type='text/css' href='../Scripts/{0}' rel='stylesheet'/>", fileName);
                }
                else
                {
                    //reference local file
                    string fileReference = "file:///" + HttpUtility.HtmlEncode(Path.Combine(Repository.ViewScriptsFolder, fileName));
                    return string.Format("<link type='text/css' href='{0}' rel='stylesheet'/>", fileReference);
                }
            }

            //generating result file, set the CSS directly in the result
            string result = "<style type='text/css'>\r\n";
            string sourceFilePath = Path.Combine(Repository.ViewScriptsFolder, fileName);
            result += File.ReadAllText(sourceFilePath);
            result += "\r\n</style>\r\n";
            return result;
        }

        [XmlIgnore]
        public int NumberOfRestrictionValuesToShow
        {
            get
            {
                int result = 1;
                foreach (ReportRestriction restriction in ExecutionCommonRestrictions)
                {
                    if (restriction.IsContainOperator || restriction.Operator == Operator.Equal || restriction.Operator == Operator.NotEqual)
                    {
                        if (restriction.HasValue4) result = 4;
                        else if (restriction.HasValue3) result = 3;
                        else if (restriction.HasValue2) result = 2;
                    }
                }
                return result;
            }
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
                    foreach (ReportModel model in ExecutionModels)
                    {
                        foreach (ReportRestriction restriction in AllExecutionRestrictions.Where(i => i.Prompt == PromptType.Prompt))
                        {
                            if (!_executionCommonRestrictions.Exists(i => i.MetaColumnGUID == restriction.MetaColumnGUID && i.DisplayNameEl == restriction.DisplayNameEl))
                            {
                                restriction.HtmlIndex = index.ToString();
                                _executionCommonRestrictions.Add(restriction);
                                index++;
                            }
                        }
                    }

                    //Set index in all models
                    foreach (ReportModel model in ExecutionModels)
                    {
                        foreach (ReportRestriction restriction in _executionCommonRestrictions)
                        {

                            ReportRestriction modelRestriction = model.Restrictions.FirstOrDefault(i => i.MetaColumnGUID == restriction.MetaColumnGUID && i.DisplayNameEl == restriction.DisplayNameEl);
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

        public bool HasOnlyRestrictionsWithNoOperator
        {
            get {
                return !ExecutionCommonRestrictions.Exists(i => i.IsEnumRE && i.HasOperator);
            }
        }

        [XmlIgnore]
        public List<ReportRestriction> AllRestrictions
        {
            get
            {
                List<ReportRestriction> result = new List<ReportRestriction>();
                foreach (ReportModel model in Models)
                {
                    foreach (ReportRestriction restriction in model.Restrictions) result.Add(restriction);
                    foreach (ReportRestriction restriction in model.AggregateRestrictions) result.Add(restriction);
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
                foreach (ReportModel model in ExecutionModels)
                {
                    foreach (ReportRestriction restriction in model.ExecutionRestrictions) result.Add(restriction);
                    foreach (ReportRestriction restriction in model.ExecutionAggregateRestrictions) result.Add(restriction);
                }
                return result;
            }
        }


        void GetModelsToExecute(ReportView view, List<ReportModel> result)
        {
            if (view.Model != null && !result.Contains(view.Model)) result.Add(view.Model);
            foreach (var child in view.Views) GetModelsToExecute(child, result);
        }


        [XmlIgnore]
        public List<ReportModel> ExecutionModels
        {
            get
            {
                List<ReportModel> result = new List<ReportModel>();
                GetModelsToExecute(ExecutionView, result);
                return result;
            }
        }

        [XmlIgnore]
        public ReportView View; //Current view used when parsing

        //Translations
        public string Translate(string reference)
        {
            if (ExecutionView == null) return reference;
            return Repository.Translate(ExecutionView.CultureInfo.TwoLetterISOLanguageName, "Report", reference);
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

        public string TranslateRestriction(ReportRestriction restriction, string value)
        {
            string result = value;
            if (restriction.IsDateTime && ReportRestriction.HasDateKeyword(value))
            {
                result = TranslateDateKeywords(result);
            }
            return result;
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
                return !string.IsNullOrEmpty(ExecutionView.ExternalViewerExtension) || ExecutionView.GetBoolValue(Parameter.PDFLayoutParameter) || ExecutionView.GetBoolValue(Parameter.ExcelLayoutParameter);
            }
        }

        [XmlIgnore]
        public bool ForPDFConversion
        {
            get
            {
                return ExecutionView.AllowPDFConversion && ExecutionView.GetBoolValue(Parameter.PDFLayoutParameter);
            }
        }

        [XmlIgnore]
        public bool ForExcelConversion
        {
            get
            {
                return ExecutionView.GetBoolValue(Parameter.ExcelLayoutParameter);
            }
        }

        [XmlIgnore]
        public bool SkipImageAttachment
        {
            get
            {
                return ExecutionView.Views.Exists(i => i.Template.SkipFileAttachments);
            }
        }

        [XmlIgnore]
        public Parameter PrintLayoutParameter
        {
            //Print layout option is only valid for HTML or PDF generation
            get { return !HasExternalViewer || ExecutionView.GetBoolValue(Parameter.PDFLayoutParameter) ? ExecutionView.Parameters.FirstOrDefault(i => i.Name == Parameter.PrintLayoutParameter) : null; }
        }

        [XmlIgnore]
        //Indicates if we use the print layout
        public bool PrintLayout
        {
            get { return PrintLayoutParameter == null ? false : PrintLayoutParameter.BoolValue; }
        }

        [XmlIgnore]
        //Indicates if we can use a fixed html header
        public bool UseFixedHTMLHeader
        {
            get { return (!PrintLayout && !ForPDFConversion) || GenerateHTMLDisplay; }
        }

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
                //string are supposed to be thread-safe...
                ExecutionMessages +=  string.Format("{0} {1}\r\n", DateTime.Now.ToLongTimeString(), string.Format(message, args));
            }
            catch (Exception ex)
            {
                ExecutionMessages += string.Format("Error logging {0}\r\n{1}\r\n", message, ex.Message);
            }
        }

        //Helpers for translations
        public string TranslateDisplayName(string displayName)
        {
            if (FilePath.Length < Repository.ReportsFolder.Length) return displayName;
            return Repository.RepositoryTranslate("ReportDisplayName", FilePath.Substring(Repository.ReportsFolder.Length), displayName);
        }

        public string TranslateViewName(string viewName)
        {
            if (FilePath.Length < Repository.ReportsFolder.Length) return viewName;
            return Repository.RepositoryTranslate("ReportViewName", FilePath.Substring(Repository.ReportsFolder.Length), viewName);
        }

        public string TranslateOutputName(string outputName)
        {
            if (FilePath.Length < Repository.ReportsFolder.Length) return outputName;
            return Repository.RepositoryTranslate("ReportOutputName", FilePath.Substring(Repository.ReportsFolder.Length), outputName);
        }

        public string TranslateGeneral(string reference)
        {
            if (FilePath.Length < Repository.ReportsFolder.Length) return reference;
            return Repository.RepositoryTranslate("ReportGeneral", FilePath.Substring(Repository.ReportsFolder.Length), reference);
        }
    }
}
