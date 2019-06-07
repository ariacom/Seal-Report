//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Xml.Serialization;

namespace Seal.Model
{

    public class DashboardItem
    {
        public string GUID { get; set; }
        public string WidgetGUID { get; set; }
        public string GroupName { get; set; } = "";
        public int GroupOrder { get; set; } = 1;
        public string Name { get; set; } = "";
        public string Icon { get; set; }
        public string Color { get; set; }
        public int Width { get; set; } = 0;
        public int Height { get; set; } = 0;
        public int Order { get; set; } = 0;
        public bool Dynamic { get; set; } = false;
        public int Refresh { get; set; } = -1;
        [XmlIgnore]
        public string DisplayName { get; set; }
        [XmlIgnore]
        public string DisplayGroupName { get; set; }
    }
}
