//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;

namespace Seal.Model
{

    /// <summary>
    /// Exception that should not be logged or audited
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Exception for Login process
    /// </summary>
    public class LoginException : Exception
    {
        public LoginException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Exception for Session Lost
    /// </summary>
    public class SessionLostException : Exception
    {
        public SessionLostException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Class dedicated to log events for audit purpose
    /// </summary>
    public class Audit
    {
        public const string AuditScriptTemplate = @"@{
    Audit audit = Model;
    //Default implementation: insert the audit record into the 'sr_audit' table of the data source
    //having a name starting with 'Audit'. The table (and new columns after an upgrade) is created automatically.
    //Customize this script to filter events or route them elsewhere, e.g.
    //if (audit.Type == AuditType.AIChat) { /* custom processing using audit properties */ }
    audit.LogToDatabase();
}
";

        public static bool CheckTableCreation = true;

        public AuditType Type;
        public string Path;
        public string Detail;
        public string Error;
        public SecurityUser User;
        public Report Report;
        public ReportSchedule Schedule;

        /// <summary>
        /// Duration in seconds for events without a Report (e.g. AI Chat exchanges).
        /// When Report is set, the report execution duration is used instead.
        /// </summary>
        public int? Duration;

        //AI Chat events
        public string AIAgentName;
        public string AIProvider;
        public string AIModel;
        public int? AIInputTokens;
        public int? AIOutputTokens;
        public int? AICalls;
        public int? AIToolCalls;
        public int? AIMessageCount;
        public double? AICost;

        static string Key = Guid.NewGuid().ToString();

        /// <summary>
        /// Audit a report execution
        /// </summary>
        public static void LogReportAudit(Report report, ReportSchedule schedule)
        {
            Audit.LogAudit(report.HasErrors ? AuditType.ReportExecutionError : AuditType.ReportExecution, report.SecurityContext, report.FilePath, null, report.ExecutionErrors, report, schedule);
        }

        /// <summary>
        /// Audit an event
        /// </summary>
        public static void LogEventAudit(AuditType type, string detail)
        {
            Audit.LogAudit(type, null, null, detail);
        }

        /// <summary>
        /// Audit an AI Chat exchange (one user message and its final reply).
        /// Token usage is taken from the agent's last exchange, the detail is the chat title.
        /// </summary>
        public static void LogAIChatAudit(SecurityUser user, Seal.AI.AIAgent agent, string error = null, int? durationSeconds = null)
        {
            try
            {
                if (agent == null || !Repository.Instance.Configuration.AuditEnabled) return;

                var providerConfiguration = agent.Configuration?.GetProviderConfiguration();
                var usage = agent.LastChatUsage;
                var audit = new Audit()
                {
                    Type = string.IsNullOrEmpty(error) ? AuditType.AIChat : AuditType.AIChatError,
                    User = user,
                    Detail = agent.EffectiveTitle,
                    Error = error,
                    Duration = durationSeconds,
                    AIAgentName = agent.Configuration?.Name,
                    AIProvider = providerConfiguration?.Name,
                    AIModel = providerConfiguration?.Model,
                    AIInputTokens = usage?.InputTokens,
                    AIOutputTokens = usage?.OutputTokens,
                    AICalls = usage?.Calls,
                    AIToolCalls = usage?.ToolCalls,
                    AIMessageCount = agent.MessageCount,
                    AICost = providerConfiguration?.GetCost(usage)
                };
                ExecuteAuditScript(audit);
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntry("Seal Audit", EventLogEntryType.Error, $"Error executing the Audit Script:\r\n{ex.Message}");
            }
        }

        /// <summary>
        /// Execute the audit script for a given event
        /// </summary>
        public static void LogAudit(AuditType type, SecurityUser user, string path = null, string detail = null, string error = null, Report report = null, ReportSchedule schedule = null)
        {
            try
            {
                if (type == AuditType.LoginFailure && string.IsNullOrEmpty(user.WebUserName)) return; //No audit for this type

                if (Repository.Instance.Configuration.AuditEnabled)
                {
                    var audit = new Audit() { Type = type, User = user, Path = path, Detail = detail, Error = error, Report = report, Schedule = schedule };
                    ExecuteAuditScript(audit);
                }
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntry("Seal Audit", EventLogEntryType.Error, $"Error executing the Audit Script:\r\n{ex.Message}");
            }
        }

