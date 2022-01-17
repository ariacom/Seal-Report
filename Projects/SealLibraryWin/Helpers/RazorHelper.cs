//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Data.OleDb;
using RazorEngine;
using RazorEngine.Templating;
using System.Data;
using System.DirectoryServices.Protocols;
using System.Xml.Linq;
using System.ServiceModel.Syndication;
#if WINDOWS
using System.Windows.Forms;
#endif
using Seal.Model;
using System.DirectoryServices.AccountManagement;
using Jose;
using Newtonsoft.Json.Linq;
using ICSharpCode.SharpZipLib.Zip;
using System.Data.Odbc;
using System.Data.SqlClient;
using Renci.SshNet;
using System.Net.Http;
using FluentFTP;
using Microsoft.AnalysisServices.AdomdClient;
using OfficeOpenXml;
using Microsoft.AspNetCore.Html;
using System.Diagnostics;
using MongoDB.Driver;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Net.Http.Json;

namespace Seal.Helpers
{
    public class RazorHelper
    {
        static readonly Object _0 = new Object();
        static HttpClient _1 = null;
        static HtmlString _2 = null;
        static DataTable _3 = null;
        static OleDbConnection _4 = null;
        static LdapConnection _5 = null;
        static SyndicationFeed _6 = null;
        static XDocument _7 = null;
#if WINDOWS
        static Control _8 = null;
#endif
        static PrincipalContext _9 = null;
        static JwtSettings _10 = null;
        static JObject _11 = null;
        static FastZip _12 = null;
        static OdbcConnection _13 = null;
        static SqlConnection _14 = null;
        static Microsoft.Data.SqlClient.SqlConnection _15 = null;
        static SftpClient _16 = null;
        static FtpClient _17 = null;
        static HttpClient _18 = null;
        static AdomdConnection _19 = null;
        static ExcelPackage _20 = null;
        static EventLogEntryType _21 = EventLogEntryType.Information;
        static MongoClient _22 = null;
        static string _23 = "";
        static JsonContent _24 = null;

        static int _loadTries = 3;
        static public void LoadRazorAssemblies()
        {
            if (_loadTries > 0)
            {
                _loadTries--;
                try
                {
                    //Load specific assemblies
                    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
                    var loadedPaths = loadedAssemblies.Where(a => !a.IsDynamic).Select(a => a.Location).ToArray();

                    var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
                    var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase)).ToList();

                    foreach (var path in toLoad)
                    {
                        //take only 
                        //Microsoft.AspNetCore.Http.* dlls
                        //MySql.Data.dll
                        var fileName = Path.GetFileName(path).ToLower();
                        if (
                            fileName.StartsWith("microsoft.aspnetcore.http.") ||
                            fileName.StartsWith("mysql.data.")
                           )
                        {
                            try
                            {
                                loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)));
                            }
                            catch (Exception ex)
                            {
                                Helper.WriteLogException("LoadRazorAssemblies", ex);
                            }
                        }
                    }

                    if (_1 == null) _1 = new HttpClient();
                    if (_2 == null) _2 = new HtmlString("");
                    if (_3 == null) _3 = new DataTable();
                    if (_4 == null) _4 = new OleDbConnection();
                    if (_5 == null) _5 = new LdapConnection("");
                    if (_6 == null) _6 = new SyndicationFeed();
                    if (_7 == null) _7 = new XDocument();
#if WINDOWS
                    if (_8 == null) _8 = new Control();
