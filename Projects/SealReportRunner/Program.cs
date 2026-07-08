//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.Extensions.Configuration;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace SealReportRunner
{
    /// <summary>
    /// Console application to execute a Seal Report in batch and stream the execution messages to the console.
    ///
    /// By DEFAULT the report is executed AND its result view is fully rendered to a self-contained HTML file
    /// (all views are parsed, so a broken .cshtml view becomes a hard error). This makes the runner suitable for
    /// regression testing of views (e.g. chart partials): exit code 1 if any view fails to compile/render, and the
    /// printed Result file is a real, browser-openable HTML that can be inspected.
    ///
    /// Usage: SealReportRunner &lt;report-file&gt; [/o &lt;output&gt;] [/v &lt;view&gt;] [--scheduler] [--out &lt;path&gt;] [--assert &lt;text&gt;]... [--quiet]
    ///   /o &lt;output&gt;   Execute the given report Output (resolved by name or GUID). Renders through the output device. Takes precedence over /v and the default render.
    ///   /v &lt;view&gt;     Render the given report View (resolved by name or GUID) instead of the default view.
    ///   --scheduler   Legacy mode: execute in TaskScheduler context and do NOT render the result views (fast, but does not validate views; the Result file is an empty stub).
    ///   --out &lt;path&gt;  Write the rendered HTML result to &lt;path&gt; (overwritten). Default: a unique file in the report generation folder (its path is printed).
    ///   --assert &lt;text&gt;  Fail (exit 1) unless the rendered HTML contains &lt;text&gt;. May be repeated; all must match.
    ///   --quiet       Only print the final status (PASS/FAIL + result path/size), not the streaming execution log.
    /// Exit codes: 0 = success, 1 = error (bad arguments, missing file/output/view, execution error, view render error, or a failed --assert).
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            //Encoding registration
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var options = new RunOptions();
            if (!ParseArguments(args, options))
            {
                PrintUsage();
                return 1;
            }

            try
            {
                //Set repository path from appsettings.json (same bootstrap as the Task Scheduler)
                var settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "appsettings.json");
                if (!File.Exists(settingsPath)) throw new Exception($"Unable to find {settingsPath}.");

                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .AddJsonFile(settingsPath)
                    .Build();
                Repository.RepositoryConfigurationPath = configuration.GetValue<string>($"{Repository.SealConfigurationSectionKeyword}:{Repository.SealConfigurationRepositoryPathKeyword}");

                //Limit memory
                var mws = configuration.GetValue<double>($"{Repository.SealConfigurationSectionKeyword}:{Repository.SealConfigurationMaxWorkingSetKeyword}", 0);
                if (mws > 0) Process.GetCurrentProcess().MaxWorkingSet = new IntPtr(Convert.ToInt64(Math.Max(1, mws) * 1024 * 1024 * 1024));

                if (!File.Exists(options.ReportFile)) throw new Exception($"Unable to find report file '{options.ReportFile}'.");

                var repository = Repository.Instance;
                var report = Report.LoadFromFile(options.ReportFile, repository);

                //Resolve what to execute: /o (output) takes precedence over /v (view)
                bool renderView = false;
                if (!string.IsNullOrEmpty(options.OutputNameOrGUID))
                {
                    var output = report.Outputs.FirstOrDefault(i => i.GUID == options.OutputNameOrGUID || i.Name == options.OutputNameOrGUID);
                    if (output == null) throw new Exception($"Unable to find output '{options.OutputNameOrGUID}' in the report.");
                    report.OutputToExecute = output;
                    report.CurrentViewGUID = output.ViewGUID;
                    if (!options.Quiet) Console.WriteLine($"Executing output '{output.Name}'...");
                }
                else
                {
                    if (!string.IsNullOrEmpty(options.ViewNameOrGUID))
                    {
                        var view = report.Views.FirstOrDefault(i => i.GUID == options.ViewNameOrGUID || i.Name == options.ViewNameOrGUID);
                        if (view == null) throw new Exception($"Unable to find view '{options.ViewNameOrGUID}' in the report.");
                        report.CurrentViewGUID = view.GUID;
                        if (!options.Quiet) Console.WriteLine($"Rendering view '{view.Name}'...");
                    }
                    else
                    {
                        report.CurrentViewGUID = report.ViewGUID;
                        if (!options.Quiet) Console.WriteLine("Rendering the default view...");
                    }
                    //Render the result views to a self-contained HTML unless legacy scheduler mode was requested
                    renderView = !options.Scheduler;
                }

                var execution = new ReportExecution() { Report = report };
                report.ExecutionContext = ReportExecutionContext.TaskScheduler;
                report.SkipExternalViewer = true;
                execution.Execute();

                //Stream the execution messages to the console while the report is executing
                int printed = 0;
                while (report.IsExecuting)
                {
                    printed = FlushMessages(report, printed, options.Quiet);
                    Thread.Sleep(200);
                }
                printed = FlushMessages(report, printed, options.Quiet);

                if (report.HasErrors)
                {
                    Console.Error.WriteLine();
                    Console.Error.WriteLine(report.ExecutionErrors);
                    return 1;
                }

                //Render the result views to a real, self-contained HTML file so that view errors are caught and the output can be inspected
                string resultPath = report.ResultFilePath;
                if (renderView)
                {
                    resultPath = execution.GenerateHTMLResult(false);
                    FlushMessages(report, printed, options.Quiet);
                    if (report.HasErrors)
                    {
                        Console.Error.WriteLine();
                        Console.Error.WriteLine(report.ExecutionErrors);
                        return 1;
                    }
                }

                //Copy the result to the requested location
                if (!string.IsNullOrEmpty(options.OutPath) && !string.IsNullOrEmpty(resultPath) && File.Exists(resultPath))
                {
                    File.Copy(resultPath, options.OutPath, true);
                    resultPath = options.OutPath;
                }

                //Content assertions against the rendered HTML
                if (options.Asserts.Count > 0)
                {
                    if (string.IsNullOrEmpty(resultPath) || !File.Exists(resultPath))
                    {
                        Console.Error.WriteLine();
                        Console.Error.WriteLine("FAIL: no result file to run --assert against (did you use --scheduler?).");
                        return 1;
                    }
                    var content = File.ReadAllText(resultPath);
                    var missing = options.Asserts.Where(a => !content.Contains(a)).ToList();
                    if (missing.Count > 0)
                    {
                        Console.Error.WriteLine();
                        foreach (var m in missing) Console.Error.WriteLine($"FAIL: result does not contain '{m}'.");
                        return 1;
                    }
                    if (!options.Quiet) foreach (var a in options.Asserts) Console.WriteLine($"assert OK: '{a}'");
                }

                Console.WriteLine();
                Console.WriteLine("Report executed successfully.");
                if (!string.IsNullOrEmpty(resultPath) && File.Exists(resultPath))
                {
                    Console.WriteLine($"Result: {resultPath} ({new FileInfo(resultPath).Length} bytes)");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// Write the part of the execution messages that has not been printed yet, and return the new printed length.
        /// </summary>
        static int FlushMessages(Report report, int printed, bool quiet)
        {
            var messages = report.ExecutionMessages ?? "";
            if (!quiet && messages.Length > printed)
            {
                Console.Write(messages.Substring(printed));
            }
            return messages.Length;
        }

        /// <summary>
        /// Parse the command line: first non-option argument is the report file; /o and /v and --out and --assert take a following value.
        /// </summary>
        static bool ParseArguments(string[] args, RunOptions options)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.Equals("/o", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length) return false;
                    options.OutputNameOrGUID = args[++i];
                }
                else if (arg.Equals("/v", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length) return false;
                    options.ViewNameOrGUID = args[++i];
                }
                else if (arg.Equals("--out", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length) return false;
                    options.OutPath = args[++i];
                }
                else if (arg.Equals("--assert", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length) return false;
                    options.Asserts.Add(args[++i]);
                }
                else if (arg.Equals("--scheduler", StringComparison.OrdinalIgnoreCase))
                {
                    options.Scheduler = true;
                }
                else if (arg.Equals("--quiet", StringComparison.OrdinalIgnoreCase))
                {
                    options.Quiet = true;
                }
                else if (options.ReportFile == null)
                {
                    options.ReportFile = arg;
                }
                else return false;
            }
            return !string.IsNullOrEmpty(options.ReportFile);
        }

        static void PrintUsage()
        {
            Console.Error.WriteLine("Usage: SealReportRunner <report-file> [/o <output>] [/v <view>] [--scheduler] [--out <path>] [--assert <text>]... [--quiet]");
            Console.Error.WriteLine("  Default: execute the report and render the result view to a self-contained HTML file (validates all views).");
            Console.Error.WriteLine("  /o <output>     Execute a report Output (by name or GUID). Takes precedence over /v and the default render.");
            Console.Error.WriteLine("  /v <view>       Render the given view (by name or GUID) instead of the default view.");
            Console.Error.WriteLine("  --scheduler     Legacy mode: TaskScheduler context, no result view rendering (Result file is an empty stub).");
            Console.Error.WriteLine("  --out <path>    Write the rendered HTML result to <path> (overwritten).");
            Console.Error.WriteLine("  --assert <text> Fail unless the rendered HTML contains <text>. May be repeated.");
            Console.Error.WriteLine("  --quiet         Print only the final status, not the streaming execution log.");
        }

        class RunOptions
        {
            public string ReportFile;
            public string OutputNameOrGUID;
            public string ViewNameOrGUID;
            public string OutPath;
            public bool Scheduler;
            public bool Quiet;
            public List<string> Asserts = new List<string>();
        }
    }
}
