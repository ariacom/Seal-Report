//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Data;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using Seal.Helpers;
using Seal.Forms;
using DynamicTypeDescriptor;
using System.Data.Common;
using System.Data.Odbc;
using System.Xml;
using System.Data.SqlClient;
using DocumentFormat.OpenXml.Bibliography;

namespace Seal.Model
{
    /// <summary>
    /// A MetaSource contains a list of MetaConnection and a MetaData
    /// </summary>
    public class MetaSource : ReportComponent
    {
        const string DefaultConnectionString = "Provider=SQLOLEDB;data source=localhost;initial catalog=adb;Integrated Security=SSPI;";

        /// <summary>
        /// Current file path of the source
        /// </summary>
        [XmlIgnore]
        public string FilePath;

        /// <summary>
        /// Current repository
        /// </summary>
        [XmlIgnore]
        public Repository Repository = null;

        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("ConnectionGUID").SetIsBrowsable(true);
                GetProperty("PreSQL").SetIsBrowsable(!IsNoSQL);
                GetProperty("PostSQL").SetIsBrowsable(!IsNoSQL);
                GetProperty("IgnorePrePostError").SetIsBrowsable(!IsNoSQL);
                GetProperty("IsDefault").SetIsBrowsable(true);
                GetProperty("IsNoSQL").SetIsBrowsable(true);

                GetProperty("InitScript").SetIsBrowsable(true);

                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);
                GetProperty("Information").SetIsReadOnly(true);
                GetProperty("Error").SetIsReadOnly(true);

