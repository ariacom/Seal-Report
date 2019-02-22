//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Converter;
using Seal.Forms;
using System.Drawing.Design;
using System.Collections.Generic;
using System.Linq;

namespace Seal.Model
{
    public class Parameter : RootComponent
    {
        public const string ReportFormatParameter = "report_format";
        public const string DrillEnabledParameter = "drill_enabled";
        public const string SubReportsEnabledParameter = "subreports_enabled";
        public const string ServerPaginationParameter = "serverpagination_enabled";
        public const string ForceExecutionParameter = "force_execution";
        public const string ForceModelsLoad = "force_models_load";
        public const string NVD3AddNullPointParameter = "nvd3_add_null_point";
        public const string ColumnsHiddenParameter = "columns_hidden";
        public const string CSVUtf8Parameter = "csv_utf8";
        public const string AutoScrollParameter = "messages_autoscroll";


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

                if (this is OutputParameter)
                {
                    GetProperty("CustomValue").SetIsBrowsable(true);
                    GetProperty("Value").SetIsReadOnly(! ((OutputParameter)this).CustomValue);
                    GetProperty("TextValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("BoolValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("NumericValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("EnumValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                }

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


        string[] _textSamples = null;
        [XmlIgnore]
        public string[] TextSamples
        {
            get { return _textSamples; }
            set { _textSamples = value; }
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

        [XmlIgnore]
        public string[] EnumValues
        {
            get {
                List<string> result = new List<string>();
                foreach (var val in _enums)
                {
                    result.Add(val.Contains("|") ? val.Split('|')[0] : val);
                }
                return result.ToArray();
            }
        }

        [XmlIgnore]
        public string[] EnumDisplays
        {
            get {
                List<string> result = new List<string>();
                foreach (var val in _enums)
                {
                    result.Add(val.Contains("|")  ? val.Split('|')[1] : val);
                }
                return result.ToArray();
            }
        }

        [DisplayName("Value"), Description("The parameter value."), Category("Definition")]
        [XmlIgnore]
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string EnumValue
        {
            get
            {
                return _value != null && _value.Contains("|") ? _value.Split('|')[0] : _value;
            }
            set
            {
                _value = value;
            }
        }

        [XmlIgnore]
        public string ConfigValue = "";

        [XmlIgnore]
        public object ConfigObject
        {
            get
            {
                if(Type == ViewParameterType.Boolean)
                {
                    if (string.IsNullOrEmpty(ConfigValue)) return false;
                    return bool.Parse(ConfigValue);
                }
                if(Type == ViewParameterType.Numeric)
                {
                    if (string.IsNullOrEmpty(ConfigValue)) return 0;
                    return int.Parse(ConfigValue);
                }
                return ConfigValue;
            }
        }

        [Category("Helpers"), DisplayName("Reset value"), Description("Reset parameter to its default value.")]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetParameterValue
        {
            get { return "<Click to reset to the default value>"; }
        }

        public string EnumGetDisplayFromValue(string value)
        {
            int index= EnumValues.ToList().FindIndex(i => i == value);
            if (index >= 0 && index < EnumDisplays.Length) return EnumDisplays[index];
            return value;
        }
        public string EnumGetValueFromDisplay(string display)
        {
            int index = EnumDisplays.ToList().FindIndex(i => i == display);
            if (index >= 0 && index < EnumValues.Length) return EnumValues[index];
            return display;
        }
    }

    public class OutputParameter : Parameter
    {
        bool _customValue = false;
        [DisplayName("Use custom value"), Description("If true, a custom parameter value is used when the report is executed for the output."), Category("Definition")]
        [DefaultValue(false)]
        public bool CustomValue
        {
            get
            {
                return _customValue;
            }
            set
            {
                _customValue = value;
                UpdateEditorAttributes();
            }
        }
    }

    public class SecurityParameter : Parameter
    {
    }

}
