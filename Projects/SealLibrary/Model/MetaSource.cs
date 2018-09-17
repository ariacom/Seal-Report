//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Data.OleDb;
using System.Data;
using System.ComponentModel;
using Seal.Converter;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.IO;
using Seal.Helpers;
using System.Text.RegularExpressions;
using Seal.Forms;
using DynamicTypeDescriptor;
using System.Data.Common;
using System.Data.Odbc;

namespace Seal.Model
{
    public class MetaSource : ReportComponent
    {
        [XmlIgnore]
        public string FilePath;

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
                GetProperty("TasksScript").SetIsBrowsable(true);

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
        public CategoryFolder CategoryFolder = new CategoryFolder();
        [XmlIgnore]
        public JoinFolder JoinFolder = new JoinFolder();
        [XmlIgnore]
        public EnumFolder EnumFolder = new EnumFolder();

        #endregion

        List<MetaConnection> _connections = new List<MetaConnection>();
        public List<MetaConnection> Connections
        {
            get { return _connections; }
            set { _connections = value; }
        }


        protected string _connectionGUID;
        [DefaultValue(null)]
        [Category("General"), DisplayName("Current Connection"), Description("The connection currently used for this data source"), Id(1, 1)]
        [TypeConverter(typeof(SourceConnectionConverter))]
        public string ConnectionGUID
        {
            get { return _connectionGUID; }
            set { _connectionGUID = value; }
        }

        bool _isDefault = false;
        [DefaultValue(false)]
        [Category("General"), DisplayName("Is Default"), Description("If true, this source is used as default when a new model is created in a report."), Id(2, 1)]
        public bool IsDefault
        {
            get { return _isDefault; }
            set { _isDefault = value; }
        }

        bool _isNoSQL =false;
        [DefaultValue(false)]
        [Category("General"), DisplayName("Is No SQL"), Description("If true, this source contains only a table built from a Razor script. The SQL engine will not be used to fill the models."), Id(3, 1)]
        public bool IsNoSQL
        {
            get { return _isNoSQL; }
            set { _isNoSQL = value; }
        }
        
