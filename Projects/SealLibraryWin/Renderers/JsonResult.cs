//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using Seal.Model;
using System.Collections.Generic;

namespace Seal.Renderer
{
    public class JsonResult
    {
        public List<object> Document = new List<object>();

        public Report Report { get; }

        public JsonResult(Report report)
        {
            Report = report;
            report.JsonResult = this;
        }
    }
}

