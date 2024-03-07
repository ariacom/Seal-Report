//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System;
using Seal.Helpers;
using MySqlX.XDevAPI.Common;
using System.Globalization;

#if WINDOWS
using Seal.Forms;
using System.Drawing.Design;
#endif


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
        public const string EnableResultsMenuParameter = "resultsmenu_enabled";
        public const string ForceExecutionParameter = "force_execution";
        public const string ForceRefreshParameter = "force_refresh";
        public const string ForceModelsLoad = "force_models_load";
        public const string NVD3AddNullPointParameter = "nvd3_add_null_point";
        public const string ColumnsHiddenParameter = "columns_hidden";
        public const string CSVUtf8Parameter = "csv_utf8";
        public const string AutoScrollParameter = "messages_autoscroll";
        public const string RestrictionsExecView = "restrictions_exec_view";
        public const string NavigationView = "navigation_view";

        public static string[] Glyphicons = new string[] { "asterisk", "plus", "minus", "eur", "euro", "cloud", "envelope", "pencil", "glass", "music", "search", "heart", "star", "star-empty", "user", "film", "th-large", "th", "th-list", "ok", "remove", "zoom-in", "zoom-out", "off", "signal", "cog", "trash", "home", "file", "time", "road", "download-alt", "download", "upload", "inbox", "play-circle", "repeat", "refresh", "list-alt", "lock", "flag", "headphones", "volume-off", "volume-down", "volume-up", "qrcode", "barcode", "tag", "tags", "book", "bookmark", "print", "camera", "font", "bold", "italic", "text-height", "text-width", "align-left", "align-center", "align-right", "align-justify", "list", "indent-left", "indent-right", "facetime-video", "picture", "map-marker", "adjust", "tint", "edit", "share", "check", "move", "step-backward", "fast-backward", "backward", "play", "pause", "stop", "forward", "fast-forward", "step-forward", "eject", "chevron-left", "chevron-right", "plus-sign", "minus-sign", "remove-sign", "ok-sign", "question-sign", "info-sign", "screenshot", "remove-circle", "ok-circle", "ban-circle", "arrow-left", "arrow-right", "arrow-up", "arrow-down", "share-alt", "resize-full", "resize-small", "exclamation-sign", "gift", "leaf", "fire", "eye-open", "eye-close", "warning-sign", "plane", "calendar", "random", "comment", "magnet", "chevron-up", "chevron-down", "retweet", "shopping-cart", "folder-close", "folder-open", "resize-vertical", "resize-horizontal", "hdd", "bullhorn", "bell", "certificate", "thumbs-up", "thumbs-down", "hand-right", "hand-left", "hand-up", "hand-down", "circle-arrow-right", "circle-arrow-left", "circle-arrow-up", "circle-arrow-down", "globe", "wrench", "tasks", "filter", "briefcase", "fullscreen", "dashboard", "paperclip", "heart-empty", "link", "phone", "pushpin", "usd", "gbp", "sort", "sort-by-alphabet", "sort-by-alphabet-alt", "sort-by-order", "sort-by-order-alt", "sort-by-attributes", "sort-by-attributes-alt", "unchecked", "expand", "collapse-down", "collapse-up", "log-in", "flash", "log-out", "new-window", "record", "save", "open", "saved", "import", "export", "send", "floppy-disk", "floppy-saved", "floppy-remove", "floppy-save", "floppy-open", "credit-card", "transfer", "cutlery", "header", "compressed", "earphone", "phone-alt", "tower", "stats", "sd-video", "hd-video", "subtitles", "sound-stereo", "sound-dolby", "sound-5-1", "sound-6-1", "sound-7-1", "copyright-mark", "registration-mark", "cloud-download", "cloud-upload", "tree-conifer", "tree-deciduous", "cd", "save-file", "open-file", "level-up", "copy", "paste", "alert", "equalizer", "king", "queen", "pawn", "bishop", "knight", "baby-formula", "tent", "blackboard", "bed", "apple", "erase", "hourglass", "lamp", "duplicate", "piggy-bank", "scissors", "bitcoin", "yen", "ruble", "scale", "ice-lolly", "ice-lolly-tasted", "education", "option-horizontal", "option-vertical", "menu-hamburger", "modal-window", "oil", "grain", "sunglasses", "text-size", "text-color", "text-background", "object-align-top", "object-align-bottom", "object-align-horizontal", "object-align-left", "object-align-vertical", "object-align-right", "triangle-right", "triangle-left", "triangle-bottom", "triangle-top", "console", "superscript", "subscript", "menu-left", "menu-right", "menu-down", "menu-up" };

