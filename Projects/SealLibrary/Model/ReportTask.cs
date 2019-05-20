//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Helpers;
using DynamicTypeDescriptor;
using Seal.Converter;
using System.Drawing.Design;
using Seal.Forms;
using System.Data.Common;
using System.Threading;
using System.Data.Odbc;
using System.Data.OleDb;

namespace Seal.Model
{
    public class ReportTask : ReportComponent, ITreeSort
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("SourceGUID").SetIsBrowsable(true);
                GetProperty("ConnectionGUID").SetIsBrowsable(true);
                GetProperty("SQL").SetIsBrowsable(true);
                GetProperty("Enabled").SetIsBrowsable(true);
                GetProperty("IgnoreError").SetIsBrowsable(true);
                GetProperty("Script").SetIsBrowsable(true);
                GetProperty("ExecuteForEachConnection").SetIsBrowsable(true);

                //Helpers
                //GetProperty("Information").SetIsBrowsable(true);
                //GetProperty("Error").SetIsBrowsable(true);

                //Readonly

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion


        public static ReportTask Create()
        {
            return new ReportTask() { GUID = Guid.NewGuid().ToString() };
        }

        public void InitReferences()
        {
        }

        string _sourceGUID;
        [DefaultValue(null)]
        [Category("Definition"), DisplayName("Source"), Description("The source used by the task."), Id(1, 1)]
        [TypeConverter(typeof(MetaSourceConverter))]
        public string SourceGUID
        {
            get { return _sourceGUID; }
            set { _sourceGUID = value; }
        }

        protected string _connectionGUID = ReportSource.DefaultReportConnectionGUID;
        [DefaultValue(ReportSource.DefaultReportConnectionGUID)]
        [DisplayName("Connection"), Description("The connection used by the task."), Category("Definition"), Id(2, 1)]
        [TypeConverter(typeof(SourceConnectionConverter))]
        public string ConnectionGUID
        {
            get
            {
                if (_connectionGUID != ReportSource.DefaultReportConnectionGUID && _connectionGUID != ReportSource.DefaultRepositoryConnectionGUID)
                {
                    //reset it if not found in current connections
                    if (Source != null && !Source.Connections.Exists(i => i.GUID == _connectionGUID) && !Source.TempConnections.Exists(i => i.GUID == _connectionGUID)) _connectionGUID = ReportSource.DefaultReportConnectionGUID;
                }
                return _connectionGUID;
            }
            set { _connectionGUID = value; }
        }


        bool _enabled = true;
        [DefaultValue(true)]
        [Category("Definition"), DisplayName("Is Enabled"), Description("If false, the task is ignorred and not executed."), Id(3, 1)]
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        [XmlIgnore]
        public MetaConnection Connection
        {
            get
            {
                MetaConnection result = Source.Connection;
                if (_connectionGUID == ReportSource.DefaultReportConnectionGUID) result = Source.Connection;
                else if (_connectionGUID == ReportSource.DefaultRepositoryConnectionGUID) result = Source.RepositoryConnection;
                else result = Source.Connections.FirstOrDefault(i => i.GUID == _connectionGUID);
                if (result == null && Source.Connections.Count > 0)
                {
                    result = Source.Connections[0];
                }
                return result;
            }
        }

        [XmlIgnore]
        public ReportSource Source
        {
            get
            {
                ReportSource result = _report.Sources.FirstOrDefault(i => i.GUID == _sourceGUID);
                if (result == null)
                {
                    if (_report.Sources.Count == 0) throw new Exception("This report has no source defined");
                    result = _report.Sources[0];
                    _sourceGUID = result.GUID;
                }
                return result;
            }
        }

        [XmlIgnore]
        public Repository Repository
        {
            get
            {
                if (_report != null) return _report.Repository;
                return null;
            }
        }

