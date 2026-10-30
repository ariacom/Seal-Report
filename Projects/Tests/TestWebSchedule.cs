//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Model;
using System;
using System.IO;
using System.Linq;

namespace Test
{
    /// <summary>
    /// Web schedules: stored in the personal folder of the owner, injected in the report and executed by the Seal Report Scheduler with the owner authenticated by the Security Provider
    /// </summary>
    [TestClass]
    public class TestWebSchedule
    {
        const string LoginId = "websched_test_user";
        const string ExecuteLoginId = "websched_test_execute";
        const string ReportWebPath = "\\Samples\\01-Simple list (Products).srex";

        static Repository _repository;
        static SecurityLogin _login, _executeLogin;
        static SchedulerMode _schedulerMode;
        static string _personalFolder, _executePersonalFolder;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _repository = Repository.Instance;
            _schedulerMode = _repository.Configuration.SchedulerMode;
            _repository.Configuration.SchedulerMode = SchedulerMode.Service;
            //A login having the right 'Edit schedules' on the reports and a personal folder
            _login = new SecurityLogin() { Id = LoginId, GroupIds = new System.Collections.Generic.List<string>() { _repository.Security.Groups.First(i => i.Name == "Group (Schedule)").GUID } };
            _repository.Security.Logins.Add(_login);
            //A login having only the right to execute the reports and their outputs: no web schedule
            _executeLogin = new SecurityLogin() { Id = ExecuteLoginId, GroupIds = new System.Collections.Generic.List<string>() { _repository.Security.Groups.First(i => i.Name == "Group (Execute Output)").GUID } };
            _repository.Security.Logins.Add(_executeLogin);
            _personalFolder = _repository.GetPersonalFolder(getOwner());
            _executePersonalFolder = _repository.GetPersonalFolder(getOwner(ExecuteLoginId));
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _repository.Security.Logins.Remove(_login);
            _repository.Security.Logins.Remove(_executeLogin);
            _repository.Configuration.SchedulerMode = _schedulerMode;
            foreach (var folder in new string[] { _personalFolder, _executePersonalFolder })
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        static SecurityUser getOwner(string loginId = LoginId)
        {
            var user = new SecurityUser(_repository.Security) { WebUserName = loginId, ForSchedule = true };
            user.Authenticate();
            Assert.IsTrue(user.IsAuthenticated, user.Error);
            return user;
        }

        static SealSchedule createWebSchedule(string fileName, string userName = LoginId, string folderLoginId = LoginId, string reportWebPath = ReportWebPath, Action<WebScheduleOutput> setOutput = null)
        {
            var report = Report.LoadFromFile(_repository.ReportsFolder + reportWebPath, _repository, false);
            var schedule = new SealSchedule()
            {
                GUID = Guid.NewGuid().ToString(),
                Name = "Test " + fileName,
                UserName = userName,
                WebPath = reportWebPath,
                ReportGUID = report.GUID,
                ReportPath = reportWebPath,
                Enabled = true,
                Type = TriggerType.Daily,
                Start = DateTime.Now.AddDays(1),
                Output = new WebScheduleOutput()
                {
                    DeviceGUID = _repository.Devices.OfType<OutputFolderDevice>().First().GUID,
                    Format = ReportFormat.csv,
                    FolderPath = Repository.SealPersonalRepositoryKeyword + _personalFolder.Substring(_repository.PersonalFolder.Length),
                    FileName = fileName
                }
            };
            setOutput?.Invoke(schedule.Output);
            var folder = _repository.GetWebSchedulesFolder(getOwner(folderLoginId));
            Directory.CreateDirectory(folder);
            schedule.FilePath = Path.Combine(folder, $"{schedule.Name} {schedule.GUID}.xml");
            schedule.SaveToFile();
            return schedule;
        }

