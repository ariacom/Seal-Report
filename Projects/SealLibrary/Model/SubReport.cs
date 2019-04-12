//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;

namespace Seal.Model
{
    public class SubReport
    {

        string _name;
        [DisplayName("Name"), Description("The name displayed in the report."), Category("Definition")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        string _path;
        [DisplayName("Path"), Description("The report path."), Category("Definition")]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        List<string> _restrictions = new List<string>();
        [Browsable(false)]
        public List<string> Restrictions
        {
            get { return _restrictions; }
            set { _restrictions = value; }
        }
    }
}
