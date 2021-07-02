//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;

namespace Seal.Model
{
    /// <summary>
    /// A SubReport defines a report executed from a column
    /// </summary>
    public class SubReport
    {
        /// <summary>
        /// The name displayed in the report
        /// </summary>
        [DisplayName("Name"), Description("The name displayed in the report."), Category("Definition")]
        public string Name { get; set; }

        /// <summary>
        /// The report path
        /// </summary>
        [DisplayName("Path"), Description("The report path."), Category("Definition")]
        public string Path { get; set; }

        /// <summary>
        /// List of restriction identifier involved in the Sub-report
        /// </summary>
        [Browsable(false)]
        public List<string> Restrictions { get; set; } = new List<string>();
    }
}
