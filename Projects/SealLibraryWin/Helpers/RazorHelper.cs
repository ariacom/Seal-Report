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
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Seal.Helpers
{
    public class RazorHelper
    {
        static readonly Object _0 = new Object();
        static readonly HttpClient _1 = new HttpClient();
        static readonly HtmlString _2 = new HtmlString("");
        static readonly DataTable _3 = new DataTable();
        static readonly OleDbConnection _4 = new OleDbConnection();
        static readonly LdapConnection _5 = new LdapConnection("");
        static readonly SyndicationFeed _6 = new SyndicationFeed();
        static readonly XDocument _7 = new XDocument();
#if WINDOWS
        static readonly Control _8 = new Control();
#endif
        static readonly PrincipalContext _9 = new PrincipalContext(ContextType.Machine);
        static readonly JwtSettings _10 = JWT.DefaultSettings;
        static readonly JObject _11 = JObject.Parse("{}");
        static readonly FastZip _12 = new FastZip();
        static readonly OdbcConnection _13 = new OdbcConnection();
        static readonly SqlConnection _14 = new SqlConnection();
        static readonly Microsoft.Data.SqlClient.SqlConnection _15 = new Microsoft.Data.SqlClient.SqlConnection();
        static readonly SftpClient _16 = new SftpClient("", "a", "");
        static readonly FtpClient _17 = new FtpClient();
        static readonly HttpClient _18 = new HttpClient();
        static readonly AdomdConnection _19 = new AdomdConnection();
        static readonly ExcelPackage _20 = new ExcelPackage();
        static readonly EventLogEntryType _21 = EventLogEntryType.Information;
        static readonly MongoClient _22 = new MongoClient();

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
                        //take only Microsoft.AspNetCore.Http. dlls
                        if (!Path.GetFileName(path).ToLower().StartsWith("microsoft.aspnetcore.http.")) continue;

                        try
                        {
                            loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                _loadTries = 0;
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
