//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.AI;
using Seal.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Test
{
    /// <summary>
    /// SealSecurity.CheckConsistency: rules of the consistency of the security groups with the AI Agents, and consistency of the security files of the repository (Security.xml for the development, SecurityORI.xml installed by the setup)
    /// </summary>
    [TestClass]
    public class TestSecurityConsistency
    {
        const string ReportDesignerGUID = "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d";
        const string DataSourceManagerGUID = "e5d4a000-1111-4aaa-bbbb-d5e0a6e70001";
        const string ScheduleOutputGUID = "b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e";

        static Repository _repository;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _repository = Repository.Instance;
        }

        static string securityFile(string name)
        {
            return Path.Combine(Path.GetDirectoryName(_repository.SecurityPath), name);
        }

        /// <summary>
        /// Elements of the file not deserialized (obsolete or misspelled properties)
        /// </summary>
        static List<string> unknownElements(string path)
        {
            var result = new List<string>();
            var serializer = new XmlSerializer(typeof(SealSecurity));
            serializer.UnknownElement += (s, e) => result.Add(e.Element.Name);
            using (var xr = XmlReader.Create(path)) serializer.Deserialize(xr);
            //SqlModel is still read for the compatibility but replaced by ReportCodeLevel
            if (File.ReadAllText(path).Contains("<SqlModel>")) result.Add("SqlModel");
            return result;
        }

        static SealSecurity newSecurity(SecurityGroup group)
        {
            var security = new SealSecurity() { Repository = _repository };
            security.Groups.Add(group);
            return security;
        }

        static SecurityGroup newGroup(string agentGUID)
        {
            var group = new SecurityGroup() { Name = "Test", GUID = "test" };
            group.Folders.Add(new SecurityFolder());
            group.AgentGUIDs.Add(agentGUID);
            return group;
        }

        [TestMethod]
        [DataRow("Security.xml")]
        [DataRow("SecurityORI.xml")]
        public void SecurityFileIsConsistent(string fileName)
        {
            var path = securityFile(fileName);
            Assert.IsTrue(File.Exists(path), path);

            var unknown = unknownElements(path);
            Assert.IsEmpty(unknown, $"{fileName}: obsolete elements {string.Join(", ", unknown)}");

            var security = SealSecurity.LoadFromFile(path, false);
            security.Repository = _repository;
            var warnings = security.CheckConsistency();
            //Unknown agents, data sources or devices, and sensitive repository folders are never expected
            var errors = warnings.Where(i => i.Contains("does not exist") || i.Contains("should not be published") || i.Contains("is ignored")).ToList();
            Assert.IsEmpty(errors, $"{fileName}:\r\n{string.Join("\r\n", errors)}");

            //The default group (first group, see SecurityUser.AddDefaultSecurityGroup) has no warning: all its agents can use their tools
            var defaultGroup = security.Groups.First();
            Assert.AreEqual(fileName == "Security.xml" ? "Developers" : "Default Group", defaultGroup.Name);
            var defaultWarnings = security.CheckGroupConsistency(defaultGroup);
            Assert.IsEmpty(defaultWarnings, $"{fileName}:\r\n{string.Join("\r\n", defaultWarnings)}");
        }

        [TestMethod]
        public void SecurityFolderPathsUseTheSeparatorOfTheOS()
        {
            //Security.xml is written on Windows with '\': on Linux the folders are '/System', and before the conversion no folder
            //matched, so every folder took the rights of the root folder (System and Scrapers visible to a subscriber in PROD).
            //Mirrored here on Windows: paths written with '/' must match the folders '\...'.
            var group = new SecurityGroup() { Name = "Test", GUID = "test" };
            group.Folders.Add(new SecurityFolder() { FolderRight = FolderRight.Execute });
            group.Folders.Add(new SecurityFolder() { Path = "/System", FolderRight = FolderRight.None });
            group.Folders.Add(new SecurityFolder() { Path = "/Catalogue", FolderRight = FolderRight.Schedule, Icon = "fa-solid fa-book-atlas" });
            var security = newSecurity(group);
            var sep = Path.DirectorySeparatorChar;

            var system = security.FindSecurityFolder(security.Groups, sep + "System");
            Assert.AreEqual(FolderRight.None, system.FolderRight, "folder written with the other separator");
            Assert.IsTrue(system.IsDefined);
            Assert.AreEqual(FolderRight.None, security.FindSecurityFolder(security.Groups, sep + "System" + sep + "Tests").FolderRight, "sub-folder inherits the folder, not the root");
            var catalogue = security.FindSecurityFolder(security.Groups, sep + "Catalogue");
            Assert.AreEqual(FolderRight.Schedule, catalogue.FolderRight);
            Assert.AreEqual("fa-solid fa-book-atlas", catalogue.Icon, "icon of the folder kept");
            Assert.AreEqual(FolderRight.Execute, security.FindSecurityFolder(security.Groups, sep + "Population").FolderRight, "other folders: root");
        }

        [TestMethod]
        public void FolderPathWrittenDifferentlyKeepsItsRights()
        {
            //PROD 2026-11-17: a guest listed and executed the reports of '/System' with the path '/./System' or '//System' (no security folder
            //matched, the root right Execute applied), and could create, rename or delete folders with the right Execute only.
            var group = new SecurityGroup() { Name = "Test", GUID = "test" };
            group.Folders.Add(new SecurityFolder() { FolderRight = FolderRight.Execute });
            group.Folders.Add(new SecurityFolder() { Path = "\\System", FolderRight = FolderRight.None });
            group.Folders.Add(new SecurityFolder() { Path = "\\Shared", FolderRight = FolderRight.Edit });
            var security = newSecurity(group);
            var user = new SecurityUser(security);
            user.SecurityGroups.Add(group);
            var sep = Path.DirectorySeparatorChar.ToString();

            foreach (var path in new[] { "/./System", "//System", "/System//Tests", "/System.", "/System ", "/System/.", "/System::$INDEX_ALLOCATION", ":/./x", "~/./Security" })
            {
                var ex = Assert.ThrowsExactly<System.Exception>(() => user.GetFolder(path), path);
                Assert.AreEqual("Error: invalid path", ex.Message, path);
            }
            Assert.AreEqual(0, user.GetFolder("/System").right);
            Assert.AreEqual(0, user.GetFolder("/System/Tests").right, "sub-folder inherits the folder, not the root");
            Assert.AreEqual(0, user.GetFolder("/System/").right, "trailing separator");
            if (sep == "\\") Assert.AreEqual(0, user.GetFolder("\\system\\Tests").right, "Windows: the case of the folder name is ignored");

            //Sub-folders managed only with the right Edit
            Assert.AreEqual(0, user.GetFolder("/Catalogue").manage, "Execute: no folder management");
            Assert.AreEqual(0, user.GetFolder("/System/Tests").manage, "None: no folder management");
            Assert.AreEqual(1, user.GetFolder("/Shared").manage, "Edit, folder defined: create only");
            Assert.AreEqual(2, user.GetFolder("/Shared/Sub").manage, "Edit: create, rename, delete");
        }

        [TestMethod]
        public void DataSourceManagerRequiresSourcesWrite()
        {
            var group = newGroup(DataSourceManagerGUID);
            group.ReportCodeLevel = ReportCodeLevel.Sql;
            var folder = new SecurityRepositoryFolder() { Path = @"\Sources", FolderRight = RepositoryFolderRight.ReadOnly };
            group.RepositoryFolders.Add(folder);
            var security = newSecurity(group);
            Assert.IsTrue(security.CheckGroupConsistency(group).Exists(i => i.Contains("edits the data sources")));

            folder.FolderRight = RepositoryFolderRight.ReadWrite;
            Assert.IsFalse(security.CheckGroupConsistency(group).Exists(i => i.Contains("edits the data sources")));
        }

        [TestMethod]
        public void SqlSkillRequiresSqlLevel()
        {
            var group = newGroup(ReportDesignerGUID);
            group.ReportCodeLevel = ReportCodeLevel.Metadata;
            var security = newSecurity(group);
            Assert.IsTrue(security.CheckGroupConsistency(group).Exists(i => i.Contains("creates reports from SQL")));

            group.ReportCodeLevel = ReportCodeLevel.Sql;
            Assert.IsEmpty(security.CheckGroupConsistency(group));
        }

        [TestMethod]
        public void ReportWriteRequiresEditFolder()
        {
            var group = newGroup(ScheduleOutputGUID);
            group.ReportCodeLevel = ReportCodeLevel.Sql;
            group.Folders[0].FolderRight = FolderRight.Execute;
            var security = newSecurity(group);
            Assert.IsTrue(security.CheckGroupConsistency(group).Exists(i => i.Contains("writes reports")));

            group.PersFolderRight = PersonalFolderRight.Reports;
            Assert.IsEmpty(security.CheckGroupConsistency(group));
        }

        static string executeSecurityCheck(string userName, string scope)
        {
            var tool = _repository.AIConfiguration.AITools.First(i => i.Name == "security_check");
            var agent = new AIAgentConfiguration() { Name = "Test agent", ToolGUIDs = new List<string>() { tool.GUID } };
            //Authenticated from the name only (as for the MCP server)
            var user = new SecurityUser(_repository.Security) { WebUserName = userName, ForSchedule = true };
            user.Authenticate();
            Assert.IsTrue(string.IsNullOrEmpty(user.Error), user.Error);
            user.SetFolders();
            var host = new AIToolHost(agent) { SecurityContext = user };
            return host.Execute(new AIToolCall() { Name = "security_check", Arguments = $"{{\"scope\":\"{scope}\"}}" }).Text;
        }

        [TestMethod]
        public void SecurityCheckTool()
        {
            //Developer login of Security.xml: group Developers, no warning
            var result = executeSecurityCheck("dev", "mine");
            StringAssert.Contains(result, "Default group (highest weight, gives the AI agents): Developers");
            StringAssert.Contains(result, "Report code level: Scripts");
            StringAssert.Contains(result, "Web schedules available: True");
            StringAssert.Contains(result, "No warning.");

            result = executeSecurityCheck("dev", "all");
            StringAssert.Contains(result, $"Consistency of the security ({_repository.Security.Groups.Count} groups)");

            //All the groups only with the right to execute the System reports
            result = executeSecurityCheck("startupfolder", "all");
            StringAssert.StartsWith(result, "Error: checking all the security groups requires");
        }

        [TestMethod]
        public void SensitiveFoldersAndUnknownItems()
        {
            var group = newGroup("unknown-agent");
            group.RepositoryFolders.Add(new SecurityRepositoryFolder() { Path = @"\SpecialFolders", FolderRight = RepositoryFolderRight.ReadOnly });
            group.RepositoryFolders.Add(new SecurityRepositoryFolder() { Path = @"Security\Providers", FolderRight = RepositoryFolderRight.ReadOnly });
            group.RepositoryFolders.Add(new SecurityRepositoryFolder() { Path = @"\Logs", FolderRight = RepositoryFolderRight.ReadWrite });
            group.OutputDeviceGUIDs.Add("unknown-device");
            var warnings = newSecurity(group).CheckGroupConsistency(group);
            Assert.AreEqual(2, warnings.Count(i => i.Contains("should not be published")), string.Join("\r\n", warnings));
            Assert.IsTrue(warnings.Exists(i => i.Contains("AI agent 'unknown-agent' does not exist")));
            Assert.IsTrue(warnings.Exists(i => i.Contains("output device 'unknown-device' does not exist")));
        }
    }
}
