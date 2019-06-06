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
        private string _guid;
        private string _widgetGUID;
        private string _groupName = "";
        private int _groupOrder = 1;
        private string _name = "";
        private string _icon = "";
        private string _color = "";
        private bool _dynamic = false;
        private int _width = 400;
        private int _height = 0;
        private int _order = 0;
        private int _refresh = -1;

        public string GUID { get => _guid; set => _guid = value; }
        public string WidgetGUID { get => _widgetGUID; set => _widgetGUID = value; }
        public string GroupName { get => _groupName; set => _groupName = value; }
        public int GroupOrder { get => _groupOrder; set => _groupOrder = value; }
        public string Name { get => _name; set => _name = value; }
        public string Icon { get => _icon; set => _icon = value; }
        public string Color { get => _color; set => _color = value; }
        public int Width { get => _width; set => _width = value; }
        public int Height { get => _height; set => _height = value; }
        public int Order { get => _order; set => _order = value; }
        public bool Dynamic { get => _dynamic; set => _dynamic = value; }
        public int Refresh { get => _refresh; set => _refresh = value; }

        [XmlIgnore]
        public DashboardWidget Widget;
    }
}
