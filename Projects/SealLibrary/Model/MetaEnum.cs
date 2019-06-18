//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Design;
using System.Xml.Serialization;
using System.Data;
using Seal.Forms;
using Seal.Helpers;
using System.Data.Common;

namespace Seal.Model
{
    public class MetaEnum : RootComponent
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
                GetProperty("IsDynamic").SetIsBrowsable(IsSQL);
                GetProperty("IsDbRefresh").SetIsBrowsable(IsSQL);
                GetProperty("UsePosition").SetIsBrowsable(true);
                GetProperty("Translate").SetIsBrowsable(true);
                GetProperty("Sql").SetIsBrowsable(IsSQL);

                GetProperty("SqlDisplay").SetIsBrowsable(IsSQL);
                GetProperty("FilterChars").SetIsBrowsable(IsSQL);
                GetProperty("Message").SetIsBrowsable(IsSQL);

                GetProperty("Values").SetIsBrowsable(IsEditable);
                GetProperty("NumberOfValues").SetIsBrowsable(true);

                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);
                GetProperty("HelperRefreshEnum").SetIsBrowsable(IsSQL);

                //Read only
                GetProperty("SqlDisplay").SetIsReadOnly(!IsDynamic);
                GetProperty("IsDbRefresh").SetIsReadOnly(!IsDynamic);
                GetProperty("Values").SetIsReadOnly(!IsDynamic);
                GetProperty("NumberOfValues").SetIsReadOnly(true);

                GetProperty("FilterChars").SetIsReadOnly(!HasDynamicDisplay || !HasFilters);
                GetProperty("Message").SetIsReadOnly(!HasDynamicDisplay || (!HasFilters && !HasDependencies));

                GetProperty("Information").SetIsReadOnly(true);
                GetProperty("Error").SetIsReadOnly(true);
                GetProperty("HelperRefreshEnum").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        public static MetaEnum Create()
        {
            return new MetaEnum() { GUID = Guid.NewGuid().ToString() };
        }

        [DefaultValue(null)]
        [Category("Definition"), DisplayName("Name"), Description("Name of the enumerated list."), Id(1, 1)]
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private Boolean _isDynamic = false;
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("List is dynamically loaded from database"), Description("If True, the list is loaded using the SQL Select Statement defined."), Id(2, 1)]
        public Boolean IsDynamic
        {
            get { return _isDynamic; }
            set
            {
                _isDynamic = value;
                UpdateEditorAttributes();
            }
        }

        private Boolean _isDbRefresh = false;
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("List is refreshed upon database connection"), Description("If True, the list is loaded before a report execution. Should be set to False if the SQL has poor performances."), Id(3, 1)]
        public Boolean IsDbRefresh
        {
            get { return _isDbRefresh; }
            set { _isDbRefresh = value; }
        }

        public bool HasDynamicDisplay
        {
            get { return !string.IsNullOrEmpty(SqlDisplay) && IsDynamic && IsDbRefresh; }
        }

        [XmlIgnore]
        private string _sql;
        [Category("Definition"), DisplayName("SQL Select Statement"), Description("If the list is loaded from the database, SQL Select statement with 1, 2, 3, 4 or 5 columns used to build the list of values. The first column is used for the identifier, the second optional column is the display value shown in the table result, the third optional column is the display value shown in the restriction list, the fourth optional column defines a custom CSS Style applied to the result cell, the fifth optional column defines a custom CSS Class applied to the result cell."), Id(4, 1)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string Sql
        {
            get { return _sql; }
            set
            {
                _sql = value;
                if (IsDynamic && IsDbRefresh && !string.IsNullOrEmpty(_sql) && _dctd != null) RefreshEnum();
            }
        }

        private Boolean _usePosition = false;
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("Use defined position to sort in reports"), Description("If True, the current position of the values in the list is used to sort the column in the report result."), Id(5, 1)]
        public Boolean UsePosition
        {
            get { return _usePosition; }
            set { _usePosition = value; }
        }

        private Boolean _translate = false;
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("Translate values"), Description("If True, the enumerated values are translated using the Repository translations."), Id(6, 1)]
        public Boolean Translate
        {
            get { return _translate; }
            set { _translate = value; }
        }

        [XmlIgnore]
        private string _sqlDisplay;
        [Category("Dynamic Display"), DisplayName("SQL Select Statement for prompted restriction"), Description("SQL Select Statement used to build the values displayed in a prompted restriction. The SQL can return 1 to 5 columns and follows the definition of 'SQL Select Statement' property. It can contain either the '{EnumFilter}' and/or '{EnumValues_<Name>}' keywords where <Name> is the name of another prompted enumerated list. The SQL is used only if the list is dynamic, refreshed upon database connection."), Id(1, 2)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string SqlDisplay
        {
            get { return _sqlDisplay; }
            set { _sqlDisplay = value;}
        }

        //List is built when prompted with xx char
        private int _filterChars = 2;
        [DefaultValue(2)]
        [Category("Dynamic Display"), DisplayName("Filter characters to type"), Description("If the list is dynamic, refreshed upon database connection and the SQL for prompted restriction contains the '{EnumFilter}' keyword, the number of characters typed by the used in the filter box before the enum is built and displayed."), Id(2, 2)]
        public int FilterChars
        {
            get { return _filterChars; }
            set { _filterChars = value; }
        }
        public bool ShouldSerializeFilterChars() { return HasFilters; }

        [XmlIgnore()]
        public bool HasFilters
        {
            get { return !string.IsNullOrEmpty(SqlDisplay) && SqlDisplay.Contains(Repository.EnumFilterKeyword); }
        }

        [XmlIgnore]
        public bool HasDependencies
        {
            get { return !string.IsNullOrEmpty(SqlDisplay) && SqlDisplay.Contains(Repository.EnumValuesKeyword); }
        }

        private string _message;
        [Category("Dynamic Display"), DisplayName("Information message"), Description("If the list is dynamic, refreshed upon database connection and has filter characters or dependencies, the message displayed to the end user to trigger the list (e.g. 'Select a country first' or 'Type 5 characters')."), Id(4, 2)]
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        private List<MetaEV> _values = new List<MetaEV>();
        [DefaultValue(null)]
        [Category("Values"), DisplayName("Values"), Description("The list of values used for this enumerated list"), Id(1, 3)]
        [Editor(typeof(EnumValueCollectionEditor), typeof(UITypeEditor))]
        public List<MetaEV> Values
        {
            get
            {
                if (HasDynamicDisplay && _values.Count == 0 && string.IsNullOrEmpty(_error)) RefreshEnum();
                return _values;
            }
            set { _values = value; }
        }
        public bool ShouldSerializeValues() { return !HasDynamicDisplay && _values.Count > 0; }

        [Category("Values"), DisplayName("Number of Values"), Description("The number of values in the collection"), Id(2, 3)]
        public int NumberOfValues
        {
            get { return Values.Count; }
        }

        [XmlIgnore]
        public bool IsEditable = true;

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
            get { return !_source.IsNoSQL; }
        }


        List<MetaEV> getValues(DbConnection connection, string sql)
        {
            var result = new List<MetaEV>();
            DataTable table = Helper.GetDataTable(connection, sql);

            if (table.Columns.Count > 0)
            {
                foreach (DataRow row in table.Rows)
                {
                    if (!row.IsNull(0))
                    {
                        MetaEV value = new MetaEV();
                        value.Id = row[0].ToString();
                        value.Val = table.Columns.Count > 1 ? (row.IsNull(1) ? null : row[1].ToString()) : null;
                        value.ValR = table.Columns.Count > 2 ? (row.IsNull(2) ? null : row[2].ToString()) : null;
                        value.Css = table.Columns.Count > 3 ? (row.IsNull(3) ? null : row[3].ToString()) : null;
                        value.Class = table.Columns.Count > 4 ? (row.IsNull(4) ? null : row[4].ToString()) : null;
                        result.Add(value);
                    }
                }
            }

            return result;
        }


        public List<MetaEV> GetSubSetValues(string filter, Dictionary<MetaEnum, string> dependencies)
        {
            DbConnection connection = _source.GetOpenConnection();

            var finalSQL = RazorHelper.CompileExecute(SqlDisplay, this);
            if (HasDynamicDisplay) finalSQL = finalSQL.Replace(Repository.EnumFilterKeyword + "}", filter);
            if (HasDynamicDisplay && dependencies != null)
            {
                foreach (var d in dependencies.Keys)
                {
                    finalSQL = finalSQL.Replace(Repository.EnumValuesKeyword + d.Name + "}", dependencies[d]);
                }
            }
            finalSQL = Helper.ClearAllSQLKeywords(finalSQL);

            return getValues(connection, finalSQL);
        }


        public void RefreshEnum(bool checkOnly = false)
        {
            if (_source == null || !IsDynamic || string.IsNullOrEmpty(Sql)) return;

            try
            {
                _error = "";
                _information = "";
                DbConnection connection = _source.GetOpenConnection();
                var result = getValues(connection, RazorHelper.CompileExecute(Sql, this));
                connection.Close();
                if (checkOnly) return;

                Values = result;
                _information = string.Format("List refreshed with {0} value(s).", Values.Count);
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _information = "Error got when refreshing the values.";
            }
            _information = Helper.FormatMessage(_information);
            UpdateEditorAttributes();
        }

        public string GetDisplayValue(string id, bool forRestriction = false)
        {
            MetaEV value = Values.FirstOrDefault(i => i.Id == id);
            return value == null ? id : (forRestriction ? value.DisplayRestriction : value.DisplayValue);
        }

        #region Helpers

        [Category("Helpers"), DisplayName("Refresh values"), Description("Refresh values of this list using the SQL Statement."), Id(1, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperRefreshEnum
        {
            get { return "<Click to refresh enum values>"; }
        }

        string _information;
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message when the enum list has been refreshed."), Id(2, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        string _error;
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(3, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }


        #endregion


    }
}
