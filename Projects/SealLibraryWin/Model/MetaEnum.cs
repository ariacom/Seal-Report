//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Data;
using Seal.Helpers;
using System.Data.Common;
#if WINDOWS
using DynamicTypeDescriptor;
using System.Drawing.Design;
using Seal.Forms;
#endif

namespace Seal.Model
{
    /// <summary>
    /// A MetaEnum defines an enumerated list in a MetaData
    /// </summary>
    public class MetaEnum : RootComponent
    {
#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Name").SetIsBrowsable(true);
                GetProperty("IsDynamic").SetIsBrowsable(true);
                GetProperty("IsDbRefresh").SetIsBrowsable(true);
                GetProperty("ValuesPerConnection").SetIsBrowsable(true);
                GetProperty("UsePosition").SetIsBrowsable(true);
                GetProperty("Translate").SetIsBrowsable(true);
                GetProperty("Sql").SetIsBrowsable(true);
                GetProperty("Script").SetIsBrowsable(true);

                GetProperty("SqlDisplay").SetIsBrowsable(true);
                GetProperty("ScriptDisplay").SetIsBrowsable(true);
                GetProperty("FilterChars").SetIsBrowsable(true);
                GetProperty("Message").SetIsBrowsable(true);

                GetProperty("Values").SetIsBrowsable(IsEditable);
                GetProperty("NumberOfValues").SetIsBrowsable(true);

                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);
                GetProperty("HelperRefreshEnum").SetIsBrowsable(true);

                //Read only
                GetProperty("SqlDisplay").SetIsReadOnly(!IsDynamic);
                GetProperty("IsDbRefresh").SetIsReadOnly(!IsDynamic);
                GetProperty("ValuesPerConnection").SetIsReadOnly(IsDbRefresh);
                GetProperty("NumberOfValues").SetIsReadOnly(true);

                GetProperty("FilterChars").SetIsReadOnly(!HasDynamicDisplay);
                GetProperty("Message").SetIsReadOnly(!HasDynamicDisplay || (FilterChars == 0 && !HasDependencies));

                GetProperty("Information").SetIsReadOnly(true);
                GetProperty("Error").SetIsReadOnly(true);
                GetProperty("HelperRefreshEnum").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

#endif
        /// <summary>
        /// Create a basic enumerated list
        /// </summary>
        /// <returns></returns>
        public static MetaEnum Create()
        {
            return new MetaEnum() { GUID = Guid.NewGuid().ToString() };
        }

        /// <summary>
        /// Name of the enumerated list
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [Category("Definition"), DisplayName("Name"), Description("Name of the enumerated list."), Id(1, 1)]
#endif
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private bool _isDynamic = false;
        /// <summary>
        /// If True, the list is loaded using the 'SQL Select Statement' and/or the 'Script' defined.
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("List is dynamically loaded from the 'SQL Select Statement' or from the 'Load Script'"), Description("If True, the list is loaded using the 'SQL Select Statement' and/or the 'Script' defined."), Id(2, 1)]
#endif
        public bool IsDynamic
        {
            get { return _isDynamic; }
            set
            {
                _isDynamic = value;
                UpdateEditorAttributes();
            }
        }

        private bool _isDbRefresh = false;
        /// <summary>
        /// If True, the list is loaded before a report execution. Should be set to False if the SQL or the Load Script has poor performances.
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("List is refreshed before the report execution"), Description("If True, the list is loaded before a report execution. Should be set to False if the SQL or the Load Script has poor performances."), Id(3, 1)]
#endif
        public bool IsDbRefresh
        {
            get { return _isDbRefresh; }
            set
            {
                _isDbRefresh = value;
                UpdateEditorAttributes();
            }
        }

        /// <summary>
        /// If True, the enum loads and stores the values for each connection.
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("List values depend on the connection"), Description("If True, the enum loads and stores the values for each connection."), Id(4, 1)]
#endif
        public bool ValuesPerConnection { get; set; } = false;

        /// <summary>
        /// True if the list has dynamic display
        /// </summary>
        [XmlIgnore]
        public bool HasDynamicDisplay
        {
            get { return IsDynamic && IsDbRefresh; }
        }

        /// <summary>
        /// True if the list need to request the server on popup 
        /// </summary>
        [XmlIgnore]
        public bool RequestServerOnPopup
        {
            get { return IsDynamic && IsDbRefresh && !(FilterChars == 0 && !HasDependencies); }
        }

        private string _sql;

        /// <summary>
        /// If the list is dynamic, SQL Select statement with 1, 2, 3, 4 or 5 columns used to build the list of values. The first column is used for the identifier, the second optional column is the display value shown in the table result, the third optional column is the display value shown in the restriction list, the fourth optional column defines a custom CSS Style applied to the result cell, the fifth optional column defines a custom CSS Class applied to the result cell.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("SQL Select Statement"), Description("If the list is dynamic, SQL Select statement with 1, 2, 3, 4 or 5 columns used to build the list of values. The first column is used for the identifier, the second optional column is the display value shown in the table result, the third optional column is the display value shown in the restriction list, the fourth optional column defines a custom CSS Style applied to the result cell, the fifth optional column defines a custom CSS Class applied to the result cell."), Id(5, 1)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
