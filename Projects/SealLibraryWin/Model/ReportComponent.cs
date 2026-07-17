//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// Component of the report having a reference to a Report object
    /// </summary>
    public class ReportComponent : RootComponent
    {
        /// <summary>
        /// Field of the Report property
        /// </summary>
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
