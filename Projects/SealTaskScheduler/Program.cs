//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Microsoft.Extensions.Configuration;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Diagnostics;

namespace SealTaskScheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                try
                {
                    //Set repository path
                    IConfigurationRoot configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();
                    var section = configuration.GetSection(Repository.SealConfigurationSectionKeyword);
                    if (section != null) Repository.RepositoryConfigurationPath = configuration.GetSection(Repository.SealConfigurationSectionKeyword)[Repository.SealConfigurationRepositoryPathKeyword];

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