#endif
        public string Sql
        {
            get { return _sql; }
            set
            {
                bool isModified = (_sql != value);
                _sql = value;
                if (isModified && IsDynamic && IsDbRefresh && !string.IsNullOrEmpty(_sql) && _dctd != null) RefreshEnum();
            }
        }

        private string _script;
        /// <summary>
        /// If the list is dynamic, Razor Script executed to load or update the enumerated list values. The Script is executed after the optional SQL load when 'SQL Select Statement' is not empty.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Script"), Description("If the list is dynamic, Razor Script executed to load or update the enumerated list values. The Script is executed after the optional SQL load when 'SQL Select Statement' is not empty."), Id(6, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string Script
        {
            get { return _script; }
            set
            {
                _script = value;
                if (IsDynamic && IsDbRefresh && !string.IsNullOrEmpty(_script) && _dctd != null) RefreshEnum();
            }
        }

        /// <summary>
        /// If True, the current position of the values in the list is used to sort the column in the report result
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("Use defined position to sort in reports"), Description("If True, the current position of the values in the list is used to sort the column in the report result."), Id(7, 1)]
#endif
        public bool UsePosition { get; set; } = false;

        /// <summary>
        /// If True, the enumerated values are translated using the Repository translations
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("Translate values"), Description("If True, the enumerated values are translated using the Repository translations."), Id(8, 1)]
#endif
        public bool Translate { get; set; } = false;

        /// <summary>
        /// If the list is dynamic, refreshed before execution and the SQL for prompted restriction contains the '{EnumFilter}' keyword, the number of characters typed by the used in the filter box before the enum is built and displayed
        /// </summary>
#if WINDOWS
        [DefaultValue(0)]
        [Category("Dynamic display"), DisplayName("Filter characters to type"), Description("If the list is dynamic, refreshed before execution and the SQL for prompted restriction contains the '{EnumFilter}' keyword, the number of characters typed by the used in the filter box before the enum is built and displayed."), Id(1, 2)]
#endif
        public int FilterChars { get; set; } = 0;
        public bool ShouldSerializeFilterChars() { return FilterChars > 0; }

        /// <summary>
        /// If the list is dynamic, refreshed before execution and has filter characters or dependencies, the message displayed to the end user to trigger the list (e.g. 'Select a country first' or 'Type 5 characters').
        /// </summary>
#if WINDOWS
        [Category("Dynamic display"), DisplayName("Information message"), Description("If the list is dynamic, refreshed before execution and has filter characters or dependencies, the message displayed to the end user to trigger the list (e.g. 'Select a country first' or 'Type 5 characters')."), Id(2, 2)]
#endif
        public string Message { get; set; }

        /// <summary>
        /// Optional SQL Select Statement used to build the values displayed in a prompted restriction. The SQL is used only if the list is dynamic, refreshed before report execution.
        /// </summary>
