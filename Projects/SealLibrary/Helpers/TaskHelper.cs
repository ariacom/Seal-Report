using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Seal.Model;
using System.Data;
using System.IO;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Diagnostics;


namespace Seal.Helpers
{
    public class TaskHelper
    {
        ReportTask _task = null;
        public TaskHelper(ReportTask task)
        {
            _task = task;
        }

        public void LogMessage(string message, params object[] args)
        {
            Log.LogMessage(message, args);
        }

        ReportExecutionLog Log
        {
            get { return _task.Report; }
        }

        public TaskDatabaseHelper DatabaseHelper = new TaskDatabaseHelper();

        public void RefreshRepositoryEnums(string sourceName = "")
        {
            Repository repository = Repository.Create();
            Log.LogMessage("Starting Refresh Enumerated Lists of all Repository sources.");
            foreach (MetaSource source in repository.Sources.OrderBy(i => i.Name).Where(i => string.IsNullOrEmpty(sourceName) || i.Name.ToLower() == sourceName.ToLower()))
            {
                try
                {
                    Log.LogMessage("Processing data source '{0}'", source.Name);
                    foreach (MetaEnum enumItem in source.MetaData.Enums.Where(i => i.IsDynamic).OrderBy(i => i.Name))
                    {
                        Log.LogMessage("Refreshing Enum '{0}'", enumItem.Name);
                        enumItem.RefreshEnum(false);
                        if (!string.IsNullOrEmpty(enumItem.Error))
                        {
                            Log.LogMessage("ERROR:" + enumItem.Error);
                        }
                    }
                    Log.LogMessage("Saving data source '{0}' in '{1}'\r\n", source.Name, source.FilePath);
                    source.SaveToFile();
                }
                catch (Exception ex)
                {
                    Log.LogMessage("\r\n[UNEXPECTED ERROR RECEIVED]\r\n{0}\r\n", ex.Message);
                }
            }
            Log.LogMessage("Refresh Enumerated Lists terminated\r\n");
        }

        public bool CheckForNewFileSource(string loadFolder, string sourceFilePath)
        {
            bool result = false;
            Log.LogMessage("Checking for new version of '{0}'", sourceFilePath);
            string loadPath = Path.Combine(loadFolder, Path.GetFileName(sourceFilePath));
            if (!Directory.Exists(Path.GetDirectoryName(loadPath))) Directory.CreateDirectory(Path.GetDirectoryName(loadPath));
            if (!File.Exists(sourceFilePath)) throw new Exception(string.Format("Invalid Excel source file '{0}'", sourceFilePath));

            //Check if the file has changed
            if (File.Exists(sourceFilePath) && (!File.Exists(loadPath) || File.GetLastWriteTime(loadPath) < File.GetLastWriteTime(sourceFilePath)))
            {
                Log.LogMessage("File has changed, reload it");
                result = true;
            }
            return result;
        }

        void LogDebug()
        {
            if (DatabaseHelper.DebugLog.Length > 0)
            {
                Log.LogMessage("Debug Log:\r\n{0}", DatabaseHelper.DebugLog.ToString());
                DatabaseHelper.DebugLog = new StringBuilder();
            }
        }


        public bool LoadTablesFromExcel(string loadFolder, string sourceExcelPath, string[] sourceTabNames, string[] destinationTableNames, bool useAllConnections = false)
        {
            bool result = false;
            try
            {
                if (sourceTabNames.Length != destinationTableNames.Length) throw new Exception("The number of Source Tabs number and the number of Destination Tables are different.");
                if (CheckForNewFileSource(loadFolder, sourceExcelPath))
                {
                    for (int i = 0; i < sourceTabNames.Length && !_task.CancelReport; i++)
                    {
                        LoadTableFromExcel(sourceExcelPath, sourceTabNames[i], destinationTableNames[i], useAllConnections);
                    }
                    File.Copy(sourceExcelPath, Path.Combine(loadFolder, Path.GetFileName(sourceExcelPath)), true);
                    result = true;
                }
                else
                {
                    Log.LogMessage("No import done");
                }
            }
            finally
            {
                LogDebug();
            }
            return result;
        }


