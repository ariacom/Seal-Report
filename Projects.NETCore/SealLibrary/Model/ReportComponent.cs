//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
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

