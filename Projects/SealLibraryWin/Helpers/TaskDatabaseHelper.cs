//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Seal.Model;
using System;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using Npgsql;
using System.Transactions;
using System.Globalization;
using System.Data.SQLite;
using System.Runtime.Intrinsics.X86;

namespace Seal.Helpers
{
    /// <summary>
    /// Custom delegate to build the CREATE TABLE command for a table
    /// </summary>
    public delegate string CustomGetTableCreateCommand(DataTable table);

    /// <summary>
    /// Custom delegate to build the comma-separated list of column names of a table
    /// </summary>
    public delegate string CustomGetTableColumnNames(DataTable table);
    /// <summary>
    /// Custom delegate to get the database column name of a column
    /// </summary>
    public delegate string CustomGetTableColumnName(DataColumn col);
    /// <summary>
    /// Custom delegate to get the database column type of a column
    /// </summary>
    public delegate string CustomGetTableColumnType(DataColumn col);

    /// <summary>
    /// Custom delegate to build the comma-separated list of values of a row for an INSERT statement
    /// </summary>
    public delegate string CustomGetTableColumnValues(DataRow row, string dateTimeFormat);
    /// <summary>
    /// Custom delegate to get the SQL value of a column of a row for an INSERT statement
    /// </summary>
    public delegate string CustomGetTableColumnValue(DataRow row, DataColumn col, string datetimeFormat);

    /// <summary>
    /// Custom delegate to load a DataTable from a connection and a SQL SELECT statement
    /// </summary>
    public delegate DataTable CustomLoadDataTable(ConnectionType connectionType, string connectionString, string sql, DbConnection openConnection = null);
    /// <summary>
    /// Custom delegate to load a DataTable from an Excel file
    /// </summary>
    public delegate DataTable CustomLoadDataTableFromExcel(string excelPath, string tabName = "", int startRow = 1, int startCol = 1, int endCol = 0, int endRow = 0, bool hasHeader = true);
    /// <summary>
    /// Custom delegate to load a DataTable from a CSV file
    /// </summary>
    public delegate DataTable CustomLoadDataTableFromCSV(string csvPath, char? separator = null);

    /// <summary>
    /// Custom delegate to detect column types and convert the values of a table
    /// </summary>
    public delegate DataTable CustomDetectAndConvertTypes(DataTable sourceTable);

    /// <summary>
    /// Helper class to perform database operations (load, create and insert tables, execute SQL statements), used from Razor scripts of report tasks
    /// </summary>
    public class TaskDatabaseHelper
    {
        //Config, may be overwritten
        /// <summary>
        /// Database column type used for character columns (if empty, the default of the current database type is used)
        /// </summary>
        public string ColumnCharType = "";
        /// <summary>
        /// Database column type used for numeric columns (if empty, the default of the current database type is used)
        /// </summary>
        public string ColumnNumericType = "";
        /// <summary>
        /// Database column type used for integer columns (if empty, the default of the current database type is used)
        /// </summary>
        public string ColumnIntegerType = "";
        /// <summary>
        /// Database column type used for date time columns (if empty, the default of the current database type is used)
        /// </summary>
        public string ColumnDateTimeType = "";
        /// <summary>
        /// Statement added before the INSERT commands (if empty, the default of the current database type is used, e.g. 'begin' for Oracle)
        /// </summary>
        public string InsertStartCommand = "";
        /// <summary>
        /// Statement added after the INSERT commands (if empty, the default of the current database type is used, e.g. 'end;' for Oracle)
        /// </summary>
        public string InsertEndCommand = "";
        /// <summary>
        /// 0 = means auto size
        /// </summary>
        public int ColumnCharLength = 0;
        /// <summary>
        /// Char length taken if the table loaded as no row
        /// </summary>
        public int NoRowsCharLength = 50;
        /// <summary>
        /// 0 = Load all records in one
        /// </summary>
        public int LoadBurstSize = 0;
        /// <summary>
        /// Sort column used if LoadBurstSize is specified
        /// </summary>
        public string LoadSortColumn = "";
        /// <summary>
        /// If true, a DbDataAdapter is used to fill the DataTable instead of a DataReader
        /// </summary>
        public bool UseDbDataAdapter = false;
        /// <summary>
        /// Number of rows inserted per INSERT batch
        /// </summary>
        public int InsertBurstSize = 2000;
        /// <summary>
        /// If set, limit the number of decimals for numeric values
        /// </summary>
        public int MaxDecimalNumber = -1;
        /// <summary>
        /// Default encoding used to load CSV files
        /// </summary>
        public Encoding DefaultEncoding = Encoding.Default;
        /// <summary>
        /// If true, text values are trimmed before insert
        /// </summary>
        public bool TrimText = true;
        /// <summary>
        /// If true, carriage returns and line feeds are replaced by spaces in text values before insert
        /// </summary>
        public bool RemoveCrLf = false;
        /// <summary>
        /// /If true, insert command is built with multi rows 
        /// </summary>
        public bool UseMultiRowsInsert = false;
        /// <summary>
        /// Optional table hints for insert
        /// </summary>
        public string InsertTableHints = "";

