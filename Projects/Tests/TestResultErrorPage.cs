//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Model;
using System.IO;

namespace Test
{
    /// <summary>
    /// Page written as result when a renderer fails (e.g. Chromium not started for the PDF): the detail of the error is not shown to the users of the
    /// Web Report Server, except those allowed to see the error details
    /// </summary>
    [TestClass]
    public class TestResultErrorPage
    {
        const string StackTrace = "Failed to launch browser! No usable sandbox! #0 0x6084a3a69f42 base::debug::CollectStackTrace() <script>";

        static Report loadReport()
        {
            var repository = Repository.Create();
            var report = Report.LoadFromFile(Path.Combine(repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex"), repository);
            return report;
        }

        [TestMethod]
        public void WebUserWithoutDetail()
        {
            var report = loadReport();
            report.WebUrl = "https://server/donnees/";
            var page = report.GetResultErrorPage(StackTrace);
            Assert.IsTrue(page.StartsWith("<!DOCTYPE html>"));
            Assert.IsTrue(page.Contains("&#39;TST040 Complete dashboard&#39; could not be generated in the format"), page);
            Assert.IsFalse(page.Contains("sandbox"), "the detail must not be shown: " + page);
            Assert.IsTrue(page.Contains("rel='icon' href='img/favicon.ico'"), page);
        }

        [TestMethod]
        public void WebUserWithDetailAndDesigner()
        {
            var report = loadReport();
            report.WebUrl = "https://server/donnees/";
            report.ForceShowErrorMessages = true;
            var page = report.GetResultErrorPage(StackTrace);
            Assert.IsTrue(page.Contains("No usable sandbox"), page);
            Assert.IsFalse(page.Contains("<script>"), "the detail must be encoded: " + page);

            //Out of the Web Report Server (designer, runner): detail shown, no icon
            report = loadReport();
            page = report.GetResultErrorPage(StackTrace);
            Assert.IsTrue(page.Contains("No usable sandbox"), page);
            Assert.IsFalse(page.Contains("favicon"), page);
        }
    }
}
