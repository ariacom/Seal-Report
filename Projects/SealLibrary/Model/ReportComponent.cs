//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
