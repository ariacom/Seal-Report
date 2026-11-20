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
    [TestClass]
    public class TestReportExecution
    {
        [TestMethod]
        public void SimpleExecution()
        {
            //Simple load and report execution and generation in a HTML Result file
            Repository repository = Repository.Create();
            Report report = Report.LoadFromFile(@"C:\ProgramData\Seal Report Repository\Reports\Search - Orders.srex", repository);
            ReportExecution execution = new ReportExecution() { Report = report };
            execution.Execute();
            while (report.IsExecuting) System.Threading.Thread.Sleep(100);
            string result = execution.GenerateResult(ReportFormat.html);
            checkResult(report, result);
        }

        [TestMethod]
        public void DataOnlySingleModel()
        {
            //Data request of the AI agent: only the model asked is executed, no view is rendered, same result as the complete execution
            var repository = Repository.Create();
            var path = Path.Combine(repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex");
            var full = Report.LoadFromFile(path, repository);
            full.SkipExternalViewer = true;
            new ReportExecution() { Report = full }.Execute();
            while (full.IsExecuting) System.Threading.Thread.Sleep(50);
            Assert.IsFalse(full.HasErrors, full.ExecutionErrors);

            var report = Report.LoadFromFile(path, repository);
            report.SkipExternalViewer = true;
            report.DataOnly = true;
            report.ExecutionModelsFilter = report.Models.Where(i => i.Name == "Sales per category").ToList();
            new ReportExecution() { Report = report }.Execute();
            while (report.IsExecuting) System.Threading.Thread.Sleep(50);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
            Assert.AreEqual(1, report.ExecutionModels.Count);
            Assert.IsFalse(report.ExecutionMessages.Contains("Rendering report"), "no view rendered");
            var model = report.Models.First(i => i.Name == "Sales per category");
            Assert.IsNotNull(model.ResultTable);
            Assert.AreEqual(full.Models.First(i => i.Name == "Sales per category").ResultTable.Rows.Count, model.ResultTable.Rows.Count);
            Assert.IsTrue(report.Models.Where(i => i != model).All(i => i.ResultTable == null), "the other models are not executed");
        }

        [TestMethod]
        public void ColumnsHiddenByName()
        {
            //'columns_hidden' of a Data Table: a column number or a column name (title, element or source column), the name is not shifted by the column order
            var repository = Repository.Create();
            var path = Path.Combine(repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex");
            var report = Report.LoadFromFile(path, repository);
            report.SkipExternalViewer = true;
            new ReportExecution() { Report = report }.Execute();
            while (report.IsExecuting) System.Threading.Thread.Sleep(50);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);

            var model = report.ExecutionModels.First(i => i.Pages.Count > 0 && i.Pages[0].DataTable != null && i.Pages[0].DataTable.ColumnCount > 1);
            var table = model.Pages[0].DataTable;
            var titleRow = table.BodyStartRow - 1;
            int last = table.ColumnCount - 1;
            var element = table[titleRow, last].Element;
            Assert.IsNotNull(element, "title cell with its element");

            void setHidden(ReportView v, string value)
            {
                if (v.GetParameter(Parameter.ColumnsHiddenParameter) == null) v.Parameters.Add(new Parameter() { Name = Parameter.ColumnsHiddenParameter });
                v.SetParameter(Parameter.ColumnsHiddenParameter, value);
            }
            var view = report.AllViews.First(i => i.TemplateName == "Data Table");
            setHidden(view, "  " + element.DisplayNameElTranslated.ToUpper() + " ;");
            for (int col = 0; col < table.ColumnCount; col++) Assert.AreEqual(col == last, view.IsColumnHidden(col, table), $"column {col} by title");

            //Name of the source column, number, unknown name
            view = Report.LoadFromFile(path, repository).AllViews.First(i => i.TemplateName == "Data Table");
            setHidden(view, "1;" + element.MetaColumn.Name);
            Assert.IsTrue(view.IsColumnHidden(0, table), "number");
            Assert.IsTrue(view.IsColumnHidden(last, table), "source column name");
            Assert.IsFalse(view.IsColumnHidden(1, table) && last != 1, "other column");
            view = Report.LoadFromFile(path, repository).AllViews.First(i => i.TemplateName == "Data Table");
            setHidden(view, "No such column");
            for (int col = 0; col < table.ColumnCount; col++) Assert.IsFalse(view.IsColumnHidden(col, table), $"column {col} unknown name");
            Assert.IsFalse(view.IsColumnHidden(0), "the number-only overload ignores the names");
        }

        [TestMethod]
        public void ReportSortFollowsAppliedSort()
        {
            //The sort shown in the headers follows the ORDER BY really built: Row elements before Data elements, whatever their sort numbers
            var repository = Repository.Create();
            var path = Path.Combine(repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex");
            var report = Report.LoadFromFile(path, repository);
            var model = report.Models.First(m => m.GetElements(PivotPosition.Row).Any() && m.GetElements(PivotPosition.Data).Any() && !m.GetElements(PivotPosition.Column).Any());
            var row = model.GetElements(PivotPosition.Row).First();
            var data = model.GetElements(PivotPosition.Data).First();
            foreach (var e in model.Elements) e.SortOrder = ReportElement.kNoSortKeyword;

            //'1 Descendant' on the data element, automatic sort on the row element: the rows are sorted by the row element first
            data.SortOrder = "1 " + ReportElement.kDescendantSortKeyword;
            row.SortOrder = ReportElement.kAutomaticAscSortKeyword;
            CollectionAssert.AreEqual(new[] { row }, model.ReportSortElements, "the automatic row sort comes first and ends the shown sort");
            Assert.IsNotNull(model.SortWarning, "warning: the data sort applies after the row sort");
            StringAssert.Contains(model.SortWarning, row.DisplayNameEl);

            //Row element not sorted: the data element sorts the rows, no warning
            row.SortOrder = ReportElement.kNoSortKeyword;
            CollectionAssert.AreEqual(new[] { data }, model.ReportSortElements, "data sort shown");
            Assert.IsNull(model.SortWarning, "no warning without automatic row sort");

            //Explicit numbers: '2 Ascendant' on the row still comes before '1 Descendant' on the data, both shown, no warning
            row.SortOrder = "2 " + ReportElement.kAscendantSortKeyword;
            CollectionAssert.AreEqual(new[] { row, data }, model.ReportSortElements, "row then data");
            Assert.IsNull(model.SortWarning, "an explicit row sort is chosen by the author");

            //Only automatic sorts: the first row element is shown, an automatic data sort is not
            row.SortOrder = ReportElement.kAutomaticAscSortKeyword;
            data.SortOrder = ReportElement.kAutomaticAscSortKeyword;
            CollectionAssert.AreEqual(new[] { row }, model.ReportSortElements, "main key of the automatic sort");
            row.SortOrder = ReportElement.kNoSortKeyword;
            Assert.AreEqual(0, model.ReportSortElements.Count, "automatic data sort not shown");
            Assert.IsNull(model.SortWarning);
        }

        [TestMethod]
        public void HeaderInformationStyles()
        {
            //Description of a header (semantic layer): tooltip (default), information icon, or icon + numbered notes written under the table
            var repository = Repository.Create();
            var path = Path.Combine(repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex");
            var report = Report.LoadFromFile(path, repository);
            report.SkipExternalViewer = true;
            new ReportExecution() { Report = report }.Execute();
            while (report.IsExecuting) System.Threading.Thread.Sleep(50);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);

            var model = report.ExecutionModels.First(i => i.Pages.Count > 0 && i.Pages[0].DataTable != null && i.Pages[0].DataTable.ColumnCount > 1);
            var table = model.Pages[0].DataTable;
            var titleRow = table.BodyStartRow - 1;
            var cell = table[titleRow, 0];
            var other = table[titleRow, 1];
            Assert.IsNotNull(cell.Element, "title cell with its element");
            cell.Element.Description = "Share of 'people' below the <threshold>";
            other.Element.Description = "";
            if (!string.IsNullOrEmpty(other.Element.MetaColumn?.Description)) other = null; //no header without description in this table

            const string show = "data_tables_show_header_information", style = "data_tables_header_information_style";
            var view = report.AllViews.First(i => i.TemplateName == "Data Table");
            void setStyle(string value)
            {
                if (view.GetParameter(style) == null) view.Parameters.Add(new Parameter() { Name = style });
                view.SetParameter(style, value);
            }
            var encoded = System.Net.WebUtility.HtmlEncode(cell.Element.Description);

            //Default: information icon, no tooltip
            Assert.AreEqual(ReportView.HeaderInformationIcon, view.GetValue(style), "default style");
            Assert.AreEqual("", view.HeaderInformationAttribute(cell, show, style));
            StringAssert.Contains(view.HeaderInformationHTML(cell, show, style), "header_info_icon");

            //Tooltip only
            setStyle(ReportView.HeaderInformationTooltip);
            Assert.AreEqual($"title='{encoded}'", view.HeaderInformationAttribute(cell, show, style));
            Assert.AreEqual("", view.HeaderInformationHTML(cell, show, style));
            Assert.AreEqual("", view.HeaderInformationNotes());

            //Icon: no tooltip, icon with the encoded description, no notes
            setStyle(ReportView.HeaderInformationIcon);
            Assert.AreEqual("", view.HeaderInformationAttribute(cell, show, style));
            var html = view.HeaderInformationHTML(cell, show, style);
            StringAssert.Contains(html, "header_info_icon");
            StringAssert.Contains(html, "data-bs-content='" + encoded + "'");
            Assert.IsFalse(html.Contains("<threshold>"), "description encoded");
            Assert.IsFalse(html.Contains("<sup"), "no note number");
            Assert.AreEqual("", view.HeaderInformationNotes());
            if (other != null) Assert.AreEqual("", view.HeaderInformationHTML(other, show, style), "no description, no icon");

            //Icon + notes: same description = same number, the notes are written once then cleared
            setStyle(ReportView.HeaderInformationIconNotes);
            StringAssert.Contains(view.HeaderInformationHTML(cell, show, style), ">1</sup>");
            StringAssert.Contains(view.HeaderInformationHTML(cell, show, style), ">1</sup>");
            var notes = view.HeaderInformationNotes();
            StringAssert.Contains(notes, "header_info_notes");
            StringAssert.Contains(notes, encoded);
            Assert.AreEqual(1, notes.Split(new[] { "<sup>" }, StringSplitOptions.None).Length - 1, "one note");
            Assert.AreEqual("", view.HeaderInformationNotes(), "notes cleared");

            //Show header information false: nothing at all
            if (view.GetParameter(show) == null) view.Parameters.Add(new Parameter() { Name = show });
            view.SetParameter(show, false);
            Assert.AreEqual("", view.HeaderInformationAttribute(cell, show, style));
            Assert.AreEqual("", view.HeaderInformationHTML(cell, show, style));
        }

        [TestMethod]
        public void ExcelHeaderLogo()
        {
            //Logo in the page header of the Excel result (default 'header_logo' true): image, VML drawing per sheet, code &G in the header
            var repository = Repository.Create();
            var path = Path.Combine(repository.ReportsFolder, "Samples", "01-Simple list (Products).srex");
            var report = Report.LoadFromFile(path, repository);
            report.SkipExternalViewer = true;
            var execution = new ReportExecution() { Report = report };
            execution.Execute();
            while (report.IsExecuting) System.Threading.Thread.Sleep(50);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
            var resultPath = execution.GenerateResult(ReportFormat.Excel);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
            Assert.IsFalse(report.ExecutionMessages.Contains("Unable to add the logo"), report.ExecutionMessages);

            int sheets = 0;
            using (var zip = System.IO.Compression.ZipFile.OpenRead(resultPath))
            {
                string read(string name)
                {
                    var entry = zip.GetEntry(name);
                    Assert.IsNotNull(entry, name);
                    using var reader = new StreamReader(entry.Open());
                    return reader.ReadToEnd();
                }
                Assert.IsNotNull(zip.GetEntry("xl/media/sealHeaderLogo.png"), "logo.svg replaced by logo.png");
                StringAssert.Contains(read("[Content_Types].xml"), "Extension=\"vml\"");
                foreach (var sheetName in zip.Entries.Select(i => i.FullName).Where(i => i.StartsWith("xl/worksheets/sheet") && i.EndsWith(".xml")))
                {
                    sheets++;
                    var sheet = read(sheetName);
                    StringAssert.Contains(sheet, "legacyDrawingHF");
                    StringAssert.Contains(sheet, "&amp;R&amp;G");
                    Assert.IsTrue(sheet.IndexOf("legacyDrawingHF") > sheet.IndexOf("headerFooter"), "legacyDrawingHF after headerFooter");
                    var rels = read("xl/worksheets/_rels/" + Path.GetFileName(sheetName) + ".rels");
                    StringAssert.Contains(rels, "sealHeaderLogo");
                }
                var vml = read("xl/drawings/sealHeaderLogo1.vml");
                StringAssert.Contains(vml, "id=\"RH\"");
                StringAssert.Contains(vml, "height:30pt");
                StringAssert.Contains(read("xl/drawings/_rels/sealHeaderLogo1.vml.rels"), "../media/sealHeaderLogo.png");
            }
            Assert.IsTrue(sheets > 0);

            //Margin and header codes
            Assert.AreEqual(0.88, Seal.Renderer.ExcelResult.GetHeaderLogoTopMargin(30, 0.314, 0.59), 0.001);
            Assert.AreEqual(0.59, Seal.Renderer.ExcelResult.GetHeaderLogoTopMargin(5, 0.1, 0.59), 0.001);
            Assert.AreEqual("&L&\"Arial,Bold\"&14Name&R&G", Seal.Renderer.ExcelResult.AddHeaderPictureCode("&L&\"Arial,Bold\"&14Name", 'R'));
            Assert.AreEqual("&L&G&\"Arial,Bold\"&14Name", Seal.Renderer.ExcelResult.AddHeaderPictureCode("&L&\"Arial,Bold\"&14Name", 'L'));
            Assert.AreEqual("&LA&C&GB&RC", Seal.Renderer.ExcelResult.AddHeaderPictureCode("&LA&CB&RC", 'C'));
            Assert.AreEqual("&L&&R&C&G", Seal.Renderer.ExcelResult.AddHeaderPictureCode("&L&&R", 'C'));
            Assert.AreEqual("&R&G", Seal.Renderer.ExcelResult.AddHeaderPictureCode("&R&G", 'R'), "added once");

            //A SVG without PNG/JPG of the same name: no logo, message
            var excel = new Seal.Renderer.ExcelResult(report);
            Assert.IsNull(excel.GetHeaderLogoPath("no_such_logo.svg"));
            StringAssert.Contains(report.ExecutionMessages, "no PNG or JPG file found");
        }

        [TestMethod]
        public void ParallelModelsSameResult()
        {
            //The queries of the models of an Execution set run in parallel: same result tables and pages as the execution one model after the other
            var repository = Repository.Create();
            var path = Path.Combine(repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex");
            string execute(int maxParallel)
            {
                var initial = ReportExecution.MaxParallelModelsOverride;
                try
                {
                    ReportExecution.MaxParallelModelsOverride = maxParallel;
                    var report = Report.LoadFromFile(path, repository);
                    report.SkipExternalViewer = true;
                    new ReportExecution() { Report = report }.Execute();
                    while (report.IsExecuting) System.Threading.Thread.Sleep(50);
                    Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
                    Assert.IsTrue(report.ExecutionModels.Count > 1, "several models");
                    var result = "";
                    foreach (var model in report.ExecutionModels)
                    {
                        Assert.IsTrue(model.ExecResultTableLoaded, model.Name);
                        result += $"{model.Name}: {model.Pages.Count} page(s)\r\n";
                        foreach (System.Data.DataRow row in model.ResultTable.Rows) result += string.Join("|", row.ItemArray) + "\r\n";
                    }
                    return result;
                }
                finally
                {
                    ReportExecution.MaxParallelModelsOverride = initial;
                }
            }
            Assert.AreEqual(execute(1), execute(4));
        }

        [TestMethod]
        public void MaxParallelModelsFromReport()
        {
            //The parallel execution of the models is set by the report: 4 by default (not serialized), 1 = one model after the other, bounded from 1 to 16
            var repository = Repository.Create();
            var report = Report.LoadFromFile(Path.Combine(repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex"), repository);
            var execution = new ReportExecution() { Report = report };
            Assert.AreEqual(4, report.MaxParallelModels, "default");
            Assert.IsFalse(report.ShouldSerializeMaxParallelModels());
            Assert.AreEqual(4, execution.MaxParallelModels);
            report.MaxParallelModels = 1;
            Assert.IsTrue(report.ShouldSerializeMaxParallelModels());
            Assert.AreEqual(1, execution.MaxParallelModels);
            report.MaxParallelModels = 0;
            Assert.AreEqual(1, execution.MaxParallelModels, "lower bound");
            report.MaxParallelModels = 100;
            Assert.AreEqual(16, execution.MaxParallelModels, "upper bound");
        }

        [TestMethod]
        public void ModelMaximumOfRecordsShownAsPartialResult()
        {
            //The 'Maximum number of records' of the model is reached: flagged and the Model view warns the user (as for the limit of the connection)
            var repository = Repository.Create();
            var path = Path.Combine(repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex");
            var report = Report.LoadFromFile(path, repository);
            report.SkipExternalViewer = true;
            var model = report.Models.First(i => i.Name == "Employees");
            model.MaxNumberOfRecords = 2;
            model.SqlOrderBy = "-- no ORDER BY"; //unsorted: an arbitrary part of the rows
            var sorted = report.Models.First(i => i.Name == "Sales per category");
            sorted.MaxNumberOfRecords = 2; //sorted by the SQL: a top 2 by design
            var execution = new ReportExecution() { Report = report };
            execution.Execute();
            while (report.IsExecuting) System.Threading.Thread.Sleep(50);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
            Assert.AreEqual(2, model.ResultTable.Rows.Count);
            Assert.IsTrue(model.MaxRowsReached && model.MaxRowsOfModel && model.MaxRowsLimit == 2, "partial result flagged");
            Assert.IsTrue(sorted.Sql.Contains("ORDER BY") && !sorted.MaxRowsReached, "sorted top N not flagged");
            Assert.IsFalse(report.Models.First(i => i.Name == "Sales per year").MaxRowsReached, "model under its limit not flagged");
            Assert.IsNull(model.MaxRowsOrderBy, "unsorted: no order");
            StringAssert.Contains(model.MaxRowsMessage, "arbitrary part");
            var html = File.ReadAllText(execution.GenerateResult(ReportFormat.html));
            StringAssert.Contains(html, "(maximum of the report)");
        }

        [TestMethod]
        public void AgentJsonRoundedAndAggregatesByRole()
        {
            Assert.AreEqual(2, ReportModel.GetFormatDecimals("N2"));
            Assert.AreEqual(0, ReportModel.GetFormatDecimals("N0"));
            Assert.AreEqual(3, ReportModel.GetFormatDecimals("P1"));
            Assert.AreEqual(1, ReportModel.GetFormatDecimals("#,##0.0"));
            Assert.AreEqual(4, ReportModel.GetFormatDecimals("0.00 %"));
            Assert.IsNull(ReportModel.GetFormatDecimals("text"));

            var model = ReportModel.Create();
            var table = new System.Data.DataTable();
            table.Columns.Add("count", typeof(double));
            table.Columns.Add("ratio", typeof(decimal));
            table.Rows.Add(41.0, 120.1915032679738562m);
            table.Rows.Add(10.0, 3.5m);
            model.ResultTable = table;
            var (tableJson, aggJson) = model.GetResultTableTranslatedJson();
            StringAssert.Contains(tableJson, "[41,120.1915]");
            Assert.IsFalse(tableJson.Contains("41.0"), tableJson);
            StringAssert.Contains(aggJson, "\"sum\":51");
        }

        [TestMethod]
        public void EnumRefreshDelayKeepsTheValues()
        {
            //The lists of a Data Source are shared by the executions and were reloaded at each report load: a list loaded from a large
            //table cost seconds at each execution when the database had not the table in memory
            var repository = Repository.Create();
            var path = Path.Combine(repository.ReportsFolder, "Samples", "09-Enumerated List - Dynamic Display.srex");
            var itemEnum = repository.Sources.First(i => i.Name == "Northwind").MetaData.Enums.Single(i => i.Name == "Product Name");
            Assert.IsTrue(itemEnum.IsDbRefresh);

            //No delay: refreshed at each report load
            Report.LoadFromFile(path, repository);
            Assert.IsTrue(itemEnum.RefreshPending);
            Assert.IsTrue(itemEnum.Values.Count > 0);
            Assert.IsFalse(itemEnum.RefreshPending);
            Assert.IsTrue(itemEnum.LastRefresh > DateTime.Now.AddMinutes(-1));
            Report.LoadFromFile(path, repository);
            Assert.IsTrue(itemEnum.RefreshPending);

            //Delay: the values loaded are kept
            itemEnum.RefreshDelay = 60;
            Assert.IsTrue(itemEnum.Values.Count > 0);
            var lastRefresh = itemEnum.LastRefresh;
            Report.LoadFromFile(path, repository);
            Assert.IsFalse(itemEnum.RefreshPending);
            Assert.IsTrue(itemEnum.Values.Count > 0);
            Assert.AreEqual(lastRefresh, itemEnum.LastRefresh);
        }

        [TestMethod]
        public void RestrictionTextsThreadSafe()
        {
            //The models of a report run in parallel (Report.MaxParallelModels): the texts of a restriction were built step by step in
            //shared fields (text = label + operator, then text += values), so two threads could add the values twice
            //(seen in PROD: 'Département (avis TED) Egale Bas-Rhin (67) Bas-Rhin (67)'), and the SQL could be corrupted the same way
            var repository = Repository.Create();
            var report = Report.LoadFromFile(Path.Combine(repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex"), repository);
            var restriction = report.Models.SelectMany(i => i.Restrictions).First(i => i.IsText);
            restriction.Operator = Operator.Equal;
            restriction.Value1 = "Beverages";
            restriction.Value2 = "Condiments";
            var expectedText = restriction.DisplayText;
            var expectedSQL = restriction.SQLText;
            var texts = new System.Collections.Concurrent.ConcurrentBag<string>();
            System.Threading.Tasks.Parallel.For(0, 20000, new System.Threading.Tasks.ParallelOptions() { MaxDegreeOfParallelism = 8 }, i =>
            {
                texts.Add(restriction.DisplayText + "|" + restriction.SQLText);
            });
            var distinct = texts.Distinct().ToList();
            Assert.HasCount(1, distinct, string.Join("\r\n", distinct.Take(5)));
            Assert.AreEqual(expectedText + "|" + expectedSQL, distinct[0]);
        }

        [TestMethod]
        public void AgentJsonRoleFromMetadata()
        {
            //The role of a number sent to the AI comes from the metadata of its element (aggregate, format, total), never from the words of its name (except latitude/longitude)
            var model = ReportModel.Create();
            var table = new System.Data.DataTable();
            table.Columns.Add("Total price", typeof(decimal));
            table.Columns.Add("Part de la fibre (%)", typeof(decimal));
            table.Columns.Add("Latitude", typeof(double));
            table.Rows.Add(100m, 80.5m, 49.1);
            table.Rows.Add(110m, 90.5m, 49.2);
            model.ResultTable = table;
            var (_, aggJson) = model.GetResultTableTranslatedJson();
            var columns = System.Text.Json.JsonDocument.Parse(aggJson).RootElement.GetProperty("columns");
            Assert.IsTrue(columns.GetProperty("Total price").TryGetProperty("sum", out _), "'price' in the name: still a sum");
            Assert.IsTrue(columns.GetProperty("Part de la fibre (%)").TryGetProperty("sum", out _), "no metadata: a sum");
            Assert.IsFalse(columns.GetProperty("Total price").TryGetProperty("sum_note", out _), aggJson);
            Assert.AreEqual("coordinate", columns.GetProperty("Latitude").GetProperty("role").GetString());

            //Element of an executed report: its aggregate and its total give the role
            var repository = Repository.Create();
            var report = Report.LoadFromFile(Path.Combine(repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex"), repository);
            report.SkipExternalViewer = true;
            new ReportExecution() { Report = report }.Execute();
            while (report.IsExecuting) System.Threading.Thread.Sleep(50);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
            var sales = report.Models.First(i => i.Name == "Sales per category");
            var data = sales.Elements.First(i => i.PivotPosition == PivotPosition.Data);
            System.Text.Json.JsonElement column()
            {
                var (_, json) = sales.GetResultTableTranslatedJson();
                return System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("columns").GetProperty(data.DisplayNameElTranslated);
            }
            Assert.IsTrue(column().TryGetProperty("sum", out _), "sum without total");
            Assert.IsFalse(column().TryGetProperty("total_shown", out _));
            data.ShowTotal = ShowTotal.Column;
            Assert.IsTrue(column().GetProperty("total_shown").GetBoolean(), "total shown: sum flagged");
            //Only a total covering all the rows is the sum of the result: one total per row (e.g. a stock per year with the categories in columns) is not
            data.ShowTotal = ShowTotal.Row;
            Assert.IsTrue(column().TryGetProperty("sum", out _), "total per row: still a sum");
            Assert.IsFalse(column().TryGetProperty("total_shown", out _), "total per row of a table with row dimensions: not the sum of the result");
            var category = sales.Elements.First(i => i.PivotPosition == PivotPosition.Row);
            category.PivotPosition = PivotPosition.Column;
            Assert.IsTrue(column().GetProperty("total_shown").GetBoolean(), "dimension in columns only: the total of the row is the sum of the result");
            data.ShowTotal = ShowTotal.Column;
            Assert.IsFalse(column().TryGetProperty("total_shown", out _), "one total per column: not the sum of the result");
            data.ShowTotal = ShowTotal.RowColumn;
            Assert.IsTrue(column().GetProperty("total_shown").GetBoolean(), "grand total of a cross table");
            category.PivotPosition = PivotPosition.Page;
            Assert.IsFalse(column().TryGetProperty("total_shown", out _), "one table per page: no total of the result");
            category.PivotPosition = PivotPosition.Row;
            data.ShowTotal = ShowTotal.Column;
            //A partial result (maximum rows reached): the sum covers only the rows loaded, never flagged as the total of the result
            sales.MaxRowsReached = true;
            Assert.IsTrue(column().TryGetProperty("sum", out _), "partial result: still a sum");
            Assert.IsFalse(column().TryGetProperty("total_shown", out _), "partial result: the sum is not the total of the data");
            sales.MaxRowsReached = false;
            Assert.IsTrue(column().GetProperty("total_shown").GetBoolean(), "complete result: total flagged again");
            data.TotalAggregateFunction = AggregateFunction.Avg;
            Assert.IsFalse(column().TryGetProperty("sum", out _), "total average: no sum");
            Assert.AreEqual("total avg", column().GetProperty("role").GetString());
            data.ShowTotal = ShowTotal.No;
            data.AggregateFunction = AggregateFunction.Avg;
            Assert.AreEqual("avg", column().GetProperty("role").GetString());
            Assert.IsFalse(column().TryGetProperty("sum", out _), "average: no sum");
            //A sum total prevails over the aggregate (e.g. the maximum per notice of an amount, summed in the total)
            data.AggregateFunction = AggregateFunction.Max;
            data.ShowTotal = ShowTotal.Column;
            data.TotalAggregateFunction = AggregateFunction.Sum;
            Assert.IsTrue(column().GetProperty("total_shown").GetBoolean(), "maximum with a sum total: sum");
        }

        [TestMethod]
        public void SortedPartialResultGivesItsOrder()
        {
            //A long list sorted by the SQL and cut by its 'Maximum number of records': partial, but the rows are the first ones in the order of the SQL
            var repository = Repository.Create();
            var path = Path.Combine(repository.ReportsFolder, "System", "Tests", "Reports", "TST040 Complete dashboard.srex");
            var report = Report.LoadFromFile(path, repository);
            report.SkipExternalViewer = true;
            var model = report.Models.First(i => i.Name == "Employees");
            model.MaxNumberOfRecords = 1000; //not a short top N
            var column = model.Elements.First().MetaColumn.Name;
            model.SqlOrderBy = $"ORDER BY {column} DESC -- the last ones first";
            var execution = new ReportExecution() { Report = report };
            execution.Execute();
            while (report.IsExecuting) System.Threading.Thread.Sleep(50);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
            //Fewer rows than 1000: complete, nothing said
            Assert.IsFalse(model.MaxRowsReached);
            Assert.AreEqual($"{column} DESC", model.SqlOrderByClause);

            model.MaxRowsReached = true; //as if the limit was reached
            model.MaxRowsOfModel = true;
            model.MaxRowsLimit = 1000;
            //An element with an enumerated list is read by its identifiers: said in the order
            var first = model.Elements.First();
            var byCode = first.IsEnum && first.EnumEL.Values.Any(i => i.Id != i.DisplayValue);
            Assert.AreEqual($"{first.DisplayNameElTranslated}{(byCode ? " (by code)" : "")} (descending)", model.MaxRowsOrderBy);
            if (first.IsEnum)
            {
                //A list whose identifiers are its labels (e.g. years): no '(by code)'
                var values = first.EnumEL.Values;
                try
                {
                    first.EnumEL.Values = new System.Collections.Generic.List<MetaEV> { new MetaEV() { Id = "2025" }, new MetaEV() { Id = "2024", Val = "2024" } };
                    Assert.AreEqual($"{first.DisplayNameElTranslated} (descending)", model.MaxRowsOrderBy);
                }
                finally { first.EnumEL.Values = values; }
            }
            StringAssert.Contains(model.MaxRowsMessage, " rows by ");
            Assert.IsFalse(model.MaxRowsMessage.Contains("arbitrary"));

            //Expressions of a custom ORDER BY: named by the column found inside, the SQL is never shown
            var label = first.DisplayNameElTranslated + (byCode ? " (by code)" : "");
            void orderBy(string clause) => model.Sql = System.Text.RegularExpressions.Regex.Replace(model.Sql, @"^\s*ORDER\s+BY\s+.*$", clause, System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            orderBy($"ORDER BY ({column} IS NULL), {column} -- names first");
            Assert.AreEqual(label, model.MaxRowsOrderBy, "an expression then the same column: cited once");
            orderBy($"ORDER BY coalesce({column}, 0) DESC NULLS LAST");
            Assert.AreEqual($"{label} (descending)", model.MaxRowsOrderBy);
            orderBy($"ORDER BY random(), {column}");
            Assert.AreEqual(label, model.MaxRowsOrderBy, "a term without column is not cited");
            orderBy("ORDER BY random()");
            Assert.AreEqual("", model.MaxRowsOrderBy, "sorted, but no term recognized");
            StringAssert.Contains(model.MaxRowsMessage, "in the order of the report");
            Assert.IsFalse(model.MaxRowsMessage.Contains("random"), model.MaxRowsMessage);
        }

        [TestMethod]
        public void SharedEnumConcurrentReadsAndAdds()
        {
            //The enumerated lists are shared by the reports executed at the same time: values added (list loaded on demand) while other threads enumerate the list
            var metaEnum = MetaEnum.Create();
            var stop = DateTime.Now.AddSeconds(2);
            int errors = 0, adds = 0;
            var tasks = Enumerable.Range(0, 8).Select(t => System.Threading.Tasks.Task.Run(() =>
            {
                while (DateTime.Now < stop)
                {
                    try
                    {
                        if (t < 2)
                        {
                            metaEnum.AddValues(new[] { new MetaEV() { Id = Guid.NewGuid().ToString(), Val = "v" } });
                            System.Threading.Interlocked.Increment(ref adds);
                        }
                        else
                        {
                            foreach (var value in metaEnum.Values) { }
                            metaEnum.Values.ToList();
                        }
                    }
                    catch { System.Threading.Interlocked.Increment(ref errors); }
                }
            })).ToArray();
            System.Threading.Tasks.Task.WaitAll(tasks);
            Assert.AreEqual(0, errors, "errors while reading the list");
            Assert.AreEqual(adds, metaEnum.Values.Count);
        }

        /// <summary>
        /// Check the HTML result file without opening it (no browser launched by the tests)
        /// </summary>
        static void checkResult(Report report, string result)
        {
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
            Assert.IsTrue(File.Exists(result), "result file generated");
            Assert.IsTrue(new FileInfo(result).Length > 0, "result file not empty");
        }

        [TestMethod]
        public void CreationAndExecution()
        {
            var repository = Repository.Create();
            Report report = Report.Create(repository);
            report.DisplayName = "Sample Report";
            var source = report.Sources.FirstOrDefault(i => i.Name.StartsWith("Northwind"));
            source.MetaData.Tables.Clear();
            //Update the data source with a new table
            var table = source.AddTable(true);
            table.DynamicColumns = true;
            table.Name = "products";
            //Instead of the name, could be a direct SQL statement:
            //table.Sql = "select * from products";
            table.Refresh();

            //Set the source of the default model
            report.Models[0].SourceGUID = source.GUID;
            //Add elements to the reports model
            foreach (var column in table.Columns)
            {
                var element = ReportElement.Create();
                element.MetaColumnGUID = column.GUID;
                element.Name = column.Name;
                element.PivotPosition = PivotPosition.Row;
                element.Source = source;
                report.Models[0].Elements.Add(element);
            }

            //Add a restriction to the model
            var restriction = ReportRestriction.CreateReportRestriction();
            restriction.Source = report.Models[0].Source;
            restriction.Report = report;
            restriction.Model = report.Models[0];
            restriction.MetaColumnGUID = table.Columns.FirstOrDefault(i => i.Name == "products.ProductName").GUID;
            restriction.SetDefaults();
            restriction.Operator = Operator.Contains;
            restriction.Value1 = "er";
            report.Models[0].Restrictions.Add(restriction);
            //Set the restriction text
            var restrictionText = report.Models[0].Restriction;
            if (!string.IsNullOrEmpty(restrictionText)) restrictionText += string.Format("({0}) AND ", restrictionText);
            restrictionText += restriction.Pattern;
            report.Models[0].Restriction = restrictionText;

            //Then execute it
            ReportExecution execution = new ReportExecution() { Report = report };
            execution.Execute();
            while (report.IsExecuting) System.Threading.Thread.Sleep(100);
            string result = execution.GenerateResult(ReportFormat.html);
            checkResult(report, result);
        }

        [TestMethod]
        public void RowElementTotal()
        {
            //Numeric Row elements with ShowTotal have their sum in the total row, with or without Data elements
            checkRowElementTotal(false);
            checkRowElementTotal(true);
        }

        static void checkRowElementTotal(bool withData)
        {
            var repository = Repository.Create();
            Report report = Report.Create(repository);
            var source = report.Sources.FirstOrDefault(i => i.Name.StartsWith("Northwind"));
            source.MetaData.Tables.Clear();
            var table = source.AddTable(true);
            table.DynamicColumns = true;
            table.Name = "products";
            table.Refresh();

            var model = report.Models[0];
            model.SourceGUID = source.GUID;
            Func<string, PivotPosition, ShowTotal, ReportElement> addElement = (name, position, showTotal) =>
            {
                var column = table.Columns.First(i => i.Name == name);
                var element = ReportElement.Create();
                element.MetaColumnGUID = column.GUID;
                element.Name = column.Name;
                element.PivotPosition = position;
                element.Source = source;
                element.ShowTotal = showTotal;
                model.Elements.Add(element);
                return element;
            };
            addElement("products.ProductName", PivotPosition.Row, ShowTotal.No);
            var stock = addElement("products.UnitsInStock", PivotPosition.Row, ShowTotal.Column);
            var order = addElement("products.UnitsOnOrder", PivotPosition.Row, ShowTotal.No);
            if (withData) addElement("products.ReorderLevel", PivotPosition.Data, ShowTotal.Column);

            ReportExecution execution = new ReportExecution() { Report = report };
            execution.Execute();
            while (report.IsExecuting) System.Threading.Thread.Sleep(100);
            Assert.IsFalse(report.HasErrors, report.ExecutionErrors);
            Assert.IsTrue(model.HasTotals, "model has totals");

            var dataTable = model.Pages[0].DataTable;
            var totalLine = dataTable.Lines[dataTable.Lines.Count - 1];
            Assert.AreEqual(dataTable.BodyEndRow, dataTable.Lines.Count - 1, "total row after the body");
            Assert.IsTrue(totalLine[0].IsTitle && totalLine[0].IsTotal, "total title in the first column");

            int stockCol = -1, orderCol = -1;
            for (int col = 0; col < dataTable.ColumnCount; col++)
            {
                var element = dataTable[dataTable.BodyStartRow, col].Element;
                if (element == stock) stockCol = col;
                if (element == order) orderCol = col;
            }
            double expected = 0;
            for (int row = dataTable.BodyStartRow; row < dataTable.BodyEndRow; row++) expected += dataTable[row, stockCol].DoubleValue ?? 0;
            Assert.IsTrue(expected > 0, "stock values");
            Assert.AreEqual(expected, totalLine[stockCol].DoubleValue, "sum of the Row element");
            Assert.IsTrue(totalLine[orderCol].Value == null, "no total for a Row element without ShowTotal");
            if (withData)
            {
                var dataCell = totalLine[dataTable.ColumnCount - 1];
                Assert.IsTrue(dataCell.IsTotal && dataCell.DoubleValue > 0, "total of the Data element");
            }
        }
    }
}
