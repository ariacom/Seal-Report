//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using RazorEngine;
using RazorEngine.Templating;
using System.Data;
#if WINDOWS
using System.Windows.Forms;
#endif
using MongoDB.Driver;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using System.Net.Http;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using FluentFTP;
using ICSharpCode.SharpZipLib.Zip;
using Jose;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AspNetCore.Html;
using Microsoft.Web.Administration;
using Newtonsoft.Json.Linq;
using Npgsql;
using OfficeOpenXml;
using Oracle.ManagedDataAccess.Client;
using PuppeteerSharp;
using Renci.SshNet;
using ScottPlot;
using System.Data.Odbc;
using System.Data.OleDb;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.AccessControl;
using System.ServiceModel.Syndication;
using System.Web;
using System.Xml.Linq;
using System.Data.SqlClient;
using System.DirectoryServices;
using System.Net;
using Twilio.Rest.Api.V2010.Account;
using Svg.Skia;
using System.Diagnostics;
using Seal.Model;

namespace Seal.Helpers
{
    public class RazorHelper
    {
        //Directory location for cached assemblies
        public static string RazorCacheDirectory = "";

        static EventLogEntryType _01 = EventLogEntryType.Information; //Necessary to compile Security Scripts
        static int _loadTries = 3;
        /// <summary>
        /// Force the load of the assemblies
        /// </summary>
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
                        var fileName = Path.GetFileName(path);
                        //Skip Seal and Microsoft.Extensions
                        if (fileName.StartsWith(Repository.SealRootProductName) ||
                            fileName.StartsWith("Microsoft.Extensions")
                            ) continue;

                        //Force load of all assemblies available for dynamic Scripts
                        try
                        {
                            loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)));
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is FileNotFoundException)) Helper.WriteLogException($"LoadRazorAssemblies for '{path}'", ex);
                        }
                    }

                    //Force other load (required to compile Razor scripts)
                    _ = new HttpClient();
                    _ = new HtmlString("");
                    _ = new DataTable();
                    _ = new OleDbConnection();
                    _ = new LdapConnection("");
                    _ = new SyndicationFeed();
                    _ = new XDocument();
                    _ = new PrincipalContext(ContextType.Machine);
                    _ = JWT.DefaultSettings;
                    _ = JObject.Parse("{}");
                    _ = new FastZip();
                    _ = new OdbcConnection();
                    _ = new SqlConnection();
                    _ = new Microsoft.Data.SqlClient.SqlConnection();
                    _ = new SftpClient("", "a", "");
                    _ = new FtpClient();
                    _ = new AdomdConnection();
                    _ = new ExcelPackage();
                    _ = new MongoClient();
                    _ = HttpUtility.HtmlEncode("");
                    _ = JsonContent.Create(new { });
                    _ = new OracleConnection("");
                    _ = new JwtSecurityTokenHandler();
                    _ = new DirectoryEntry();
                    _ = new ServerManager();
                    _ = new FileSystemAccessRule("a", FileSystemRights.Read, AccessControlType.Deny);
                    _ = new MessageResource.ScheduleTypeEnum();
                    _ = new WebProxy();
                    _ = ColorTranslator.FromHtml("#00000");
                    _ = new Plot();
                    _ = new Workbook();
                    _ = SpreadsheetDocument.Create(FileHelper.GetTempUniqueFileName("dummy.xlsx"), SpreadsheetDocumentType.Workbook);
                    _ = new SKSvg();
                    _ = new PdfOptions();
                    _ = new NpgsqlConnection("");
                    _ = AngleSharp.Configuration.Default;
#if WINDOWS
                    _ = new System.Windows.Forms.Control();