                GetProperty("IsNoSQL").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }

        [XmlIgnore]
        public ConnectionFolder ConnectionFolder = new ConnectionFolder();
        [XmlIgnore]
        public TableFolder TableFolder = new TableFolder();
        [XmlIgnore]
        public TableLinkFolder TableLinksFolder = new TableLinkFolder();
        [XmlIgnore]
        public CategoryFolder CategoryFolder = new CategoryFolder();
        [XmlIgnore]
        public JoinFolder JoinFolder = new JoinFolder();
        [XmlIgnore]
        public EnumFolder EnumFolder = new EnumFolder();
        #endregion

        /// <summary>
        /// List of MetaConnection
        /// </summary>
        public List<MetaConnection> Connections { get; set; } = new List<MetaConnection>();
        public bool ShouldSerializeConnections() { return Connections.Count > 0; }


        protected string _connectionGUID;
        /// <summary>
        /// The connection currently used for this data source
        /// </summary>
        [DefaultValue(null)]
        [Category("General"), DisplayName("Current connection"), Description("The connection currently used for this data source"), Id(1, 1)]
        [TypeConverter(typeof(SourceConnectionConverter))]
        public string ConnectionGUID
        {
            get { return _connectionGUID; }
            set { _connectionGUID = value; }
        }

        /// <summary>
        /// If true, this source is used as default when a new model is created in a report
        /// </summary>
        [DefaultValue(false)]
        [Category("General"), DisplayName("Is Default"), Description("If true, this source is used as default when a new model is created in a report."), Id(2, 1)]
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// If true, this source contains only tables built from dedicated Razor Scripts (one for the definition and one for the load). The a LINQ query will then be used to fill the models.
        /// </summary>
        [DefaultValue(false)]
        [Category("General"), DisplayName("Is LINQ"), Description("If true, this source contains only tables built from dedicated Razor Scripts (one for the definition and one for the load). The a LINQ query will then be used to fill the models."), Id(3, 1)]
        public bool IsNoSQL { get; set; } = false;

        /// <summary>
        /// If true, this source contains only a table built from a database. The SQL engine will be used to fill the models.
        /// </summary>
        [XmlIgnore]
        public bool IsSQL { get { return !IsNoSQL; } }

        /// <summary>
        /// If set, the script is executed when a report is initialized for an execution. This may be useful to change dynamically components of the source (e.g. modifying connections, tables, columns, enums, etc.).
        /// </summary>
        [Category("Scripts"), DisplayName("Report Execution Init Script"), Description("If set, the script is executed when a report is initialized for an execution. This may be useful to change dynamically components of the source (e.g. modifying connections, tables, columns, enums, etc.)."), Id(4, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string InitScript { get; set; } = "";
        public bool ShouldSerializeInitScript() { return !string.IsNullOrEmpty(InitScript); }

        /// <summary>
        /// SQL Statement executed after the connection is open and before the query is executed. The statement may contain Razor script if it starts with '@'.
        /// </summary>
        [Category("SQL"), DisplayName("Pre SQL Statement"), Description("SQL Statement executed after the connection is open and before the query is executed. The statement may contain Razor script if it starts with '@'."), Id(5, 4)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string PreSQL { get; set; }
        public bool ShouldSerializePreSQL() { return !string.IsNullOrEmpty(PreSQL); }

        /// <summary>
        /// SQL Statement executed before the connection is closed and after the query is executed. The statement may contain Razor script if it starts with '@'.
        /// </summary>
        [Category("SQL"), DisplayName("Post SQL Statement"), Description("SQL Statement executed before the connection is closed and after the query is executed. The statement may contain Razor script if it starts with '@'."), Id(6, 4)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string PostSQL { get; set; }
        public bool ShouldSerializePostSQL() { return !string.IsNullOrEmpty(PostSQL); }

        /// <summary>
        /// If true, errors occuring during the Pre or Post SQL statements are ignored and the execution continues
        /// </summary>
        [DefaultValue(false)]
        [Category("SQL"), DisplayName("Ignore Pre and Post SQL Errors"), Description("If true, errors occuring during the Pre or Post SQL statements are ignored and the execution continues."), Id(7, 4)]
        public bool IgnorePrePostError { get; set; } = false;

        /// <summary>
        /// Last modification Date Time
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification;

        /// <summary>
        /// Current MetaConnection
        /// </summary>
        [XmlIgnore]
        public virtual MetaConnection Connection
        {
            get
            {
                if (Connections.Count == 0)
                {
                    //add a connection
                    AddConnection();
                }

                MetaConnection result = Connections.FirstOrDefault(i => i.GUID == _connectionGUID);
                if (result == null && Connections.Count > 0 && _connectionGUID != ReportSource.DefaultRepositoryConnectionGUID)
                {
                    result = Connections[0];
                    _connectionGUID = result.GUID;
                }
                return result;
            }
        }

        /// <summary>
        /// Object that can be used at run-time for any purpose
        /// </summary>
        [XmlIgnore]
        public object Tag;

        MetaData _metaData = null;
        /// <summary>
        /// MetaData contained in the source 
        /// </summary>
        public MetaData MetaData
        {
            get
            {
                if (_metaData == null) Refresh();
                return _metaData;
            }
            set { _metaData = value; }
        }

        /// <summary>
        /// Create a MetaConnection in the source 
        /// </summary>
        public MetaConnection AddConnection()
        {
            MetaConnection result = MetaConnection.Create(this);
            result.ConnectionString = DefaultConnectionString;
            result.DatabaseType = ConnectionStringEditor.GetDatabaseType(result.ConnectionString); //!NETCore

            result.Name = Helper.GetUniqueName(result.Name, (from i in Connections select i.Name).ToList());
            Connections.Add(result);
            return result;
        }

        /// <summary>
        /// Remove a MetaConnection from the source
        /// </summary>
        public void RemoveConnection(MetaConnection item)
        {
            if (Connection == item) throw new Exception("This connection is used as the current connection and cannot be removed.");
            Connections.Remove(item);
        }

        /// <summary>
        /// Add a MetaTable in the source
        /// </summary>
        public MetaTable AddTable(bool forReport)
        {
            MetaTable result = MetaTable.Create();
            result.Name = "NewTable";
            result.DynamicColumns = forReport || IsNoSQL;
            result.Source = this;
            result.Name = Helper.GetUniqueName(result.Name, (from i in MetaData.Tables select i.Name).ToList());
            MetaData.Tables.Add(result);
            return result;
        }

        /// <summary>
        /// Remove a MetaTable from the source
        /// </summary>
        public void RemoveTable(MetaTable item)
        {
            //remove joins related
            MetaData.Joins.RemoveAll(i => i.LeftTableGUID == item.GUID || i.RightTableGUID == item.GUID);
            MetaData.Tables.Remove(item);
        }

        /// <summary>
        /// Remove a MetaTableLink from the source
        /// </summary>
        public void RemoveTableLink(MetaTableLink item)
        {
            MetaData.TableLinks.Remove(item);
        }

        /// <summary>
        /// Add a MetaColumn in a MetaTable
        /// </summary>
        public MetaColumn AddColumn(MetaTable table)
        {
            MetaColumn result = MetaColumn.Create("ColumnName");
            result.Source = this;
            MetaColumn col = table.Columns.FirstOrDefault();
            if (col != null) result.Category = col.Category;
            else result.Category = !string.IsNullOrEmpty(table.AliasName) ? table.AliasName : Helper.DBNameToDisplayName(table.Name.Trim());
            result.DisplayOrder = table.GetLastDisplayOrder();
            table.Columns.Add(result);
            return result;
        }

        /// <summary>
        /// Add a MetaJoin to the MetaData
        /// </summary>
        public MetaJoin AddJoin()
        {
            MetaJoin result = MetaJoin.Create();
            result.Name = Repository.JoinAutoName;
            result.Name = Helper.GetUniqueName(result.Name, (from i in MetaData.Joins select i.Name).ToList());
            result.Source = this;
            result.IsBiDirectional = IsSQL;
            MetaData.Joins.Add(result);
            return result;
        }

        /// <summary>
        /// Remove a MetaJoin from the MetaData
        /// </summary>
        public void RemoveJoin(MetaJoin item)
        {
            MetaData.Joins.Remove(item);
        }

        /// <summary>
        /// Add a MetaEnum to the MetaData
        /// </summary>
        public MetaEnum AddEnum()
        {
            MetaEnum result = MetaEnum.Create();
            result.Name = "Enum";
            result.Name = Helper.GetUniqueName(result.Name, (from i in MetaData.Enums select i.Name).ToList());
            MetaData.Enums.Add(result);
            result.Source = this;
            return result;
        }

        /// <summary>
        /// Helper to create a MetaEnum for a given MetaColumn
        /// </summary>
        public MetaEnum CreateEnumFromColumn(MetaColumn column)
        {
            MetaEnum result = AddEnum();
            result.IsEditable = true;
            result.Name = column.DisplayName;
            result.IsDynamic = true;
            if (!IsNoSQL)
            {
                result.Sql = string.Format("SELECT DISTINCT \r\n{0} \r\nFROM {1} \r\nORDER BY 1", column.Name, column.MetaTable.FullSQLName);
            }
            else
            {
                result.Script = @"@using System.Data
@{
    MetaEnum enumList = Model;
    MetaSource source = enumList.Source;
    MetaTable table = source.MetaData.Tables.FirstOrDefault(i => i.Name == TableName);
    if (table != null)
    {
        DataTable dataTable = table.BuildNoSQLTable(true);
        enumList.Values.Clear();
        foreach (DataRow val in dataTable.Rows)
        {
            if (!enumList.Values.Exists(i => i.Id == val[ColumnName].ToString()))
            {
                enumList.Values.Add(new MetaEV() { Id = val[ColumnName].ToString() });
            }
        }
    }
}
";
                result.Script = result.Script.Replace("TableName", Helper.QuoteDouble(column.MetaTable.Name));
                result.Script = result.Script.Replace("ColumnName", Helper.QuoteDouble(column.Name));
            }
            result.RefreshEnum();
            return result;
        }

        /// <summary>
        /// Remove a MetaEnum from the MetaData
        /// </summary>
        public void RemoveEnum(MetaEnum item)
        {
            //Clean up enum references from columns
            foreach (MetaTable table in MetaData.Tables)
            {
                foreach (MetaColumn column in table.Columns.Where(i => i.EnumGUID == item.GUID))
                {
                    column.EnumGUID = "";
                }
            }
            MetaData.Enums.Remove(item);
        }

        /// <summary>
        /// Init all object references
        /// </summary>
        public void InitReferences(Repository repository)
        {
            Repository = repository;

            //init references in objects
            foreach (var connection in Connections)
            {
                connection.Source = this;
            }
            foreach (var table in MetaData.Tables)
            {
                table.Source = this;
                foreach (var column in table.Columns)
                {
                    column.Source = this;
                }
                table.InitParameters();
            }
            foreach (var link in MetaData.TableLinks)
            {
                //First report GUID
                if (Report != null)
                {
                    link.Source = Report.Sources.FirstOrDefault(i => i.GUID == link.SourceGUID);
                }
                if (link.Source == null)
                {
                    link.Source = repository.Sources.FirstOrDefault(i => i.GUID == link.SourceGUID);
                }
            }
            //remove lost links
            MetaData.TableLinks.RemoveAll(i => i.Source == null);

            foreach (var join in MetaData.Joins)
            {
                join.Source = this;
            }
            foreach (var item in MetaData.Enums)
            {
                item.Source = this;
            }
        }

        /// <summary>
        /// Add a default MetaConnection to the source
        /// </summary>
        public void AddDefaultConnection(Repository repository)
        {
            if (Connections.Count == 0)
            {
                //Add default connection
                MetaConnection connection = MetaConnection.Create(this);
                connection.ConnectionString = DefaultConnectionString;
                Connections.Add(connection);
                ConnectionGUID = connection.GUID;
            }
        }

        /// <summary>
        /// Create a basic MetaSource
        /// </summary>
        static public MetaSource Create(Repository repository)
        {
            MetaSource result = new MetaSource() { GUID = Guid.NewGuid().ToString(), Name = "Data Source", Repository = repository };
            result.AddDefaultConnection(repository);
            return result;
        }

        /// <summary>
        /// Load the MetaSource from a file
        /// </summary>
        static public MetaSource LoadFromFile(string path)
        {
            MetaSource result = null;
            try
            {
                path = FileHelper.ConvertOSFilePath(path);
                if (!File.Exists(path)) throw new Exception("File not found: " + path);

                XmlSerializer serializer = new XmlSerializer(typeof(MetaSource));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (MetaSource)serializer.Deserialize(xr);
                }
                result.Name = Path.GetFileNameWithoutExtension(path);
                result.FilePath = path;
                result.LastModification = File.GetLastWriteTime(path);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to read the file '{0}'.\r\n{1}", path, ex.Message));
            }
            return result;
        }

        /// <summary>
        /// Save to the current file
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(FilePath);
        }

        /// <summary>
        /// Save to a file
        /// </summary>
        /// <param name="path"></param>
        public void SaveToFile(string path)
        {
            //Check last modification
            if (LastModification != DateTime.MinValue && File.Exists(path))
            {
                if (LastModification != File.GetLastWriteTime(path))
                {
                    throw new Exception("Unable to save the Data Source file. The file has been modified by another user.");
                }
            }

            try
            {
                foreach (var table in MetaData.Tables) table.BeforeSerialization();

                Name = Path.GetFileNameWithoutExtension(path);
                XmlSerializer serializer = new XmlSerializer(typeof(MetaSource));
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.NewLineHandling = NewLineHandling.Entitize;
                using (XmlWriter xw = XmlWriter.Create(path, ws))
                {
                    serializer.Serialize(xw, this);
                }
                FilePath = path;
                LastModification = File.GetLastWriteTime(path);
            }
            finally
            {
                foreach (var table in MetaData.Tables) table.AfterSerialization();
            }
        }


        /// <summary>
        /// Refresh all tables having dynamic columns and needed a refresh 
        /// </summary>
        public void Refresh()
        {
            if (_metaData == null) _metaData = new MetaData();
            foreach (var table in _metaData.Tables.Where(i => i.DynamicColumns == true && i.MustRefresh))
            {
                table.Refresh();
            }
        }

        /// <summary>
        /// Check a SQL statement, the check includes also all the Pre/Post SQL statements defined.
        /// </summary>
        public string CheckSQL(string sql, List<MetaTable> tables, ReportModel model, bool isPrePost)
        {
            string result = "", finalSQL = "";
            if (!string.IsNullOrEmpty(sql))
            {
                try
                {
                    DbConnection connection = (model != null ? model.Connection.GetOpenConnection() : GetOpenConnection());
                    Helper.ExecutePrePostSQL(connection, Helper.ClearAllSQLKeywords(PreSQL, model), this, this.IgnorePrePostError);
                    if (tables != null) foreach (var table in tables) Helper.ExecutePrePostSQL(connection, Helper.ClearAllSQLKeywords(table.PreSQL, model), table, table.IgnorePrePostError);
                    if (!isPrePost && model != null) Helper.ExecutePrePostSQL(connection, Helper.ClearAllSQLKeywords(model.PreSQL, model), model, model.IgnorePrePostError);
                    var command = connection.CreateCommand();
                    finalSQL = Helper.ClearAllSQLKeywords(sql, model);
                    command.CommandText = finalSQL;
                    var reader = command.ExecuteReader();
                    reader.Close();
                    if (isPrePost && model != null) Helper.ExecutePrePostSQL(connection, Helper.ClearAllSQLKeywords(model.PostSQL, model), model, model.IgnorePrePostError);
                    if (tables != null) foreach (var table in tables) Helper.ExecutePrePostSQL(connection, Helper.ClearAllSQLKeywords(table.PostSQL, model), table, table.IgnorePrePostError);
                    Helper.ExecutePrePostSQL(connection, Helper.ClearAllSQLKeywords(PostSQL, model), this, this.IgnorePrePostError);
                    command.Connection.Close();
                }
                catch (Exception ex)
                {
                    result = ex.Message;
                    if (!string.IsNullOrEmpty(finalSQL)) result += "\r\nSQL Executed:\r\n" + finalSQL.Replace("\n", "\r\n");
                }
            }

            return result;
        }

        /// <summary>
        /// Check a LINQ statement
        /// </summary>
        public string CheckLINQ(string linq, List<MetaTable> tables, ReportModel model)
        {
            string result = "";
            if (!string.IsNullOrEmpty(linq))
            {
                try
                {
                    RazorHelper.Compile(linq, typeof(MetaJoin), Helper.NewGUID());
                }
                catch (Exception ex)
                {
                    result = ex.Message;
                }
            }

            return result;
        }


        /// <summary>
        /// Returns an open DbConnection
        /// </summary>
        public DbConnection GetOpenConnection()
        {
            if (Connection == null) throw new Exception("No connection defined for this source. Please configure the database connection");
            return Connection.GetOpenConnection();
        }

        /// <summary>
        /// Fill a list of columns from a table catalog
        /// </summary>
        public void AddColumnsFromCatalog(List<MetaColumn> columns, DbConnection connection, MetaTable table)
        {
            if (table.Name == null) throw new Exception("No table name has been defined...");

            //handle if table name = dbname.owner.tablename
            string name = table.Name.Replace("[", "").Replace("]", "");
            string[] names = name.Split('.');
            DataTable schemaColumns = null;

            Helper.ExecutePrePostSQL(connection, ReportModel.ClearCommonRestrictions(table.PreSQL), table, table.IgnorePrePostError);
            if (names.Length == 3) schemaColumns = connection.GetSchema("Columns", names);
            else if (names.Length == 2) schemaColumns = connection.GetSchema("Columns", new string[] { null, names[0], names[1] });
            else schemaColumns = connection.GetSchema("Columns", new string[] { null, null, name });
            Helper.ExecutePrePostSQL(connection, ReportModel.ClearCommonRestrictions(table.PostSQL), table, table.IgnorePrePostError);

            foreach (DataRow row in schemaColumns.Rows)
            {
                try
                {
                    string tableName = (!string.IsNullOrEmpty(table.AliasName) ? table.AliasName : Helper.DBNameToDisplayName(row["TABLE_NAME"].ToString().Trim()));
                    MetaColumn column = MetaColumn.Create(tableName + "." + GetColumnName(row["COLUMN_NAME"].ToString()));
                    column.DisplayName = table.KeepColumnNames ? row["COLUMN_NAME"].ToString().Trim() : Helper.DBNameToDisplayName(row["COLUMN_NAME"].ToString().Trim());
                    column.DisplayOrder = table.GetLastDisplayOrder();

                    MetaColumn col = table.Columns.FirstOrDefault();
                    if (col != null) column.Category = col.Category;
                    else column.Category = !string.IsNullOrEmpty(table.AliasName) ? table.AliasName : Helper.DBNameToDisplayName(table.Name.Trim());
                    column.Source = this;
                    string dbType = "";
                    if (row.Table.Columns.Contains("TYPE_NAME")) dbType = row["TYPE_NAME"].ToString();
                    if (connection is OdbcConnection) column.Type = Helper.ODBCToNetTypeConverter(dbType);
                    else if (connection is SqlConnection) column.Type = Helper.ODBCToNetTypeConverter(row["DATA_TYPE"].ToString());
                    else column.Type = Helper.DatabaseToNetTypeConverter(row["DATA_TYPE"]);
                    column.SetStandardFormat();
                    if (!columns.Exists(i => i.Name == column.Name)) columns.Add(column);
                }
                catch { }
            }
        }

        string getDelimiters(DatabaseType dbType)
        {
            if (dbType == DatabaseType.Oracle) return "\"\"";
            if (dbType == DatabaseType.MySQL) return "``";
            return "[]";
        }


        static string[] MSSQLKeywords = new string[] { "ADD", "EXTERNAL", "PROCEDURE", "ALL", "FETCH", "PUBLIC", "ALTER", "FILE", "RAISERROR", "AND", "FILLFACTOR", "READ", "ANY", "FOR", "READTEXT", "AS", "FOREIGN", "RECONFIGURE", "ASC", "FREETEXT", "REFERENCES", "AUTHORIZATION", "FREETEXTTABLE", "REPLICATION", "BACKUP", "FROM", "RESTORE", "BEGIN", "FULL", "RESTRICT", "BETWEEN", "FUNCTION", "RETURN", "BREAK", "GOTO", "REVERT", "BROWSE", "GRANT", "REVOKE", "BULK", "GROUP", "RIGHT", "BY", "HAVING", "ROLLBACK", "CASCADE", "HOLDLOCK", "ROWCOUNT", "CASE", "IDENTITY", "ROWGUIDCOL", "CHECK", "IDENTITY_INSERT", "RULE", "CHECKPOINT", "IDENTITYCOL", "SAVE", "CLOSE", "IF", "SCHEMA", "CLUSTERED", "IN", "SECURITYAUDIT", "COALESCE", "INDEX", "SELECT", "COLLATE", "INNER", "SEMANTICKEYPHRASETABLE", "COLUMN", "INSERT", "SEMANTICSIMILARITYDETAILSTABLE", "COMMIT", "INTERSECT", "SEMANTICSIMILARITYTABLE", "COMPUTE", "INTO", "SESSION_USER", "CONSTRAINT", "IS", "SET", "CONTAINS", "JOIN", "SETUSER", "CONTAINSTABLE", "KEY", "SHUTDOWN", "CONTINUE", "KILL", "SOME", "CONVERT", "LEFT", "STATISTICS", "CREATE", "LIKE", "SYSTEM_USER", "CROSS", "LINENO", "TABLE", "CURRENT", "LOAD", "TABLESAMPLE", "CURRENT_DATE", "MERGE", "TEXTSIZE", "CURRENT_TIME", "NATIONAL", "THEN", "CURRENT_TIMESTAMP", "NOCHECK", "TO", "CURRENT_USER", "NONCLUSTERED", "TOP", "CURSOR", "NOT", "TRAN", "DATABASE", "NULL", "TRANSACTION", "DBCC", "NULLIF", "TRIGGER", "DEALLOCATE", "OF", "TRUNCATE", "DECLARE", "OFF", "TRY_CONVERT", "DEFAULT", "OFFSETS", "TSEQUAL", "DELETE", "ON", "UNION", "DENY", "OPEN", "UNIQUE", "DESC", "OPENDATASOURCE", "UNPIVOT", "DISK", "OPENQUERY", "UPDATE", "DISTINCT", "OPENROWSET", "UPDATETEXT", "DISTRIBUTED", "OPENXML", "USE", "DOUBLE", "OPTION", "USER", "DROP", "OR", "VALUES", "DUMP", "ORDER", "VARYING", "ELSE", "OUTER", "VIEW", "END", "OVER", "WAITFOR", "ERRLVL", "PERCENT", "WHEN", "ESCAPE", "PIVOT", "WHERE", "EXCEPT", "PLAN", "WHILE", "EXEC", "PRECISION", "WITH", "EXECUTE", "PRIMARY", "WITHIN GROUP", "EXISTS", "PRINT", "WRITETEXT", "EXIT", "PROC" };

        string[] getKeywords(DatabaseType dbType)
        {
            if (dbType == DatabaseType.MSSQLServer) return MSSQLKeywords;
            //TODO for other db types
            return new string[] { "" };
        }

        /// <summary>
        /// Returns a full table name from a raw name
        /// </summary>
        public string GetTableName(string rawName)
        {
            string delimiters = getDelimiters(Connection.DatabaseType);
            var keywords = getKeywords(Connection.DatabaseType);
            if ((!rawName.StartsWith(delimiters[0].ToString()) && !rawName.EndsWith(delimiters[1].ToString()) && rawName.IndexOfAny(" '\"-$-".ToCharArray()) != -1)
                || keywords.Contains(rawName.ToUpper()))
                return string.Format("{0}{1}{2}", delimiters[0], rawName, delimiters[1]);
            return rawName;
        }

        /// <summary>
        /// Returns a full column name from a raw name
        /// </summary>
        public string GetColumnName(string rawName)
        {
            string delimiters = getDelimiters(Connection.DatabaseType);
            var keywords = getKeywords(Connection.DatabaseType);
            if ((!rawName.StartsWith(delimiters[0].ToString()) && !rawName.EndsWith(delimiters[1].ToString()) && rawName.IndexOfAny(" '\"-$-".ToCharArray()) != -1)
                || keywords.Contains(rawName.ToUpper()))
                return string.Format("{0}{1}{2}", delimiters[0], rawName, delimiters[1]);
            return rawName;
        }

        #region Helpers

        /// <summary>
        /// Last information message
        /// </summary>
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(8, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(9, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error { get; set; }

        #endregion

    }
}
