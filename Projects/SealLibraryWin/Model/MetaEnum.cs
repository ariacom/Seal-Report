//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
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
using System.ComponentModel.Design;
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
                GetProperty("Description").SetIsBrowsable(true);
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
                GetProperty("LoadOnDemand").SetIsBrowsable(true);
                GetProperty("SqlSelected").SetIsBrowsable(true);

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
                GetProperty("LoadOnDemand").SetIsReadOnly(!HasDynamicDisplay);
                GetProperty("SqlSelected").SetIsReadOnly(!LoadOnDemand);

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

        /// <summary>
        /// Business description of the enumerated list. Used by the AI agent and shown as help in the Report Designer.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Description"), Description("Business description of the enumerated list. Used by the AI agent and shown as help in the Report Designer."), Id(2, 1)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
#endif
        public string Description { get; set; }
        /// <summary>
        /// Serialize Description only if not empty
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeDescription() { return !string.IsNullOrEmpty(Description); }

        private bool _isDynamic = false;
        /// <summary>
        /// If True, the list is loaded using the 'SQL Select Statement' and/or the 'Script' defined.
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("List is dynamically loaded from the 'SQL Select Statement' or from the 'Load Script'"), Description("If True, the list is loaded using the 'SQL Select Statement' and/or the 'Script' defined."), Id(3, 1)]
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
        /// <summary>
        /// Serialize IsDynamic only if not the default value
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeIsDynamic() { return IsDynamic; }

        private bool _isDbRefresh = false;
        /// <summary>
        /// If True, the list is loaded before a report execution. Should be set to False if the SQL or the Load Script has poor performances.
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("List is refreshed before the report execution"), Description("If True, the list is loaded before a report execution. Should be set to False if the SQL or the Load Script has poor performances."), Id(4, 1)]
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
        /// Serialize IsDbRefresh only if not the default value
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeIsDbRefresh() { return IsDbRefresh; }

        /// <summary>
        /// If True, the enum loads and stores the values for each connection.
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("List values depend on the connection"), Description("If True, the enum loads and stores the values for each connection."), Id(5, 1)]
#endif
        public bool ValuesPerConnection { get; set; } = false;
        /// <summary>
        /// Serialize ValuesPerConnection only if not the default value
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeValuesPerConnection() { return ValuesPerConnection; }

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
        [Category("Definition"), DisplayName("SQL Select Statement"), Description("If the list is dynamic, SQL Select statement with 1, 2, 3, 4 or 5 columns used to build the list of values. The first column is used for the identifier, the second optional column is the display value shown in the table result, the third optional column is the display value shown in the restriction list, the fourth optional column defines a custom CSS Style applied to the result cell, the fifth optional column defines a custom CSS Class applied to the result cell."), Id(6, 1)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
#endif
        [SealCode(ReportCodeLevel.Sql)]
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
        [Category("Definition"), DisplayName("Script"), Description("If the list is dynamic, Razor Script executed to load or update the enumerated list values. The Script is executed after the optional SQL load when 'SQL Select Statement' is not empty."), Id(7, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        [SealCode(ReportCodeLevel.Scripts)]
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
        [Category("Definition"), DisplayName("Use defined position to sort in reports"), Description("If True, the current position of the values in the list is used to sort the column in the report result."), Id(8, 1)]
#endif
        public bool UsePosition { get; set; } = false;
        /// <summary>
        /// Serialize UsePosition only if not the default value
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeUsePosition() { return UsePosition; }

        /// <summary>
        /// If True, the enumerated values are translated using the Repository translations
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("Translate values"), Description("If True, the enumerated values are translated using the Repository translations."), Id(9, 1)]
#endif
        public bool Translate { get; set; } = false;
        /// <summary>
        /// Serialize Translate only if not the default value
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeTranslate() { return Translate; }

        /// <summary>
        /// If the list is dynamic, refreshed before execution and the SQL for prompted restriction contains the '{EnumFilter}' keyword, the number of characters typed by the used in the filter box before the enum is built and displayed
        /// </summary>
#if WINDOWS
        [DefaultValue(0)]
        [Category("Dynamic display"), DisplayName("Filter characters to type"), Description("If the list is dynamic, refreshed before execution and the SQL for prompted restriction contains the '{EnumFilter}' keyword, the number of characters typed by the used in the filter box before the enum is built and displayed."), Id(1, 2)]
#endif
        public int FilterChars { get; set; } = 0;
        /// <summary>
        /// Serialize FilterChars only if positive
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
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
        [Category("Dynamic display"), DisplayName("SQL Select Statement for prompted restriction"), Description("Optional SQL Select Statement used to build the values displayed in a prompted restriction. The SQL can return 1 to 5 columns and follows the definition of 'SQL Select Statement' property. It can contain either the '{EnumFilter}' and/or '{EnumValues_<Name>}' keywords where <Name> is the name of another prompted enumerated list. '{EnumValues_<Name>}' is replaced by the values checked in the other list only, the operator chosen for that restriction is not taken into account (e.g. use it with 'IN (...)'). The SQL is used only if the list is dynamic, refreshed before report execution."), Id(3, 2)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
#endif
        [SealCode(ReportCodeLevel.Sql)]
        public string SqlDisplay { get; set; }

        /// <summary>
        /// Optional Script used to build the values displayed in a prompted restriction. The Script is used only if the list is dynamic, refreshed before report execution. It can contain either the '{EnumFilter}' and/or '{EnumValues_&lt;Name>}' keywords where &lt;Name> is the name of another prompted enumerated list.
        /// </summary>
#if WINDOWS
        [Category("Dynamic display"), DisplayName("Script for prompted restriction"), Description("Optional Script used to build the values displayed in a prompted restriction. It can contain either the '{EnumFilter}' and/or '{EnumValues_<Name>}' keywords where <Name> is the name of another prompted enumerated list. '{EnumValues_<Name>}' is replaced by the values checked in the other list only, the operator chosen for that restriction is not taken into account. The Script is used only if the list is dynamic, refreshed before report execution."), Id(4, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        [SealCode(ReportCodeLevel.Scripts)]
        public string ScriptDisplay { get; set; }

        private bool _loadOnDemand = false;
        /// <summary>
        /// If True, the values are never loaded as a whole: a prompted restriction only gets the values returned by the 'SQL Select Statement for prompted restriction' (or its Script) for the filter typed by the user, and the selected values are loaded with the 'SQL Select Statement for selected values'. Used for large lists (e.g. thousands of customers or companies). Requires a list dynamic, refreshed before the execution, with filter characters to type and a '{EnumFilter}' keyword. The list should be bound to restrictions only (property 'Enumerated List' of the restriction): a column displayed with this list shows the identifiers.
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Dynamic display"), DisplayName("Values loaded on demand"), Description("If True, the values are never loaded as a whole: a prompted restriction only gets the values returned by the 'SQL Select Statement for prompted restriction' (or its Script) for the filter typed by the user, and the selected values are loaded with the 'SQL Select Statement for selected values'. Used for large lists (e.g. thousands of customers or companies). Requires a list dynamic, refreshed before the execution, with filter characters to type and a '{EnumFilter}' keyword. The list should be bound to restrictions only (property 'Enumerated List' of the restriction): a column displayed with this list shows the identifiers."), Id(5, 2)]
#endif
        public bool LoadOnDemand
        {
            get { return _loadOnDemand; }
            set
            {
                _loadOnDemand = value;
                UpdateEditorAttributes();
            }
        }
        /// <summary>
        /// Serialize LoadOnDemand only if not the default value
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeLoadOnDemand() { return LoadOnDemand; }

        /// <summary>
        /// If the values are loaded on demand, optional SQL Select Statement returning the values of the identifiers selected (e.g. by a navigation link or saved in the report). It follows the definition of 'SQL Select Statement' and contains the '{EnumSelected}' keyword, replaced by the list of the identifiers quoted (e.g. WHERE id IN ({EnumSelected})). If empty, each selected identifier is searched with the 'SQL Select Statement for prompted restriction' used as filter.
        /// </summary>
#if WINDOWS
        [Category("Dynamic display"), DisplayName("SQL Select Statement for selected values"), Description("If the values are loaded on demand, optional SQL Select Statement returning the values of the identifiers selected (e.g. by a navigation link or saved in the report). It follows the definition of 'SQL Select Statement' and contains the '{EnumSelected}' keyword, replaced by the list of the identifiers quoted (e.g. WHERE id IN ({EnumSelected})). If empty, each selected identifier is searched with the 'SQL Select Statement for prompted restriction' used as filter."), Id(6, 2)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
#endif
        [SealCode(ReportCodeLevel.Sql)]
        public string SqlSelected { get; set; }

        /// <summary>
        /// True if the values are loaded on demand: the list is never loaded as a whole
        /// </summary>
        [XmlIgnore]
        public bool IsLoadOnDemand
        {
            get { return LoadOnDemand && HasDynamicDisplay && FilterChars > 0 && (!string.IsNullOrEmpty(SqlDisplay) || !string.IsNullOrEmpty(ScriptDisplay)); }
        }

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
        public List<MetaEV> Values
        {
            get
            {
                //Deferred refresh: the list is loaded from the database the first time its values are used
                if (RefreshPending)
                {
                    lock (_refreshLock)
                    {
                        if (RefreshPending) RefreshEnum();
                    }
                }
                return _values;
            }
            set { _values = value; }
        }
        List<MetaEV> _values = new List<MetaEV>();
        readonly object _refreshLock = new object();

        /// <summary>
        /// If True, the values are refreshed from the database the first time they are used (set when a report is loaded for execution, an unused list is never loaded)
        /// </summary>
        [XmlIgnore]
        public bool RefreshPending { get; set; } = false;

        /// <summary>
        /// Serialize Values only if not empty and the list has no dynamic display
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeValues() { return !HasDynamicDisplay && Values.Count > 0; }


        /// <summary>
        /// Returns the list of values for a given connection (all values if ValuesPerConnection is false)
        /// </summary>
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

        /// <summary>
        /// Current MetaSource
        /// </summary>
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
                    //The filter is typed by the end user and inserted in a string literal (e.g. LIKE '%{EnumFilter}%'): escape it to prevent SQL injection
                    if (HasDynamicDisplay) finalSQL = finalSQL.Replace(Repository.EnumFilterKeyword + "}", Helper.EscapeSQLStringLiteral(filter, _source.Connection != null ? _source.Connection.DatabaseType : DatabaseType.Standard));
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
                //The filter is typed by the end user and compiled in the script: a C# verbatim literal cannot be escaped with a backslash (unlike "..." built by QuoteDouble)
                if (HasDynamicDisplay) finalScript = finalScript.Replace(Repository.EnumFilterKeyword + "}", Helper.ToCSharpVerbatimLiteral(filter));
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
            RefreshPending = false;
            if (_source == null || !IsDynamic) return;

            if (IsLoadOnDemand)
            {
                //Never loaded as a whole: values are added by the prompted restriction (filter typed or selected values)
                if (!checkOnly) _values = new List<MetaEV>();
                Error = "";
                Information = Helper.FormatMessage("The values are loaded on demand: the list is built from the filter typed by the user and the values selected.");
                UpdateEditorAttributes();
                return;
            }

            if (string.IsNullOrEmpty(Sql) && string.IsNullOrEmpty(Script)) return;

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

            //RefreshEnum() rebuilds Values with brand new MetaEV objects whose HtmlId is null. The html ids are
            //normally assigned by ReportRestriction.SetEnumHtmlIds() during Report.InitForExecution(), but a report
            //task can refresh an enum AFTER that, leaving the values without ids: every rendered prompt option then
            //gets the same (empty) id and selecting another value silently falls back to the first/previous one.
            //Re-stamp the positional ids here so a refreshed enum stays renderable. Mirror SetEnumHtmlIds: positional
            //index, per connection when ValuesPerConnection (each restriction view only shows its connection subset).
            SetValuesHtmlIds();

            Information = Helper.FormatMessage(Information);
            UpdateEditorAttributes();
        }

        /// <summary>
        /// Assign positional html ids to the enum values (per connection when ValuesPerConnection), so the values
        /// stay renderable after a refresh that replaced the MetaEV objects. Consistent with ReportRestriction.SetEnumHtmlIds().
        /// </summary>
        private void SetValuesHtmlIds()
        {
            if (ValuesPerConnection)
            {
                foreach (var grp in Values.GroupBy(v => v.ConnectionGUID))
                {
                    int i = 0;
                    foreach (var ev in grp) ev.HtmlId = (i++).ToString();
                }
            }
            else
            {
                int i = 0;
                foreach (var ev in Values) ev.HtmlId = (i++).ToString();
            }
        }

        /// <summary>
        /// Return the display value from the identifier
        /// </summary>
        public string GetDisplayValue(string id, MetaConnection connection, bool forRestriction = false)
        {
            var values = ValuesPerConnection ? GetValues(connection) : Values;
            MetaEV value = values.FirstOrDefault(i => i.Id == id);
            return value == null ? id : (forRestriction ? value.DisplayRestriction : value.DisplayValue);
        }

        /// <summary>
        /// List loaded on demand: add the values not yet in the list. They are appended so the positional html ids already given stay valid.
        /// </summary>
        public void AddValues(IEnumerable<MetaEV> values)
        {
            lock (_refreshLock)
            {
                //Process a pending refresh first: it would reset the list after the values are added
                var current = Values;
                foreach (var value in values)
                {
                    if (!current.Exists(i => i.Id == value.Id)) current.Add(value);
                }
                SetValuesHtmlIds();
            }
        }

        /// <summary>
        /// List loaded on demand: load the values of the identifiers selected that are not yet in the list (e.g. a value set by a navigation link or saved in the report)
        /// </summary>
        public void EnsureValues(IEnumerable<string> ids)
        {
            if (!IsLoadOnDemand || ids == null) return;
            var missing = ids.Where(i => !string.IsNullOrEmpty(i) && !Values.Exists(j => j.Id == i)).Distinct().ToList();
            if (missing.Count == 0) return;

            try
            {
                var found = new List<MetaEV>();
                if (!string.IsNullOrEmpty(SqlSelected))
                {
                    var databaseType = _source.Connection != null ? _source.Connection.DatabaseType : DatabaseType.Standard;
                    var idList = string.Join(",", missing.Select(i => "'" + Helper.EscapeSQLStringLiteral(i, databaseType) + "'"));
                    var finalSQL = RazorHelper.CompileExecute(SqlSelected, this).Replace(Repository.EnumSelectedKeyword + "}", idList);
                    finalSQL = Helper.ClearAllSQLKeywords(finalSQL);
                    DbConnection connection = _source.GetOpenConnection();
                    try
                    {
                        found = getValues(connection, finalSQL);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
                else
                {
                    //No dedicated SQL: each identifier is used as filter of the prompted restriction, the exact identifier is kept
                    foreach (var id in missing)
                    {
                        found.AddRange(GetSubSetValues(id, null).Where(i => i.Id == id));
                    }
                }
                AddValues(found.Where(i => missing.Contains(i.Id)));
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Helper.WriteLogException("EnsureValues", ex);
            }
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
