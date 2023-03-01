//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Helpers;
using System.Data.Common;
using System.Threading;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Data;
#if WINDOWS
using DynamicTypeDescriptor;
using System.Drawing.Design;
using Seal.Forms;
#endif

namespace Seal.Model
{
    /// <summary>
    /// A ReportTask defines the exection of a task: SQL statement or Razor script
    /// </summary>
    public class ReportTask : ReportComponent
#if WINDOWS
        , ITreeSort
#endif
    {
        public const string ParentTaskConnectionGUID = "5";
        public const string ExecInputKeyword = "%EXECINPUT%";
        public const string ParentExecResultKeyword = "%PARENTEXECRESULT%";
        public const string TranslatedParameterDescription = " The parameter can contain the '%PARENTEXECRESULT%' keyword to specify the result of the parent task, '%EXECINPUT% for an optional input set in the task (e.g. used in Loop).";
        public const string TranslatedParameterDescriptionFull = " The parameter can contain the '%SEALREPOSITORY%' keyword to specify the repository path, '%PARENTEXECRESULT%' for the result of the parent task, '%EXECINPUT% for an optional input set in the task (e.g. used in Loop).";

#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                InitParameters();

                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("SourceGUID").SetIsBrowsable(true);
                GetProperty("ConnectionGUID").SetIsBrowsable(true);
                GetProperty("TemplateName").SetIsBrowsable(true);
                GetProperty("Description").SetIsBrowsable(true);

                GetProperty("ParameterValues").SetIsBrowsable(Parameters.Count > 0);
                GetProperty("SQL").SetIsBrowsable(true);
                GetProperty("SQLSeparator").SetIsBrowsable(true);
                GetProperty("Enabled").SetIsBrowsable(true);
                GetProperty("IgnoreError").SetIsBrowsable(true);
                GetProperty("Retries").SetIsBrowsable(true);
                GetProperty("RetryDuration").SetIsBrowsable(true);
                GetProperty("BodyScript").SetIsBrowsable(true);
                GetProperty("Script").SetIsBrowsable(true);
                GetProperty("ExecuteForEachConnection").SetIsBrowsable(true);
                GetProperty("Step").SetIsBrowsable(ParentTask == null);

                //Helpers
                //GetProperty("Information").SetIsBrowsable(true);
                //GetProperty("Error").SetIsBrowsable(true);

                //Readonly
                GetProperty("TemplateName").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

#endif

        /// <summary>
        /// Creates a basic ReportTask
        /// </summary>
        public static ReportTask Create()
        {
            return new ReportTask() { GUID = Guid.NewGuid().ToString() };
        }


        /// <summary>
        /// Init all references of the task
        /// </summary>
        public void InitReferences()
        {
            if (string.IsNullOrEmpty(TemplateName)) TemplateName = ReportTaskTemplate.DefaultName;
            foreach (var child in Tasks)
            {
                child.Report = Report;
                child.ParentTask = this;
                child.InitReferences();
            }
        }

        /// <summary>
        /// The Razor Script used to execute the task
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Template name"), Description("The Template used to define the task."), Id(1, 1)]
#endif
        public string TemplateName { get; set; }

        private ReportTaskTemplate _taskTemplate = null;
        [XmlIgnore]
        public ReportTaskTemplate TaskTemplate
        {
            get
            {
                if (_taskTemplate == null)
                {
                    if (!string.IsNullOrEmpty(TemplateName)) _taskTemplate = RepositoryServer.TaskTemplates.FirstOrDefault(i => i.Name == TemplateName);
                    if (_taskTemplate == null) _taskTemplate = RepositoryServer.TaskTemplates.FirstOrDefault(i => i.Name == ReportTaskTemplate.DefaultName);

                    InitParameters();
                }
                return _taskTemplate;
            }
        }

        /// <summary>
        /// Description coming from the template
        /// </summary>
        [XmlIgnore]
#if WINDOWS
        [Category("Definition"), DisplayName("Template description"), Description("Description coming from the template."), Id(2, 1)]
#endif
        public string Description
        {
            get
            {
                string result = TaskTemplate.Description;
                return result ?? TemplateDescription;
            }
        }

        public bool ShouldSerializeViews() { return Tasks.Count > 0; }

        /// <summary>
        /// Current parent task if any
        /// </summary>
        [XmlIgnore]
        public ReportTask ParentTask { get; set; } = null;

        /// <summary>
        /// Children of the task
        /// </summary>
        public List<ReportTask> Tasks = new List<ReportTask>();

        /// <summary>
        /// List of Table Parameters
        /// </summary>
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        public bool ShouldSerializeParameters() { return Parameters.Count > 0; }

        /// <summary>
        /// Init the  parameters from the template
        /// </summary>
        public void InitParameters()
        {
            if (TaskTemplate != null)
            {
                lock (this)
                {
                    var initialParameters = Parameters.ToList();
                    Parameters.Clear();

                    var defaultParameters = TaskTemplate.DefaultParameters;
                    foreach (var configParameter in defaultParameters)
                    {
                        Parameter parameter = initialParameters.FirstOrDefault(i => i.Name == configParameter.Name);
                        if (parameter == null) parameter = new Parameter() { Name = configParameter.Name, Value = configParameter.Value };
                        Parameters.Add(parameter);
                        parameter.InitFromConfiguration(configParameter);
                    }
                    //Show Error if any
                    if (!string.IsNullOrEmpty(TaskTemplate.Error)) Error = TaskTemplate.Error;

                    if (string.IsNullOrEmpty(_name)) _name = "Master"; //Force a name for backward compatibility
                }
            }
        }

#if WINDOWS
        /// <summary>
        /// The parameter values for edition.
        /// </summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("Task parameters"), Description("The task parameter values."), Category("Definition"), Id(10, 1)]
        [XmlIgnore]
        public ParametersEditor ParameterValues
        {
            get
            {
                var editor = new ParametersEditor();
                editor.Init(Parameters);
                return editor;
            }
        }
#endif

        //Temporary variables to help for report serialization...
        private List<Parameter> _tempParameters;

        /// <summary>
        /// Operations performed before the serialization
        /// </summary>
        public void BeforeSerialization()
        {
            InitParameters();
            _tempParameters = Parameters.ToList();
            //Remove parameters identical to config
            Parameters.RemoveAll(i => i.Value == null || i.Value == i.ConfigValue);

            if (Script != null && Script.Trim().Replace("\r\n", "\n") == DefaultScript.Trim().Replace("\r\n", "\n")) Script = null;
            if (BodyScript != null && BodyScript.Trim().Replace("\r\n", "\n") == DefaultBodyScript.Trim().Replace("\r\n", "\n")) BodyScript = null;

            foreach (var task in Tasks) task.BeforeSerialization();
        }

        /// <summary>
        /// Operations performed after the serialization
        /// </summary>
        public void AfterSerialization()
        {
            Parameters = _tempParameters;
            foreach (var task in Tasks) task.AfterSerialization();
        }

        /// <summary>
        /// Returns the parameter value
        /// </summary>
        public string GetValue(string name)
        {
            Parameter parameter = Parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? "" : (string.IsNullOrEmpty(parameter.Value) ? parameter.ConfigValue : parameter.Value);
        }

        /// <summary>
        /// Returns the parameter value replacing Exec and Repository keywords
        /// </summary>
        public string GetValueTranslated(string name)
        {
            var result = GetValue(name);
            if (!string.IsNullOrEmpty(result))
            {
                var execInput = "";
                if (ExecInput is string) execInput = ExecInput as string;
                else if (ExecInput is DataRow) execInput = ((DataRow)ExecInput)[0].ToString();
                result = result.Replace(ExecInputKeyword, execInput);

                var parentExecResult = "";
                if (ParentTask?.ExecResult is string) parentExecResult = ParentTask?.ExecResult as string;
                else if (ParentTask?.ExecResult is DataRow) parentExecResult = ((DataRow)ParentTask?.ExecResult)[0].ToString();
                result= result.Replace(ParentExecResultKeyword, parentExecResult);

                result = Repository.ReplaceRepositoryKeyword(result);
            }
            return result;
        }

        /// <summary>
        /// Returns a parameter boolean value with a default if it does not exist
        /// </summary>
        public bool GetBoolValue(string name, bool defaultValue)
        {
            Parameter parameter = Parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? defaultValue : parameter.BoolValue;
        }

        /// <summary>
        /// Returns a paramter ineteger value
        /// </summary>
        public int GetNumericValue(string name)
        {
            Parameter parameter = Parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? 0 : parameter.NumericValue;
        }

        /// <summary>
        /// Identifier of the current report source
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [Category("Definition"), DisplayName("Source"), Description("The source used by the task."), Id(3, 1)]
        [TypeConverter(typeof(MetaSourceConverter))]
#endif
        public string SourceGUID { get; set; }

        protected string _connectionGUID = ReportSource.DefaultReportConnectionGUID;

        /// <summary>
        /// The connection identifier used by the task
        /// </summary>
#if WINDOWS
        [DefaultValue(ReportSource.DefaultReportConnectionGUID)]
        [DisplayName("Connection"), Description("The connection used by the task."), Category("Definition"), Id(4, 1)]
        [TypeConverter(typeof(SourceConnectionConverter))]
#endif
        public string ConnectionGUID
        {
            get
            {
                if (_connectionGUID != ReportSource.DefaultReportConnectionGUID
                    && _connectionGUID != ReportSource.DefaultRepositoryConnectionGUID
                    && !(_connectionGUID == ParentTaskConnectionGUID && ParentTask != null))
                {
                    //reset it if not found in current connections
                    if (Source != null && !Source.Connections.Exists(i => i.GUID == _connectionGUID) && !Source.TempConnections.Exists(i => i.GUID == _connectionGUID)) _connectionGUID = ReportSource.DefaultReportConnectionGUID;
                }
                return _connectionGUID;
            }
            set { _connectionGUID = value; }
        }

        /// <summary>
        /// If false, the task is ignored and not executed
        /// </summary>
#if WINDOWS
        [DefaultValue(true)]
        [Category("Definition"), DisplayName("Is enabled"), Description("If false, the task is ignored and not executed."), Id(5, 1)]
#endif
        public bool Enabled { get; set; } = true;
        public bool ShouldSerializeEnabled() { return !Enabled; }

        /// <summary>
        /// The Report Execution Step to execute the task. By default, tasks are executed before the models generation.
        /// </summary>
#if WINDOWS
        [DefaultValue(ExecutionStep.BeforeModel)]
        [Category("Definition"), DisplayName("Execution step"), Description("The Report Execution Step to execute the task. By default, tasks are executed before the models generation."), Id(4, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
#endif
        public ExecutionStep Step { get; set; } = ExecutionStep.BeforeModel;

        /// <summary>
        /// Current MetaConnection
        /// </summary>
        [XmlIgnore]
        public MetaConnection Connection
        {
            get
            {
                MetaConnection result = Source.Connection;
                if (_connectionGUID == ReportSource.DefaultReportConnectionGUID) result = Source.Connection;
                else if (_connectionGUID == ReportSource.DefaultRepositoryConnectionGUID) result = Source.RepositoryConnection;
                else if (_connectionGUID == ParentTaskConnectionGUID && ParentTask != null) result = ParentTask.Connection;
                else result = Source.Connections.FirstOrDefault(i => i.GUID == _connectionGUID);
                if (result == null && Source.Connections.Count > 0)
                {
                    result = Source.Connections[0];
                }
                return result;
            }
        }

        /// <summary>
        /// Current ReportSource
        /// </summary>
        [XmlIgnore]
        public ReportSource Source
        {
            get
            {
                ReportSource result = _report.Sources.FirstOrDefault(i => i.GUID == SourceGUID);
                if (result == null)
                {
                    if (_report.Sources.Count == 0) throw new Exception("This report has no source defined");
                    result = _report.Sources[0];
                    SourceGUID = result.GUID;
                }
                return result;
            }
        }

        /// <summary>
        /// Current Repository
        /// </summary>
        [XmlIgnore]
        public Repository Repository
        {
            get
            {
                if (_report != null) return _report.Repository;
                return null;
            }
        }

        /// <summary>
        /// Body Razor script executed for the Task.
        /// </summary>
#if WINDOWS
        [Category("Options"), DisplayName("Body script"), Description("Body Razor script executed for the Task."), Id(1, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string BodyScript { get; set; }

        public const string BodyScriptTemplate = @"@{
    ReportTask task = Model;

    //Execute SQL
    task.ExecuteSQL();
    
    //Execute Script
    task.ExecuteScript();

    //Execute children
    if (task.ExecProcessChildren) {
        foreach (var childTask in task.Tasks.OrderBy(i => i.SortOrder))
        {
            childTask.Execute();
        }
    }
}
";

        public const string NoChildrenBodyScriptTemplate = @"@{
    ReportTask task = Model;

    //Execute SQL
    task.ExecuteSQL();
    
    //Execute Script
    task.ExecuteScript();
}
";

        /// <summary>
        /// Description coming from the template
        /// </summary>
        [XmlIgnore]
        public string TemplateDescription;

        /// <summary>
        /// Default script coming from the template
        /// </summary>
        [XmlIgnore]
        public string DefaultScript
        {
            get
            {
                string result = TaskTemplate?.DefaultScript;
                return result ?? "";
            }
        }

        /// <summary>
        /// Default body script coming from the template
        /// </summary>
        [XmlIgnore]
        public string DefaultBodyScript
        {
            get
            {
                string result = TaskTemplate?.DefaultBodyScript;
                return result ?? "";
            }
        }

        /// <summary>
        /// SQL Statement executed for the task. It may be empty if a Razor Script is defined. The statement may contain Razor script if it starts with '@'. If the SQL result returns 0, the report is cancelled and the next tasks are not executed.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("SQL statement"), Description("SQL Statement executed for the task. It may be empty if a Razor Script is defined. The statement may contain Razor script if it starts with '@'."), Id(5, 1)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
#endif
        public string SQL { get; set; }

        /// <summary>
        /// Separator used in the SQL Statement to split the script in several sub-scripts and executions (e.g. GO or ;). The SQL statement must contain the separator plus a carriage retrun line feed to be detected.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("SQL separator"), Description("Separator used in the SQL Statement to split the script in several sub-scripts (e.g. GO or ;). The SQL statement must contain the separator plus a line feed to be detected."), Id(6, 1)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
#endif
        public string SQLSeparator { get; set; }

        /// <summary>
        /// Razor script executed for the Task. It may be empty if the SQL Script is defined. If the script returns 0, the report is cancelled and the next tasks are not executed.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Script"), Description("Razor script executed for the Task. It may be empty if the SQL Script is defined."), Id(7, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string Script { get; set; }

        /// <summary>
        /// If true, errors occuring during the task execution are ignored and the report execution continues
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Options"), DisplayName("Ignore errors"), Description("If true, errors occuring during the task execution are ignored and the report execution continues."), Id(2, 2)]
#endif
        public bool IgnoreError { get; set; } = false;

        /// <summary>
        /// Number of retries in case of error
        /// </summary>
#if WINDOWS
        [DefaultValue(0)]
        [Category("Options"), DisplayName("Retries in case of error"), Description("Number of retries in case of error."), Id(3, 2)]
#endif
        public int Retries { get; set; } = 0;

        /// <summary>
        /// Duration in seconds to wait between each retry
        /// </summary>
#if WINDOWS
        [DefaultValue(60)]
        [Category("Options"), DisplayName("Retry delay in seconds"), Description("Number of seconds elapsed before retrying."), Id(4, 2)]
#endif
        public int RetryDuration { get; set; } = 60;

        /// <summary>
        /// If true, the task will be executed for each connection defined in the Data Source. If false, only the current connection is used.
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Options"), DisplayName("Execute for each connection"), Description("If true, the task will be executed for each connection defined in the Data Source. If false, only the current connection is used."), Id(5, 2)]
#endif
        public bool ExecuteForEachConnection { get; set; } = false;

        /// <summary>
        /// Order of the task amongst the tasks of the report
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Custom Tag the can be used at execution time to store any object
        /// </summary>
        [XmlIgnore]
        public object Tag;

        /// <summary>
        /// Returns the order of the task
        /// </summary>
        public int GetSort()
        {
            return SortOrder;
        }

        /// <summary>
        /// Current progression of the task in percentage
        /// </summary>
        [XmlIgnore]
        public int Progression = 0;

        /// <summary>
        /// The current report execution executing the task
        /// </summary>
        [XmlIgnore]
        public ReportExecution Execution;

        #region Helpers

        /// <summary>
        /// Last information message
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(20, 20)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string Information { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(20, 20)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string Error { get; set; }

        #endregion

        #region Database

        DbCommand _command = null;
        Mutex _commandMutex = new Mutex();

        /// <summary>
        /// Information message for database
        /// </summary>
        [XmlIgnore]
        public StringBuilder DbInfoMessage = new StringBuilder();

        /// <summary>
        /// Log Interface implementation
        /// </summary>
        public void LogMessage(string message, params object[] args)
        {
            Report.LogMessage(message, args);
        }

        /// <summary>
        /// Cancel the task
        /// </summary>
        public void Cancel()
        {
            LogMessage("Cancelling task and report...");
            Report.Cancel = true;
            if (_commandMutex.WaitOne(1000))
            {
                try
                {
                    if (_command != null)
                    {
                        _command.Cancel();
                    }
                }
                finally
                {
                    _commandMutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// Returns a DbCommand from a MetaConnection
        /// </summary>
        public DbCommand GetDbCommand(MetaConnection metaConnection)
        {
            DbCommand result = null;
            if (_commandMutex.WaitOne(1000))
            {
                try
                {
                    DbConnection connection = metaConnection.GetOpenConnection();
                    if (connection is OdbcConnection)
                    {
                        ((OdbcConnection)connection).InfoMessage += new OdbcInfoMessageEventHandler(OdbcInfoMessage);
                        result = ((OdbcConnection)connection).CreateCommand();
                    }
                    else if (connection is SqlConnection)
                    {
                        ((SqlConnection)connection).InfoMessage += new SqlInfoMessageEventHandler(SqlInfoMessage);
                        result = ((SqlConnection)connection).CreateCommand();
                    }
                    else if (connection is Microsoft.Data.SqlClient.SqlConnection)
                    {
                        ((Microsoft.Data.SqlClient.SqlConnection)connection).InfoMessage += new Microsoft.Data.SqlClient.SqlInfoMessageEventHandler(MicrosoftSqlInfoMessage);
                        result = ((Microsoft.Data.SqlClient.SqlConnection)connection).CreateCommand();
                    }
                    else if (connection is MySql.Data.MySqlClient.MySqlConnection)
                    {
                        ((MySql.Data.MySqlClient.MySqlConnection)connection).InfoMessage += new MySql.Data.MySqlClient.MySqlInfoMessageEventHandler(MySqlInfoMessage);
                        result = ((MySql.Data.MySqlClient.MySqlConnection)connection).CreateCommand();
                    }
                    else if (connection is OracleConnection)
                    {
                        ((OracleConnection)connection).InfoMessage += new OracleInfoMessageEventHandler(OracleInfoMessage);
                        result = ((OracleConnection)connection).CreateCommand();
                    }
                    else
                    {
                        ((OleDbConnection)connection).InfoMessage += new OleDbInfoMessageEventHandler(OleDbInfoMessage);
                        result = ((OleDbConnection)connection).CreateCommand();
                    }
                    result.CommandTimeout = 0;
                }
                finally
                {
                    _commandMutex.ReleaseMutex();
                }
            }
            else
            {
                throw new Exception("Unable to get task command mutex...");
            }
            return result;
        }

        /// <summary>
        /// Executes the task
        /// </summary>
        public void Execute()
        {
            if (!Enabled) return;

            var initialConnectionGUID = ConnectionGUID;

            int tries = Math.Max(0, Retries);
            while (tries >= 0)
            {
                try
                {
                    InitParameters();

                    DbInfoMessage = new StringBuilder();
                    //Temp list to avoid change of connections during a task...
                    var connections = Source.Connections.Where(i => ExecuteForEachConnection || i.GUID == Connection.GUID).ToList();
                    foreach (var connection in connections)
                    {
                        if (Report.Cancel) return;

                        ConnectionGUID = connection.GUID;
                        LogMessage($"Starting task '{Name}' with connection '{Connection.Name}'");
                        Progression = 0;

                        var bodyScript = string.IsNullOrEmpty(BodyScript) ? DefaultBodyScript : BodyScript;
                        RazorHelper.CompileExecute(bodyScript, this);

                        LogMessage("Ending task '{0}'", Name);
                        Progression = 100; //100%
                    }
                }
                catch (Exception ex)
                {
                    if (tries > 0)
                    {
                        var message = ex.Message + (ex.InnerException != null ? "\r\n" + ex.InnerException.Message : "");
                        if (string.IsNullOrEmpty(Report.WebUrl)) LogMessage("Error in task '{0}': {1}", Name, message);
                        else LogMessage("Error got in task '{0}'", Name);

                        int cnt = Math.Max(1, RetryDuration);
                        LogMessage($"Waiting for {cnt} seconds.");

                        while (--cnt >= 0 && !Report.Cancel) Thread.Sleep(1000);
                        LogMessage($"Retrying execution (try {Retries - tries + 1} of {Retries}).");
                    }
                    else
                    {
                        HandleException(ex);
                    }
                }
                finally
                {
                    ConnectionGUID = initialConnectionGUID;
                }
                tries--;
            }
        }
        /// <summary>
        /// List of SQL statements to execute
        /// </summary>
        [XmlIgnore]
        public List<string> SQLStatements 
        {
            get
            {
                var result = new List<string>();
                string finalSql = RazorHelper.CompileExecute(SQL, this);
                if (!string.IsNullOrEmpty(SQLSeparator)) result = finalSql.Replace("\r\n","\n").Split(SQLSeparator + "\n").ToList();
                else result.Add(finalSql);
                return result;
            }
        }

        public void ExecuteSQL()
        {
            if (!string.IsNullOrEmpty(SQL))
            {
                _command = GetDbCommand(Connection);
                try
                {
                    foreach( var sql in SQLStatements)
                    {
                        if (!string.IsNullOrWhiteSpace(sql))
                        {
                            _command.CommandText = sql;
                            _command.ExecuteScalar();
                        }
                    }
                }
                finally
                {
                    _command.Connection.Close();
                }
            }
        }

        public void ExecuteScript()
        {
            var script = string.IsNullOrEmpty(Script) ? DefaultScript : Script;
            if (!string.IsNullOrEmpty(script))
            {
                RazorHelper.CompileExecute(script, this);
            }
        }

        public void HandleException(Exception ex)
        {
            var message = ex.Message + (ex.InnerException != null ? "\r\n" + ex.InnerException.Message : "");
            if (string.IsNullOrEmpty(Report.WebUrl)) LogMessage("Error in task '{0}': {1}\r\n{2}", Name, message, ex.StackTrace);
            else LogMessage("Error got in task '{0}'", Name);
            if (!IgnoreError)
            {
                Report.ExecutionErrors = message;
                Report.ExecutionErrorStackTrace = ex.StackTrace;
                Cancel();
            }
        }

        void OleDbInfoMessage(object sender, OleDbInfoMessageEventArgs e)
        {
            DbInfoMessage.Append(e.Message);
        }

        void OdbcInfoMessage(object sender, OdbcInfoMessageEventArgs e)
        {
            DbInfoMessage.Append(e.Message);
        }
        void SqlInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            DbInfoMessage.Append(e.Message);
        }

        void MicrosoftSqlInfoMessage(object sender, Microsoft.Data.SqlClient.SqlInfoMessageEventArgs e)
        {
            DbInfoMessage.Append(e.Message);
        }

        void MySqlInfoMessage(object sender, MySql.Data.MySqlClient.MySqlInfoMessageEventArgs e)
        {
            DbInfoMessage.Append(e.errors);
        }
        void OracleInfoMessage(object sender, OracleInfoMessageEventArgs e)
        {
            DbInfoMessage.Append(e.Errors);
        }
        #endregion

        /// <summary>
        /// Optional input object for the task
        /// </summary>
        public object ExecInput;

        /// <summary>
        /// The result of the task, if any
        /// </summary>
        public object ExecResult;

        /// <summary>
        /// Exec flag to disable children processing
        /// </summary>
        public bool ExecProcessChildren = true;

    }
}