        /// <summary>
        /// Execute the audit for a record: the default database insertion when no custom script is
        /// configured (no Razor compilation involved), otherwise the custom Audit Script.
        /// </summary>
        static void ExecuteAuditScript(Audit audit)
        {
            var script = Repository.Instance.Configuration.AuditScript;
            Action executeAudit = string.IsNullOrEmpty(script)
                ? audit.LogToDatabase
                : () => RazorHelper.CompileExecute(script, audit, Key);

            var auditSource = Repository.Instance.Sources.FirstOrDefault(i => i.Name.StartsWith("Audit"));
            bool lockDatabase = (auditSource != null && auditSource.Connection.ConnectionType == ConnectionType.SQLite);

            if (lockDatabase)
            {
                lock (Key)
                {
                    ExecuteWithRetry(executeAudit);
                }
            }
            else
            {
                executeAudit();
            }
        }

        /// <summary>
        /// Default audit implementation: inserts the audit record into the 'sr_audit' table of the
        /// data source having a name starting with 'Audit'. The table is created on first use and
        /// upgraded automatically when new columns are introduced.
        /// Called by the default Audit Script; a custom script can also call it (Model.LogToDatabase())
        /// before or after its own processing.
        /// </summary>
        public void LogToDatabase()
        {
            var auditSource = Repository.Instance.Sources.FirstOrDefault(i => i.Name.StartsWith("Audit"));
            if (auditSource == null) return;

            var connection = auditSource.Connection.GetOpenConnection();
            try
            {
                var command = new TaskDatabaseHelper().GetDbCommand(connection);
                CheckAuditTable(command);

                var date = DateTime.Now;
                //Keep the Local kind: with a DateTimeKind=Utc connection (Audit SQLite source), an Unspecified
                //kind gets stored with a 'Z' suffix, shifting values read back from SQL aggregates (e.g. max) by the UTC offset
                date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);

                var values = new List<Tuple<string, DbType, object>>
                {
                    Tuple.Create("event_date", DbType.DateTime, (object)date),
                    Tuple.Create("event_type", DbType.AnsiString, (object)Type.ToString()),
                    Tuple.Create("event_path", DbType.AnsiString, (object)Path),
                    Tuple.Create("event_detail", DbType.AnsiString, (object)Detail),
                    Tuple.Create("event_error", DbType.AnsiString, (object)Error),
                    Tuple.Create("user_name", DbType.AnsiString, (object)User?.Name),
                    Tuple.Create("user_groups", DbType.AnsiString, (object)User?.SecurityGroupsDisplay),
                    Tuple.Create("user_session", DbType.AnsiString, (object)User?.SessionID),
                    Tuple.Create("execution_name", DbType.AnsiString, (object)Report?.ExecutionName),
                    Tuple.Create("execution_context", DbType.AnsiString, (object)Report?.ExecutionContext.ToString()),
                    Tuple.Create("execution_view", DbType.AnsiString, (object)Report?.ExecutionView.Name),
                    Tuple.Create("execution_duration", DbType.Int32, Report != null ? (object)Convert.ToInt32(Report.ExecutionFullDuration.TotalSeconds) : (object)Duration),
                    Tuple.Create("output_type", DbType.AnsiString, (object)Report?.OutputToExecute?.DeviceName),
                    Tuple.Create("output_name", DbType.AnsiString, (object)Report?.OutputToExecute?.Name),
                    Tuple.Create("output_information", DbType.AnsiString, (object)Report?.OutputToExecute?.Information),
                    Tuple.Create("schedule_name", DbType.AnsiString, (object)Schedule?.Name),
                    Tuple.Create("ai_agent", DbType.AnsiString, (object)AIAgentName),
                    Tuple.Create("ai_provider", DbType.AnsiString, (object)AIProvider),
                    Tuple.Create("ai_model", DbType.AnsiString, (object)AIModel),
                    Tuple.Create("ai_input_tokens", DbType.Int32, (object)AIInputTokens),
                    Tuple.Create("ai_output_tokens", DbType.Int32, (object)AIOutputTokens),
                    Tuple.Create("ai_calls", DbType.Int32, (object)AICalls),
                    Tuple.Create("ai_tool_calls", DbType.Int32, (object)AIToolCalls),
                    Tuple.Create("ai_message_count", DbType.Int32, (object)AIMessageCount),
                    Tuple.Create("ai_cost", DbType.Double, (object)AICost)
                };

                // OleDb/Odbc are positional ('?'), Oracle uses ':pN', others '@pN'
                bool positional = command is OleDbCommand || command is OdbcCommand;
                var oracleCommand = command as Oracle.ManagedDataAccess.Client.OracleCommand;
                if (oracleCommand != null) oracleCommand.BindByName = true;
                var placeholderPrefix = oracleCommand != null ? ":" : "@";

                command.CommandText = string.Format("insert into sr_audit({0}) values({1})",
                    string.Join(",", values.Select(v => v.Item1)),
                    positional ? string.Join(",", values.Select(v => "?"))
                               : string.Join(",", values.Select((v, i) => placeholderPrefix + "p" + (i + 1))));

                for (int i = 0; i < values.Count; i++)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = placeholderPrefix + "p" + (i + 1);
                    parameter.DbType = values[i].Item2;
                    var value = values[i].Item3 ?? DBNull.Value;
                    if (value is string stringValue && stringValue.Length >= 255) value = stringValue.Substring(0, 254);
                    parameter.Value = value;
                    command.Parameters.Add(parameter);
                }
                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Creates the sr_audit table on first use, or adds the AI Chat columns when upgrading an
        /// existing audit database. Executed once per process (see <see cref="CheckTableCreation"/>).
        /// </summary>
        static void CheckAuditTable(DbCommand command)
        {
            if (!CheckTableCreation) return;
            CheckTableCreation = false;
            try
            {
                command.CommandText = "select 1 from sr_audit where 1=0";
                command.ExecuteNonQuery();

                //Table exists: add the AI Chat columns when upgrading from a version without them
                try
                {
                    command.CommandText = "select ai_input_tokens from sr_audit where 1=0";
                    command.ExecuteNonQuery();
                }
                catch
                {
                    foreach (var columnDef in new string[] { "ai_agent varchar(255)", "ai_provider varchar(255)", "ai_model varchar(255)", "ai_input_tokens int null", "ai_output_tokens int null", "ai_calls int null", "ai_tool_calls int null", "ai_message_count int null" })
                    {
                        command.CommandText = "alter table sr_audit add " + columnDef;
                        command.ExecuteNonQuery();
                    }
                }

                //Add the AI cost column when upgrading from a version having the AI Chat columns but not ai_cost
                try
                {
                    command.CommandText = "select ai_cost from sr_audit where 1=0";
                    command.ExecuteNonQuery();
                }
                catch
                {
                    command.CommandText = "alter table sr_audit add ai_cost float null";
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                //Create the table (to be adapted for your database type, e.g. ident identity(1,1), execution_error varchar(max) for SQLServer)
                command.CommandText = @"create table sr_audit (
                        event_date datetime,event_type varchar(255),event_path varchar(255),event_detail varchar(255),event_error varchar(255),user_name varchar(255),user_groups varchar(255),user_session varchar(255),execution_name varchar(255),execution_context varchar(255),execution_view varchar(255),execution_status varchar(255),execution_duration int null,execution_locale varchar(255),execution_error varchar(255),output_type varchar(255),output_name varchar(255),output_information varchar(255),schedule_name varchar(255),ai_agent varchar(255),ai_provider varchar(255),ai_model varchar(255),ai_input_tokens int null,ai_output_tokens int null,ai_calls int null,ai_tool_calls int null,ai_message_count int null,ai_cost float null
                    )";
                command.ExecuteNonQuery();
            }
        }

        private static void ExecuteWithRetry(Action action, int maxRetryCount = 3, int delayMilliseconds = 200)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    action();
                    break;
                }
                catch (SQLiteException ex) when (ex.Message.Contains("database is locked") && retryCount < maxRetryCount)
                {
                    retryCount++;
                    System.Threading.Thread.Sleep(delayMilliseconds);
                }
            }
        }
    }
}