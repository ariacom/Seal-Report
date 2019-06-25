//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Helpers;
using DynamicTypeDescriptor;
using Seal.Converter;
using System.Globalization;
using Seal.Forms;
using System.Drawing.Design;

namespace Seal.Model
{
    [ClassResource(BaseName = "DynamicTypeDescriptorApp.Properties.Resources", KeyPrefix = "ReportElement_")]
    public class ReportElement : MetaColumn
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable

                GetProperty("DisplayNameEl").SetIsBrowsable(true);
                GetProperty("SQL").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("SortOrder").SetIsBrowsable(true);
                GetProperty("TypeEd").SetIsBrowsable(!IsEnum && !Source.IsNoSQL);
                GetProperty("ShowSubTotals").SetIsBrowsable(PivotPosition == PivotPosition.Row);

                GetProperty("AggregateFunction").SetIsBrowsable(PivotPosition == PivotPosition.Data && !MetaColumn.IsAggregate);
                GetProperty("TotalAggregateFunction").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                GetProperty("ShowTotal").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                GetProperty("CellScript").SetIsBrowsable(true);
                GetProperty("CalculationOption").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                GetProperty("EnumGUIDEL").SetIsBrowsable(true);
                GetProperty("ForceAggregate").SetIsBrowsable(true);

                GetProperty("Format").SetIsBrowsable(!IsEnum && (TypeEd == ColumnType.DateTime || TypeEd == ColumnType.Numeric || Type == ColumnType.Default));
                GetProperty("NumericStandardFormat").SetIsBrowsable(!IsEnum && IsNumeric && (TypeEd == ColumnType.Numeric || Type == ColumnType.Default));
                GetProperty("DateTimeStandardFormat").SetIsBrowsable(!IsEnum && IsDateTime && (TypeEd == ColumnType.DateTime || Type == ColumnType.Default));

                GetProperty("SerieDefinition").SetIsBrowsable(PivotPosition == PivotPosition.Row || PivotPosition == PivotPosition.Column);
                GetProperty("Nvd3Serie").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                GetProperty("ChartJSSerie").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                GetProperty("PlotlySerie").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                //FUTURE GetProperty("XAxisType").SetIsBrowsable(PivotPosition == PivotPosition.Row || PivotPosition == PivotPosition.Column || PivotPosition == PivotPosition.Data);
                GetProperty("YAxisType").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                GetProperty("SerieSortOrder").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                GetProperty("SerieSortType").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                GetProperty("AxisUseValues").SetIsBrowsable((PivotPosition == PivotPosition.Row || PivotPosition == PivotPosition.Column) && (IsNumeric || IsDateTime));

                //Read only
                GetProperty("Format").SetIsReadOnly((IsNumeric && NumericStandardFormat != NumericStandardFormat.Custom) || (IsDateTime && DateTimeStandardFormat != DateTimeStandardFormat.Custom));
                GetProperty("TotalAggregateFunction").SetIsReadOnly(ShowTotal == ShowTotal.No);
                //FUTURE GetProperty("XAxisType").SetIsReadOnly(!IsSerie || _serieDefinition == SerieDefinition.SplitterBoth);
                GetProperty("YAxisType").SetIsReadOnly(!IsSerie);
                GetProperty("SerieSortOrder").SetIsReadOnly(!IsSerie || _serieSortType == SerieSortType.None);
                GetProperty("SerieSortType").SetIsReadOnly(!IsSerie);
                GetProperty("AxisUseValues").SetIsReadOnly(SerieDefinition != SerieDefinition.Axis);
                GetProperty("CalculationOption").SetIsReadOnly(!IsNumeric);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion


        public static ReportElement Create()
        {
            return new ReportElement() { GUID = Guid.NewGuid().ToString() };                                                           
        }

