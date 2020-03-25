using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Seal.Model
{
    /// <summary>
    /// Class dedicated to log events for audit purpose
    /// </summary>
    public class Audit
    {
        public const string AuditScriptTemplate = @"@using System.Data
@using System.Data.Common
@using System.Data.OleDb
@using System.Data.Odbc

@{
    Audit audit = Model;
    var auditSource = Repository.Instance.Sources.FirstOrDefault(i => i.Name.StartsWith(""Audit""));  
    if (auditSource != null) {
        var helper = new TaskDatabaseHelper();
        var command = helper.GetDbCommand(auditSource.Connection.GetOpenConnection());

        //Create audit table if necessary
        checkTableCreation(command);
        command.CommandText = @""insert into sr_audit(event_date,event_type,event_detail,user_name,user_groups,report_name,report_path,execution_context,execution_view,execution_duration,execution_error,output_type,output_name,output_information,output_error,schedule_name)"";
        if (command is OleDbCommand || command is OdbcCommand) {
            command.CommandText += "" values(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)"";
        }
        else {
            command.CommandText += "" values(@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14,@p15,@p16)"";
        }
        
        var date = DateTime.Now;
        date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second);
        int index=1;
        addParameter(command, index++, DbType.DateTime, date); //event_date,
        addParameter(command, index++, DbType.AnsiString, audit.Type.ToString()); //event_type,
        addParameter(command, index++, DbType.AnsiString, audit.Detail != null ? audit.Detail : (object) DBNull.Value); //event_detail,
        addParameter(command, index++, DbType.AnsiString, audit.User != null ? audit.User.Name : (object) DBNull.Value); //user_name,
        addParameter(command, index++, DbType.AnsiString, audit.User != null ? audit.User.SecurityGroupsDisplay : (object) DBNull.Value); //user_groups,
        addParameter(command, index++, DbType.AnsiString, audit.Report != null ? audit.Report.ExecutionName : (object) DBNull.Value); //report_name,
        addParameter(command, index++, DbType.AnsiString, audit.Report != null ? audit.Report.FilePath : (object) DBNull.Value); //report_path,
        addParameter(command, index++, DbType.AnsiString, audit.Report != null ? audit.Report.ExecutionContext.ToString() : (object) DBNull.Value); //execution_context,
        addParameter(command, index++, DbType.AnsiString, audit.Report != null ? audit.Report.ExecutionView.Name : (object) DBNull.Value); //execution_view,
        addParameter(command, index++, DbType.Int32, audit.Report != null ? Convert.ToInt32(audit.Report.ExecutionFullDuration.TotalSeconds) : (object) DBNull.Value); //execution_duration,
        addParameter(command, index++, DbType.AnsiString, audit.Report != null ? audit.Report.ExecutionErrors : (object) DBNull.Value); //execution_error,
        addParameter(command, index++, DbType.AnsiString, audit.Report != null && audit.Report.OutputToExecute != null ? audit.Report.OutputToExecute.DeviceName : (object) DBNull.Value); //output_type,
        addParameter(command, index++, DbType.AnsiString, audit.Report != null && audit.Report.OutputToExecute != null ? audit.Report.OutputToExecute.Name : (object) DBNull.Value);//output_name,
        addParameter(command, index++, DbType.AnsiString, audit.Report != null && audit.Report.OutputToExecute != null && audit.Report.OutputToExecute.Information != null ? audit.Report.OutputToExecute.Information : (object) DBNull.Value);//output_information,
        addParameter(command, index++, DbType.AnsiString, audit.Report != null && audit.Report.OutputToExecute != null && audit.Report.OutputToExecute.Error != null ? audit.Report.OutputToExecute.Error : (object) DBNull.Value);//output_error,
        addParameter(command, index++, DbType.AnsiString, audit.Report != null && audit.Schedule != null ? audit.Schedule.Name : (object) DBNull.Value);//schedule_name
        command.ExecuteNonQuery();                
    }

}

@functions {
    void checkTableCreation(DbCommand command)
    {
        if (Audit.CheckTableCreation)
        {
            //Check table creation
            Audit.CheckTableCreation = false;
            try
            {
                command.CommandText = ""select 1 from sr_audit where 1=0"";
                command.ExecuteNonQuery();
            }
            catch
            {
                //Create the table (to be adapted for your database type, e.g. ident identity(1,1), execution_error varchar(max) for SQLServer)
                command.CommandText = @""create table sr_audit (
                        event_date datetime,event_type varchar(20),event_detail varchar(255),user_name varchar(255),user_groups varchar(255),report_name varchar(255),report_path varchar(255),execution_context varchar(255),execution_view varchar(255),execution_status varchar(255),execution_duration int null,execution_locale varchar(255),execution_error varchar(max),output_type varchar(255),output_name varchar(255),output_information varchar(max),output_error varchar(max),schedule_name varchar(255)
                    )"";
                command.ExecuteNonQuery();
            }
        }
    }

    void addParameter(DbCommand command, int index, DbType type, Object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = ""@p"" + index.ToString();
        parameter.DbType = type;
        if (value == null) value = (object) DBNull.Value;

        if (value is string && ((string)value).Length >= 255) parameter.Value = ((string)value).Substring(0, 254);
        else parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
";

        public static bool CheckTableCreation = true;

        public AuditType Type;
        public string Detail;
        public SecurityUser User;
        public Report Report;
        public ReportSchedule Schedule;

        static string Key = Guid.NewGuid().ToString();

        /// <summary>
        /// Executes the audit script for a given event
        /// </summary>
        public static void LogAudit(AuditType type, SecurityUser user, Report report, ReportSchedule schedule, string detail)
        {
            try
            {
                if (Repository.Instance.Configuration.AuditEnabled)
                {
                    var script = Repository.Instance.Configuration.AuditScript;
                    if (string.IsNullOrEmpty(script)) script = AuditScriptTemplate;
                    var audit = new Audit() { Type = type, User = user, Report = report, Schedule = schedule, Detail = detail };
                    RazorHelper.CompileExecute(script, audit, Key);
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("Error executing the Audit Script:\r\n{0}", ex.Message);
                Helper.WriteLogEntry("Seal Audit", EventLogEntryType.Error, ex.Message);
            }
        }
    }
}