        [TestMethod]
        public void ScheduleCheckScript()
        {
            var security = _repository.Security;
            var report = Report.LoadFromFile(_repository.ReportsFolder + ReportWebPath, _repository, false);
            Assert.IsNull(security.CheckSchedule(report, null, ScheduleCheckMoment.Save), "no script: every schedule is accepted");
            try
            {
                //The script receives the moment, the user and the new flag
                security.ScheduleCheckScript = "@using Seal.Model\r\n@{ ScheduleCheck check = Model; if (check.Moment == ScheduleCheckMoment.Save && check.IsNew && check.User != null) { check.Refusal = \"refused \" + check.Report.GUID; } }";
                Assert.AreEqual("refused " + report.GUID, security.CheckSchedule(report, null, ScheduleCheckMoment.Save, getOwner(), true));
                Assert.IsNull(security.CheckSchedule(report, null, ScheduleCheckMoment.Save, getOwner(), false), "update of a schedule");
                Assert.IsNull(security.CheckSchedule(report, null, ScheduleCheckMoment.Execution), "execution");

                //A script in error refuses the schedule
                security.ScheduleCheckScript = "@{ throw new Exception(\"script error\"); }";
                StringAssert.Contains(security.CheckSchedule(report, null, ScheduleCheckMoment.Execution), "script error");

                //Refused execution: no result, the error is stored and the schedule stays enabled
                security.ScheduleCheckScript = "@using Seal.Model\r\n@{ ScheduleCheck check = Model; if (check.Moment == ScheduleCheckMoment.Execution) { check.Refusal = \"no more space\"; } }";
                var schedule = createWebSchedule("refused schedule result");
                ReportExecution.ExecuteReportSchedule(schedule.GUID, null);
                var saved = SealReportScheduler.LoadSealSchedule(schedule.GUID);
                Assert.AreEqual("no more space", saved.LastError);
                Assert.IsTrue(saved.Enabled, "the schedule stays enabled");
                Assert.IsFalse(File.Exists(Path.Combine(_personalFolder, "refused schedule result.csv")), "no output");
                Assert.IsTrue(saved.NextExecution > DateTime.Now, "next execution still planned");
            }
            finally
            {
                security.ScheduleCheckScript = null;
            }
        }

        [TestMethod]
        public void ExecuteWebSchedule()
        {
            var schedule = createWebSchedule("web schedule result");
            Assert.IsTrue(SealReportScheduler.LoadSealSchedule(schedule.GUID).IsWebSchedule, "the web schedules of the personal folders are loaded by the scheduler");

            ReportExecution.ExecuteReportSchedule(schedule.GUID, null);

            var saved = SealSchedule.LoadFromFile(schedule.FilePath);
            Assert.AreNotEqual(DateTime.MinValue, saved.LastExecution, "the last execution is saved in the schedule file");
            Assert.IsTrue(string.IsNullOrEmpty(saved.LastError), saved.LastError);
            Assert.IsTrue(saved.Enabled);
            Assert.IsTrue(saved.NextExecution > DateTime.Now && saved.NextExecution != DateTime.MaxValue, "next execution calculated");
            Assert.AreEqual(1, Directory.GetFiles(_personalFolder, "web schedule result*.csv").Length, "result generated in the personal folder");

            //The report file is not modified
            var report = Report.LoadFromFile(_repository.ReportsFolder + ReportWebPath, _repository, false);
            Assert.AreEqual(0, report.Outputs.Count(i => i.GUID == schedule.GUID));
            Assert.AreEqual(0, report.Schedules.Count);
        }

        [TestMethod]
        public void InjectWebSchedule()
        {
            var schedule = createWebSchedule("injected");
            var report = SealReportScheduler.GetScheduledReport(SealReportScheduler.LoadSealSchedule(schedule.GUID));
            var reportSchedule = report.Schedules.Single(i => i.GUID == schedule.GUID);
            Assert.IsTrue(reportSchedule.IsWebSchedule);
            Assert.AreEqual(ReportFormat.csv, reportSchedule.Output.Format, "format of the output");
            Assert.IsTrue(string.IsNullOrEmpty(reportSchedule.Output.UserName), "the security context is the authenticated owner, not the output user");
            Assert.IsFalse(report.SchedulesModified);
        }

        [TestMethod]
        public void ReportSynchronizationKeepsWebSchedules()
        {
            var schedule = createWebSchedule("kept");
            var report = Report.LoadFromFile(_repository.ReportsFolder + ReportWebPath, _repository, false);
            Assert.AreEqual(0, report.Schedules.Count);
            //Called when a report is saved: the schedules of the report not defined in the report file are deleted, except the web schedules
            report.SynchronizeTasks();
            Assert.IsTrue(File.Exists(schedule.FilePath));
        }

        [TestMethod]
        public void OwnerOfAnotherFolderIsRefused()
        {
            //A schedule file copied in the folder of another user is not executed and is disabled
            var schedule = createWebSchedule("other owner", "ex");
            ReportExecution.ExecuteReportSchedule(schedule.GUID, null);

            var saved = SealSchedule.LoadFromFile(schedule.FilePath);
            Assert.IsFalse(saved.Enabled);
            Assert.IsFalse(string.IsNullOrEmpty(saved.LastError));
            Assert.AreEqual(0, Directory.GetFiles(_personalFolder, "other owner*.csv").Length);
        }

