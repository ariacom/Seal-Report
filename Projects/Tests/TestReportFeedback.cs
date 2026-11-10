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
    /// Feedbacks on a report result: context set from the execution, 'Report Feedback Script' of the server configuration, audit without the text typed
    /// </summary>
    [TestClass]
    public class TestReportFeedback
    {
        static Repository _repository;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _repository = Repository.Instance;
        }

        static Report executeReport(string userName)
        {
            var report = Report.LoadFromFile(Path.Combine(_repository.ReportsFolder, "Samples", "01-Simple list (Products).srex"), _repository);
            report.SecurityContext = new SecurityUser(_repository.Security) { Name = userName };
            var execution = new ReportExecution() { Report = report };
            execution.Execute();
            while (report.Status != ReportStatus.Executed && !report.HasErrors) System.Threading.Thread.Sleep(100);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
            return report;
        }

        static ReportFeedback newFeedback(Report report, string text)
        {
            var feedback = new ReportFeedback() { User = report.SecurityContext, Kind = ReportFeedbackKind.Data, Text = text, ExpectedValue = " 42 ", TargetLabel = "Unit Price", RowLabel = "Chai" };
            feedback.Normalize();
            feedback.SetReportContext(report);
            return feedback;
        }

        [TestMethod]
        public void FeedbackProposalWithoutReport()
        {
            //Tool 'feedback_send': a proposal has no report, the feedback is processed by the script and audited with its origin
            var configuration = _repository.Configuration;
            var script = configuration.FeedbackScript;
            var auditEnabled = configuration.AuditEnabled;
            try
            {
                configuration.AuditEnabled = false;
                configuration.FeedbackScript = "@{ ReportFeedback feedback = Model; feedback.FeedbackId = feedback.Kind + \"/\" + feedback.Origin + \"/\" + (feedback.Report == null); }";
                var feedback = new ReportFeedback() { User = new SecurityUser(_repository.Security) { Name = "TestReportFeedback" }, Kind = ReportFeedbackKind.Proposal, Origin = ReportFeedbackOrigin.MCP, Text = " A report on the sales by region " };
                feedback.Normalize();
                feedback.SetReportContext(null);
                Assert.IsNull(configuration.ProcessFeedback(feedback));
                Assert.AreEqual("Proposal/MCP/True", feedback.FeedbackId);
                Assert.AreEqual("Proposal (MCP)", feedback.AuditDetail);
                Assert.AreEqual("A report on the sales by region", feedback.Text);
            }
            finally
            {
                configuration.FeedbackScript = script;
                configuration.AuditEnabled = auditEnabled;
            }

            //Limit per sender of the tool
            var sender = "TestReportFeedback " + Guid.NewGuid();
            for (int i = 0; i < 3; i++) Assert.IsTrue(ReportFeedback.CheckSenderLimit(sender, 3));
            Assert.IsFalse(ReportFeedback.CheckSenderLimit(sender, 3), "4th feedback of the hour refused");
        }

        [TestMethod]
        public void FeedbackSendTool()
        {
            //AI tool 'feedback_send' of the repository: report loaded with the rights of the user, context without execution, refusals
            var configuration = _repository.Configuration;
            var script = configuration.FeedbackScript;
            var auditEnabled = configuration.AuditEnabled;
            var tool = _repository.AIConfiguration.AITools.First(i => i.Name == "feedback_send");
            var user = new SecurityUser(_repository.Security) { Name = "TestFeedbackTool " + Guid.NewGuid() };
            user.SecurityGroups.Add(_repository.Security.Groups.First(g => g.Name == "Developers"));
            user.SetFolders();
            string send(string arguments, bool mcp = false) { return tool.Execute(new Seal.AI.AIToolCall() { Name = "feedback_send", Arguments = arguments, SecurityContext = user, FromMcp = mcp }); }
            try
            {
                configuration.AuditEnabled = false;
                configuration.FeedbackScript = "";
                StringAssert.StartsWith(send("{\"kind\":\"Other\",\"text\":\"x\"}"), "Error: the feedbacks are not enabled");

                configuration.FeedbackScript = "@{ ReportFeedback feedback = Model; if (feedback.Text == \"refuse\") { feedback.Refusal = \"No.\"; return; } feedback.FeedbackId = \"7\"; feedback.Reply = string.Join(\"|\", feedback.Kind, feedback.Origin, feedback.ReportPath, feedback.ReportHash?.Length, feedback.Restrictions, feedback.TargetLabel, feedback.ExpectedValue, feedback.RowCount == null); }";
                var result = send("{\"kind\":\"Data\",\"text\":\"Wrong price\",\"report_path\":\"Reports\\\\Samples\\\\01-Simple list (Products).srex\",\"criteria\":\"Category = Beverages\",\"target_label\":\"Unit Price\",\"expected_value\":\"19\"}", true);
                Assert.AreEqual(@"Feedback sent (number 7). Message for the user: Data|MCP|\Samples\01-Simple list (Products).srex|12|Category = Beverages|Unit Price|19|True", result);

                result = send("{\"kind\":\"Proposal\",\"text\":\"A report on the sales by region\"}");
                StringAssert.Contains(result, "Proposal|AI||");

                StringAssert.StartsWith(send("{\"kind\":\"Data\",\"text\":\"x\"}"), "Error: report_path is required");
                StringAssert.StartsWith(send("{\"kind\":\"Usefulness\",\"text\":\"x\"}"), "Error: kind must be");
                StringAssert.StartsWith(send("{\"kind\":\"Other\",\"text\":\" \"}"), "Error: text is required");
                StringAssert.StartsWith(send("{\"kind\":\"Data\",\"text\":\"x\",\"report_path\":\"Reports\\\\Nothing.srex\"}"), "Error: report 'Reports");
                Assert.AreEqual("Error: the feedback was refused: No.", send("{\"kind\":\"Other\",\"text\":\"refuse\"}"));
            }
            finally
            {
                configuration.FeedbackScript = script;
                configuration.AuditEnabled = auditEnabled;
            }
        }

        [TestMethod]
        public void FeedbackContextAndNormalization()
        {
            var report = executeReport("TestReportFeedback " + Guid.NewGuid());
            var feedback = newFeedback(report, new string('x', ReportFeedback.MaxTextLength + 10));

            Assert.AreEqual(ReportFeedback.MaxTextLength, feedback.Text.Length, "text truncated");
            Assert.AreEqual("42", feedback.ExpectedValue, "value trimmed");
            Assert.AreEqual(@"\Samples\01-Simple list (Products).srex", feedback.ReportPath);
            Assert.AreEqual(report.ExecutionName, feedback.ReportName);
            Assert.IsTrue(feedback.RowCount > 0, "rows of the result");
            Assert.IsNotNull(feedback.ExecutionDate);
            Assert.AreEqual(Repository.ProductVersionWithBuild, feedback.ApplicationVersion);
            StringAssert.StartsWith(feedback.ApplicationVersion, Repository.ProductVersion);
            Assert.AreEqual(File.GetLastWriteTime(report.FilePath), feedback.ReportModification);
            Assert.AreEqual(12, feedback.ReportHash?.Length, "hash of the report file");
            Assert.AreEqual(feedback.ReportHash, ReportFeedback.GetReportHash(report), "same content, same hash");

            //A copy of the file (other date, same content) keeps the hash; a modified file has no hash
            var copy = Path.Combine(Path.GetTempPath(), $"seal_feedback_{Guid.NewGuid()}.srex");
            try
            {
                File.Copy(report.FilePath, copy);
                File.SetLastWriteTime(copy, DateTime.Now.AddDays(1));
                var copied = Report.LoadFromFile(copy, _repository, false);
                Assert.AreEqual(feedback.ReportHash, ReportFeedback.GetReportHash(copied), "copy: same hash");
                File.SetLastWriteTime(copy, DateTime.Now.AddDays(2));
                Assert.IsNull(ReportFeedback.GetReportHash(copied), "file changed since the load: no hash");
            }
            finally
            {
                if (File.Exists(copy)) File.Delete(copy);
            }
            Assert.AreEqual("Data: Unit Price | Chai", feedback.AuditDetail, "no text in the audit detail");

            var empty = new ReportFeedback() { Text = "  " };
            empty.Normalize();
            Assert.IsFalse(empty.HasContent);
            var rating = new ReportFeedback() { Rating = 5 };
            rating.Normalize();
            Assert.AreEqual(1, rating.Rating);
            Assert.IsTrue(rating.HasContent);
        }

        [TestMethod]
        public void FeedbackPointedElement()
        {
            //Pointing mode of the dialog: the 'viewid' attribute of the element clicked gives the name of the view commented
            var report = executeReport("TestReportFeedback " + Guid.NewGuid());
            ReportView find(ReportView view, string templateName)
            {
                if (view.TemplateName == templateName) return view;
                return view.Views.Select(i => find(i, templateName)).FirstOrDefault(i => i != null);
            }
            var dataTable = find(report.ExecutionView, ReportViewTemplate.DataTableName);
            Assert.IsNotNull(dataTable, "Data Table view of the sample");

            var feedback = new ReportFeedback() { Kind = ReportFeedbackKind.Data, Text = "Wrong price", ViewId = dataTable.ViewId, TargetLabel = "Unit Price", TargetValue = "18.00", RowLabel = "Chai" };
            feedback.Normalize();
            feedback.SetReportContext(report);
            Assert.AreEqual(dataTable.Name, feedback.ViewName);
            Assert.AreEqual($"Data: {dataTable.Name} | Unit Price | Chai", feedback.AuditDetail);

            var unknown = new ReportFeedback() { Kind = ReportFeedbackKind.Data, Text = "x", ViewId = "unknown_view" };
            unknown.SetReportContext(report);
            Assert.IsNull(unknown.ViewName, "unknown view id");
        }

        [TestMethod]
        public void FeedbackScript()
        {
            var configuration = _repository.Configuration;
            var userName = "TestReportFeedback " + Guid.NewGuid();
            var report = executeReport(userName);
            var file = Path.Combine(Path.GetTempPath(), $"seal_feedback_{Guid.NewGuid()}.txt");
            try
            {
                Assert.IsFalse(configuration.FeedbackEnabled, "no script: feedbacks disabled");
                Assert.IsNotNull(configuration.ProcessFeedback(newFeedback(report, "text")), "refused without script");

                //The script receives the values typed and the context (System.Linq and System.IO as in the template of the Server Manager)
                configuration.FeedbackScript = "@using System.IO\r\n@{ ReportFeedback feedback = Model; if (feedback.Text == \"refuse\") { feedback.Refusal = \"refused\"; return; }\r\n"
                    + "File.AppendAllText(@\"" + file + "\", string.Join(\"|\", new string[] { feedback.Kind.ToString(), feedback.ReportPath, feedback.Text, feedback.ExpectedValue, feedback.User?.Name }.Select(i => i ?? \"\")));\r\n"
                    + "feedback.Reply = \"thanks \" + feedback.RowCount; }";
                Assert.IsTrue(configuration.FeedbackEnabled);
                var feedback = newFeedback(report, "the price is wrong");
                Assert.IsNull(configuration.ProcessFeedback(feedback));
                Assert.AreEqual("thanks " + feedback.RowCount, feedback.Reply);
                Assert.AreEqual($@"Data|\Samples\01-Simple list (Products).srex|the price is wrong|42|{userName}", File.ReadAllText(file));
                Assert.AreEqual(("ReportFeedback", "Data: Unit Price | Chai", "Feedback", (string)null), lastAudit(userName), "audited without the text");

                Assert.AreEqual("refused", configuration.ProcessFeedback(newFeedback(report, "refuse")));
                Assert.AreEqual(("ReportFeedback", "Data: Unit Price | Chai", "Feedback", "refused"), lastAudit(userName), "refusal audited as error");

                //A script in error refuses the feedback with a generic message
                configuration.FeedbackScript = "@{ throw new Exception(\"script error\"); }";
                var refusal = configuration.ProcessFeedback(newFeedback(report, "text"));
                Assert.IsNotNull(refusal);
                Assert.IsFalse(refusal.Contains("script error"), "the error is not shown to the user");
            }
            finally
            {
                configuration.FeedbackScript = null;
                if (File.Exists(file)) File.Delete(file);
            }
        }

        static (string, string, string, string) lastAudit(string userName)
        {
            using var connection = new SQLiteConnection($"Data Source={Path.Combine(_repository.RepositoryPath, "Databases", "Audit.sqlite")}");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "select event_type, event_detail, execution_kind, event_error from sr_audit where user_name=@user and event_type='ReportFeedback' order by rowid desc limit 1";
            command.Parameters.AddWithValue("@user", userName);
            using var reader = command.ExecuteReader();
            Assert.IsTrue(reader.Read(), "feedback audited");
            return (reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3));
        }
    }
}
