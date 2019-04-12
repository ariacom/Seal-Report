//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Xml.Serialization;

namespace Seal.Model
{
    public class ReportComponent : RootComponent
    {
        protected Report _report;
        [XmlIgnore]
        public Report Report
        {
            get { return _report; }
            set { _report = value; }
        }
    }
}
