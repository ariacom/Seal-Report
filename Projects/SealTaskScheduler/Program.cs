//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Seal.Model;

namespace SealTaskHandler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                ReportExecution.ExecuteReportSchedule(args[0].ToString());
                return;
            }

        }
    }
}
