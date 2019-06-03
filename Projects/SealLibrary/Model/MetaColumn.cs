//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;
using Seal.Converter;
using DynamicTypeDescriptor;
using Seal.Helpers;
using System.Drawing.Design;
using Seal.Forms;

namespace Seal.Model
{
    public class MetaColumn : RootComponent, ITreeSort
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Name").SetIsBrowsable(true);
                GetProperty("Type").SetIsBrowsable(true);
                GetProperty("IsAggregate").SetIsBrowsable(IsSQL);
                GetProperty("Category").SetIsBrowsable(true);
                GetProperty("Tag").SetIsBrowsable(true);
                GetProperty("DisplayName").SetIsBrowsable(true);
                GetProperty("DisplayOrder").SetIsBrowsable(true);
                GetProperty("Format").SetIsBrowsable(true);
                GetProperty("EnumGUID").SetIsBrowsable(true);
                GetProperty("DrillChildren").SetIsBrowsable(true);
                GetProperty("DrillChildren").SetDisplayName("Drill Children: " + (_drillChildren.Count == 0 ? "None" : _drillChildren.Count.ToString() + " Items(s)"));
                GetProperty("DrillUpOnlyIfDD").SetIsBrowsable(true);
                GetProperty("SubReports").SetIsBrowsable(true);
                GetProperty("SubReports").SetDisplayName("Sub-Reports: " + (_subReports.Count == 0 ? "None" : _subReports.Count.ToString() + " Items(s)"));

