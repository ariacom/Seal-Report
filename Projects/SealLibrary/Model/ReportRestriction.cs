//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
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
using Seal.Converter;
using Seal.Forms;

namespace Seal.Model
{


    [ClassResource(BaseName = "DynamicTypeDescriptorApp.Properties.Resources", KeyPrefix = "ReportRestriction_")]
    public class ReportRestriction : ReportElement
    {
        #region Editor
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
                GetProperty("DisplayNameEl").SetIsBrowsable(true);
                GetProperty("SQL").SetIsBrowsable(!IsInputValue && !IsCommonValue && !IsNoSQL);
                GetProperty("FormatRe").SetIsBrowsable(!IsEnum);
                GetProperty("TypeRe").SetIsBrowsable(!IsNoSQL);
                GetProperty("OperatorLabel").SetIsBrowsable(!IsInputValue && !IsCommonValue);
                GetProperty("EnumGUIDRE").SetIsBrowsable(true);
                GetProperty("ChangeOperator").SetIsBrowsable(!IsInputValue && !IsCommonValue);
                GetProperty("InputRows").SetIsBrowsable((IsText || IsNumeric) && !IsEnum);

                //Conditional
                if (IsEnum)
                {
                    GetProperty("EnumValue").SetIsBrowsable(true);
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

                GetProperty("Required").SetIsReadOnly(_prompt == PromptType.None);
                GetProperty("ChangeOperator").SetIsReadOnly(_prompt == PromptType.None);

                //Aggregate restriction
                if (PivotPosition == PivotPosition.Data && !(MetaColumn != null && MetaColumn.IsAggregate)) GetProperty("AggregateFunction").SetIsBrowsable(true);

                if (!GetProperty("Date1Keyword").IsReadOnly) GetProperty("Date1").SetIsReadOnly(HasDateKeyword(Date1Keyword));
                if (!GetProperty("Date2Keyword").IsReadOnly) GetProperty("Date2").SetIsReadOnly(HasDateKeyword(Date2Keyword));
                if (!GetProperty("Date3Keyword").IsReadOnly) GetProperty("Date3").SetIsReadOnly(HasDateKeyword(Date3Keyword));
                if (!GetProperty("Date4Keyword").IsReadOnly) GetProperty("Date4").SetIsReadOnly(HasDateKeyword(Date4Keyword));

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        public override string ToString()
        {
            return Name;
        }

        public const char kStartRestrictionChar = '[';
        public const char kStopRestrictionChar = ']';

        public static ReportRestriction CreateReportRestriction()
        {
            return new ReportRestriction() { GUID = Guid.NewGuid().ToString(), _type = ColumnType.Default, _numericStandardFormat = NumericStandardFormat.Default, _datetimeStandardFormat = DateTimeStandardFormat.Default };
        }

        private PromptType _prompt = PromptType.None;
        [DefaultValue(PromptType.None)]
        [Category("Definition"), DisplayName("\tPrompt restriction"), Description("Define if the value of the restriction is prompted to the user when the report is executed."), Id(5, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public PromptType Prompt
        {
            get { return _prompt; }
            set { _prompt = value; }
        }

        private bool _required = false;
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("Is required"), Description("If true and the restriction is prompted, a value is required to execute the report."), Id(6, 1)]
        public bool Required
        {
            get { return _required; }
            set { _required = value; }
        }

        [Category("Advanced"), DisplayName("Custom SQL"), Description("If not empty, overwrite the default SQL used for the restriction in the WHERE clause."), Id(1, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public new string SQL
        {
            get
            {
                return _SQL;
            }
            set { _SQL = value; }
        }

        [DefaultValue(ColumnType.Default)]
        [Category("Advanced"), DisplayName("Data Type"), Description("Data type of the restriction."), Id(2, 3)]
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

        [DefaultValue(DateTimeStandardFormat.Default)]
        [Category("Advanced"), DisplayName("Format"), Description("Standard display format applied to the restriction display value."), Id(3, 3)]
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
                else _datetimeStandardFormat = value;
            }
        }

        [DefaultValue(NumericStandardFormat.Default)]
        [Category("Advanced"), DisplayName("Format"), Description("Standard display format applied to the restriction display value."), Id(3, 3)]
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
                else _numericStandardFormat = value;
            }
        }

        [Category("Advanced"), DisplayName("Custom Format"), Description("If not empty, specify the format of the restriction display values (.Net Format Strings)."), Id(4, 3)]
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

