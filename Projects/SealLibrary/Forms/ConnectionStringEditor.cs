//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Design;
using System.ComponentModel;
using Seal.Model;
using System.Windows.Forms;
using Seal.Helpers;

namespace Seal.Forms
{
    class ConnectionStringEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (!Helper.CheckOLEDBOS()) return value;
            MetaConnection connection = (MetaConnection)context.Instance;
            if (value == null || value is string) value = PromptForConnectionString(connection, connection.FullConnectionString, connection.ConnectionString);
            return value;
        }

        private string PromptForConnectionString(MetaConnection connection, string currentConnectionString, string initialConnectionString)
        {
            MSDASC.DataLinks dataLinks = new MSDASC.DataLinks();

            string generatedConnectionString = currentConnectionString;
            string resultConnectionString = "";

            ADODB.Connection dialogConnection = (ADODB.Connection)dataLinks.PromptNew();
            if (dialogConnection != null)
            {
                connection.UserName = "";
                connection.ClearPassword = "";
                generatedConnectionString = dialogConnection.ConnectionString.ToString();

                if (dialogConnection.Properties["Password"] != null && dialogConnection.Properties["Password"].Value != null && !generatedConnectionString.Contains("Password="))
                {
                    generatedConnectionString += string.Format(";Password={0}", dialogConnection.Properties["Password"].Value);
                }

                foreach (string config in generatedConnectionString.Split(';'))
                {
                    if (config.StartsWith("User ID="))
                    {
                        connection.UserName = config.Replace("User ID=", "").Replace("\"", "");
                        continue;
                    }
                    if (config.StartsWith("Password="))
                    {
                        connection.ClearPassword = config.Replace("Password=", "");
                        continue;
                    }
                    if (resultConnectionString != "") resultConnectionString += ";";
                    resultConnectionString += config;
                }

                if (!string.IsNullOrEmpty(connection.UserName) && string.IsNullOrEmpty(connection.ClearPassword)) MessageBox.Show("Note that the Password is empty (Perhaps did you not check the option 'Allow Saving Password')", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                DatabaseType newType = GetDatabaseType(resultConnectionString);
                if (newType != connection.DatabaseType)
                {
                    connection.DatabaseType = newType;
                    MessageBox.Show(string.Format("The database type has been set to {0}", newType), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                resultConnectionString = initialConnectionString;
            }
            return resultConnectionString;
        }

        public static DatabaseType GetDatabaseType(string connectionString)
        {
            DatabaseType result = DatabaseType.Standard;
            if (connectionString.ToLower().Contains("oracle"))
            {
                result = DatabaseType.Oracle;
            }
            else if (connectionString.ToLower().Contains(".mdb") || connectionString.ToLower().Contains(".accdb"))
            {
                result = DatabaseType.MSAccess;
            }
            else if (connectionString.ToLower().Contains(".xls") || connectionString.ToLower().Contains("excel driver"))
            {
                result = DatabaseType.MSExcel;
            }
            else if (connectionString.ToLower().Contains("sqlncli"))
            {
                result = DatabaseType.MSSQLServer;
            }
            
            return result;
        }

    }
}