        [TestMethod]
        public void OwnerWithoutScheduleRightIsRefused()
        {
            //The right 'Edit schedules' (or 'Edit') on the folder of the report is required at each execution
            var schedule = createWebSchedule("execute only", ExecuteLoginId, ExecuteLoginId);
            ReportExecution.ExecuteReportSchedule(schedule.GUID, null);

            var saved = SealSchedule.LoadFromFile(schedule.FilePath);
            Assert.IsFalse(saved.Enabled);
            StringAssert.Contains(saved.LastError, "no right to schedule");
        }

        [TestMethod]
        public void FolderRightOrder()
        {
            var user = getOwner();
            user.SetFolders();
            var samples = Path.Combine(_repository.ReportsFolder, "Samples");
            Assert.IsTrue(user.HasFolderRight(samples, (int)FolderRight.Schedule));
            Assert.IsTrue(user.HasFolderRight(samples, (int)FolderRight.Execute));
            Assert.IsFalse(user.HasFolderRight(samples, (int)FolderRight.Edit));

            var executeUser = getOwner(ExecuteLoginId);
            executeUser.SetFolders();
            Assert.IsTrue(executeUser.HasFolderRight(samples, (int)FolderRight.ExecuteReportOuput));
            Assert.IsFalse(executeUser.HasFolderRight(samples, (int)FolderRight.Schedule));
        }

        const string RestrictionsReportWebPath = "\\Samples\\02-Restrictions (Orders).srex";

        /// <summary>
        /// Restriction values of the report 02-Restrictions: one order, no country, orders until today (date keyword)
        /// </summary>
        static void setOrderRestrictions(WebScheduleOutput output)
        {
            var report = Report.LoadFromFile(_repository.ReportsFolder + RestrictionsReportWebPath, _repository, false);
            var restrictions = WebScheduleOutput.GetPromptedRestrictions(report);
            foreach (var restriction in restrictions.Where(i => i.DisplayNameEl == "Order ID"))
            {
                restriction.Operator = Operator.Equal;
                restriction.Value1 = "10250";
            }
            foreach (var restriction in restrictions.Where(i => i.DisplayNameEl == "Customer Country")) restriction.EnumValues.Clear();
            foreach (var restriction in restrictions.Where(i => i.DisplayNameEl == "Order Date")) restriction.Date2Keyword = "Today";
            output.SetRestrictions(report);
        }

        [TestMethod]
        public void RestrictionValuesAreApplied()
        {
            var schedule = createWebSchedule("restrictions applied", reportWebPath: RestrictionsReportWebPath, setOutput: setOrderRestrictions);
            var saved = SealSchedule.LoadFromFile(schedule.FilePath);
            Assert.IsTrue(saved.Output.Restrictions.Count > 0, "restriction values stored in the schedule file");

            var report = SealReportScheduler.GetScheduledReport(SealReportScheduler.LoadSealSchedule(schedule.GUID));
            var restrictions = WebScheduleOutput.GetPromptedRestrictions(report);
            Assert.IsTrue(restrictions.Where(i => i.DisplayNameEl == "Order ID").All(i => i.Value1 == "10250"));
            Assert.IsTrue(restrictions.Where(i => i.DisplayNameEl == "Customer Country").All(i => i.EnumValues.Count == 0));
            Assert.IsTrue(restrictions.Where(i => i.DisplayNameEl == "Order Date").All(i => i.Date2Keyword == "Today"), "date keyword kept");
            StringAssert.Contains(saved.Output.GetRestrictionsText(report), "Today");

            //The scheduled output is generated with the values
            ReportExecution.ExecuteReportSchedule(schedule.GUID, null);
            var result = SealSchedule.LoadFromFile(schedule.FilePath);
            Assert.IsTrue(string.IsNullOrEmpty(result.LastError), result.LastError);
            var content = File.ReadAllText(Directory.GetFiles(_personalFolder, "restrictions applied*.csv").Single());
            //Order 10250 only (numbers are formatted with the thousand separator of the culture)
            StringAssert.Contains(content, "Hanari Carnes");
            Assert.IsFalse(content.Contains("1997"), "other orders filtered");
        }

        [TestMethod]
        public void NotPromptedRestrictionIsIgnored()
        {
            var output = new WebScheduleOutput();
            setOrderRestrictions(output);
            var report = Report.LoadFromFile(_repository.ReportsFolder + RestrictionsReportWebPath, _repository, false);
            var orderRestrictions = report.AllRestrictions.Where(i => i.DisplayNameEl == "Order ID").ToList();
            //The designer has removed the prompt: the value of the schedule is not applied anymore
            foreach (var restriction in orderRestrictions) restriction.Prompt = PromptType.None;
            var initialValues = orderRestrictions.Select(i => i.Value1).ToList();
            output.ApplyRestrictions(report);
            CollectionAssert.AreEqual(initialValues, orderRestrictions.Select(i => i.Value1).ToList());
        }

