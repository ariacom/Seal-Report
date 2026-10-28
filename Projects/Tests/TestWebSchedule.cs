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

        static SealSchedule createWebSchedule(string fileName, string userName = LoginId, string folderLoginId = LoginId)
        {
            var report = Report.LoadFromFile(_repository.ReportsFolder + ReportWebPath, _repository, false);
            var schedule = new SealSchedule()
            {
                GUID = Guid.NewGuid().ToString(),
                Name = "Test " + fileName,
                UserName = userName,
                WebPath = ReportWebPath,
                ReportGUID = report.GUID,
                ReportPath = ReportWebPath,
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
