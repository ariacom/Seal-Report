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