#endif
                    if (_9 == null) _9 = new PrincipalContext(ContextType.Machine);
                    if (_10 == null) _10 = JWT.DefaultSettings;
                    if (_11 == null) _11 = JObject.Parse("{}");
                    if (_12 == null) _12 = new FastZip();
                    if (_13 == null) _13 = new OdbcConnection();
                    if (_14 == null) _14 = new SqlConnection();
                    if (_15 == null) _15 = new Microsoft.Data.SqlClient.SqlConnection();
                    if (_16 == null) _16 = new SftpClient("", "a", "");
                    if (_17 == null) _17 = new FtpClient();
                    if (_18 == null) _18 = new HttpClient();
                    if (_19 == null) _19 = new AdomdConnection();
                    if (_20 == null) _20 = new ExcelPackage();
                    if (_22 == null) _22 = new MongoClient();
                    if (_23 == null) _23 = HttpUtility.HtmlEncode("");
                    if (_24 == null) _24 = JsonContent.Create(new { });
                }
                catch (Exception ex)
                {
                    Helper.WriteLogException("LoadRazorAssemblies", ex);
                }
                finally
                {
                    _loadTries = 0;
                }
            }
        }

        static public string GetFullScript(string script, object model, string header = null)
        {
            var result = (script == null ? "" : script);
            Report report = null;
            SealServerConfiguration configuration = null;
            if (model is SealServerConfiguration)
            {
                configuration = (SealServerConfiguration)model;
            }
            else if (model is Report)
            {
                var ob = (Report)model;
                report = ob;
                if (ob.Repository != null) configuration = ob.Repository.Configuration;
                else if (ob.Tag != null && ob.Tag is SealServerConfiguration) configuration = (SealServerConfiguration)ob.Tag;
            }
            else if (model is ReportComponent)
            {
                var ob = (ReportComponent)model;
                if (ob.Report != null)
                {
                    report = ob.Report;
                    if (report.Repository != null) configuration = report.Repository.Configuration;
                    else if (report.Tag != null && report.Tag is SealServerConfiguration) configuration = (SealServerConfiguration)ob.Report.Tag;
                }
            }
            else if (model is NavigationLink)
            {
                var ob = (NavigationLink)model;
                if (ob.Report != null)
                {
                    report = ob.Report;
                    if (report.Repository != null) configuration = report.Repository.Configuration;
                }
            }
            else if (model is ResultCell)
            {
                var ob = (ResultCell)model;
                report = ob.Element.Report;
                if (report.Repository != null) configuration = report.Repository.Configuration;
            }
            else if (model is MetaEnum)
            {
                var ob = (MetaEnum)model;
                if (ob.Source != null)
                {
                    report = ob.Source.Report;
                    configuration = ob.Source.Repository.Configuration;
                }
            }
            else if (model is MetaTable)
            {
                var ob = (MetaTable)model;
                if (ob.Source != null)
                {
                    report = ob.Source.Report;
                    configuration = ob.Source.Repository.Configuration;
                }
            }
            else if (model is MetaConnection)
            {
                var ob = (MetaConnection)model;
                if (ob.Source != null)
                {
                    report = ob.Source.Report;
                    configuration = ob.Source.Repository.Configuration;
                }
            }
            else if (model is SealExcelConverter)
            {
                var ob = (SealExcelConverter)model;
                report = ob.GetReport();
                configuration = (report == null ? Repository.Instance.Configuration : report.Repository.Configuration);
            }
            else if (model is SealPdfConverter)
            {
                var ob = (SealPdfConverter)model;
                report = ob.GetReport();
                configuration = (report == null ? Repository.Instance.Configuration : report.Repository.Configuration);
            }

            if (!string.IsNullOrEmpty(header)) result += "\r\n" + header;

            if (report != null && header == null)
            {
                if (!string.IsNullOrEmpty(report.CommonScriptsHeader)) result = result + "\r\n" + report.CommonScriptsHeader;
            }

            if (configuration != null)
            {
                result = configuration.SetConfigurationCommonScripts(result);
            }

            if (!string.IsNullOrEmpty(result))
            {
                //Add default using
                if (!result.Contains("@using Seal.Model")) result += "\r\n@using Seal.Model\r\n";
                if (!result.Contains("@using Seal.Helpers")) result += "\r\n@using Seal.Helpers\r\n";
            }
            return result;
        }

        static public string CompileExecute(string script, object model, string key = null)
        {
            if (model != null && script != null && script.Trim().StartsWith("@"))
            {
                if (string.IsNullOrEmpty(key))
                {
                    if (model != null)
                    {
                        key = model.GetType().ToString() + "_" + GetFullScript(script, model);
                    }
                    else
                    {
                        key = script;
                    }
                }

                if (!(Engine.Razor.IsTemplateCached(key, model.GetType())))
                {
                    Compile(GetFullScript(script, model), model.GetType(), key);
                }
                string result = Engine.Razor.Run(key, model.GetType(), model);
                return string.IsNullOrEmpty(result) ? "" : result;
            }
            return script;
        }

        static object lockObject = new object();
        static public void Compile(string script, Type modelType, string key)
        {
            lock (lockObject)
            {
                if (!string.IsNullOrEmpty(script) && !Engine.Razor.IsTemplateCached(key, modelType))
                {
                    LoadRazorAssemblies();
                    Engine.Razor.Compile(script, key, modelType);
                }
            }
        }
    }
}
