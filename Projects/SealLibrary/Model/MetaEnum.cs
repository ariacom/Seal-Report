//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Seal.Converter;
using DynamicTypeDescriptor;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Xml.Serialization;
using System.Data.OleDb;
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
                GetProperty("Values").SetIsBrowsable(IsEditable);
                GetProperty("NumberOfValues").SetIsBrowsable(true);
                GetProperty("Width").SetIsBrowsable(true);

                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);
                GetProperty("HelperRefreshEnum").SetIsBrowsable(IsSQL);

                //Read only
                GetProperty("Sql").SetIsReadOnly(!IsDynamic);
                GetProperty("IsDbRefresh").SetIsReadOnly(!IsDynamic);
                GetProperty("Values").SetIsReadOnly(!IsDynamic);
                GetProperty("NumberOfValues").SetIsReadOnly(true);

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

        [Category("Definition"), DisplayName("Name"), Description("Name of the enumerated list."), Id(1, 1)]
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private Boolean _isDynamic = false;
        [Category("Definition"), DisplayName("List is dynamically loaded from database"), Description("If True, the list is loaded using the Select SQL statement defined."), Id(2, 1)]
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
        [Category("Definition"), DisplayName("List is refreshed upon database connection"), Description("If True, the list is loaded before a report execution. Should be set to False if the SQL has poor performances."), Id(3, 1)]
        public Boolean IsDbRefresh
        {
            get { return _isDbRefresh; }
            set { _isDbRefresh = value; }
        }

        private Boolean _usePosition = false;
        [Category("Definition"), DisplayName("Use defined position to sort in reports"), Description("If True, the current position of the values in the list is used to sort the column in the report result."), Id(5, 1)]
        public Boolean UsePosition
        {
            get { return _usePosition; }
            set { _usePosition = value; }
        }

        private Boolean _translate = false;
        [Category("Definition"), DisplayName("Translate values"), Description("If True, the enumerated values are transalted using the Repository translations."), Id(6, 1)]
        public Boolean Translate
        {
            get { return _translate; }
            set { _translate = value; }
        }

        public string DefaultSQL = "select col1,col2 from table order by col2";

        private string _sql;
        [Category("Definition"), DisplayName("Select SQL Statement"), Description("If the list is loaded from the database, SQL Select statement with 1 or 2 columns used to build the list of values. The first column is used for the identifier value, the second optional column is used for the display value."), Id(4, 1)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string Sql
        {
            get { return _sql; }
            set { 
                _sql = value;
                if (IsDynamic && !string.IsNullOrEmpty(_sql) && _dctd != null) RefreshEnum();
            }
        }

        private List<MetaEV> _values = new List<MetaEV>();
        [Category("Values"), DisplayName("Values"), Description("The list of values used for this enumerated list"), Id(1, 2)]
        [Editor(typeof(EnumValueCollectionEditor), typeof(UITypeEditor))]
        public List<MetaEV> Values
        {
            get { return _values; }
            set { _values = value; }
        }

        [Category("Values"), DisplayName("Number of Values"), Description("The number of values in the collection"), Id(2, 2)]
        public int NumberOfValues
        {
            get { return Values.Count; }
        }

        private int _width = 160;
        [Category("Display"), DisplayName("Width in pixels"), Description("The width of the list when displayed in the restrictions panel."), Id(1, 3)]
        public int Width
        {
            get { return _width; }
            set { _width = value; }
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


        public void RefreshEnum(bool checkOnly = false)
        {
            if (_source == null || !IsDynamic) return;

            try
            {
                _error = "";
                _information = "";
                DbConnection connection = _source.GetOpenConnection();
                DataTable table = Helper.GetDataTable(connection, Sql);
                connection.Close();

                if (checkOnly) return;

                Values.Clear();
                foreach (DataRow row in table.Rows)
                {
                    if (table.Columns.Count > 0)
                    {
                        if (!row.IsNull(0))
                        {
                            MetaEV value = new MetaEV();
                            value.Id = row[0].ToString();
                            value.Val = table.Columns.Count > 1 ? (row.IsNull(1) ? "" : row[1].ToString()) : value.Id;
                            Values.Add(value);
                        }
                    }
                }
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

        public string GetDisplayValue(string id)
        {
            MetaEV value = Values.FirstOrDefault(i => i.Id == id);
            return value == null ? id : value.Val;
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