        private string _operatorLabel;
        [Category("Advanced"), DisplayName("Operator Label"), Description("If not empty, overwrite the operator display text."), Id(5, 3)]
        public string OperatorLabel
        {
            get { return _operatorLabel; }
            set { _operatorLabel = value; }
        }

        private int _inputRows = 0;
        [DefaultValue(0)]
        [Category("Advanced"), DisplayName("Input lines for first value"), Description("If greater than 0, specifies the number of lines available to edit the first restriction value (only valid for text or numeric when the restriction is prompted)."), Id(6, 3)]
        public int InputRows
        {
            get { return _inputRows; }
            set { _inputRows = value; }
        }

        private bool _changeOperator = true;
        [DefaultValue(true)]
        [Category("Advanced"), DisplayName("Can Change Operator"), Description("If true, the operator can be changed when the restriction is prompted."), Id(7, 3)]
        public bool ChangeOperator
        {
            get { return _changeOperator; }
            set { _changeOperator = value; }
        }


        [DefaultValue(null)]
        [Category("Advanced"), DisplayName("Custom Enumerated List"), Description("If defined, the restriction values are selected using the enumerated list."), Id(8, 3)]
        [TypeConverter(typeof(MetaEnumConverter))]
        public string EnumGUIDRE
        {
            get { return _enumGUID; }
            set { _enumGUID = value; }
        }

        Operator _operator = Operator.Equal;
        [DefaultValue(Operator.Equal)]
        [TypeConverter(typeof(RestrictionOperatorConverter))]
        [Category("Definition"), DisplayName("\tOperator"), Description("The Operator used for the restriction. If Value Only is selected, the restriction is replaced by the value only (with no column name and operator)."), Id(2, 1)]
        public Operator Operator
        {
            get { return _operator; }
            set
            {
                _operator = value;
                UpdateEditorAttributes();
            }
        }

        [XmlIgnore]
        public MetaEnum EnumRE
        {
            get
            {
                if (Enum != null) return Enum;
                if (IsCommonRestrictionValue) return null;
                return MetaColumn.Enum;
            }
        }