        string _initScript = "";
        [Category("Scripts"), DisplayName("Init Script"), Description("If set, the script is executed when a report is initialized for an execution. This may be useful to change dynamically components of the source (e.g. modifying connections, tables, columns, enums, etc.)."), Id(4, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string InitScript
        {
            get { return _initScript; }
            set { _initScript = value; }
        }

        string _tasksScript = "";
        [Category("Scripts"), DisplayName("Tasks Script"), Description("If set, the script is added to all task scripts executed with this source. This may be useful to defined common functions for the source."), Id(5, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string TasksScript
        {
            get { return _tasksScript; }
            set { _tasksScript = value; }
        }

        string _preSQL;
        [Category("SQL"), DisplayName("Pre SQL Statement"), Description("SQL Statement executed after the connection is open and before the query is executed. The statement may contain Razor script if it starts with '@'."), Id(5, 4)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string PreSQL
        {
            get { return _preSQL; }
            set { _preSQL = value; }
        }

        string _postSQL;
        [Category("SQL"), DisplayName("Post SQL Statement"), Description("SQL Statement executed before the connection is closed and after the query is executed. The statement may contain Razor script if it starts with '@'."), Id(6, 4)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string PostSQL
        {
            get { return _postSQL; }
            set { _postSQL = value; }
        }

        bool _ignorePrePostError = false;
        [DefaultValue(false)]
        [Category("SQL"), DisplayName("Ignore Pre and Post SQL Errors"), Description("If true, errors occuring during the Pre or Post SQL statements are ignored and the execution continues."), Id(7, 4)]
        public bool IgnorePrePostError
        {
            get { return _ignorePrePostError; }
            set { _ignorePrePostError = value; }
        }

        [XmlIgnore]
        public DateTime LastModification;


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

        MetaData _metaData = null;
        public MetaData MetaData
        {
            get
            {
                if (_metaData == null) Refresh();
                return _metaData;
            }
            set { _metaData = value; }
        }


        public MetaConnection AddConnection()
        {
            MetaConnection result = MetaConnection.Create(this);
            result.ConnectionString = Repository.Configuration.DefaultConnectionString;
            result.DatabaseType = ConnectionStringEditor.GetDatabaseType(result.ConnectionString);

            result.Name = Helper.GetUniqueName(result.Name, (from i in Connections select i.Name).ToList());
            Connections.Add(result);
            return result;
        }

        public void RemoveConnection(MetaConnection item)
        {
            if (Connection == item) throw new Exception("This connection is used as the current connection and cannot be removed.");
            Connections.Remove(item);
        }

        public MetaTable AddTable(bool forReport)
        {
            MetaTable result = MetaTable.Create();
            result.Name = "NewTable";
            result.DynamicColumns = forReport;
            result.Source = this;
            result.Name = Helper.GetUniqueName(result.Name, (from i in MetaData.Tables select i.Name).ToList());
            MetaData.Tables.Add(result);
            return result;
        }

        public void RemoveTable(MetaTable item)
        {
            //remove joins related
            MetaData.Joins.RemoveAll(i => i.LeftTableGUID == item.GUID || i.RightTableGUID == item.GUID);
            MetaData.Tables.Remove(item);
        }

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

        public MetaJoin AddJoin()
        {
            MetaJoin result = MetaJoin.Create();
            result.Name = "Join";
            result.Name = Helper.GetUniqueName(result.Name, (from i in MetaData.Joins select i.Name).ToList());
            result.Source = this;
            MetaData.Joins.Add(result);
            return result;
        }

        public void RemoveJoin(MetaJoin item)
        {
            MetaData.Joins.Remove(item);
        }

        public MetaEnum AddEnum()
        {
            MetaEnum result = MetaEnum.Create();
            result.Name = "Enum";
            result.Name = Helper.GetUniqueName(result.Name, (from i in MetaData.Enums select i.Name).ToList());
            MetaData.Enums.Add(result);
            result.Source = this;
            return result;
        }

        public MetaEnum CreateEnumFromColumn(MetaColumn column)
        {
            MetaEnum result = AddEnum();
            result.IsEditable = true;
            result.Name = column.DisplayName;
            if (!IsNoSQL)
            {
                result.IsDynamic = true;
                result.Sql = string.Format("SELECT DISTINCT \r\n{0} \r\nFROM {1} \r\nORDER BY 1", column.Name, column.MetaTable.FullSQLName);
                result.RefreshEnum();
            }
            else
            {
                result.IsDynamic = false;
                column.MetaTable.BuildNoSQLTable(true);
                foreach (DataRow row in column.MetaTable.NoSQLTable.Rows)
                {
                    result.Values.Add(new MetaEV() { Id = row[column.Name].ToString() });
                }
            }
            return result;
        }

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
            }
            foreach (var join in MetaData.Joins)
            {
                join.Source = this;
            }
            foreach (var item in MetaData.Enums)
            {
                item.Source = this;
            }
        }


        public void AddDefaultConnection(Repository repository)
        {
            if (Connections.Count == 0)
            {
                //Add default connection
                MetaConnection connection = MetaConnection.Create(this);
                connection.ConnectionString = repository.Configuration.DefaultConnectionString;
                Connections.Add(connection);
                ConnectionGUID = connection.GUID;
            }
        }

        static public MetaSource Create(Repository repository)
        {
            MetaSource result = new MetaSource() { GUID = Guid.NewGuid().ToString(), Name = "Data Source", Repository = repository };
            result.AddDefaultConnection(repository);
            return result;
        }

        static public MetaSource LoadFromFile(string path, Repository repository)
        {
            MetaSource result = null;
            try
            {
                StreamReader sr = new StreamReader(path);
                XmlSerializer serializer = new XmlSerializer(typeof(MetaSource));
                result = (MetaSource)serializer.Deserialize(sr);
                sr.Close();
                result.Name = Path.GetFileNameWithoutExtension(path); 
                result.FilePath = path;
                result.LastModification = File.GetLastWriteTime(path);
                result.InitReferences(repository);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to read the file '{0}'.\r\n{1}", path, ex.Message));
            }
            return result;
        }

        public void SaveToFile()
        {
            SaveToFile(FilePath);
        }

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
            Name = Path.GetFileNameWithoutExtension(path);
            XmlSerializer serializer = new XmlSerializer(typeof(MetaSource));
            StreamWriter sw = new StreamWriter(path);
            serializer.Serialize(sw, (MetaSource)this);
            sw.Close();
            FilePath = path;
            LastModification = File.GetLastWriteTime(path);
        }

        public static string RefreshEnums(string path, Repository repository)
        {
            string result = "";

            return result;
        }

        public void Refresh()
        {
            if (_metaData == null) _metaData = new MetaData();
            foreach (var table in _metaData.Tables.Where(i => i.DynamicColumns == true && i.MustRefresh))
            {
                table.Refresh();
            }
        }


        public string CheckSQL(string sql, List<MetaTable> tables, ReportModel model, bool isPrePost)
        {
            string result = "";
            if (!string.IsNullOrEmpty(sql))
            {
                try
                {
                    DbConnection connection = (model != null ? model.Connection.GetOpenConnection() : GetOpenConnection());
                    Helper.ExecutePrePostSQL(connection, PreSQL, this, this.IgnorePrePostError); 
                    if (tables != null) foreach (var table in tables) Helper.ExecutePrePostSQL(connection, table.PreSQL, table, table.IgnorePrePostError);
                    if (!isPrePost && model != null) Helper.ExecutePrePostSQL(connection, model.PreSQL, model, model.IgnorePrePostError);
                    var command = connection.CreateCommand();
                    command.CommandText = sql;
                    command.ExecuteReader();
                    if (isPrePost && model != null) Helper.ExecutePrePostSQL(connection, model.PostSQL, model, model.IgnorePrePostError);
                    if (tables != null) foreach (var table in tables) Helper.ExecutePrePostSQL(connection, table.PostSQL, table, table.IgnorePrePostError);
                    Helper.ExecutePrePostSQL(connection, PostSQL, this, this.IgnorePrePostError);
                    command.Connection.Close();
                }
                catch (Exception ex)
                {
                    result = ex.Message;
                }
            }

            return result;
        }

        public DbConnection GetOpenConnection()
        {
            if (Connection == null) throw new Exception("No connection defined for this source. Please configure the database connection");
            return Connection.GetOpenConnection();
        }

        public void AddColumnsFromCatalog(List<MetaColumn> columns, DbConnection connection, MetaTable table)
        {
            if (table.Name == null) throw new Exception("No table name has been defined...");

            //handle if table name = dbname.owner.tablename
            string name = table.Name.Replace("[", "").Replace("]", "");
            string[] names = name.Split('.');
            DataTable schemaColumns = null;

            Helper.ExecutePrePostSQL(connection, table.PreSQL, table, table.IgnorePrePostError);
            if (names.Length == 3) schemaColumns = connection.GetSchema("Columns", names);
            else if (names.Length == 2) schemaColumns = connection.GetSchema("Columns", new string[] { null, names[0], names[1] });
            else schemaColumns = connection.GetSchema("Columns", new string[] { null, null, name });
            Helper.ExecutePrePostSQL(connection, table.PostSQL, table, table.IgnorePrePostError);

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
                    string odbcType = "";
                    if (row.Table.Columns.Contains("TYPE_NAME")) odbcType = row["TYPE_NAME"].ToString();
                    column.Type = connection is OdbcConnection ? Helper.ODBCToNetTypeConverter(odbcType) : Helper.DatabaseToNetTypeConverter(row["DATA_TYPE"]);
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

        public string GetTableName(string rawName)
        {
            string delimiters = getDelimiters(Connection.DatabaseType);
            var keywords = getKeywords(Connection.DatabaseType);
            if ( (!rawName.StartsWith("[") && !rawName.EndsWith("]") && rawName.IndexOfAny(" '\"-$-".ToCharArray()) != -1) 
                || keywords.Contains(rawName.ToUpper()))
                return string.Format("{0}{1}{2}", delimiters[0], rawName, delimiters[1]);
            return rawName;
        }

        public string GetColumnName(string rawName)
        {
            string delimiters = getDelimiters(Connection.DatabaseType);
            var keywords = getKeywords(Connection.DatabaseType);
            if ((!rawName.StartsWith("[") && !rawName.EndsWith("]") && rawName.IndexOfAny(" '\"-$-".ToCharArray()) != -1)
                || keywords.Contains(rawName.ToUpper()))
                return string.Format("{0}{1}{2}", delimiters[0], rawName, delimiters[1]);
            return rawName;
        }



        #region Helpers

        string _information;
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(8, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        string _error;
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(9, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        #endregion

    }
}
