//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using Seal.Model;
using Seal.Helpers;
using System.Reflection;

namespace SealTaskHandler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (AppDomain.CurrentDomain.IsDefaultAppDomain())
                {
                    Helper.RunInAnotherAppDomain(Assembly.GetExecutingAssembly().Location, args);
                }
                else
                {
                    ReportExecution.ExecuteReportSchedule(args[0].ToString());
                }
                return;
            }

        }
    }
}