        static string configureWebSchedule(Report report, string arguments)
        {
            var tool = new Seal.AI.AIToolConfiguration() { CurrentToolCall = new Seal.AI.AIToolCall() { Name = "report_configure_schedule", Arguments = arguments, SecurityContext = getUserWithFolders() } };
            return Seal.AI.AIReportWriter.ConfigureWebSchedule(tool, report, ReportWebPath);
        }

        static SecurityUser getUserWithFolders()
        {
            var user = getOwner();
            user.SetFolders();
            return user;
        }

        [TestMethod]
        public void AIAgentCreatesWebSchedule()
        {
            var report = Report.LoadFromFile(_repository.ReportsFolder + ReportWebPath, _repository, false);
            var fileContent = File.ReadAllText(report.FilePath);
            Directory.CreateDirectory(_personalFolder);

            //The user has the right 'Edit schedules' only: the result goes to his personal folder
            var result = configureWebSchedule(report, "{\"type\":\"weekly\",\"start_datetime\":\"2030-01-07T07:00:00\",\"weekdays\":[1],\"schedule_name\":\"AI schedule\",\"file_name\":\"ai result\",\"output_format\":\"csv\"}");
            StringAssert.Contains(result, "My schedules");
            var schedule = SealSchedule.LoadWebSchedules(getOwner()).Single(i => i.Name == "AI schedule");
            Assert.AreEqual(ReportWebPath, schedule.WebPath);
            Assert.AreEqual(LoginId, schedule.UserName);
            Assert.AreEqual(TriggerType.Weekly, schedule.Type);
            Assert.AreEqual(ReportFormat.csv, schedule.Output.Format);
            StringAssert.StartsWith(schedule.Output.FolderPath, Repository.SealPersonalRepositoryKeyword);
            Assert.AreEqual(new DateTime(2030, 1, 7, 7, 0, 0), schedule.NextExecution);
            Assert.AreEqual(fileContent, File.ReadAllText(report.FilePath), "the report file is not modified");

            //Executed by the scheduler as the schedules created from the web
            ReportExecution.ExecuteReportSchedule(schedule.GUID, null);
            var executed = SealSchedule.LoadFromFile(schedule.FilePath);
            Assert.IsTrue(string.IsNullOrEmpty(executed.LastError), executed.LastError);
            Assert.AreEqual(1, Directory.GetFiles(_personalFolder, "ai result*.csv").Length);

            //Update: only the arguments given are changed
            result = configureWebSchedule(report, $"{{\"schedule_guid\":\"{schedule.GUID}\",\"output_format\":\"html\"}}");
            Assert.IsFalse(result.StartsWith("Error"), result);
            var updated = SealSchedule.LoadWebSchedules(getOwner()).Single(i => i.GUID == schedule.GUID);
            Assert.AreEqual(ReportFormat.html, updated.Output.Format);
            Assert.AreEqual(TriggerType.Weekly, updated.Type);
            Assert.AreEqual("ai result", updated.Output.FileName);

            //A folder without the right to write is refused
            result = configureWebSchedule(report, "{\"type\":\"daily\",\"start_datetime\":\"2030-01-07T07:00:00\",\"folder_path\":\"Reports\\\\Samples\"}");
            StringAssert.StartsWith(result, "Error");
            result = configureWebSchedule(report, "{\"type\":\"daily\",\"start_datetime\":\"2030-01-07T07:00:00\",\"folder_path\":\"C:\\\\temp\"}");
            StringAssert.StartsWith(result, "Error");

            result = configureWebSchedule(report, $"{{\"action\":\"delete\",\"schedule_guid\":\"{schedule.GUID}\"}}");
            Assert.IsFalse(result.StartsWith("Error"), result);
            Assert.IsFalse(File.Exists(updated.FilePath));
        }

        [TestMethod]
        public void SystemFoldersAreHidden()
        {
            var user = getOwner();
            user.SetFolders();
            Assert.Throws<Exception>(() => user.GetFolder(SWIFolder.GetPersonalRoot() + Path.DirectorySeparatorChar + Repository.WebSchedulesFolderName));
            Assert.Throws<Exception>(() => user.GetFolder(SWIFolder.GetPersonalRoot() + Path.DirectorySeparatorChar + AgentFolders.FolderName));
            Assert.IsFalse(user.AllFolders.Any(i => i.IsPersonal && i.path.Contains(Repository.WebSchedulesFolderName)), "not in the personal tree");
        }
    }
}
