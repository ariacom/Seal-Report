//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Model;
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
    }
}
