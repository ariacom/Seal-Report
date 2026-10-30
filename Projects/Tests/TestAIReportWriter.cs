//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.AI;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Test
{
    /// <summary>
    /// Writer of the reports of the AI tools report_create_from_xml and report_patch_xml: patch operations, internal GUIDs and validation of the data source references
    /// </summary>
    [TestClass]
    public class TestAIReportWriter
    {
        static Repository _repository;
        static MetaSource _northwind;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _repository = Repository.Create();
            _northwind = _repository.Sources.First(i => i.Name == "Northwind");
        }

        static string ColumnGuid(string name) => _northwind.MetaData.Tables.SelectMany(i => i.Columns).First(i => i.Name == name).GUID;

        static string ReportXml(string columnGuid, string restrictionGuid = "f1") => $@"<?xml version=""1.0""?>
<Report xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <GUID>a</GUID><ViewGUID>b</ViewGUID><DisplayName>Test</DisplayName>
  <Sources><ReportSource><GUID>c</GUID><Name>Northwind</Name><ConnectionGUID>1</ConnectionGUID><MetaSourceGUID>{_northwind.GUID}</MetaSourceGUID></ReportSource></Sources>
  <Models><ReportModel><GUID>d</GUID><Name>Orders</Name><SourceGUID>c</SourceGUID>
    <Elements><ReportElement><GUID>e1</GUID><Name>Orders.ShipName</Name><PivotPosition>Row</PivotPosition><MetaColumnGUID>{columnGuid}</MetaColumnGUID></ReportElement></Elements>
    <Restriction>[{restrictionGuid}]</Restriction>
    <Restrictions><ReportRestriction><GUID>f1</GUID><Name>Orders.ShipName</Name><MetaColumnGUID>{columnGuid}</MetaColumnGUID><Operator>Contains</Operator><Value1>Ernst</Value1></ReportRestriction></Restrictions>
  </ReportModel></Models>
  <Views><ReportView><GUID>b</GUID><Name>View</Name><TemplateName>Report</TemplateName><Views>
    <ReportView><GUID>h</GUID><Name>Orders</Name><TemplateName>Model</TemplateName><ModelGUID>d</ModelGUID></ReportView>
  </Views></ReportView></Views>
</Report>";

        static string Validate(XDocument doc, bool all = true, List<XElement> inserted = null)
        {
            return new AIReportWriter(null).Validate(doc, null, all, inserted, false, new List<string>(), out _);
        }

        [TestMethod]
        public void ExecutionHasTheUserOfTheToolCall()
        {
            var user = new SecurityUser(_repository.Security) { Name = "ai_user" };
            user.AddSecurityGroup(_repository.Security.Groups.First().Name);
            var tool = new AIToolConfiguration() { CurrentToolCall = new AIToolCall() { Name = "report_execute_from_xml", Arguments = "{}", SecurityContext = user } };
            var result = new AIReportWriter(tool).ExecuteFromXml(ReportXml(ColumnGuid("Orders.ShipName")), null);
            StringAssert.Contains(result, "User:'ai_user'");
        }

        [TestMethod]
        public void MedianOfTheResultValues()
        {
            Assert.AreEqual(3m, ReportModel.GetMedian(new List<decimal> { 5, 1, 3 }));
            Assert.AreEqual(2.5m, ReportModel.GetMedian(new List<decimal> { 4, 1, 3, 2 }));
            Assert.AreEqual(7m, ReportModel.GetMedian(new List<decimal> { 7 }));
        }

        [TestMethod]
        public void PartialEnumLabelIsMapped()
        {
            var column = _northwind.MetaData.Tables.SelectMany(i => i.Columns).First(i => i.Enum?.Name == "Ship Country");
            var doc = XDocument.Parse(ReportXml(column.GUID).Replace("Orders.ShipName", column.Name).Replace("<Operator>Contains</Operator><Value1>Ernst</Value1>", "<Prompt>Prompt</Prompt><Operator>Equal</Operator><EnumValues><string>Germ</string></EnumValues>"));
            var warnings = new List<string>();
            Assert.IsNull(new AIReportWriter(null).Validate(doc, null, true, null, false, warnings, out _));
            Assert.AreEqual("Germany", doc.Descendants("EnumValues").Single().Elements("string").Single().Value);
            Assert.AreEqual(0, warnings.Count);

            //Several labels: no choice, the warning lists them
            doc = XDocument.Parse(ReportXml(column.GUID).Replace("Orders.ShipName", column.Name).Replace("<Operator>Contains</Operator><Value1>Ernst</Value1>", "<Prompt>Prompt</Prompt><Operator>Equal</Operator><EnumValues><string>an</string></EnumValues>"));
            Assert.IsNull(new AIReportWriter(null).Validate(doc, null, true, null, false, warnings, out _));
            Assert.AreEqual("an", doc.Descendants("EnumValues").Single().Elements("string").Single().Value);
            Assert.IsTrue(warnings.Single().Contains("ambiguous"), warnings.Single());
        }

        [TestMethod]
        public void PartialEnumLabelPrefersTheWholeWord()
        {
            var values = new List<MetaEV> { new MetaEV() { Id = "01" }, new MetaEV() { Id = "35" }, new MetaEV() { Id = "49" } };
            var labels = new Dictionary<string, string> { { "01", "01 - Ain" }, { "35", "35 - Ille-et-Vilaine" }, { "49", "49 - Maine-et-Loire" } };
            Assert.AreEqual("01", AIReportWriter.PreferWholeWord(values, i => labels[i.Id], "ain").Single().Id);
            //No whole word: all the values stay, the caller tells the ambiguity
            Assert.AreEqual(3, AIReportWriter.PreferWholeWord(values, i => labels[i.Id], "e").Count);
        }

        [TestMethod]
        public void PartialCriterionNameIsAccepted()
        {
            var xml = ReportXml(ColumnGuid("Orders.ShipName")).Replace("<Name>Orders.ShipName</Name><MetaColumnGUID>", "<Name>Orders.ShipName</Name><DisplayName>Name of the ship</DisplayName><MetaColumnGUID>").Replace("<Operator>Contains</Operator>", "<Prompt>Prompt</Prompt><Operator>Contains</Operator>");
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".srex");
            System.IO.File.WriteAllText(path, xml);
            try
            {
                var report = Report.LoadFromFile(path, _repository, false);
                Assert.IsNull(AIReportWriter.ApplyCriteria(report, @"[{""name"":""the ship"",""values"":[""Hanari""]}]"));
                Assert.AreEqual("Hanari", report.AllRestrictions.Single().Value1);
                Assert.IsTrue(AIReportWriter.ApplyCriteria(report, @"[{""name"":""Unknown"",""values"":[""x""]}]").StartsWith("Error: unknown criterion"));
            }
            finally { System.IO.File.Delete(path); }
        }

        [TestMethod]
        public void ValidReportGetsNewGuidsAndLimit()
        {
            var doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipName")));
            Assert.IsNull(Validate(doc));

            var root = doc.Root;
            var model = root.Descendants("ReportModel").Single();
            var modelGuid = (string)model.Element("GUID");
            Assert.IsTrue(Guid.TryParse(modelGuid, out _));
            Assert.AreEqual(modelGuid, (string)root.Descendants("ReportView").Single(i => (string)i.Element("TemplateName") == "Model").Element("ModelGUID"));
            Assert.AreEqual((string)root.Element("ViewGUID"), (string)root.Element("Views").Element("ReportView").Element("GUID"));
            Assert.AreEqual((string)root.Descendants("ReportSource").Single().Element("GUID"), (string)model.Element("SourceGUID"));
            var restrictionGuid = (string)root.Descendants("ReportRestriction").Single().Element("GUID");
            Assert.AreEqual("[" + restrictionGuid + "]", (string)model.Element("Restriction"));
            //External references kept
            Assert.AreEqual("1", (string)root.Descendants("ConnectionGUID").Single());
            Assert.AreEqual(_northwind.GUID, (string)root.Descendants("MetaSourceGUID").Single());
            //Default record limit and prompted restriction
            Assert.AreEqual(AIReportWriter.DefaultMaxNumberOfRecords.ToString(), (string)model.Element("MaxNumberOfRecords"));
            Assert.AreEqual("Prompt", (string)root.Descendants("ReportRestriction").Single().Element("Prompt"));
        }

        [TestMethod]
        public void InvalidReferencesAreRejected()
        {
            //Fabricated column GUID (the name of the element does not match a column display name either)
            var doc = XDocument.Parse(ReportXml("00000000-0000-0000-0000-000000000001").Replace("<Name>Orders.ShipName</Name>", "<Name>Unknown.Column</Name>"));
            StringAssert.Contains(Validate(doc), "are not columns of source");

            //GUID of another column
            doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipCity")));
            StringAssert.Contains(Validate(doc), "designate different columns");

            //Restriction not used by the expression
            doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipName"), "other"));
            StringAssert.Contains(Validate(doc), "not referenced in the <Restriction> expression");

            //Unknown data source
            doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipName")).Replace(_northwind.GUID, Guid.NewGuid().ToString()));
            StringAssert.Contains(Validate(doc), "does not match any data source");

            //View on no model
            doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipName")).Replace("<ModelGUID>d</ModelGUID>", "<ModelGUID>zz</ModelGUID>"));
            StringAssert.Contains(Validate(doc), "broken internal references");
        }

        [TestMethod]
        public void PatchOperations()
        {
            var doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipName")));
            Assert.IsNull(Validate(doc));
            var savedGuids = doc.Descendants("GUID").Select(i => i.Value).ToList();
            var elementGuid = (string)doc.Descendants("ReportElement").Single().Element("GUID");

            var inserted = new List<XElement>();
            var error = AIReportWriter.ApplyPatch(doc, new List<AIPatchOperation>()
            {
                new AIPatchOperation() { Op = "set", XPath = "/Report/DisplayName", Value = "Patched" },
                //Missing element created in its parent
                new AIPatchOperation() { Op = "set", XPath = "/Report/Models/ReportModel[Name='Orders']/ShowFirstLine", Value = "false" },
                //New element with a placeholder GUID, and a copy of an existing element with its GUID
                new AIPatchOperation() { Op = "insert", XPath = "//ReportModel[Name='Orders']/Elements", Xml = $"<ReportElement><GUID>e9</GUID><Name>Orders.ShipCity</Name><PivotPosition>Row</PivotPosition><MetaColumnGUID>{ColumnGuid("Orders.ShipCity")}</MetaColumnGUID></ReportElement>" },
                new AIPatchOperation() { Op = "insert", XPath = "//ReportElement[Name='Orders.ShipName']", Position = "after", Xml = $"<ReportElement><GUID>{elementGuid}</GUID><Name>Orders.ShipName</Name><PivotPosition>Row</PivotPosition><MetaColumnGUID>{ColumnGuid("Orders.ShipName")}</MetaColumnGUID></ReportElement>" },
                new AIPatchOperation() { Op = "remove", XPath = "//ReportRestriction/Value1" },
            }, inserted);
            Assert.IsNull(error, error);
            Assert.AreEqual(2, inserted.Count);
            Assert.AreEqual("Patched", (string)doc.Root.Element("DisplayName"));
            Assert.AreEqual("false", (string)doc.Descendants("ShowFirstLine").Single());
            Assert.AreEqual(0, doc.Descendants("Value1").Count());

            //Patch: the GUIDs of the report are kept, the placeholder and the duplicated GUID are regenerated
            Assert.IsNull(Validate(doc, false, inserted));
            var guids = doc.Descendants("GUID").Select(i => i.Value).ToList();
            foreach (var guid in savedGuids) CollectionAssert.Contains(guids, guid);
            Assert.AreEqual(guids.Count, guids.Distinct().Count(), "duplicated GUID");
            Assert.IsTrue(guids.All(i => Guid.TryParse(i, out _)));
        }

        [TestMethod]
        public void PatchErrors()
        {
            var doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipName")));
            StringAssert.Contains(AIReportWriter.ApplyPatch(doc, new List<AIPatchOperation>() { new AIPatchOperation() { Op = "remove", XPath = "//NoSuchElement" } }, new List<XElement>()), "matches nothing");
            StringAssert.Contains(AIReportWriter.ApplyPatch(doc, new List<AIPatchOperation>() { new AIPatchOperation() { Op = "set", XPath = "//ReportModel", Value = "x" } }, new List<XElement>()), "has child elements");
            StringAssert.Contains(AIReportWriter.ApplyPatch(doc, new List<AIPatchOperation>() { new AIPatchOperation() { Op = "insert", XPath = "//Elements", Xml = "<ReportElement>" } }, new List<XElement>()), "invalid 'xml'");
            StringAssert.Contains(AIReportWriter.ApplyPatch(doc, new List<AIPatchOperation>() { new AIPatchOperation() { Op = "set", XPath = "//[", Value = "x" } }, new List<XElement>()), "invalid XPath");
            StringAssert.Contains(AIReportWriter.ApplyPatch(doc, new List<AIPatchOperation>() { new AIPatchOperation() { Op = "remove", XPath = "/Report" } }, new List<XElement>()), "root element");
            StringAssert.Contains(AIReportWriter.ApplyPatch(doc, new List<AIPatchOperation>() { new AIPatchOperation() { Op = "rename", XPath = "/Report" } }, new List<XElement>()), "must be set, insert or remove");

            var operations = AIReportWriter.ParseOperations("[{\"op\":\"set\",\"xpath\":\"/Report/DisplayName\",\"value\":\"X\"},{\"op\":\"insert\",\"xpath\":\"//Elements\",\"xml\":\"<ReportElement/>\",\"position\":\"prepend\"}]");
            Assert.AreEqual(2, operations.Count);
            Assert.AreEqual("prepend", operations[1].Position);
        }

        [TestMethod]
        public void EmptyCharts()
        {
            //Dashboard of TST040: every chart view has a serie of its engine
            var report = Report.LoadFromFile(System.IO.Path.Combine(_repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex"), _repository);
            Assert.AreEqual(0, AIReportWriter.EmptyCharts(report).Count);

            //Without the ECharts serie, the 'Chart Echarts' view of the model is empty (the Chart JS pie is not)
            var model = report.Models.First(i => i.Name == "Sales per category");
            foreach (var element in model.Elements) element.EChartsSerie = EChartsSerieDefinition.None;
            var charts = AIReportWriter.EmptyCharts(report);
            Assert.AreEqual(1, charts.Count);
            StringAssert.Contains(charts[0], "[Chart Echarts] of model 'Sales per category'");
            StringAssert.Contains(charts[0], "<EChartsSerie>");
        }

        [TestMethod]
        public void ChartWiring()
        {
            //Dashboard of TST040 and the sample reports: charts correctly built
            var report = Report.LoadFromFile(System.IO.Path.Combine(_repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex"), _repository);
            Assert.AreEqual(0, AIReportWriter.ChartWiring(report).Count);
            foreach (var path in System.IO.Directory.GetFiles(System.IO.Path.Combine(_repository.ReportsFolder, "Samples"), "*.srex", System.IO.SearchOption.AllDirectories)
                .Concat(System.IO.Directory.GetFiles(System.IO.Path.Combine(_repository.ReportsFolder, "System", "Tests"), "*.srex", System.IO.SearchOption.AllDirectories)))
            {
                var issues = AIReportWriter.ChartWiring(Report.LoadFromFile(path, _repository));
                Assert.AreEqual(0, issues.Count, path + ": " + string.Join("; ", issues));
            }

            //Serie definitions on the measures instead of the dimension (as written by an agent): the Data elements are reported, and the chart has no X axis
            var model = report.Models.First(i => i.Name == "Sales per category");
            var axis = model.Elements.First(i => i.SerieDefinition == SerieDefinition.Axis);
            axis.SerieDefinition = SerieDefinition.None;
            var measure = model.Elements.First(i => i.PivotPosition == PivotPosition.Data);
            measure.SerieDefinition = SerieDefinition.Splitter;
            var issues2 = AIReportWriter.ChartWiring(report);
            Assert.IsTrue(issues2.Exists(i => i.Contains($"'{measure.Name}' of model 'Sales per category' is a Data element") && i.Contains("<SerieDefinition>Splitter</SerieDefinition>")), string.Join("; ", issues2));
            Assert.IsTrue(issues2.Exists(i => i.Contains("[Chart JS] of model 'Sales per category' has no X axis") && i.Contains($"'{axis.Name}'")), string.Join("; ", issues2));

            //Secondary Y axis title without any serie on the secondary axis
            measure.SerieDefinition = SerieDefinition.None;
            axis.SerieDefinition = SerieDefinition.Axis;
            Assert.AreEqual(0, AIReportWriter.ChartWiring(report).Count);
            var modelView = report.AllViews.First(i => i.TemplateName == ReportViewTemplate.ModelName && i.Model == model);
            IEnumerable<ReportView> descendants(ReportView view) => view.Views.SelectMany(i => new[] { i }.Concat(descendants(i)));
            var chartView = descendants(modelView).First(i => i.TemplateName == ReportViewTemplate.ChartJSName);
            var y2Title = chartView.Parameters.FirstOrDefault(i => i.Name == "chartjs_yaxis2_title");
            if (y2Title == null) chartView.Parameters.Add(y2Title = new Parameter() { Name = "chartjs_yaxis2_title" });
            y2Title.Value = "Population";
            var issues3 = AIReportWriter.ChartWiring(report);
            Assert.AreEqual(1, issues3.Count, string.Join("; ", issues3));
            StringAssert.Contains(issues3[0], "chartjs_yaxis2_title");
            measure.YAxisType = AxisType.Secondary;
            Assert.AreEqual(0, AIReportWriter.ChartWiring(report).Count);
        }

        [TestMethod]
        public void ViewOrder()
        {
            //Dashboard of TST040: no chart below a table
            var report = Report.LoadFromFile(System.IO.Path.Combine(_repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex"), _repository);
            Assert.AreEqual(0, AIReportWriter.ViewOrder(report).Count, string.Join("; ", AIReportWriter.ViewOrder(report)));

            //Sample 05-Maps: map then table in the Container; the map moved after the table (a map appended by an agent) is reported
            report = Report.LoadFromFile(System.IO.Path.Combine(_repository.ReportsFolder, "Samples", "05-Maps.srex"), _repository);
            Assert.AreEqual(0, AIReportWriter.ViewOrder(report).Count, string.Join("; ", AIReportWriter.ViewOrder(report)));
            var container = report.AllViews.First(i => i.TemplateName == ReportViewTemplate.ContainerName && i.Views.Exists(j => j.TemplateName == ReportViewTemplate.MapName));
            var map = container.Views.First(i => i.TemplateName == ReportViewTemplate.MapName);
            map.SortOrder = container.Views.Max(i => i.SortOrder) + 1;
            var issues = AIReportWriter.ViewOrder(report);
            Assert.AreEqual(1, issues.Count, string.Join("; ", issues));
            StringAssert.Contains(issues[0], $"'{map.Name}' [Map] is shown below the table");
        }

        [TestMethod]
        public void ColorMappings()
        {
            //Sample 05-Maps executed: map colored by 'Customer Country'
            var report = Report.LoadFromFile(System.IO.Path.Combine(_repository.ReportsFolder, "Samples", "05-Maps.srex"), _repository);
            var execution = new ReportExecution() { Report = report };
            execution.Execute();
            while (report.IsExecuting) System.Threading.Thread.Sleep(100);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
            var map = report.AllViews.First(i => i.TemplateName == ReportViewTemplate.MapName && i.HasValue("map_color_column"));
            var colors = map.Parameters.FirstOrDefault(i => i.Name == "map_colors");
            if (colors == null) map.Parameters.Add(colors = new Parameter() { Name = "map_colors" });

            //No 'value=color' entry (palette only): nothing to check
            colors.Value = "#1f77b4\n#ff7f0e";
            Assert.AreEqual(0, AIReportWriter.ColorMappings(report).Count);

            //Keys that are not displayed values (labels invented by an agent): reported with the displayed values
            colors.Value = "Faible=#2ca02c\nFort=#d62728";
            var issues = AIReportWriter.ColorMappings(report);
            Assert.AreEqual(1, issues.Count, string.Join("; ", issues));
            StringAssert.Contains(issues[0], "no key of map_colors ('Faible', 'Fort') matches a displayed value");
            StringAssert.Contains(issues[0], "displayed:");

            //One key matching a value of the color column is enough
            colors.Value = "Faible=#2ca02c\nFrance=#d62728";
            Assert.AreEqual(0, AIReportWriter.ColorMappings(report).Count, string.Join("; ", AIReportWriter.ColorMappings(report)));
        }

        [TestMethod]
        public void UnreadableXmlIsRejected()
        {
            var doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipName")));
            Assert.IsNull(Validate(doc));
            Assert.IsNull(AIReportWriter.CheckDeserialization(AIReportWriter.ToXmlText(doc)));

            //A label in an enumerated element: the cause and the line are given, not only the position
            doc.Descendants("ReportRestriction").Single().Element("Prompt").Value = "Département";
            var error = AIReportWriter.CheckDeserialization(AIReportWriter.ToXmlText(doc));
            StringAssert.Contains(error, "<Prompt>Département</Prompt>");
            StringAssert.Contains(error, "PromptType");
        }
    }
}
