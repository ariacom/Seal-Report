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
        [DefaultValue(null)]
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// The type of the source database
        /// </summary>
        [DefaultValue(DatabaseType.Standard)]
        public DatabaseType DatabaseType { get; set; } = DatabaseType.Standard;

        /// <summary>
        /// OLEDB Connection string used to connect to the database
        /// </summary>
        [DefaultValue(null)]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Property Helper for editor
        /// </summary>
        [DefaultValue(null)]
        [XmlIgnore]
        public string ConnectionString2
        {
            get { return ConnectionString; }
        }

        /// <summary>
        /// If set and the Database type is 'MS SQLServer', a native MS SQLServer connection is used (SqlConnection object instead of OleDbConnection or OdbcConnection) and the 'OLE DB Connection string' is not used.
        /// </summary>
        [DefaultValue(null)]
        public string MSSqlServerConnectionString { get; set; }

        /// <summary>
        /// The date time format used to build date restrictions in the SQL WHERE clauses. This is not used for MS Access database (Serial Dates).
        /// </summary>
        [DefaultValue("yyyy-MM-dd HH:mm:ss")]
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Full OLEdb Connection String with user name and password
        /// </summary>
        [Browsable(false)]
        public string FullConnectionString
        {
            get
            {
                string result = Helper.GetOleDbConnectionString(ConnectionString, UserName, ClearPassword);
                return Source.Repository.ReplaceRepositoryKeyword(result);
            }
        }

        /// <summary>
        /// Full MS SqlServer Connection String with user name and password
        /// </summary>
        public string FullMSSqlServerConnectionString
        {
            get
            {
                string result = Helper.GetOleDbConnectionString(MSSqlServerConnectionString, UserName, ClearPassword);
                return Source.Repository.ReplaceRepositoryKeyword(result);
            }
        }

        /// <summary>
        /// True, if the SqlServer driver will be used intead of the OLEdb driver
        /// </summary>
        public bool IsMSSqlServerConnection
        {
            get
            {
                return !string.IsNullOrEmpty(MSSqlServerConnectionString) && DatabaseType == DatabaseType.MSSQLServer;
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
                if (IsMSSqlServerConnection)
                    return new SqlConnection(FullMSSqlServerConnectionString);
                else
                    return Helper.DbConnectionFromConnectionString(FullConnectionString);
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

#if !NETCOREAPP
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
            
            Cursor.Current = Cursors.Default;
        }
#endif

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

