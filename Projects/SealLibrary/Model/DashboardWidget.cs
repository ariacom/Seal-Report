//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using DynamicTypeDescriptor;
using Seal.Converter;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    public class DashboardWidget : RootEditor
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                TypeDescriptor.Refresh(this);
            }
        }

        public override void InitEditor()
        {
            base.InitEditor();
        }

        #endregion

        public override string ToString()
        {
            return _name;
        }

        private string _guid;
        private string _name = "";
        private string _tag;
        private string _description;
        private string _icon = "glyphicon glyphicon-info-sign";
        private string _color = "default";
        private int _width = 400;
        private int _height = 300;
        private bool _dynamic = false;
        private bool _exec = true;
        private int _refresh = -1;

        [Browsable(false)]
        public string GUID { get => _guid; set => _guid = value; }

        [DisplayName("Name"), Description("The widget name."), Id(1, 1)]
        public string Name { get => _name;
            set {
                _name = value;
                //Create guid for the first time
                if (!string.IsNullOrEmpty(_name) && string.IsNullOrEmpty(_guid))
                {
                    _guid = Guid.NewGuid().ToString();
                }
            }
        }

        [DisplayName("Description"), Description("Description of the widget."), Id(2, 1)]
        public string Description { get => _description; set => _description = value; }

        [DisplayName("Security tag"), Description("Tag used to define the security of the Dashboard Designer (Widgets of the Security Groups defined in the Web Security)."), Id(3, 1)]
        public string Tag { get => _tag; set => _tag = value; }

        [DisplayName("Is dynamic"), Description("If true, the widget may modify dynamically the name, icon or color after the execution (e.g. set the color in red if no record in the model)."), Id(4, 1)]
        [DefaultValue(false)]
        public bool Dynamic { get => _dynamic; set => _dynamic = value; }

        [DisplayName("Icon class"), Description("CSS class defining the icon of the widget header."), Id(5, 1)]
        [TypeConverter(typeof(WidgetIconClassConverter))]
        [DefaultValue("glyphicon glyphicon-info-sign")]
        public string Icon { get => _icon; set => _icon = value; }

        [DisplayName("Color class"), Description("CSS class defining the background color of the widget header."), Id(6, 1)]
        [TypeConverter(typeof(WidgetColorClassConverter))]
        [DefaultValue("default")]
        public string Color { get => _color; set => _color = value; }

        [DisplayName("Width"), Description("Width of the widget in pixels."), Id(7, 1)]
        [DefaultValue(400)]
        public int Width { get => _width; set => _width = value; }

        [DisplayName("Height"), Description("Height of the widget in pixels."), Id(8, 1)]
        [DefaultValue(300)]
        public int Height { get => _height; set => _height = value; }

        [DisplayName("Allow report execution"), Description("If true, the widget name is a link to execute the full report."), Id(9, 1)]
        [DefaultValue(true)]
        public bool Exec { get => _exec; set => _exec = value; }

        [DisplayName("Auto-Refresh (seconds)"), Description("Number of seconds before the widget is re-executed. If -1, the rate of the root view is used (defined in property 'Options: Auto-Refresh (seconds)'). A value of 0 means no refresh."), Id(10, 1)]
        [DefaultValue(-1)]
        public int Refresh { get => _refresh; set => _refresh = value; }

        [XmlIgnore]
        [DisplayName("Dashboard XML"), Description("The XML to insert in a dashboard definition file to show this widget."), Id(11, 1)]
        public string XML
        {
            get
            {
                return string.IsNullOrEmpty(_name) ? "" : string.Format("<DashboardItem><WidgetGUID>{0}</WidgetGUID></DashboardItem>", _guid);
            }
        }
        [XmlIgnore, Browsable(false)]
        public bool IsPublished
        {
            get { return !string.IsNullOrEmpty(_name);  }
        }

        //Run-time
        [XmlIgnore]
        public string ReportName;
        [XmlIgnore]
        public string ReportPath;
        [XmlIgnore]
        public DateTime LastModification;
    }
}