        public bool LoadTableFromExcel(string loadFolder, string sourceExcelPath, string sourceTabName, string destinationTableName, bool useAllConnections = false)
        {
            bool result = false;
            try
            {
                if (CheckForNewFileSource(loadFolder, sourceExcelPath))
                {
                    LoadTableFromExcel(sourceExcelPath, sourceTabName, destinationTableName, useAllConnections);
                    File.Copy(sourceExcelPath, Path.Combine(loadFolder, Path.GetFileName(sourceExcelPath)), true);
                    result = true;
                }
                else
                {
                    Log.LogMessage("No import done");
                }
            }
            finally
            {
                LogDebug();
            }
            return result;
        }

        public void LoadTableFromExcel(string sourceExcelPath, string sourceTabName, string destinationTableName, bool useAllConnections = false)
        {
            try
            {
                string sourcePath = _task.Repository.ReplaceRepositoryKeyword(sourceExcelPath);
                Log.LogMessage("Starting Loading Excel Table from '{0}'", sourcePath);
                DataTable table = DatabaseHelper.LoadDataTableFromExcel(sourcePath, sourceTabName);
                table.TableName = destinationTableName;
                foreach (var connection in _task.Source.Connections.Where(i => useAllConnections || i.GUID == _task.Connection.GUID))
                {
                    if (_task.CancelReport) break;
                    Log.LogMessage("\r\nImporting table for connection '{0}'.", connection.Name);
                    DatabaseHelper.SetDatabaseDefaultConfiguration(connection.DatabaseType);
                    Log.LogMessage("Dropping and creating table '{0}'", destinationTableName);
                    DatabaseHelper.CreateTable(_task.GetDbCommand(connection), table);
                    Log.LogMessage("Copying {0} rows in '{1}'", table.Rows.Count, destinationTableName);
                    DatabaseHelper.InsertTable(_task.GetDbCommand(connection), table, connection.DateTimeFormat, false);
                }
            }
            finally
            {
                LogDebug();
            }
        }

        public bool LoadTableFromCSV(string loadFolder, string sourceCsvPath, string destinationTableName, char? separator=null, bool useAllConnections = false)
        {
            bool result = false;
            try
            {
                if (CheckForNewFileSource(loadFolder, sourceCsvPath))
                {
                    LoadTableFromCSV(sourceCsvPath, destinationTableName, separator, useAllConnections);
                    File.Copy(sourceCsvPath, Path.Combine(loadFolder, Path.GetFileName(sourceCsvPath)), true);
                    result = true;
                }
                else
                {
                    Log.LogMessage("No import done");
                }
            }
            finally
            {
                LogDebug();
            }
            return result;
        }

        public void LoadTableFromCSV(string sourceCsvPath, string destinationTableName, char? separator = null, bool useAllConnections = false)
        {
            try
            {
                string sourcePath = _task.Repository.ReplaceRepositoryKeyword(sourceCsvPath);
                Log.LogMessage("Starting Loading CSV Table from '{0}'", sourcePath);
                DataTable table = DatabaseHelper.LoadDataTableFromCSV(sourcePath, separator);
                table.TableName = destinationTableName;
                foreach (var connection in _task.Source.Connections.Where(i => useAllConnections || i.GUID == _task.Connection.GUID))
                {
                    if (_task.CancelReport) break;
                    Log.LogMessage("\r\nImporting table for connection '{0}'.", connection.Name);
                    DatabaseHelper.SetDatabaseDefaultConfiguration(connection.DatabaseType);
                    Log.LogMessage("Dropping and creating table '{0}'", destinationTableName);
                    DatabaseHelper.CreateTable(_task.GetDbCommand(connection), table);
                    Log.LogMessage("Copying {0} rows in '{1}'", table.Rows.Count, destinationTableName);
                    DatabaseHelper.InsertTable(_task.GetDbCommand(connection), table, connection.DateTimeFormat, false);
                }
            }
            finally
            {
                LogDebug();
            }
        }

