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

        public static bool IsTemplateCached(string key) => !string.IsNullOrEmpty(key) && _cache.ContainsKey(key);

        /// <summary>Stable, filesystem-safe file name for a (possibly path-like) cache key.</summary>
        static string CacheFileName(string key)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
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

                string dll = string.IsNullOrEmpty(cacheDir) ? null : Path.Combine(cacheDir, CacheFileName(key));

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
                            _cache[key] = RazorEngineCompiledTemplate<SealCoreTemplateBase>.LoadFromFile(dll);
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
                    compiled = _engine.Compile<SealCoreTemplateBase>(script, b =>
                    {
                        // default namespaces (parity with the fork's TemplateServiceConfiguration)
                        b.AddUsing("System");
                        b.AddUsing("System.Collections.Generic");
                        b.AddUsing("System.Linq");

                        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (a.IsDynamic) continue;
                            string loc; try { loc = a.Location; } catch { continue; }
                            if (string.IsNullOrEmpty(loc) || !seen.Add(loc)) continue;
                            try { b.AddAssemblyReference(a); } catch { }
                        }
                    });
                }
                catch (RazorEngineCompilationException ex)
                {
                    throw ToTemplateCompilationException(ex, script);
                }

                _cache[key] = compiled;

                // 3) persist for next time
                if (dll != null)
                {
                    try { Directory.CreateDirectory(cacheDir); compiled.SaveToFile(dll); } catch { }
                }
            }
        }

        public static string Run(string key, object model, object viewBag = null)
        {
            if (!_cache.TryGetValue(key, out var t))
                throw new ArgumentException($"No template could be resolved with name '{key}'. The partial must be compiled before it is included.");
            var result = t.Run(i => { i.Model = model; i.ViewBag = viewBag; });
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
