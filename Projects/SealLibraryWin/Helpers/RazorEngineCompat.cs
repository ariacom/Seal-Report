//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
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
        /// <summary>
        /// Constructor from the generated source code and the temporary folder
        /// </summary>
        public CompilationData(string sourceCode, string tmpFolder)
        {
            SourceCode = sourceCode;
            TmpFolder = tmpFolder;
        }

        /// <summary>The generated C# source code (used by the designer to map errors to lines).</summary>
        public string SourceCode { get; }

        /// <summary>
        /// Legacy temporary compilation folder (always null with RazorEngineCore, which compiles in memory)
        /// </summary>
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
        /// <summary>
        /// Constructor from the error message and its position in the template
        /// </summary>
        public TemplateParsingException(string errorMessage, int lineIndex, int characterIndex)
            : base(string.Format("({0}:{1}) - {2}", lineIndex, characterIndex, errorMessage))
        {
            Line = lineIndex;
            Column = characterIndex;
        }

        /// <summary>
        /// Column index of the parse error
        /// </summary>
        public int Column { get; }
        /// <summary>
        /// Line index of the parse error
        /// </summary>
        public int Line { get; }
    }

    /// <summary>Compatibility shim describing a single compiler error.</summary>
    public class RazorEngineCompilerError
    {
        /// <summary>
        /// Constructor from the compiler error details
        /// </summary>
        public RazorEngineCompilerError(string errorText, string fileName, int line, int column, string errorNumber, bool isWarning)
        {
            ErrorText = errorText;
            FileName = fileName;
            Line = line;
            Column = column;
            ErrorNumber = errorNumber;
            IsWarning = isWarning;
        }

        /// <summary>
        /// Compiler error message
        /// </summary>
        public string ErrorText { get; }
        /// <summary>
        /// File name of the error
        /// </summary>
        public string FileName { get; }
        /// <summary>
        /// Line number of the error
        /// </summary>
        public int Line { get; }
        /// <summary>
        /// Column number of the error
        /// </summary>
        public int Column { get; }
        /// <summary>
        /// Compiler error number (e.g. CS0103)
        /// </summary>
        public string ErrorNumber { get; }
        /// <summary>
        /// If true, the error is a warning
        /// </summary>
        public bool IsWarning { get; }
    }

    /// <summary>
    /// Compatibility shim raised when a Razor script fails to compile. Kept in the original namespace so
    /// existing catch sites and user report scripts (e.g. TST020) keep working after the fork was dropped.
    /// </summary>
    public class TemplateCompilationException : Exception
    {
        /// <summary>
        /// Constructor from the list of compiler errors and the compilation data
        /// </summary>
        public TemplateCompilationException(IEnumerable<RazorEngineCompilerError> errors, RazorEngine.Compilation.CompilationData files, object template = null)
            : base(BuildMessage(errors))
        {
            CompilerErrors = new ReadOnlyCollection<RazorEngineCompilerError>((errors ?? Enumerable.Empty<RazorEngineCompilerError>()).ToList());
            CompilationData = files;
        }

        /// <summary>
        /// List of compiler errors
        /// </summary>
        public ReadOnlyCollection<RazorEngineCompilerError> CompilerErrors { get; }

        /// <summary>
        /// Compilation data containing the generated source code
        /// </summary>
        public RazorEngine.Compilation.CompilationData CompilationData { get; }

        static string BuildMessage(IEnumerable<RazorEngineCompilerError> errors)
        {
            var first = errors?.FirstOrDefault();
            return first != null ? "Unable to compile template: " + first.ErrorText : "Unable to compile template.";
        }
    }
}
