//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Microsoft.Extensions.Configuration;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace SealTaskScheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                //Encoding registration
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                try
                {
                    var settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "appsettings.json");
                    if (!File.Exists(settingsPath)) throw new Exception($"Unable to find {settingsPath}.");

                    //Set repository path
                    IConfigurationRoot configuration = new ConfigurationBuilder()
                        .AddJsonFile(settingsPath)
                        .Build();
                    var section = configuration.GetSection(Repository.SealConfigurationSectionKeyword);
                    if (section != null)
                    {
                        Repository.RepositoryConfigurationPath = configuration.GetValue<string>($"{Repository.SealConfigurationSectionKeyword}:{Repository.SealConfigurationRepositoryPathKeyword}");

                        //Limit memory
                        var mws = configuration.GetValue<double>($"{Repository.SealConfigurationSectionKeyword}:{Repository.SealConfigurationMaxWorkingSetKeyword}", 0);
                        if (mws > 0) Process.GetCurrentProcess().MaxWorkingSet = new IntPtr(Convert.ToInt64(Math.Max(1, mws) * 1024 * 1024 * 1024));

                    }
                    ReportExecution.ExecuteReportSchedule(args[0].ToString());
                }
                catch (Exception ex)
                {
                    Helper.WriteLogEntryScheduler(EventLogEntryType.Error, ex.Message);
                }
            }
        }
    }
}