        /// <summary>
        /// If  true, column types can be detected and values converted during a table load to database (For CSV and Excel load)
        /// </summary>
        public bool TableLoadDetectAndConvertTypes = false;

        /// <summary>
        /// If true, the SQL commands executed are traced in the DebugLog
        /// </summary>
        public bool DebugMode = false;
        /// <summary>
        /// Log of the SQL commands executed when DebugMode is true
        /// </summary>
        public StringBuilder DebugLog = new StringBuilder();
        /// <summary>
        /// Command timeout in seconds used for SELECT statements (0 means no timeout)
        /// </summary>
        public int SelectTimeout = 0;
        /// <summary>
        /// Command timeout in seconds used to execute statements (0 means no timeout)
        /// </summary>
        public int ExecuteTimeout = 0;

        /// <summary>
        /// If set, custom delegate used instead of the default implementation to build the CREATE TABLE command
        /// </summary>
        public CustomGetTableCreateCommand MyGetTableCreateCommand = null;

        /// <summary>
        /// If set, custom delegate used instead of the default implementation to build the list of column names
        /// </summary>
        public CustomGetTableColumnNames MyGetTableColumnNames = null;
        /// <summary>
        /// If set, custom delegate used instead of the default implementation to get a database column name
        /// </summary>
        public CustomGetTableColumnName MyGetTableColumnName = null;
        /// <summary>
        /// If set, custom delegate used instead of the default implementation to get a database column type
        /// </summary>
        public CustomGetTableColumnType MyGetTableColumnType = null;

        /// <summary>
        /// If set, custom delegate used instead of the default implementation to build the list of values of a row for an INSERT statement
        /// </summary>
        public CustomGetTableColumnValues MyGetTableColumnValues = null;
        /// <summary>
        /// If set, custom delegate used instead of the default implementation to get the SQL value of a column for an INSERT statement
        /// </summary>
        public CustomGetTableColumnValue MyGetTableColumnValue = null;

        /// <summary>
        /// If set, custom delegate used instead of the default implementation to load a DataTable from a connection
        /// </summary>
        public CustomLoadDataTable MyLoadDataTable = null;
        /// <summary>
        /// If set, custom delegate used instead of the default implementation to load a DataTable from an Excel file
        /// </summary>
        public CustomLoadDataTableFromExcel MyLoadDataTableFromExcel = null;
        /// <summary>
        /// If set, custom delegate used instead of the default implementation to load a DataTable from a CSV file
        /// </summary>
        public CustomLoadDataTableFromCSV MyLoadDataTableFromCSV = null;

        /// <summary>
        /// If set, custom delegate used instead of the default implementation to detect column types and convert the values of a table
        /// </summary>
        public CustomDetectAndConvertTypes MyDetectAndConvertTypes = null;

        string _defaultColumnCharType = "";
        string _defaultColumnIntegerType = "";
        string _defaultColumnNumericType = "";
        string _defaultColumnDateTimeType = "";
        string _defaultInsertStartCommand = "";
        string _defaultInsertEndCommand = "";

        /// <summary>
        /// Returns a valid database object name from a given name: invalid characters are replaced and the name is quoted according to the current database type
        /// </summary>
        public string GetDatabaseName(string name)
        {
            char[] chars = new char[] { '-', '\"', '\'', '[', ']', '`', '(', ')', '/', '%', '\r', '\t', '\n' };
            var result = chars.Aggregate(name, (c1, c2) => c1.Replace(c2, '_'));
            if (DatabaseType == DatabaseType.MSSQLServer) result = "[" + result + "]";
            else if (DatabaseType == DatabaseType.Oracle) result = Helper.QuoteDouble(result);
            else if (DatabaseType == DatabaseType.PostgreSQL) result = Helper.QuoteDouble(result);
            else if (DatabaseType == DatabaseType.SQLite) result = "" + result + "";
            else if (DatabaseType == DatabaseType.MySQL) result = "`" + result + "`";
            else result = result.Replace(" ", "_");
            return result;
        }

        /// <summary>
        /// Returns the final INSERT command with the insert start and end commands added
        /// </summary>
        public string GetInsertCommand(string sql)
        {
            return Helper.IfNullOrEmpty(InsertStartCommand, _defaultInsertStartCommand) + " " + sql + " " + Helper.IfNullOrEmpty(InsertEndCommand, _defaultInsertEndCommand);
        }

        /// <summary>
        /// Load a DataTable from an ODBC connection using a SQL SELECT statement
        /// </summary>
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

