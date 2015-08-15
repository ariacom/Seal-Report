using Seal.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Seal.Helpers
{
    public delegate string CustomGetTableCreateCommand(DataTable table);

    public delegate string CustomGetTableColumnNames(DataTable table);
    public delegate string CustomGetTableColumnName(string columnName);

    public delegate string CustomGetTableColumnValues(DataRow row, string dateTimeFormat);
    public delegate string CustomGetTableColumnValue(DataRow row, DataColumn col, string datetimeFormat);

    public class TaskDatabaseHelper
    {
        //Config, may be overwritten
        public string ColumnCharType = "";
        public string ColumnNumericType = "";
        public string ColumnDateTimeType = "";
        public string InsertStartCommand = "";
        public string InsertEndCommand = "";
        public int ColumnCharLength = 400;
        public int InsertBurstSize = 500;
        public string ExcelOdbcDriver = "Driver={{Microsoft Excel Driver (*.xls, *.xlsx, *.xlsm, *.xlsb)}};DBQ={0}";
        public bool DebugMode = false;
        public StringBuilder DebugLog = new StringBuilder();
        public int SelectTimeout = 0;

        public CustomGetTableCreateCommand MyGetTableCreateCommand = null;

        public CustomGetTableColumnNames MyGetTableColumnNames = null;
        public CustomGetTableColumnName MyGetTableColumnName = null;

        public CustomGetTableColumnValues MyGetTableColumnValues = null;
        public CustomGetTableColumnValue MyGetTableColumnValue = null;

        string _defaultColumnCharType = "";
        string _defaultColumnNumericType = "";
        string _defaultColumnDateTimeType = "";
        string _defaultInsertStartCommand = "";
        string _defaultInsertEndCommand = "";

        public string CleanName(string name)
        {
            return name.Replace("-", "_").Replace(" ", "_").Replace("\"", "_").Replace("'", "_").Replace("[", "_").Replace("]", "_").Replace("/", "_").Replace("%", "_").Replace("(", "_").Replace(")", "_");
        }

        public string GetInsertCommand(string sql)
        {
            return Helper.IfNullOrEmpty(InsertStartCommand, _defaultInsertStartCommand) + " " + sql + " " + Helper.IfNullOrEmpty(InsertEndCommand, _defaultInsertEndCommand);
        }

        public DataTable OdbcLoadDataTable(string odbcConnectionString, string sql)
        {
            DataTable table = new DataTable();
            using (OdbcConnection connection = new OdbcConnection(odbcConnectionString))
            {
                connection.Open();
                OdbcDataAdapter adapter = new OdbcDataAdapter(sql, connection);
                adapter.SelectCommand.CommandTimeout = SelectTimeout;
                adapter.Fill(table);
                connection.Close();
            }
            return table;
        }

        public DataTable LoadDataTable(string connectionString, string sql)
        {
            DataTable table = new DataTable();
            DbDataAdapter adapter = null;
            var connection = Helper.DbConnectionFromConnectionString(connectionString);
            connection.Open();
            if (connection is OdbcConnection) adapter = new OdbcDataAdapter(sql, (OdbcConnection)connection);
            else adapter = new OleDbDataAdapter(sql, (OleDbConnection)connection);
            adapter.SelectCommand.CommandTimeout = SelectTimeout;
            adapter.Fill(table);
            return table;
        }

        public DataTable LoadDataTableFromExcel(string excelPath, string tabName = "")
        {
            string connectionString = string.Format(ExcelOdbcDriver, excelPath);
            string sql = string.Format("select * from [{0}$]", Helper.IfNullOrEmpty(tabName, "Sheet1"));
            return OdbcLoadDataTable(connectionString, sql);

        }

        public void SetDatabaseDefaultConfiguration(DatabaseType type)
        {
            if (type == DatabaseType.Oracle)
            {
                _defaultColumnCharType = "varchar2";
                _defaultColumnNumericType = "number(18,3)";
                _defaultColumnDateTimeType = "date";
                _defaultInsertStartCommand = "begin";
                _defaultInsertEndCommand = "end;";
            }
            else
            {
                //Default, tested on SQLServer...
                _defaultColumnCharType = "varchar";
                _defaultColumnNumericType = "numeric(18,3)";
                _defaultColumnDateTimeType = "datetime";
                _defaultInsertStartCommand = "";
                _defaultInsertEndCommand = "";
            }
        }

        public void CreateTable(DbCommand command, DataTable table)
        {
            try
            {
                command.CommandText = string.Format("drop table {0}", CleanName(table.TableName));
                if (DebugMode) DebugLog.AppendLine("Executing SQL Command\r\n" + command.CommandText);
                command.ExecuteNonQuery();
            }
            catch { }
            command.CommandText = GetTableCreateCommand(table);
            if (DebugMode) DebugLog.AppendLine("Executing SQL Command\r\n" + command.CommandText);
            command.ExecuteNonQuery();
        }

        public void InsertTable(DbCommand command, DataTable table, string dateTimeFormat, bool deleteFirst)
        {
            DbTransaction transaction = command.Connection.BeginTransaction();
            int cnt = 0;
            try
            {
                command.Transaction = transaction;
                if (deleteFirst)
                {
                    command.CommandText = string.Format("delete from {0}", CleanName(table.TableName));
                    command.ExecuteNonQuery();
                }

                StringBuilder sql = new StringBuilder("");
                string sqlTemplate = string.Format("insert into {0} ({1})", CleanName(table.TableName), GetTableColumnNames(table)) + " values ({0});";
                foreach (DataRow row in table.Rows)
                {
                    sql.AppendFormat(sqlTemplate, GetTableColumnValues(row, dateTimeFormat));
                    cnt++;
                    if (cnt % InsertBurstSize == 0)
                    {
                        command.CommandText = GetInsertCommand(sql.ToString());
                        if (DebugMode) DebugLog.AppendLine("Executing SQL Command\r\n" + command.CommandText);
                        command.ExecuteNonQuery();
                        sql = new StringBuilder("");
                    }
                }

                if (sql.Length != 0)
                {
                    command.CommandText = GetInsertCommand(sql.ToString());
                    if (DebugMode) DebugLog.AppendLine("Executing SQL Command\r\n" + command.CommandText);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public string GetTableCreateCommand(DataTable table)
        {
            if (MyGetTableCreateCommand != null) return MyGetTableCreateCommand(table);

            StringBuilder result = new StringBuilder();
            foreach (DataColumn col in table.Columns)
            {
                if (result.Length > 0) result.Append(',');
                result.AppendFormat("{0} ", GetTableColumnName(col.ColumnName));
                if (col.DataType.Name == "Int32" || col.DataType.Name == "Integer" || col.DataType.Name == "Double" || col.DataType.Name == "Decimal" || col.DataType.Name == "Number")
                {
                    result.Append(Helper.IfNullOrEmpty(ColumnNumericType, _defaultColumnNumericType));
                }
                else if (col.DataType.Name == "DateTime")
                {
                    result.Append(Helper.IfNullOrEmpty(ColumnDateTimeType, _defaultColumnDateTimeType));
                }
                else
                {
                    int len = col.MaxLength;
                    if (len <= 0) len = ColumnCharLength;
                    result.AppendFormat("{0}({1})", Helper.IfNullOrEmpty(ColumnCharType, _defaultColumnCharType), len);
                }
                result.Append(" NULL");
            }

            return string.Format("CREATE TABLE {0} ({1})", CleanName(table.TableName), result);
        }

        public string GetTableColumnNames(DataTable table)
        {
            if (MyGetTableColumnNames != null) return MyGetTableColumnNames(table);

            StringBuilder result = new StringBuilder();
            foreach (DataColumn col in table.Columns)
            {
                if (result.Length > 0) result.Append(',');
                result.AppendFormat("{0}", GetTableColumnName(col.ColumnName));
            }
            return result.ToString();

        }

        public string GetTableColumnName(string columnName)
        {
            if (MyGetTableColumnName != null) return MyGetTableColumnName(columnName);

            return CleanName(columnName);
        }

        public string GetTableColumnValues(DataRow row, string dateTimeFormat)
        {
            if (MyGetTableColumnValues != null) return MyGetTableColumnValues(row, dateTimeFormat);

            StringBuilder result = new StringBuilder();
            foreach (DataColumn col in row.Table.Columns)
            {
                if (result.Length > 0) result.Append(',');
                result.Append(GetTableColumnValue(row, col, dateTimeFormat));
            }
            return result.ToString();
        }

        public string GetTableColumnValue(DataRow row, DataColumn col, string dateTimeFormat)
        {
            if (MyGetTableColumnValue != null) return MyGetTableColumnValue(row, col, dateTimeFormat);

            StringBuilder result = new StringBuilder();
            if (row.IsNull(col))
            {
                result.Append("NULL");
            }
            else if (col.DataType.Name == "Integer" || col.DataType.Name == "Double" || col.DataType.Name == "Decimal" || col.DataType.Name == "Number")
            {
                result.AppendFormat(row[col].ToString().Replace(',', '.'));
            }
            else if (col.DataType.Name == "DateTime" || col.DataType.Name == "Date")
            {
                result.Append(Helper.QuoteSingle(((DateTime)row[col]).ToString(dateTimeFormat)));
            }
            else
            {
                string res = row[col].ToString().Replace("\r", " ").Replace("\n", " ");
                if (res.Length > ColumnCharLength) res = res.Substring(0, ColumnCharLength - 1);
                result.Append(Helper.QuoteSingle(res));
            }

            return result.ToString();
        }

        public bool AreTablesIdentical(DataTable checkTable1, DataTable checkTable2)
        {
            bool result = true;
            if (checkTable1.Rows.Count != checkTable2.Rows.Count || checkTable1.Columns.Count != checkTable2.Columns.Count) result = false;
            if (checkTable1.Rows.Count != checkTable2.Rows.Count) result = false;
            else
            {
                for (int i = 0; i < checkTable1.Rows.Count && result; i++)
                {
                    for (int j = 0; j < checkTable1.Columns.Count && result; j++)
                    {
                        if (checkTable1.Rows[i][j].ToString() != checkTable2.Rows[i][j].ToString()) result = false;
                    }
                }
            }
            return result;
        }
    }
}
