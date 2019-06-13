//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Xml.Serialization;

namespace Seal.Model
{

    public class DashboardItem
    {
        public string GUID { get; set; }
        public string WidgetGUID { get; set; }
        public string GroupName { get; set; } = "";
        public int GroupOrder { get; set; } = 1;
        public int Order { get; set; } = 0;

        public string Name { get; set; } = "";
        public bool ShouldSerializeName() {
            return (_widget != null && _widget.Name != Name);
        }

        public string Icon { get; set; }
        public bool ShouldSerializeIcon()
        {
            return (_widget != null && _widget.Icon != Icon);
        }

        public string Color { get; set; }
        public bool ShouldSerializeColor()
        {
            return (_widget != null && _widget.Color != Color);
        }

        public int Width { get; set; } = -1;
        public bool ShouldSerializeWidth()
        {
            return (_widget != null && _widget.Width != Width);
        }

        public int Height { get; set; } = -1;
        public bool ShouldSerializeHeight()
        {
            return (_widget != null && _widget.Height != Height);
        }

        public bool? Dynamic { get; set; } = null;
        public bool ShouldSerializeDynamic()
        {
            return (_widget != null && _widget.Dynamic != Dynamic);
        }
        public int Refresh { get; set; } = -2;
        public bool ShouldSerializeRefresh()
        {
            return (_widget != null && _widget.Refresh != Refresh);
        }

        [XmlIgnore]
        public string DisplayName { get; set; }
        [XmlIgnore]
        public string DisplayGroupName { get; set; }
        [XmlIgnore]
        private DashboardWidget _widget;
        [XmlIgnore]
        public DashboardWidget Widget { get { return _widget; } }

        public void SetWidget(DashboardWidget widget)
        {
            _widget = widget;
            WidgetGUID = _widget.GUID;
            if (string.IsNullOrEmpty(Name)) Name = _widget.Name;
            if (string.IsNullOrEmpty(Icon)) Icon = _widget.Icon;
            if (string.IsNullOrEmpty(Color)) Color = _widget.Color;
            if (Width == -1) Width = _widget.Width;
            if (Height == -1) Height = _widget.Height;
            if (Dynamic == null) Dynamic = _widget.Dynamic;
            if (Refresh == -2)  Refresh = _widget.Refresh;
        }
    }
}
