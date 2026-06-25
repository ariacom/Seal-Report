//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using RazorEngineCore;

namespace Seal.Helpers
{
    /// <summary>
    /// Marker for content that must be emitted RAW (not HTML-encoded), mirroring the fork's IEncodedString/RawString.
    /// Produced by SealCoreTemplateBase.Raw(...) and .Include(...).
    /// </summary>
    public sealed class RawContent
    {
        public readonly string Value;
        public RawContent(string v) { Value = v ?? ""; }
        public override string ToString() => Value;
    }

    /// <summary>
    /// Base template every Seal Razor template/partial compiles against when RazorHelper.UseRazorCore is on.
    /// Reproduces the Antaris fork contract used by Seal templates:
    ///  - bare @expr is HTML-encoded by default; @Raw(...) is emitted raw
    ///  - @Include(key, model) resolves a previously-compiled partial BY KEY and emits it raw (nestable)
    ///  - a dynamic ViewBag shared down the include chain
    /// </summary>
    public class SealCoreTemplateBase : RazorEngineTemplateBase
    {
        /// <summary>Dynamic state bag, shared with included partials (fork parity).</summary>
        public dynamic ViewBag { get; set; }

        /// <summary>@expr -> HTML-encode unless it is already raw content.</summary>
        public override void Write(object obj)
        {
            if (obj is RawContent rc) base.WriteLiteral(rc.Value);
            else base.WriteLiteral(WebUtility.HtmlEncode(obj?.ToString() ?? ""));
        }

        /// <summary>@Raw(...) -> emit as-is.</summary>
        public RawContent Raw(object value) => new RawContent(value?.ToString() ?? "");

        /// <summary>@Include(key, model) -> run the partial cached under 'name' and emit its output raw.</summary>
        public RawContent Include(string name, object model = null, Type modelType = null)
            => new RawContent(RazorCoreEngine.Run(name, model, ViewBag));
    }

    /// <summary>
    /// RazorEngineCore-backed replacement for the bits of the Antaris fork that Seal uses:
    /// a string-keyed compiled-template cache with optional on-disk persistence (RazorCacheDirectory).
    /// Reached only via RazorHelper when UseRazorCore is enabled.
    /// </summary>
    public static class RazorCoreEngine
    {
        static readonly IRazorEngine _engine = new RazorEngineCore.RazorEngine();
        static readonly ConcurrentDictionary<string, IRazorEngineCompiledTemplate<SealCoreTemplateBase>> _cache = new();
        static readonly object _lock = new object();

        /// <summary>
        /// Version stamp folded into every persisted cache file name so a compiled assembly built
        /// against an older base template / helper API is never reused after a Seal upgrade.
        /// </summary>
        static readonly string _engineVersion =
            typeof(SealCoreTemplateBase).Assembly.GetName().Version?.ToString() ?? "0";

        public static bool IsTemplateCached(string key) => !string.IsNullOrEmpty(key) && _cache.ContainsKey(key);

        /// <summary>
        /// Run a RazorEngineCore call with no ambient SynchronizationContext. RazorEngineCore's synchronous
        /// Compile/SaveToFile/LoadFromFile/Run wrap async work with GetAwaiter().GetResult(); on a UI thread
        /// (WinForms designer) the captured context would deadlock. Clearing it lets continuations run inline.
        /// </summary>
        static T NoSyncContext<T>(Func<T> func)
        {
            var ctx = SynchronizationContext.Current;
            if (ctx == null) return func();
            SynchronizationContext.SetSynchronizationContext(null);
            try { return func(); }
            finally { SynchronizationContext.SetSynchronizationContext(ctx); }
        }

