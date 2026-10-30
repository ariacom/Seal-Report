//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Linq;

namespace Test
{
    /// <summary>
    /// SqlGuard, hook SecurityUser.CheckSql, limit of the rows loaded and level of the timeout of a model.
    /// </summary>
    [TestClass]
    public class TestSqlGuard
    {
        [TestMethod]
        [DataRow("select 1")]
        [DataRow("select 1;")]
        [DataRow("SELECT a FROM t WHERE b = 'x;y' -- a comment; with a semicolon\n")]
        [DataRow("with c as (select 1 a) select rank() over (order by a) from c")]
        [DataRow("(select 1) union (select 2)")]
        [DataRow("select \"update\", 'delete' from t /* insert; */")]
        [DataRow("select 'it''s'")]
        [DataRow("select date_update from t")]
        public void ReadStatementIsAccepted(string sql)
        {
            Assert.IsNull(SqlGuard.Check(sql));
        }

        [TestMethod]
        [DataRow("set statement_timeout = 0")]
        [DataRow("select 1; set statement_timeout = 0")]
        [DataRow("select 1; select 2;")]
        [DataRow("begin read write")]
        [DataRow("delete from t")]
        [DataRow("with d as (delete from t returning *) select * from d")]
        [DataRow("select * into x from t")]
        [DataRow("select 1 for update")]
        [DataRow("do $$ begin null; end $$")]
        [DataRow("select $$a;b$$")]
        [DataRow("select $x$a$x$")]
        [DataRow("select E'\\''; set statement_timeout = 0; --'")]
        [DataRow("select '\\'")]
        [DataRow("select 'not closed")]
        [DataRow("select /* not closed")]
        [DataRow("select /* /* nested */ 1")]
        [DataRow("select /*! 1 */")]
        [DataRow("")]
        public void OtherTextIsRefused(string sql)
        {
            Assert.IsNotNull(SqlGuard.Check(sql), sql);
        }

        [TestMethod]
        public void PartOfStatement()
        {
            Assert.IsNull(SqlGuard.Check("t.a > 1 and t.b = 'x'", false));
            Assert.IsNull(SqlGuard.Check("rank() over (order by a)", false));
            Assert.IsNotNull(SqlGuard.Check("1=1; set statement_timeout = 0", false));
            Assert.IsNotNull(SqlGuard.Check("a;", false));
        }

        const string TableXml = @"<?xml version=""1.0""?>
<Report xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Models><ReportModel><GUID>m</GUID><Name>model</Name>
    <Table><GUID>t</GUID><Sql>{0}</Sql></Table>
  </ReportModel></Models>
</Report>";

        static SecurityUser GetUser(Func<string, bool, string> check)
        {
            return new SecurityUser(new SealSecurity()) { CheckSql = check };
        }

        [TestMethod]
        public void HookChecksTheSqlOfAReport()
        {
            var xml = string.Format(TableXml, "select 1; set x = 1");
            //No hook: unchanged
            Assert.IsEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Sql, null, GetUser(null)));

            var messages = ReportCodeGuard.Check(xml, null, ReportCodeLevel.Sql, null, GetUser((sql, isStatement) => SqlGuard.Check(sql, isStatement)));
            Assert.HasCount(1, messages);
            StringAssert.Contains(messages[0], "only one SQL statement");

            //Content already saved and Scripts level are not checked
            Assert.IsEmpty(ReportCodeGuard.Check(xml, xml, ReportCodeLevel.Sql, null, GetUser((sql, isStatement) => "refused")));
            Assert.IsEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Scripts, null, GetUser((sql, isStatement) => "refused")));

            Assert.IsEmpty(ReportCodeGuard.Check(string.Format(TableXml, "select 1"), null, ReportCodeLevel.Sql, null, GetUser((sql, isStatement) => isStatement ? SqlGuard.Check(sql) : "not a statement")));
        }

        [TestMethod]
        public void ExceptionOfTheHookRefusesTheSql()
        {
            Assert.IsNotNull(GetUser((sql, isStatement) => throw new Exception("error")).GetSqlRefusal("select 1", true));
            Assert.IsNull(GetUser(null).GetSqlRefusal("select 1; drop table t", true));
        }

        [TestMethod]
        public void ModelTimeoutRequiresTheScriptsLevel()
        {
            var xml = TableXml.Replace("<Table><GUID>t</GUID><Sql>{0}</Sql></Table>", "<CommandTimeout>0</CommandTimeout>");
            Assert.IsNotEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Sql));
            Assert.IsEmpty(ReportCodeGuard.Check(xml, null, ReportCodeLevel.Scripts));
        }

        [TestMethod]
        public void RowsLoadedAreLimited()
        {
            var connectionString = "Data Source=:memory:";
            const string sql = "with recursive n(i) as (select 1 union all select i + 1 from n where i < 5000) select i from n";
            foreach (var useAdapter in new[] { false, true })
            {
                var helper = new TaskDatabaseHelper() { SelectMaxRows = 100, UseDbDataAdapter = useAdapter };
                Assert.AreEqual(100, helper.LoadDataTable(ConnectionType.SQLite, connectionString, sql).Rows.Count);
                helper.SelectMaxRows = 0;
                Assert.AreEqual(5000, helper.LoadDataTable(ConnectionType.SQLite, connectionString, sql).Rows.Count);
            }
        }
    }
}
