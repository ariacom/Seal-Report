//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using Microsoft.Extensions.Configuration;
using Seal.Model;
using System;
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
    /// Usage: SealReportRunner &lt;report-file&gt; [/o &lt;output-name-or-GUID&gt;] [/v &lt;view-name-or-GUID&gt;]
    ///   /o  Execute the given report Output (resolved by name or GUID). Takes precedence over /v.
    ///   /v  Render the given report View (resolved by name or GUID). Used only when /o is not specified.
    ///   If neither /o nor /v is specified, the report's default view is rendered.
    /// Exit codes: 0 = success, 1 = error (bad arguments, missing file/output/view, or report execution errors).
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            //Encoding registration
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string reportFile = null, outputNameOrGUID = null, viewNameOrGUID = null;
            if (!ParseArguments(args, ref reportFile, ref outputNameOrGUID, ref viewNameOrGUID))
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

                if (!File.Exists(reportFile)) throw new Exception($"Unable to find report file '{reportFile}'.");

                var repository = Repository.Instance;
                var report = Report.LoadFromFile(reportFile, repository);

                //Resolve what to execute: /o (output) takes precedence over /v (view)
                if (!string.IsNullOrEmpty(outputNameOrGUID))
                {
                    var output = report.Outputs.FirstOrDefault(i => i.GUID == outputNameOrGUID || i.Name == outputNameOrGUID);
                    if (output == null) throw new Exception($"Unable to find output '{outputNameOrGUID}' in the report.");
                    report.OutputToExecute = output;
                    report.CurrentViewGUID = output.ViewGUID;
                    Console.WriteLine($"Executing output '{output.Name}'...");
                }
                else if (!string.IsNullOrEmpty(viewNameOrGUID))
                {
                    var view = report.Views.FirstOrDefault(i => i.GUID == viewNameOrGUID || i.Name == viewNameOrGUID);
                    if (view == null) throw new Exception($"Unable to find view '{viewNameOrGUID}' in the report.");
                    report.CurrentViewGUID = view.GUID;
                    Console.WriteLine($"Rendering view '{view.Name}'...");
                }
                else
                {
                    report.CurrentViewGUID = report.ViewGUID;
                    Console.WriteLine("Rendering the default view...");
                }

                var execution = new ReportExecution() { Report = report };
                report.ExecutionContext = ReportExecutionContext.TaskScheduler;
                execution.Execute();

                //Stream the execution messages to the console while the report is executing
                int printed = 0;
                while (report.IsExecuting)
                {
                    printed = FlushMessages(report, printed);
                    Thread.Sleep(200);
                }
                FlushMessages(report, printed);

                if (report.HasErrors)
                {
                    Console.Error.WriteLine();
                    Console.Error.WriteLine(report.ExecutionErrors);
                    return 1;
                }

                Console.WriteLine();
                Console.WriteLine($"Report executed successfully.");
                if (!string.IsNullOrEmpty(report.ResultFilePath) && File.Exists(report.ResultFilePath))
                    Console.WriteLine($"Result: {report.ResultFilePath}");
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
        static int FlushMessages(Report report, int printed)
        {
            var messages = report.ExecutionMessages ?? "";
            if (messages.Length > printed)
            {
                Console.Write(messages.Substring(printed));
                printed = messages.Length;
            }
            return printed;
        }

        /// <summary>
        /// Parse the command line: first non-option argument is the report file, /o and /v take a following value.
        /// </summary>
        static bool ParseArguments(string[] args, ref string reportFile, ref string outputNameOrGUID, ref string viewNameOrGUID)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.Equals("/o", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length) return false;
                    outputNameOrGUID = args[++i];
                }
                else if (arg.Equals("/v", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length) return false;
                    viewNameOrGUID = args[++i];
                }
                else if (reportFile == null)
                {
                    reportFile = arg;
                }
                else return false;
            }
            return !string.IsNullOrEmpty(reportFile);
        }

        static void PrintUsage()
        {
            Console.Error.WriteLine("Usage: SealReportRunner <report-file> [/o <output-name-or-GUID>] [/v <view-name-or-GUID>]");
            Console.Error.WriteLine("  /o  Execute the given report Output (by name or GUID). Takes precedence over /v.");
            Console.Error.WriteLine("  /v  Render the given report View (by name or GUID), used only when /o is not specified.");
            Console.Error.WriteLine("  If neither /o nor /v is specified, the report's default view is rendered.");
        }
    }
}