#if WINDOWS
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
                GetProperty("DoubleValue").SetIsBrowsable(Type == ViewParameterType.Double);
                GetProperty("Description").SetIsBrowsable(true);
                GetProperty("HelperResetParameterValue").SetIsBrowsable(true);

                //Read only
                GetProperty("Description").SetIsReadOnly(true);

                if (this is OutputParameter)
                {
                    GetProperty("CustomValue").SetIsBrowsable(true);
                    GetProperty("Value").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("TextValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("BoolValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("NumericValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("DoubleValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("EnumValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                }

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

#endif
        /// <summary>
        /// Parameter type
        /// </summary>
        [XmlIgnore]
        public ViewParameterType Type { get; set; } = ViewParameterType.Text;

        /// <summary>
        /// If true and the parameter is an enum, only the enum values defined can be selected
        /// </summary>
        [XmlIgnore]
        public bool UseOnlyEnumValues { get; set; } = true;

        /// <summary>
        /// The parameter display name
        /// </summary>
#if WINDOWS
        [DisplayName("Name"), Description("The parameter display name."), Category("Definition")]
#endif
        [XmlIgnore]
        public string DisplayName { get; set; }

        /// <summary>
        /// The parameter description
        /// </summary>
#if WINDOWS
        [DisplayName("Description"), Description("The parameter description."), Category("Helpers")]
#endif
        [XmlIgnore]
        public string Description { get; set; }

        /// <summary>
        /// The parameter value
        /// </summary>
#if WINDOWS
        [DisplayName("Value"), Description("The parameter value."), Category("Definition")]
#endif
        public string Value { get; set; } = "";

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
#if WINDOWS
        [DisplayName("Value"), Description("The boolean parameter value."), Category("Definition")]
#endif
        [XmlIgnore]
        public bool BoolValue
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Type != ViewParameterType.Boolean) return false;
                if (bool.TryParse(Value, out bool boolValue)) return boolValue;
                return false;
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
#if WINDOWS
        [DisplayName("Value"), Description("The numeric parameter value."), Category("Definition")]
#endif
        [XmlIgnore]
        public int NumericValue
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Type != ViewParameterType.Numeric) return 0;
                if (int.TryParse(Value, out int intValue)) return intValue;
                return 0;
            }
            set
            {
                Type = ViewParameterType.Numeric;
                Value = value.ToString();
            }
        }

        /// <summary>
        /// The double parameter value
        /// </summary>
#if WINDOWS
        [DisplayName("Value"), Description("The double parameter value."), Category("Definition")]
#endif
        [XmlIgnore]
        public double DoubleValue
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Type != ViewParameterType.Double) return 0;
                if (double.TryParse(Value, CultureInfo.InvariantCulture, out double doubleValue)) return doubleValue;
                return 0;
            }
            set
            {
                Type = ViewParameterType.Double;
                Value = value.ToString(CultureInfo.InvariantCulture);
            }
        }
        /// <summary>
                 /// The text parameter value
                 /// </summary>
#if WINDOWS
        [DisplayName("Value"), Description("The text parameter value."), Category("Definition")]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
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
            get
            {
                if (_enums == null) return null;

                var vals = new List<string>(_enums);
                if (_enumType != null)
                {
                    foreach (var val in _enumType.GetEnumValues())
                    {
                        //Add names
                        var desc = Helper.GetEnumDescription(EnumType, val);
                        if(desc == val.ToString()) desc = Helper.DBNameToDisplayName(desc);
                        vals.Add(val.ToString() + "|" + desc);
                    }
                }

                return vals.ToArray();
            }
            set
            {
                if (value != null) Type = ViewParameterType.Enum;
                _enums = value;
            }
        }

        /// <summary>
        /// If set, the enum values are taken from the Enum defined
        /// </summary>
        Type _enumType = null;

        public Type EnumType
        {
            get
            {
                return _enumType;
            }
            set
            {
                if (value != null) Type = ViewParameterType.Enum;
                _enumType = value;

            }
        }

        /// <summary>
        /// List of enum values if the parameter is an enum
        /// </summary>
        [XmlIgnore]
        public string[] EnumValues
        {
            get
            {
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
            get
            {
                List<string> result = new List<string>();
                foreach (var val in _enums)
                {
                    result.Add(val.Contains("|") ? val.Split('|')[1] : val);
                }
                return result.ToArray();
            }
        }

        /// <summary>
        /// The enum parameter value
        /// </summary>
#if WINDOWS
        [DisplayName("Value"), Description("The parameter value."), Category("Definition")]
        [TypeConverter(typeof(ViewParameterEnumConverter))]
#endif
        [XmlIgnore]
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
                if (Type == ViewParameterType.Boolean)
                {
                    if (string.IsNullOrEmpty(ConfigValue)) return false;
                    return bool.Parse(ConfigValue);
                }
                else if (Type == ViewParameterType.Numeric)
                {
                    if (string.IsNullOrEmpty(ConfigValue)) return 0;
                    return int.Parse(ConfigValue);
                }
                else if (Type == ViewParameterType.Double)
                {
                    if (string.IsNullOrEmpty(ConfigValue)) return 0;
                    return double.Parse(ConfigValue, CultureInfo.InvariantCulture);
                }
                return ConfigValue;
            }
        }

        /// <summary>
        /// Editor Helper: Reset parameter to its default value
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Reset value"), Description("Reset parameter to its default value.")]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
#endif
        public string HelperResetParameterValue
        {
            get { return "<Click to reset to the default value>"; }
        }

        /// <summary>
        /// For an enum, returns the display text from the value
        /// </summary>
        public string EnumGetDisplayFromValue(string value)
        {
            int index = EnumValues.ToList().FindIndex(i => i == value);
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

        /// <summary>
        /// Init parameter values from a reference 
        /// </summary>
        public void InitFromConfiguration(Parameter configuration)
        {
            Name = configuration.Name;
            Enums = configuration.Enums;
            Description = configuration.Description;
            Type = configuration.Type;
            UseOnlyEnumValues = configuration.UseOnlyEnumValues;
            DisplayName = configuration.DisplayName;
            ConfigValue = configuration.Value;
            EditorLanguage = configuration.EditorLanguage;
            TextSamples = configuration.TextSamples;
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
#if WINDOWS
        [DisplayName("Use custom value"), Description("If true, a custom parameter value is used when the report is executed for the output."), Category("Definition")]
        [DefaultValue(false)]
#endif
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
    /// SecurityParameter are Parameters used to define the security
    /// </summary>
    public class SecurityParameter : Parameter
    {
    }
}

