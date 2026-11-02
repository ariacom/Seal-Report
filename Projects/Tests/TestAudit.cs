//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Model;
using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace Test
{
    /// <summary>
    /// Path of the report executions in the audit: the location of the repository is not written
    /// </summary>
    [TestClass]
    public class TestAudit
    {
        Repository _repository;

        [TestInitialize]
        public void Initialize()
        {
            _repository = Repository.Create();
        }

        Report executeReport(string path, string userName, object tag = null)
        {
            var report = Report.LoadFromFile(path, _repository);
            report.SecurityContext = new SecurityUser(_repository.Security) { Name = userName };
            report.Tag = tag;
            var execution = new ReportExecution() { Report = report };
            execution.Execute();
            //The status stays NotExecuted until the execution thread has started
            while (report.Status != ReportStatus.Executed && !report.HasErrors) System.Threading.Thread.Sleep(100);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
            return report;
        }

        string lastAuditedPath(string userName)
        {
            using var connection = new SQLiteConnection($"Data Source={Path.Combine(_repository.RepositoryPath, "Databases", "Audit.sqlite")}");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "select event_path from sr_audit where user_name=@user order by rowid desc limit 1";
            command.Parameters.AddWithValue("@user", userName);

            //The report status is set before the audit: wait for the record
            string result = null;
            for (int i = 0; i < 100 && result == null; i++)
            {
                result = command.ExecuteScalar() as string;
                if (result == null) System.Threading.Thread.Sleep(100);
            }
            return result;
        }

        int executionsCount(string reportPath, string userName)
        {
            var report = executeReport(Path.Combine(_repository.SubReportsFolder, "Report Executions.srex"), userName, reportPath);
            return report.Models.First().ResultTable.Rows.Count;
        }

        [TestMethod]
        public void AuditPathHasNoRepositoryLocation()
        {
            var reports = _repository.ReportsFolder;
            Assert.AreEqual(@"\Samples\05-Maps.srex", _repository.GetAuditPath(Path.Combine(reports, "Samples", "05-Maps.srex")));
            Assert.AreEqual(@"\SubReports\Report Executions.srex", _repository.GetAuditPath(Path.Combine(_repository.SubReportsFolder, "Report Executions.srex")));
            Assert.AreEqual(@"\SpecialFolders\Personal\user\a.srex", _repository.GetAuditPath(Path.Combine(_repository.PersonalFolder, "user", "a.srex")));
            Assert.AreEqual("ai_query.srex", _repository.GetAuditPath(Path.Combine(Path.GetTempPath(), "ai_query.srex")));
            Assert.AreEqual(@"\Reports2\a.srex", _repository.GetAuditPath(reports + @"2\a.srex"), "A folder starting with the same name is not the Reports folder");
            Assert.AreEqual("a.srex", _repository.GetAuditPath(_repository.RepositoryPath.TrimEnd('\\') + @"2\a.srex"), "A folder starting with the same name is outside of the repository");
            Assert.AreEqual("a.srex", _repository.GetAuditPath("a.srex"));
            Assert.AreEqual("", _repository.GetAuditPath(""));
        }

        [TestMethod]
        public void ReportExecutionsFindRelativeAndFullPaths()
        {
            var path = Path.Combine(_repository.ReportsFolder, "Samples", "05-Maps.srex");

            //A real execution is audited without the location of the repository
            var user = "TestAudit " + Guid.NewGuid();
            executeReport(path, user);
            Assert.AreEqual(@"\Samples\05-Maps.srex", lastAuditedPath(user));
            Assert.IsTrue(executionsCount(path, user) > 0);
            Assert.AreEqual(0, executionsCount(Path.Combine(_repository.ReportsFolder, "Samples", "Other.srex"), user));

            //An execution audited with the full path before the change is still found
            var oldUser = "TestAudit " + Guid.NewGuid();
            Audit.LogAudit(AuditType.ReportExecution, new SecurityUser(_repository.Security) { Name = oldUser }, path);
            Assert.IsTrue(executionsCount(path, oldUser) > 0);
        }
    }
}