        string _SQL;
        [Category("Definition"), DisplayName("SQL Statement"), Description("SQL Statement executed for the task. It may be empty if a script is defined. The statement may contain Razor script if it starts with '@'. If the SQL result returns 0, the report is cancelled and the next tasks are not executed."), Id(4, 1)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string SQL
        {
            get { return _SQL; }
            set { _SQL = value; }
        }

        string _script;
        [Category("Definition"), DisplayName("Script"), Description("Razor script executed for the Task. It may be empty if the SQL if defined. If the script returns 0, the report is cancelled and the next tasks are not executed."), Id(5, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string Script
        {
            get { return _script; }
            set { _script = value; }
        }


        bool _ignoreError = false;
        [DefaultValue(false)]
        [Category("Options"), DisplayName("Ignore Errors"), Description("If true, errors occuring during the task execution are ignored and the report execution continues."), Id(2, 2)]
        public bool IgnoreError
        {
            get { return _ignoreError; }
            set { _ignoreError = value; }
        }

        bool _executeForEachConnection = false;
        [DefaultValue(false)]
        [Category("Options"), DisplayName("Execute for each connection"), Description("If true, the task will be executed for each connection defined in the Data Source. If false, only the current connection is used."), Id(3, 2)]
        public bool ExecuteForEachConnection
        {
            get { return _executeForEachConnection; }
            set { _executeForEachConnection = value; }
        }

        int _sortOrder = 0;
        public int SortOrder
        {
            get { return _sortOrder; }
            set { _sortOrder = value; }
        }

        public int GetSort()
        {
            return _sortOrder;
        }

        [XmlIgnore]
        public int Progression = 0;

        #region Helpers
        string _information;
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(20, 20)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        string _error;
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(20, 20)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }
        public bool CancelReport = false;

        #endregion

        #region Database

        DbCommand _command = null;
        Mutex _commandMutex = new Mutex();
        [XmlIgnore]
        public StringBuilder DbInfoMessage = new StringBuilder();

        //Log Interface implementation
        public void LogMessage(string message, params object[] args)
        {
            Report.LogMessage(message, args);
        }

        public void Cancel()
        {
            CancelReport = true;
            LogMessage("Cancelling task and report...");
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
                        ((OdbcConnection)connection).InfoMessage += new OdbcInfoMessageEventHandler(OdbcDbInfoMessage);
                        result = ((OdbcConnection)connection).CreateCommand();
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

        public void Execute()
        {
            CancelReport = false;
            DbInfoMessage = new StringBuilder();
            //Temp list to avoid change of connections during a task...
            var connections = Source.Connections.Where(i => ExecuteForEachConnection || i.GUID == Connection.GUID).ToList();
            foreach (var connection in connections)
            {
                Execute(connection);
            }
        }

        public void Execute(MetaConnection currentConnection)
        {
            LogMessage("Starting task with connection '{0}'", currentConnection.Name);
            Progression = 0;
            if (!Report.Cancel && !string.IsNullOrEmpty(SQL))
            {
                if (string.IsNullOrEmpty(currentConnection.ConnectionString)) throw new Exception("The connection string is not defined for this Task.");
                _command = GetDbCommand(currentConnection);
                string finalSql = RazorHelper.CompileExecute(SQL, this);
                LogMessage("Executing SQL: {0}", finalSql);
                _command.CommandText = finalSql;
                object sqlResult = _command.ExecuteScalar();

                if (sqlResult != null && !(sqlResult is DBNull))
                {
                    if (sqlResult.ToString().Trim() == "0")
                    {
                        LogMessage("SQL returns 0, the report is cancelled.");
                        CancelReport = true;
                    }
                }
            }

            if (!Report.Cancel && !string.IsNullOrEmpty(Script))
            {
                LogMessage("Executing Script...");
                string result = RazorHelper.CompileExecute(Script, this);
                if (result.Trim() == "0")
                {
                    LogMessage("Script returns 0, the report is cancelled.");
                    CancelReport = true;
                }
            }

            Progression = 100; //100%
        }

        void OleDbInfoMessage(object sender, OleDbInfoMessageEventArgs e)
        {
            DbInfoMessage.Append(e.Message);
        }

        void OdbcDbInfoMessage(object sender, OdbcInfoMessageEventArgs e)
        {
            DbInfoMessage.Append(e.Message);
        }

        #endregion
    }
}
