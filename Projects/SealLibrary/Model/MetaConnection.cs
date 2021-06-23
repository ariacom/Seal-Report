//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.ComponentModel;
using System.Drawing.Design;
using Seal.Forms;
using System.Xml.Serialization;
using DynamicTypeDescriptor;
using Seal.Helpers;
using System.Windows.Forms;
using System.Data.Common;
using System.Data;
using System.Data.SqlClient;
using System.Data.Odbc;

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

        static string PasswordKey = "1awéàèüwienyjhdl+256()$$";

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

                GetProperty("ConnectionType").SetIsBrowsable(true);
                GetProperty("ConnectionString").SetIsBrowsable(true);
                GetProperty("OdbcConnectionString").SetIsBrowsable(true);
                GetProperty("MSSqlServerConnectionString").SetIsBrowsable(true);

                GetProperty("ConnectionString").SetIsReadOnly(!IsEditable);
                GetProperty("OdbcConnectionString").SetIsReadOnly(!IsEditable);
                GetProperty("MSSqlServerConnectionString").SetIsReadOnly(!IsEditable);

                GetProperty("UserName").SetIsBrowsable(true);
                if (IsEditable) GetProperty("ClearPassword").SetIsBrowsable(true);

                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);
                GetProperty("HelperCheckConnection").SetIsBrowsable(true);
                if (IsEditable && !Environment.Is64BitProcess)
                {
                    GetProperty("HelperCreateFromExcelAccess").SetIsBrowsable(true);
                    GetProperty("HelperCreateFromExcelAccess").SetIsReadOnly(true);
                }
                GetProperty("Information").SetIsReadOnly(true);
                GetProperty("Error").SetIsReadOnly(true);
                GetProperty("HelperCheckConnection").SetIsReadOnly(true);

                GetProperty("DateTimeFormat").SetIsReadOnly(!IsEditable || DatabaseType == DatabaseType.MSAccess || DatabaseType == DatabaseType.MSExcel);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

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
        [DefaultValue(null)]
        [DisplayName("Name"), Description("The name of the connection."), Category("Definition"), Id(1, 1)]
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// The type of the source database
        /// </summary>
        [DefaultValue(DatabaseType.Standard)]
        [DisplayName("Database type"), Description("The type of the source database."), Category("Definition"), Id(2, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public DatabaseType DatabaseType { get; set; } = DatabaseType.Standard;

        private ConnectionType _connectionType = ConnectionType.OleDb;
        /// <summary>
        /// The type of the connection used
        /// </summary>
        [DefaultValue(ConnectionType.OleDb)]
        [DisplayName("Connection type"), Description("The type of the connection object used: OleDbConnection, OdbcConnection or SqlConnection."), Category("Definition"), Id(3, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public ConnectionType ConnectionType
        {
            get {
                return _connectionType;
            }
            set
            {
                _connectionType = value;
                if (_connectionType == ConnectionType.MSSQLServer && DatabaseType != DatabaseType.MSSQLServer) {
                    DatabaseType = DatabaseType.MSSQLServer;
                    if (_dctd != null) MessageBox.Show(string.Format("The database type has been set to {0}", DatabaseType), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                UpdateEditorAttributes();
            }
        }

        /// <summary>
        /// OLE DB Connection string used to connect to the database if the connection type is OLE DB
        /// </summary>
        [DefaultValue(null)]
        [DisplayName("OLE DB Connection string"), Description("OLE DB Connection string used to connect to the database if the connection type is OleDb. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Category("Definition"), Id(4, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string ConnectionString { get; set; }

        /// <summary>
        /// ODBC Connection string used to connect to the database if the connection type is ODBC
        /// </summary>
        [DefaultValue(null)]
        [DisplayName("ODBC Connection string"), Description("ODBC Connection string used to connect to the database if the connection type is ODBC. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Category("Definition"), Id(5, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string OdbcConnectionString { get; set; }


        /// <summary>
        /// MS SQLServer Connection string used to connect to the database if the connection type is MS SQLServer
        /// </summary>
        [DefaultValue(null)]
        [DisplayName("MS SQLServer Connection string"), Description("MS SQLServer Connection string used to connect to the database if the connection type is MS SQLServer. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Category("Definition"), Id(6, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string MSSqlServerConnectionString { get; set; }

        /// <summary>
        /// The date time format used to build date restrictions in the SQL WHERE clauses. This is not used for MS Access database (Serial Dates).
        /// </summary>
        [DefaultValue("yyyy-MM-dd HH:mm:ss")]
        [DisplayName("Database Date Time format"), Description("The date time format used to build date restrictions in the SQL WHERE clauses. This is not used for MS Access database (Serial Dates)."), Category("Definition"), Id(7, 1)]
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Full Connection String (Oledb, Odbc or MSSQLServer) with user name and password
        /// </summary>
        [Browsable(false)]
        public string FullConnectionString
        {
            get
            {
                var result = "";
                if (ConnectionType == ConnectionType.MSSQLServer)
                {
                    result = Helper.GetOleDbConnectionString(MSSqlServerConnectionString, UserName, ClearPassword);

                }
                else if (ConnectionType == ConnectionType.Odbc)
                {
                    result = Helper.GetOdbcConnectionString(OdbcConnectionString, UserName, ClearPassword);
                }
                else
                {
                    result = Helper.GetOleDbConnectionString(ConnectionString, UserName, ClearPassword);
                }

                return Source.Repository.ReplaceRepositoryKeyword(result);
            }
        }

        /// <summary>
        /// User name used to connect to the database
        /// </summary>
        [DisplayName("User name"), Description("User name used to connect to the database."), Category("Security"), Id(1, 2)]
        public string UserName { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }
        public bool ShouldSerializePassword() { return !string.IsNullOrEmpty(Password); }

        /// <summary>
        /// Password in clear text
        /// </summary>
        [DisplayName("User password"), PasswordPropertyText(true), Description("Password used to connect to the database."), Category("Security"), Id(2, 2)]
        [XmlIgnore]
        public string ClearPassword
        {
            get
            {
                try
                {
                    return CryptoHelper.DecryptTripleDES(Password, PasswordKey);
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
                    Password = CryptoHelper.EncryptTripleDES(value, PasswordKey);
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
        /// Result for the database connection script
        /// </summary>
        [XmlIgnore]
        public DbConnection DbConnectionResult;

        [XmlIgnore]
        private DbConnection DbConnection
        {
            get
            {
                return Helper.DbConnectionFromConnectionString(ConnectionType, FullConnectionString);
            }
        }

        /// <summary>
        /// Returns an open DbConnection object
        /// </summary>
        public DbConnection GetOpenConnection()
        {
            try
            {
                DbConnection connection = DbConnection;
                connection.Open();
                if (DatabaseType == DatabaseType.Oracle)
                {
                    try
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = "alter session set nls_date_format='yyyy-mm-dd hh24:mi:ss'";
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                return connection;
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
            Cursor.Current = Cursors.WaitCursor; 
            Error = "";
            Information = "";
            try
            {
                DbConnection connection = DbConnection;
                connection.Open();
                connection.Close();
                Information = "Database connection checked successfully.";
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Information = "Error got when checking the connection.";
            }
            Information = Helper.FormatMessage(Information);
            UpdateEditorAttributes(); 
            Cursor.Current = Cursors.Default; 
        }


        #region Helpers
        /// <summary>
        /// Editor Helper: Check the database connection
        /// </summary>
        [Category("Helpers"), DisplayName("Check connection"), Description("Check the database connection."), Id(1, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperCheckConnection
        {
            get { return "<Click to check database connection>"; }
        }

        /// <summary>
        /// Editor Helper: Helper to create a connection string to query an Excel workbook or a MS Access database
        /// </summary>
        [Category("Helpers"), DisplayName("Create connection from Excel or MS Access"), Description("Helper to create a connection string to query an Excel workbook or a MS Access database."), Id(1, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperCreateFromExcelAccess
        {
            get { return "<Click to create a connection from an Excel or a MS Access file>"; }
        }

        /// <summary>
        /// Last information message when the enum list has been refreshed
        /// </summary>
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message when the enum list has been refreshed."), Id(2, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(3, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error { get; set; }

        #endregion
    }
}
