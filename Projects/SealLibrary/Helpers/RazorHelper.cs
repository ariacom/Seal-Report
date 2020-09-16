//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Data.OleDb;
using System.Web;
using RazorEngine;
using RazorEngine.Templating;
using System.Data;
using System.DirectoryServices.Protocols;
using System.Xml.Linq;
using System.ServiceModel.Syndication;
using System.Windows.Forms;
using Seal.Model;
using System.DirectoryServices.AccountManagement;
using Jose;
using Newtonsoft.Json.Linq;
using ICSharpCode.SharpZipLib.Zip;
using System.Data.Odbc;
using System.Data.SqlClient;
using Renci.SshNet;
using System.Net;
using System.Net.Http;
using FluentFTP;
using Microsoft.AnalysisServices.AdomdClient;
#if NETCOREAPP
    using Microsoft.AspNetCore.Html;
#endif

namespace Seal.Helpers
{
    public class RazorHelper
    {

        static HtmlString dummy = null;
        static DataTable dummy2 = null;
        static OleDbConnection dummy3 = null;
        static LdapConnection dummy4 = null;
        static SyndicationFeed dummy5 = null;
        static XDocument dummy6 = null;
        static Control dummy7 = null; //!NETCore
        static PrincipalContext dummy8 = null;
        static JwtSettings dummy9 = null; 
        static JObject dummy10 = null;
        static FastZip dummy11 = null;
        static OdbcConnection dummy12 = null;
        static SqlConnection dummy13 = null;
        static SftpClient dummy14 = null;
        static FtpClient dummy15 = null;
        static HttpClient dummy16 = null;
        static AdomdConnection dummy17 = null;

        static bool _loadDone = false;
        static public void LoadRazorAssemblies()
        {
            if (!_loadDone)
            {
                try
                {
                    //Force the load of the assemblies
                    if (dummy == null) dummy = new HtmlString("");
                    if (dummy2 == null)
                    {
                        dummy2 = new DataTable();
                        dummy2.AsEnumerable();
                    }
                    if (dummy3 == null) dummy3 = new OleDbConnection();
                    if (dummy4 == null) dummy4 = new LdapConnection("");
                    if (dummy5 == null) dummy5 = new SyndicationFeed();
                    if (dummy6 == null) dummy6 = new XDocument();
                    if (dummy7 == null) dummy7 = new Control(); //!NETCore
                    if (dummy8 == null) dummy8 = new PrincipalContext(ContextType.Machine);
                    if (dummy9 == null) dummy9 = JWT.DefaultSettings; 
                    if (dummy10 == null) dummy10 = JObject.Parse("{}");
                    if (dummy11 == null) dummy11 = new FastZip();
                    if (dummy12 == null) dummy12 = new OdbcConnection();
                    if (dummy13 == null) dummy13 = new SqlConnection();
                    if (dummy14 == null) dummy14 = new SftpClient("", "a", "");
                    if (dummy15 == null) dummy15 = new FtpClient();
                    if (dummy16 == null) dummy16 = new HttpClient();
                    if (dummy17 == null) dummy17 = new AdomdConnection();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                _loadDone = true;
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
