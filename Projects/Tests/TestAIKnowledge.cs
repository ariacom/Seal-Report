//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.AI;
using Seal.Model;
using System.Collections.Generic;
using System.IO;

namespace Test
{
    /// <summary>
    /// Read-only knowledge tools of the AI agents: seal_read / seal_search (knowledge folders only) and seal_describe_class
    /// </summary>
    [TestClass]
    public class TestAIKnowledge
    {
        static Repository _repository;
        static readonly List<string> _folders = new List<string>(AIServerConfiguration.DefaultKnowledgeFolders.Split("\r\n"));

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _repository = Repository.Create();
        }

        static bool IsError(string result) => result.StartsWith("Error:");

        [TestMethod]
        public void ReadKnowledgeFiles()
        {
            var result = AIKnowledge.Read(_repository, _folders, @"Views\Model.cshtml");
            Assert.IsFalse(IsError(result), result);
            StringAssert.StartsWith(result, @"File 'Views");

            result = AIKnowledge.Read(_repository, _folders, @"Reports\Samples\11-Tab Control - Model Layout.srex");
            Assert.IsFalse(IsError(result), result);
            StringAssert.Contains(result, "<Report");

            result = AIKnowledge.Read(_repository, _folders, @"Settings\AI\Skills\build-report-from-metadata.md");
            Assert.IsFalse(IsError(result), result);

            //'/' separators and a '..' that stays in the knowledge folders
            result = AIKnowledge.Read(_repository, _folders, "Views/Renderers/../Model.cshtml");
            Assert.IsFalse(IsError(result), result);

            //A folder lists its content
            result = AIKnowledge.Read(_repository, _folders, @"Reports\Samples");
            StringAssert.Contains(result, "11-Tab Control - Model Layout.srex");
        }

        [TestMethod]
        public void ReadPaging()
        {
            var result = AIKnowledge.Read(_repository, _folders, @"Reports\Samples\11-Tab Control - Model Layout.srex", 1, 10);
            StringAssert.Contains(result, "lines 1-10 of");
            StringAssert.Contains(result, "offset=11");
            result = AIKnowledge.Read(_repository, _folders, @"Reports\Samples\11-Tab Control - Model Layout.srex", 100000, 10);
            Assert.IsTrue(IsError(result), result);
        }

        [TestMethod]
        public void ReadOutsideKnowledgeFoldersIsDenied()
        {
            foreach (var path in new[] {
                @"Sources\Northwind.scfx",
                @"Views\..\Sources\Northwind.scfx",
                "Views/../Security/Security.xml",
                @"Security\Security.xml",
                @"Settings\AI\AIConfiguration.xml",
                @"Settings\AI\Skills\..\AIConfiguration.xml",
                @"Settings\AI\Skills\..\AIConfiguration_AIProviders.xml",
                @"Settings\Configuration.xml",
                @"Logs",
                @"Reports\Search - Orders.srex",
                @"..\Projects\Tests\TestAIKnowledge.cs",
                @"..\..\Windows\win.ini",
                @"C:\Windows\win.ini",
                @"Views\Model.cshtml:stream",
                @"Views\*.cshtml",
                ""
            })
            {
                var result = AIKnowledge.Read(_repository, _folders, path);
                Assert.IsTrue(IsError(result), $"'{path}' must be refused: {result}");
                Assert.IsFalse(result.Contains("<?xml"), path);
            }
        }

        [TestMethod]
        public void ForbiddenFoldersEvenIfListed()
        {
            var folders = new List<string>() { "Sources", "Security", @"Settings\AI", "Logs", "Settings", @"..\Projects" };
            Assert.AreEqual(0, AIKnowledge.GetAllowedFolders(_repository, folders).Count);
            Assert.IsTrue(IsError(AIKnowledge.Read(_repository, folders, @"Sources\Northwind.scfx")));
            Assert.IsTrue(IsError(AIKnowledge.Read(_repository, folders, @"Settings\AI\AIConfiguration.xml")));

            //The repository root: allowed, but its forbidden folders are not
            folders = new List<string>() { "." };
            Assert.IsFalse(IsError(AIKnowledge.Read(_repository, folders, @"Views\Model.cshtml")));
            Assert.IsTrue(IsError(AIKnowledge.Read(_repository, folders, @"Sources\Northwind.scfx")));
            Assert.IsTrue(IsError(AIKnowledge.Read(_repository, folders, @"Security\Security.xml")));
            Assert.IsFalse(IsError(AIKnowledge.Read(_repository, folders, @"Settings\AI\Skills\style-report-view.md")));
            var search = AIKnowledge.Search(_repository, folders, "ConnectionString", null, "*.scfx");
            Assert.IsFalse(search.Contains(@"Sources\"), search);
        }

        [TestMethod]
        public void Search()
        {
            var result = AIKnowledge.Search(_repository, _folders, "<TemplateName>Tab Page</TemplateName>", @"Reports\Samples", "*.srex");
            Assert.IsFalse(IsError(result), result);
            //file:line: text
            StringAssert.Matches(result, new System.Text.RegularExpressions.Regex(@"Reports\\Samples\\11-Tab Control - Model Layout\.srex:\d+: "));

            Assert.IsTrue(IsError(AIKnowledge.Search(_repository, _folders, "GUID", "Sources")));
            Assert.IsTrue(IsError(AIKnowledge.Search(_repository, _folders, "GUID", @"Views\..\Security")));
            Assert.IsTrue(IsError(AIKnowledge.Search(_repository, _folders, "(unclosed")));
            StringAssert.StartsWith(AIKnowledge.Search(_repository, _folders, "zzqx_no_match_qxzz"), "No match");
        }

        [TestMethod]
        public void DescribeClass()
        {
            var result = AIKnowledge.DescribeClass("ReportElement");
            Assert.IsFalse(IsError(result), result);
            StringAssert.Contains(result, "# Class ReportElement");
            StringAssert.Contains(result, "- PivotPosition | enum PivotPosition | Row");
            StringAssert.Contains(result, "- MetaColumnGUID | string");
            StringAssert.Contains(result, "PivotPosition: Page");
            //Code level of a SQL property
            StringAssert.Contains(result, "- SQL | string |  | Sql");

            result = AIKnowledge.DescribeClass("reportrestriction");
            StringAssert.Contains(result, "# Class ReportRestriction");
            StringAssert.Contains(result, "Operator");

            //Class level
            StringAssert.Contains(AIKnowledge.DescribeClass("MetaJoin"), "requires the 'Sql' code level");
            StringAssert.Contains(AIKnowledge.DescribeClass("ReportView"), "list of <ReportView>");

            //Not a class of a report
            Assert.IsTrue(IsError(AIKnowledge.DescribeClass("Repository")));
            Assert.IsTrue(IsError(AIKnowledge.DescribeClass("SecurityUser")));
        }
    }
}
