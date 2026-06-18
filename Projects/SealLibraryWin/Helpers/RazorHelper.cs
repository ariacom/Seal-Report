//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
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
using System.Data.SQLite;
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
using Microsoft.Data.SqlClient;
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

        /// <summary>Logical cache key used by the RazorEngineCore backend.</summary>
        static string GetCoreKey(string script, object model, string key)
        {
            if (!string.IsNullOrEmpty(key)) return key;
            return model != null ? model.GetType().ToString() + "_" + GetFullScript(script) : script;
        }
        public static EventLogEntryType _01; //Necessary to compile Security Scripts
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
                    _ = new SQLiteConnection("");
                    _ = AngleSharp.Configuration.Default;
#if WINDOWS
                    _ = new System.Windows.Forms.Control();
#endif
                    _01 = EventLogEntryType.Warning;
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

        static public string CompileExecute(string script, object model, string key = null, DateTime? lastModification = null)
        {
            if (model != null && script != null && script.Trim().StartsWith("@"))
            {
                //Validate on every call, not only on (re)compilation: a script already in the
                //in-memory or on-disk cache must not be able to skip validation.
                if (Validator != null && !Validator.CheckScript(script)) throw InvalidScript();
                var coreKey = GetCoreKey(script, model, key);
                if (!RazorCoreEngine.IsTemplateCached(coreKey))
                {
                    RazorCoreEngine.Compile(GetFullScript(script), coreKey, RazorCacheDirectory, lastModification);
                }
                var result = RazorCoreEngine.Run(coreKey, model);
                return string.IsNullOrEmpty(result) ? "" : result;
            }
            return script;
        }

        static Exception InvalidScript()
        {
            var ex = new Exception("Invalid script detected.");
            Helper.WriteLogException("Compile", ex);
            return ex;
        }

        static public void Compile(string script, Type modelType, string key)
        {
            if (Validator != null && !Validator.CheckScript(script)) throw InvalidScript();
            if (!string.IsNullOrEmpty(script) && !RazorCoreEngine.IsTemplateCached(key))
                RazorCoreEngine.Compile(script, key, RazorCacheDirectory, null);
        }

        static public string CompilePartial(string script, object model, string key, DateTime? lastModification)
        {
            if (model != null && script != null && script.Trim().StartsWith("@"))
            {
                //Validate on every call (see CompileExecute): a cached script must not bypass validation.
                if (Validator != null && !Validator.CheckScript(script)) throw InvalidScript();
                var coreKey = GetCoreKey(script, model, key);
                if (!RazorCoreEngine.IsTemplateCached(coreKey))
                {
                    RazorCoreEngine.Compile(script, coreKey, RazorCacheDirectory, lastModification);
                }
                return coreKey;
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

    /// <summary>
    /// Optional, opt-in deny-list validator (defense-in-depth) wired by Repository.Init when
    /// SealServerConfiguration.EnableRazorScriptValidation is true. DISABLED by default because
    /// Seal scripts may legitimately use file/process/reflection APIs (e.g. report tasks).
    /// IMPORTANT: a substring deny-list on a Turing-complete language can be bypassed — this is
    /// only a speed-bump. The real security boundary remains "who is allowed to author/edit
    /// Razor scripts (reports, meta sources, security providers, dynamics)".
    /// </summary>
    public class DefaultScriptValidator : ScriptValidator
    {
        /// <summary>Conservative starter deny-list, used when no custom tokens are configured.</summary>
        public static readonly string[] DefaultForbiddenTokens = new[]
        {
            "System.Diagnostics.Process", "Process.Start",
            "System.Reflection.Emit", "Assembly.Load", "Assembly.LoadFrom", "Assembly.LoadFile",
            "System.Runtime.InteropServices", "DllImport", "Marshal.",
            "Microsoft.Win32.Registry", "Environment.Exit", "Environment.FailFast",
        };

        readonly string[] _forbidden;

        /// <param name="forbiddenTokens">Custom forbidden substrings; when null/empty the built-in starter list is used.</param>
        public DefaultScriptValidator(IEnumerable<string> forbiddenTokens = null)
        {
            var tokens = forbiddenTokens?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
            _forbidden = (tokens != null && tokens.Length > 0) ? tokens : DefaultForbiddenTokens;
        }

        /// <summary>Reject the script if it contains any forbidden token (case-insensitive).</summary>
        public override bool CheckScript(string script)
        {
            if (string.IsNullOrEmpty(script)) return true;
            foreach (var token in _forbidden)
            {
                if (script.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Helper.WriteLogEntry("Seal Razor", EventLogEntryType.Warning, $"A Razor script was rejected by the script validator (forbidden token '{token}').");
                    return false;
                }
            }
            return true;
        }
    }
}
