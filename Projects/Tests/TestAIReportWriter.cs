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
    }
}
