//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Test
{
    /// <summary>
    /// Report code levels: marking of the SQL/script properties with [SealCode] and checks of the ReportCodeGuard.
    /// </summary>
    [TestClass]
    public class TestReportCodeGuard
    {
        static string FindSourceFolder()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "SealLibraryWin");
                if (Directory.Exists(Path.Combine(candidate, "Model"))) return candidate;
                candidate = Path.Combine(dir.FullName, "Projects", "SealLibraryWin");
                if (Directory.Exists(Path.Combine(candidate, "Model"))) return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        /// <summary>
        /// Every property edited with the TemplateTextEditor or the SQLEditor (Razor script or SQL) in Seal.Model and the renderers must have the [SealCode] attribute.
        /// The editors are declared only in the Windows build, so the sources are scanned and the attribute is checked by reflection on the compiled library.
        /// </summary>
        [TestMethod]
        public void CodePropertiesHaveSealCodeAttribute()
        {
            var sourceFolder = FindSourceFolder();
            Assert.IsNotNull(sourceFolder, "SealLibraryWin source folder not found");

            var files = Directory.GetFiles(Path.Combine(sourceFolder, "Model"), "*.cs").ToList();
            files.Add(Path.Combine(sourceFolder, "Renderers", "RootRenderer.cs"));

            var classRegex = new Regex(@"^\s*public\s+(?:abstract\s+|sealed\s+|partial\s+)*class\s+(\w+)");
            var editorRegex = new Regex(@"\[Editor\(typeof\((TemplateTextEditor|SQLEditor)\)");
            var propertyRegex = new Regex(@"^\s*public\s+(?:new\s+|override\s+|virtual\s+)*[\w<>\[\],\s]+?\s(\w+)\s*(\{.*)?$");
            var assembly = typeof(Report).Assembly;
            var missing = new List<string>();
            int count = 0;

            foreach (var file in files)
            {
                string editor = null;
                bool sealCodeInSource = false;
                //Classes declared in the file (nested classes included), as compiled in SealLibrary (a Windows only class is not)
                var classNames = new List<string>();
                foreach (var line in File.ReadAllLines(file))
                {
                    var classMatch = classRegex.Match(line);
                    if (classMatch.Success) { classNames.Add(classMatch.Groups[1].Value); editor = null; continue; }
                    var editorMatch = editorRegex.Match(line);
                    if (editorMatch.Success) { editor = editorMatch.Groups[1].Value; sealCodeInSource = false; continue; }
                    if (editor == null) continue;
                    if (line.Contains("[SealCode(")) sealCodeInSource = true;
                    if (line.TrimStart().StartsWith("[") || line.TrimStart().StartsWith("#")) continue;

                    var propertyMatch = propertyRegex.Match(line);
                    if (!propertyMatch.Success) continue;
                    var propertyName = propertyMatch.Groups[1].Value;
                    count++;
                    var types = assembly.GetTypes().Where(i => classNames.Contains(i.Name)).ToList();
                    var property = types.Select(i => i.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)).LastOrDefault(i => i != null);
                    var ok = property != null ? property.GetCustomAttribute<SealCodeAttribute>(false) != null : sealCodeInSource && types.Count == 0;
                    if (!ok) missing.Add($"{classNames.LastOrDefault()}.{propertyName} ({editor}, {Path.GetFileName(file)})");
                    editor = null;
                }
            }

            Assert.IsGreaterThan(50, count, "Too few code properties found: the source scan is broken");
            Assert.IsEmpty(missing, "Properties edited as SQL or script without [SealCode]: " + string.Join(", ", missing));
        }

        const string ReportStart = @"<?xml version=""1.0""?>
<Report xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <GUID>r1</GUID>
  <Models>
    <ReportModel>
      <GUID>m1</GUID>
      <Name>model</Name>
      <Elements>
        <ReportElement>
          <GUID>e1</GUID>
          <Name>Orders.OrderID</Name>
          <DisplayName>Order</DisplayName>
";
        const string ReportEnd = @"
        </ReportElement>
      </Elements>
      <Restriction>{0}</Restriction>
    </ReportModel>
  </Models>
