//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.AI;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.IO;
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
        public void PathWithJsonEscapesIsRestored()
        {
            //"Personal\new.srex" sent unescaped in the JSON of the tool call arrives with a new line
            Assert.AreEqual("Personal\\new.srex", AIReportWriter.RestoreJsonEscapes("Personal\new.srex"));
            Assert.AreEqual("Personal\\top\\report.srex", AIReportWriter.RestoreJsonEscapes("Personal\top\report.srex"));
            Assert.AreEqual("Reports\\Sales\\a.srex", AIReportWriter.RestoreJsonEscapes("Reports\\Sales\\a.srex"));
            Assert.IsNull(AIReportWriter.RestoreJsonEscapes(null));
        }

        [TestMethod]
        public void SourceResolvedByNameOrByDefault()
        {
            var available = _repository.Sources.ToList();
            //By GUID, by name in <MetaSourceGUID>, by the <Name> of the report source when <MetaSourceGUID> is empty or unknown
            Assert.AreEqual(_northwind, AIReportWriter.ResolveMetaSource(_northwind.GUID.ToUpper(), null, available));
            Assert.AreEqual(_northwind, AIReportWriter.ResolveMetaSource("northwind", null, available));
            Assert.AreEqual(_northwind, AIReportWriter.ResolveMetaSource("", "Northwind", available));
            Assert.AreEqual(_northwind, AIReportWriter.ResolveMetaSource("not-a-guid", "Northwind", available));
            //Empty with one source only: this source; with several sources: no choice
            Assert.AreEqual(_northwind, AIReportWriter.ResolveMetaSource("", "My source", new List<MetaSource> { _northwind }));
            Assert.IsNull(AIReportWriter.ResolveMetaSource("", "My source", available));
            //Unknown value: no guess, even with one source
            Assert.IsNull(AIReportWriter.ResolveMetaSource("not-a-guid", "My source", new List<MetaSource> { _northwind }));
            //Ambiguous name: no choice
            var twin = new MetaSource() { Name = "Northwind", GUID = Guid.NewGuid().ToString() };
            Assert.IsNull(AIReportWriter.ResolveMetaSource("Northwind", null, new List<MetaSource> { _northwind, twin }));
        }

        [TestMethod]
        public void SourceNameIsReplacedByItsGuid()
        {
            var doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipName")).Replace($"<MetaSourceGUID>{_northwind.GUID}</MetaSourceGUID>", "<MetaSourceGUID></MetaSourceGUID>"));
            Assert.IsNull(Validate(doc));
            Assert.AreEqual(_northwind.GUID, (string)doc.Descendants("MetaSourceGUID").Single());
        }

        [TestMethod]
        public void UnknownSourceErrorListsTheSources()
        {
            var doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipName")).Replace($"<MetaSourceGUID>{_northwind.GUID}</MetaSourceGUID>", "<MetaSourceGUID>x1</MetaSourceGUID>").Replace("<Name>Northwind</Name>", "<Name>Unknown</Name>"));
            var error = Validate(doc);
            StringAssert.StartsWith(error, "Error: MetaSourceGUID 'x1' does not match");
            StringAssert.Contains(error, $"'Northwind' ({_northwind.GUID})");
            //More than 15 sources: the list is cut
            var many = Enumerable.Range(1, 20).Select(i => new MetaSource() { Name = "S" + i, GUID = "g" + i }).ToList();
            error = AIReportWriter.UnknownSourceError("", many);
            StringAssert.Contains(error, "'S15' (g15)");
            Assert.IsFalse(error.Contains("'S16'"), error);
            StringAssert.Contains(error, "5 more in datasource_list");
        }

        [TestMethod]
        public void InventedStructureIsExplained()
        {
            //Seen with gpt-5.4-mini: a valid GUID in a structure that is not a Seal report
            var doc = XDocument.Parse($"<Report><MetaSourceGUID>{_northwind.GUID}</MetaSourceGUID><Name>Test</Name><Models><Model><Name>Orders</Name><DataItems /></Model></Models></Report>");
            var error = Validate(doc);
            StringAssert.StartsWith(error, "Error: the XML has no <Sources><ReportSource> element");
            StringAssert.Contains(error, "(<MetaSourceGUID>, <Name> do not exist)");
            StringAssert.Contains(error, "build-report-from-metadata");
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
        public void ModelNameIgnoresTheCase()
        {
            //Seen with GPT-6 Luna: 'Chiffres clés Finances' for the model 'Chiffres clés finances'
            var user = new SecurityUser(_repository.Security) { Name = "ai_user" };
            user.AddSecurityGroup(_repository.Security.Groups.First().Name);
            var tool = new AIToolConfiguration() { CurrentToolCall = new AIToolCall() { Name = "report_execute_from_xml", Arguments = "{}", SecurityContext = user } };
            var result = new AIReportWriter(tool).ExecuteFromXml(ReportXml(ColumnGuid("Orders.ShipName")), " oRDERS ");
            StringAssert.Contains(result, "=== Model: Orders ===");
            result = new AIReportWriter(tool).ExecuteFromXml(ReportXml(ColumnGuid("Orders.ShipName")), "Customers");
            StringAssert.Contains(result, "No model named 'Customers' found. Available: Orders.");
        }

        [TestMethod]
        public void RequiredCriterionIsEnforced()
        {
            //As the Web Report Server: a required criterion without value is refused (the result would cover the whole data)
            var user = new SecurityUser(_repository.Security) { Name = "ai_user" };
            user.AddSecurityGroup(_repository.Security.Groups.First().Name);
            var tool = new AIToolConfiguration() { CurrentToolCall = new AIToolCall() { Name = "report_execute_from_xml", Arguments = "{}", SecurityContext = user } };
            var xml = ReportXml(ColumnGuid("Orders.ShipName")).Replace("<Operator>Contains</Operator><Value1>Ernst</Value1>", "<Prompt>Prompt</Prompt><Required>true</Required><Operator>Contains</Operator>");
            var result = new AIReportWriter(tool).ExecuteFromXml(xml, null);
            StringAssert.Contains(result, "requires a value for the criteria");
            Assert.IsFalse(result.Contains("=== Model:"), result);
            //With a value: executed
            result = new AIReportWriter(tool).ExecuteFromXml(xml.Replace("<Operator>Contains</Operator>", "<Operator>Contains</Operator><Value1>Ernst</Value1>"), null);
            StringAssert.Contains(result, "=== Model: Orders ===");

            //Required criterion on a model without element (the '_Reference' model of a Fiche), also when one model is asked
            var reference = $"<ReportModel><GUID>r</GUID><Name>_Reference</Name><SourceGUID>c</SourceGUID><Restriction>[f9]</Restriction><Restrictions><ReportRestriction><GUID>f9</GUID><Name>Orders.ShipName</Name><MetaColumnGUID>{ColumnGuid("Orders.ShipName")}</MetaColumnGUID><Prompt>Prompt</Prompt><Required>true</Required><Operator>Contains</Operator></ReportRestriction></Restrictions></ReportModel>";
            var xmlReference = ReportXml(ColumnGuid("Orders.ShipName")).Replace("</ReportModel></Models>", "</ReportModel>" + reference + "</Models>");
            result = new AIReportWriter(tool).ExecuteFromXml(xmlReference, "Orders");
            StringAssert.Contains(result, "requires a value for the criteria");
        }

        [TestMethod]
        public void IgnoredEnumValueIsReported()
        {
            //A report written by hand with Value1 on an enumerated list: the engine ignores the value, the execution messages say it
            var column = _northwind.MetaData.Tables.SelectMany(i => i.Columns).First(i => i.Enum?.Name == "Ship Country");
            var xml = ReportXml(column.GUID).Replace("Orders.ShipName", column.Name).Replace("<Operator>Contains</Operator><Value1>Ernst</Value1>", "<Value1>Germany</Value1>");
            var path = Path.Combine(Path.GetTempPath(), $"ignored_enum_{Guid.NewGuid():N}.srex");
            File.WriteAllText(path, xml);
            try
            {
                var report = Report.LoadFromFile(path, _repository);
                var restriction = report.Models[0].Restrictions[0];
                StringAssert.Contains(restriction.IgnoredEnumValueMessage, "its value 'Germany' is ignored");
                report.SkipExternalViewer = true;
                new ReportExecution() { Report = report }.Execute();
                while (report.IsExecuting) System.Threading.Thread.Sleep(50);
                StringAssert.Contains(report.ExecutionMessages, "its value 'Germany' is ignored");
                //With the value selected in the list: no message
                restriction.EnumValues.Add("Germany");
                Assert.IsNull(restriction.IgnoredEnumValueMessage);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void PartialResultIsFlagged()
        {
            //The 'Maximum number of records' of the model is reached: the agent is told the result is partial
            var user = new SecurityUser(_repository.Security) { Name = "ai_user" };
            user.AddSecurityGroup(_repository.Security.Groups.First().Name);
            var tool = new AIToolConfiguration() { CurrentToolCall = new AIToolCall() { Name = "report_execute_from_xml", Arguments = "{}", SecurityContext = user } };
            var xml = ReportXml(ColumnGuid("Orders.ShipName")).Replace("<Value1>Ernst</Value1>", "<Value1>a</Value1>").Replace("<SourceGUID>c</SourceGUID>", "<SourceGUID>c</SourceGUID><MaxNumberOfRecords>3</MaxNumberOfRecords><SqlOrderBy>-- unsorted</SqlOrderBy>");
            var result = new AIReportWriter(tool).ExecuteFromXml(xml, null);
            StringAssert.Contains(result, "WARNING: only 3 rows were loaded (maximum number of records of the report)");
            //Under the limit: no warning
            result = new AIReportWriter(tool).ExecuteFromXml(xml.Replace("<MaxNumberOfRecords>3</MaxNumberOfRecords>", "<MaxNumberOfRecords>5000</MaxNumberOfRecords>"), null);
            Assert.IsFalse(result.Contains("WARNING: only"), result);
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
        public void CriterionOperatorMustBeAllowed()
        {
            var column = _northwind.MetaData.Tables.SelectMany(i => i.Columns).First(i => i.Enum?.Name == "Ship Country");
            var xml = ReportXml(column.GUID).Replace("Orders.ShipName", column.Name).Replace("<Operator>Contains</Operator><Value1>Ernst</Value1>", "<Prompt>Prompt</Prompt><Operator>Equal</Operator>");
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".srex");
            System.IO.File.WriteAllText(path, xml);
            try
            {
                var report = Report.LoadFromFile(path, _repository, false);
                var restriction = report.AllRestrictions.Single();
                var error = AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""{column.Name}"",""operator"":""StartsWith"",""values"":[""Germany""]}}]");
                Assert.IsTrue(error.StartsWith("Error: the operator 'StartsWith' is not allowed"), error);
                Assert.AreEqual(Operator.Equal, restriction.Operator);

                Assert.IsNull(AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""{column.Name}"",""operator"":""NotEqual"",""values"":[""Germany""]}}]"));
                Assert.AreEqual(Operator.NotEqual, restriction.Operator);
            }
            finally { System.IO.File.Delete(path); }
        }

        [TestMethod]
        public void UnknownEnumValueGivesCloseValues()
        {
            Assert.AreEqual(1, AIReportWriter.LevenshteinDistance("thionvile", "thionville"));
            var values = new List<MetaEV> { new MetaEV() { Id = "78337", Val = "Limetz-Villez (78337)" } };
            Assert.IsFalse(AIReportWriter.IsWholeWord(values, i => i.Val, "Metz-Ville"), "a value inside a word");
            Assert.IsTrue(AIReportWriter.IsWholeWord(values, i => i.Val, "Limetz"));

            var column = _northwind.MetaData.Tables.SelectMany(i => i.Columns).First(i => i.Enum?.Name == "Ship Country");
            var xml = ReportXml(column.GUID).Replace("Orders.ShipName", column.Name).Replace("<Operator>Contains</Operator><Value1>Ernst</Value1>", "<Prompt>Prompt</Prompt><Operator>Equal</Operator>");
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".srex");
            System.IO.File.WriteAllText(path, xml);
            try
            {
                var report = Report.LoadFromFile(path, _repository, false);
                //Spelling mistake: the close value is given
                var error = AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""{column.Name}"",""values"":[""Germny""]}}]");
                StringAssert.Contains(error, "Close values: 'Germany'");
                //Nothing close (e.g. a code instead of a name): some values of the list are shown
                error = AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""{column.Name}"",""values"":[""67""]}}]");
                StringAssert.Contains(error, "The list has ");
                //A value inside a word only is not chosen silently
                error = AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""{column.Name}"",""values"":[""ermany""]}}]");
                Assert.IsNotNull(error);
                StringAssert.Contains(error, "Close values: 'Germany'");
            }
            finally { System.IO.File.Delete(path); }
        }

        [TestMethod]
        public void DisplayValueNotFoundByTheSearchIsAccepted()
        {
            //List loaded on demand whose display value is 'Name (id)' while its SQL searches the name only: the display value typed
            //as a whole finds nothing, it was accepted only if another execution had already loaded it (refused just after a restart)
            var report = Report.LoadFromFile(Path.Combine(_repository.ReportsFolder, "Samples", "09-Enumerated List - Dynamic Display.srex"), _repository);
            var enumRE = report.Sources.SelectMany(i => i.MetaData.Enums).Single(i => i.Name == "Product Name Search On Demand");
            enumRE.SqlDisplay = "SELECT TOP 100 ProductID, ProductName + ' (' + CAST(ProductID AS varchar(10)) + ')' FROM Products WHERE ProductName LIKE '%{EnumFilter}%' ORDER BY ProductName";
            enumRE.SqlSelected = "SELECT ProductID, ProductName + ' (' + CAST(ProductID AS varchar(10)) + ')' FROM Products WHERE ProductID IN ({EnumSelected})";
            Assert.IsTrue(enumRE.IsLoadOnDemand);
            Assert.IsFalse(enumRE.Values.Exists(i => i.Id == "1"), "the list starts empty");

            Assert.IsNull(AIReportWriter.ApplyCriteria(report, @"[{""name"":""Product Name Search (on demand)"",""values"":[""Chai (1)""]}]"));
            var restriction = report.AllRestrictions.First(i => i.DisplayNameEl == "Product Name Search (on demand)");
            CollectionAssert.AreEqual(new List<string> { "1" }, restriction.EnumValues);
            Assert.AreEqual("Chai (1)", enumRE.GetDisplayValue("1", null, true));

            //A display value that does not exist is still refused, with the close values
            var error = AIReportWriter.ApplyCriteria(report, @"[{""name"":""Product Name Search (on demand)"",""values"":[""Chai (99)""]}]");
            StringAssert.Contains(error, "Close values: 'Chai (1)'");
        }

        [TestMethod]
        public void CloseValuesStartWithTheText()
        {
            var values = new List<MetaEV> { new MetaEV() { Id = "52070", Val = "Brennes (52070)" }, new MetaEV() { Id = "88164", Val = "Estrennes (88164)" },
                new MetaEV() { Id = "35210", Val = "Saint-Rennes (35210)" }, new MetaEV() { Id = "35238", Val = "Rennes (35238)" } };
            //Equal label, then almost equal (one edit), then starting with the text as a whole word, then containing it
            CollectionAssert.AreEqual(new List<string> { "35238", "52070", "35210", "88164" }, AIReportWriter.OrderByCloseness(values, i => i.Val, "rennes").Select(i => i.Id).ToList());
        }

        [TestMethod]
        public void CloseValuesRankSpellingMistakesFirst()
        {
            Assert.AreEqual("Name", AIReportWriter.LabelWithoutCode("Name (A12)"));
            Assert.AreEqual("Name", AIReportWriter.LabelWithoutCode("01 - Name"));
            Assert.AreEqual("Two - Words", AIReportWriter.LabelWithoutCode("Two - Words"));
            Assert.AreEqual("A12", AIReportWriter.GetCodeInParentheses("Some Name (A12)", out string label));
            Assert.AreEqual("Some Name", label);
            Assert.IsNull(AIReportWriter.GetCodeInParentheses("Some Name", out _));
            Assert.AreEqual("saint etienne sur l eau", AIReportWriter.NormalizeLabel("Saint-Étienne-sur-l'Eau"));
            Assert.AreEqual(1, AIReportWriter.DamerauLevenshteinDistance("valence", "valecne"), "transposition");
            Assert.AreEqual(2, AIReportWriter.LevenshteinDistance("valence", "valecne"));

            var values = new List<MetaEV> { new MetaEV() { Id = "1", Val = "Northbrook (1)" }, new MetaEV() { Id = "2", Val = "Upper-Northbrooks (2)" },
                new MetaEV() { Id = "3", Val = "Northbrookfield (3)" }, new MetaEV() { Id = "4", Val = "Northbrooks (4)" },
                new MetaEV() { Id = "5", Val = "Westvale (5)" }, new MetaEV() { Id = "6", Val = "Westvalley (6)" }, new MetaEV() { Id = "7", Val = "Eastvale (7)" } };
            //One letter missing: the value with one edit first, before the values only starting with or containing the text
            Assert.AreEqual("4", AIReportWriter.CloseValues(values, i => i.Val, "Northbrook").Skip(1).First().Id);
            Assert.AreEqual("1", AIReportWriter.CloseValues(values, i => i.Val, "Northbrok").First().Id);
            Assert.AreEqual("4", AIReportWriter.CloseValues(values, i => i.Val, "Northbrook (99)").Skip(1).First().Id);
            //Spelling mistake: 'Westvale', not 'Westvalley' or 'Eastvale' (3 edits)
            CollectionAssert.AreEqual(new List<string> { "5" }, AIReportWriter.CloseValues(values, i => i.Val, "Westvole").Select(i => i.Id).ToList());
            //Inverted letters: one edit
            Assert.AreEqual("5", AIReportWriter.CloseValues(values, i => i.Val, "Wsetvale").First().Id);
            //Nothing close
            Assert.AreEqual(0, AIReportWriter.CloseValues(values, i => i.Val, "Southport").Count);
        }

        static Report LoadOnDemandProductReport()
        {
            var report = Report.LoadFromFile(Path.Combine(_repository.ReportsFolder, "Samples", "09-Enumerated List - Dynamic Display.srex"), _repository);
            var enumRE = report.Sources.SelectMany(i => i.MetaData.Enums).Single(i => i.Name == "Product Name Search On Demand");
            enumRE.SqlDisplay = "SELECT TOP 100 ProductID, ProductName + ' (' + CAST(ProductID AS varchar(10)) + ')' FROM Products WHERE ProductName LIKE '%{EnumFilter}%' ORDER BY ProductName";
            enumRE.SqlSelected = "SELECT ProductID, ProductName + ' (' + CAST(ProductID AS varchar(10)) + ')' FROM Products WHERE ProductID IN ({EnumSelected})";
            return report;
        }

        static string ApplyProduct(Report report, string value) => AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""Product Name Search (on demand)"",""values"":[""{value}""]}}]");

        [TestMethod]
        public void EnumValueWithCodeOrLabelIsResolved()
        {
            var report = LoadOnDemandProductReport();
            var restriction = report.AllRestrictions.First(i => i.DisplayNameEl == "Product Name Search (on demand)");
            //'label (code)' with a misspelled label: the code in parentheses identifies the value
            Assert.IsNull(ApplyProduct(report, "Chnag (2)"));
            CollectionAssert.AreEqual(new List<string> { "2" }, restriction.EnumValues);
            //The label of one value only, without its code, ignoring the case
            Assert.IsNull(ApplyProduct(report, "tofu"));
            CollectionAssert.AreEqual(new List<string> { "14" }, restriction.EnumValues);
            //A code that does not exist: refused, the value having the label is proposed
            StringAssert.Contains(ApplyProduct(report, "Tofu (999)"), "Close values: 'Tofu (14)'");
        }

        [TestMethod]
        public void PartialEnumLabelIsRefusedWithCloseValues()
        {
            //Tool criteria: a part of a label is no longer chosen silently (another value may be the one asked), the close values are given
            var report = LoadOnDemandProductReport();
            var error = ApplyProduct(report, "Longlife");
            StringAssert.StartsWith(error, "Error: the value 'Longlife' is not in the list");
            StringAssert.Contains(error, "Close values: 'Longlife Tofu (74)'");
            //Spelling mistake in a list loaded on demand: the searches with a shorter text find the value
            error = ApplyProduct(report, "Pavlowa");
            StringAssert.Contains(error, "Close values: 'Pavlova (16)'");
        }

        [TestMethod]
        public void CodeAloneIsResolved()
        {
            //List whose identifier is the display value 'label (code)': the code alone identifies the value (it was refused without close value)
            var column = _northwind.MetaData.Tables.SelectMany(i => i.Columns).First(i => i.Enum?.Name == "Ship Country");
            var xml = ReportXml(column.GUID).Replace("Orders.ShipName", column.Name).Replace("<Operator>Contains</Operator><Value1>Ernst</Value1>", "<Prompt>Prompt</Prompt><Operator>Equal</Operator>");
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".srex");
            File.WriteAllText(path, xml);
            try
            {
                var report = Report.LoadFromFile(path, _repository, false);
                var enumRE = report.AllRestrictions.Single().EnumRE;
                enumRE.Values.AddRange(new[] { "North (59)", "Northern Hills (590)", "East (99)", "West (99)" }.Select(i => new MetaEV() { Id = i }));

                Assert.IsNull(AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""{column.Name}"",""values"":[""59""]}}]"));
                CollectionAssert.AreEqual(new List<string> { "North (59)" }, report.AllRestrictions.Single().EnumValues);
                //Several values with the code: ambiguous, they are listed
                var error = AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""{column.Name}"",""values"":[""99""]}}]");
                StringAssert.Contains(error, "is ambiguous");
                StringAssert.Contains(error, "'West (99)'");
                //A code that no value has: still refused
                StringAssert.StartsWith(AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""{column.Name}"",""values"":[""5""]}}]"), "Error: the value '5' is not in the list");
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void LabelWithOtherSeparatorsIsFoundByItsWords()
        {
            //Separators typed differing from those of the list ('Chef Anton s' for 'Chef Anton's') and a first word too common to find the value
            //within the rows returned by the search (one row here): the search with another word of the label finds it
            var report = LoadOnDemandProductReport();
            var enumRE = report.Sources.SelectMany(i => i.MetaData.Enums).Single(i => i.Name == "Product Name Search On Demand");
            enumRE.SqlDisplay = enumRE.SqlDisplay.Replace("SELECT TOP 100", "SELECT TOP 1");
            Assert.IsNull(ApplyProduct(report, "Chef Anton s Gumbo Mix"));
            CollectionAssert.AreEqual(new List<string> { "5" }, report.AllRestrictions.First(i => i.DisplayNameEl == "Product Name Search (on demand)").EnumValues);
        }

        const string HomonymProductsSql = "SELECT TOP 2 ProductID, ProductName + ' (' + CAST(ProductID AS varchar(10)) + ')' FROM (SELECT ProductID, ProductName FROM Products UNION ALL SELECT ProductID + 1000, ProductName FROM Products) p WHERE ProductName LIKE '%{EnumFilter}%' ORDER BY ProductName";

        [TestMethod]
        public void HomonymsFoundByTheSearchesAreAmbiguous()
        {
            //Two values with the same label (ids 5 and 1005) found by a search with a word: ambiguous, never the first one found
            var report = LoadOnDemandProductReport();
            var enumRE = report.Sources.SelectMany(i => i.MetaData.Enums).Single(i => i.Name == "Product Name Search On Demand");
            enumRE.SqlDisplay = HomonymProductsSql;
            var error = ApplyProduct(report, "Chef Anton s Gumbo Mix");
            StringAssert.Contains(error, "is ambiguous");
            StringAssert.Contains(error, "(id 1005)");

            //One homonym already in the shared list (loaded by another execution): the search with its label finds the other one
            report = LoadOnDemandProductReport();
            enumRE = report.Sources.SelectMany(i => i.MetaData.Enums).Single(i => i.Name == "Product Name Search On Demand");
            enumRE.SqlDisplay = HomonymProductsSql;
            enumRE.AddValues(new List<MetaEV> { new MetaEV() { Id = "5", Val = "Chef Anton's Gumbo Mix (5)" } });
            error = ApplyProduct(report, "Chef Anton s Gumbo Mix");
            StringAssert.Contains(error, "is ambiguous");
            StringAssert.Contains(error, "(id 1005)");
        }

        [TestMethod]
        public void AlmostEqualValueDoesNotStopTheSearchByWords()
        {
            //'Saint Martin': the search of the first word returns 'Saint-Marcan', almost equal (2 edits); the search stopped there and the
            //value asked, found only by another word, was never searched. Here the search returns one row, sorted descending:
            //'Chef Anton' and 'Anton' return the decoy 'Chef Anton's Jumbo Mix' (1 edit), 'Gumbo' returns the value asked
            var report = LoadOnDemandProductReport();
            var enumRE = report.Sources.SelectMany(i => i.MetaData.Enums).Single(i => i.Name == "Product Name Search On Demand");
            enumRE.SqlDisplay = "SELECT TOP 1 ProductID, ProductName + ' (' + CAST(ProductID AS varchar(10)) + ')' FROM (SELECT ProductID, ProductName FROM Products UNION ALL SELECT 2000, 'Chef Anton''s Jumbo Mix') p WHERE ProductName LIKE '%{EnumFilter}%' ORDER BY ProductName DESC";
            var result = ApplyProduct(report, "Chef Anton s Gumbo Mix");
            Assert.IsNull(result, result);
            CollectionAssert.AreEqual(new List<string> { "5" }, report.AllRestrictions.First(i => i.DisplayNameEl == "Product Name Search (on demand)").EnumValues);
        }

        [TestMethod]
        public void AmbiguousListIsCompletedBySearchingTheLabel()
        {
            //'Saint Gilles': two homonyms already in the shared list, the third one not loaded yet: the error lists the 3 values
            var report = LoadOnDemandProductReport();
            var enumRE = report.Sources.SelectMany(i => i.MetaData.Enums).Single(i => i.Name == "Product Name Search On Demand");
            enumRE.SqlDisplay = "SELECT TOP 3 ProductID, ProductName + ' (' + CAST(ProductID AS varchar(10)) + ')' FROM (SELECT ProductID, ProductName FROM Products UNION ALL SELECT ProductID + 1000, ProductName FROM Products UNION ALL SELECT ProductID + 2000, ProductName FROM Products) p WHERE ProductName LIKE '%{EnumFilter}%' ORDER BY ProductID";
            enumRE.AddValues(new List<MetaEV> { new MetaEV() { Id = "5", Val = "Chef Anton's Gumbo Mix (5)" }, new MetaEV() { Id = "1005", Val = "Chef Anton's Gumbo Mix (1005)" } });
            var error = ApplyProduct(report, "Chef Anton s Gumbo Mix");
            StringAssert.Contains(error, "is ambiguous");
            StringAssert.Contains(error, "(id 2005)");
        }

        [TestMethod]
        public void LigaturesAreComparedAsTwoLetters()
        {
            Assert.AreEqual("marcq en baroeul", AIReportWriter.NormalizeLabel("Marcq-en-Barœul"));
            Assert.AreEqual("aegir strasse", AIReportWriter.NormalizeLabel("Ægir Straße"));
            var column = _northwind.MetaData.Tables.SelectMany(i => i.Columns).First(i => i.Enum?.Name == "Ship Country");
            var xml = ReportXml(column.GUID).Replace("Orders.ShipName", column.Name).Replace("<Operator>Contains</Operator><Value1>Ernst</Value1>", "<Prompt>Prompt</Prompt><Operator>Equal</Operator>");
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".srex");
            File.WriteAllText(path, xml);
            try
            {
                var report = Report.LoadFromFile(path, _repository, false);
                report.AllRestrictions.Single().EnumRE.Values.Add(new MetaEV() { Id = "Marcq-en-Barœul (59378)" });
                Assert.IsNull(AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""{column.Name}"",""values"":[""Marcq en Baroeul""]}}]"));
                CollectionAssert.AreEqual(new List<string> { "Marcq-en-Barœul (59378)" }, report.AllRestrictions.Single().EnumValues);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void FailedSearchIsNotAnAbsentValue()
        {
            //A search with a shorter text fails: the value is not said absent from the list, the failure is told
            var report = LoadOnDemandProductReport();
            var enumRE = report.Sources.SelectMany(i => i.MetaData.Enums).Single(i => i.Name == "Product Name Search On Demand");
            enumRE.SqlDisplay = "SELECT TOP 100 ProductID, ProductName FROM Products WHERE ProductName LIKE '%{EnumFilter}%' AND 1 = CASE WHEN '{EnumFilter}' = 'Mix' THEN 1 / 0 ELSE 1 END";
            var error = ApplyProduct(report, "Xyz Mix");
            StringAssert.Contains(error, "a search of the list failed");
            Assert.IsFalse(error.Contains("is not in the list"), error);
        }

        [TestMethod]
        public void OneValueCriterionRefusesSeveralValues()
        {
            var column = _northwind.MetaData.Tables.SelectMany(i => i.Columns).First(i => i.Enum?.Name == "Ship Country");
            var xml = ReportXml(column.GUID).Replace("Orders.ShipName", column.Name).Replace("<Operator>Contains</Operator><Value1>Ernst</Value1>", "<Prompt>PromptOneValue</Prompt><Operator>Equal</Operator>");
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".srex");
            File.WriteAllText(path, xml);
            try
            {
                var report = Report.LoadFromFile(path, _repository, false);
                var error = AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""{column.Name}"",""values"":[""Germany"",""France""]}}]");
                StringAssert.StartsWith(error, $"Error: the criterion '{column.Name}' accepts only one value");
                Assert.IsNull(AIReportWriter.ApplyCriteria(report, $@"[{{""name"":""{column.Name}"",""values"":[""Germany""]}}]"));
                CollectionAssert.AreEqual(new List<string> { "Germany" }, report.AllRestrictions.Single().EnumValues);
            }
            finally { File.Delete(path); }
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

            //Unknown data source (a wrong GUID with the name of a source is resolved by SourceResolvedByNameOrByDefault)
            doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipName")).Replace(_northwind.GUID, Guid.NewGuid().ToString()).Replace("<Name>Northwind</Name>", "<Name>Unknown</Name>"));
            StringAssert.Contains(Validate(doc), "does not match any data source");

            //View on no model
            doc = XDocument.Parse(ReportXml(ColumnGuid("Orders.ShipName")).Replace("<ModelGUID>d</ModelGUID>", "<ModelGUID>zz</ModelGUID>"));
            StringAssert.Contains(Validate(doc), "broken internal references");
        }

        [TestMethod]
        public void ColumnsOfTheModelTableAreAccepted()
        {
            //SQL model: the columns are defined in the table of the model, not in the source
            var guid = Guid.NewGuid().ToString();
            var xml = ReportXml(guid).Replace("<Name>Orders.ShipName</Name>", "<Name>totalsales</Name>")
                .Replace("<Elements>", $"<Table><GUID>t</GUID><Sql>select 1 as TotalSales</Sql><Columns><MetaColumn><GUID>{guid}</GUID><Name>totalsales</Name></MetaColumn></Columns></Table><Elements>");
            var doc = XDocument.Parse(xml);
            Assert.IsNull(Validate(doc));

            //The references follow the regenerated GUID of the column
            var column = (string)doc.Descendants("MetaColumn").Single().Element("GUID");
            Assert.AreNotEqual(guid, column);
            Assert.IsTrue(doc.Descendants("MetaColumnGUID").All(i => i.Value == column));
        }

        [TestMethod]
        public void EmptyColumnsAreReported()
        {
            var report = Report.Create(_repository);
            var model = report.Models.First();
            model.ResultTable = new System.Data.DataTable();
            model.ResultTable.Columns.Add("Commune", typeof(string));
            model.ResultTable.Columns.Add("Nombre", typeof(int));
            model.ResultTable.Rows.Add(DBNull.Value, 5);
            model.ResultTable.Rows.Add("", 7);
            if (!report.ExecutionModels.Contains(model)) report.ExecutionModels.Add(model);

            var result = AIReportWriter.EmptyColumns(report);
            Assert.AreEqual(1, result.Count);
            StringAssert.Contains(result[0], "'Commune'");
            Assert.IsFalse(result[0].Contains("'Nombre'"));
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

        [TestMethod]
        public void ModelSavedWithoutLimitKeepsItInAPatch()
        {
            var modelGuid = Guid.NewGuid().ToString();
            string xml(string max) => ReportXml(ColumnGuid("Orders.ShipName")).Replace("<GUID>d</GUID>", $"<GUID>{modelGuid}</GUID>{max}").Replace("<ModelGUID>d</ModelGUID>", $"<ModelGUID>{modelGuid}</ModelGUID>");
            int validate(string existingXml, out XDocument doc)
            {
                doc = XDocument.Parse(xml("<MaxNumberOfRecords>0</MaxNumberOfRecords>"));
                Assert.IsNull(new AIReportWriter(null).Validate(doc, existingXml, false, null, false, new List<string>(), out int limited));
                return limited;
            }

            //Saved without limit (no_record_limit given at the creation): a patch without no_record_limit keeps it
            Assert.AreEqual(0, validate(xml("<MaxNumberOfRecords>0</MaxNumberOfRecords>"), out XDocument patched));
            Assert.AreEqual("0", (string)patched.Descendants("MaxNumberOfRecords").Single());

            //Saved with the default limit: removing it without no_record_limit puts the default back
            Assert.AreEqual(1, validate(xml($"<MaxNumberOfRecords>{AIReportWriter.DefaultMaxNumberOfRecords}</MaxNumberOfRecords>"), out patched));
            Assert.AreEqual(AIReportWriter.DefaultMaxNumberOfRecords.ToString(), (string)patched.Descendants("MaxNumberOfRecords").Single());

            //New report: the default limit
            Assert.AreEqual(1, validate(null, out _));
        }

        static string EnumReportXml(string enumGuid) => $@"<Report>
  <Sources><ReportSource><GUID>c</GUID><MetaData><Enums><MetaEnum><GUID>{enumGuid}</GUID><Name>Types</Name></MetaEnum></Enums></MetaData></ReportSource></Sources>
  <Models><ReportModel><GUID>d</GUID>
    <Elements><ReportElement><GUID>e1</GUID><EnumGUIDEL>{enumGuid}</EnumGUIDEL></ReportElement></Elements>
    <Restriction>[f1]</Restriction>
    <Restrictions><ReportRestriction><GUID>f1</GUID><EnumGUIDRE>{enumGuid}</EnumGUIDRE></ReportRestriction></Restrictions>
  </ReportModel></Models>
</Report>";

        static void AssertEnumReferencesFollow(XDocument doc)
        {
            var enumGuid = (string)doc.Descendants("MetaEnum").Single().Element("GUID");
            Assert.IsTrue(Guid.TryParse(enumGuid, out _));
            Assert.AreEqual(enumGuid, (string)doc.Descendants("EnumGUIDEL").Single());
            Assert.AreEqual(enumGuid, (string)doc.Descendants("EnumGUIDRE").Single());
        }

        [TestMethod]
        public void EnumReferencesFollowTheRegeneratedGuid()
        {
            //Complete XML (report_create_from_xml): every GUID is regenerated, the enumerated list referenced by an element (EnumGUIDEL) and a restriction (EnumGUIDRE) included
            var doc = XDocument.Parse(EnumReportXml("7d3c1f2a-5b6e-4c8d-9a0b-1e2f3a4b5c6d"));
            AIReportWriter.RegenerateGuids(doc, true, null);
            Assert.AreNotEqual("7d3c1f2a-5b6e-4c8d-9a0b-1e2f3a4b5c6d", (string)doc.Descendants("MetaEnum").Single().Element("GUID"));
            AssertEnumReferencesFollow(doc);

            //Patch (report_patch_xml): the placeholder of a new enumerated list is regenerated with its references
            doc = XDocument.Parse(EnumReportXml("enp"));
            AIReportWriter.RegenerateGuids(doc, false, null);
            AssertEnumReferencesFollow(doc);
        }
    }
}
