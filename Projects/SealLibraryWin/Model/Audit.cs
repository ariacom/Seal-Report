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
        /// <summary>
        /// Constructor with a message
        /// </summary>
        public ValidationException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Exception for Login process
    /// </summary>
    public class LoginException : Exception
    {
        /// <summary>
        /// Constructor with a message
        /// </summary>
        public LoginException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Exception for Session Lost
    /// </summary>
    public class SessionLostException : Exception
    {
        /// <summary>
        /// Constructor with a message
        /// </summary>
        public SessionLostException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Class dedicated to log events for audit purpose
    /// </summary>
    public class Audit
    {
        /// <summary>
        /// Default template of the Audit Script proposed in the server configuration
        /// </summary>
        public const string AuditScriptTemplate = @"@{
    Audit audit = Model;
    //Default implementation: insert the audit record into the 'sr_audit' table of the data source
    //having a name starting with 'Audit'. The table (and new columns after an upgrade) is created automatically.
    //Customize this script to filter events or route them elsewhere, e.g.
    //if (audit.Type == AuditType.AIChat) { /* custom processing using audit properties */ }
    audit.LogToDatabase();
}
";

        /// <summary>
        /// If true, the existence of the 'sr_audit' table is checked (and the table created or upgraded if necessary) on the next audit. Reset to false after the first check.
        /// </summary>
        public static bool CheckTableCreation = true;

        /// <summary>
        /// Type of the audit event
        /// </summary>
        public AuditType Type;
        /// <summary>
        /// Path involved in the event (e.g. for a report execution, the report path without the location of the repository, see <see cref="Repository.GetAuditPath"/>)
        /// </summary>
        public string Path;
        /// <summary>
        /// Detail of the event
        /// </summary>
        public string Detail;
        /// <summary>
        /// Error message if the event is an error
        /// </summary>
        public string Error;
        /// <summary>
        /// Security user involved in the event
        /// </summary>
        public SecurityUser User;
        /// <summary>
        /// Report involved in the event or null
        /// </summary>
        public Report Report;
        /// <summary>
        /// Report schedule involved in the event or null
        /// </summary>
        public ReportSchedule Schedule;

        /// <summary>
        /// Duration in seconds of the event (e.g. AI Chat exchange, export of a report result).
        /// If not set and Report is set, the report execution duration is used.
        /// </summary>
        public int? Duration;

        //Report events
        /// <summary>
        /// For report events, number of rows of the result: sum of the rows of the models executed, null if no model was executed. 0 means an empty result.
        /// Set from the report if not set by the caller (logged in the column 'execution_rows').
        /// </summary>
        public int? ExecutionRows;
        /// <summary>
        /// For report events, restrictions prompted having a value at the execution, e.g. 'Country In France ; Order Date >= 01/01/2026'.
        /// Set from the report if not set by the caller (logged in the column 'execution_restrictions', 254 characters at most). It may contain values typed by the user.
        /// </summary>
        public string ExecutionRestrictions;
        /// <summary>
        /// For report events, kind of the action: 'Execution', 'Navigation' (report executed or opened by a navigation link: sub-report or drill) or 'Export' (result exported from the Web Report Server).
        /// Set from the report if not set by the caller (logged in the column 'execution_kind').
        /// </summary>
        public string ExecutionKind;

        /// <summary>
        /// If set, logged in the column 'execution_context' instead of the execution context of the report (e.g. context of the check of a report)
        /// </summary>
        public string ExecutionContext;
        /// <summary>
        /// If set, logged in the column 'output_information' instead of the information of the report output (e.g. size of the HTML result of a check)
        /// </summary>
        public string OutputInformation;

        /// <summary>
        /// Columns of the table 'sr_audit' for the report events, added when upgrading an existing audit database
        /// </summary>
        static readonly string[] ExecutionColumns = new string[] { "execution_rows int null", "execution_restrictions varchar(255)", "execution_kind varchar(255)" };

        /// <summary>
        /// False if the columns execution_rows, execution_restrictions and execution_kind are not in the table 'sr_audit' and could not be added
        /// (e.g. the account of the audit source cannot alter the table): the events are then audited without them.
        /// </summary>
        public static bool HasExecutionColumns = true;

        //AI Chat events
        /// <summary>
        /// For AI Chat events, name of the AI agent
        /// </summary>
        public string AIAgentName;
        /// <summary>
        /// For AI Chat events, name of the AI provider used
        /// </summary>
        public string AIProvider;
        /// <summary>
        /// For AI Chat events, name of the AI model used
        /// </summary>
        public string AIModel;
        /// <summary>
        /// For AI Chat events, number of input tokens of the exchange
        /// </summary>
        public int? AIInputTokens;
        /// <summary>
        /// For AI Chat events, number of output tokens of the exchange
        /// </summary>
        public int? AIOutputTokens;
        /// <summary>
        /// For AI Chat events, number of API calls of the exchange
        /// </summary>
        public int? AICalls;
        /// <summary>
        /// For AI Chat events, number of tool calls of the exchange
        /// </summary>
        public int? AIToolCalls;
        /// <summary>
        /// For AI Chat events, number of messages in the chat
        /// </summary>
        public int? AIMessageCount;
        /// <summary>
        /// For AI Chat events, cost of the exchange based on the provider token costs
        /// </summary>
        public double? AICost;

        //MCP server events
        /// <summary>
        /// For MCP server events, IP address of the client (logged in the column 'user_session')
        /// </summary>
        public string ClientAddress;
        /// <summary>
        /// For MCP server events, application of the client: HTTP header 'User-Agent' (logged in the column 'execution_name')
        /// </summary>
        public string ClientApplication;

        static string Key = Guid.NewGuid().ToString();

        /// <summary>
        /// Audit a report execution
        /// </summary>
        public static void LogReportAudit(Report report, ReportSchedule schedule)
        {
            Audit.LogAudit(report.HasErrors ? AuditType.ReportExecutionError : AuditType.ReportExecution, report.SecurityContext, report.Repository.GetAuditPath(report.FilePath), null, report.ExecutionErrors, report, schedule);
        }

        /// <summary>
        /// Audit the export of a report result from the Web Report Server (e.g. Excel, CSV, PDF, HTML Print).
        /// The detail is the format, the duration is the time to generate the result.
        /// </summary>
        public static void LogReportExportAudit(Report report, ReportFormat format, int durationSeconds)
        {
            try
            {
                if (report == null || !Repository.Instance.Configuration.AuditEnabled) return;
                var audit = new Audit()
                {
                    Type = AuditType.ReportExport,
                    User = report.SecurityContext,
                    Path = report.Repository.GetAuditPath(report.FilePath),
                    Detail = format.ToString(),
                    Report = report,
                    Duration = durationSeconds,
                    ExecutionKind = "Export"
                };
                ExecuteAuditScript(audit);
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntry("Seal Audit", EventLogEntryType.Error, $"Error executing the Audit Script:\r\n{ex.Message}");
            }
        }

        /// <summary>
        /// Audit the check of a report execution by the System report '310 Reports - Check Executions':
        /// status in the detail (OK, Error, Empty, Skipped), duration, rows and restrictions of the execution, size of the HTML result in the output information, kind 'Check'.
        /// </summary>
        /// <param name="report">Report checked</param>
        /// <param name="status">Status of the check: OK, Error, Empty or Skipped</param>
        /// <param name="error">Error of the execution or reason of the skip</param>
        /// <param name="context">Execution context of the check (context of the report executing the checks)</param>
        /// <param name="durationSeconds">Duration of the execution, null to take the execution duration of the report</param>
        /// <param name="htmlSize">Size in bytes of the HTML result, null if not rendered</param>
        public static void LogReportCheckAudit(Report report, string status, string error, ReportExecutionContext context, int? durationSeconds = null, long? htmlSize = null)
        {
            try
            {
                if (report == null || !Repository.Instance.Configuration.AuditEnabled) return;
                var audit = new Audit()
                {
                    Type = AuditType.ReportCheck,
                    Path = report.Repository.GetAuditPath(report.FilePath),
                    Detail = status,
                    Error = error,
                    Report = report,
                    Duration = durationSeconds,
                    ExecutionKind = "Check",
                    ExecutionContext = context.ToString(),
                    OutputInformation = htmlSize?.ToString()
                };
                ExecuteAuditScript(audit);
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntry("Seal Audit", EventLogEntryType.Error, $"Error executing the Audit Script:\r\n{ex.Message}");
            }
        }

        /// <summary>
        /// Audit a feedback sent on a report result from the Web Report Server: kind, rating and element commented in the detail (never the text typed),
        /// refusal of the 'Feedback Script' as error, kind 'Feedback', rows and restrictions of the result commented.
        /// </summary>
        public static void LogReportFeedbackAudit(ReportFeedback feedback)
        {
            try
            {
                if (feedback == null || !Repository.Instance.Configuration.AuditEnabled) return;
                var audit = new Audit()
                {
                    Type = AuditType.ReportFeedback,
                    User = feedback.User ?? feedback.Report?.SecurityContext,
                    Path = feedback.ReportPath,
                    Detail = feedback.AuditDetail,
                    Error = feedback.Refusal,
                    Report = feedback.Report,
                    Duration = 0,
                    ExecutionRows = feedback.RowCount,
                    ExecutionRestrictions = feedback.Restrictions,
                    ExecutionKind = "Feedback"
                };
                ExecuteAuditScript(audit);
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntry("Seal Audit", EventLogEntryType.Error, $"Error executing the Audit Script:\r\n{ex.Message}");
            }
        }

        /// <summary>
        /// Set the execution properties not set by the caller (rows, restrictions, kind) from the report of the event
        /// </summary>
        void SetExecutionProperties()
        {
            if (Report == null) return;
            if (ExecutionRows == null) ExecutionRows = GetExecutionRows(Report);
            if (ExecutionRestrictions == null) ExecutionRestrictions = GetExecutionRestrictions(Report);
            if (ExecutionKind == null) ExecutionKind = Report.IsNavigating || Report.FromNavigation ? "Navigation" : "Execution";
        }

        /// <summary>
        /// Number of rows of the result of a report: sum of the rows of the models executed, null if no model was executed or if the report is checked without reading data
        /// </summary>
        public static int? GetExecutionRows(Report report)
        {
            int? result = null;
            try
            {
                //No rows when the report is checked without reading data (SQL with 1=0)
                if (report != null && !report.CheckingExecution)
                {
                    foreach (var model in report.ExecutionModels.Where(i => i.ResultTable != null))
                    {
                        result = (result ?? 0) + model.ResultTable.Rows.Count;
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Restrictions prompted having a value at the execution of a report, e.g. 'Country In France ; Order Date >= 01/01/2026', null if none
        /// </summary>
        public static string GetExecutionRestrictions(Report report)
        {
            try
            {
                if (report == null) return null;
                var texts = report.ExecutionPromptedRestrictions.Union(report.ExecutionViewRestrictions)
                    .Where(i => i.Prompt != PromptType.None && i.HasValue)
                    .Select(i => i.DisplayText)
                    .Where(i => !string.IsNullOrEmpty(i))
                    .Distinct()
                    .ToList();
                if (texts.Count > 0) return string.Join(" ; ", texts);
            }
            catch { }
            return null;
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
        /// A message refused by the limits of the user is audited as <see cref="AuditType.AIChatRefused"/> with the refusal as error.
        /// </summary>
        public static void LogAIChatAudit(SecurityUser user, Seal.AI.AIAgent agent, string error = null, int? durationSeconds = null)
        {
            try
            {
                if (agent == null || !Repository.Instance.Configuration.AuditEnabled) return;

                var providerConfiguration = agent.Configuration?.GetProviderConfiguration();
                var usage = agent.LastChatUsage;
                var type = string.IsNullOrEmpty(error) ? AuditType.AIChat : AuditType.AIChatError;
                if (string.IsNullOrEmpty(error) && agent.LastChatRefusal != null)
                {
                    type = AuditType.AIChatRefused;
                    error = agent.LastChatRefusal;
                }
                else if (string.IsNullOrEmpty(error) && agent.LastChatCostLimitReached) error = "AI budget reached";
                var audit = new Audit()
                {
                    Type = type,
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
        /// Audit a request of the MCP server: a tool call (<see cref="AuditType.AIMcpCall"/>, <see cref="AuditType.AIMcpCallError"/>) or a refusal (<see cref="AuditType.AIMcpRefused"/>).
        /// The detail is the tool name (or the request refused).
        /// </summary>
        /// <param name="type">Type of the event</param>
        /// <param name="user">User of the access token</param>
        /// <param name="agentName">Name of the agent requested</param>
        /// <param name="detail">Name of the tool, or the request refused</param>
        /// <param name="error">Error or reason of the refusal</param>
        /// <param name="durationSeconds">Duration of the tool call</param>
        /// <param name="clientAddress">IP address of the client</param>
        /// <param name="clientApplication">Application of the client (HTTP header 'User-Agent')</param>
        public static void LogMcpAudit(AuditType type, SecurityUser user, string agentName, string detail, string error = null, int? durationSeconds = null, string clientAddress = null, string clientApplication = null)
        {
            try
            {
                if (!Repository.Instance.Configuration.AuditEnabled) return;
                var audit = new Audit()
                {
                    Type = type,
                    User = user,
                    Detail = detail,
                    Error = error,
                    Duration = durationSeconds,
                    AIAgentName = agentName,
                    AIToolCalls = type == AuditType.AIMcpRefused ? 0 : 1,
                    ClientAddress = string.IsNullOrEmpty(clientAddress) ? null : clientAddress,
                    ClientApplication = string.IsNullOrEmpty(clientApplication) ? null : (clientApplication.Length > 250 ? clientApplication.Substring(0, 250) : clientApplication)
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
            audit.SetExecutionProperties();
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
                CheckAuditTable(command, auditSource.Connection.ConnectionType);

                var date = DateTime.Now;
                //Keep the Local kind: with a DateTimeKind=Utc connection (Audit SQLite source), an Unspecified
                //kind gets stored with a 'Z' suffix, shifting values read back from SQL aggregates (e.g. max) by the UTC offset
                date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);

                //PostgreSQL (Npgsql): DbType.DateTime is 'timestamp with time zone', refused for a local date; DateTime2 is 'timestamp' (the type of the column)
                var dateType = auditSource.Connection.ConnectionType == ConnectionType.PostgreSQL ? DbType.DateTime2 : DbType.DateTime;
                var values = new List<Tuple<string, DbType, object>>
                {
                    Tuple.Create("event_date", dateType, (object)date),
                    Tuple.Create("event_type", DbType.AnsiString, (object)Type.ToString()),
                    Tuple.Create("event_path", DbType.AnsiString, (object)Path),
                    Tuple.Create("event_detail", DbType.AnsiString, (object)Detail),
                    Tuple.Create("event_error", DbType.AnsiString, (object)Error),
                    Tuple.Create("user_name", DbType.AnsiString, (object)User?.Name),
                    Tuple.Create("user_groups", DbType.AnsiString, (object)User?.SecurityGroupsDisplay),
                    Tuple.Create("user_session", DbType.AnsiString, (object)(ClientAddress ?? User?.SessionID)),
                    Tuple.Create("execution_name", DbType.AnsiString, (object)(Report?.ExecutionName ?? ClientApplication)),
                    Tuple.Create("execution_context", DbType.AnsiString, (object)(ExecutionContext ?? Report?.ExecutionContext.ToString())),
                    Tuple.Create("execution_view", DbType.AnsiString, (object)Report?.ExecutionView.Name),
                    Tuple.Create("execution_duration", DbType.Int32, Duration != null || Report == null ? (object)Duration : (object)Convert.ToInt32(Report.ExecutionFullDuration.TotalSeconds)),
                    Tuple.Create("output_type", DbType.AnsiString, (object)Report?.OutputToExecute?.DeviceName),
                    Tuple.Create("output_name", DbType.AnsiString, (object)Report?.OutputToExecute?.Name),
                    Tuple.Create("output_information", DbType.AnsiString, (object)(OutputInformation ?? Report?.OutputToExecute?.Information)),
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
                if (HasExecutionColumns)
                {
                    values.Add(Tuple.Create("execution_rows", DbType.Int32, (object)ExecutionRows));
                    values.Add(Tuple.Create("execution_restrictions", DbType.AnsiString, (object)ExecutionRestrictions));
                    values.Add(Tuple.Create("execution_kind", DbType.AnsiString, (object)ExecutionKind));
                }

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
                    if (value is string stringValue && stringValue.Length >= 255) value = values[i].Item1.EndsWith("_error") ? ShortenError(stringValue) : stringValue.Substring(0, 254);
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
        /// Shortens an error for a column of 254 characters: the start and the end of the message, the error of the database
        /// being usually at the end (e.g. after the SQL statement of a model in error).
        /// </summary>
        public static string ShortenError(string error)
        {
            if (string.IsNullOrEmpty(error) || error.Length < 255) return error;
            var text = System.Text.RegularExpressions.Regex.Replace(error.Trim(), @"\s+", " ");
            if (text.Length < 255) return text;
            return text.Substring(0, 80) + " (...) " + text.Substring(text.Length - 167);
        }

        /// <summary>
        /// SQL type of a date and time column for the audit tables: 'timestamp' for PostgreSQL and Oracle (no 'datetime' type), 'datetime' otherwise (SQLite, SQL Server, MySQL).
        /// Also used by the System report '900 Tests - Run Tests and Audit' to create the sr_test_results table.
        /// </summary>
        public static string GetDateTimeSqlType(ConnectionType connectionType)
        {
            return connectionType == ConnectionType.PostgreSQL || connectionType == ConnectionType.Oracle ? "timestamp" : "datetime";
        }

        /// <summary>
        /// Creates the sr_audit table on first use, or adds the AI Chat columns and the columns of the report events when upgrading an
        /// existing audit database. Executed once per process (see <see cref="CheckTableCreation"/>).
        /// The column types are standard SQL; the date type depends on the database (see <see cref="GetDateTimeSqlType"/>).
        /// </summary>
        static void CheckAuditTable(DbCommand command, ConnectionType connectionType)
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

                //Add the columns of the report events (rows, restrictions, kind) when upgrading from a version without them.
                //If a column cannot be added (e.g. the account of the audit source may not alter the table), the events are audited without these columns.
                foreach (var columnDef in ExecutionColumns)
                {
                    try
                    {
                        command.CommandText = "select " + columnDef.Split(' ')[0] + " from sr_audit where 1=0";
                        command.ExecuteNonQuery();
                    }
                    catch
                    {
                        try
                        {
                            command.CommandText = "alter table sr_audit add " + columnDef;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            HasExecutionColumns = false;
                            Helper.WriteLogEntry("Seal Audit", EventLogEntryType.Warning, $"The columns execution_rows, execution_restrictions and execution_kind could not be added to the table sr_audit: the events are audited without them.\r\n{ex.Message}");
                            break;
                        }
                    }
                }
            }
            catch
            {
                //Create the table (to be adapted for your database type, e.g. ident identity(1,1), execution_error varchar(max) for SQLServer)
                command.CommandText = @"create table sr_audit (
                        event_date " + GetDateTimeSqlType(connectionType) + @",event_type varchar(255),event_path varchar(255),event_detail varchar(255),event_error varchar(255),user_name varchar(255),user_groups varchar(255),user_session varchar(255),execution_name varchar(255),execution_context varchar(255),execution_view varchar(255),execution_status varchar(255),execution_duration int null,execution_locale varchar(255),execution_error varchar(255),output_type varchar(255),output_name varchar(255),output_information varchar(255),schedule_name varchar(255),ai_agent varchar(255),ai_provider varchar(255),ai_model varchar(255),ai_input_tokens int null,ai_output_tokens int null,ai_calls int null,ai_tool_calls int null,ai_message_count int null,ai_cost float null,execution_rows int null,execution_restrictions varchar(255),execution_kind varchar(255)
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