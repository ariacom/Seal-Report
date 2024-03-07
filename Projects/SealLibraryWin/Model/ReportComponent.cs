//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// Component of the report having a reference to a Report object
    /// </summary>
    public class ReportComponent : RootComponent
    {
        protected Report _report;
        /// <summary>
        /// The current report
        /// </summary>
        [XmlIgnore]
        public Report Report
        {
            get { return _report; }
            set { _report = value; }
        }
    }
}
