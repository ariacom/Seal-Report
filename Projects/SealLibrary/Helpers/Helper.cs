//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using Seal.Model;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Web;
using RazorEngine.Templating;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Xml.Serialization;
using System.Globalization;
using System.Net.Mail;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AnalysisServices.AdomdClient;

namespace Seal.Helpers
{
    /// <summary>
    /// Helper Objects
    /// </summary>
    internal class NamespaceDoc
    {
    }

    public partial class Helper
    {
        public static string GetEnumDescription(Type type, Object value)
        {
            FieldInfo fi = type.GetField(Enum.GetName(type, value));
            DescriptionAttribute dna = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
            if (dna != null) return dna.Description;
            else return value.ToString();
        }

        static public void CopyProperties(object src, object dest, string[] skipNames = null)
        {
            foreach (PropertyDescriptor item in TypeDescriptor.GetProperties(src))
            {
                if (skipNames != null && skipNames.Contains(item.Name)) continue;
                item.SetValue(dest, item.GetValue(src));
            }
        }

        static public bool ArePropertiesIdentical(object obj1, object obj2, string skipEmptySuffix = "")
        {
            bool result = true;
            foreach (PropertyDescriptor item in TypeDescriptor.GetProperties(obj1))
            {
                if (item.IsReadOnly) continue;
                if (!string.IsNullOrEmpty(skipEmptySuffix) && string.IsNullOrEmpty(item.GetValue(obj1).ToString()) && item.Name.EndsWith(skipEmptySuffix)) continue;

                if (item.GetValue(obj1).ToString() != item.GetValue(obj2).ToString())
                {
                    result = false;
                    break;
                }
            }
            return result;
        }


        static public void CopyPropertiesDifferentObjects(object src, object dest)
        {
            var propSource = TypeDescriptor.GetProperties(src);
            var propDest = TypeDescriptor.GetProperties(dest);
            foreach (PropertyDescriptor itemDest in propDest)
            {
                var itemSource = propSource.Find(itemDest.Name, true);
                if (itemSource != null)
                {
                    try
                    {
                        itemDest.SetValue(dest, itemSource.GetValue(src));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(itemDest.Name + " " + ex.Message);
                    }
                }
            }
        }

        static public void CopyPropertiesFromReference(object defaultObject, object referenceObject, object destObject)
        {
            foreach (PropertyDescriptor item in TypeDescriptor.GetProperties(defaultObject))
            {
                if (item.GetValue(destObject) == item.GetValue(defaultObject)) item.SetValue(destObject, item.GetValue(referenceObject));
            }
        }


        static public void SetPropertyValue(object item, string propertyName, string propertyValue)
        {
            PropertyInfo prop = item.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (null != prop && prop.CanWrite)
            {
                if (prop.PropertyType.IsEnum)
                {
                    var propertyVals = propertyValue.Split(' ');
                    prop.SetValue(item, Enum.Parse(prop.PropertyType, propertyVals[0]));
                }
                else if (prop.PropertyType.Name == "Boolean" && !string.IsNullOrWhiteSpace(propertyValue))
                {
                    prop.SetValue(item, bool.Parse(propertyValue));
                }
                else if (prop.PropertyType.Name == "Int32")
                {
                    prop.SetValue(item, int.Parse(propertyValue));
                }
                else
                {
                    prop.SetValue(item, propertyValue);
                }
            }
        }


        static public string NewGUID()
        {
            return Guid.NewGuid().ToString().Replace("-","");
        } 

        static public string QuoteDouble(string input)
        {
            if (input == null) input = "";
            return string.Format("\"{0}\"", input.Replace("\"", "\"\""));
        }

        static public string QuoteSingle(string input)
        {
            if (input == null) input = "";
            return string.Format("'{0}'", input.Replace("'", "''"));
        }

        static public string FirstNotEmpty(string str1, string str2 = null, string str3 = null, string str4 = null, string str5 = null)
        {
            if (!string.IsNullOrEmpty(str1)) return str1;
            if (!string.IsNullOrEmpty(str2)) return str2;
            if (!string.IsNullOrEmpty(str3)) return str3;
            if (!string.IsNullOrEmpty(str4)) return str4;
            if (!string.IsNullOrEmpty(str5)) return str5;
            return "";
        }

