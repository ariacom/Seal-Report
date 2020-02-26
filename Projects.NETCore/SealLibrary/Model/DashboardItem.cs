//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A Dashboard Item is part of a Dashboard and display a report view published as a Widget 
    /// </summary>
    public class DashboardItem
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// Widget identifier part of a report
        /// </summary>
        public string WidgetGUID { get; set; }

        /// <summary>
        /// Group name
        /// </summary>
        public string GroupName { get; set; } = "";

        /// <summary>
        /// Group order
        /// </summary>
        public int GroupOrder { get; set; } = 1;

        /// <summary>
        /// Order used to sort the items
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; } = "";
        public bool ShouldSerializeName() {
            return JSonSerialization || (_widget != null && _widget.Name != Name);
        }

        /// <summary>
        /// Icon
        /// </summary>
        public string Icon { get; set; }
        public bool ShouldSerializeIcon()
        {
            return JSonSerialization || (_widget != null && _widget.Icon != Icon);
        }

        /// <summary>
        /// Color
        /// </summary>
        public string Color { get; set; }
        public bool ShouldSerializeColor()
        {
            return JSonSerialization || (_widget != null && _widget.Color != Color);
        }

        /// <summary>
        /// Width in pixel. -1 = use default Widget width.
        /// </summary>
        public int Width { get; set; } = -1;
        public bool ShouldSerializeWidth()
        {
            return JSonSerialization || (_widget != null && _widget.Width != Width);
        }

        /// <summary>
        /// Height in pixel. -1 = use default Widget height.
        /// </summary>
        public int Height { get; set; } = -1;
        public bool ShouldSerializeHeight()
        {
            return JSonSerialization || (_widget != null && _widget.Height != Height);
        }

        /// <summary>
        /// True if the item can modify its look after the widget execution
        /// </summary>
        public bool? Dynamic { get; set; } = null;
        public bool ShouldSerializeDynamic()
        {
            return JSonSerialization || (_widget != null && _widget.Dynamic != Dynamic);
        }

        /// <summary>
        /// Refresh rate of the item in seconds. -2 = use Widget rate, -1 = use report report, 0 = no refresh 
        /// </summary>
        public int Refresh { get; set; } = -2;
        public bool ShouldSerializeRefresh()
        {
            return JSonSerialization || (_widget != null && _widget.Refresh != Refresh);
        }

        /// <summary>
        /// Display name
        /// </summary>
        [XmlIgnore]
        public string DisplayName { get; set; }

        /// <summary>
        /// Group name
        /// </summary>
        [XmlIgnore]
        public string DisplayGroupName { get; set; }
        [XmlIgnore]
        private DashboardWidget _widget;

        /// <summary>
        /// The Widget referenced by the item
        /// </summary>
        [XmlIgnore]
        public DashboardWidget Widget { get { return _widget; } }

        /// <summary>
        /// If true, the item is being serialized for Json
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public bool JSonSerialization = false;

        /// <summary>
        /// Set a widget to an item
        /// </summary>
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

