//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Seal.Converter;
using Seal.Forms;
using System.Xml.Serialization;
using DynamicTypeDescriptor;
using System.Data.OleDb;
using System.Data.Odbc;
using Seal.Helpers;
using System.Windows.Forms;
using System.Data.Common;
using System.Data;

namespace Seal.Model
{

    public class MetaConnection : RootComponent
    {
        [XmlIgnore]
        public MetaSource Source = null;

        #region Editor

        static string PasswordKey = "1awéàèüwienyjhdl+256()$$";

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
                if (IsEditable) GetProperty("ConnectionString").SetIsBrowsable(true);
                else GetProperty("ConnectionString2").SetIsBrowsable(true);
                GetProperty("UserName").SetIsBrowsable(true);
                if (IsEditable) GetProperty("ClearPassword").SetIsBrowsable(true);
                
                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);
                GetProperty("HelperCheckConnection").SetIsBrowsable(true);
                if (IsEditable) GetProperty("HelperCreateFromExcelAccess").SetIsBrowsable(true);

                GetProperty("Information").SetIsReadOnly(true);
                GetProperty("Error").SetIsReadOnly(true);
                GetProperty("HelperCheckConnection").SetIsReadOnly(true);
                if (IsEditable) GetProperty("HelperCreateFromExcelAccess").SetIsReadOnly(true);

                GetProperty("DateTimeFormat").SetIsReadOnly(!IsEditable || _databaseType == DatabaseType.MSAccess || _databaseType == DatabaseType.MSExcel);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        public static MetaConnection Create(MetaSource source)
        {
            return new MetaConnection() { Name = "connection", GUID = Guid.NewGuid().ToString(), Source = source };
        }

        [DefaultValue(null)]
        [DisplayName("Name"), Description("The name of the connection."), Category("Definition"), Id(1, 1)]
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private DatabaseType _databaseType = DatabaseType.Standard;
        [DefaultValue(DatabaseType.Standard)]
        [DisplayName("Database type"), Description("The type of the source database."), Category("Definition"), Id(2, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public DatabaseType DatabaseType
        {
            get { return _databaseType; }
            set { _databaseType = value; }
        }

        private string _connectionString;
        [DefaultValue(null)]
        [DisplayName("Connection string"), Description("OLEDB Connection string used to connect to the database. The string can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Category("Definition"), Id(3, 1)]
        [Editor(typeof(ConnectionStringEditor), typeof(UITypeEditor))]
        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        [DefaultValue(null)]
        [DisplayName("Connection string"), Description("OLEDB Connection string used to connect to the database."), Category("Definition"), Id(3, 1)]
        [XmlIgnore]
        public string ConnectionString2
        {
            get { return _connectionString; }
        }

        private string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        [DefaultValue("yyyy-MM-dd HH:mm:ss")]
        [DisplayName("Date Time format"), Description("The date time format used to build date restrictions in the SQL WHERE clauses. This is not used for MS Access database (Serial Dates)."), Category("Definition"), Id(4, 1)]
        public string DateTimeFormat
        {
            get { return _dateTimeFormat; }
            set { _dateTimeFormat = value; }
        }

        [Browsable(false)]
        public string FullConnectionString
        {
            get {
                string result = Helper.GetOleDbConnectionString(_connectionString, _userName, ClearPassword);
                return Source.Repository.ReplaceRepositoryKeyword(result); 
            }
        }

        public string SQLServerConnectionString
        {
            get
            {
                OleDbConnectionStringBuilder builder = new System.Data.OleDb.OleDbConnectionStringBuilder(FullConnectionString);
                string result = string.Format("Server={0};Database={1};", builder["Data Source"], builder["Initial Catalog"], builder["User ID"], builder["Password"]);
                result += (builder.ContainsKey("User ID") ? string.Format("User Id={0};Password={1};", builder["User ID"], builder["Password"]) : "Trusted_Connection=True;");
                return result;
            }
        }

        private string _userName;
        [DisplayName("User name"), Description("User name used to connect to the database."), Category("Security"), Id(1, 2)]
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }
        public bool ShouldSerializePassword() { return !string.IsNullOrEmpty(_password); }

        [DisplayName("User password"), PasswordPropertyText(true), Description("Password used to connect to the database."), Category("Security"), Id(2, 2)]
        [XmlIgnore]
        public string ClearPassword
        {
            get {
                try
                {
                    return CryptoHelper.DecryptTripleDES(_password, PasswordKey);
                }
                catch (Exception ex)
                {
                    _error = "Error during password decryption:" + ex.Message;
                    TypeDescriptor.Refresh(this);
                    return _password;
                }
            }
            set {
                try
                {
                    _password = CryptoHelper.EncryptTripleDES(value, PasswordKey);
                }
                catch(Exception ex)
                {
                    _error = "Error during password encryption:" + ex.Message;
                    TypeDescriptor.Refresh(this);
                    _password = value;
                }
            }
        }

        
        [XmlIgnore]
        public bool IsEditable = true;

        [XmlIgnore]
        private DbConnection DbConnection
        {
            get
            {
                return Helper.DbConnectionFromConnectionString(FullConnectionString);
            }
        }

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
                    catch { }
                }

                return connection;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error opening database connection:\r\n{0}", ex.Message));
            }
        }

        public void CheckConnection()
        {
            Cursor.Current = Cursors.WaitCursor;
            _error = "";
            _information = "";
            try
            {
                DbConnection connection = DbConnection;
                connection.Open();
                connection.Close();
                _information = "Database connection checked successfully.";
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _information = "Error got when checking the connection.";
            }
            _information = Helper.FormatMessage(_information);
            UpdateEditorAttributes();
            Cursor.Current = Cursors.Default;
        }

        #region Helpers

        [Category("Helpers"), DisplayName("Check connection"), Description("Check the database connection."), Id(1, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperCheckConnection
        {
            get { return "<Click to check database connection>"; }
        }

        [Category("Helpers"), DisplayName("Create connection from Excel or MS Access"), Description("Helper to create a connection string to query an Excel workbook or a MS Access database."), Id(1, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperCreateFromExcelAccess
        {
            get { return "<Click to create a connection from an Excel or a MS Access file>"; }
        }
        string _information;
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message when the enum list has been refreshed."), Id(2, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        string _error;
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(3, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        #endregion
    }
}