        static public string AddAttribute(string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) return string.Format("{0}='{1}'", name, value);
            return "";
        }

        static public string AddIfNotNull(string prefix, object input, string suffix)
        {
            if (input != null && input.ToString() != "") return prefix + input.ToString() + suffix;
            return "";
        }


        static public string AddIfNotEmpty(string prefix, string input, string suffix)
        {
            if (!string.IsNullOrEmpty(input)) return prefix + input + suffix;
            return "";
        }

        static public string AddNotEmpty(string separator, string input)
        {
            if (!string.IsNullOrEmpty(input)) return separator + input;
            return "";
        }

        static public string AddNotEmpty2(string input, string separator)
        {
            if (!string.IsNullOrEmpty(input)) return input + separator;
            return "";
        }

        static public void AddValue(ref string input, string separator, string value)
        {
            if (!string.IsNullOrEmpty(input)) input += separator + value;
            else input = value;
        }

        static public void AddValue(ref StringBuilder input, string separator, string value)
        {
            if (input.Length > 0) input.Append(separator + value);
            else input = new StringBuilder(value);
        }

        static public bool CompareTrim(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return true;
            if (s1 != null && s2 != null) return s1.Trim() == s2.Trim();
            return false;
        }

        static public List<string> GetStringList(string listInput)
        {
            var result = new List<string>();
            if (!string.IsNullOrEmpty(listInput))
            {
                foreach (var input in listInput.Replace("\r\n", ";").Split(';'))
                {
                    if (!string.IsNullOrEmpty(input)) result.Add(input);
                }
            }
            return result;
        }

        static public bool ValidateNumeric(string value, out Double d)
        {
            bool result = false;
            if (Double.TryParse(value, out d))
            {
                result = true;
            }
            else
            {
                if (value.Contains(".")) value = value.Replace(".", ",");
                else if (value.Contains(",")) value = value.Replace(",", ".");
                if (Double.TryParse(value, out d)) result = true;
            }
            return result;
        }

        static public string ConcatCellValues(ResultCell[] cells, string separator)
        {
            string result = "";
            foreach (var cell in cells) Helper.AddValue(ref result, separator, cell.DisplayValue);
            return result;
        }