        /// <summary>
        /// Stable, filesystem-safe file name BOUND TO THE SCRIPT CONTENT (not the logical cache key).
        /// Binding the name to the content (plus the engine version) means any change to the script
        /// yields a different file, so a stale or mismatched persisted assembly is never silently
        /// trusted, and a DLL compiled for one script can never be reused for a different script that
        /// happens to share a key. (The repository 'Assemblies' tree — including this RazorCache
        /// folder — is loaded as in-process code and must be writable only by the service account.)
        /// </summary>
        static string CacheFileName(string script)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(_engineVersion + "\0" + (script ?? "")));
            return Convert.ToHexString(hash) + ".dll";
        }

        /// <summary>
        /// Compile 'script' under 'key' (no-op if already cached). When cacheDir is set, a persisted
        /// assembly is reused (and invalidated by lastModification), mirroring GetGlobalAssemblyCache/SaveAssemblyInCache.
        /// </summary>
        public static void Compile(string script, string key, string cacheDir, DateTime? lastModification)
        {
            if (string.IsNullOrEmpty(script) || string.IsNullOrEmpty(key) || _cache.ContainsKey(key)) return;

            lock (_lock)
            {
                if (_cache.ContainsKey(key)) return;

                string dll = string.IsNullOrEmpty(cacheDir) ? null : Path.Combine(cacheDir, CacheFileName(script));

                // 1) reuse a persisted assembly if present and not stale
                if (dll != null && File.Exists(dll))
                {
                    if (lastModification != null && lastModification.Value > File.GetLastWriteTime(dll))
                    {
                        try { File.Delete(dll); } catch { }
                    }
                    else
                    {
                        try
                        {
                            _cache[key] = NoSyncContext(() => RazorEngineCompiledTemplate<SealCoreTemplateBase>.LoadFromFile(dll));
                            return;
                        }
                        catch { /* fall through to recompile */ }
                    }
                }

                // 2) compile fresh, referencing every currently loaded assembly (== UseCurrentAssembliesReferenceResolver)
                RazorHelper.LoadRazorAssemblies();
                IRazorEngineCompiledTemplate<SealCoreTemplateBase> compiled;
                try
                {
                    compiled = NoSyncContext(() => _engine.Compile<SealCoreTemplateBase>(script, b =>
                    {
                        // default namespaces (parity with the fork's TemplateServiceConfiguration)
                        b.AddUsing("System");
                        b.AddUsing("System.Collections.Generic");
                        b.AddUsing("System.Linq");
                        b.AddUsing("System.Threading.Tasks");

                        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (a.IsDynamic) continue;
                            string loc; try { loc = a.Location; } catch { continue; }
                            if (string.IsNullOrEmpty(loc) || !seen.Add(loc)) continue;
                            try { b.AddAssemblyReference(a); } catch { }
                        }
                    }));
                }
                catch (RazorEngineCompilationException ex)
                {
                    throw ToTemplateCompilationException(ex, script);
                }

                _cache[key] = compiled;

                // 3) persist for next time
                if (dll != null)
                {
                    try { Directory.CreateDirectory(cacheDir); NoSyncContext<object>(() => { compiled.SaveToFile(dll); return null; }); } catch { }
                }
            }
        }

        public static string Run(string key, object model, object viewBag = null)
        {
            if (!_cache.TryGetValue(key, out var t))
                throw new ArgumentException($"No template could be resolved with name '{key}'. The partial must be compiled before it is included.");
            var result = NoSyncContext(() => t.Run(i => { i.Model = model; i.ViewBag = viewBag; }));
            return result ?? "";
        }

        /// <summary>
        /// Map a RazorEngineCore compilation failure onto the fork's TemplateCompilationException so that
        /// every existing catch site (Helper.GetExceptionMessage, ReportView, SecurityProvider, user scripts, ...) keeps working.
        /// </summary>
        static Exception ToTemplateCompilationException(RazorEngineCompilationException ex, string script)
        {
            try
            {
                var errors = (ex.Errors ?? new List<Microsoft.CodeAnalysis.Diagnostic>())
                    .Select(d =>
                    {
                        var span = d.Location.GetLineSpan();
                        return new RazorEngine.Templating.RazorEngineCompilerError(
                            d.GetMessage(),
                            span.Path ?? "",
                            span.StartLinePosition.Line + 1,
                            span.StartLinePosition.Character + 1,
                            d.Id,
                            d.Severity != Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
                    })
                    .ToList();
                if (errors.Count == 0)
                    errors.Add(new RazorEngine.Templating.RazorEngineCompilerError(ex.Message, "", 0, 0, "", false));

                return new RazorEngine.Templating.TemplateCompilationException(
                    errors,
                    new RazorEngine.Compilation.CompilationData(ex.GeneratedCode, null),
                    null);
            }
            catch
            {
                return ex; // worst case: surface the original
            }
        }
    }
}
