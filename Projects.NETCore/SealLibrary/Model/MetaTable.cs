//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Data;
using System.Drawing.Design;
using System.Xml.Serialization;
using Seal.Helpers;
using RazorEngine.Templating;
using System.Globalization;
using System.Data.Common;
using System.Text;

namespace Seal.Model
{
    /// <summary>
    /// A MetaTable defines a table in a database and contains a list of MetaColumns.
    /// </summary>
    public class MetaTable : RootComponent, ReportExecutionLog
    {

        /// <summary>
        /// Creates a basic MetaTable
        /// </summary>
        public static MetaTable Create()
        {
            return new MetaTable() { GUID = Guid.NewGuid().ToString() };
        }

        /// <summary>
        /// Current execution log
        /// </summary>
        [XmlIgnore]
        public ReportExecutionLog Log = null;

        /// <summary>
        /// Logs message in the execution log
        /// </summary>
        public void LogMessage(string message, params object[] args)
        {
            if (Log != null) Log.LogMessage(message, args);
        }

        /// <summary>
        /// Name of the table in the database. The name can be empty if an SQL Statement is specified.
        /// </summary>
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// SQL Select Statement executed to define the table. If empty, the table name is used.
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// DataTable used for No SQL Source 
        /// </summary>
        [XmlIgnore]
        public DataTable NoSQLTable = null;

        /// <summary>
        /// ReportModel set for No SQL Source
        /// </summary>
        [XmlIgnore]
        public ReportModel NoSQLModel = null;

        /// <summary>
        /// The Razor Script used to built the DataTable object that defines the table
        /// </summary>
        public string DefinitionScript { get; set; }

        /// <summary>
        /// The Default Razor Script used to load the data in the table. This can be overwritten in the model.
        /// </summary>
        public string LoadScript { get; set; }

        /// <summary>
        /// If not empty, table alias name used in the SQL statement. The table alias is necessary if a SQL Statement is specified.
        /// </summary>
        public string Alias { get; set; }

        bool _dynamicColumns = false;
        /// <summary>
        /// If true, columns are generated automatically from the Table Name or the SQL Select Statement by reading the database catalog
        /// </summary>
        [DefaultValue(false)]
        public bool DynamicColumns
        {
            get { return _dynamicColumns; }
            set
            {
                _dynamicColumns = value;
            }
        }
        public bool ShouldSerializeDynamicColumns() { return _dynamicColumns; }

        /// <summary>
        /// "If true, the display names of the columns are kept when generated from the source SQL
        /// </summary>
        [DefaultValue(false)]
        public bool KeepColumnNames { get; set; } = false;

        /// <summary>
        /// Type of the table got from database catalog
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// If true, the table must be refreshed for dynamic columns
        /// </summary>
        [DefaultValue(false)]
        public bool MustRefresh { get; set; } = false;
        public bool ShouldSerializeMustRefresh() { return MustRefresh; }

        /// <summary>
        /// SQL Statement executed before the query when the table is involved. The statement may contain Razor script if it starts with '@'.
        /// </summary>
        public string PreSQL { get; set; }

        /// <summary>
        /// SQL Statement executed after the query when the table is involved. The statement may contain Razor script if it starts with '@'.
        /// </summary>
        public string PostSQL { get; set; }

        /// <summary>
        /// If true, errors occuring during the Pre or Post SQL statements are ignored and the execution continues
        /// </summary>
        [DefaultValue(false)]
        public bool IgnorePrePostError { get; set; } = false;
        public bool ShouldSerializeIgnorePrePostError() { return IgnorePrePostError; }

        /// <summary>
        /// Additional SQL added in the WHERE clause when the table is involved in a query. The text may contain Razor script if it starts with '@'.
        /// </summary>
        public string WhereSQL { get; set; }

        /// <summary>
        /// List of MetColumn defined for the table
        /// </summary>
        public List<MetaColumn> Columns { get; set; } = new List<MetaColumn>();
        public bool ShouldSerializeColumns() { return Columns.Count > 0; }

        /// <summary>
        /// Alias name of the table
        /// </summary>
        [XmlIgnore]
        public string AliasName
        {
            get
            {
                if (!string.IsNullOrEmpty(Alias)) return Alias;
                return string.Format("{0}", _name);
            }
        }

        /// <summary>
        /// Full SQL name of the table
        /// </summary>
        [XmlIgnore]
        public string FullSQLName
        {
            get
            {
                if (!string.IsNullOrEmpty(Sql))
                {
                    return string.Format("(\r\n{0}\r\n) {1}", Sql, AliasName);
                }
                else if (!string.IsNullOrEmpty(_name) && !string.IsNullOrEmpty(Alias))
                {
                    return string.Format("{0} {1}", _name, Alias);
                }
                return AliasName;
            }
        }

