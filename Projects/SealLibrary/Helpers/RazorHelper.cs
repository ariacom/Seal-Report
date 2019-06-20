//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
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
using RazorEngine.Configuration;

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
        static Control dummy7 = null;

        static bool _loadDone = false;
        static public void LoadRazorAssemblies()
        {
            if (!_loadDone)
            {
                //Force the load of the assemblies
                if (dummy == null) dummy = new HtmlString("");
                if (dummy2 == null) dummy2 = new DataTable();
                if (dummy3 == null) dummy3 = new OleDbConnection();
                if (dummy4 == null) dummy4 = new LdapConnection("");
                if (dummy5 == null) dummy5 = new SyndicationFeed();
                if (dummy6 == null) dummy6 = new XDocument();
                if (dummy7 == null) dummy7 = new Control(); 
                _loadDone = true;
            }
        }


        static public string GetScriptHeader(object model)
        {
            var result = "";
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
                else if (ob.Tag != null && ob.Tag is SealServerConfiguration) configuration = (SealServerConfiguration) ob.Tag;
            }
            else if (model is ReportComponent)
            {
                var ob = (ReportComponent)model;
                if (ob.Report != null)
                {
                    report = ob.Report;
                    if (ob.Report.Repository != null) configuration = ob.Report.Repository.Configuration;
                    else if (ob.Report.Tag != null && ob.Report.Tag is SealServerConfiguration) configuration = (SealServerConfiguration)ob.Report.Tag;
                }
            }
            else if (model is MetaEnum)
            {
                var ob = (MetaEnum)model;
                report = ob.Source.Report;
                configuration = ob.Source.Repository.Configuration;
            }
            else if (model is MetaTable)
            {
                var ob = (MetaTable)model;
                report = ob.Source.Report;
                configuration = ob.Source.Repository.Configuration;
            }
            else if (model is MetaConnection)
            {
                var ob = (MetaConnection)model;
                report = ob.Source.Report;
                configuration = ob.Source.Repository.Configuration;
            }

            if (configuration != null)
            {
                if (!string.IsNullOrEmpty(configuration.CommonScriptsHeader)) result += configuration.CommonScriptsHeader + "\r\n";
                if (model is ReportTask && !string.IsNullOrEmpty(configuration.TasksScript)) result += configuration.TasksScript + "\r\n";
            }

            if (report != null)
            {
                if (!string.IsNullOrEmpty(report.CommonScriptsHeader)) result += report.CommonScriptsHeader + "\r\n";
                if (model is ReportTask && !string.IsNullOrEmpty(report.TasksScript)) result += report.TasksScript + "\r\n";
            }
            return result;
        }

        static public string CompileExecute(string script, object model, string key = null)
        {
            if (model != null && script != null && script.StartsWith("@"))
            {
                string result = "";
                LoadRazorAssemblies();
                if (string.IsNullOrEmpty(key))
                {
                    if (model != null) {
                        key = model.GetType().ToString() + "_" + GetScriptHeader(model) +"_" + script;
                    }
                    else
                    {
                        key = script;
                    }
                }
                if (Engine.Razor.IsTemplateCached(key, model.GetType()))
                {
                    result = Engine.Razor.Run(key, model.GetType(), model);
                }
                else
                {
                    result = Engine.Razor.RunCompile(GetScriptHeader(model) + script, key, model.GetType(), model);
                }
                return string.IsNullOrEmpty(result) ? "" : result;
            }
            return script;
        }

        static public void Compile(string script, Type modelType, string key)
        {
            if (!string.IsNullOrEmpty(script) && !Engine.Razor.IsTemplateCached(key, modelType))
            {
                LoadRazorAssemblies();
                Engine.Razor.Compile(script, key, modelType);
            }
        }
    }
}
