using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Model;
using System.Diagnostics;
using System.Data.OleDb;
using System.Data;
using System.Collections.Generic;
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
            while (report.Status != ReportStatus.Executed) System.Threading.Thread.Sleep(100);
            string result = execution.GenerateHTMLResult();
            Process.Start(result);
        }

        [TestMethod]
        public void ExecutionWithExternalDataTables()
        {
            //Get Data Table from another source
            string sql = @"
SELECT DISTINCT
  DateSerial(DatePart('yyyy',[Orders.OrderDate]), 1, 1) AS C0,
  Products.CategoryID AS C1,
  Customers.Country AS C2,
  999999 AS C3
FROM 
(Products INNER JOIN 
([Order Details] INNER JOIN 
(Orders INNER JOIN Customers
 ON Customers.CustomerID = Orders.CustomerID)
 ON Orders.OrderID = [Order Details].OrderID)
 ON Products.ProductID = [Order Details].ProductID)
";

            var connection = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=C:\ProgramData\Seal Report Repository\Databases\Northwind.mdb;Persist Security Info=False");
            var command = connection.CreateCommand();
            command.CommandText = sql;
            var adapter = new OleDbDataAdapter(command);
            var newTable = new DataTable();
            adapter.Fill(newTable);

            //Data table must have the same column definition as the one defined in the report
            var repository = Repository.Create();
            var report = Report.LoadFromFile(@"C:\ProgramData\Seal Report Repository\Reports\Samples\03-Cross tab - Simple chart (Orders).srex", repository);

            //Set data table to the report model (here there is only 1 model)
            report.Models[0].ResultTable = newTable;
            //Set report in Render only mode so the Result table is not loaded during the execution 
            report.RenderOnly = true;
            //Note that if model.ResultTable is null, the table will be loaded anyway

            var execution = new ReportExecution() { Report = report };
            execution.Execute();
            while (report.Status != ReportStatus.Executed) System.Threading.Thread.Sleep(100);
            string result = execution.GenerateHTMLResult();
            Process.Start(result);
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
            restriction.Model = report.Models[0];
            restriction.MetaColumnGUID = table.Columns.FirstOrDefault(i => i.Name == "products.ProductName").GUID;
            restriction.SetDefaults();
            restriction.Operator = Operator.Contains;
            restriction.Value1 = "er";
            report.Models[0].Restrictions.Add(restriction);
            //Set the restriction text
            if (!string.IsNullOrEmpty(report.Models[0].Restriction)) report.Models[0].Restriction = string.Format("({0}) AND ", report.Models[0].Restriction);
            report.Models[0].Restriction += ReportRestriction.kStartRestrictionChar + restriction.GUID + ReportRestriction.kStopRestrictionChar;

            //Then execute it
            ReportExecution execution = new ReportExecution() { Report = report };
            execution.Execute();
            while (report.Status != ReportStatus.Executed) System.Threading.Thread.Sleep(100);
            string result = execution.GenerateHTMLResult();
            Process.Start(result);
        }
    }
}
