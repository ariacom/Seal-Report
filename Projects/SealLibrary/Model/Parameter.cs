//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Forms;
using System.Drawing.Design;
using System.Collections.Generic;
using System.Linq;

namespace Seal.Model
{
    /// <summary>
    /// Parameters are used to configure report templates, outputs and repository security
    /// </summary>
    public class Parameter : RootComponent
    {
        public const string ReportFormatParameter = "report_format";
        public const string DrillEnabledParameter = "drill_enabled";
        public const string DrillAllParameter = "drill_all";
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

        /// <summary>
        /// Parameter type
        /// </summary>
        [XmlIgnore]
        public ViewParameterType Type { get; set; }

        /// <summary>
        /// If true and the parameter is an enum, only the enum values defined can be selected
        /// </summary>
        [XmlIgnore]
        public bool UseOnlyEnumValues { get; set; } = true;

        /// <summary>
        /// The parameter display name
        /// </summary>
        [DisplayName("Name"), Description("The parameter display name."), Category("Definition")]
        [XmlIgnore]
        public string DisplayName { get; set; }

        /// <summary>
        /// The parameter description
        /// </summary>
        [DisplayName("Description"), Description("The parameter description."), Category("Helpers")]
        [XmlIgnore]
        public string Description { get; set; }

        /// <summary>
        /// The parameter value
        /// </summary>
        [DisplayName("Value"), Description("The parameter value."), Category("Definition")]
        public string Value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public string EditorLanguage { get; set; } = "";

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public string[] TextSamples { get; set; } = null;

        /// <summary>
        /// The boolean parameter value
        /// </summary>
        [DisplayName("Value"), Description("The boolean parameter value."), Category("Definition")]
        [XmlIgnore]
        public bool BoolValue
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Type != ViewParameterType.Boolean) return false;
                return bool.Parse(Value);
            }
            set
            {
                Type = ViewParameterType.Boolean;
                Value = value.ToString();
            }
        }

        /// <summary>
        /// The numeric parameter value
        /// </summary>
        [DisplayName("Value"), Description("The numeric parameter value."), Category("Definition")]
        [XmlIgnore]
        public int NumericValue
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Type != ViewParameterType.Numeric) return 0;
                return int.Parse(Value);
            }
            set
            {
                Type = ViewParameterType.Numeric;
                Value = value.ToString();
            }
        }

        /// <summary>
        /// The text parameter value
        /// </summary>
        [XmlIgnore]
        [DisplayName("Value"), Description("The text parameter value."), Category("Definition")]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string TextValue
        {
            get
            {
                return Value;
            }
            set
            {
                Type = ViewParameterType.Text;
                Value = value;
            }
        }


        private string[] _enums = null;
        /// <summary>
        /// List of string values if the parameter is an enum. Each enum can have an id and an optional display. 
        /// </summary>
        [XmlIgnore]
        public string[] Enums
        {
            get { return _enums; }
            set
            {
                if (value != null) Type = ViewParameterType.Enum;
                _enums = value;
            }
        }

        /// <summary>
        /// List of enum values if the parameter is an enum
        /// </summary>
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

        /// <summary>
        /// List of enum display values if the parameter is an enum
        /// </summary>
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

        /// <summary>
        /// The enum parameter value
        /// </summary>
        [DisplayName("Value"), Description("The parameter value."), Category("Definition")]
        [XmlIgnore]
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string EnumValue
        {
            get
            {
                return Value != null && Value.Contains("|") ? Value.Split('|')[0] : Value;
            }
            set
            {
                Value = value;
            }
        }

        /// <summary>
        /// String to store the default configuration value
        /// </summary>
        [XmlIgnore]
        public string ConfigValue = "";

        /// <summary>
        /// Default configuration value
        /// </summary>
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

        /// <summary>
        /// Editor Helper: Reset parameter to its default value
        /// </summary>
        [Category("Helpers"), DisplayName("Reset value"), Description("Reset parameter to its default value.")]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperResetParameterValue
        {
            get { return "<Click to reset to the default value>"; }
        }

        /// <summary>
        /// For an enum, returns the display text from the value
        /// </summary>
        public string EnumGetDisplayFromValue(string value)
        {
            int index= EnumValues.ToList().FindIndex(i => i == value);
            if (index >= 0 && index < EnumDisplays.Length) return EnumDisplays[index];
            return value;
        }

        /// <summary>
        /// For an enum, returns the value from the display text
        /// </summary>
        public string EnumGetValueFromDisplay(string display)
        {
            int index = EnumDisplays.ToList().FindIndex(i => i == display);
            if (index >= 0 && index < EnumValues.Length) return EnumValues[index];
            return display;
        }
    }

    /// <summary>
    /// OutputParameter are Parameter used for report output
    /// </summary>
    public class OutputParameter : Parameter
    {
        bool _customValue = false;
        /// <summary>
        /// If true, a custom parameter value is used when the report is executed for the output
        /// </summary>
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

    /// <summary>
    /// SecurityParameter are Parameter used to define the security
    /// </summary>
    public class SecurityParameter : Parameter
    {
    }
}
