//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace Test
{
    /// <summary>
    /// Security of the navigations: sub-report folders protected once declared by a group (SecurityGroup.SubReportFolders), paths of the sub-reports and report executions kept in the repository,
    /// navigations accepted only if issued in the result of the report (Report.AllowedNavigations)
    /// </summary>
    [TestClass]
    public class TestSubReportSecurity
    {
        const string CustomerDetail = Repository.SealRepositoryKeyword + "\\SubReports\\Customer Detail.srex";

        static Repository _repository;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _repository = Repository.Instance;
        }

        static SecurityGroup newGroup(string name, params SecuritySubReportFolder[] folders)
        {
            var group = new SecurityGroup() { Name = name, GUID = name };
            group.SubReportFolders.AddRange(folders);
            return group;
        }

        static SecurityUser authenticate(string userName)
        {
            var user = new SecurityUser(_repository.Security) { WebUserName = userName, ForSchedule = true };
            user.Authenticate();
            Assert.IsTrue(string.IsNullOrEmpty(user.Error), user.Error);
            return user;
        }

        [TestMethod]
        public void SubReportFolderIsProtectedOnceDeclared()
        {
            var rh = newGroup("RH", new SecuritySubReportFolder() { Path = "\\RH" }, new SecuritySubReportFolder() { Path = "\\RH\\Salary", Execute = false });
            var sales = newGroup("Sales", new SecuritySubReportFolder() { Path = "\\Sales", UseSubFolders = false });
            var other = newGroup("Other");
            var security = new SealSecurity() { Repository = _repository };
            security.Groups.AddRange(new[] { rh, sales, other });

            //Folders declared by no group: free
            Assert.IsTrue(security.CanExecuteSubReport(new List<SecurityGroup>() { other }, "\\"));
            Assert.IsTrue(security.CanExecuteSubReport(new List<SecurityGroup>() { other }, "\\Products"));
            //Declared: only the groups having the right
            Assert.IsTrue(security.CanExecuteSubReport(new List<SecurityGroup>() { rh }, "\\RH"));
            Assert.IsTrue(security.CanExecuteSubReport(new List<SecurityGroup>() { rh }, "\\RH\\Detail"));
            Assert.IsFalse(security.CanExecuteSubReport(new List<SecurityGroup>() { other }, "\\RH"));
            Assert.IsFalse(security.CanExecuteSubReport(new List<SecurityGroup>() { sales }, "\\RH\\Detail"));
            //Sub-folder excluded by the group having the right on the parent
            Assert.IsFalse(security.CanExecuteSubReport(new List<SecurityGroup>() { rh }, "\\RH\\Salary"));
            Assert.IsFalse(security.CanExecuteSubReport(new List<SecurityGroup>() { rh }, "\\RH\\Salary\\2026"));
            //Rights of the groups are added
            Assert.IsTrue(security.CanExecuteSubReport(new List<SecurityGroup>() { other, rh }, "\\RH"));
            //Not inherited by the sub-folders: the sub-folder is not declared
            Assert.IsFalse(security.CanExecuteSubReport(new List<SecurityGroup>() { other }, "\\Sales"));
            Assert.IsTrue(security.CanExecuteSubReport(new List<SecurityGroup>() { other }, "\\Sales\\Archive"));
            //Path normalized
            Assert.IsTrue(security.CanExecuteSubReport(new List<SecurityGroup>() { rh }, "RH\\"));
        }

        [TestMethod]
        public void NavigationPathsStayInTheRepository()
        {
            var path = NavigationContext.GetSubReportPath(CustomerDetail, _repository);
            Assert.AreEqual(Path.GetFullPath(Path.Combine(_repository.SubReportsFolder, "Customer Detail.srex")), path);
            //Relative to the Reports folder
            Assert.AreEqual(Path.GetFullPath(Path.Combine(_repository.ReportsFolder, "Samples", "x.srex")), NavigationContext.GetSubReportPath("\\Samples\\x.srex", _repository));
            Assert.AreEqual(Path.GetFullPath(Path.Combine(_repository.ReportsFolder, "x.srex")), NavigationContext.GetSubReportPath("x.srex", _repository));
            //Outside the repository
            Assert.ThrowsExactly<Exception>(() => NavigationContext.GetSubReportPath(Path.Combine(Path.GetTempPath(), "x.srex"), _repository));
            Assert.ThrowsExactly<Exception>(() => NavigationContext.GetSubReportPath(Repository.SealRepositoryKeyword + "\\..\\x.srex", _repository));
            Assert.ThrowsExactly<Exception>(() => NavigationContext.GetSubReportPath("\\..\\..\\x.srex", _repository));

            //Report execution: in the Reports folder only
            Assert.AreEqual(Path.GetFullPath(Path.Combine(_repository.ReportsFolder, "Samples", "x.srex")), NavigationContext.GetReportExecutionPath("\\Samples\\x.srex", _repository));
            Assert.ThrowsExactly<Exception>(() => NavigationContext.GetReportExecutionPath("\\..\\SubReports\\Customer Detail.srex", _repository));
            Assert.ThrowsExactly<Exception>(() => NavigationContext.GetReportExecutionPath("\\..\\Personal\\other\\x.srex", _repository));
        }

        [TestMethod]
        public void NavigationReportRights()
        {
            var user = authenticate("dev");
            var subReport = NavigationContext.GetSubReportPath(CustomerDetail, _repository);
            Assert.IsTrue(user.CanExecuteNavigationReport(subReport));
            Assert.IsTrue(user.CanExecuteNavigationReport(Path.Combine(_repository.ReportsFolder, "System", "x.srex")));
            Assert.IsTrue(user.CanExecuteNavigationReport(Path.Combine(_repository.GetPersonalFolder(user), "x.srex")));
            //Personal folder of another user, other repository folders, outside the repository
            Assert.IsFalse(user.CanExecuteNavigationReport(Path.Combine(_repository.PersonalFolder, "other_user", "x.srex")));
            Assert.IsFalse(user.CanExecuteNavigationReport(Path.Combine(_repository.RepositoryPath, "Settings", "x.srex")));
            Assert.IsFalse(user.CanExecuteNavigationReport(Path.Combine(Path.GetTempPath(), "x.srex")));

            //Root of the sub-reports protected by another group
            var group = newGroup("Protection", new SecuritySubReportFolder() { Path = "\\", Execute = false });
            _repository.Security.Groups.Add(group);
            try
            {
                Assert.IsFalse(user.CanExecuteNavigationReport(subReport));
                Assert.IsTrue(_repository.Security.CheckGroupConsistency(group).Exists(i => i.Contains("all the sub-reports are protected")));
            }
            finally
            {
                _repository.Security.Groups.Remove(group);
            }
            Assert.IsTrue(user.CanExecuteNavigationReport(subReport));
        }

        [TestMethod]
        public void NavigationNotIssuedIsRefused()
        {
            var report = Report.LoadFromFile(Path.Combine(_repository.SubReportsFolder, "Product Detail.srex"), _repository);
            report.SecurityContext = authenticate("dev");
            var execution = new ReportExecution() { Report = report, RootReport = report };
            var context = new NavigationContext();
            var navigation = "rpa=" + Uri.EscapeDataString(CustomerDetail) + "&res1=guid&val1=ALFKI";

            //Forged navigation
            var ex = Assert.ThrowsExactly<Exception>(() => context.Navigate(navigation, execution, true));
            StringAssert.Contains(ex.Message, "invalid navigation");

            //Issued navigation
            report.RegisterNavigation(navigation);
            var newExecution = context.Navigate(navigation, execution, true);
            Assert.AreEqual(Path.Combine(_repository.SubReportsFolder, "Customer Detail.srex"), newExecution.Report.FilePath);
            //Another value is still refused
            Assert.ThrowsExactly<Exception>(() => context.Navigate(navigation.Replace("ALFKI", "BLAUS"), execution, true));

            //Issued but the folder is protected
            var group = newGroup("Protection", new SecuritySubReportFolder() { Path = "\\", Execute = false });
            _repository.Security.Groups.Add(group);
            try
            {
                ex = Assert.ThrowsExactly<Exception>(() => context.Navigate(navigation, execution, true));
                StringAssert.Contains(ex.Message, "no right");
            }
            finally
            {
                _repository.Security.Groups.Remove(group);
            }
        }
    }
}