        public bool IsEnum
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return false;
                return (EnumEL != null);
            }
        }


        public bool IsNumeric
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return true;
                return (TypeEl == ColumnType.Numeric);
            }
        }

        public bool IsText
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return false;
                return (TypeEl == ColumnType.Text || TypeEl == ColumnType.UnicodeText);
            }
        }

        public bool IsDateTime
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return false;
                return (TypeEl == ColumnType.DateTime);
            }
        }

        public void SetDefaults()
        {
            //Default aggregate
            if (IsEnum)
            {
                AggregateFunction = AggregateFunction.Count;
            }
            else if (IsNumeric)
            {
                AggregateFunction = AggregateFunction.Sum;
            }
            else if (IsDateTime)
            {
                AggregateFunction = AggregateFunction.Max;
                TotalAggregateFunction = AggregateFunction.Max;
            }
            else
            {
                AggregateFunction = AggregateFunction.Count;
            }
        }

        [Browsable(false)]
        private PivotPosition _pivotPosition = PivotPosition.Row;
        public PivotPosition PivotPosition
        {
            get { return _pivotPosition; }
            set { _pivotPosition = value; }
        }

        [DefaultValue(null)]
        [Category("Definition"), DisplayName("\tName"), Description("Name of the element when displayed in result tables or restrictions."), Id(1, 1)]
        [XmlIgnore]
        [TypeConverter(typeof(CustomNameConverter))]
        public string DisplayNameEl
        {
            get
            {
                if (!string.IsNullOrEmpty(DisplayName)) return DisplayName;
                return RawDisplayName;
            }
            set
            {
                DisplayName = value;
                if (MetaColumn != null && RawDisplayName == DisplayName) DisplayName = "";
            }
        }

        [XmlIgnore]
        public string DisplayNameElTranslated
        {
            get
            {
                return Report.TranslateElement(this, DisplayNameEl);
            }
        }

        string _sortOrder = SortOrderConverter.kAutomaticAscSortKeyword;
        [DefaultValue(SortOrderConverter.kAutomaticAscSortKeyword)]
        [Category("Definition"), DisplayName("Sort Order"), Description("Sort the result tables. Page elements are sorted first, then Row, Column and Data elements."), Id(2, 1)]
        [TypeConverter(typeof(SortOrderConverter))]
        public string SortOrder
        {
            get { return _sortOrder; }
            set { _sortOrder = value; }
        }

        [DefaultValue(ColumnType.Default)]
        [Category("Options"), DisplayName("Data Type"), Description("Data type of the column."), Id(1, 3)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public ColumnType TypeEd
        {
            get { return _type; }
            set
            {
                if (Type != value && _dctd != null)
                {
                    Type = value;
                    _numericStandardFormat = NumericStandardFormat.Default;
                    _datetimeStandardFormat = DateTimeStandardFormat.Default;
                    SetStandardFormat();
                }
                else Type = value;
                UpdateEditorAttributes();
            }
        }

        bool _showSubTotals = false;
        [Category("Options"), DisplayName("Show sub-totals"), Description("If true, a line showing sub-totals is added to the main data table when the value of the element changes."), Id(2, 3)]
        [DefaultValue(false)]
        public bool ShowSubTotals
        {
            get { return _showSubTotals; }
            set { _showSubTotals = value; }
        }

        [XmlIgnore]
        public ColumnType TypeEl
        {
            get
            {
                if (_type == ColumnType.Default && !IsCommonRestrictionValue) return MetaColumn.Type;
                return _type;
            }
        }

        [XmlIgnore]
        public string FormatEl
        {
            get
            {
                SetStandardFormat();
                if (IsCommonRestrictionValue) return Format;

                string result = Format;
                if (string.IsNullOrEmpty(result)) result = MetaColumn.Format;
                if (string.IsNullOrEmpty(result))
                {
                    if (IsNumeric) result = Source.Repository.Configuration.NumericFormat;
                    else if (IsDateTime) result = Source.Repository.Configuration.DateTimeFormat;
                    else result = "0";
                }
                if (string.IsNullOrEmpty(result)) result = "0";
                return result;
            }
        }


        [XmlIgnore]
        public bool HasTimeEl
        {
            get
            {
                if (!IsDateTime) return false;
                return HasTimeFormat(DateTimeStandardFormat, FormatEl);
            }
        }

        public string ElementDisplayValue(object value)
        {
            if (value == null) return "";
            if (value is IFormattable)
            {
                string result = value.ToString();
                try
                {
                    result = ((IFormattable)value).ToString(FormatEl, Report.ExecutionView.CultureInfo);
                }
                catch { }
                return result;
            }
            return value.ToString();
        }

        [XmlIgnore]
        public bool IsSorted
        {
            get { return SortOrder != SortOrderConverter.kNoSortKeyword && !string.IsNullOrEmpty(SortOrder); }
        }

        public string GetEnumSortValue(string enumValue, bool useDisplayValue)
        {
            string result = enumValue;
            MetaEnum en = EnumEL;

            bool elementSortPosition = (IsSorted && en.UsePosition);
            MetaEV value = null;
            if (useDisplayValue) value = en.Values.FirstOrDefault(i => i.DisplayValue == enumValue);
            else value = en.Values.FirstOrDefault(i => i.Id == enumValue);

            if (value != null)
            {
                string sortPrefix = elementSortPosition ? string.Format("{0:000000}", en.Values.LastIndexOf(value)) : "";
                result = sortPrefix + Report.EnumDisplayValue(en, value.Id);
            }
            else
            {
                string sortPrefix = elementSortPosition ? "000000" : "";
                result = sortPrefix + result;
            }
            return result;
        }

        string _finalSortOrder;
        [XmlIgnore]
        public string FinalSortOrder
        {
            get { return _finalSortOrder; }
            set { _finalSortOrder = value; }
        }

        AggregateFunction _aggregateFunction = AggregateFunction.Sum;
        [DefaultValue(AggregateFunction.Sum)]
        [Category("Data Options"), DisplayName("Aggregate"), Description("Aggregate function applied to the Data element."), Id(1, 4)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public AggregateFunction AggregateFunction
        {
            get { return _aggregateFunction; }
            set { _aggregateFunction = value; UpdateEditorAttributes(); }
        }

        CalculationOption _calculationOption = CalculationOption.No;
        [DefaultValue(CalculationOption.No)]
        [Category("Data Options"), DisplayName("Calculation Option"), Description("For numeric Data elements, define calculation option applied on the element in the table."), Id(2, 4)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public CalculationOption CalculationOption
        {
            get { return _calculationOption; }
            set
            {
                if (Source != null && _calculationOption != value)
                {
                    if (value != CalculationOption.No && (string.IsNullOrEmpty(Format) || NumericStandardFormat == NumericStandardFormat.Default)) NumericStandardFormat = NumericStandardFormat.Percentage0;
                    else if (value == CalculationOption.No && (NumericStandardFormat == NumericStandardFormat.Percentage0 || NumericStandardFormat == NumericStandardFormat.Percentage1 || NumericStandardFormat == NumericStandardFormat.Percentage2))
                    {
                        Format = "";
                        NumericStandardFormat = NumericStandardFormat.Default;
                    }
                }
                _calculationOption = value;
                UpdateEditorAttributes();
            }
        }

        ShowTotal _showTotal = ShowTotal.No;
        [DefaultValue(ShowTotal.No)]
        [Category("Data Options"), DisplayName("Show Total"), Description("For Data elements, add a row or a column showing the total of the element in the table. 'Show only total' means that the columns containing the values of the element will be hidden in the table, only the column containing the total of the element is displayed."), Id(3, 4)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public ShowTotal ShowTotal
        {
            get { return _showTotal; }
            set
            {
                _showTotal = value;
                UpdateEditorAttributes();
            }
        }

        AggregateFunction _totalAggregateFunction = AggregateFunction.Sum;
        [DefaultValue(AggregateFunction.Sum)]
        [Category("Data Options"), DisplayName("Total Aggregate"), Description("Aggregate function applied for the totals."), Id(4, 4)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public AggregateFunction TotalAggregateFunction
        {
            get { return _totalAggregateFunction; }
            set { _totalAggregateFunction = value; }
        }

        //Charts
        SerieDefinition _serieDefinition = SerieDefinition.None;
        [DefaultValue(SerieDefinition.None)]
        [Category("Chart"), DisplayName("Serie Definition"), Description("Defines how the element is used in the chart. Row or Column elements can be either Axis or Splitter (to create a serie for each splitter value)."), Id(1, 2)]
        [TypeConverter(typeof(SerieDefinitionConverter))]
        public SerieDefinition SerieDefinition
        {
            get { return _serieDefinition; }
            set
            {
                _serieDefinition = value;
                UpdateEditorAttributes();
            }
        }

        public bool IsSerie
        {
            get { return _nvd3Serie != NVD3SerieDefinition.None || _chartJSSerie != ChartJSSerieDefinition.None || _plotlySerie != PlotlySerieDefinition.None; }
        }

        bool _axisUseValues = true;
        [DefaultValue(true)]
        [Category("Chart"), DisplayName("Use values for axis"), Description("For Numeric or Date Time axis, if true, the element values are used for the axis, otherwise axis values are linear. This feature does not work for all types of chart."), Id(2, 2)]
        public bool AxisUseValues
        {
            get { return _axisUseValues; }
            set { _axisUseValues = value; }
        }


        ChartJSSerieDefinition _chartJSSerie = ChartJSSerieDefinition.None;
        [DefaultValue(ChartJSSerieDefinition.None)]
        [Category("Chart"), DisplayName("Chart JS Serie"), Description("Definition of the serie for the element in the Chart JS chart."), Id(2, 2)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public ChartJSSerieDefinition ChartJSSerie
        {
            get { return _chartJSSerie; }
            set {
                _chartJSSerie = value;
                UpdateEditorAttributes();
            }
        }

        NVD3SerieDefinition _nvd3Serie = NVD3SerieDefinition.None;
        [DefaultValue(NVD3SerieDefinition.None)]
        [Category("Chart"), DisplayName("NVD3 Serie"), Description("Definition of the serie for the element in the NVD3 chart."), Id(3, 2)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public NVD3SerieDefinition Nvd3Serie
        {
            get { return _nvd3Serie; }
            set {
                _nvd3Serie = value;
                UpdateEditorAttributes();
            }
        }

        PlotlySerieDefinition _plotlySerie = PlotlySerieDefinition.None;
        [DefaultValue(PlotlySerieDefinition.None)]
        [Category("Chart"), DisplayName("Plotly Serie"), Description("Definition of the serie for the element in the Plotly chart."), Id(4, 2)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public PlotlySerieDefinition PlotlySerie
        {
            get { return _plotlySerie; }
            set {
                _plotlySerie = value;
                UpdateEditorAttributes();
            }
        }

        private SerieSortType _serieSortType = SerieSortType.Y;
        [DefaultValue(SerieSortType.Y)]
        [Category("Chart"), DisplayName("Sort Type"), Description("Defines how the serie is sorted in the chart."), Id(5, 2)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public SerieSortType SerieSortType
        {
            get { return _serieSortType; }
            set
            {
                _serieSortType = value;
                UpdateEditorAttributes();
            }
        }

        private PointSortOrder _serieSortOrder = PointSortOrder.Ascending;
        [DefaultValue(PointSortOrder.Ascending)]
        [Category("Chart"), DisplayName("Sort Order"), Description("Defines if the serie is sorted ascending or descending in the chart."), Id(6, 2)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public PointSortOrder SerieSortOrder
        {
            get { return _serieSortOrder; }
            set { _serieSortOrder = value; }
        }

        //Not used from v4, FUTURE...
        private AxisType _xAxisType = AxisType.Primary;
        [DefaultValue(AxisType.Primary)]
        [Category("Chart"), DisplayName("X Axis Type"), Description("Definition of the X axis of the serie (Primary or Secondary)."), Id(7, 2)]
        public AxisType XAxisType
        {
            get { return _xAxisType; }
            set { _xAxisType = value; }
        }

        private AxisType _yAxisType = AxisType.Primary;
        [DefaultValue(AxisType.Primary)]
        [Category("Chart"), DisplayName("Y Axis Type"), Description("Definition of the Y axis of the serie (Primary or Secondary)."), Id(8, 2)]
        public AxisType YAxisType
        {
            get { return _yAxisType; }
            set { _yAxisType = value; }
        }


        string _metaColumnGUID;
        [Browsable(false)]
        public string MetaColumnGUID
        {
            get { return _metaColumnGUID; }
            set { _metaColumnGUID = value; }
        }

        public void ChangeColumnGUID(string guid)
        {
            _metaColumn = null;
            _metaColumnGUID = guid;
            _type = ColumnType.Default;
            _numericStandardFormat = NumericStandardFormat.Default;
            _datetimeStandardFormat = DateTimeStandardFormat.Default;
            _displayName = "";
        }

        MetaColumn _metaColumn = null;
        [XmlIgnore, Browsable(false)]
        public MetaColumn MetaColumn
        {
            get
            {
                if (_metaColumn == null && !string.IsNullOrEmpty(_metaColumnGUID))
                {
                    if (Model != null && Model.IsSQLModel) _metaColumn = Model.Table.Columns.FirstOrDefault(i => i.GUID == MetaColumnGUID);
                    else if (Source != null && Source.MetaData != null) _metaColumn = Source.MetaData.GetColumnFromGUID(MetaColumnGUID);
                }
                return _metaColumn;
            }
            set
            {
                _metaColumn = value;
            }
        }

        public void SetSourceReference(MetaSource source)
        {
            _metaColumn = null;
            _source = source;
        }

        public string RawDisplayName
        {
            get
            {
                if (MetaColumn != null && PivotPosition == PivotPosition.Data && AggregateFunction != AggregateFunction.Sum && !MetaColumn.IsAggregate) return string.Format("{0} {1}", Report.Translate(Helper.GetEnumDescription(typeof(AggregateFunction), AggregateFunction) + " of"), MetaColumn.DisplayName);
                else if (MetaColumn != null) return MetaColumn.DisplayName;
                return "";
            }
        }


        protected string _SQL;
        [Category("Advanced"), DisplayName("Custom SQL"), Description("If not empty, overwrite the default SQL used for the element in the SELECT statement."), Id(1, 5)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string SQL
        {
            get
            {
                return _SQL;
            }
            set { _SQL = value; }
        }


        string _cellScript;
        [Category("Advanced"), DisplayName("Cell Script"), Description("If not empty, the script is executed to calculate custom cell value and CSS."), Id(3, 5)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string CellScript
        {
            get { return _cellScript; }
            set { _cellScript = value; }
        }

        [DefaultValue(null)]
        [Category("Advanced"), DisplayName("Custom Enumerated List"), Description("If defined, the enumerated list is used for the display and for sorting."), Id(4, 5)]
        [TypeConverter(typeof(MetaEnumConverter))]
        public string EnumGUIDEL
        {
            get { return _enumGUID; }
            set { _enumGUID = value; }
        }

        YesNoDefault _forceAggregate = YesNoDefault.Default;
        [DefaultValue(YesNoDefault.Default)]
        [Category("Advanced"), DisplayName("Force aggregrate"), Description("If Yes, it indicates that the element is an aggregate even it is set in a dimension (Page/Row/Column). By default, the metacolumn flag 'Is Aggregate' is used. This flag impacts the build of the GROUP BY Clause."), Id(5, 5)]
        public YesNoDefault ForceAggregate
        {
            get { return _forceAggregate; }
            set { _forceAggregate = value; }
        }
        public bool ShouldSerializeHasAggregate() { return _forceAggregate != YesNoDefault.Default; }

        [XmlIgnore, Browsable(false)]
        public bool IsAggregateEl
        {
            get
            {
                if (_forceAggregate == YesNoDefault.Yes) return true;
                if (_forceAggregate == YesNoDefault.No) return false;
                if (MetaColumn != null) return MetaColumn.IsAggregate;
                return false;
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool IsCommonRestrictionValue
        {
            get
            {
                return string.IsNullOrEmpty(MetaColumnGUID);
            }
        }

        [XmlIgnore]
        public bool IsCommonValue = false;

        [XmlIgnore]
        public MetaEnum EnumEL
        {
            get
            {
                if (Enum != null) return Enum;
                if (IsCommonRestrictionValue) return null;
                return MetaColumn.Enum;
            }
        }

        [XmlIgnore, Browsable(false)]
        public string RawSQLColumn
        {
            get
            {
                if (IsCommonRestrictionValue) return Name;

                string result = MetaColumn.Name;
                if (PivotPosition == PivotPosition.Data && !MetaColumn.IsAggregate)
                {
                    //aggregate
                    result = string.Format("{0}({1})", AggregateFunction, result);
                }
                return result;
            }
        }

        [XmlIgnore, Browsable(false)]
        public string SQLColumn
        {
            get
            {
                return !string.IsNullOrEmpty(_SQL) ? _SQL : RawSQLColumn;
            }
        }

        private string _SQLColumnName;
        [XmlIgnore, Browsable(false)]
        public string SQLColumnName
        {
            get { return string.IsNullOrEmpty(_SQLColumnName) && !IsCommonRestrictionValue ? MetaColumn.Name : _SQLColumnName; }
            set { _SQLColumnName = value; }
        }

        protected Report _report;
        [XmlIgnore, Browsable(false)]
        public Report Report
        {
            get { return _report; }
            set { _report = value; }
        }

        protected ReportModel _model;
        [XmlIgnore, Browsable(false)]
        public ReportModel Model
        {
            get { return _model; }
            set { _model = value; }
        }

        [XmlIgnore, Browsable(false)]
        public bool IsForNavigation = false;

        public string GetD3Format(CultureInfo culture, string NVD3ChartType)
        {
            //try to convert from .net to d3 format... from https://github.com/mbostock/d3/wiki/Formatting
            if (IsNumeric)
            {
                string format = FormatEl;
                if (format == "0" || format == "N" || format == "D" || format == "G") return ".0f";
                else if (format == "N0" || format == "D0" || format == "G0") return ",.0f";
                else if (format == "N1" || format == "D1" || format == "G1") return ",.1f";
                else if (format == "N2" || format == "D2" || format == "G2") return ",.2f";
                else if (format == "N3" || format == "D3" || format == "G3") return ",.3f";
                else if (format == "N4" || format == "D4" || format == "G4") return ",.4f";
                else if (format == "P0") return ",.0%";
                else if (format == "P1") return ",.1%";
                else if (format == "P2") return ",.2%";
                else if (format == "P3") return ",.3%";
                else if (format == "P4") return ",.4%";
                else if (format == "C0") return "$,.0f";
                else if (format == "C1") return "$,.1f";
                else if (format == "C2") return "$,.2f";
                else if (format == "C3") return "$,.3f";
                else if (format == "C4") return "$,.4f";
            }
            else if (IsDateTime)
            {
                string format = FormatEl;
                if (format == "d") format = culture.DateTimeFormat.ShortDatePattern;
                else if (format == "D") format = culture.DateTimeFormat.LongDatePattern;
                else if (format == "t") format = culture.DateTimeFormat.ShortTimePattern;
                else if (format == "T") format = culture.DateTimeFormat.LongTimePattern;
                else if (format == "g") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "G") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
                else if (format == "f") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "F") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.LongTimePattern;

                StringBuilder result = new StringBuilder();
                for (int i = 0; i < format.Length; i++)
                {
                    if (Helper.FindReplacePattern(format, ref i, "dddd", "%A", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "ddd", "%a", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "dd", "%d", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "d", "%e", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "MMMM", "%B", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "MMM", "%b", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "MM", "%m", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "M", "%m", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "yyyy", "%Y", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "yyy", "%Y", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "yy", "%y", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "y", "%y", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "HH", "%H", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "H", "%H", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "hh", "%I", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "h", "%I", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "mm", "%M", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "m", "%M", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "ss", "%S", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "s", "%S", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "FFF", "%L", result)) continue;
                    result.Append(format[i]);
                }
                return result.ToString().Replace("/", culture.DateTimeFormat.DateSeparator);
            }
            return "g";
        }

        public string GetMomentJSFormat(CultureInfo culture)
        {
            return Helper.ToMomentJSFormat(culture, FormatEl);
        }

        public string GetExcelFormat(CultureInfo culture)
        {
            //try to convert from .net to Excel format... 
            string format = FormatEl;
            if (IsNumeric)
            {
                if (format == "N1" || format == "D1") return "#,##0.0";
                else if (format == "N2" || format == "D2") return "#,##0.00";
                else if (format == "N3" || format == "D3") return "#,##0.000";
                else if (format == "N4" || format == "D4") return "#,##0.0000";
                else if (format.StartsWith("N") || format.StartsWith("D")) return "#,##0";
                else if (format == "P1") return "0.0%";
                else if (format == "P2") return "0.00%";
                else if (format == "P3") return "0.00%";
                else if (format == "P4") return "0.0000%";
                else if (format.StartsWith("P")) return "0%";
                else if (format == "C1") return "$ #,##0.0";
                else if (format == "C2") return "$ #,##0.00";
                else if (format == "C3") return "$ #,##0.000";
                else if (format == "C4") return "$ #,##0.0000";
                else if (format.StartsWith("C")) return "$ #,##0";
                else if (format.StartsWith("N") || format.StartsWith("D") || format.StartsWith("E") || format.StartsWith("F") || format.StartsWith("G") || format.StartsWith("H")) return "";
            }
            else if (IsDateTime)
            {
                if (format == "d") return culture.DateTimeFormat.ShortDatePattern;
                else if (format == "D") return culture.DateTimeFormat.LongDatePattern;
                else if (format == "t") return culture.DateTimeFormat.ShortTimePattern;
                else if (format == "T") return culture.DateTimeFormat.LongTimePattern;
                else if (format == "g") return culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "G") return culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
                else if (format == "f") return culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "F") return culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
                else if (format == "g") return culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            }
            return FormatEl;
        }
    }
}
