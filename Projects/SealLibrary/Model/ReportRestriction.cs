//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Reflection;
using System.Globalization;
using DynamicTypeDescriptor;
using System.ComponentModel.Design;
using System.Drawing.Design;
using Seal.Helpers;
using Seal.Forms;
using System.IO;

namespace Seal.Model
{
    /// <summary>
    /// A ReportRestriction defines a restriction applied to a report model. A report restriction is a child of a ReportElement.
    /// </summary>
    [ClassResource(BaseName = "DynamicTypeDescriptorApp.Properties.Resources", KeyPrefix = "ReportRestriction_")]
    public class ReportRestriction : ReportElement
    {
        #region Editor
        /// <summary>
        /// Update editor attributes in the property grid
        /// </summary>
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Prompt").SetIsBrowsable(true);
                GetProperty("Required").SetIsBrowsable(true);
                GetProperty("Operator").SetIsBrowsable(!IsInputValue && !IsCommonValue);
                GetProperty("OperatorStyle").SetIsBrowsable(!IsInputValue && !IsCommonValue);
                GetProperty("ShowName").SetIsBrowsable(IsInputValue || IsCommonValue);
                GetProperty("DisplayNameEl").SetIsBrowsable(true);
                GetProperty("SQL").SetIsBrowsable(!IsInputValue && !IsCommonValue);
                GetProperty("SQL").SetDisplayName(IsSQL ? "Custom SQL" : "Custom Expression");
                GetProperty("SQL").SetDescription(IsSQL ? "If not empty, overwrite the default SQL used for the restriction in the WHERE clause." : "If not empty, overwrite the default LINQ Expression used for the restriction in the LINQ query.");
                GetProperty("CaseSensitive").SetIsBrowsable(!IsSQL && IsText);                

                GetProperty("FormatRe").SetIsBrowsable(!IsEnum);
                GetProperty("TypeRe").SetIsBrowsable(true);
                GetProperty("OperatorLabel").SetIsBrowsable(!IsInputValue && !IsCommonValue);
                GetProperty("EnumGUIDRE").SetIsBrowsable(true);
                GetProperty("InputRows").SetIsBrowsable((IsText || IsNumeric) && !IsEnum);
                GetProperty("DisplayOrderRE").SetIsBrowsable(true);
                GetProperty("AllowAPI").SetIsBrowsable(true);
                GetProperty("TriggerExecution").SetIsBrowsable(true);
                //Conditional
                if (IsEnum)
                {
                    GetProperty("EnumValue").SetIsBrowsable(true);
                    GetProperty("FirstSelection").SetIsBrowsable(true);
                    GetProperty("EnumLayout").SetIsBrowsable(true);
                }
                else if (IsDateTime)
                {
                    GetProperty("Date1").SetIsBrowsable(true);
                    GetProperty("Date2").SetIsBrowsable(true);
                    GetProperty("Date3").SetIsBrowsable(true);
                    GetProperty("Date4").SetIsBrowsable(true);
                    GetProperty("Date1Keyword").SetIsBrowsable(true);
                    GetProperty("Date2Keyword").SetIsBrowsable(true);
                    GetProperty("Date3Keyword").SetIsBrowsable(true);
                    GetProperty("Date4Keyword").SetIsBrowsable(true);
                }
                else
                {
                    GetProperty("Value1").SetIsBrowsable(true);
                    GetProperty("Value2").SetIsBrowsable(true);
                    GetProperty("Value3").SetIsBrowsable(true);
                    GetProperty("Value4").SetIsBrowsable(true);
                }

                if (!IsEnum)
                {
                    GetProperty("NumericStandardFormatRe").SetIsBrowsable(IsNumeric);
                    GetProperty("DateTimeStandardFormatRe").SetIsBrowsable(IsDateTime);
                }

                //Readonly
                foreach (var property in Properties) property.SetIsReadOnly(false);

                GetProperty("FormatRe").SetIsReadOnly((IsNumeric && NumericStandardFormat != NumericStandardFormat.Custom) || (IsDateTime && DateTimeStandardFormat != DateTimeStandardFormat.Custom));
                if (_operator == Operator.IsNull || _operator == Operator.IsNotNull || _operator == Operator.IsEmpty || _operator == Operator.IsNotEmpty)
                {
                    GetProperty("Value1").SetIsReadOnly(true);
                    GetProperty("Date1").SetIsReadOnly(true);
                    GetProperty("Date1Keyword").SetIsReadOnly(true);
                    GetProperty("EnumValue").SetIsReadOnly(true);
                }

                if (IsGreaterSmallerOperator || _operator == Operator.IsNull || _operator == Operator.IsNotNull || _operator == Operator.IsEmpty || _operator == Operator.IsNotEmpty || _operator == Operator.ValueOnly)
                {
                    GetProperty("Value2").SetIsReadOnly(true);
                    GetProperty("Date2").SetIsReadOnly(true);
                    GetProperty("Date2Keyword").SetIsReadOnly(true);
                }

                if (_operator == Operator.Between || _operator == Operator.NotBetween || IsGreaterSmallerOperator || _operator == Operator.IsNull || _operator == Operator.IsNotNull || _operator == Operator.IsEmpty || _operator == Operator.IsNotEmpty || _operator == Operator.ValueOnly)
                {
                    GetProperty("Value3").SetIsReadOnly(true);
                    GetProperty("Date3").SetIsReadOnly(true);
                    GetProperty("Date3Keyword").SetIsReadOnly(true);

                    GetProperty("Value4").SetIsReadOnly(true);
                    GetProperty("Date4").SetIsReadOnly(true);
                    GetProperty("Date4Keyword").SetIsReadOnly(true);
                }

                GetProperty("Required").SetIsReadOnly(Prompt == PromptType.None);
                GetProperty("ChangeOperator").SetIsReadOnly(Prompt == PromptType.None);

                //Aggregate restriction
                if (PivotPosition == PivotPosition.Data && !(MetaColumn != null && MetaColumn.IsAggregate))
                {
                    GetProperty("AggregateFunction").SetIsBrowsable(true);
                }

                if (!GetProperty("Date1Keyword").IsReadOnly) GetProperty("Date1").SetIsReadOnly(HasDateKeyword(Date1Keyword));
                if (!GetProperty("Date2Keyword").IsReadOnly) GetProperty("Date2").SetIsReadOnly(HasDateKeyword(Date2Keyword));
                if (!GetProperty("Date3Keyword").IsReadOnly) GetProperty("Date3").SetIsReadOnly(HasDateKeyword(Date3Keyword));
                if (!GetProperty("Date4Keyword").IsReadOnly) GetProperty("Date4").SetIsReadOnly(HasDateKeyword(Date4Keyword));

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        /// <summary>
        /// Name of the restriction
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Start character of a restriction
        /// </summary>
        public const char kStartRestrictionChar = '[';
        /// <summary>
        /// Stop character of a restriction
        /// </summary>
        public const char kStopRestrictionChar = ']';

        /// <summary>
        /// Create a default report restriction
        /// </summary>
        public static ReportRestriction CreateReportRestriction()
        {
            return new ReportRestriction()
            {
                GUID = Guid.NewGuid().ToString(),
                _type = ColumnType.Default,
                _numericStandardFormat = NumericStandardFormat.Default,
                _datetimeStandardFormat = DateTimeStandardFormat.Default
            };
        }

