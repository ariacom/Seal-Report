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
        public static bool CheckTableCreation = true;

        public AuditType Type;
        public SecurityUser User;
        public Report Report;
        public ReportSchedule Schedule;

        static string Key = Guid.NewGuid().ToString();

        /// <summary>
        /// Executes the audit script for a given event
        /// </summary>
        public static void LogAudit(AuditType type, SecurityUser user, Report report, ReportSchedule schedule)
        {
            try
            {
                var script = Repository.Instance.Configuration.AuditScript;
                if (!string.IsNullOrEmpty(Repository.Instance.Configuration.AuditScript))
                {
                    var audit = new Audit() { Type = type, User = user, Report = report, Schedule = schedule };
                    RazorHelper.CompileExecute(script, audit, Key);
                }
            }
            catch(Exception ex)
            {
                var message = string.Format("Error executing the Audit Script:\r\n{0}", ex.Message);
                Helper.WriteLogEntry("Seal Audit", EventLogEntryType.Error, ex.Message);
            }
        }
    }
}