        /// <summary>
        /// Display name including the type
        /// </summary>
        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(Type)) return AliasName;
                return string.Format("{0} ({1})", AliasName, Type);
            }
        }

        /// <summary>
        /// True if table is the master table
        /// </summary>
        [XmlIgnore]
        public bool IsMasterTable
        {
            get
            {
                return (Alias == MetaData.MasterTableName);
            }
        }

        /// <summary>
        /// True if the table is editable
        /// </summary>
        [XmlIgnore]
        public bool IsEditable = true;

        /// <summary>
        /// True if the source containing the table is a standard SQL source
        /// </summary>
        [XmlIgnore]
        public bool IsSQL
        {
            get { return !_source.IsNoSQL; }
        }

        /// <summary>
        /// True if the table is for a SQL Model
        /// </summary>
        [XmlIgnore]
        public bool IsForSQLModel
        {
            get { return Model != null; }
        }

        /// <summary>
        /// ReportModel when the MetaTable comes from a SQL Model
        /// </summary>
        [XmlIgnore]
        public ReportModel Model = null;

        /// <summary>
        /// Returns the SQL with the name and the CTE (Common Table Expression)
        /// </summary>
        public void GetExecSQLName(ref string CTE, ref string name)
        {
            CTE = "";
            string sql = "";
            if (Sql != null && Sql.Length > 5 && Sql.ToLower().Trim().StartsWith("with"))
            {
                var startIndex = Sql.IndexOf("(");
                if (startIndex > 0)
                {
                    bool inComment = false, inQuote = false;
                    for (int i = 0; i < Sql.Length - 5; i++)
                    {
                        switch (Sql[i])
                        {
                            case ')':
                                if (!inComment && !inQuote)
                                {
                                    CTE = Sql.Substring(0, i + 1).Trim() + "\r\n";
                                    sql = Sql.Substring(i + 1).Trim();
                                    i = Sql.Length;
                                }
                                break;
                            case '\'':
                                inQuote = !inQuote;
                                break;
                            case '/':
                                if (Sql[i + 1] == '*')
                                {
                                    inComment = true;
                                }
                                break;
                            case '*':
                                if (inComment && Sql[i + 1] == '/')
                                {
                                    inComment = false;
                                }
                                break;
                            case '-':
                                if (inComment && Sql[i + 1] == '-')
                                {
                                    while (i < Sql.Length - 5 && (Sql[i] != '\r' || Sql[i] != '\n')) i++;
                                }
                                break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(CTE) || string.IsNullOrEmpty(sql))
            {
                name = FullSQLName;
            }
            else
            {
                name = string.Format("(\r\n{0}\r\n) {1}", sql, AliasName); ;
            }
        }

        protected MetaSource _source;
        /// <summary>
        /// Current MetaSource
        /// </summary>
        [XmlIgnore]
        public MetaSource Source
        {
            get { return _source; }
            set { _source = value; }
        }

        /// <summary>
        /// Returns the last order to display the columns
        /// </summary>
        public int GetLastDisplayOrder()
        {
            if (Columns.Count > 0) return Columns.Max(i => i.DisplayOrder) + 1;
            return 1;
        }

        /// <summary>
        /// For No SQL Source, build the DataTable from the DefinitionScript, if withLoad is true, the table is then loaded with the LoadScript
        /// </summary>
        public DataTable BuildNoSQLTable(bool withLoad)
        {
            lock (this)
            {
                if (string.IsNullOrEmpty(DefinitionScript)) throw new Exception("No Definition Script for the table.");
                RazorHelper.CompileExecute(DefinitionScript, this);
                if (withLoad && !string.IsNullOrEmpty(LoadScript)) RazorHelper.CompileExecute(LoadScript, this);
            }
            return NoSQLTable;
        }

        DataTable GetDefinitionTable(string sql)
        {
            DataTable result = null;
            var finalSQL = sql;
            try
            {
                if (IsSQL)
                {
                    DbConnection connection = _source.GetOpenConnection();

                    Helper.ExecutePrePostSQL(connection, Model == null ? ReportModel.ClearCommonRestrictions(PreSQL) : Model.ParseCommonRestrictions(PreSQL), this, IgnorePrePostError);
                    finalSQL = Model == null ? ReportModel.ClearCommonRestrictions(sql) : Model.ParseCommonRestrictions(sql);
                    result = Helper.GetDataTable(connection, finalSQL);
                    Helper.ExecutePrePostSQL(connection, Model == null ? ReportModel.ClearCommonRestrictions(PostSQL) : Model.ParseCommonRestrictions(PostSQL), this, IgnorePrePostError);
                    connection.Close();
                }
                else
                {
                    result = BuildNoSQLTable(false);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\r\n" + finalSQL);
            }

            return result;
        }

        /// <summary>
        /// Refresh the dynamic columns
        /// </summary>
        public void Refresh()
        {
            if (_source == null || !DynamicColumns) return;

            try
            {
                Information = "";
                Error = "";
                MustRefresh = true;
                //Build table def from SQL or table name

                var sql = "";
                if (IsForSQLModel)
                {
                    if (Model.UseRawSQL) sql = Sql;
                    else sql = string.Format("SELECT * FROM ({0}) a WHERE 1=0", Sql);
                }
                else
                {
                    string CTE = "", name = "";
                    GetExecSQLName(ref CTE, ref name);
                    sql = string.Format("{0}SELECT * FROM {1} WHERE 1=0", CTE, name);
                }

                DataTable defTable = GetDefinitionTable(sql);

                int position = 1;
                foreach (DataColumn column in defTable.Columns)
                {
                    string fullColumnName = (IsSQL && !IsForSQLModel ? Source.GetTableName(AliasName) + "." : "") + Source.GetColumnName(column.ColumnName);
                    MetaColumn newColumn = Columns.FirstOrDefault(i => i.Name.ToLower() == fullColumnName.ToLower());
                    column.ColumnName = fullColumnName; //Set it here to clear the columns later
                    ColumnType type = Helper.NetTypeConverter(column.DataType);
                    if (newColumn == null)
                    {
                        newColumn = MetaColumn.Create(fullColumnName);
                        newColumn.Source = _source;
                        newColumn.DisplayName = (KeepColumnNames ? column.ColumnName.Trim() : Helper.DBNameToDisplayName(column.ColumnName.Trim()));
                        newColumn.Category = (Alias == MetaData.MasterTableName ? "Master" : AliasName);
                        newColumn.DisplayOrder = GetLastDisplayOrder();
                        Columns.Add(newColumn);
                        newColumn.Type = type;
                        newColumn.SetStandardFormat();
                    }
                    newColumn.Source = _source;
                    if (type != newColumn.Type)
                    {
                        newColumn.Type = type;
                        newColumn.SetStandardFormat();
                    }
                    newColumn.DisplayOrder = position++;
                }

                //Clear columns for No SQL or SQL Model
                if (!IsSQL || IsForSQLModel)
                {
                    Columns.RemoveAll(i => !defTable.Columns.Contains(i.Name));
                }

                MustRefresh = false;
                Information = "Dynamic columns have been refreshed successfully";
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Information = "Error got when refreshing dynamic columns.";
            }
            Information = Helper.FormatMessage(Information);
        }

        /// <summary>
        /// Sort the table columns either by alphanumeric order or by position
        /// </summary>
        public void SortColumns(bool byPosition)
        {
            if (_source == null) return;

            try
            {
                Information = "";
                Error = "";

                List<string> colNames = new List<string>();

                if (byPosition)
                {
                    DataTable defTable = GetDefinitionTable(string.Format("SELECT * FROM {0} WHERE 1=0", FullSQLName));
                    foreach (DataColumn column in defTable.Columns)
                    {
                        colNames.Add(Source.GetTableName(AliasName) + "." + Source.GetColumnName(column.ColumnName));
                    }
                }
                else
                {
                    foreach (var col in Columns) colNames.Add(col.DisplayName);
                    colNames.Sort();
                }

                foreach (var col in Columns) col.DisplayOrder = -1;

                int position = 1;
                foreach (string columnName in colNames)
                {
                    var cols = Columns.Where(i => (byPosition && i.Name.Trim().ToLower() == columnName.Trim().ToLower()) || (!byPosition && i.DisplayName.Trim().ToLower() == columnName.Trim().ToLower()));
                    foreach (var col in cols) col.DisplayOrder = position++;
                }

                foreach (var col in Columns) if (col.DisplayOrder == -1) col.DisplayOrder = GetLastDisplayOrder();

                Information = "Columns have been sorted by " + (byPosition ? "SQL position" : "Name");
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Information = "Error got when sorting columns.";
            }
            Information = Helper.FormatMessage(Information);
        }

        /// <summary>
        /// Check the table. If a MetaColumn is specified, only the column is checked.
        /// </summary>
        public void CheckTable(MetaColumn column)
        {
            if (_source == null) return;

            Information = "";
            Error = "";

            if (IsMasterTable && IsSQL && string.IsNullOrEmpty(Sql))
            {
                Information = Helper.FormatMessage("No SQL Select Statement defined for the Master table...");
                return;
            }
            if (IsMasterTable && !IsSQL && string.IsNullOrEmpty(DefinitionScript))
            {
                Information = Helper.FormatMessage("No Script defined for the Master table...");
                return;
            }

            try
            {
                if (IsSQL)
                {
                    string colNames = "", groupByNames = "";
                    foreach (var col in Columns)
                    {
                        if (column != null && col != column) continue;
                        Helper.AddValue(ref colNames, ",", col.Name);
                        if (!col.IsAggregate) Helper.AddValue(ref groupByNames, ",", col.Name);
                    }
                    if (string.IsNullOrEmpty(colNames)) colNames = "1";

                    string CTE = "", name = "";
                    GetExecSQLName(ref CTE, ref name);
                    string sql = string.Format("{0}SELECT {1} FROM {2} WHERE 1=0", CTE, colNames, name);

                    if (!string.IsNullOrWhiteSpace(WhereSQL))
                    {
                        var where = RazorHelper.CompileExecute(WhereSQL, this);
                        if (!string.IsNullOrWhiteSpace(where)) sql += string.Format("\r\nAND ({0})", RazorHelper.CompileExecute(where, this));
                    }
                    if (Columns.Exists(i => i.IsAggregate) && !string.IsNullOrEmpty(groupByNames))
                    {
                        sql += string.Format("\r\nGROUP BY {0}", groupByNames);
                    }
                    Error = _source.CheckSQL(sql, new List<MetaTable>() { this }, null, false);
                }
                else
                {
                    BuildNoSQLTable(true);
                }

                if (string.IsNullOrEmpty(Error)) Information = "Table checked successfully";
                else Information = "Error got when checking table.";
                if (column != null)
                {
                    column.Error = Error;
                    if (string.IsNullOrEmpty(column.Error)) column.Information = "Column checked successfully";
                    else column.Information = "Error got when checking column.";
                    column.Information = Helper.FormatMessage(column.Information);
                }
            }
            catch (TemplateCompilationException ex)
            {
                Error = Helper.GetExceptionMessage(ex);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Information = "Error got when checking the table.";
            }
            Information = Helper.FormatMessage(Information);
        }

        /// <summary>
        /// Return the values from a column in the table
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public string ShowValues(MetaColumn column)
        {
            string result = "";
            try
            {
                if (IsSQL)
                {
                    string sql = string.Format("SELECT {0} FROM {1}", column.Name, FullSQLName);
                    result = string.Format("{0}\r\n\r\n{1}:\r\n", sql, column.DisplayName);
                    DbConnection connection = _source.GetOpenConnection();
                    DbCommand command = connection.CreateCommand();
                    command.CommandText = sql;
                    var reader = command.ExecuteReader();
                    int cnt = 1000;
                    while (reader.Read() && --cnt >= 0)
                    {
                        string valueStr = "";
                        if (!reader.IsDBNull(0))
                        {
                            object value = reader[0];

                            CultureInfo culture = (_source.Report != null ? _source.Report.ExecutionView.CultureInfo : _source.Repository.CultureInfo);
                            if (value is IFormattable) valueStr = ((IFormattable)value).ToString(column.Format, culture);
                            else valueStr = value.ToString();
                        }
                        result += string.Format("{0}\r\n", valueStr);
                    }
                    reader.Close();
                    command.Connection.Close();
                }
                else
                {
                    result = string.Format("{0}:\r\n", column.DisplayName);
                    DataTable table = BuildNoSQLTable(true);
                    foreach (DataRow row in table.Rows)
                    {
                        result += string.Format("{0}\r\n", row[column.Name]);
                    }
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            return result;
        }

        #region Helpers

        /// <summary>
        /// Editor Helper: Create or update dynamic columns for this table
        /// </summary>
        public string HelperRefreshColumns
        {
            get { return "<Click to refresh dynamic columns>"; }
        }

        /// <summary>
        /// Editor Helper: Check the table definition
        /// </summary>
        public string HelperCheckTable
        {
            get { return "<Click to check the table in the database>"; }
        }

        /// <summary>
        /// Last information message
        /// </summary>
        [XmlIgnore]
        public string Information { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
        [XmlIgnore]
        public string Error { get; set; }

        #endregion



    }
}