#endif
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

        static public string GetFullScript(string script)
        {
            var result = (script == null ? "" : script);
            if (!string.IsNullOrEmpty(result))
            {
                //Add default using
                if (!result.Contains("@using Seal.Model")) result += "\r\n@using Seal.Model\r\n";
                if (!result.Contains("@using Seal.Helpers")) result += "\r\n@using Seal.Helpers\r\n";
            }
            return result;
        }

        static public string GetGlobalAssemblyCache(string key, DateTime lastModification)
        {
            //Find a global assembly for this key
            var result = "";
            if (!string.IsNullOrEmpty(RazorCacheDirectory))
            {
                foreach (var f in Directory.GetFiles(RazorCacheDirectory, key + "*.dll"))
                {
                    if (lastModification > File.GetLastWriteTime(f))
                    {
                        try
                        {
                            File.Delete(f);
                        }
                        catch { }
                    }
                    else
                    {
                        result = f;
                    }
                }
            }
            return result;
        }


        static public bool GetFinalKey(string script, object model, ref string key, DateTime? lastModification)
        {
            bool saveAssemblyCache = false;
            if (!string.IsNullOrEmpty(key) && lastModification != null)
            {
                //Set the dll path in the keyName if exists
                key = GetGlobalAssemblyCache(key, lastModification.Value);
                saveAssemblyCache = string.IsNullOrEmpty(key);
            }

            if (string.IsNullOrEmpty(key))
            {
                if (model != null)
                {
                    key = model.GetType().ToString() + "_" + GetFullScript(script);
                }
                else
                {
                    key = script;
                }
            }

            return saveAssemblyCache;
        }

        static public void SaveAssemblyInCache(string initialKey, object model, string key)
        {
            //Save the dll in global cache
            var template = Engine.Razor.GetTemplate(key, model.GetType());
            if (template != null)
            {
                try
                {
                    var dll = template.TemplateAssembly.Location;
                    var className = Path.GetFileNameWithoutExtension(dll).Split("_").Last();
                    if (File.Exists(dll))
                    {
                        File.Copy(dll, Path.Combine(RazorCacheDirectory, initialKey + "_" + className + ".dll"));
                    }
                }
                catch (Exception ex)
                {
                    Helper.WriteLogException("CompileExecute", ex);
                }
            }

        }


        static public string CompileExecute(string script, object model, string key = null, DateTime? lastModification = null)
        {
            if (model != null && script != null && script.Trim().StartsWith("@"))
            {
                var initialKey = key;
                bool saveAssemblyCache = GetFinalKey(script, model, ref key, lastModification);

                if (!(Engine.Razor.IsTemplateCached(key, model.GetType())))
                {
                    Compile(GetFullScript(script), model.GetType(), key);

                    if (!string.IsNullOrEmpty(RazorCacheDirectory) && saveAssemblyCache)
                    {
                        SaveAssemblyInCache(initialKey, model, key);
                    }
                }

                string result = Engine.Razor.Run(key, model.GetType(), model);
                return string.IsNullOrEmpty(result) ? "" : result;
            }
            return script;
        }

        static object lockObject = new object();
        static public void Compile(string script, Type modelType, string key)
        {
            if (Validator != null && !Validator.CheckScript(script))
            {
                var ex = new Exception("Invalid script detected.");
                Helper.WriteLogException("Compile", ex);
                throw ex;
            }

            lock (lockObject)
            {
                if (!string.IsNullOrEmpty(script) && !Engine.Razor.IsTemplateCached(key, modelType))
                {
                    LoadRazorAssemblies();
                    Engine.Razor.Compile(script, key, modelType);
                }
            }
        }

        static public string CompilePartial(string script, object model, string key, DateTime? lastModification)
        {
            if (model != null && script != null && script.Trim().StartsWith("@"))
            {
                var initialKey = key;
                bool saveAssemblyCache = GetFinalKey(script, model, ref key, lastModification);
                if (!(Engine.Razor.IsTemplateCached(key, model.GetType())))
                {
                    Compile(script, model.GetType(), key);

                    if (saveAssemblyCache)
                    {
                        SaveAssemblyInCache(initialKey, model, key);
                    }
                }
            }

            return key;
        }

        static public ScriptValidator Validator = null;
    }

    /// <summary>
    /// Optional script validation before compilation
    /// </summary>
    public class ScriptValidator
    {
        /// <summary>
        /// Check is the script is valid, may be used to control/limit the scripts executed
        /// </summary>
        public virtual bool CheckScript(string script)
        {
            return true;
        }

    }
}