        static public string IfNullOrEmpty(string value, string defaultValue)
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            return value;
        }

        static public string ToHtml(string value)
        {
            if (value != null) return HttpUtility.HtmlEncode(value).Replace("\r\n", "<br>").Replace("\n", "<br>");
            return "";
        }

        static public string ToJS(bool value)
        {
            return value.ToString().ToLower();
        }

        static public string ToJS(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return HttpUtility.JavaScriptStringEncode(value);
        }


        static public string ToMomentJSFormat(CultureInfo culture, string datetimeFormat)
        {
            string format = datetimeFormat;
            if (datetimeFormat == "d") format = culture.DateTimeFormat.ShortDatePattern;
            else if (datetimeFormat == "D") format = culture.DateTimeFormat.LongDatePattern;
            else if (datetimeFormat == "t") format = culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "T") format = culture.DateTimeFormat.LongTimePattern;
            else if (datetimeFormat == "g") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "G") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
            else if (datetimeFormat == "f") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "F") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.LongTimePattern;

            return format.Replace("y", "Y").Replace("d", "D").Replace("tt", "A").Replace("z", "Z").Replace("/", culture.DateTimeFormat.DateSeparator);
        }

        static public string RemoveHTMLTags(string value)
        {
            return Regex.Replace(value, @"<[^>]+>|&nbsp;", "").Trim();
        }

        static public string GetUniqueName(string name, List<string> entities)
        {
            string result;
            int i = 1;
            while (true)
            {
                result = name;
                if (i != 1) result += i.ToString();
                if (!entities.Contains(result)) break;
                i++;
            }
            return result;
        }

        static public string DBNameToDisplayName(string name)
        {
            string result = name;
            result = string.Join(" ", Regex.Split(result, @"([A-Z][a-z]+)"));
            result = result.Replace('_', ' ').Trim();
            result = result.Replace("  ", " ");
            result = result.Replace("  ", " ");
            result = result.Replace("  ", " ");
            if (result.Length > 0) result = result.Substring(0, 1).ToUpper() + result.Substring(1);
            return result.Trim();
        }


        static public ColumnType DatabaseToNetTypeConverter(object dbValue)
        {
            ColumnType result = ColumnType.Text;
            try
            {
                OleDbType columnType = (OleDbType)Convert.ToInt32(dbValue);
                result = Helper.NetTypeConverter(Helper.OleDbToNetTypeConverter(columnType));
                if (columnType == OleDbType.WChar || columnType == OleDbType.VarWChar || columnType == OleDbType.LongVarWChar) result = ColumnType.UnicodeText;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }

        static public ColumnType ODBCToNetTypeConverter(string odbcType)
        {
            ColumnType result = ColumnType.Text;
            try
            {
                result = Helper.OdbcTypeConverter(odbcType);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }

        static public ColumnType OdbcTypeConverter(string dbType)
        {
            string t = dbType.ToLower();

            if (t == "char" || t == "varchar" || t == "varchar2" || t == "text" || t == "uniqueidentifier") return ColumnType.Text;
            if (t == "nchar" || t == "ntext" || t == "nvarchar") return ColumnType.UnicodeText;
            if (t == "date" || t == "datetime" || t == "datetime2" || t == "smalldatetime" || t == "timestamp") return ColumnType.DateTime;
            return ColumnType.Numeric;
        }

        static public Type OleDbToNetTypeConverter(OleDbType oleDbTypeNumber)
        {
            switch ((int)oleDbTypeNumber)
            {
                case 0: return typeof(Nullable);
                case 2: return typeof(Int16);
                case 3: return typeof(Int32);
                case 4: return typeof(Single);
                case 5: return typeof(Double);
                case 6: return typeof(Decimal);
                case 7: return typeof(DateTime);
                case 8: return typeof(String);
                case 9: return typeof(Object);
                case 10: return typeof(Exception);
                case 11: return typeof(Boolean);
                case 12: return typeof(Object);
                case 13: return typeof(Object);
                case 14: return typeof(Decimal);
                case 16: return typeof(SByte);
                case 17: return typeof(Byte);
                case 18: return typeof(UInt16);
                case 19: return typeof(UInt32);
                case 20: return typeof(Int64);
                case 21: return typeof(UInt64);
                case 64: return typeof(DateTime);
                case 72: return typeof(Guid);
                case 128: return typeof(Byte[]);
                case 129: return typeof(String);
                case 130: return typeof(String);
                case 131: return typeof(Decimal);
                case 133: return typeof(DateTime);
                case 134: return typeof(TimeSpan);
                case 135: return typeof(DateTime);
                case 138: return typeof(Object);
                case 139: return typeof(Decimal);
                case 200: return typeof(String);
                case 201: return typeof(String);
                case 202: return typeof(String);
                case 203: return typeof(String);
                case 204: return typeof(Byte[]);
                case 205: return typeof(Byte[]);
            }
            throw (new Exception("DataType Not Supported"));
        }

        static public ColumnType NetTypeConverter(Type netType)
        {
            if (netType == typeof(string) || netType == typeof(Guid)) return ColumnType.Text;
            if (netType == typeof(DateTime)) return ColumnType.DateTime;
            return ColumnType.Numeric;
        }

        public static List<String> GetSystemDriverList()
        {
            List<string> names = new List<string>();
            // get system dsn's
            Microsoft.Win32.RegistryKey reg = (Microsoft.Win32.Registry.LocalMachine).OpenSubKey("Software");
            if (reg != null)
            {
                reg = reg.OpenSubKey("ODBC");
                if (reg != null)
                {
                    reg = reg.OpenSubKey("ODBCINST.INI");
                    if (reg != null)
                    {

                        reg = reg.OpenSubKey("ODBC Drivers");
                        if (reg != null)
                        {
                            // Get all DSN entries defined in DSN_LOC_IN_REGISTRY.
                            foreach (string sName in reg.GetValueNames())
                            {
                                names.Add(sName);
                            }
                        }
                        try
                        {
                            reg.Close();
                        }
                        catch { /* ignore this exception if we couldn't close */ }
                    }
                }
            }

            return names;
        }
        static public string ConvertNumericStandardFormat(NumericStandardFormat format)
        {
            if (format == NumericStandardFormat.Numeric0) return "N0";
            else if (format == NumericStandardFormat.Numeric1) return "N1";
            else if (format == NumericStandardFormat.Numeric2) return "N2";
            else if (format == NumericStandardFormat.Numeric3) return "N3";
            else if (format == NumericStandardFormat.Numeric4) return "N4";
            else if (format == NumericStandardFormat.Percentage0) return "P0";
            else if (format == NumericStandardFormat.Percentage1) return "P1";
            else if (format == NumericStandardFormat.Percentage2) return "P2";
            else if (format == NumericStandardFormat.Currency0) return "C0";
            else if (format == NumericStandardFormat.Currency2) return "C2";
            else if (format == NumericStandardFormat.Decimal) return "D";
            else if (format == NumericStandardFormat.Decimal0) return "D0";
            else if (format == NumericStandardFormat.Decimal1) return "D1";
            else if (format == NumericStandardFormat.Decimal2) return "D2";
            else if (format == NumericStandardFormat.Decimal3) return "D3";
            else if (format == NumericStandardFormat.Decimal4) return "D4";
            else if (format == NumericStandardFormat.Exponential) return "E";
            else if (format == NumericStandardFormat.Exponential2) return "E2";
            else if (format == NumericStandardFormat.Fixedpoint) return "F";
            else if (format == NumericStandardFormat.Fixedpoint2) return "F2";
            else if (format == NumericStandardFormat.General) return "G";
            else if (format == NumericStandardFormat.General2) return "G2";
            else if (format == NumericStandardFormat.General5) return "G5";
            else if (format == NumericStandardFormat.Hexadecimal) return "H";
            else if (format == NumericStandardFormat.Hexadecimal8) return "H8";

            return "0";
        }

        static public string ConvertDateTimeStandardFormat(DateTimeStandardFormat format)
        {
            if (format == DateTimeStandardFormat.ShortDate) return "d";
            else if (format == DateTimeStandardFormat.LongDate) return "D";
            else if (format == DateTimeStandardFormat.ShortTime) return "t";
            else if (format == DateTimeStandardFormat.LongTime) return "T";
            else if (format == DateTimeStandardFormat.ShortDateTime) return "g";
            else if (format == DateTimeStandardFormat.LongDateTime) return "G";
            else if (format == DateTimeStandardFormat.FullShortDateTime) return "f";
            else if (format == DateTimeStandardFormat.FullLongDateTime) return "F";
            return "0";
        }

        static public string GetExceptionMessage(TemplateCompilationException ex)
        {
            var result = new StringBuilder("");
            var firstError = "";
            foreach (var err in ex.CompilerErrors)
            {
                if (string.IsNullOrEmpty(firstError) && err.Line > 0) firstError = err.ErrorText + "\r\n\r\n";
                result.AppendFormat("{0}\r\nLine {1} Column {2} Error Number {3}\r\n", err.ErrorText, err.Line, err.Column, err.ErrorNumber);
            }
            return firstError + result.ToString();
        }

        static public void ExecutePrePostSQL(DbConnection connection, string sql, object model, bool ignoreErrors)
        {
            try
            {
                if (!string.IsNullOrEmpty(sql))
                {
                    string finalSql = RazorHelper.CompileExecute(sql, model);
                    if (!string.IsNullOrEmpty(sql))
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = finalSql;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                if (!ignoreErrors) throw;
            }
        }


        public static string FormatMessage(string message)
        {
            return string.Format("[{0} {1}] {2} ", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), message);
        }

        public static string CleanFileName(string s)
        {
            foreach (char character in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(character.ToString(), string.Empty);
            }

            foreach (char character in Path.GetInvalidPathChars())
            {
                s = s.Replace(character.ToString(), string.Empty);
            }

            return s;
        }

        static DateTime NextDailyPuge = DateTime.MinValue;
        const string TaskSchedulerEntry = "Seal Task Scheduler";
        public static void WriteDailyLog(string prefix, string logsFolder, int logDays, string message)
        {
            try
            {
                if (logDays <= 0) return;

                if (!Directory.Exists(logsFolder)) Directory.CreateDirectory(logsFolder);

                lock (TaskSchedulerEntry)
                {

                    string logFileName = Path.Combine(logsFolder, string.Format("{0}_{1:yyyy_MM_dd}.txt", prefix, DateTime.Now));
                    File.AppendAllText(logFileName, message);

                    if (NextDailyPuge < DateTime.Now)
                    {
                        NextDailyPuge = DateTime.Now.AddDays(1);
                        foreach (var file in Directory.GetFiles(logsFolder, "*"))
                        {
                            //purge old files...
                            if (File.GetLastWriteTime(file).AddDays(logDays) < DateTime.Now)
                            {
                                try
                                {
                                    File.Delete(file);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(message + "\r\n" + ex.Message);
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(message + "\r\n" + ex.Message);
            }
        }

        public static void WriteLogEntry(string source, EventLogEntryType type, string message, params object[] args)
        {
            string msg = message;
            try
            {
                if (args.Length != 0) msg = string.Format(message, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            try
            {
                Console.WriteLine(msg);

                var fullMessage = string.Format("**********\r\n{0} {1}\r\n{2}\r\n\r\n", DateTime.Now, type.ToString(), msg);
                Helper.WriteDailyLog(source == TaskSchedulerEntry ? "schedules" : "events", Repository.Instance.LogsFolder, Repository.Instance.Configuration.LogDays, fullMessage);

                if (msg.Length > 25000)
                {
                    msg = msg.Substring(0, 25000) + "\r\n...\r\nMessage truncated, check the event log files in the Logs Repository sub-folder.";
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) EventLog.WriteEntry(source, msg, type);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void WriteLogEntryScheduler(EventLogEntryType type, string message, params object[] args)
        {
            WriteLogEntry(TaskSchedulerEntry, type, message, args);
        }

        public static bool IsMachineAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);

                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Couldn't determine user account status: " + ex.Message);
                return false;
            }
            return true;
        }


        public static bool IsValidOS()
        {
            int major = Environment.OSVersion.Version.Major;
            return (major >= 6);
        }


        public static void DisplayDataTable(DataTable table)
        {
            foreach (System.Data.DataRow row in table.Rows)
            {
                foreach (System.Data.DataColumn col in table.Columns)
                {
                    Debug.WriteLine("{0} = {1}", col.ColumnName, row[col]);
                }
                Debug.WriteLine("============================");
            }
        }


        public static DataTable GetDataTable(DbConnection connection, string sql)
        {
            DataTable result = new DataTable();
            DbDataAdapter adapter = null;
            if (connection is OdbcConnection)
            {
                OdbcCommand command = ((OdbcConnection)connection).CreateCommand();
                command.CommandTimeout = 0;
                command.CommandText = sql;
                adapter = new OdbcDataAdapter(command);
            }
            else if (connection is SqlConnection)
            {
                SqlCommand command = ((SqlConnection)connection).CreateCommand();
                command.CommandTimeout = 0;
                command.CommandText = sql;
                adapter = new SqlDataAdapter(command);
            }
            else
            {
                OleDbCommand command = ((OleDbConnection)connection).CreateCommand();
                command.CommandTimeout = 0;
                command.CommandText = sql;
                adapter = new OleDbDataAdapter(command);
            }
            adapter.Fill(result);
            return result;
        }

        public static bool FindReplacePattern(string source, ref int index, string pattern, string replace, StringBuilder result)
        {
            if (index + pattern.Length <= source.Length && source.Substring(index, pattern.Length) == pattern)
            {
                result.Append(replace);
                index += pattern.Length - 1;
                return true;
            }
            return false;

        }

        public static object Clone(Object source)
        {
            XmlSerializer serializer = new XmlSerializer(source.GetType());
            MemoryStream ms = new MemoryStream();
            serializer.Serialize(ms, source);
            ms.Position = 0;
            return serializer.Deserialize(ms);
        }

        public static DbConnection DbConnectionFromConnectionString(ConnectionType connectionType, string connectionString)
        {
            DbConnection connection;
            if (connectionType == ConnectionType.MSSQLServer)
            {
                connection = new SqlConnection(connectionString);
            }
            else if (connectionType == ConnectionType.Odbc)
            {
                connection = new OdbcConnection(connectionString);
            }
            else
            {
                OleDbConnectionStringBuilder builder = new OleDbConnectionStringBuilder(connectionString);
                string provider = builder["Provider"].ToString();
                if (provider.StartsWith("MSDASQL"))
                {
                    //Provider=MSDASQL.1;Persist Security Info=False;Extended Properties="DSN=mysql2;SERVER=localhost;UID=root;DATABASE=sakila;PORT=3306";Initial Catalog=sakila
                    //Provider=MSDASQL.1;Persist Security Info=True;Data Source=mysql;Initial Catalog=sakila
                    //Provider=MSDASQL.1;Persist Security Info=False;Extended Properties="DSN=brCRM;DBQ=C:\tem\adb.mdb;DriverId=25;FIL=MS Access;MaxBufferSize=2048;PageTimeout=5;UID=admin;"

                    //Extract the real ODBC connection string...to be able to use the OdbcConnection
                    string odbcConnectionString = "";
                    if (builder.ContainsKey("Extended Properties")) odbcConnectionString = builder["Extended Properties"].ToString();
                    else if (builder.ContainsKey("Data Source") && !string.IsNullOrEmpty(builder["Data Source"].ToString())) odbcConnectionString = "DSN=" + builder["Data Source"].ToString();
                    if (odbcConnectionString != "" && builder.ContainsKey("Initial Catalog")) odbcConnectionString += ";DATABASE=" + builder["Initial Catalog"].ToString();
                    if (odbcConnectionString != "" && builder.ContainsKey("User ID")) odbcConnectionString += ";UID=" + builder["User ID"].ToString();
                    if (odbcConnectionString != "" && builder.ContainsKey("Password")) odbcConnectionString += ";PWD=" + builder["Password"].ToString();

                    connection = new OdbcConnection(odbcConnectionString);
                }
                else
                {
                    connection = new OleDbConnection(connectionString);
                }
            }

            return connection;
        }

        static public string GetOleDbConnectionString(string input, string userName, string password)
        {
            string result = input;
            if (input != null && !input.Contains("User ID=") && !string.IsNullOrEmpty(userName)) result += string.Format(";User ID={0}", userName);
            if (input != null && !input.Contains("Password=") && !string.IsNullOrEmpty(password)) result += string.Format(";Password={0}", password);
            return result;
        }

        static public string GetOdbcConnectionString(string input, string userName, string password)
        {
            string result = input;
            if (input != null && !input.Contains("UID=") && !string.IsNullOrEmpty(userName)) result += string.Format(";UID={0}", userName);
            if (input != null && !input.Contains("PWD=") && !string.IsNullOrEmpty(password)) result += string.Format(";PWD={0}", password);
            return result;
        }

        public static int CalculateHash(string str)
        {
            return string.IsNullOrEmpty(str) ? 0 : str.GetHashCode();
        }


        public static string HtmlMakeImageSrcData(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] filebytes = new byte[fs.Length];
            fs.Read(filebytes, 0, Convert.ToInt32(fs.Length));
            var ext = Path.GetExtension(path);
            string type;
            if (ext == ".ico") type = "x-icon";
            else type = ext.Replace(".", "");
            return "data:image/" + type + ";base64," + Convert.ToBase64String(filebytes, Base64FormattingOptions.None);
        }


        static public bool HasTimeFormat(DateTimeStandardFormat formatType, string format)
        {
            if (formatType.ToString().Contains("Time")) return true;
            return ((formatType == DateTimeStandardFormat.Custom || formatType == DateTimeStandardFormat.Default)
                && (format.ToLower().Contains("t") || format.Contains("H") || format.Contains("m") || format.Contains("s")));
        }

        /// <summary>
        /// Add email address to a MailAddressCollection
        /// </summary>
        static public void AddEmailAddresses(MailAddressCollection collection, string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                string[] addresses = input.Replace(";", "\r\n").Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                foreach (string address in addresses)
                {
                    if (!string.IsNullOrWhiteSpace(address)) collection.Add(address);
                }
            }
        }

        //SQL Keywords management
        public static string ClearAllSQLKeywords(string sql, ReportModel model = null)
        {
            sql = ClearSQLKeywords(sql, Repository.EnumFilterKeyword, "filter");
            sql = ClearSQLKeywords(sql, Repository.EnumValuesKeyword, "NULL");
            if (model != null) sql = model.ParseCommonRestrictions(sql);
            sql = ClearSQLKeywords(sql, Repository.CommonRestrictionKeyword, "1=1");
            sql = ClearSQLKeywords(sql, Repository.CommonValueKeyword, "NULL");
            return sql;
        }

        //SQL Keywords management
        public static string ClearAllLINQKeywords(string script)
        {
            script = ClearSQLKeywords(script, Repository.EnumFilterKeyword, "null");
            script = ClearSQLKeywords(script, Repository.EnumValuesKeyword, "null");
            return script;
        }

        public static string ClearSQLKeywords(string sql, string keyword, string replacedBy)
        {
            if (string.IsNullOrEmpty(sql)) return "";

            //Replace keyword by 1=1
            int index = 0;
            do
            {
                index = sql.IndexOf(keyword, index);
                if (index > 0)
                {
                    index += keyword.Length;
                    for (int i = index; i < sql.Length; i++)
                    {
                        if (sql[i] == '}')
                        {
                            sql = sql.Replace(keyword + sql.Substring(index, i - index) + "}", replacedBy);
                            index -= keyword.Length;
                            break;
                        }
                    }
                }
            }
            while (index > 0 && index < sql.Length);
            return sql;
        }

        public static string AddCTE(string current, string CTE)
        {
            var result = current;
            if (!string.IsNullOrEmpty(result))
            {
                if (CTE != null && CTE.Length > 5 && CTE.ToLower().Trim().StartsWith("with"))
                {
                    var startIndex = CTE.ToLower().IndexOf("with");
                    if (startIndex >= 0) result += "," + CTE.Substring(startIndex + 5);
                }
            }
            else result = CTE;

            return result;
        }

        public static List<string> GetSQLKeywordNames(string sql, string keyword)
        {
            var result = new List<string>();
            //Get keywords
            int index = 0;
            do
            {
                index = sql.IndexOf(keyword, index);
                if (index > 0)
                {
                    index += keyword.Length;
                    string restrictionName = "";
                    for (int i = index; i < sql.Length; i++)
                    {
                        if (sql[i] == '}')
                        {
                            restrictionName = sql.Substring(index, i - index); ;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(restrictionName)) result.Add(restrictionName);
                }

            }
            while (index > 0);
            return result;
        }

#if !NETCOREAPP
        public static void RunInAnotherAppDomain(string assemblyFile, string[] args)
        {
            // RazorEngine cannot clean up from the default appdomain...
            Console.WriteLine("Switching to second AppDomain, for RazorEngine...");
            AppDomainSetup adSetup = new AppDomainSetup();
            adSetup.ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var current = AppDomain.CurrentDomain;
            // You only need to add strongnames when your appdomain is not a full trust environment.
            var strongNames = new System.Security.Policy.StrongName[0];

            var domain = AppDomain.CreateDomain(
                Path.GetFileNameWithoutExtension(assemblyFile), null,
                current.SetupInformation, new System.Security.PermissionSet(System.Security.Permissions.PermissionState.Unrestricted),
                strongNames);
            domain.ExecuteAssembly(assemblyFile, args);
            // RazorEngine will cleanup. 
            AppDomain.Unload(domain);
        }
#endif

        public static string GetApplicationDirectory()
        {
            var assembly = Assembly.GetEntryAssembly();
            if (assembly == null) assembly = Assembly.GetCallingAssembly();
            if (assembly == null) assembly = new StackTrace().GetFrames().Last().GetMethod().Module.Assembly;
            return Path.GetDirectoryName(assembly.Location);
        }

        public static string FromTimeSpan(TimeSpan val, string def, Repository repository)
        {
            string result = "";
            if (val != null)
            {
                string secStr = repository == null ? "second" : repository.TranslateWebJS("second").ToLower();
                string minStr = repository == null ? "minute" : repository.TranslateWebJS("minute").ToLower();
                string hourStr = repository == null ? "hour" : repository.TranslateWebJS("hour").ToLower();
                string dayStr = repository == null ? "day" : repository.TranslateWebJS("day").ToLower();
                string secStr2 = repository == null ? "seconds" : repository.TranslateWebJS("seconds").ToLower();
                string minStr2 = repository == null ? "minutes" : repository.TranslateWebJS("minutes").ToLower();
                string hourStr2 = repository == null ? "hours" : repository.TranslateWebJS("hours").ToLower();
                string dayStr2 = repository == null ? "days" : repository.TranslateWebJS("days").ToLower();

                if (val.Seconds > 0) result = val.Seconds.ToString() + " " + (val.Seconds > 1 ? secStr2 : secStr);
                else if (val.Minutes > 0) result = val.Minutes.ToString() + " " + (val.Minutes > 1 ? minStr2 : minStr);
                else if (val.Hours > 0) result = val.Hours.ToString() + " " + (val.Hours > 1 ? hourStr2 : hourStr);
                else if (val.Days > 0) result = val.Days.ToString() + " " + (val.Days > 1 ? dayStr2 : dayStr);
                else result = def;
            }
            return result;
        }

        public static TimeSpan ToTimeSpan(string str, Repository repository)
        {
            string secStr = repository == null ? "second" : repository.TranslateWebJS("second").ToLower();
            string minStr = repository == null ? "minute" : repository.TranslateWebJS("minute").ToLower();
            string hourStr = repository == null ? "hour" : repository.TranslateWebJS("hour").ToLower();
            string dayStr = repository == null ? "day" : repository.TranslateWebJS("day").ToLower();
            TimeSpan result = new TimeSpan(0, 0, 0);
            if (!string.IsNullOrEmpty(str))
            {
                int val;
                str = str.ToLower();
                if (str.Contains(secStr))
                {
                    if (int.TryParse(str.Replace(" ", "").Replace(secStr, "").Replace("s", ""), out val)) result = new TimeSpan(0, 0, val);
                    else throw new Exception("Invalid interval");
                }
                else if (str.Contains(minStr))
                {
                    if (int.TryParse(str.Replace(" ", "").Replace(minStr, "").Replace("s", ""), out val)) result = new TimeSpan(0, val, 0);
                    else throw new Exception("Invalid interval");
                }
                else if (str.Contains(hourStr))
                {
                    if (int.TryParse(str.Replace(" ", "").Replace(hourStr, "").Replace("s", ""), out val)) result = new TimeSpan(val, 0, 0);
                    else throw new Exception("Invalid interval");
                }
                else if (str.Contains(dayStr))
                {
                    if (int.TryParse(str.Replace(" ", "").Replace(dayStr, "").Replace("s", ""), out val)) result = new TimeSpan(val, 0, 0, 0);
                    else throw new Exception("Invalid interval");
                }
            }
            return result;
        }

        /// <summary>
        /// Convert an objet to a string, handling DBNull value
        /// </summary>
        static public string ToString(object obj)
        {
            if (obj != null && obj == DBNull.Value) obj = null;
            return Convert.ToString(obj);
        }

        /// <summary>
        /// Convert an objet to a datetime, handling DBNull value
        /// </summary>
        static public DateTime? ToDateTime(object obj)
        {
            if (obj != null && obj == DBNull.Value) obj = null;
            return obj == null ? (DateTime?) null : Convert.ToDateTime(obj);
        }

        /// <summary>
        /// Convert an objet to a double, handling DBNull value
        /// </summary>
        static public double? ToDouble(object obj)
        {
            if (obj != null && obj == DBNull.Value) obj = null;
            return obj == null ? (double?)null : Convert.ToDouble(obj);
        }
    }
}
