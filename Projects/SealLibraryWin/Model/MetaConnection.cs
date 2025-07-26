//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Seal.Helpers;
using System.Data.Common;
using MongoDB.Driver;
using Oracle.ManagedDataAccess.Client;
using System.Data;
#if WINDOWS
using System.Drawing.Design;
using Seal.Forms;
using DynamicTypeDescriptor;
using System.Windows.Forms;
#endif

namespace Seal.Model
{
    /// <summary>
    /// A MetaConnection defines a connection to a database
    /// </summary>
    public class MetaConnection : RootComponent
    {
        /// <summary>
        /// Current MetaSource
        /// </summary>
        [XmlIgnore]
        public MetaSource Source = null;

        public const string PasswordKeyName = "Meta Connection Password";
        public const string PasswordKeyValue = "1awéàèüwienyjhdl+256()$$";

#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Name").SetIsBrowsable(true);
                GetProperty("DatabaseType").SetIsBrowsable(true);
                GetProperty("DateTimeFormat").SetIsBrowsable(true);
                GetProperty("CommandTimeout").SetIsBrowsable(true);

                GetProperty("ConnectionType").SetIsBrowsable(true);
                GetProperty("ConnectionString").SetIsBrowsable(true);
                GetProperty("OdbcConnectionString").SetIsBrowsable(true);
                GetProperty("MSSqlServerConnectionString").SetIsBrowsable(true);
                GetProperty("MySQLConnectionString").SetIsBrowsable(true);
                GetProperty("OracleConnectionString").SetIsBrowsable(true);
                GetProperty("PostgreSQLConnectionString").SetIsBrowsable(true);
                GetProperty("SQLiteConnectionString").SetIsBrowsable(true);
                GetProperty("MongoDBConnectionString").SetIsBrowsable(Source != null && Source.IsNoSQL);
                GetProperty("ConnectionScript").SetIsBrowsable(true);

                GetProperty("ConnectionString").SetIsReadOnly(!IsEditable);
                GetProperty("OdbcConnectionString").SetIsReadOnly(!IsEditable);
                GetProperty("MSSqlServerConnectionString").SetIsReadOnly(!IsEditable);
                GetProperty("MySQLConnectionString").SetIsReadOnly(!IsEditable);
                GetProperty("OracleConnectionString").SetIsReadOnly(!IsEditable);
                GetProperty("PostgreSQLConnectionString").SetIsReadOnly(!IsEditable);
                GetProperty("SQLiteConnectionString").SetIsReadOnly(!IsEditable);
                GetProperty("MongoDBConnectionString").SetIsReadOnly(!IsEditable);
                GetProperty("CommandTimeout").SetIsReadOnly(!IsEditable);

                GetProperty("UserName").SetIsBrowsable(true);
                if (IsEditable) GetProperty("ClearPassword").SetIsBrowsable(true);

                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);
                GetProperty("HelperCheckConnection").SetIsBrowsable(true);
                GetProperty("Information").SetIsReadOnly(true);
                GetProperty("Error").SetIsReadOnly(true);
                GetProperty("HelperCheckConnection").SetIsReadOnly(true);

                GetProperty("DateTimeFormat").SetIsReadOnly(!IsEditable || DatabaseType == DatabaseType.MSAccess || DatabaseType == DatabaseType.MSExcel);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

#endif
        /// <summary>
        /// Create a basic connection into a source
        /// </summary>
        public static MetaConnection Create(MetaSource source)
        {
            return new MetaConnection() { Name = "connection", GUID = Guid.NewGuid().ToString(), Source = source };
        }

