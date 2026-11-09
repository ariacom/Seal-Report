//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Helpers;
using Seal.Model;
using System.Linq;

namespace Test
{
    /// <summary>
    /// String literals of the restriction values in the SQL: on MySQL and MariaDB, a backslash ending a value must not escape the closing quote (GHSA-m2jp-w6g7-xgh9).
    /// </summary>
    [TestClass]
    public class TestRestrictionSqlLiteral
    {
        [TestMethod]
        [DataRow(DatabaseType.MySQL, @"\' OR 1=1-- ", @"'\\'' OR 1=1-- '")]
        [DataRow(DatabaseType.MySQL, @"a\b", @"'a\\b'")]
        [DataRow(DatabaseType.MSSQLServer, @"\' OR 1=1-- ", @"'\'' OR 1=1-- '")]
        [DataRow(DatabaseType.PostgreSQL, @"it's", @"'it''s'")]
        [DataRow(DatabaseType.Standard, null, "''")]
        public void QuoteSQLStringLiteral(DatabaseType databaseType, string input, string expected)
        {
            Assert.AreEqual(expected, Helper.QuoteSQLStringLiteral(input, databaseType));
        }

        [TestMethod]
        public void TextRestrictionValueIsEscapedForMySQL()
        {
            var restriction = createRestriction(out var model);
            model.Connection.DatabaseType = DatabaseType.MySQL;
            restriction.Operator = Operator.Equal;
            restriction.Value1 = @"\' OR 1=1-- ";
            StringAssert.Contains(restriction.SQLText, @"('\\'' OR 1=1--')");

            //Contains: the value is inside the wildcards
            restriction.Operator = Operator.Contains;
            restriction.Value1 = @"x\";
            StringAssert.Contains(restriction.SQLText, @"'%x\\%'");

            //Other databases: the backslash is kept as is
            model.Connection.DatabaseType = DatabaseType.MSSQLServer;
            restriction.Operator = Operator.Equal;
            restriction.Value1 = @"x\";
            StringAssert.Contains(restriction.SQLText, @"('x\')");
        }

        static ReportRestriction createRestriction(out ReportModel model)
        {
            var repository = Repository.Create();
            Report report = Report.Create(repository);
            var source = report.Sources.FirstOrDefault(i => i.Name.StartsWith("Northwind"));
            source.MetaData.Tables.Clear();
            var table = source.AddTable(true);
            table.DynamicColumns = true;
            table.Name = "products";
            table.Refresh();
            model = report.Models[0];
            model.SourceGUID = source.GUID;

            var restriction = ReportRestriction.CreateReportRestriction();
            restriction.Source = model.Source;
            restriction.Report = report;
            restriction.Model = model;
            restriction.MetaColumnGUID = table.Columns.FirstOrDefault(i => i.Name == "products.ProductName").GUID;
            restriction.SetDefaults();
            return restriction;
        }
    }
}
