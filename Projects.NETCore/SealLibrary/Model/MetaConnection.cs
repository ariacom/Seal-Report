//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Xml.Serialization;
using Seal.Helpers;
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
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// The type of the source database
        /// </summary>
        public DatabaseType DatabaseType { get; set; } = DatabaseType.Standard;

        private ConnectionType _connectionType = ConnectionType.OleDb;
        /// <summary>
        /// The type of the connection used
        /// </summary>
        public ConnectionType ConnectionType
        {
            get {
                return _connectionType;
            }
            set
            {
                _connectionType = value;
#if !NETCOREAPP
                if (_connectionType == ConnectionType.MSSQLServer && DatabaseType != DatabaseType.MSSQLServer) {
                    DatabaseType = DatabaseType.MSSQLServer;
                    if (_dctd != null) MessageBox.Show(string.Format("The database type has been set to {0}", DatabaseType), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
#endif
            }

        }

        /// <summary>
        /// OLE DB Connection string used to connect to the database if the connection type is OLE DB
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Property Helper for editor
        /// </summary>
        [XmlIgnore]
        public string ConnectionString2
        {
            get { return ConnectionString; }
        }

        /// <summary>
        /// ODBC Connection string used to connect to the database if the connection type is ODBC
        /// </summary>
        public string OdbcConnectionString { get; set; }


        /// <summary>
        /// MS SQLServer Connection string used to connect to the database if the connection type is MS SQLServer
        /// </summary>
        public string MSSqlServerConnectionString { get; set; }

        /// <summary>
        /// The date time format used to build date restrictions in the SQL WHERE clauses. This is not used for MS Access database (Serial Dates).
        /// </summary>
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
        public string UserName { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }
        public bool ShouldSerializePassword() { return !string.IsNullOrEmpty(Password); }

        /// <summary>
        /// Password in clear text
        /// </summary>
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
        }


        #region Helpers
        /// <summary>
        /// Editor Helper: Check the database connection
        /// </summary>
        public string HelperCheckConnection
        {
            get { return "<Click to check database connection>"; }
        }

        /// <summary>
        /// Editor Helper: Helper to create a connection string to query an Excel workbook or a MS Access database
        /// </summary>
        public string HelperCreateFromExcelAccess
        {
            get { return "<Click to create a connection from an Excel or a MS Access file>"; }
        }

        /// <summary>
        /// Last information message when the enum list has been refreshed
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