        /// <summary>
        /// Load a DataTable from a connection using a SQL SELECT statement (an open connection may be provided)
        /// </summary>
        public DataTable LoadDataTable(ConnectionType connectionType, string connectionString, string sql, DbConnection openConnection = null)
        {
            DataTable table = new DataTable();
            try
            {
                if (MyLoadDataTable != null) return MyLoadDataTable(connectionType, connectionString, sql, openConnection);
                var connection = openConnection;
                try
                {
                    if (connection == null)
                    {
                        connection = Helper.DbConnectionFromConnectionString(connectionType, connectionString);
                        connection.Open();
                    }
                    if (UseDbDataAdapter)
                    {
                        DbDataAdapter adapter = null;
                        if (connection is OdbcConnection) adapter = new OdbcDataAdapter(sql, (OdbcConnection)connection);
                        else if (connection is SqlConnection) adapter = new SqlDataAdapter(sql, (SqlConnection)connection);
                        else if (connection is MySqlConnector.MySqlConnection) adapter = new MySqlConnector.MySqlDataAdapter(sql, (MySqlConnector.MySqlConnection)connection);
                        else if (connection is OracleConnection) adapter = new OracleDataAdapter(sql, (OracleConnection)connection);
                        else if (connection is NpgsqlConnection) adapter = new NpgsqlDataAdapter(sql, (NpgsqlConnection)connection);
                        else if (connection is SQLiteConnection) adapter = new SQLiteDataAdapter(sql, (SQLiteConnection)connection);
                        else adapter = new OleDbDataAdapter(sql, (OleDbConnection)connection);
                        adapter.SelectCommand.CommandTimeout = SelectTimeout;
                        adapter.Fill(table);
                    }
                    else
                    {
                        DbCommand cmd = null;
                        if (connection is OdbcConnection) cmd = new OdbcCommand(sql, (OdbcConnection)connection);
                        else if (connection is SqlConnection) cmd = new SqlCommand(sql, (SqlConnection)connection);
                        else if (connection is MySqlConnector.MySqlConnection) cmd = new MySqlConnector.MySqlCommand(sql, (MySqlConnector.MySqlConnection)connection);
                        else if (connection is OracleConnection) cmd = new OracleCommand(sql, (OracleConnection)connection);
                        else if (connection is NpgsqlConnection) cmd = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
                        else if (connection is SQLiteConnection) cmd = new SQLiteCommand(sql, (SQLiteConnection)connection);
                        else cmd = new OleDbCommand(sql, (OleDbConnection)connection);
                        cmd.CommandTimeout = SelectTimeout;
                        cmd.CommandType = CommandType.Text;

                        DbDataReader dr = cmd.ExecuteReader();

                        DataTable schemaTable = dr.GetSchemaTable();
                        foreach (DataRow dataRow in schemaTable.Rows)
                        {
                            DataColumn dataColumn = new DataColumn();
                            dataColumn.ColumnName = dataRow["ColumnName"].ToString();
                            dataColumn.DataType = Type.GetType(dataRow["DataType"].ToString());
                            if (dataRow["IsReadOnly"] is bool) dataColumn.ReadOnly = (bool)dataRow["IsReadOnly"];
                            if (dataRow["IsAutoIncrement"] is bool) dataColumn.AutoIncrement = (bool)dataRow["IsAutoIncrement"];
                            if (dataRow["IsUnique"] is bool) dataColumn.Unique = (bool)dataRow["IsUnique"];

                            for (int i = 0; i < table.Columns.Count; i++)
                            {
                                if (dataColumn.ColumnName == table.Columns[i].ColumnName)
                                {
                                    dataColumn.ColumnName += "_" + table.Columns.Count.ToString();
                                }
                            }
                            table.Columns.Add(dataColumn);
                        }

                        while (dr.Read())
                        {
                            DataRow dataRow = table.NewRow();
                            for (int i = 0; i < table.Columns.Count; i++)
                            {
                                dataRow[i] = dr[i];
                            }
                            table.Rows.Add(dataRow);
                        }

                        dr.Close();
                    }
                }
                finally
                {
                    connection?.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error got when executing '{0}':\r\n{1}\r\n", sql, ex.Message));
            }

            return table;
        }

        /// <summary>
        /// Returns true if a row of an Excel worksheet is empty
        /// </summary>
        public bool IsRowEmpty(ExcelWorksheet worksheet, int row, int startCol, int colCount)
        {
            bool rowEmpty = true;
            for (int i = startCol; i <= startCol + colCount; i++)
            {
                if (worksheet.Cells[row, i].Value != null)
                {
                    rowEmpty = false;
                    break;
                }
            }
            return rowEmpty;
        }


        /// <summary>
        /// Detect the column types (integer, double, date time) of a source table containing string columns and return a new table with converted values
        /// </summary>
        public DataTable DetectAndConvertTypes(DataTable sourceTable)
        {
            if (MyDetectAndConvertTypes != null) return MyDetectAndConvertTypes(sourceTable);

            var resultTable = new DataTable();
            //New types
            foreach (DataColumn column in sourceTable.Columns)
            {
                var finalType = typeof(string);
                bool typeOk = true;
                int cnt = 0;

                if (finalType == typeof(string))
                {
                    //Try integer
                    foreach (DataRow row in sourceTable.Rows)
                    {
                        if (column.DataType == typeof(string) && !row.IsNull(column))
                        {
                            var str = row[column].ToString();
                            int value;
                            if (!string.IsNullOrEmpty(str))
                            {
                                bool isOk = false;
                                if (int.TryParse(str, out value)) isOk = true;
                                if (!isOk)
                                {
                                    typeOk = false;
                                    break;
                                }
                                cnt++;
                            }
                        }
                    }
                    if (typeOk && cnt > 0) finalType = typeof(int);
                }

                if (finalType == typeof(string))
                {
                    typeOk = true;
                    cnt = 0;
                    //Try double
                    foreach (DataRow row in sourceTable.Rows)
                    {
                        if (column.DataType == typeof(string) && !row.IsNull(column))
                        {
                            var str = row[column].ToString();
                            double value;
                            if (!string.IsNullOrEmpty(str))
                            {
                                bool isOk = false;
                                if (double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out value)) isOk = true;
                                else if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) isOk = true;
                                if (!isOk) {
                                    typeOk = false;
                                    break;
                                }
                                cnt++;
                            }
                        }
                    }
                    if (typeOk && cnt > 0) finalType = typeof(double);
                }

                if (finalType == typeof(string))
                {
                    typeOk = true;
                    cnt = 0;
                    //Try DateTime
                    foreach (DataRow row in sourceTable.Rows)
                    {
                        if (column.DataType == typeof(string) && !row.IsNull(column))
                        {
                            var str = row[column].ToString();
                            DateTime value;
                            if (!string.IsNullOrEmpty(str))
                            {
                                bool isOk = false;
                                if (DateTime.TryParse(str, CultureInfo.CurrentCulture, out value)) isOk = true;
                                else if (DateTime.TryParse(str, CultureInfo.InvariantCulture, out value)) isOk = true;
                                if (!isOk) {
                                    typeOk = false;
                                    break;
                                }
                                cnt++;
                            }
                        }
                    }
                    if (typeOk && cnt > 0) finalType = typeof(DateTime);
                }

                resultTable.Columns.Add(column.ColumnName, finalType);
            }