</Report>";

        static string GetXml(string elementContent, string restriction = "[f1] AND ([f2] OR NOT [f3])")
        {
            return ReportStart + elementContent + string.Format(ReportEnd, restriction);
        }

        [TestMethod]
        public void MetadataReportIsAccepted()
        {
            Assert.IsEmpty(ReportCodeGuard.Check(GetXml("<Format>N0</Format>"), null, ReportCodeLevel.Metadata));
        }

        [TestMethod]
        public void NewScriptIsRefused()
        {
            var xml = GetXml("<CellScript>@{ ResultCell cell = Model; }</CellScript>");
            var messages = ReportCodeGuard.Check(xml, null, ReportCodeLevel.Metadata);
            Assert.HasCount(1, messages);
            StringAssert.Contains(messages[0], "CellScript of element 'Orders.OrderID' requires the Scripts level");
            Assert.IsNotEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Sql));
            Assert.IsEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Scripts));
        }

        [TestMethod]
        public void ExistingScriptIsAccepted()
        {
            var existing = GetXml("<CellScript>@{ ResultCell cell = Model; }</CellScript>\r\n<Format>N0</Format>");
            var xml = GetXml("<CellScript>\r\n@{ ResultCell cell = Model; }  </CellScript>\r\n<Format>N2</Format>");
            Assert.IsEmpty(ReportCodeGuard.Check(xml, existing, ReportCodeLevel.Metadata));
            //Modified script
            var modified = GetXml("<CellScript>@{ ResultCell cell = Model; cell.Value = 1; }</CellScript>");
            Assert.IsNotEmpty(ReportCodeGuard.Check(modified, existing, ReportCodeLevel.Metadata));
            //Removed script
            Assert.IsEmpty(ReportCodeGuard.Check(GetXml(""), existing, ReportCodeLevel.Metadata));
        }

        [TestMethod]
        public void CustomSqlRequiresSqlLevel()
        {
            var xml = GetXml("<SQL>Orders.Freight * 2</SQL>");
            Assert.IsNotEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Metadata));
            Assert.IsEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Sql));
        }

        [TestMethod]
        public void ElementNameIsNotSql()
        {
            //ReportElement inherits MetaColumn: its Name is not the SQL of the column
            Assert.IsEmpty(ReportCodeGuard.Check(GetXml("<Format>N0</Format>").Replace("Orders.OrderID", "1=1; DROP TABLE x"), null, ReportCodeLevel.Metadata));
        }

        [TestMethod]
        public void RestrictionExpressionLevels()
        {
            Assert.IsEmpty(ReportCodeGuard.Check(GetXml("", "[a-1] AND\r\n([b-2] OR NOT [c3])"), null, ReportCodeLevel.Metadata));
            var xml = GetXml("", "[a-1] AND 1=1");
            Assert.IsNotEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Metadata));
            Assert.IsEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Sql));
            //Same expression with other GUIDs in the saved report
            Assert.IsEmpty(ReportCodeGuard.Check(xml, GetXml("", "[x-9] AND 1=1"), ReportCodeLevel.Metadata));
        }

        [TestMethod]
        public void RazorTextRequiresScriptsLevel()
        {
            var xml = GetXml("<Format>N0</Format>").Replace("<DisplayName>Order</DisplayName>", "<DisplayName>@Raw(\"x\")</DisplayName>");
            Assert.IsNotEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Sql));
            Assert.IsEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Sql, new[] { "@Raw(\"x\")" }));
        }

        [TestMethod]
        public void TableTemplateRequiresScriptsLevel()
        {
            var xml = @"<Report><Sources><ReportSource><GUID>s1</GUID><Name>src</Name><MetaData><Tables><MetaTable><GUID>t1</GUID><Alias>SealMasterTable</Alias>
<TemplateName>CSV</TemplateName><Parameters><Parameter><Name>file_path</Name><Value>C:\secret.csv</Value></Parameter></Parameters></MetaTable></Tables></MetaData></ReportSource></Sources></Report>";
            var messages = ReportCodeGuard.Check(xml, null, ReportCodeLevel.Sql);
            Assert.HasCount(2, messages);
            StringAssert.Contains(messages[0], "TemplateName of table 'SealMasterTable' requires the Scripts level");
            StringAssert.Contains(messages[1], "Value of parameter 'file_path' requires the Scripts level");
            Assert.IsEmpty(ReportCodeGuard.Check(xml, xml, ReportCodeLevel.Metadata));
        }

        [TestMethod]
        public void ActiveHtmlRequiresScriptsLevel()
        {
            Func<string, string> report = guide => $"<Report><Guide>{System.Security.SecurityElement.Escape(guide)}</Guide></Report>";
            Assert.IsEmpty(ReportCodeGuard.Check(report("<b>Select a country</b> then <a href='https://sealreport.org'>execute</a>"), null, ReportCodeLevel.Metadata));
            foreach (var active in new[] { "<script>alert(1)</script>", "<img src=x onerror=alert(1)>", "<a href='javascript:alert(1)'>x</a>", "<iframe src='x'></iframe>" })
                Assert.IsNotEmpty(ReportCodeGuard.Check(report(active), null, ReportCodeLevel.Sql), active);
        }

        [TestMethod]
        public void RawViewParameterValues()
        {
            //The view templates of the in-repo repository (Debug): ChartJS renders chartjs_point_radius without encoding, chartjs_title is encoded with Helper.ToJS
            Repository.Create();
            var template = RepositoryServer.ViewTemplates.First(i => i.Name == "Chart JS");
            Assert.Contains("chartjs_point_radius", template.RawParameterNames);
            Assert.Contains("chartjs_colors", template.RawParameterNames);
            Assert.DoesNotContain("chartjs_title", template.RawParameterNames);

            Func<string, string, string> view = (name, value) => $"<Report><Views><ReportView><GUID>v1</GUID><Name>chart</Name><TemplateName>Chart JS</TemplateName><Parameters><Parameter><Name>{name}</Name><Value>{System.Security.SecurityElement.Escape(value)}</Value></Parameter></Parameters></ReportView></Views></Report>";
            Assert.IsEmpty(ReportCodeGuard.Check(view("chartjs_point_radius", "3"), null, ReportCodeLevel.Metadata));
            Assert.IsEmpty(ReportCodeGuard.Check(view("chartjs_colors", "d3c"), null, ReportCodeLevel.Metadata));
            Assert.IsEmpty(ReportCodeGuard.Check(view("chartjs_title", "Nombre d'entreprises (\"total\")"), null, ReportCodeLevel.Metadata));
            var xml = view("chartjs_point_radius", "1,onClick:function(){alert(1)}");
            var messages = ReportCodeGuard.Check(xml, null, ReportCodeLevel.Sql);
            Assert.HasCount(1, messages);
            StringAssert.Contains(messages[0], "parameter 'chartjs_point_radius' of view 'chart' is rendered as code");
            Assert.IsEmpty(ReportCodeGuard.Check(xml, xml, ReportCodeLevel.Metadata));
            Assert.IsEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Scripts));
        }

        [TestMethod]
        public void ConnectionAndJoinInReport()
        {
            var xml = @"<Report><Sources><ReportSource><GUID>s1</GUID><Name>src</Name>
<Connections><MetaConnection><GUID>c1</GUID><Name>cnx</Name><DatabaseType>MSSQLServer</DatabaseType></MetaConnection></Connections>
<MetaData><Tables><MetaTable><GUID>t1</GUID><Alias>SealMasterTable</Alias><DynamicColumns>true</DynamicColumns></MetaTable></Tables>
<Joins><MetaJoin><GUID>j1</GUID><Name>join</Name></MetaJoin></Joins></MetaData></ReportSource></Sources></Report>";
            var messages = ReportCodeGuard.Check(xml, null, ReportCodeLevel.Sql);
            Assert.HasCount(1, messages);
            StringAssert.Contains(messages[0], "connection 'cnx' requires the Scripts level");
            messages = ReportCodeGuard.Check(xml, null, ReportCodeLevel.Metadata);
            Assert.HasCount(2, messages);
            //Same connection and join in the saved report
            Assert.IsEmpty(ReportCodeGuard.Check(xml, xml.Replace("\r\n", "\n  "), ReportCodeLevel.Metadata));
            //An empty master table is allowed, a table with columns requires Sql
            var table = Regex.Replace(Regex.Replace(xml, "<Connections>.*</Connections>", ""), "<Joins>.*</Joins>", "");
            Assert.IsEmpty(ReportCodeGuard.Check(table, null, ReportCodeLevel.Metadata));
            table = table.Replace("<DynamicColumns>true</DynamicColumns>", "<Columns><MetaColumn><GUID>k1</GUID><Name>secret.password</Name></MetaColumn></Columns>");
            Assert.IsNotEmpty(ReportCodeGuard.Check(table, null, ReportCodeLevel.Metadata));
            Assert.IsEmpty(ReportCodeGuard.Check(table, null, ReportCodeLevel.Sql));
        }
    }
}