        /// <summary>
        /// Define if the value of the restriction is prompted to the user when the report is executed
        /// </summary>
        [DefaultValue(PromptType.None)]
        [Category("Definition"), DisplayName("\tPrompt restriction"), Description("Define if the value of the restriction is prompted to the user when the report is executed."), Id(5, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public PromptType Prompt { get; set; } = PromptType.None;

        Operator _operator = Operator.Equal;
        /// <summary>
        /// The Operator used for the restriction. If Value Only is selected, the restriction is replaced by the value only (with no column name and operator).
        /// </summary>
        [DefaultValue(Operator.Equal)]
        [TypeConverter(typeof(RestrictionOperatorConverter))]
        [Category("Definition"), DisplayName("Operator"), Description("The Operator used for the restriction. If Value Only is selected, the restriction is replaced by the value only (with no column name and operator)."), Id(10, 1)]
        public Operator Operator
        {
            get { return _operator; }
            set
            {
                _operator = value;
                UpdateEditorAttributes();
            }
        }


        /// <summary>
        /// If not empty, overwrite the operator display text
        /// </summary>
        [Category("Definition"), DisplayName("Operator Label"), Description("If not empty, overwrite the operator display text."), Id(11, 1)]
        public string OperatorLabel { get; set; }

        bool _changeOperator = true;
        /// <summary>
        /// If true, the operator can be changed when the restriction is prompted. Deprecated, kept for backward compatibility and will be removed in future versions, if false -> OperatorStyle is set to RestrictionOperatorStyle.NotModifiable
        /// </summary>
        public bool ChangeOperator
        {
            get { return _changeOperator; }
            set
            {
                if (!value)
                {
                    OperatorStyle = RestrictionOperatorStyle.NotModifiable;
                }
                _changeOperator = true; //Back to default
            }
        }

        /// <summary>
        /// How the element name and restriction operator is displayed or not.
        /// </summary>
        [DefaultValue(RestrictionOperatorStyle.Visible)]
        [Category("Definition"), DisplayName("Operator style"), Description("How the restriction operator is displayed or not."), Id(12, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public RestrictionOperatorStyle OperatorStyle { get; set; } = RestrictionOperatorStyle.Visible;
        public bool ShouldSerializeOperatorStyle() { return OperatorStyle != RestrictionOperatorStyle.Visible; }


        /// <summary>
        /// Control of the OperatorStyle dedicated for Input Values or Common Restrictions
        /// </summary>
        [DefaultValue(true)]
        [Category("Definition"), DisplayName("Show name"), Description("Show or hide the element name for the restriction."), Id(12, 1)]
        [XmlIgnore]
        public bool ShowName
        {
            get { return OperatorStyle != RestrictionOperatorStyle.NotVisible; }
            set
            {
                OperatorStyle = value ? RestrictionOperatorStyle.NotModifiable : RestrictionOperatorStyle.NotVisible;
            }
        }


        /// <summary>
        /// If true and the restriction is prompted, a value is required to execute the report
        /// </summary>
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("Is required"), Description("If true and the restriction is prompted, a value is required to execute the report."), Id(18, 1)]
        public bool Required { get; set; } = false;


        /// <summary>
        /// Sort order used for the display of the prompted restrictions when the report is executed.
        /// </summary>
        [DefaultValue(1)]
        [Category("Definition"), DisplayName("Display Order"), Description("Order used for the display of the prompted restrictions when the report is executed."), Id(20, 1)]
        public int DisplayOrderRE
        {
            get { return DisplayOrder; }
            set { DisplayOrder = value; }
        }

        /// <summary>
        /// The final enumerated list of the restriction
        /// </summary>
        [XmlIgnore]
        public MetaEnum EnumRE
        {
            get
            {
                MetaEnum result = null;
                if (Enum != null) result = Enum;
                else if (IsCommonRestrictionValue) return null;

                if (result == null) result = MetaColumn.Enum;

                if (result != null && result.IsDynamic && result.Values.Count == 0 && string.IsNullOrEmpty(result.Error))
                {
                    result.RefreshEnum();
                }
                return result;
            }
        }

        /// <summary>
        /// Initialize the Html Ids of the enum values of the restriction
        /// </summary>
        public void SetEnumHtmlIds()
        {
            if (IsEnum)
            {
                int i = 0;
                foreach (var enumDef in EnumRE.Values) enumDef.HtmlId = (i++).ToString();
            }
        }

        /// <summary>
        /// List of prompted enum values
        /// </summary>
        [XmlIgnore]
        public List<MetaEV> PromptedEnumValues
        {
            get
            {
                if (EnumRE == null) return new List<MetaEV>();

                if (!EnumRE.HasDynamicDisplay) return EnumRE.Values;

                //Add only selected values...
                var result = new List<MetaEV>();
                foreach (var v in EnumRE.Values)
                {
                    if (EnumValues.Contains(v.Id))
                    {
                        result.Add(v);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// True if the source is No SQL
        /// </summary>
        [XmlIgnore]
        public bool IsNoSQL
        {
            get
            {
                return Source != null && Source.IsNoSQL;
            }
        }

        /// <summary>
        /// True if the restriction is an input value
        /// </summary>
        [XmlIgnore]
        public bool IsInputValue
        {
            get
            {
                return Source == null;
            }
        }

        /// <summary>
        /// True is the restriction is an enumerated list
        /// </summary>
        [XmlIgnore]
        public bool IsEnumRE
        {
            get
            {
                if (Enum != null) return true;
                return IsEnum;
            }
        }

        /// <summary>
        /// Number of lines for the first input
        /// </summary>
        [XmlIgnore]
        public int InputRows1
        {
            get
            {
                if (IsText || IsNumeric)
                {
                    if (InputRows > 0) return InputRows;
                    else if (!string.IsNullOrEmpty(Value1) && Value1.Contains("\n")) return 2;
                }
                return 0;
            }
        }

        /// <summary>
        /// Operator label translated
        /// </summary>
        public string GetOperatorLabel(Operator op)
        {
            if (Operator == Operator.ValueOnly || Model == null) return OperatorLabel;
            return Report.Translate(Helper.GetEnumDescription(typeof(Operator), op));
        }

        /// <summary>
        /// Check an input value, throw an exception in case of error
        /// </summary>
        /// <param name="value"></param>
        void CheckInputValue(string value)
        {
            if (Source == null) return;
            if (IsNumeric && !string.IsNullOrEmpty(value))
            {
                foreach (var val in GetVals(value))
                {
                    double d;
                    if (!Helper.ValidateNumeric(val, out d))
                    {
                        throw new Exception("Invalid numeric value: " + val);
                    }
                }
            }
        }

        /// <summary>
        /// Set a restriction value for navigation
        /// </summary>
        public void SetNavigationValue(string val)
        {
            if (IsEnum) EnumValues.Add(val);
            else if (IsDateTime) Date1 = DateTime.FromOADate(double.Parse(val, CultureInfo.InvariantCulture));
            else Value1 = val;
        }

        /// <summary>
        /// List of allowed operators for this restriction
        /// </summary>
        [XmlIgnore]
        public List<Operator> AllowedOperators
        {
            get
            {
                List<Operator> result = new List<Operator>();
                result.Add(Operator.Equal);
                result.Add(Operator.NotEqual);

                if (IsText && !IsEnum)
                {
                    result.Add(Operator.Contains);
                    result.Add(Operator.NotContains);
                    result.Add(Operator.StartsWith);
                    result.Add(Operator.EndsWith);
                    result.Add(Operator.IsEmpty);
                    result.Add(Operator.IsNotEmpty);
                }

                if ((IsSQL && !IsEnum) || (!IsSQL && (IsNumeric || IsDateTime)))
                {
                    result.Add(Operator.Between);
                    result.Add(Operator.NotBetween);
                    result.Add(Operator.Smaller);
                    result.Add(Operator.SmallerEqual);
                    result.Add(Operator.Greater);
                    result.Add(Operator.GreaterEqual);
                }

                result.Add(Operator.IsNull);
                result.Add(Operator.IsNotNull);
                result.Add(Operator.ValueOnly);
                return result;
            }
        }

        /// <summary>
        /// List of operators to display
        /// </summary>
        [XmlIgnore]
        public List<Operator> AllowedDisplayOperators
        {
            get
            {
                if (Operator == Operator.ValueOnly) return AllowedOperators.Where(i => i == Operator.ValueOnly).ToList();
                return AllowedOperators.Where(i => i != Operator.ValueOnly).ToList();

            }
        }


        string _value1;
        /// <summary>
        /// Value used for the restriction. Multiple values can be set (one per line).
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 1"), Description("Value used for the restriction. Multiple values can be set (one per line)."), Id(1, 3)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string Value1
        {
            get { return _value1; }
            set
            {
                CheckInputValue(value);
                _value1 = value;
            }
        }

        string _value2;
        /// <summary>
        /// Second value used for the restriction
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 2"), Description("Second value used for the restriction."), Id(3, 3)]
        public string Value2
        {
            get { return _value2; }
            set
            {
                CheckInputValue(value);
                _value2 = value;
            }
        }

        string _value3;
        /// <summary>
        /// Third value used for the restriction
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 3"), Description("Third value used for the restriction."), Id(5, 3)]
        public string Value3
        {
            get { return _value3; }
            set
            {
                CheckInputValue(value);
                _value3 = value;
            }
        }

        string _value4;
        /// <summary>
        /// Fourth value used for the restriction
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 4"), Description("Fourth value used for the restriction."), Id(7, 3)]
        public string Value4
        {
            get { return _value4; }
            set
            {
                CheckInputValue(value);
                _value4 = value;
            }
        }

        /// <summary>
        /// Value as a double?
        /// </summary>
        public double? DoubleValue
        {
            get
            {
                if (IsDateTime || !HasValue1) return null;
                double result;
                if (double.TryParse(Value1.ToString(), out result)) return result;
                return null;
            }
        }

        /// <summary>
        /// First restriction value
        /// </summary>
        public object FirstValue
        {
            get
            {
                object result = null;
                if (IsEnum)
                {
                    if (EnumValues.Count > 0) result = EnumValues[0];
                }
                else if (IsNumeric)
                {
                    result = DoubleValue;
                }
                else if (IsDateTime)
                {
                    result = FinalDate1;
                }
                else result = Value1;
                return result;
            }
        }

        /// <summary>
        /// First string value
        /// </summary>
        public string FirstStringValue
        {
            get
            {
                var result = FirstValue;
                if (result != null) return result.ToString();
                return null;
            }
        }

        /// <summary>
        /// First numeric value
        /// </summary>
        public double? FirstNumericValue
        {
            get
            {
                if (IsNumeric) return DoubleValue;
                return null;
            }
        }

        /// <summary>
        /// First Date Time value
        /// </summary>
        public DateTime? FirstDateValue
        {
            get
            {
                if (IsDateTime) return FinalDate1;
                return null;
            }
        }

        /// <summary>
        /// Enumerated values for the restriction
        /// </summary>
        public List<string> EnumValues { get; set; } = new List<string>();
        public bool ShouldSerializeEnumValues() { return EnumValues.Count > 0; }

        /// <summary>
        /// Helper to edit the enumerated values
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value"), Description("Value used for the restriction."), Id(1, 3)]
        [Editor(typeof(RestrictionEnumValuesEditor), typeof(UITypeEditor))]
        [XmlIgnore]
        public string EnumValue
        {
            get { return "<Click to edit values>"; }
            set { } //keep set for modification handler
        }

        /// <summary>
        /// Value used for the restriction (DateTime)
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 1"), Description("Value used for the restriction."), Id(1, 3)]
        [TypeConverter(typeof(RestrictionDateConverter))]
        public DateTime Date1 { get; set; }
        public bool ShouldSerializeDate1() { return Date1 != DateTime.MinValue; }

        /// <summary>
        /// Second value used for the restriction (DateTime)
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 2"), Description("Second value used for the restriction."), Id(3, 3)]
        [TypeConverter(typeof(RestrictionDateConverter))]
        public DateTime Date2 { get; set; }
        public bool ShouldSerializeDate2() { return Date2 != DateTime.MinValue; }

        /// <summary>
        /// Third value used for the restriction (DateTime)
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 3"), Description("Third value used for the restriction."), Id(5, 3)]
        [TypeConverter(typeof(RestrictionDateConverter))]
        public DateTime Date3 { get; set; }
        public bool ShouldSerializeDate3() { return Date3 != DateTime.MinValue; }

        /// <summary>
        /// Fourth value used for the restriction (DateTime)
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 4"), Description("Fourth value used for the restriction."), Id(7, 3)]
        [TypeConverter(typeof(RestrictionDateConverter))]
        public DateTime Date4 { get; set; }
        public bool ShouldSerializeDate4() { return Date4 != DateTime.MinValue; }

        const string DateKeywordDescription = "Date keyword can be used to specify relative date and time for the restriction value. From the chosen keyword, operations +/- are allowed with the following units: Y(Year), S(Semester), Q(Quarter), M(Month), D(Day), h(hour), m(minute), s(second)";
        /// <summary>
        /// Date1 Keyword value
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 1 Keyword"), Description(DateKeywordDescription), Id(2, 3)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date1Keyword { get; set; }

        /// <summary>
        /// Date2 Keyword value
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 2 Keyword"), Description(DateKeywordDescription), Id(4, 3)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date2Keyword { get; set; }

        /// <summary>
        /// Date3 Keyword value
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 3 Keyword"), Description(DateKeywordDescription), Id(6, 3)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date3Keyword { get; set; }

        /// <summary>
        /// Date4 Keyword value
        /// </summary>
        [Category("Restriction Values"), DisplayName("Value 4 Keyword"), Description(DateKeywordDescription), Id(8, 3)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date4Keyword { get; set; }


        /// <summary>
        /// Layout of the restriction for the values of the enumerated list: Either a select list or buttons (for a small number of values).
        /// </summary>
        [DefaultValue(RestrictionLayout.SelectWithFilter)]
        [Category("Restriction Values"), DisplayName("Enum layout"), Description("Layout of the restriction for the values of the enumerated list: Either a select list or buttons (for a small number of values)."), Id(9, 3)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public RestrictionLayout EnumLayout { get; set; } = RestrictionLayout.SelectWithFilter;
        public bool ShouldSerializeEnumLayout() { return EnumLayout != RestrictionLayout.SelectWithFilter; }

        /// <summary>
        /// If set, the values are selected for the first report execution: All values, first or last value. This may be used for dynamic list.
        /// </summary>
        [DefaultValue(FirstEnumSelection.None)]
        [Category("Restriction Values"), DisplayName("First selection"), Description("If set, the values are selected for the first report execution: All values, first or last value. This may be used for dynamic list."), Id(10, 3)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public FirstEnumSelection FirstSelection { get; set; } = FirstEnumSelection.None;
        public bool ShouldSerializeFirstSelection() { return FirstSelection != FirstEnumSelection.None; }

        /// <summary>
        /// If set, the values are selected for the first report execution: All values, first or last value. This may be used for dynamic list.
        /// </summary>
        [DefaultValue(false)]
        [Category("Restriction Values"), DisplayName("Trigger execution"), Description("If true, the report is executed or updated when a value is selected."), Id(12, 3)]
        public bool TriggerExecution { get; set; } = false;
        public bool ShouldSerializeTriggerExecution() { return TriggerExecution; }

        /// <summary>
        /// If not empty, overwrite the default SQL used for the restriction in the WHERE clause
        /// </summary>
        [Category("Advanced"), DisplayName("Custom SQL"), Description("If not empty, overwrite the default SQL used for the restriction in the WHERE clause."), Id(1, 4)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public new string SQL
        {
            get
            {
                return _SQL;
            }
            set { _SQL = value; }
        }

        /// <summary>
        /// Data type of the restriction
        /// </summary>
        [DefaultValue(ColumnType.Default)]
        [Category("Advanced"), DisplayName("Data Type"), Description("Data type of the restriction."), Id(2, 4)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public ColumnType TypeRe
        {
            get { return _type; }
            set
            {
                Type = value;
                UpdateEditorAttributes();
            }
        }
        public bool ShouldSerializeTypeRe() { return _type != ColumnType.Default; }

        /// <summary>
        /// Standard display format applied to the restriction display value
        /// </summary>
        [DefaultValue(DateTimeStandardFormat.Default)]
        [Category("Advanced"), DisplayName("Format"), Description("Standard display format applied to the restriction display value."), Id(3, 4)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public DateTimeStandardFormat DateTimeStandardFormatRe
        {
            get { return _datetimeStandardFormat; }
            set
            {
                if (_dctd != null && _datetimeStandardFormat != value)
                {
                    _datetimeStandardFormat = value;
                    SetStandardFormat();
                    UpdateEditorAttributes();
                }
                else
                    _datetimeStandardFormat = value;
            }
        }
        public bool ShouldSerializeDateTimeStandardFormatRe() { return _datetimeStandardFormat != DateTimeStandardFormat.Default; }

        /// <summary>
        /// Standard display format applied to the restriction display value
        /// </summary>
        [DefaultValue(NumericStandardFormat.Default)]
        [Category("Advanced"), DisplayName("Format"), Description("Standard display format applied to the restriction display value."), Id(4, 4)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public NumericStandardFormat NumericStandardFormatRe
        {
            get { return _numericStandardFormat; }
            set
            {
                if (_dctd != null && _numericStandardFormat != value)
                {
                    _numericStandardFormat = value;
                    SetStandardFormat();
                    UpdateEditorAttributes();
                }
                else
                    _numericStandardFormat = value;
            }
        }

        /// <summary>
        /// If not empty, specify the format of the restriction display values (.Net Format Strings)
        /// </summary>
        [Category("Advanced"), DisplayName("Custom Format"), Description("If not empty, specify the format of the restriction display values (.Net Format Strings)."), Id(5, 4)]
        [TypeConverter(typeof(CustomFormatConverter))]
        public string FormatRe
        {
            get
            {
                SetDefaultFormat();
                return _format;
            }
            set
            {
                _format = value;
            }
        }
        public bool ShouldSerializeFormatRe() { return !string.IsNullOrEmpty(_format); }

        /// <summary>
        /// If greater than 0, specifies the number of lines available to edit the first restriction value (only valid for text or numeric when the restriction is prompted)
        /// </summary>
        [DefaultValue(0)]
        [Category("Advanced"), DisplayName("Input lines for first value"), Description("If greater than 0, specifies the number of lines available to edit the first restriction value (only valid for text or numeric when the restriction is prompted)."), Id(7, 4)]
        public int InputRows { get; set; } = 0;

        /// <summary>
        /// If defined, the restriction values are selected using the enumerated list
        /// </summary>
        [DefaultValue(null)]
        [Category("Advanced"), DisplayName("Custom Enumerated List"), Description("If defined, the restriction values are selected using the enumerated list."), Id(9, 4)]
        [TypeConverter(typeof(MetaEnumConverter))]
        public string EnumGUIDRE
        {
            get { return _enumGUID; }
            set { _enumGUID = value; }
        }

        /// <summary>
        /// If True, the restriction text is case sensitive in the LINQ where clause.
        /// </summary>
        [DefaultValue(false)]
        [Category("Advanced"), DisplayName("Is case sensitive"), Description("If True, the restriction text is case sensitive in the LINQ where clause."), Id(10, 4)]
        public bool CaseSensitive { get; set; } = false;
        public bool ShouldSerializeCaseSensitive() { return CaseSensitive; }

        /// <summary>
        /// If True, the restriction can be modified through the Web API, even if the restriction is not prompted.
        /// </summary>
        [DefaultValue(false)]
        [Category("Advanced"), DisplayName("Allow modifications through API"), Description("If True, the restriction can be modified through the Web API, even if the restriction is not prompted."), Id(11, 4)]
        public bool AllowAPI { get; set; } = false;
        public bool ShouldSerializeAllowAPI() { return AllowAPI; }


        /// <summary>
        /// True if the restriction has a value
        /// </summary>
        public bool HasValue
        {
            get
            {
                return Operator == Operator.IsNull
                    || Operator == Operator.IsNotNull
                    || Operator == Operator.IsEmpty
                    || Operator == Operator.IsNotEmpty
                    || (IsEnum && EnumValues.Count > 0)
                    || (!IsEnum && HasValue1)
                    || (!IsEnum && HasValue2 && !IsGreaterSmallerOperator)
                    || (!IsEnum && HasValue3 && !IsGreaterSmallerOperator && !IsBetweenOperator)
                    || (!IsEnum && HasValue4 && !IsGreaterSmallerOperator && !IsBetweenOperator);
            }
        }

        /// <summary>
        /// True if the restriction has a value 1
        /// </summary>
        public bool HasValue1
        {
            get
            {
                return
                    (
                    IsDateTime && (HasDateKeyword(Date1Keyword) || Date1 != DateTime.MinValue)
                    ||
                    (!IsDateTime && !string.IsNullOrEmpty(Value1))
                    );
            }
        }

        /// <summary>
        /// True if the restriction has a value 2
        /// </summary>
        public bool HasValue2
        {
            get
            {
                return
                    (
                    IsDateTime && (HasDateKeyword(Date2Keyword) || Date2 != DateTime.MinValue)
                    ||
                    (!IsDateTime && !string.IsNullOrEmpty(Value2))
                    );
            }
        }

        /// <summary>
        /// True if the restriction has a value 3
        /// </summary>
        public bool HasValue3
        {
            get
            {
                return
                    (
                    IsDateTime && (HasDateKeyword(Date3Keyword) || Date3 != DateTime.MinValue)
                    ||
                    (!IsDateTime && !string.IsNullOrEmpty(Value3))
                    );
            }
        }

        /// <summary>
        /// True if the restriction has a value 4
        /// </summary>
        public bool HasValue4
        {
            get
            {
                return
                    (
                    IsDateTime && (HasDateKeyword(Date4Keyword) || Date4 != DateTime.MinValue)
                    ||
                    (!IsDateTime && !string.IsNullOrEmpty(Value4))
                    );
            }
        }

        /// <summary>
        /// Helper to find if a string has a date keyword
        /// </summary>
        static public bool HasDateKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return false;

            return
                keyword.StartsWith(DateRestrictionKeyword.Now.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.ThisMinute.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.ThisHour.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.Today.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.ThisWeek.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.ThisMonth.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.ThisQuarter.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.ThisSemester.ToString()) ||
                keyword.StartsWith(DateRestrictionKeyword.ThisYear.ToString())
                ;
        }

        static char[] DateUnits = new char[] { 's', 'm', 'h', 'D', 'W', 'M', 'Q', 'S', 'Y' };
        DateTime CalcFinalDate(DateTime start, string input, DateRestrictionKeyword keyword, char def)
        {
            string val = input.Replace(keyword.ToString(), "").Replace(" ", "").Replace("+", "§+").Replace("-", "§-");
            var vals = val.Split('§');

            foreach (var v in vals.Where(i => !string.IsNullOrEmpty(i)))
            {
                var index = v.IndexOfAny(DateUnits);
                double vNum = 0;
                char unit = def;
                if (index > 0)
                {
                    double.TryParse(v.Substring(0, index).Trim(), out vNum);
                    unit = v[index];
                }
                else
                {
                    double.TryParse(v.Trim(), out vNum);
                }

                if (vNum == 0) continue;

                switch (unit)
                {
                    case 's':
                        start = start.AddSeconds(vNum);
                        break;
                    case 'm':
                        start = start.AddMinutes(vNum);
                        break;
                    case 'h':
                        start = start.AddHours(vNum);
                        break;
                    case 'D':
                        start = start.AddDays(vNum);
                        break;
                    case 'W':
                        start = start.AddDays(7 * vNum);
                        break;
                    case 'M':
                        start = start.AddMonths(Convert.ToInt32(vNum));
                        break;
                    case 'Q':
                        start = start.AddMonths(Convert.ToInt32(3 * vNum));
                        break;
                    case 'S':
                        start = start.AddMonths(Convert.ToInt32(6 * vNum));
                        break;
                    case 'Y':
                        start = start.AddYears(Convert.ToInt32(vNum));
                        break;
                }
            }
            return start;
        }


        DateTime GetFinalDate(string dateKeyword, DateTime date)
        {
            DateTime result = date;
            if (!string.IsNullOrEmpty(dateKeyword))
            {
                if (dateKeyword.StartsWith(DateRestrictionKeyword.Now.ToString()))
                {
                    result = CalcFinalDate(DateTime.Now, dateKeyword, DateRestrictionKeyword.Now, 's');
                }
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.ThisMinute.ToString()))
                {
                    result = CalcFinalDate(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0), dateKeyword, DateRestrictionKeyword.ThisMinute, 'm');
                }
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.ThisHour.ToString()))
                {
                    result = CalcFinalDate(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, DateTime.Now.Hour, 0, 0), dateKeyword, DateRestrictionKeyword.ThisHour, 'h');
                }
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.Today.ToString()))
                {
                    result = CalcFinalDate(DateTime.Today, dateKeyword, DateRestrictionKeyword.Today, 'D');
                }
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.ThisWeek.ToString()))
                {
                    //First monday of the week...
                    result = CalcFinalDate(DateTime.Today.AddDays(1 - (int)DateTime.Today.DayOfWeek), dateKeyword, DateRestrictionKeyword.ThisWeek, 'W');

                }
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.ThisMonth.ToString()))
                {
                    result = CalcFinalDate(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), dateKeyword, DateRestrictionKeyword.ThisMonth, 'M');
                }
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.ThisQuarter.ToString()))
                {
                    int thisQuarter = (DateTime.Today.Month + 2) / 3;
                    result = CalcFinalDate(new DateTime(DateTime.Today.Year, 1 + 3 * (thisQuarter - 1), 1), dateKeyword, DateRestrictionKeyword.ThisQuarter, 'Q');
                }
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.ThisSemester.ToString()))
                {
                    result = CalcFinalDate(new DateTime(DateTime.Today.Year, DateTime.Today.Month >= 7 ? 7 : 1, 1), dateKeyword, DateRestrictionKeyword.ThisSemester, 'S');
                }
                else if (dateKeyword.StartsWith(DateRestrictionKeyword.ThisYear.ToString()))
                {
                    result = CalcFinalDate(new DateTime(DateTime.Today.Year, 1, 1), dateKeyword, DateRestrictionKeyword.ThisYear, 'Y');
                }
            }
            else if (date == DateTime.MinValue) result = DateTime.Now;
            return result;
        }

        /// <summary>
        /// Final Date Time value 1
        /// </summary>
        public DateTime FinalDate1
        {
            get
            {
                return GetFinalDate(Date1Keyword, Date1);
            }
        }

        /// <summary>
        /// Final Date Time value 2
        /// </summary>
        public DateTime FinalDate2
        {
            get
            {
                return GetFinalDate(Date2Keyword, Date2);
            }
        }

        /// <summary>
        /// Final Date Time value 3
        /// </summary>
        public DateTime FinalDate3
        {
            get
            {
                return GetFinalDate(Date3Keyword, Date3);
            }
        }

        /// <summary>
        /// Final Date Time value 4
        /// </summary>
        public DateTime FinalDate4
        {
            get
            {
                return GetFinalDate(Date4Keyword, Date4);
            }
        }

        /// <summary>
        /// String containing all the enumerated values with their display labels
        /// </summary>
        public string EnumDisplayValue
        {
            get
            {
                string result = "";
                if (IsEnum)
                {
                    foreach (string enumValue in EnumValues)
                    {
                        Helper.AddValue(ref result, Report.ExecutionView.CultureInfo.TextInfo.ListSeparator, Report.EnumDisplayValue(EnumRE, enumValue, true));
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Final display value 1
        /// </summary>
        public string DisplayValue1
        {
            get
            {
                return GetDisplayValue(Value1, Date1);
            }
        }

        /// <summary>
        /// Final display value 2
        /// </summary>
        public string DisplayValue2
        {
            get
            {
                return GetDisplayValue(Value2, Date2);
            }
        }

        /// <summary>
        /// Final display value 3
        /// </summary>
        public string DisplayValue3
        {
            get
            {
                return GetDisplayValue(Value3, Date3);
            }
        }

        /// <summary>
        /// Final display value 4
        /// </summary>
        public string DisplayValue4
        {
            get
            {
                return GetDisplayValue(Value4, Date4);
            }
        }

        /// <summary>
        /// Helper to return the display value
        /// </summary>
        string GetDisplayValue(string value, DateTime date)
        {
            string result = "";
            if (IsNumeric)
            {
                if (string.IsNullOrEmpty(value)) result = "0";
                else
                {
                    foreach (var val in GetVals(value))
                    {
                        if (string.IsNullOrEmpty(val)) continue;
                        if (!string.IsNullOrEmpty(result)) result += ";";

                        double d;
                        Helper.ValidateNumeric(val, out d);
                        result += ElementDisplayValue(d);
                    }
                }
            }
            else if (IsDateTime)
            {
                if (date == DateTime.MinValue) date = DateTime.Now;
                result = "'" + ElementDisplayValue(date) + "'";
            }
            else
            {
                if (string.IsNullOrEmpty(value)) value = "";
                else
                {
                    foreach (var val in GetVals(value))
                    {
                        if (string.IsNullOrEmpty(val)) continue;
                        if (!string.IsNullOrEmpty(result)) result += ";";
                        result += "'" + ElementDisplayValue(val) + "'";
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Display value for navigation
        /// </summary>
        /// <returns></returns>
        public string GeNavigationDisplayValue()
        {
            var result = IsEnum ? EnumDisplayValue : GetDisplayValue(Value1, Date1);
            if (result.Length > 2 && result[0] == '\'' && result[result.Length - 1] == '\'') result = result.Substring(1, result.Length - 2);
            return result;
        }

        string GetDisplayRestriction(string value, string dateKeyword, DateTime date)
        {
            string result = "";
            if (IsDateTime)
            {
                if (!string.IsNullOrEmpty(dateKeyword)) result = Helper.QuoteSingle(dateKeyword);
                else result = GetDisplayValue(null, date);
            }
            else
            {
                result = GetDisplayValue(value, DateTime.MinValue);
            }
            return result;
        }

        /// <summary>
        /// Helper to return the display value of an enum from its id
        /// </summary>
        public string GetEnumDisplayValue(string id)
        {
            return Report.EnumDisplayValue(EnumRE, id, true);
        }

        /// <summary>
        /// Enum message defined in the MetaData
        /// </summary>
        /// <returns></returns>
        public string GetEnumMessage()
        {
            return Report.EnumMessage(EnumRE);
        }

        /// <summary>
        /// True is the operastor is Greater or Smaller
        /// </summary>
        public bool IsGreaterSmallerOperator
        {
            get { return _operator == Operator.Greater || _operator == Operator.GreaterEqual || _operator == Operator.Smaller || _operator == Operator.SmallerEqual; }
        }

        /// <summary>
        /// True is the operastor is Contain (or Not),Starts, Ends
        /// </summary>
        public bool IsContainOperator
        {
            get { return _operator == Operator.Contains || _operator == Operator.StartsWith || _operator == Operator.EndsWith || _operator == Operator.NotContains; }
        }

        /// <summary>
        /// True is the operator is Between (or Not)
        /// </summary>
        public bool IsBetweenOperator
        {
            get { return _operator == Operator.Between || _operator == Operator.NotBetween; }
        }

        /// <summary>
        /// String containing the SQL values of the enum values
        /// </summary>
        public string EnumSQLValue
        {
            get
            {

                string result = "";
                if (IsEnum)
                {
                    var type = (IsCommonRestrictionValue ? Type : MetaColumn.Type);
                    if (EnumValues.Count == 0) result = (type == ColumnType.Numeric ? "0" : "''");
                    foreach (string enumValue in EnumValues)
                    {
                        Helper.AddValue(ref result, ",", type == ColumnType.Numeric ? enumValue : Helper.QuoteSingle(enumValue));
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// String containing the LINQ values of the enum values
        /// </summary>
        public string EnumLINQValue
        {
            get
            {

                string result = "";
                if (IsEnum)
                {
                    var type = MetaColumn.Type;
                    if (EnumValues.Count == 0) result = "\"\"";
                    foreach (string enumValue in EnumValues)
                    {
                        Helper.AddValue(ref result, ",", Helper.QuoteDouble(enumValue));
                    }
                    result = string.Format("new List<string>(){{{0}}}", result);
                }
                return result;
            }
        }

        string GetSQLValue(string value, DateTime date, Operator op)
        {
            string result = "";
            if (IsNumeric)
            {
                if (string.IsNullOrEmpty(value)) result = "0";
                else
                {
                    var vals = GetVals(value);
                    if (vals.Length > 0)
                    {
                        double d;
                        if (!Helper.ValidateNumeric(vals[0], out d))
                        {
                            throw new Exception("Invalid numeric value: " + vals[0]);
                        }
                        result = d.ToString(CultureInfo.InvariantCulture.NumberFormat);
                    }
                }
            }
            else if (IsDateTime)
            {
                if (date == DateTime.MinValue) date = DateTime.Now;
                if (Model == null)
                {
                    result = date.ToString();
                }
                else if (Model.Connection.DatabaseType == DatabaseType.MSAccess || Model.Connection.DatabaseType == DatabaseType.MSExcel)
                {
                    //Serial
                    result = Double.Parse(date.ToOADate().ToString()).ToString(CultureInfo.InvariantCulture.NumberFormat);
                }
                else
                {
                    //Ansi Format
                    result = Helper.QuoteSingle(date.ToString(Model.Connection.DateTimeFormat));
                }
            }
            else
            {
                string value2 = value;
                if (string.IsNullOrEmpty(value)) value2 = "";
                if (op == Operator.Contains || op == Operator.NotContains) value2 = string.Format("%{0}%", value);
                else if (op == Operator.StartsWith) value2 = string.Format("{0}%", value);
                else if (op == Operator.EndsWith) value2 = string.Format("%{0}", value);
                if (TypeEl == ColumnType.UnicodeText)
                {
                    if (Model == null)
                    {
                        result = value2;
                    }
                    else if (Model.Connection.DatabaseType == DatabaseType.Oracle)
                    {
                        //For Oracle, we convert the unicode char using UNISTR
                        result = "";
                        for (int i = 0; i < value2.Length; i++)
                        {
                            string unicode = BitConverter.ToString(Encoding.Unicode.GetBytes(value2[i].ToString())).Replace("-", "");
                            result += Path.DirectorySeparatorChar + unicode.Substring(2, 2) + unicode.Substring(0, 2);
                        }
                        result = "UNISTR(" + Helper.QuoteSingle(result) + ")";
                    }
                    else if (Model.Connection.DatabaseType == DatabaseType.MSSQLServer)
                    {
                        result = "N" + Helper.QuoteSingle(value2); ;
                    }
                }
                else
                {
                    result = Helper.QuoteSingle(value2);
                }
            }
            return result;
        }

        string GetLINQValue(string value, DateTime date, Operator op)
        {
            string result = "";
            if (IsNumeric)
            {
                if (string.IsNullOrEmpty(value)) result = "0";
                else
                {
                    var vals = GetVals(value);
                    if (vals.Length > 0)
                    {
                        double d;
                        if (!Helper.ValidateNumeric(vals[0], out d))
                        {
                            throw new Exception("Invalid numeric value: " + vals[0]);
                        }
                        result = d.ToString(CultureInfo.InvariantCulture.NumberFormat);
                    }
                }
            }
            else if (IsDateTime)
            {
                if (date == DateTime.MinValue) date = DateTime.Now;
                result = string.Format("new DateTime({0},{1},{2},{3},{4},{5})", date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second);
            }
            else
            {
                string value2 = value;
                if (string.IsNullOrEmpty(value)) value2 = "";
                result = "@" + Helper.QuoteDouble(value2) + (!IsEnum && CaseSensitive ? "" : ".ToLower()");
            }
            return result;
        }

        /// <summary>
        /// Array of string from a value having CR/LF
        /// </summary>
        static public string[] GetVals(string value)
        {
            if (string.IsNullOrEmpty(value)) return new string[0];

            if (value.Contains("\n")) return value.Replace("\r", "").Split('\n');
            else return new string[] { value };
        }

        void addEqualOperator(ref string displayText, ref string displayRestriction, ref string sqlText, string value, DateTime finalDate, string dateKeyword, DateTime date)
        {
            Helper.AddValue(ref displayText, Report.ExecutionView.CultureInfo.TextInfo.ListSeparator, GetDisplayValue(value, finalDate));
            Helper.AddValue(ref displayRestriction, Report.ExecutionView.CultureInfo.TextInfo.ListSeparator, GetDisplayRestriction(value, dateKeyword, date));
            if (IsDateTime) Helper.AddValue(ref sqlText, ",", GetSQLValue(value, finalDate, _operator));
            else
            {
                foreach (var val in GetVals(value))
                {
                    Helper.AddValue(ref sqlText, ",", GetSQLValue(val, finalDate, _operator));
                }
            }
        }

        void addContainOperator(ref string displayText, ref string displayRestriction, ref string sqlText, string value, DateTime finalDate, string sqlOperator, string dateKeyword, DateTime date)
        {
            string separator = (_operator == Operator.NotContains ? " AND " : " OR ");
            Helper.AddValue(ref displayText, Report.ExecutionView.CultureInfo.TextInfo.ListSeparator, GetDisplayValue(value, finalDate));
            Helper.AddValue(ref displayRestriction, Report.ExecutionView.CultureInfo.TextInfo.ListSeparator, GetDisplayRestriction(value, dateKeyword, date));
            if (IsDateTime) Helper.AddValue(ref sqlText, separator, string.Format("{0}{1}{2}", SQLColumn, sqlOperator, GetSQLValue(value, finalDate, _operator)));
            else
            {
                foreach (var val in GetVals(value))
                {
                    Helper.AddValue(ref sqlText, separator, string.Format("{0} {1}{2}", SQLColumn, sqlOperator, GetSQLValue(val, finalDate, _operator)));
                }
            }
        }

        void addLINQOperator(ref string LINQText, string value, DateTime finalDate, string LINQOperator, string LINQSuffix)
        {
            string separator = (_operator == Operator.NotContains || _operator == Operator.NotEqual ? " && " : " || ");
            string prefix = _operator == Operator.NotContains ? "!" : "";
            if (IsDateTime)
            {
                Helper.AddValue(ref LINQText, separator, string.Format("{0}{1}{2}", LINQColumnName, LINQOperator, GetLINQValue(value, finalDate, _operator)));
            }
            else
            {
                var colName = LINQColumnName;
                if (IsText && !CaseSensitive && string.IsNullOrEmpty(SQL)) colName += ".ToLower()"; 
                foreach (var val in GetVals(value))
                {
                    Helper.AddValue(ref LINQText, separator, string.Format("{0}{1}{2}{3}{4}", prefix, colName, LINQOperator, GetLINQValue(val, finalDate, _operator), LINQSuffix));
                }
            }
        }

        void BuildTexts()
        {
            string displayLabel = DisplayNameElTranslated;

            if (_operator == Operator.ValueOnly)
            {
                if (IsEnum)
                {
                    _SQLText = string.Format("({0})", (HasValue ? EnumSQLValue : "NULL"));
                    _displayText = displayLabel + " " + (string.IsNullOrEmpty(OperatorLabel) ? "" : OperatorLabel + " ") + (HasValue ? EnumDisplayValue : "?");
                    _displayRestriction = _displayText;
                }
                else
                {
                    if (!HasValue1 && IsText) _SQLText = GetSQLValue("", FinalDate1, _operator);
                    else _SQLText = (HasValue1 ? GetSQLValue(Value1, FinalDate1, _operator) : "NULL");
                    _displayText = displayLabel + " " + (string.IsNullOrEmpty(OperatorLabel) ? "" : OperatorLabel + " ") + (HasValue1 ? GetDisplayValue(Value1, FinalDate1) : "?");
                    _displayRestriction = displayLabel + " " + (string.IsNullOrEmpty(OperatorLabel) ? "" : OperatorLabel + " ") + (HasValue1 ? GetDisplayRestriction(Value1, Date1Keyword, Date1) : "?");
                }
                if (!IsSQL) BuildLINQText();
                return;
            }

            _SQLText = "";

            string operatorLabel = Helper.GetEnumDescription(typeof(Operator), _operator);
            if (Report != null) operatorLabel = Report.Translate(operatorLabel);
            _displayText = displayLabel + " " + (string.IsNullOrEmpty(OperatorLabel) ? operatorLabel : OperatorLabel);
            _displayRestriction = displayLabel + " " + (string.IsNullOrEmpty(OperatorLabel) ? operatorLabel : OperatorLabel);

            if (!HasValue)
            {
                _SQLText = "(1=1)";
                _displayText += " ?";
                _displayRestriction += " ?";
                if (!IsSQL) BuildLINQText();
                return;
            }

            string sqlOperator = Helper.GetEnumDescription(typeof(Operator), _operator).ToUpper();
            if (_operator == Operator.IsNull || _operator == Operator.IsNotNull)
            {
                //Not or Not Null
                _SQLText += SQLColumn + " " + sqlOperator;
            }
            else if (_operator == Operator.IsEmpty || _operator == Operator.IsNotEmpty)
            {
                string op = _operator == Operator.IsEmpty ? " = " : " <> ";
                //add a space to make it work with Oracle...
                string val = (Model != null && Model.Connection != null && Model.Connection.DatabaseType == DatabaseType.Oracle) ? "' '" : "''";
                _SQLText += SQLColumn + op + val;
            }
            else
            {
                //Other cases
                if (IsContainOperator)
                {
                    _SQLText = "(";
                    sqlOperator = (_operator == Operator.NotContains ? "NOT " : "") + "LIKE ";
                }
                else if (_operator == Operator.Equal || _operator == Operator.NotEqual)
                {
                    sqlOperator = (_operator == Operator.Equal ? "IN (" : "NOT IN (");
                }
                else if (_operator == Operator.Smaller) sqlOperator = "<";
                else if (_operator == Operator.SmallerEqual) sqlOperator = "<=";
                else if (_operator == Operator.Greater) sqlOperator = ">";
                else if (_operator == Operator.GreaterEqual) sqlOperator = ">=";


                if (_operator == Operator.Between || _operator == Operator.NotBetween)
                {
                    sqlOperator = sqlOperator.Replace("IS ", "");

                    _displayText += " " + GetDisplayValue(Value1, FinalDate1);
                    _displayRestriction += " " + GetDisplayRestriction(Value1, Date1Keyword, Date1);

                    _displayText += " " + Report.Translate("and") + " " + GetDisplayValue(Value2, FinalDate2);
                    _displayRestriction += " " + Report.Translate("and") + " " + GetDisplayRestriction(Value2, Date2Keyword, Date2);
                    _SQLText += "(" + SQLColumn + " " + sqlOperator + " ";
                    _SQLText += GetSQLValue(Value1, FinalDate1, _operator);
                    _SQLText += " AND " + GetSQLValue(Value2, FinalDate2, _operator) + ")";
                }
                else if (_operator == Operator.Equal || _operator == Operator.NotEqual)
                {
                    _SQLText += SQLColumn + " " + sqlOperator;
                    string displayText = "", displayRestriction = "", sqlText = "";
                    if (IsEnum)
                    {
                        displayText = EnumDisplayValue;
                        displayRestriction = displayText;
                        sqlText = EnumSQLValue;
                    }
                    else
                    {
                        if (HasValue1) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value1, FinalDate1, Date1Keyword, Date1);
                        if (HasValue2) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value2, FinalDate2, Date2Keyword, Date2);
                        if (HasValue3) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value3, FinalDate3, Date3Keyword, Date3);
                        if (HasValue4) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value4, FinalDate4, Date4Keyword, Date4);
                    }
                    _displayText += " " + displayText;
                    _displayRestriction += " " + displayRestriction;
                    _SQLText += sqlText + ")";
                }
                else if (IsContainOperator)
                {
                    string displayText = "", displayRestriction = "", sqlText = "";
                    if (HasValue1) addContainOperator(ref displayText, ref displayRestriction, ref sqlText, Value1, FinalDate1, sqlOperator, Date1Keyword, Date1);
                    if (HasValue2) addContainOperator(ref displayText, ref displayRestriction, ref sqlText, Value2, FinalDate2, sqlOperator, Date2Keyword, Date2);
                    if (HasValue3) addContainOperator(ref displayText, ref displayRestriction, ref sqlText, Value3, FinalDate3, sqlOperator, Date3Keyword, Date3);
                    if (HasValue4) addContainOperator(ref displayText, ref displayRestriction, ref sqlText, Value4, FinalDate4, sqlOperator, Date4Keyword, Date4);
                    _displayText += " " + displayText;
                    _displayRestriction += " " + displayRestriction;
                    _SQLText += sqlText + ")";
                }
                else
                {
                    if (IsGreaterSmallerOperator && _SQLText.Length > 0)
                    {
                        _SQLText += " ";
                    }
                    _SQLText += SQLColumn + " " + sqlOperator;
                    _displayText += " " + GetDisplayValue(Value1, FinalDate1);
                    _displayRestriction += " " + GetDisplayRestriction(Value1, Date1Keyword, Date1);
                    _SQLText += " " + GetSQLValue(Value1, FinalDate1, _operator);
                }
            }

            if (!IsSQL) BuildLINQText();
        }

        void BuildLINQText()
        {
            if (_operator == Operator.ValueOnly)
            {
                if (IsEnum)
                {
                    _LINQText = string.Format("({0})", (HasValue ? GetLINQValue(EnumValues[0], DateTime.Now, _operator) : "null"));
                }
                else
                {
                    if (!HasValue1 && IsText) _LINQText = GetLINQValue("", FinalDate1, _operator);
                    else _LINQText = (HasValue1 ? GetLINQValue(Value1, FinalDate1, _operator) : "null");
                }
                return;
            }

            _LINQText = "";

            if (!HasValue)
            {
                _LINQText = "    true";
                return;
            }

            string LINQOperator = "", LINQSuffix = "";
            if (_operator == Operator.IsNull || _operator == Operator.IsNotNull)
            {
                //Not or Not Null
                _LINQText += LINQColumnName + (_operator == Operator.IsNull ? "==" : "!=") + "null";
            }
            else if (_operator == Operator.IsEmpty || _operator == Operator.IsNotEmpty)
            {
                _LINQText += (_operator == Operator.IsNotEmpty ? "!" : "") + string.Format("string.IsNullOrEmpty({0})", LINQColumnName);
            }
            else
            {
                //Other cases
                if (_operator == Operator.Contains)
                {
                    LINQOperator =  ".Contains(";
                    LINQSuffix = ")";
                }
                else if (_operator == Operator.NotContains)
                {
                    LINQOperator = ".Contains(";
                    LINQSuffix = ")";
                }
                else if (_operator == Operator.StartsWith)
                {
                    LINQOperator = ".StartsWith(";
                    LINQSuffix = ")";
                }
                else if (_operator == Operator.EndsWith)
                {
                    LINQOperator = ".EndsWith(";
                    LINQSuffix = ")";
                }
                else if (_operator == Operator.Equal) LINQOperator = "==";
                else if (_operator == Operator.NotEqual) LINQOperator = "!=";
                else if (_operator == Operator.Smaller) LINQOperator = "<";
                else if (_operator == Operator.SmallerEqual) LINQOperator = "<=";
                else if (_operator == Operator.Greater) LINQOperator = ">";
                else if (_operator == Operator.GreaterEqual) LINQOperator = ">=";

                if (_operator == Operator.Between || _operator == Operator.NotBetween)
                {
                    _LINQText += (_operator == Operator.NotBetween ? "!" : "") + "(" + LINQColumnName + ">=" + GetLINQValue(Value1, FinalDate1, _operator);
                    _LINQText += " && " + LINQColumnName + "<=" + GetLINQValue(Value2, FinalDate2, _operator) + ")";
                }
                else if (_operator == Operator.Equal || _operator == Operator.NotEqual || IsContainOperator)
                {
                    _LINQText += "(";
                    string val = "";
                    if (IsEnum)
                    {
                        foreach (var ev in EnumValues)
                        {
                            addLINQOperator(ref val, ev, FinalDate1, LINQOperator, LINQSuffix);
                        }
                    }
                    else
                    {                        
                        if (HasValue1) addLINQOperator(ref val, Value1, FinalDate1, LINQOperator, LINQSuffix);
                        if (HasValue2) addLINQOperator(ref val, Value2, FinalDate2, LINQOperator, LINQSuffix);
                        if (HasValue3) addLINQOperator(ref val, Value3, FinalDate3, LINQOperator, LINQSuffix);
                        if (HasValue4) addLINQOperator(ref val, Value4, FinalDate4, LINQOperator, LINQSuffix);
                    }
                    _LINQText += val+")";
                }
                else
                {
                    _LINQText += LINQColumnName + LINQOperator;
                    _LINQText += GetLINQValue(Value1, FinalDate1, _operator);
                }
            }
        }

        [XmlIgnore]
        string _displayRestriction;
        /// <summary>
        /// Display text of the restriction (value only)
        /// </summary>
        public string DisplayRestriction
        {
            get
            {
                BuildTexts();
                return _displayRestriction;
            }
        }

        /// <summary>
        /// Display text of the restriction (for editor)
        /// </summary>
        [XmlIgnore]
        public string DisplayRestrictionForEditor
        {
            get
            {
                BuildTexts();
                return DisplayRestriction.Replace("[", "{").Replace("]", "}"); //Avoid this perturbing chars for editor....
            }
        }

        [XmlIgnore]
        string _displayText;
        /// <summary>
        /// Display text of the full restriction (label and value)
        /// </summary>
        public string DisplayText
        {
            get
            {
                BuildTexts();
                return _displayText;
            }
        }

        [XmlIgnore]
        string _SQLText;
        /// <summary>
        /// SQL of the restriction 
        /// </summary>
        public string SQLText
        {
            get
            {
                BuildTexts();
                return _SQLText;
            }
        }

        [XmlIgnore]
        string _LINQText;
        /// <summary>
        /// LINQ of the restriction 
        /// </summary>
        public string LINQText
        {
            get
            {
                BuildTexts();
                return _LINQText;
            }
        }

        /// <summary>
        /// Html identifier for the restriction operator
        /// </summary>
        [XmlIgnore]
        public string OperatorHtmlId
        {
            get
            {
                return HtmlIndex + "_Operator";
            }
        }

        /// <summary>
        /// Html identifier for the restriction value
        /// </summary>
        [XmlIgnore]
        public string ValueHtmlId
        {
            get
            {
                return HtmlIndex + "_Value";
            }
        }

        /// <summary>
        /// Html identifier for the restriction option value (for enumerated list)
        /// </summary>
        [XmlIgnore]
        public string OptionValueHtmlId
        {
            get
            {
                return HtmlIndex + "_Option_Value";
            }
        }

        /// <summary>
        /// Html identifier for the restriction option (for enumerated list)
        /// </summary>
        [XmlIgnore]
        public string OptionHtmlId
        {
            get
            {
                return HtmlIndex + "_Option";
            }
        }

        string _htmlIndex;
        /// <summary>
        /// Helper to build the HTML index
        /// </summary>
        [XmlIgnore]
        public string HtmlIndex
        {
            get
            {
                if (string.IsNullOrEmpty(_htmlIndex)) _htmlIndex = Helper.NewGUID();
                return _htmlIndex;
            }
            set
            {
                _htmlIndex = value;
            }
        }

        /// <summary>
        /// True if the restriction has time
        /// </summary>
        [XmlIgnore]
        public bool HasTimeRe
        {
            get
            {
                if (!IsDateTime) return false;
                return Helper.HasTimeFormat(DateTimeStandardFormatRe, FormatRe);
            }
        }

        /// <summary>
        /// Input date time format for the restriction
        /// </summary>
        [XmlIgnore]
        public string InputDateFormat
        {
            get
            {
                var format = Report.ExecutionView.CultureInfo.DateTimeFormat.ShortDatePattern;
                if (HasTimeRe) format = "G";
                return format;
            }
        }

        string GetHtmlValue(string value, string keyword, DateTime date, bool forEdition)
        {
            string result = "";
            if (IsNumeric)
            {
                if (string.IsNullOrEmpty(value)) result = "";
                else result = ElementDisplayValue(value);
            }
            else if (IsDateTime)
            {
                if (forEdition && HasDateKeyword(keyword))
                {
                    result = Report.TranslateDateKeywords(keyword);
                }
                else if (date == DateTime.MinValue && !HasDateKeyword(keyword))
                {
                    result = "";
                }
                else
                {
                    var culture = Report.ExecutionView.CultureInfo;
                    date = GetFinalDate(keyword, date);
                    //for date, format should be synchro with the date picker, which should use short date
                    result = date.ToString(InputDateFormat, culture);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(value)) value = "";
                result = ElementDisplayValue(value);
            }
            return result;
        }

        /// <summary>
        /// HTML value of the restriction for index 1,2,3 or 4
        /// </summary>
        public string GetHtmlValue(int index, bool forEdition = false)
        {
            if (index == 2) return GetHtmlValue(Value2, Date2Keyword, Date2, forEdition);
            if (index == 3) return GetHtmlValue(Value3, Date3Keyword, Date3, forEdition);
            if (index == 4) return GetHtmlValue(Value4, Date4Keyword, Date4, forEdition);
            return GetHtmlValue(Value1, Date1Keyword, Date1, forEdition);
        }

        /// <summary>
        /// True if the restriction is consider as identical for prompt: only one restriction will be generated in the report
        /// Same name and same source column
        /// </summary>
        public bool IsIdenticalForPrompt(ReportRestriction restriction)
        {
            return (IsCommonRestrictionValue && restriction.IsCommonRestrictionValue && Name == restriction.Name) || (!IsCommonRestrictionValue && !restriction.IsCommonRestrictionValue && MetaColumnGUID == restriction.MetaColumnGUID && DisplayNameEl == restriction.DisplayNameEl);
        }

        public void CopyForPrompt(ReportRestriction restriction)
        {
                HtmlIndex = restriction.HtmlIndex;
                Prompt = restriction.Prompt;
                Operator = restriction.Operator;
                Value1 = restriction.Value1;
                Date1 = restriction.Date1;
                Date1Keyword = restriction.Date1Keyword;
                Value2 = restriction.Value2;
                Date2 = restriction.Date2;
                Date2Keyword = restriction.Date2Keyword;
                Value3 = restriction.Value3;
                Date3 = restriction.Date3;
                Date3Keyword = restriction.Date3Keyword;
                Value4 = restriction.Value4;
                Date4 = restriction.Date4;
                Date4Keyword = restriction.Date4Keyword;
        }
    }
}