        [XmlIgnore]
        public List<MetaEV> PromptedEnumValues
        {
            get
            {
                if (EnumRE == null) return new List<MetaEV>();
                int i = 0;
                foreach (var enumDef in EnumRE.Values) enumDef.HtmlId = (i++).ToString();

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

        [XmlIgnore]
        public bool IsNoSQL
        {
            get
            {
                return Source != null && Source.IsNoSQL;
            }
        }

        [XmlIgnore]
        public bool IsInputValue
        {
            get
            {
                return Source == null;
            }
        }


        [XmlIgnore]
        public bool IsEnumRE
        {
            get
            {
                if (Enum != null) return true;
                return IsEnum;
            }
        }

        [XmlIgnore]
        public bool HasOperator
        {
            get
            {
                return !(Operator == Operator.ValueOnly && string.IsNullOrEmpty(OperatorLabel));
            }
        }

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

        public string GetOperatorLabel(Operator op)
        {
            if (Operator == Operator.ValueOnly || Model == null) return OperatorLabel;
            return Report.Translate(Helper.GetEnumDescription(typeof(Operator), op));
        }



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

        public void SetNavigationValue(string val)
        {
            if (IsEnum) EnumValues.Add(val);
            else if (IsDateTime) Date1 = DateTime.FromOADate(double.Parse(val, CultureInfo.InvariantCulture));
            else Value1 = val;
        }

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
                if (!IsEnum)
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
        [Category("Restriction Values"), DisplayName("Value 1"), Description("Value used for the restriction. Multiple values can be set (one per line)"), Id(1, 2)]
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
        [Category("Restriction Values"), DisplayName("Value 2"), Description("Second value used for the restriction."), Id(3, 2)]
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
        [Category("Restriction Values"), DisplayName("Value 3"), Description("Third value used for the restriction."), Id(5, 2)]
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
        [Category("Restriction Values"), DisplayName("Value 4"), Description("Fourth value used for the restriction."), Id(7, 2)]
        public string Value4
        {
            get { return _value4; }
            set
            {
                CheckInputValue(value);
                _value4 = value;
            }
        }

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

        public string FirstStringValue
        {
            get
            {
                var result = FirstValue;
                if (result != null) return result.ToString();
                return null;
            }
        }

        public double? FirstNumericValue
        {
            get
            {
                if (IsNumeric) return DoubleValue;
                return null;
            }
        }

        public DateTime? FirstDateValue
        {
            get
            {
               if (IsDateTime) return FinalDate1;
                return null;
            }
        }

        List<string> _enumValues = new List<string>();
        public List<string> EnumValues
        {
            get { return _enumValues; }
            set { _enumValues = value; }
        }
        public bool ShouldSerializeEnumValues() { return _enumValues.Count > 0; }

        [Category("Restriction Values"), DisplayName("Value"), Description("Value used for the restriction."), Id(1, 2)]
        [Editor(typeof(RestrictionEnumValuesEditor), typeof(UITypeEditor))]
        [XmlIgnore]
        public string EnumValue
        {
            get { return "<Click to edit values>"; }
            set { } //keep set for modification handler
        }



        [CategoryAttribute("Restriction Values"), DisplayName("Value 1"), Description("Value used for the restriction."), Id(1, 2)]
        [TypeConverter(typeof(RestrictionDateConverter))]
        public DateTime Date1 { get; set; }
        public bool ShouldSerializeDate1() { return Date1 != DateTime.MinValue; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 2"), Description("Second value used for the restriction."), Id(3, 2)]
        [TypeConverter(typeof(RestrictionDateConverter))]
        public DateTime Date2 { get; set; }
        public bool ShouldSerializeDate2() { return Date2 != DateTime.MinValue; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 3"), Description("Third value used for the restriction."), Id(5, 2)]
        [TypeConverter(typeof(RestrictionDateConverter))]
        public DateTime Date3 { get; set; }
        public bool ShouldSerializeDate3() { return Date3 != DateTime.MinValue; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 4"), Description("Fourth value used for the restriction."), Id(7, 2)]
        [TypeConverter(typeof(RestrictionDateConverter))]
        public DateTime Date4 { get; set; }
        public bool ShouldSerializeDate4() { return Date4 != DateTime.MinValue; }

        const string DateKeywordDescription = "Date keyword can be used to specify relative date and time for the restriction value. From the chosen keyword, operations +/- are allowed with the following units: Y(Year), S(Semester), Q(Quarter), M(Month), D(Day), h(hour), m(minute), s(second)";
        [Category("Restriction Values"), DisplayName("Value 1 Keyword"), Description(DateKeywordDescription), Id(2, 2)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date1Keyword { get; set; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 2 Keyword"), Description(DateKeywordDescription), Id(4, 2)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date2Keyword { get; set; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 3 Keyword"), Description(DateKeywordDescription), Id(6, 2)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date3Keyword { get; set; }

        [CategoryAttribute("Restriction Values"), DisplayName("Value 4 Keyword"), Description(DateKeywordDescription), Id(8, 2)]
        [TypeConverter(typeof(DateKeywordConverter))]
        public string Date4Keyword { get; set; }



        public bool HasValue
        {
            get
            {
                return Operator == Operator.IsNull
                    || Operator == Operator.IsNotNull
                    || Operator == Operator.IsEmpty
                    || Operator == Operator.IsNotEmpty
                    || (IsEnum && EnumValues.Count > 0)
                    || HasValue1
                    || (HasValue2 && !IsGreaterSmallerOperator)
                    || (HasValue3 && !IsGreaterSmallerOperator && !IsBetweenOperator)
                    || (HasValue4 && !IsGreaterSmallerOperator && !IsBetweenOperator);
            }
        }

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

        public DateTime FinalDate1
        {
            get
            {
                return GetFinalDate(Date1Keyword, Date1);
            }
        }

        public DateTime FinalDate2
        {
            get
            {
                return GetFinalDate(Date2Keyword, Date2);
            }
        }

        public DateTime FinalDate3
        {
            get
            {
                return GetFinalDate(Date3Keyword, Date3);
            }
        }

        public DateTime FinalDate4
        {
            get
            {
                return GetFinalDate(Date4Keyword, Date4);
            }
        }

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

        public string DisplayValue1
        {
            get
            {
                return GetDisplayValue(Value1, Date1);
            }
        }

        public string DisplayValue2
        {
            get
            {
                return GetDisplayValue(Value2, Date2);
            }
        }

        public string DisplayValue3
        {
            get
            {
                return GetDisplayValue(Value3, Date3);
            }
        }

        public string DisplayValue4
        {
            get
            {
                return GetDisplayValue(Value4, Date4);
            }
        }

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

        public string GetEnumDisplayValue(string id)
        {
            return Report.EnumDisplayValue(EnumRE, id, true);
        }


        public string GetEnumMessage()
        {
            return Report.EnumMessage(EnumRE);
        }

        public bool IsGreaterSmallerOperator
        {
            get { return _operator == Operator.Greater || _operator == Operator.GreaterEqual || _operator == Operator.Smaller || _operator == Operator.SmallerEqual; }
        }

        public bool IsContainOperator
        {
            get { return _operator == Operator.Contains || _operator == Operator.StartsWith || _operator == Operator.EndsWith || _operator == Operator.NotContains; }
        }

        public bool IsBetweenOperator
        {
            get { return _operator == Operator.Between || _operator == Operator.NotBetween; }
        }

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
                result = Helper.QuoteSingle(value2);
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
                            result += "\\" + unicode.Substring(2, 2) + unicode.Substring(0, 2);
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
            if (IsDateTime) Helper.AddValue(ref sqlText, separator, string.Format("{0} {1}{2}", SQLColumn, sqlOperator, GetSQLValue(value, finalDate, _operator)));
            else
            {
                foreach (var val in GetVals(value))
                {
                    Helper.AddValue(ref sqlText, separator, string.Format("{0} {1}{2}", SQLColumn, sqlOperator, GetSQLValue(val, finalDate, _operator)));
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
                    if (IsNoSQL)
                    {
                        //Between is not supported for NoSQL
                        _SQLText += "(" + SQLColumn + ">=" + GetSQLValue(Value1, FinalDate1, _operator);
                        _SQLText += " AND " + SQLColumn + "<=" + GetSQLValue(Value2, FinalDate2, _operator) + ")";
                    }
                    else
                    {
                        _SQLText += "(" + SQLColumn + " " + sqlOperator + " ";
                        _SQLText += GetSQLValue(Value1, FinalDate1, _operator);
                        _SQLText += " AND " + GetSQLValue(Value2, FinalDate2, _operator) + ")";
                    }
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
                        if (HasValue1 && !IsEnum) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value1, FinalDate1, Date1Keyword, Date1);
                        if (HasValue2 && !IsEnum) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value2, FinalDate2, Date2Keyword, Date2);
                        if (HasValue3 && !IsEnum) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value3, FinalDate3, Date3Keyword, Date3);
                        if (HasValue4 && !IsEnum) addEqualOperator(ref displayText, ref displayRestriction, ref sqlText, Value4, FinalDate4, Date4Keyword, Date4);
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
                    if (IsGreaterSmallerOperator)
                    {
                        _SQLText += " ";
                    }
                    _SQLText += SQLColumn + " " + sqlOperator;
                    _displayText += " " + GetDisplayValue(Value1, FinalDate1);
                    _displayRestriction += " " + GetDisplayRestriction(Value1, Date1Keyword, Date1);
                    _SQLText += GetSQLValue(Value1, FinalDate1, _operator);
                }
            }
        }

        [XmlIgnore]
        string _displayRestriction;
        public string DisplayRestriction
        {
            get
            {
                BuildTexts();
                return _displayRestriction;
            }
        }

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
        public string SQLText
        {
            get
            {
                BuildTexts();
                return _SQLText;
            }
        }

        [XmlIgnore]
        public string OperatorHtmlId
        {
            get
            {
                return HtmlIndex + "_Operator";
            }
        }

        [XmlIgnore]
        public string ValueHtmlId
        {
            get
            {
                return HtmlIndex + "_Value";
            }
        }

        [XmlIgnore]
        public string OptionValueHtmlId
        {
            get
            {
                return HtmlIndex + "_Option_Value";
            }
        }

        [XmlIgnore]
        public string OptionHtmlId
        {
            get
            {
                return HtmlIndex + "_Option";
            }
        }

        string _htmlIndex;
        [XmlIgnore]
        public string HtmlIndex
        {
            get { return _htmlIndex; }
            set { _htmlIndex = value; }
        }

        [XmlIgnore]
        public bool HasTimeRe
        {
            get
            {
                if (!IsDateTime) return false;
                return HasTimeFormat(DateTimeStandardFormatRe, FormatRe);
            }
        }


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

        public string GetHtmlValue(int index, bool forEdition = false)
        {
            if (index == 2) return GetHtmlValue(Value2, Date2Keyword, Date2, forEdition);
            if (index == 3) return GetHtmlValue(Value3, Date3Keyword, Date3, forEdition);
            if (index == 4) return GetHtmlValue(Value4, Date4Keyword, Date4, forEdition);
            return GetHtmlValue(Value1, Date1Keyword, Date1, forEdition);
        }
    }
}
