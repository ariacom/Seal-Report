//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RazorEngine;
using System.Xml.Serialization;
using Seal.Helpers;
using System.ComponentModel;
using Seal.Converter;
using Seal.Forms;
using System.Drawing.Design;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using System.ComponentModel.Design;

namespace Seal.Model
{
    public class Parameter : RootComponent
    {
        public const string PrintLayoutParameter = "print_layout";
        public const string PDFLayoutParameter = "pdf_layout";
        public const string ExcelLayoutParameter = "excel_layout";
        public const string DrillEnabledParameter = "drill_enabled";
        public const string SubReportsEnabledParameter = "subreports_enabled";
        public const string ServerPaginationParameter = "serverpagination_enabled";
        
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Value").SetIsBrowsable(Type == ViewParameterType.String);
                GetProperty("TextValue").SetIsBrowsable(Type == ViewParameterType.Text);
                GetProperty("BoolValue").SetIsBrowsable(Type == ViewParameterType.Boolean);
                GetProperty("NumericValue").SetIsBrowsable(Type == ViewParameterType.Numeric);
                GetProperty("EnumValue").SetIsBrowsable(Type == ViewParameterType.Enum);
                GetProperty("Description").SetIsBrowsable(true);
                GetProperty("HelperResetParameterValue").SetIsBrowsable(true);

                //Read only
                GetProperty("Description").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        ViewParameterType _type;
        [XmlIgnore]
        public ViewParameterType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        bool _useOnlyEnumValues = true;
        [XmlIgnore]
        public bool UseOnlyEnumValues
        {
            get { return _useOnlyEnumValues; }
            set { _useOnlyEnumValues = value; }
        }

        string _displayName;
        [DisplayName("Name"), Description("The parameter display name."), Category("Definition")]
        [XmlIgnore]
        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        string _description;
        [DisplayName("Description"), Description("The parameter description."), Category("Helpers")]
        [XmlIgnore]
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        string _value;
        [DisplayName("Value"), Description("The parameter value."), Category("Definition")]
        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        string _editorLanguage = "";
        [XmlIgnore]
        public string EditorLanguage
        {
            get { return _editorLanguage; }
            set { _editorLanguage = value; }
        }

        ViewParameterCategory _category = ViewParameterCategory.General;
        [XmlIgnore]
        public ViewParameterCategory Category
        {
            get { return _category; }
            set { _category = value; }
        }

        [DisplayName("Value"), Description("The boolean parameter value."), Category("Definition")]
        [XmlIgnore]
        public bool BoolValue
        {
            get
            {
                if (string.IsNullOrEmpty(_value) || _type != ViewParameterType.Boolean) return false;
                return bool.Parse(_value);
            }
            set
            {
                _type = ViewParameterType.Boolean;
                _value = value.ToString();
            }
        }


        [DisplayName("Value"), Description("The numeric parameter value."), Category("Definition")]
        [XmlIgnore]
        public int NumericValue
        {
            get
            {
                if (string.IsNullOrEmpty(_value) || _type != ViewParameterType.Numeric) return 0;
                return int.Parse(_value);
            }
            set
            {
                _type = ViewParameterType.Numeric;
                _value = value.ToString();
            }
        }

        [XmlIgnore]
        [DisplayName("Value"), Description("The text parameter value."), Category("Definition")]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string TextValue
        {
            get
            {
                return _value;
            }
            set
            {
                _type = ViewParameterType.Text;
                _value = value;
            }
        }


        private string[] _enums = null;
        [XmlIgnore]
        public string[] Enums
        {
            get { return _enums; }
            set
            {
                if (value != null) _type = ViewParameterType.Enum;
                _enums = value;
            }
        }

        [DisplayName("Value"), Description("The parameter value."), Category("Definition")]
        [XmlIgnore]
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string EnumValue
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        [XmlIgnore]
        public string ConfigValue = "";

        [Category("Helpers"), DisplayName("Reset value"), Description("Reset parameter to its default value.")]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetParameterValue
        {
            get { return "<Click to reset to the default value>"; }
        }

    }

    public class SecurityParameter : Parameter
    {
    }

}