#if WINDOWS
        [Category("Dynamic display"), DisplayName("SQL Select Statement for prompted restriction"), Description("Optional SQL Select Statement used to build the values displayed in a prompted restriction. The SQL can return 1 to 5 columns and follows the definition of 'SQL Select Statement' property. It can contain either the '{EnumFilter}' and/or '{EnumValues_<Name>}' keywords where <Name> is the name of another prompted enumerated list. The SQL is used only if the list is dynamic, refreshed before report execution."), Id(3, 2)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
#endif
        public string SqlDisplay { get; set; }

        /// <summary>
        /// Optional Script used to build the values displayed in a prompted restriction. The Script is used only if the list is dynamic, refreshed before report execution. It can contain either the '{EnumFilter}' and/or '{EnumValues_&lt;Name>}' keywords where &lt;Name> is the name of another prompted enumerated list.
        /// </summary>
#if WINDOWS
        [Category("Dynamic display"), DisplayName("Script for prompted restriction"), Description("Optional Script used to build the values displayed in a prompted restriction. It can contain either the '{EnumFilter}' and/or '{EnumValues_<Name>}' keywords where <Name> is the name of another prompted enumerated list. The Script is used only if the list is dynamic, refreshed before report execution."), Id(4, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ScriptDisplay { get; set; }

        /// <summary>
        /// True if the list has dependencies to other list
        /// </summary>
        [XmlIgnore]
        public bool HasDependencies
        {
            get
            {
                return
                  (!string.IsNullOrEmpty(SqlDisplay) && SqlDisplay.Contains(Repository.EnumValuesKeyword)) ||
                  (!string.IsNullOrEmpty(ScriptDisplay) && ScriptDisplay.Contains(Repository.EnumValuesKeyword))
                  ;
            }
        }

        /// <summary>
        /// The list of values used for this enumerated list
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [Category("Values"), DisplayName("Values"), Description("The list of values used for this enumerated list"), Id(1, 3)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<MetaEV> Values { get; set; } = new List<MetaEV>();
        public bool ShouldSerializeValues() { return !HasDynamicDisplay && Values.Count > 0; }


        public List<MetaEV> GetValues(MetaConnection connection)
        {
            if (!ValuesPerConnection || connection == null) return Values;
            return Values.Where(i => i.ConnectionGUID == connection.GUID).ToList();
        }

        /// <summary>
        /// New enum values set by the dynamic display Script
        /// </summary>
        [XmlIgnore]
        public List<MetaEV> NewValues = new List<MetaEV>();

        /// <summary>
        /// The number of values in the collection
        /// </summary>
#if WINDOWS
        [Category("Values"), DisplayName("Number of Values"), Description("The number of values in the collection"), Id(2, 3)]
#endif
        public int NumberOfValues
        {
            get { return Values.Count; }
        }

        /// <summary>
        /// True if the list is editable
        /// </summary>
        [XmlIgnore]
        public bool IsEditable = true;

        protected MetaSource _source;
        /// <summary>
        /// Current MetaSource
        /// </summary>
        [XmlIgnore, Browsable(false)]
        public MetaSource Source
        {
            get { return _source; }
            set
            {
                _source = value;
            }
        }

        /// <summary>
        /// True if the source is a standard SQL source
        /// </summary>
        [XmlIgnore]
        public bool IsSQL
        {
            get { return !_source.IsNoSQL; }
        }


        List<MetaEV> getValues(DbConnection connection, string sql)
        {
            var result = new List<MetaEV>();

            if (!string.IsNullOrEmpty(sql))
            {
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
            }
            return result;
        }

        /// <summary>
        /// Returns the list of the enum values to display after applying filter and depencies
        /// </summary>
        public List<MetaEV> GetSubSetValues(string filter, Dictionary<MetaEnum, List<string>> dependencies)
        {
            var result = new List<MetaEV>();
            if (!string.IsNullOrEmpty(SqlDisplay))
            {
                DbConnection connection = _source.GetOpenConnection();
                try
                {
                    var finalSQL = RazorHelper.CompileExecute(SqlDisplay, this);
                    if (HasDynamicDisplay) finalSQL = finalSQL.Replace(Repository.EnumFilterKeyword + "}", filter);
                    if (HasDynamicDisplay && dependencies != null)
                    {
                        foreach (var d in dependencies.Keys)
                        {
                            //First of list for SQL dependencies
                            finalSQL = finalSQL.Replace(Repository.EnumValuesKeyword + d.Name + "}", dependencies[d][0]);
                        }
                    }
                    finalSQL = Helper.ClearAllSQLKeywords(finalSQL);
                    result = getValues(connection, finalSQL);
                }
                finally
                {
                    connection.Close();
                }
            }

            if (!string.IsNullOrEmpty(ScriptDisplay))
            {
                var finalScript = ScriptDisplay;
                if (HasDynamicDisplay) finalScript = finalScript.Replace(Repository.EnumFilterKeyword + "}", Helper.QuoteDouble(filter));
                if (HasDynamicDisplay && dependencies != null)
                {
                    foreach (var d in dependencies.Keys)
                    {
                        //Second of list for Script dependencies
                        finalScript = finalScript.Replace(Repository.EnumValuesKeyword + d.Name + "}", dependencies[d][1]);
                    }
                }
                finalScript = Helper.ClearAllLINQKeywords(finalScript);
                try
                {
                    RazorHelper.CompileExecute(finalScript, this);
                }
                catch (Exception ex)
                {
                    Helper.WriteLogException("GetSubSetValues", ex);
                    throw;
                }
                result = NewValues.ToList();
            }

            return result;
        }

        /// <summary>
        /// Refresh the values of the enumerated list
        /// </summary>
        /// <param name="checkOnly"></param>
        public void RefreshEnum(bool checkOnly = false)
        {
            if (_source == null || !IsDynamic || (string.IsNullOrEmpty(Sql) && string.IsNullOrEmpty(Script))) return;

            DbConnection connection = null;
            var initialValues = Values.ToList();
            try
            {
                Error = "";
                Information = "";

                if (!string.IsNullOrEmpty(Sql))
                {
                    if (ValuesPerConnection)
                    {
                        Values.Clear();
                        foreach (var metaConnection in _source.Connections)
                        {
                            connection = metaConnection.GetOpenConnection();
                            try
                            {
                                var vals = getValues(connection, RazorHelper.CompileExecute(Sql, this));
                                foreach (var ev in vals) ev.ConnectionGUID = metaConnection.GUID;
                                Values.AddRange(vals);
                            }
                            finally
                            {
                                connection.Close();
                            }
                        }
                    }
                    else
                    {
                        connection = _source.GetOpenConnection();
                        try
                        {
                            Values = getValues(connection, RazorHelper.CompileExecute(Sql, this));
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(Script))
                {
                    RazorHelper.CompileExecute(Script, this);
                }
                if (Values.Count == 0)
                {
                    //force at least a value
                    Values.Add(new MetaEV() { Id = "", Val = "" });
                }


                Information = string.Format("List refreshed with {0} value(s).", Values.Count);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Information = "Error got when refreshing the values.";
            }
            finally
            {
                if (connection != null && connection.State == ConnectionState.Open) connection.Close();
                if (checkOnly) Values = initialValues;
            }
            Information = Helper.FormatMessage(Information);
            UpdateEditorAttributes();
        }

        /// <summary>
        /// Return the display value from the identifer
        /// </summary>
        public string GetDisplayValue(string id, MetaConnection connection, bool forRestriction = false)
        {
            var values = ValuesPerConnection ? GetValues(connection) : Values;
            MetaEV value = values.FirstOrDefault(i => i.Id == id);
            return value == null ? id : (forRestriction ? value.DisplayRestriction : value.DisplayValue);
        }

        #region Helpers
        /// <summary>
        /// Editor Helper: Refresh values of this list using the SQL Statement
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Refresh values"), Description("Refresh values of this list using the SQL Statement."), Id(1, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
#endif
        public string HelperRefreshEnum
        {
            get { return "<Click to refresh enum values>"; }
        }

        /// <summary>
        /// Last information message when the enum list has been refreshed
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Information"), Description("Last information message when the enum list has been refreshed."), Id(2, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string Information { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(3, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string Error { get; set; }


        #endregion

        /// <summary>
        /// Object that can be used at run-time for any purpose
        /// </summary>
        [XmlIgnore]
        public object Tag;

        /// <summary>
        /// Object that can be used at run-time for any purpose
        /// </summary>
        [XmlIgnore]
        public object Tag2;


    }
}