        public bool LoadTableFromExternalSource(string sourceConnectionString, string sourceSelectStatement, string destinationTableName, bool useAllConnections = false, string sourceCheckSelect = "", string destinationCheckSelect = "")
        {
            bool result = false;
            try
            {
                string connectionString = _task.Repository.ReplaceRepositoryKeyword(sourceConnectionString);
                Log.LogMessage("Starting Loading Table using '{0}'", sourceSelectStatement);
                DataTable table = null;
                foreach (var connection in _task.Source.Connections.Where(i => useAllConnections || i.GUID == _task.Connection.GUID))
                {
                    if (_task.CancelReport) break;
                    Log.LogMessage("\r\nImporting table for connection '{0}'.", connection.Name);
                    bool doIt = true;
                    if (!string.IsNullOrEmpty(sourceCheckSelect) && !string.IsNullOrEmpty(destinationCheckSelect))
                    {
                        Log.LogMessage("Checking if load is required using '{0}' and '{1}'", sourceCheckSelect, destinationCheckSelect);
                        doIt = false;
                        DataTable checkTable1 = DatabaseHelper.LoadDataTable(connectionString, sourceCheckSelect);
                        if (_task.CancelReport) break;
                        DataTable checkTable2 = DatabaseHelper.LoadDataTable(connection.FullConnectionString, destinationCheckSelect);
                        if (_task.CancelReport) break;
                        if (!DatabaseHelper.AreTablesIdentical(checkTable1, checkTable2)) doIt = true;
                    }

                    if (doIt && !_task.CancelReport)
                    {
                        result = true;
                        if (table == null)
                        {
                            table = DatabaseHelper.LoadDataTable(connectionString, _task.Repository.ReplaceRepositoryKeyword(sourceSelectStatement));
                            table.TableName = destinationTableName;
                        }

                        Log.LogMessage("Dropping and creating table '{1}'", connection.Name, destinationTableName);
                        DatabaseHelper.SetDatabaseDefaultConfiguration(connection.DatabaseType);
                        DatabaseHelper.CreateTable(_task.GetDbCommand(connection), table);
                        Log.LogMessage("Copying {0} rows in '{1}'", table.Rows.Count, destinationTableName);
                        DatabaseHelper.InsertTable(_task.GetDbCommand(connection), table, connection.DateTimeFormat, false);
                    }
                    else
                    {
                        Log.LogMessage("No import done");
                    }
                }
            }
            finally
            {
                LogDebug();
            }
            return result;
        }

        public void ExecuteProcess(string path)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            Log.LogMessage("Executing '{0}'", path);
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            string err = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            Log.LogMessage(output);
            if (!string.IsNullOrEmpty(err))
            {
                throw new Exception(err);
            }
        }

        public void ExecuteNonQuery(string sql, bool useAllConnections = false)
        {
            foreach (var connection in _task.Source.Connections.Where(i => useAllConnections || i.GUID == _task.Connection.GUID))
            {
                if (_task.CancelReport) break;
                var command = _task.GetDbCommand(connection);
                command.CommandText = sql;
                DatabaseHelper.ExecuteCommand(command);
            }
        }

        public object ExecuteScalar(string sql)
        {
            var connection = _task.Source.Connections.FirstOrDefault(i => i.GUID == _task.Connection.GUID);
            if (connection != null)
            {
                var command = _task.GetDbCommand(connection);
                command.CommandText = sql;
                return command.ExecuteScalar();
            }
            return null;
        }


        //SANDBOX !
        //Just use this to code, compile and debug your Razor Script within Visual Studio...
        //When OK, just cut and paste it into the Script of your Task using the Report Designer
        public void DesignMyRazorScript()
        {
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("Id", typeof(string)));
            table.Columns.Add(new DataColumn("Date", typeof(DateTime)));
            table.Columns.Add(new DataColumn("Title", typeof(string)));
            table.Columns.Add(new DataColumn("Summary", typeof(string)));
            table.Columns.Add(new DataColumn("Link", typeof(string)));
            table.Columns.Add(new DataColumn("Categories", typeof(string)));

            var task = this._task;
        	TaskHelper helper = this;
            ReportExecutionLog log = task.Report;
            //Just replace helper.DesignMyRazorScript(); with the code below            


            var reader = System.Xml.XmlReader.Create("http://msdn.microsoft.com/en-us/subscriptions/subscription-downloads.rss");
            var feed = System.ServiceModel.Syndication.SyndicationFeed.Load(reader);
            foreach (var item in feed.Items)
            {
                string link = item.Links.Count >0 ? item.Links[0].Uri.AbsoluteUri : "";
                string categories = "";
                foreach (var category in item.Categories)
                {
                    categories += category.Name + ";";
                }
                table.Rows.Add(item.Id, item.LastUpdatedTime.DateTime, item.Title.Text, item.Summary.Text, link, categories);
            }


            foreach(var path in File.ReadAllLines(@"c:\temp\test.sql"))
            {
                var newPath = path.Replace("@", "").Replace("\"", "").Replace(";", "");
                var command = task.GetDbCommand(task.Connection);
                command.CommandText = File.ReadAllText(newPath);
                command.ExecuteScalar();
            }
        }
    }

}
