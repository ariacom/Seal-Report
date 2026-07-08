//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
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

