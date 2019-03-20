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
        private double _width = 2;
        private double _height = 2;
        private bool _dynamic = false;
        private bool _exec = true;

        [Browsable(false)]
        public string GUID { get => _guid; set => _guid = value; }

        [DisplayName("Name"), Description("The widget name."), Id(1, 1)]
        public string Name { get => _name;
            set {
                _name = value;
                //Create guid for the first time
                if (!string.IsNullOrEmpty(_name) && string.IsNullOrEmpty(_guid)) _guid = Guid.NewGuid().ToString();
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

        [DisplayName("Width"), Description("Width of the widget of unit of 200 pixels."), Id(7, 1)]
        [DefaultValue(2)]
        public double Width { get => _width; set => _width = value; }

        [DisplayName("Height"), Description("Height of the widget of 140 pixels."), Id(8, 1)]
        [DefaultValue(2)]
        public double Height { get => _height; set => _height = value; }

        [DisplayName("Allow report execution"), Description("If true, the widget name is a link to execute the full report."), Id(9, 1)]
        [DefaultValue(true)]
        public bool Exec { get => _exec; set => _exec = value; }

        //Run-time
        [XmlIgnore]
        public string ReportName;
        [XmlIgnore]
        public string ReportPath;
        [XmlIgnore]
        public DateTime LastModification;
    }
}