        /// <summary>
        /// The name of the connection
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [DisplayName("Name"), Description("The name of the connection."), Category("Definition"), Id(1, 1)]
#endif
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// The type of the source database
        /// </summary>
#if WINDOWS
        [DefaultValue(DatabaseType.Standard)]
        [DisplayName("Database type"), Description("The type of the source database."), Category("Definition"), Id(2, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
#endif
        public DatabaseType DatabaseType { get; set; } = DatabaseType.Standard;

        public char StartDelimiter
        {
            get
            {
                if (DatabaseType == DatabaseType.Oracle) return '\"';
                if (DatabaseType == DatabaseType.MySQL) return '`';
                if (DatabaseType == DatabaseType.PostgreSQL) return '\"';
                if (DatabaseType == DatabaseType.SQLite) return '\"';
                return '[';
            }
        }
        public char EndDelimiter
        {
            get
            {
                if (DatabaseType == DatabaseType.Oracle) return '\"';
                if (DatabaseType == DatabaseType.MySQL) return '`';
                if (DatabaseType == DatabaseType.PostgreSQL) return '\"';
                if (DatabaseType == DatabaseType.SQLite) return '\"';
                return ']';
            }
        }

        private ConnectionType _connectionType = ConnectionType.OleDb;
        /// <summary>
        /// The type of the connection used
        /// </summary>
#if WINDOWS
        [DefaultValue(ConnectionType.OleDb)]
        [DisplayName("Connection type"), Description("The type of the connection object used: OleDbConnection, OdbcConnection, SqlConnection (either from System.Data or Microsoft.Data) or MongoClient (valid only for a LINQ Source)."), Category("Definition"), Id(3, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
#endif
        public ConnectionType ConnectionType
        {
            get
            {
                return _connectionType;
            }
            set
            {
                _connectionType = value;
                if (_connectionType == ConnectionType.MySQL && DatabaseType != DatabaseType.MySQL)
                {
                    DatabaseType = DatabaseType.MySQL;
                }
                else if (_connectionType == ConnectionType.Oracle && DatabaseType != DatabaseType.Oracle)
                {
                    DatabaseType = DatabaseType.Oracle;
                }
                else if (_connectionType == ConnectionType.PostgreSQL && DatabaseType != DatabaseType.PostgreSQL)
                {
                    DatabaseType = DatabaseType.PostgreSQL;
                }
                else if (_connectionType == ConnectionType.SQLite && DatabaseType != DatabaseType.SQLite)
                {
                    DatabaseType = DatabaseType.SQLite;
                }
                else if ((_connectionType == ConnectionType.MSSQLServer || _connectionType == ConnectionType.MSSQLServerMicrosoft) && DatabaseType != DatabaseType.MSSQLServer)
                {
                    DatabaseType = DatabaseType.MSSQLServer;
#if WINDOWS
                    if (_dctd != null) MessageBox.Show(string.Format("The database type has been set to {0}", DatabaseType), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
#endif
                }
                UpdateEditorAttributes();
            }
        }

        /// <summary>
        /// OLE DB Connection string used to connect to the database if the connection type is OLE DB
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [DisplayName("OLE DB Connection string"), Description("OLE DB Connection string used to connect to the database if the connection type is OleDb. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Category("Connection String"), Id(4, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ConnectionString { get; set; } = null;
        public bool ShouldSerializeConnectionString() { return !string.IsNullOrEmpty(ConnectionString); }

        /// <summary>
        /// ODBC Connection string used to connect to the database if the connection type is ODBC
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [DisplayName("ODBC Connection string"), Description("ODBC Connection string used to connect to the database if the connection type is ODBC. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Category("Connection String"), Id(5, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string OdbcConnectionString { get; set; } = null;
        public bool ShouldSerializeOdbcConnectionString() { return !string.IsNullOrEmpty(OdbcConnectionString); }


        /// <summary>
        /// MS SQLServer Connection string used to connect to the database if the connection type is MS SQLServer
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [DisplayName("MS SQLServer Connection string"), Description("MS SQLServer Connection string used to connect to the database if the connection type is MS SQLServer. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Category("Connection String"), Id(6, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string MSSqlServerConnectionString { get; set; } = null;
        public bool ShouldSerializeMSSqlServerConnectionString() { return !string.IsNullOrEmpty(MSSqlServerConnectionString); }

        /// <summary>
        /// Mongo DB Connection string used to connect to the database if the connection type is Mongo DB
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [DisplayName("Mongo DB Connection string"), Description("Mongo DB Connection string used to connect to the database if the connection type is Mongo DB. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder. %USER% for the user name. %PASSWORD% for the password."), Category("Connection String"), Id(7, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string MongoDBConnectionString { get; set; } = null;
        public bool ShouldSerializeMongoDBConnectionString() { return !string.IsNullOrEmpty(MongoDBConnectionString); }

        /// <summary>
        /// MySQL Connection string used to connect to the database if the connection type is MySQL
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [DisplayName("MySQL Connection string"), Description("MySQL Connection string used to connect to the database if the connection type is MySQL. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Category("Connection String"), Id(8, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string MySQLConnectionString { get; set; } = null;
        public bool ShouldSerializeMySQLConnectionString() { return !string.IsNullOrEmpty(MySQLConnectionString); }

        /// <summary>
        /// Oracle Connection string used to connect to the database if the connection type is Oracle
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [DisplayName("Oracle Connection string"), Description("Oracle Connection string used to connect to the database if the connection type is Oracle. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Category("Connection String"), Id(9, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string OracleConnectionString { get; set; } = null;
        public bool ShouldSerializeOracleConnectionString() { return !string.IsNullOrEmpty(OracleConnectionString); }

#if WINDOWS
        [DefaultValue(null)]
        [DisplayName("PostgreSQL Connection string"), Description("PostgreSQL Connection string used to connect to the database if the connection type is PostgreSQL. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Category("Connection String"), Id(10, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string PostgreSQLConnectionString { get; set; } = null;
        public bool ShouldSerializePostgreSQLConnectionString() { return !string.IsNullOrEmpty(PostgreSQLConnectionString); }

        /// <summary>
        /// If set, script executed to instanciate and open the connection
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [DisplayName("SQLite Connection string"), Description("SQLite Connection string used to connect to the database if the connection type is SQLite. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Category("Connection String"), Id(10, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string SQLiteConnectionString { get; set; } = null;
        public bool ShouldSerializeSQLiteConnectionString() { return !string.IsNullOrEmpty(SQLiteConnectionString); }

        /// <summary>
        /// If set, script executed to instanciate and open the connection
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [DisplayName("Connection Script"), Description("If set, script executed to instanciate and open the connection."), Category("Definition"), Id(7, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ConnectionScript { get; set; } = null;
        public bool ShouldSerializeConnectionScript() { return !string.IsNullOrEmpty(ConnectionScript); }

        /// <summary>
        /// The date time format used to build date restrictions in the SQL WHERE clauses. This is not used for MS Access database (Serial Dates).
        /// </summary>
#if WINDOWS
        [DefaultValue("yyyy-MM-dd HH:mm:ss")]
        [DisplayName("Database Date Time format"), Description("The date time format used to build date restrictions in the SQL WHERE clauses. This is not used for MS Access database (Serial Dates)."), Category("Definition"), Id(10, 1)]
#endif
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public bool ShouldSerializeDateTimeFormat() { return DateTimeFormat != "yyyy-MM-dd HH:mm:ss"; }

        /// <summary>
        /// "Default Timeout in seconds for the SQL Statements executed. 0 means no Timeout.
        /// </summary>
#if WINDOWS
        [DefaultValue(0)]
        [DisplayName("Command Timeout"), Description("Default Timeout in seconds for the SQL Statements executed. 0 means no Timeout."), Category("Definition"), Id(11, 1)]
#endif
        public int CommandTimeout { get; set; } = 0;
        public bool ShouldSerializeCommandTimeout() { return CommandTimeout != 0; }

        /// <summary>
        /// Full Connection String (Oledb, Odbc, MSSQLServer, MongoDB, Oracle, MySQL) with user name and password
        /// </summary>
#if WINDOWS
        [Browsable(false)]
#endif
        public string FullConnectionString
        {
            get
            {
                return GetFullConnectionString(UserName, ClearPassword);
            }
        }

        public string GetRawConnectionString()
        {
            var result = "";
            if (ConnectionType == ConnectionType.MSSQLServer || ConnectionType == ConnectionType.MSSQLServerMicrosoft)
            {
                result = MSSqlServerConnectionString;
            }
            else if (ConnectionType == ConnectionType.Odbc)
            {
                result = OdbcConnectionString;
            }
            else if (ConnectionType == ConnectionType.MongoDB)
            {
                result = MongoDBConnectionString;
            }
            else if (ConnectionType == ConnectionType.MySQL)
            {
                result = MySQLConnectionString;
            }
            else if (ConnectionType == ConnectionType.Oracle)
            {
                result = OracleConnectionString;
            }
            else if (ConnectionType == ConnectionType.PostgreSQL)
            {
                result = PostgreSQLConnectionString;
            }
            else if (ConnectionType == ConnectionType.SQLite)
            {
                result = SQLiteConnectionString;
            }
            else
            {
                result = ConnectionString;
            }

            return result;
        }

        public string GetFullConnectionString(string userName, string password)
        {
            var result = "";
            if (ConnectionType == ConnectionType.MSSQLServer || ConnectionType == ConnectionType.MSSQLServerMicrosoft)
            {
                result = Helper.GetOleDbConnectionString(MSSqlServerConnectionString, userName, password);
            }
            else if (ConnectionType == ConnectionType.Odbc)
            {
                result = Helper.GetOdbcConnectionString(OdbcConnectionString, userName, password);
            }
            else if (ConnectionType == ConnectionType.MongoDB)
            {
                result = Helper.GetMongoConnectionString(MongoDBConnectionString, userName, password);
            }
            else if (ConnectionType == ConnectionType.MySQL)
            {
                result = Helper.GetMySQLConnectionString(MySQLConnectionString, userName, password);
            }
            else if (ConnectionType == ConnectionType.Oracle)
            {
                result = Helper.GetOleDbConnectionString(OracleConnectionString, userName, password);
            }
            else if (ConnectionType == ConnectionType.PostgreSQL)
            {
                result = Helper.GetOleDbConnectionString(PostgreSQLConnectionString, userName, password);
            }
            else if (ConnectionType == ConnectionType.SQLite)
            {
                result = Helper.GetOleDbConnectionString(SQLiteConnectionString, userName, password);
            }
            else
            {
                result = Helper.GetOleDbConnectionString(ConnectionString, userName, password);
            }

            return Source.Repository.ReplaceRepositoryKeyword(result);
        }

        public void  SetConnectionString(string connectionString)
        {
            if (ConnectionType == ConnectionType.MSSQLServer || ConnectionType == ConnectionType.MSSQLServerMicrosoft)
            {
                MSSqlServerConnectionString = connectionString;
            }
            else if (ConnectionType == ConnectionType.Odbc)
            {
                OdbcConnectionString = connectionString;
            }
            else if (ConnectionType == ConnectionType.MongoDB)
            {
                MongoDBConnectionString = connectionString;
            }
            else if (ConnectionType == ConnectionType.MySQL)
            {
                MySQLConnectionString = connectionString;
            }
            else if (ConnectionType == ConnectionType.Oracle)
            {
                OracleConnectionString = connectionString;
            }
            else if (ConnectionType == ConnectionType.PostgreSQL)
            {
                PostgreSQLConnectionString = connectionString;
            }
            else if (ConnectionType == ConnectionType.SQLite)
            {
                SQLiteConnectionString = connectionString;
            }
            else
            {
                ConnectionString = connectionString;
            }
        }

        /// <summary>
        /// User name used to connect to the database
        /// </summary>
#if WINDOWS
        [DisplayName("User name"), Description("User name used to connect to the database."), Category("Security"), Id(1, 5)]
#endif
        public string UserName { get; set; }
        public bool ShouldSerializeUserName() { return !string.IsNullOrEmpty(UserName); }

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }
        public bool ShouldSerializePassword() { return !string.IsNullOrEmpty(Password); }

        /// <summary>
        /// Password in clear text
        /// </summary>
#if WINDOWS
        [DisplayName("User password"), PasswordPropertyText(true), Description("Password used to connect to the database."), Category("Security"), Id(2, 5)]
#endif
        [XmlIgnore]
        public string ClearPassword
        {
            get
            {
                try
                {
                    return Source?.Repository.DecryptValue(Password, PasswordKeyName);
                }
                catch (Exception ex)
                {
                    Error = "Error during password decryption:" + ex.Message;
                    TypeDescriptor.Refresh(this);
                    return Password;
                }
            }
            set
            {
                try
                {
                    if (Source != null) Password = Source.Repository.EncryptValue(value, PasswordKeyName);
                }
                catch (Exception ex)
                {
                    Error = "Error during password encryption:" + ex.Message;
                    TypeDescriptor.Refresh(this);
                    Password = value;
                }
            }
        }

        /// <summary>
        /// True if the connection is editable
        /// </summary>
        [XmlIgnore]
        public bool IsEditable = true;


        /// <summary>
        /// Result connection set by the ConnectionScript if any
        /// </summary>
        [XmlIgnore]
        public DbConnection DbConnection;

        /// <summary>
        /// Returns an open DbConnection object
        /// </summary>
        public DbConnection GetOpenConnection()
        {
            try
            {
                DbConnection result = null;
                lock (this) //Protect several connections at the same time
                {
                    if (!string.IsNullOrEmpty(ConnectionScript))
                    {
                        RazorHelper.CompileExecute(ConnectionScript, this);
                        return DbConnection;
                    }

                    result = Helper.DbConnectionFromConnectionString(ConnectionType, FullConnectionString); ;
                    result.Open();
                    if (DatabaseType == DatabaseType.Oracle)
                    {
                        try
                        {
                            var command = result.CreateCommand();
                            command.CommandText = "alter session set nls_date_format='yyyy-mm-dd hh24:mi:ss'";
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Helper.WriteLogException("GetOpenConnection", ex);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error opening database connection:\r\n{0}", ex.Message));
            }
        }

        /// <summary>
        /// Check the current connection
        /// </summary>
        public void CheckConnection()
        {
#if WINDOWS
            Cursor.Current = Cursors.WaitCursor;
#endif
            Error = "";
            Information = "";
            try
            {
                if (ConnectionType != ConnectionType.MongoDB)
                {
                    GetOpenConnection();
                }
                else
                {
                    MongoClient client = new MongoClient(FullConnectionString);
                    var dbs = client.ListDatabaseNames().ToList();
                }
                Information = "Database connection checked successfully.";

            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Information = "Error got when checking the connection.";
                if (ConnectionType == ConnectionType.Oracle && OracleConfiguration.OracleDataSources.Count == 0)
                {
                    Information = "For Oracle, consider to use the 'Connection Script' to configure OracleConfiguration.OracleDataSources";
                }

            }
            if (DbConnection != null && DbConnection.State == System.Data.ConnectionState.Open)
            {
                DbConnection.Close();
            };


            Information = Helper.FormatMessage(Information);
#if WINDOWS
            UpdateEditorAttributes();
            Cursor.Current = Cursors.Default;
#endif
        }

        #region Helpers
        /// <summary>
        /// Editor Helper: Check the database connection
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Check connection"), Description("Check the database connection."), Id(1, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
#endif
        public string HelperCheckConnection
        {
            get { return "<Click to check database connection>"; }
        }

        /// <summary>
        /// Last information message when the enum list has been refreshed
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Information"), Description("Last information message when the enum list has been refreshed."), Id(2, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string Information { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(3, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string Error { get; set; }

        #endregion
    }
}
