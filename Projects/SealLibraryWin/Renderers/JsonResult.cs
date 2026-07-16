//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Seal.Model;
using System.Collections.Generic;

namespace Seal.Renderer
{
    /// <summary>
    /// Current Json objects shared by the Json renderer templates during the report execution. The instance is available from the report with Report.JsonResult.
    /// </summary>
    public class JsonResult
    {
        /// <summary>
        /// Objects serialized to generate the Json result
        /// </summary>
        public List<object> Document = new List<object>();

        /// <summary>
        /// Current report
        /// </summary>
        public Report Report { get; }

        /// <summary>
        /// Creates the Json result and assigns it to the report
        /// </summary>
        public JsonResult(Report report)
        {
            Report = report;
            report.JsonResult = this;
        }
    }
}

