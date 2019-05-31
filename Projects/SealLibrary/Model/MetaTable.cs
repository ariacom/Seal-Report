//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
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
using Seal.Forms;
using DynamicTypeDescriptor;
using RazorEngine.Templating;
using System.Globalization;
using System.Data.Common;
using System.Text;

namespace Seal.Model
{
    public class MetaTable : RootComponent, ReportExecutionLog
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Name").SetIsBrowsable(IsSQL);
                GetProperty("Type").SetIsBrowsable(IsSQL);
                GetProperty("Alias").SetIsBrowsable(IsSQL);
                GetProperty("DynamicColumns").SetIsBrowsable(IsSQL);
                GetProperty("KeepColumnNames").SetIsBrowsable(true);
                GetProperty("PreSQL").SetIsBrowsable(IsSQL);
                GetProperty("Sql").SetIsBrowsable(IsSQL);
                GetProperty("DefinitionScript").SetIsBrowsable(!IsSQL);
                GetProperty("LoadScript").SetIsBrowsable(!IsSQL);
                GetProperty("PostSQL").SetIsBrowsable(IsSQL);
                GetProperty("IgnorePrePostError").SetIsBrowsable(IsSQL);
                GetProperty("WhereSQL").SetIsBrowsable(IsSQL);

                GetProperty("HelperRefreshColumns").SetIsBrowsable(true);
                GetProperty("HelperCheckTable").SetIsBrowsable(true);
                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);

                GetProperty("Name").SetIsReadOnly(IsMasterTable);
                GetProperty("Type").SetIsReadOnly(true);
                GetProperty("DynamicColumns").SetIsReadOnly(IsMasterTable);
                GetProperty("Alias").SetIsReadOnly(IsMasterTable);

                GetProperty("HelperRefreshColumns").SetIsReadOnly(true);
                GetProperty("HelperCheckTable").SetIsReadOnly(true);
                GetProperty("Information").SetIsReadOnly(true);
                GetProperty("Error").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        public static MetaTable Create()
        {
            return new MetaTable() { GUID = Guid.NewGuid().ToString() };
        }

        [XmlIgnore]
        public ReportExecutionLog Log = null;
        public void LogMessage(string message, params object[] args)
        {
            if (Log != null) Log.LogMessage(message, args);
        }

        [Category("Definition"), DisplayName("Name"), Description("Name of the table in the database. The name can be empty if an SQL Statement is specified."), Id(1, 1)]
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        string _sql;
        [Category("Definition"), DisplayName("SQL Select Statement"), Description("SQL Select Statement executed to define the table. If empty, the table name is used."), Id(2, 1)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string Sql
        {
            get { return _sql; }
            set { _sql = value; }
        }

        //Table used for No SQL Source
        [XmlIgnore]
        public DataTable NoSQLTable = null;

        //Model set for No SQL Source
        [XmlIgnore]
        public ReportModel NoSQLModel = null;

        string _definitionScript;
        [Category("Definition"), DisplayName("Definition Script"), Description("The Razor Script used to built the DataTable object that defines the table."), Id(1, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string DefinitionScript
        {
            get { return _definitionScript; }
            set { _definitionScript = value; }
        }

        string _loadScript;
        [Category("Definition"), DisplayName("Default Load Script"), Description("The Default Razor Script used to load the data in the table. This can be overwritten in the model."), Id(4, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string LoadScript
        {
            get { return _loadScript; }
            set { _loadScript = value; }
        }

        private string _alias;
        [Category("Definition"), DisplayName("Table Alias"), Description("If not empty, table alias name used in the SQL statement. The table alias is necessary if a SQL Statement is specified."), Id(5, 1)]
        public string Alias
        {
            get { return _alias; }
            set { _alias = value; }
        }

        bool _dynamicColumns = false;
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("Dynamic Columns"), Description("If true, columns are generated automatically from the Table Name or the SQL Select Statement by reading the database catalog."), Id(6, 1)]
        public bool DynamicColumns
        {
            get { return _dynamicColumns; }
            set
            {
                _dynamicColumns = value;
                UpdateEditorAttributes();
            }
        }
        public bool ShouldSerializeDynamicColumns() { return _dynamicColumns; }

        bool _keepColumnNames = false;
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("Keep Column Names"), Description("If true, the display names of the columns are kept when generated from the source SQL."), Id(7, 1)]
        public bool KeepColumnNames
        {
            get { return _keepColumnNames; }
            set { _keepColumnNames = value; }
        }


        private string _type;
        [Category("Definition"), DisplayName("Table Type"), Description("Type of the table got from database catalog."), Id(8, 1)]
        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }


        private bool _mustRefresh = false;
        [DefaultValue(false)]
        public bool MustRefresh
        {
            get { return _mustRefresh; }
            set { _mustRefresh = value; }
        }
        public bool ShouldSerializeMustRefresh() { return _mustRefresh; }

        string _preSQL;
        [Category("SQL"), DisplayName("Pre SQL Statement"), Description("SQL Statement executed before the query when the table is involved. The statement may contain Razor script if it starts with '@'."), Id(2, 2)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string PreSQL
        {
            get { return _preSQL; }
            set { _preSQL = value; }
        }

        string _postSQL;
        [Category("SQL"), DisplayName("Post SQL Statement"), Description("SQL Statement executed after the query when the table is involved. The statement may contain Razor script if it starts with '@'."), Id(3, 2)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string PostSQL
        {
            get { return _postSQL; }
            set { _postSQL = value; }
        }

        bool _ignorePrePostError = false;
        [DefaultValue(false)]
        [Category("SQL"), DisplayName("Ignore Pre and Post SQL Errors"), Description("If true, errors occuring during the Pre or Post SQL statements are ignored and the execution continues."), Id(4, 2)]
        public bool IgnorePrePostError
        {
            get { return _ignorePrePostError; }
            set { _ignorePrePostError = value; }
        }
        public bool ShouldSerializeIgnorePrePostError() { return _ignorePrePostError; }

        string _whereSQL;
        [Category("SQL"), DisplayName("Additional WHERE Clause"), Description("Additional SQL added in the WHERE clause when the table is involved in a query. The text may contain Razor script if it starts with '@'."), Id(5, 2)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string WhereSQL
        {
            get { return _whereSQL; }
            set { _whereSQL = value; }
        }

        private List<MetaColumn> _columns = new List<MetaColumn>();
        public List<MetaColumn> Columns
        {
            get { return _columns; }
            set { _columns = value; }
        }
        public bool ShouldSerializeColumns() { return _columns.Count > 0; }

        [XmlIgnore]
        public string AliasName
        {
            get
            {
                if (!string.IsNullOrEmpty(_alias)) return _alias;
                return string.Format("{0}", _name);
            }
        }

        [XmlIgnore]
        public string FullSQLName
        {
            get
            {
                if (!string.IsNullOrEmpty(Sql))
                {
                    return string.Format("(\r\n{0}\r\n) {1}", Sql, AliasName);
                }
                else if (!string.IsNullOrEmpty(_name) && !string.IsNullOrEmpty(_alias))
                {
                    return string.Format("{0} {1}", _name, _alias);
                }
                return AliasName;
            }
        }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(_type)) return AliasName;
                return string.Format("{0} ({1})", AliasName, _type);
            }
        }

        [XmlIgnore]
        public bool IsMasterTable
        {
            get
            {
                return (Alias == MetaData.MasterTableName);
            }
        }

        [XmlIgnore]
        public bool IsEditable = true;

        [XmlIgnore]
        public bool IsSQL
        {
            get { return !_source.IsNoSQL; }
        }

        [XmlIgnore]
        public bool IsForSQLModel
        {
            get { return Model != null; }
        } 

        //Set when MetaTable comes from a SQL Model
        [XmlIgnore]
        public ReportModel Model = null;

        public void GetExecSQLName(ref string CTE, ref string name)
        {
            CTE = "";
            string sql = "";
            if (_sql != null && _sql.Length > 5 && _sql.ToLower().Trim().StartsWith("with"))
            {
                var startIndex = _sql.IndexOf("(");
                if (startIndex > 0)
                {
                    bool inComment = false, inQuote = false;
                    for (int i = 0; i < _sql.Length - 5; i++)
                    {
                        switch (_sql[i])
                        {
                            case ')':
                                if (!inComment && !inQuote)
                                {
                                    CTE = _sql.Substring(0, i + 1).Trim() + "\r\n";
                                    sql = _sql.Substring(i + 1).Trim();
                                    i = _sql.Length;
                                }
                                break;
                            case '\'':
                                inQuote = !inQuote;
                                break;
                            case '/':
                                if (_sql[i + 1] == '*')
                                {
                                    inComment = true;
                                }
                                break;
                            case '*':
                                if (inComment && _sql[i + 1] == '/')
                                {
                                    inComment = false;
                                }
                                break;
                            case '-':
                                if (inComment && _sql[i + 1] == '-')
                                {
                                    while (i < _sql.Length - 5 && (_sql[i] != '\r' || _sql[i] != '\n')) i++;
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
        [XmlIgnore, Browsable(false)]
        public MetaSource Source
        {
            get { return _source; }
            set { _source = value; }
        }

        public int GetLastDisplayOrder()
        {
            if (Columns.Count > 0) return Columns.Max(i => i.DisplayOrder) + 1;
            return 1;
        }

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

        public void Refresh()
        {
            if (_source == null || !DynamicColumns) return;

            try
            {
                _information = "";
                _error = "";
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

                foreach (DataColumn column in defTable.Columns)
                {
                    string fullColumnName = (IsSQL && !IsForSQLModel ? Source.GetTableName(AliasName) + "." : "") + Source.GetColumnName(column.ColumnName);
                    MetaColumn newColumn = Columns.FirstOrDefault(i => i.Name == fullColumnName);
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
                }

                //Clear columns for No SQL or SQL Model
                if (!IsSQL || IsForSQLModel)
                {
                    Columns.RemoveAll(i => !defTable.Columns.Contains(i.Name));
                }

                MustRefresh = false;
                _information = "Dynamic columns have been refreshed successfully";
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _information = "Error got when refreshing dynamic columns.";
            }
            _information = Helper.FormatMessage(_information);
            UpdateEditorAttributes();
        }

        public void SortColumns(bool byPosition)
        {
            if (_source == null) return;

            try
            {
                _information = "";
                _error = "";

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

                _information = "Columns have been sorted by " + (byPosition ? "SQL position" : "Name");
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _information = "Error got when sorting columns.";
            }
            _information = Helper.FormatMessage(_information);
            UpdateEditorAttributes();
        }

        public void CheckTable(MetaColumn column)
        {
            if (_source == null) return;

            _information = "";
            _error = "";

            if (IsMasterTable && IsSQL && string.IsNullOrEmpty(Sql))
            {
                _information = Helper.FormatMessage("No SQL Select Statement defined for the Master table...");
                return;
            }
            if (IsMasterTable && !IsSQL && string.IsNullOrEmpty(DefinitionScript))
            {
                _information = Helper.FormatMessage("No Script defined for the Master table...");
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
                    _error = _source.CheckSQL(sql, new List<MetaTable>() { this }, null, false);
                }
                else
                {
                    BuildNoSQLTable(true);
                }

                if (string.IsNullOrEmpty(_error)) _information = "Table checked successfully";
                else _information = "Error got when checking table.";
                if (column != null)
                {
                    column.Error = _error;
                    if (string.IsNullOrEmpty(column.Error)) column.Information = "Column checked successfully";
                    else column.Information = "Error got when checking column.";
                    column.Information = Helper.FormatMessage(column.Information);
                }
            }
            catch (TemplateCompilationException ex)
            {
                _error = Helper.GetExceptionMessage(ex);
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _information = "Error got when checking the table.";
            }
            _information = Helper.FormatMessage(_information);
            UpdateEditorAttributes();
        }

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

        [Category("Helpers"), DisplayName("Refresh dynamic columns"), Description("Create or update dynamic columns for this table."), Id(2, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperRefreshColumns
        {
            get { return "<Click to refresh dynamic columns>"; }
        }

        [Category("Helpers"), DisplayName("Check table"), Description("Check the table definition."), Id(3, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperCheckTable
        {
            get { return "<Click to check the table in the database>"; }
        }

        string _information;
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(4, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        string _error;
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(5, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        #endregion



    }
}
