//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

// Compatibility shims kept after the legacy Antaris RazorEngine fork was removed (RazorEngineCore is the
// engine now, see RazorCoreEngine). Only the few public types still referenced by Seal and by user report
// scripts are reproduced here, in their original namespaces, so existing catch sites and reports keep working:
//   - RazorEngine.Engine.AlternateTemporaryDirectory (set in Repository.Init)
//   - RazorEngine.Templating.TemplateCompilationException / RazorEngineCompilerError
//   - RazorEngine.Compilation.CompilationData
// RazorCoreEngine throws TemplateCompilationException so Helper.GetExceptionMessage, ReportView,
// SecurityProvider, FormHelper, ReportModel, ReportExecution, etc. behave as before.

namespace RazorEngine
{
    /// <summary>Compatibility shim for the dropped RazorEngine fork.</summary>
    public static class Engine
    {
        /// <summary>
        /// No-op kept for backward compatibility: the legacy engine used this as its temporary compile
        /// directory. RazorEngineCore compiles in memory, so the value is unused.
        /// </summary>
        public static string AlternateTemporaryDirectory { get; set; }
    }
}

namespace RazorEngine.Compilation
{
    /// <summary>Compatibility shim: carries the generated source code for a compilation error.</summary>
    public class CompilationData
    {
        public CompilationData(string sourceCode, string tmpFolder)
        {
            SourceCode = sourceCode;
            TmpFolder = tmpFolder;
        }

        /// <summary>The generated C# source code (used by the designer to map errors to lines).</summary>
        public string SourceCode { get; }

        public string TmpFolder { get; }
    }
}

namespace RazorEngine.Templating
{
    /// <summary>
    /// Compatibility shim for a Razor parsing error. RazorEngineCore surfaces parse errors as
    /// TemplateCompilationException, so this is no longer thrown, but the type is kept so existing
    /// catch sites (e.g. the designer's FormHelper) still compile.
    /// </summary>
    public class TemplateParsingException : Exception
    {
        public TemplateParsingException(string errorMessage, int lineIndex, int characterIndex)
            : base(string.Format("({0}:{1}) - {2}", lineIndex, characterIndex, errorMessage))
        {
            Line = lineIndex;
            Column = characterIndex;
        }

        public int Column { get; }
        public int Line { get; }
    }

    /// <summary>Compatibility shim describing a single compiler error.</summary>
    public class RazorEngineCompilerError
    {
        public RazorEngineCompilerError(string errorText, string fileName, int line, int column, string errorNumber, bool isWarning)
        {
            ErrorText = errorText;
            FileName = fileName;
            Line = line;
            Column = column;
            ErrorNumber = errorNumber;
            IsWarning = isWarning;
        }

        public string ErrorText { get; }
        public string FileName { get; }
        public int Line { get; }
        public int Column { get; }
        public string ErrorNumber { get; }
        public bool IsWarning { get; }
    }

    /// <summary>
    /// Compatibility shim raised when a Razor script fails to compile. Kept in the original namespace so
    /// existing catch sites and user report scripts (e.g. TST020) keep working after the fork was dropped.
    /// </summary>
    public class TemplateCompilationException : Exception
    {
        public TemplateCompilationException(IEnumerable<RazorEngineCompilerError> errors, RazorEngine.Compilation.CompilationData files, object template = null)
            : base(BuildMessage(errors))
        {
            CompilerErrors = new ReadOnlyCollection<RazorEngineCompilerError>((errors ?? Enumerable.Empty<RazorEngineCompilerError>()).ToList());
            CompilationData = files;
        }

        public ReadOnlyCollection<RazorEngineCompilerError> CompilerErrors { get; }

        public RazorEngine.Compilation.CompilationData CompilationData { get; }

        static string BuildMessage(IEnumerable<RazorEngineCompilerError> errors)
        {
            var first = errors?.FirstOrDefault();
            return first != null ? "Unable to compile template: " + first.ErrorText : "Unable to compile template.";
        }
    }
}
