//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
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
using Oracle.ManagedDataAccess.Client;
using System.IdentityModel.Tokens.Jwt;
using System.DirectoryServices;
using Microsoft.Web.Administration;
using System.Security.AccessControl;
using Twilio.Rest.Api.V2010.Account;
using System.Net;
using System.Drawing;
using ScottPlot;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using QuestPDF.Infrastructure;
using Svg.Skia;
using PuppeteerSharp;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System.Runtime.Loader;
using System.Threading;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

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
        static System.Windows.Forms.Control _8 = null;
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
        static EventLogEntryType _21 = EventLogEntryType.Information; //Necessary to compile Security Scripts
        static MongoClient _22 = null;
        static string _23 = "";
        static JsonContent _24 = null;
        static OracleConnection _25 = null;
        static JwtSecurityTokenHandler _26 = null;
        static DirectoryEntry _27 = null;
        static ServerManager _28 = null;
        static FileSystemAccessRule _29 = null;
        static MessageResource.ScheduleTypeEnum _30 = null;
        static WebProxy _31 = null;
        static System.Drawing.Color? _32 = null;
        static Plot _33 = null;
        static Workbook _34 = null;
        static SpreadsheetDocument _35 = null;
        static LicenseType _36 = LicenseType.Community;
        static SKSvg _37 = null;
        static PdfOptions _38 = null;
        static AngleSharp.IConfiguration _39 = null;

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
                        //Force load
                        //Microsoft.AspNetCore.Http.* dlls
                        //MySql.Data.
                        var fileName = Path.GetFileName(path).ToLower();
                        if (
                            fileName.StartsWith("microsoft.aspnetcore.http.") ||
                            fileName.StartsWith("mysql.data.") ||
                            fileName.StartsWith("microsoft.bcl.asyncinterfaces")  //For IAsyncEnumerable                          
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
                    if (_8 == null) _8 = new System.Windows.Forms.Control();
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
                    if (_25 == null) _25 = new OracleConnection("");
                    if (_26 == null) _26 = new JwtSecurityTokenHandler();
                    if (_27 == null) _27 = new DirectoryEntry();
                    if (_28 == null) _28 = new ServerManager();
                    if (_29 == null) _29 = new FileSystemAccessRule("a", FileSystemRights.Read, AccessControlType.Deny);
                    if (_30 == null) _30 = new MessageResource.ScheduleTypeEnum();
                    if (_31 == null) _31 = new WebProxy();
                    if (_32 == null) _32 = ColorTranslator.FromHtml("#00000");
                    if (_33 == null) _33 = new Plot();
                    if (_34 == null) _34 = new Workbook();
                    if (_35 == null) _35 = SpreadsheetDocument.Create(FileHelper.GetTempUniqueFileName("dummy.xlsx"), SpreadsheetDocumentType.Workbook);
                    if (_37 == null) _37 = new SKSvg();
                    if (_38 == null) _38 = new PdfOptions();
                    if (_39 == null) _39 = AngleSharp.Configuration.Default;
#if !WINDOWS
                    var si = SealInterface.Create();
                    si.Init();
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

        static public string CompileExecute(string script, object model, string key = null)
        {
            if (model != null && script != null && script.Trim().StartsWith("@"))
            {
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

                if (!(Engine.Razor.IsTemplateCached(key, model.GetType())))
                {
                    Compile(GetFullScript(script), model.GetType(), key);
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