                GetProperty("HelperCreateSubReport").SetIsBrowsable(IsSQL);
                GetProperty("HelperAddSubReport").SetIsBrowsable(IsSQL);
                GetProperty("HelperOpenSubReportFolder").SetIsBrowsable(IsSQL);
                GetProperty("HelperCheckColumn").SetIsBrowsable(IsSQL);
                GetProperty("HelperCreateEnum").SetIsBrowsable(true);
                GetProperty("HelperShowValues").SetIsBrowsable(true);
                GetProperty("HelperCreateDrillDates").SetIsBrowsable(Type == ColumnType.DateTime && (Source.Connection.DatabaseType == DatabaseType.MSAccess || Source.Connection.DatabaseType == DatabaseType.Oracle || Source.Connection.DatabaseType == DatabaseType.MSSQLServer));
                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);

                GetProperty("NumericStandardFormat").SetIsBrowsable(Type == ColumnType.Numeric);
                GetProperty("DateTimeStandardFormat").SetIsBrowsable(Type == ColumnType.DateTime);

                //Read only
                GetProperty("Format").SetIsReadOnly((Type == ColumnType.Numeric && NumericStandardFormat != NumericStandardFormat.Custom) || (Type == ColumnType.DateTime && DateTimeStandardFormat != DateTimeStandardFormat.Custom));

                GetProperty("HelperCreateSubReport").SetIsReadOnly(true);
                GetProperty("HelperAddSubReport").SetIsReadOnly(true);
                GetProperty("HelperOpenSubReportFolder").SetIsReadOnly(true);
                GetProperty("HelperCheckColumn").SetIsReadOnly(true);
                GetProperty("HelperCreateEnum").SetIsReadOnly(true);
                GetProperty("HelperShowValues").SetIsReadOnly(true);
                GetProperty("Information").SetIsReadOnly(true);
                GetProperty("Error").SetIsReadOnly(true);

                GetProperty("Name").SetIsReadOnly(!IsSQL);
                GetProperty("Type").SetIsReadOnly(!IsSQL);

                TypeDescriptor.Refresh(this);
            }
        }

        override public void SetReadOnly()
        {
            base.SetReadOnly();
            GetProperty("HelperCreateEnum").SetIsBrowsable(false);
            GetProperty("HelperCreateSubReport").SetIsBrowsable(false);
            GetProperty("HelperAddSubReport").SetIsBrowsable(false);
            TypeDescriptor.Refresh(this);
        }

        public void HideSubReports()
        {
            GetProperty("SubReports").SetIsBrowsable(false);
            GetProperty("HelperOpenSubReportFolder").SetIsBrowsable(false);
            GetProperty("HelperCreateSubReport").SetIsBrowsable(false);
            GetProperty("HelperAddSubReport").SetIsBrowsable(false);
            TypeDescriptor.Refresh(this);
        }
        #endregion

        public static MetaColumn Create(string name)
        {
            MetaColumn result = new MetaColumn() { Name = name, DisplayName = name, Type = ColumnType.Text, Category = "Default" };
            result.GUID = Guid.NewGuid().ToString();
            return result;
        }


        [DefaultValue(null)]
        [DisplayName("Name"), Description("The name of the column in the table or the SQL Statement used for the column."), Category("Definition"), Id(1, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        protected ColumnType _type = ColumnType.Default;
        [DefaultValue(ColumnType.Default)]
        [Category("Definition"), DisplayName("Data Type"), Description("Data type of the column."), Id(2, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public ColumnType Type
        {
            get { return _type; }
            set
            {
                _type = value;
                UpdateEditorAttributes();
            }
        }

        private bool _isAggregate = false;
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("Is Aggregate"), Description("Must be True if the column contains SQL aggregate functions like SUM,MIN,MAX,COUNT,AVG."), Id(3, 1)]
        public bool IsAggregate
        {
            get { return _isAggregate; }
            set { _isAggregate = value; }
        }
        public bool ShouldSerializeIsAggregate() { return _isAggregate; }


        private string _category;
        [Category("Display"), DisplayName("Category Name"), Description("Category used to display the column in the Report Designer tree view. Category hierarchy can be defined using the '/' character (e.g. 'Master/Name1/Name2')"), Id(2, 2)]
        public string Category
        {
            get { return _category; }
            set { _category = value; }
        }

        private string _tag;
        [Category("Security"), DisplayName("Security Tag"), Description("Tag used to define the security of the Web Report Designer (Columns of the Security Groups defined in the Web Security)."), Id(2, 2)]
        public string Tag
        {
            get { return string.IsNullOrEmpty(_tag) ? "" : _tag; }
            set { _tag = value; }
        }
        public bool ShouldSerializeTag() { return !string.IsNullOrEmpty(_tag); }

        protected string _displayName;
        [Category("Display"), DisplayName("Display Name"), Description("Name used to display the column in the Report Designer tree view and in the report results."), Id(3, 2)]
        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        int _displayOrder = 0;
        [DefaultValue(0)]
        [Category("Display"), DisplayName("Display Order"), Description("The order number used to sort the column in the tree view (by table and by category)."), Id(4, 2)]
        public int DisplayOrder
        {
            get { return _displayOrder; }
            set { _displayOrder = value; }
        }
        public bool ShouldSerializeDisplayOrder() { return _displayOrder != 0; }

        public int GetSort()
        {
            return _displayOrder;
        }


        protected NumericStandardFormat _numericStandardFormat = NumericStandardFormat.Default;
        [DefaultValue(NumericStandardFormat.Default)]
        [Category("Options"), DisplayName("Format"), Description("Standard display format applied to the element."), Id(2, 3)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public NumericStandardFormat NumericStandardFormat
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
        public bool ShouldSerializeNumericStandardFormat() { return _numericStandardFormat != NumericStandardFormat.Default; }

        protected DateTimeStandardFormat _datetimeStandardFormat = DateTimeStandardFormat.Default;
        [DefaultValue(DateTimeStandardFormat.Default)]
        [Category("Options"), DisplayName("Format"), Description("Standard display format applied to the element."), Id(2, 3)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public DateTimeStandardFormat DateTimeStandardFormat
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
        public bool ShouldSerializeDateTimeStandardFormat() { return _datetimeStandardFormat != DateTimeStandardFormat.Default; }

        protected string _format = "";
        [Category("Options"), DisplayName("Custom Format"), Description("If not empty, specify the format of the elements values displayed in the result tables (.Net Format Strings)."), Id(3, 3)]
        [TypeConverter(typeof(CustomFormatConverter))]
        public string Format
        {
            get {
                SetDefaultFormat();
                return _format; 
            }
            set { 
                _format = value;
            }
        }
        public bool ShouldSerializeFormat() { return !string.IsNullOrEmpty(_format); }

        [XmlIgnore]
        public bool HasTime
        {
            get
            {
                if (Type != ColumnType.DateTime) return false;
                return HasTimeFormat(DateTimeStandardFormat, Format);
            }
        }

        static public bool HasTimeFormat(DateTimeStandardFormat formatType, string format)
        {
            if (formatType.ToString().Contains("Time")) return true;
            return ((formatType == DateTimeStandardFormat.Custom || formatType == DateTimeStandardFormat.Default)  
                && (format.ToLower().Contains("t") || format.Contains("H") || format.Contains("m") || format.Contains("s")));
        }

        public void SetStandardFormat()
        {
            ColumnType type = Type;
            if (this is ReportElement)
            {
                //Force the type of the ReportElement
                ReportElement element = (ReportElement)this;
                if (element.IsDateTime) type = ColumnType.DateTime;
                else if (element.IsNumeric) type = ColumnType.Numeric;
                else type = ColumnType.Text;
            }
            SetDefaultFormat();
            if (type == ColumnType.Numeric && NumericStandardFormat != NumericStandardFormat.Custom && NumericStandardFormat != NumericStandardFormat.Default)
            {
                _format = Helper.ConvertNumericStandardFormat(NumericStandardFormat);
            }
            else if (type == ColumnType.DateTime && DateTimeStandardFormat != DateTimeStandardFormat.Custom && DateTimeStandardFormat != DateTimeStandardFormat.Default)
            {
                _format = Helper.ConvertDateTimeStandardFormat(DateTimeStandardFormat);
            }
        }

        protected void SetDefaultFormat()
        {
            if (this is ReportElement)
            {
                //Force the type of the ReportElement
                ReportElement element = (ReportElement)this;
                if (element.MetaColumn == null) return;

                element.MetaColumn.SetDefaultFormat();
                if (element.IsNumeric && NumericStandardFormat == NumericStandardFormat.Default)
                {
                    _format = element.MetaColumn.Type == ColumnType.Numeric ? element.MetaColumn.Format : Source.Repository.Configuration.NumericFormat;
                }
                else if (element.IsDateTime && DateTimeStandardFormat == DateTimeStandardFormat.Default)
                {
                    _format = element.MetaColumn.Type == ColumnType.DateTime ? element.MetaColumn.Format : Source.Repository.Configuration.DateTimeFormat;
                }
            }
            else
            {
                if (Type == ColumnType.Numeric && NumericStandardFormat == NumericStandardFormat.Default) _format = Source.Repository.Configuration.NumericFormat;
                else if (Type == ColumnType.DateTime && DateTimeStandardFormat == DateTimeStandardFormat.Default) _format = Source.Repository.Configuration.DateTimeFormat;
            }
        }

        protected string _enumGUID;
        [DefaultValue(null)]
        [Category("Options"), DisplayName("Enumerated List"), Description("If defined, a list of values is proposed when the column is used for restrictions."), Id(4, 3)]
        [TypeConverter(typeof(MetaEnumConverter))]
        public string EnumGUID
        {
            get { return _enumGUID; }
            set { _enumGUID = value; }
        }
        public bool ShouldSerializeEnumGUID() { return !string.IsNullOrEmpty(_enumGUID); }

        [XmlIgnore]
        public MetaEnum Enum
        {
            get {
                MetaEnum result = null;
                if (!string.IsNullOrEmpty(_enumGUID))
                {
                    if (_source  != null) result = _source.MetaData.Enums.FirstOrDefault(i => i.GUID == _enumGUID);
                    else if (this is ReportRestriction)
                    {
                        //task restriction
                        var restriction = this as ReportRestriction;
                        foreach (var source in restriction.Report.Sources)
                        {
                            result = source.MetaData.Enums.FirstOrDefault(i => i.GUID == _enumGUID);
                            if (result != null) break;
                        }
                    }
                }
                return result;
            }
        }

        protected MetaSource _source;
        [XmlIgnore, Browsable(false)]
        public MetaSource Source
        {
            get { return _source; }
            set
            {
                _source = value;
            }
        }

        [XmlIgnore]
        public bool IsSQL
        {
            get { return _source == null || !_source.IsNoSQL; }
        }

        MetaTable _metaTable = null;
        [XmlIgnore, Browsable(false)]
        public MetaTable MetaTable
        {
            get
            {
                if (_metaTable == null && _source != null)
                {
                    foreach (MetaTable table in _source.MetaData.Tables)
                    {
                        if (table.Columns.Exists(i => i.GUID == GUID))
                        {
                            _metaTable = table;
                            break;
                        }
                    }
                }
                return _metaTable;
            }
            set
            {
                _metaTable = value;
            }
        }


        [XmlIgnore]
        public string FullDisplayName
        {
            get {
                if (MetaTable == null) return _displayName;
                return string.Format("{0}.{1}", string.IsNullOrEmpty(MetaTable.Name) ? MetaTable.Alias : MetaTable.Name, _displayName);
            }
        }

        [XmlIgnore]
        public string DisplayName2
        {
            get
            {
                return string.Format("{0} ({1})", _name, _displayName);
            }
        }

        List<string> _drillChildren = new List<string>();
        [Category("Drill"), DisplayName("Drill Children"), Description("Defines the child columns to navigate from this column with the drill feature."), Id(1, 4)]
        [Editor(typeof(ColumnsSelector), typeof(UITypeEditor))]
        public List<string> DrillChildren
        {
            get { return _drillChildren; }
            set { _drillChildren = value; }
        }
        public bool ShouldSerializeDrillChildren() { return _drillChildren.Count > 0; }

        bool _drillUpOnlyIfDD = false;
        [DefaultValue(false)]
        [Category("Drill"), DisplayName("Drill Up only if drill down occured."), Description("If true, Drill Up is activated only if a drill down occured."), Id(2, 4)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public bool DrillUpOnlyIfDD
        {
            get { return _drillUpOnlyIfDD; }
            set { _drillUpOnlyIfDD = value; }
        }

        [Category("Drill"), DisplayName("Create Date Columns for Drill"), Description("Create automatically a 'Year' column and a 'Month' column to drill down to the date."), Id(3, 4)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperCreateDrillDates
        {
            get { return "<Click to create a 'Year' and a 'Month' column having Drill navigation>"; }
        }

        List<SubReport> _subReports = new List<SubReport>();
        [Category("Sub-Reports"), DisplayName("Sub Reports"), Description("Defines sub-reports to navigate from this column."), Id(1, 5)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<SubReport> SubReports
        {
            get { return _subReports; }
            set { _subReports = value; }
        }
        public bool ShouldSerializeSubReports() { return _subReports.Count > 0; }

        [Category("Sub-Reports"), DisplayName("Create a Sub-Report"), Description("Create a Sub-Report to display the detail of this table."), Id(2, 5)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperCreateSubReport
        {
            get { return "<Click to create a Sub-Report to display the detail of this table>"; }
        }

        [Category("Sub-Reports"), DisplayName("Add an existing Sub-Report"), Description("Add an existing Sub-Report to this column."), Id(2, 5)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperAddSubReport
        {
            get { return "<Click to add an existing Sub-Report to this column>"; }
        }

        [Category("Sub-Reports"), DisplayName("Open Sub-Report folder"), Description("Open the Sub-Report folder in Windows Explorer."), Id(3, 5)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperOpenSubReportFolder
        {
            get { return "<Click to open the Sub-Report folder in Windows Explorer>"; }
        }

        #region Helpers

        [Category("Helpers"), DisplayName("Check column SQL syntax"), Description("Check the column SQL statement in the database."), Id(1, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperCheckColumn
        {
            get { return "<Click to check the column SQL syntax>"; }
        }

        [Category("Helpers"), DisplayName("Create enumerated list from this column"), Description("Click to create an enumerated list from this table column."), Id(2, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperCreateEnum
        {
            get { return "<Click to create an enum from the column>"; }
        }

        [Category("Helpers"), DisplayName("Show column values"), Description("Show the first 1000 values of the column."), Id(3, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperShowValues
        {
            get { return "<Click to show the column values>"; }
        }

        string _information;
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message ther column has been checked."), Id(5, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        string _error;
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(6, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        #endregion

    }
}