            //Then values
            foreach (DataRow row in sourceTable.Rows)
            {
                var values = new List<object>();
                for (int i=0; i < sourceTable.Columns.Count; i++)
                {
                    var str = row[i].ToString();
                    var destColumn = resultTable.Columns[i];
                    if (destColumn.DataType == typeof(int))
                    {
                        int value;
                        if (int.TryParse(str, out value)) values.Add(value);
                        else values.Add(null);
                    }
                    else if (destColumn.DataType == typeof(double))
                    {
                        double value;
                        if (double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out value)) values.Add(value);
                        else if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) values.Add(value);
                        else values.Add(null);
                    }
                    else if (destColumn.DataType == typeof(DateTime))
                    {
                        DateTime value;
                        if (DateTime.TryParse(str, CultureInfo.CurrentCulture, out value)) values.Add(value);
                        else if (DateTime.TryParse(str, CultureInfo.InvariantCulture, out value)) values.Add(value);
                        else values.Add(null);
                    }
                    else
                    {
                        values.Add(str);
                    }
                }
                resultTable.Rows.Add(values.ToArray());
            }

            return resultTable;
        }

        /// <summary>
        /// Load a DataTable from an Excel tab. A start row, and/or column can be specified. An end row and/or column can be specified.
        /// </summary>
        public DataTable LoadDataTableFromExcel(string excelPath, string tabName = "", int startRow = 1, int startCol = 1, int endCol = 0, int endRow = 0, bool hasHeader = true)
        {
            if (MyLoadDataTableFromExcel != null) return MyLoadDataTableFromExcel(excelPath, tabName, startRow, startCol, endCol, endRow, hasHeader);

            var result = ExcelHelper.LoadDataTableFromExcel(excelPath, tabName, startRow, startCol, endCol, endRow, hasHeader);
            if (TableLoadDetectAndConvertTypes) result = DetectAndConvertTypes(result);
            return result;
        }


        /// <summary>
        /// Load a DataTable from a CSV file
        /// </summary>
        public DataTable LoadDataTableFromCSV(string csvPath, char? separator = null, Encoding encoding = null, bool noHeader = false)
        {
            if (MyLoadDataTableFromCSV != null) return MyLoadDataTableFromCSV(csvPath, separator);

            var result = ExcelHelper.LoadDataTableFromCSV(csvPath, separator, encoding ?? DefaultEncoding, noHeader);
            if (TableLoadDetectAndConvertTypes) result = DetectAndConvertTypes(result);
            return result;
        }


        /// <summary>
        /// Load a DataTable from a CSV file using the Microsoft Visual Basic Parser
        /// </summary>
        public DataTable LoadDataTableFromCSVVBParser(string csvPath, char? separator = null, Encoding encoding = null)
        {
            if (MyLoadDataTableFromCSV != null) return MyLoadDataTableFromCSV(csvPath, separator);

            var result = ExcelHelper.LoadDataTableFromCSVVBParser(csvPath, separator, encoding ?? DefaultEncoding);
            if (TableLoadDetectAndConvertTypes) result = DetectAndConvertTypes(result);
            return result;
        }

        /// <summary>
        /// Current database type used for the default configuration (set by SetDatabaseDefaultConfiguration)
        /// </summary>
        public DatabaseType DatabaseType = DatabaseType.MSSQLServer;
        /// <summary>
        /// Set the default configuration values (column types, insert commands) for a given database type
        /// </summary>
        public void SetDatabaseDefaultConfiguration(DatabaseType type)
        {
            DatabaseType = type;
            if (type == DatabaseType.Oracle)
            {
                _defaultColumnCharType = "varchar2";
                _defaultColumnNumericType = "number(18,5)";
                _defaultColumnIntegerType = "number(12)";
                _defaultColumnDateTimeType = "date";
                _defaultInsertStartCommand = "begin";
                _defaultInsertEndCommand = "end;";
            }
            else if (type == DatabaseType.PostgreSQL)
            {
                _defaultColumnCharType = "varchar";
                _defaultColumnNumericType = "numeric(18,5)";
                _defaultColumnIntegerType = "integer";
                _defaultColumnDateTimeType = "timestamp";
                _defaultInsertStartCommand = "";
                _defaultInsertEndCommand = "";
            }
            else if (type == DatabaseType.SQLite)
            {
                _defaultColumnCharType = "varchar";
                _defaultColumnNumericType = "numeric(18,5)";
                _defaultColumnIntegerType = "integer";
                _defaultColumnDateTimeType = "timestamp";
                _defaultInsertStartCommand = "";
                _defaultInsertEndCommand = "";
            }
            else
            {
                //Default, tested on SQLServer...
                _defaultColumnCharType = "varchar";
                _defaultColumnNumericType = "numeric(18,5)";
                _defaultColumnIntegerType = "int";
                _defaultColumnDateTimeType = "datetime2";
                _defaultInsertStartCommand = "";
                _defaultInsertEndCommand = "";
            }
        }


        /// <summary>
        /// Execute a DbCommand (the command is traced in the DebugLog if DebugMode is true)
        /// </summary>
        public void ExecuteCommand(DbCommand command)
        {
            if (DebugMode) DebugLog.AppendLine("Executing SQL Command\r\n" + command.CommandText);
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing SQL:\r\n{0}\r\n\r\n{1}", command.CommandText, ex.Message));
            }
        }

        /// <summary>
        /// Returns a DbCommand created from a DbConnection
        /// </summary>
        public DbCommand GetDbCommand(DbConnection connection)
        {
            DbCommand result = null;
            if (connection is OdbcConnection) result = ((OdbcConnection)connection).CreateCommand();
            else if (connection is SqlConnection) result = ((SqlConnection)connection).CreateCommand();
            else if (connection is SqlConnection) result = ((SqlConnection)connection).CreateCommand();
            else if (connection is MySqlConnector.MySqlConnection) result = ((MySqlConnector.MySqlConnection)connection).CreateCommand();
            else if (connection is OracleConnection) result = ((OracleConnection)connection).CreateCommand();
            else if (connection is NpgsqlConnection) result = ((NpgsqlConnection)connection).CreateCommand();
            else if (connection is SQLiteConnection) result = ((SQLiteConnection)connection).CreateCommand();
            else result = ((OleDbConnection)connection).CreateCommand();
            result.CommandTimeout = SelectTimeout;
            return result;
        }

        /// <summary>
        /// Execute a SQL statement on a connection. Several statements may be executed using a commands separator.
        /// </summary>
        public void ExecuteNonQuery(ConnectionType connectionType, string connectionString, string sql, string commandsSeparator = null, DbConnection openConnection = null)
        {
            var connection = openConnection;
            try
            {
                if (connection == null)
                {
                    connection = Helper.DbConnectionFromConnectionString(connectionType, connectionString);
                    connection.Open();
                }
                DbCommand command = GetDbCommand(connection);
                string[] commandTexts = new string[] { sql };
                if (!string.IsNullOrEmpty(commandsSeparator))
                {
                    commandTexts = sql.Split(new string[] { commandsSeparator }, StringSplitOptions.RemoveEmptyEntries);
                }
                foreach (var commandText in commandTexts)
                {
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                connection?.Close();
            }
        }

        /// <summary>
        /// Execute a SQL statement on a connection and return the first column of the first row of the result
        /// </summary>
        public object ExecuteScalar(ConnectionType connectionType, string connectionString, string sql, DbConnection openConnection = null)
        {
            object result = null;
            var connection = openConnection;
            try
            {
                if (connection == null)
                {
                    connection = Helper.DbConnectionFromConnectionString(connectionType, connectionString);
                    connection.Open();
                }
                DbCommand command = GetDbCommand(connection);
                command.CommandText = sql;
                result = command.ExecuteScalar();
            }
            finally
            {
                connection?.Close();
            }
            return result;
        }


        /// <summary>
        /// Load a DataTable from a MetaConnection using a SQL SELECT statement
        /// </summary>
        public DataTable LoadDataTable(MetaConnection connection, string sql)
        {
            return LoadDataTable(connection.ConnectionType, connection.FullConnectionString, sql, connection.GetOpenConnection());
        }
        /// <summary>
        /// Load a list of strings from a MetaConnection using a SQL SELECT statement (first column of the result)
        /// </summary>
        public List<string> LoadStringList(MetaConnection connection, string sql)
        {
            return (from r in LoadDataTable(connection.ConnectionType, connection.FullConnectionString, sql, connection.GetOpenConnection()).AsEnumerable() select r[0].ToString()).ToList();
        }

        /// <summary>
        /// Execute a SQL statement on a MetaConnection. Several statements may be executed using a commands separator.
        /// </summary>
        public void ExecuteNonQuery(MetaConnection connection, string sql, string commandsSeparator = null)
        {
            ExecuteNonQuery(connection.ConnectionType, connection.FullConnectionString, sql, commandsSeparator, connection.GetOpenConnection());
        }

        /// <summary>
        /// Execute a SQL statement on a MetaConnection and return the first column of the first row of the result
        /// </summary>
        public object ExecuteScalar(MetaConnection connection, string sql)
        {
            return ExecuteScalar(connection.ConnectionType, connection.FullConnectionString, sql, connection.GetOpenConnection());
        }


        /// <summary>
        /// Drop and create a table in the database from a DataTable definition
        /// </summary>
        public void CreateTable(DbCommand command, DataTable table)
        {
            try
            {
                command.CommandText = string.Format("drop table {0}", GetDatabaseName(table.TableName));
                ExecuteCommand(command);
            }
            catch { }
            command.CommandText = GetTableCreateCommand(table);
            ExecuteCommand(command);
        }

        /// <summary>
        /// Insert the rows of a DataTable in the database table within a transaction (rows may be deleted first)
        /// </summary>
        public void InsertTable(DbCommand command, DataTable table, string dateTimeFormat, bool deleteFirst)
        {
            DbTransaction transaction = command.Connection.BeginTransaction();
            int cnt = 0;
            try
            {
                command.Transaction = transaction;
                var tableName = GetDatabaseName(table.TableName);
                if (deleteFirst)
                {
                    command.CommandText = $"delete from {tableName}";
                    ExecuteCommand(command);
                }

                StringBuilder sql = new StringBuilder("");
                string sqlTemplate = "";
                if (UseMultiRowsInsert)
                {
                    //use the multi rows insert
                    sqlTemplate = string.Format("insert into {0} {1} ({2}) values\r\n", tableName, InsertTableHints, GetTableColumnNames(table));
                    sql.Append(sqlTemplate);
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        DataRow row = table.Rows[i];
                        sql.AppendFormat("({0})", GetTableColumnValues(row, dateTimeFormat));
                        cnt++;
                        if (cnt % InsertBurstSize == 0)
                        {
                            command.CommandText = GetInsertCommand(sql.ToString());
                            ExecuteCommand(command);
                            sql = new StringBuilder(i != table.Rows.Count - 1 ? sqlTemplate : "");
                        }
                        else
                        {
                            if (i != table.Rows.Count - 1) sql.Append(",\r\n");
                        }
                    }

                    if (sql.Length != 0)
                    {
                        command.CommandText = GetInsertCommand(sql.ToString());
                        ExecuteCommand(command);
                    }
                }
                else
                {
                    //insert for standard SQL
                    sqlTemplate = string.Format("insert into {0} {1} ({2})", tableName, InsertTableHints, GetTableColumnNames(table)) + " values ({0});\r\n";
                    foreach (DataRow row in table.Rows)
                    {
                        sql.AppendFormat(sqlTemplate, GetTableColumnValues(row, dateTimeFormat));
                        cnt++;
                        if (cnt % InsertBurstSize == 0)
                        {
                            command.CommandText = GetInsertCommand(sql.ToString());
                            ExecuteCommand(command);
                            sql = new StringBuilder("");
                        }
                    }

                    if (sql.Length != 0)
                    {
                        command.CommandText = GetInsertCommand(sql.ToString());
                        ExecuteCommand(command);
                    }
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        /// <summary>
        /// Insert rows given as arrays of SQL literal values in a database table within a transaction (rows may be deleted first)
        /// </summary>
        public void RawInsertTable(DbCommand command, string tableName, string[] columns, List<string[]> rows, bool deleteFirst)
        {
            DbTransaction transaction = command.Connection.BeginTransaction();
            int cnt = 0;
            try
            {
                command.Transaction = transaction;
                tableName = GetDatabaseName(tableName);
                if (deleteFirst)
                {
                    command.CommandText = $"delete from {tableName}";
                    ExecuteCommand(command);
                }

                StringBuilder sql = new StringBuilder("");
                string sqlTemplate = "";
                if (UseMultiRowsInsert)
                {
                    //use the multi rows insert
                    sqlTemplate = string.Format("insert into {0} {1} ({2}) values\r\n", tableName, InsertTableHints, string.Join(",", columns));
                    sql.Append(sqlTemplate);
                    for (int i = 0; i < rows.Count; i++)
                    {
                        sql.AppendFormat("({0})", string.Join(",", rows[i]));
                        cnt++;
                        if (cnt % InsertBurstSize == 0)
                        {
                            command.CommandText = GetInsertCommand(sql.ToString());
                            ExecuteCommand(command);
                            sql = new StringBuilder(i != rows.Count - 1 ? sqlTemplate : "");
                        }
                        else
                        {
                            if (i != rows.Count - 1) sql.Append(",\r\n");
                        }
                    }

                    if (sql.Length != 0)
                    {
                        command.CommandText = GetInsertCommand(sql.ToString());
                        ExecuteCommand(command);
                    }
                }
                else
                {
                    //insert for standard SQL
                    sqlTemplate = string.Format("insert into {0} {1} ({2})", tableName, InsertTableHints, string.Join(",", columns)) + " values ({0});\r\n";
                    for (int i = 0; i < rows.Count; i++)
                    {
                        sql.AppendFormat(sqlTemplate, string.Join(",", rows[i]));
                        cnt++;
                        if (cnt % InsertBurstSize == 0)
                        {
                            command.CommandText = GetInsertCommand(sql.ToString());
                            ExecuteCommand(command);
                            sql = new StringBuilder("");
                        }
                    }

                    if (sql.Length != 0)
                    {
                        command.CommandText = GetInsertCommand(sql.ToString());
                        ExecuteCommand(command);
                    }
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        /// <summary>
        /// Default implementation to build the CREATE TABLE command for a DataTable
        /// </summary>
        public string RootGetTableCreateCommand(DataTable table)
        {
            StringBuilder result = new StringBuilder();
            foreach (DataColumn col in table.Columns)
            {
                if (result.Length > 0) result.Append(',');
                result.AppendFormat("{0} ", GetTableColumnName(col));
                result.Append(GetTableColumnType(col));
                result.Append(" NULL");
            }
            return string.Format("CREATE TABLE {0} ({1})", GetDatabaseName(table.TableName), result);
        }


        /// <summary>
        /// Returns the CREATE TABLE command for a DataTable (uses MyGetTableCreateCommand if defined)
        /// </summary>
        public string GetTableCreateCommand(DataTable table)
        {
            if (MyGetTableCreateCommand != null) return MyGetTableCreateCommand(table);
            return RootGetTableCreateCommand(table);
        }

        /// <summary>
        /// Default implementation to build the comma-separated list of column names of a table
        /// </summary>
        public string RootGetTableColumnNames(DataTable table)
        {
            StringBuilder result = new StringBuilder();
            foreach (DataColumn col in table.Columns)
            {
                if (result.Length > 0) result.Append(',');
                result.AppendFormat("{0}", GetTableColumnName(col));
            }
            return result.ToString();
        }

        /// <summary>
        /// Returns the comma-separated list of column names of a table (uses MyGetTableColumnNames if defined)
        /// </summary>
        public string GetTableColumnNames(DataTable table)
        {
            if (MyGetTableColumnNames != null) return MyGetTableColumnNames(table);
            return RootGetTableColumnNames(table);
        }

        /// <summary>
        /// Default implementation to get the database column name of a column
        /// </summary>
        public string RootGetTableColumnName(DataColumn col)
        {
            return GetDatabaseName(col.ColumnName);
        }

        /// <summary>
        /// Returns the database column name of a column (uses MyGetTableColumnName if defined)
        /// </summary>
        public string GetTableColumnName(DataColumn col)
        {
            if (MyGetTableColumnName != null) return MyGetTableColumnName(col);
            return RootGetTableColumnName(col);
        }

        /// <summary>
        /// Returns true if the column has a numeric type
        /// </summary>
        public bool IsNumeric(DataColumn col)
        {
            if (col == null) return false;
            // Make this const
            var numericTypes = new[] { typeof(Byte), typeof(Decimal), typeof(Double), typeof(Int16), typeof(Int32), typeof(Int64), typeof(SByte), typeof(Single), typeof(UInt16), typeof(UInt32), typeof(UInt64) };
            return numericTypes.Contains(col.DataType);
        }

        /// <summary>
        /// Default implementation to get the database column type of a column
        /// </summary>
        public string RootGetTableColumnType(DataColumn col)
        {
            StringBuilder result = new StringBuilder();
            if (IsNumeric(col))
            {
                //Check for integer
                bool isInteger = true;
                foreach (DataRow row in col.Table.Rows)
                {
                    int a;
                    if (!row.IsNull(col) && !int.TryParse(row[col].ToString(), out a))
                    {
                        isInteger = false;
                        break;
                    }
                }

                if (isInteger) result.Append(Helper.IfNullOrEmpty(ColumnIntegerType, _defaultColumnIntegerType));
                else result.Append(Helper.IfNullOrEmpty(ColumnNumericType, _defaultColumnNumericType));
            }
            else if (col.DataType.Name == "DateTime" || col.DataType.Name == "Date")
            {
                result.Append(Helper.IfNullOrEmpty(ColumnDateTimeType, _defaultColumnDateTimeType));
            }
            else
            {
                int len = col.MaxLength;
                if (len <= 0) len = ColumnCharLength;
                if (ColumnCharLength <= 0)
                {
                    //auto size
                    len = 1;
                    foreach (DataRow row in col.Table.Rows)
                    {
                        if (row[col].ToString().Length > len) len = row[col].ToString().Length + 1;
                    }

                    if (col.Table.Rows.Count == 0) len = NoRowsCharLength;
                }
                if (ColumnCharLength <= 0 && DatabaseType == DatabaseType.MSSQLServer && len > 8000)
                    result.AppendFormat("{0}(max)", Helper.IfNullOrEmpty(ColumnCharType, _defaultColumnCharType));
                else
                    result.AppendFormat("{0}({1})", Helper.IfNullOrEmpty(ColumnCharType, _defaultColumnCharType), len);
            }
            return result.ToString();
        }

        /// <summary>
        /// Returns the database column type of a column (uses MyGetTableColumnType if defined)
        /// </summary>
        public string GetTableColumnType(DataColumn col)
        {
            if (MyGetTableColumnType != null) return MyGetTableColumnType(col);
            return RootGetTableColumnType(col);
        }

        /// <summary>
        /// Default implementation to build the comma-separated list of values of a row for an INSERT statement
        /// </summary>
        public string RootGetTableColumnValues(DataRow row, string dateTimeFormat)
        {
            StringBuilder result = new StringBuilder();
            foreach (DataColumn col in row.Table.Columns)
            {
                if (result.Length > 0) result.Append(',');
                result.Append(GetTableColumnValue(row, col, dateTimeFormat));
            }
            return result.ToString();
        }


        /// <summary>
        /// Returns the comma-separated list of values of a row for an INSERT statement (uses MyGetTableColumnValues if defined)
        /// </summary>
        public string GetTableColumnValues(DataRow row, string dateTimeFormat)
        {
            if (MyGetTableColumnValues != null) return MyGetTableColumnValues(row, dateTimeFormat);
            return RootGetTableColumnValues(row, dateTimeFormat);
        }

        /// <summary>
        /// Default implementation to get the SQL value of a column of a row for an INSERT statement
        /// </summary>
        public string RootGetTableColumnValue(DataRow row, DataColumn col, string dateTimeFormat)
        {
            string result = "";
            if (row.IsNull(col))
            {
                result = "NULL";
            }
            else if (IsNumeric(col))
            {
                result = row[col].ToString().Replace(',', '.');
                if (!(row[col] is int) && MaxDecimalNumber >= 0 && result.Length > MaxDecimalNumber)
                {
                    string[] parts = result.Split('.');
                    if (parts.Length == 2 && parts[1].Length > MaxDecimalNumber)
                    {
                        result = parts[0] + "." + parts[1].Substring(0, MaxDecimalNumber);
                    }
                }
            }
            else if (col.DataType.Name == "DateTime" || col.DataType.Name == "Date")
            {
                result = Helper.QuoteSingle(((DateTime)row[col]).ToString(dateTimeFormat));
            }
            else
            {
                string res = row[col].ToString();
                if (TrimText) res = res.Trim();
                if (RemoveCrLf) res = res.Replace("\r", " ").Replace("\n", " ");
                result = Helper.QuoteSingle(res);
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns the SQL value of a column of a row for an INSERT statement (uses MyGetTableColumnValue if defined)
        /// </summary>
        public string GetTableColumnValue(DataRow row, DataColumn col, string dateTimeFormat)
        {
            if (MyGetTableColumnValue != null) return MyGetTableColumnValue(row, col, dateTimeFormat);
            return RootGetTableColumnValue(row, col, dateTimeFormat);
        }

        /// <summary>
        /// Returns true if two rows have identical values
        /// </summary>
        public bool AreRowsIdentical(DataRow row1, DataRow row2)
        {
            bool result = true;
            if (row1.ItemArray.Length != row2.ItemArray.Length) result = false;
            else
            {
                for (int j = 0; j < row1.ItemArray.Length && result; j++)
                {
                    if (row1[j].ToString() != row2[j].ToString()) result = false;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true if two tables have identical rows and values
        /// </summary>
        public bool AreTablesIdentical(DataTable checkTable1, DataTable checkTable2)
        {
            bool result = true;
            if (checkTable1.Rows.Count != checkTable2.Rows.Count || checkTable1.Columns.Count != checkTable2.Columns.Count) result = false;
            if (checkTable1.Rows.Count != checkTable2.Rows.Count) result = false;
            else
            {
                for (int i = 0; i < checkTable1.Rows.Count && result; i++)
                {
                    if (!AreRowsIdentical(checkTable1.Rows[i], checkTable2.Rows[i])) result = false;
                }
            }
            return result;
        }
    }
}
