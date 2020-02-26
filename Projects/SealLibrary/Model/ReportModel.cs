//
// Copyright 2015 (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml.Serialization;
using Seal.Helpers;
using System.ComponentModel;
using System.Data.OleDb;
using System.Drawing.Design;
using DynamicTypeDescriptor;
using Seal.Forms;
using System.Threading;
using RazorEngine.Templating;
using System.Diagnostics;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.IO;

namespace Seal.Model
{
    /// <summary>
    /// A ReportModel defines how to generate the Result Set (Data Table) and Series from the database.
    /// </summary>
    public class ReportModel : ReportComponent
    {
        const string DefaultClause = "<Default Clause>";

        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);

                //Then enable
                GetProperty("SourceGUID").SetIsBrowsable(true);
                GetProperty("ConnectionGUID").SetIsBrowsable(true);

                GetProperty("CommonRestrictions").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("PreLoadScript").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("LoadScript").SetIsBrowsable(true);
                GetProperty("FinalScript").SetIsBrowsable(true);
                if (Source.IsNoSQL)
                {
                    GetProperty("LoadScript").SetDisplayName("Load Script");
                    GetProperty("LoadScript").SetDescription("The Razor Script used to load the data in the table. If empty, the load script defined in the master table is used.");
                }
                else
                {
                    GetProperty("LoadScript").SetDisplayName("Post Load Script");
                    GetProperty("LoadScript").SetDescription("Optional Razor Script to modify the result table of the model just after the database load.");
                }
                GetProperty("ShowFirstLine").SetIsBrowsable(true);


                GetProperty("SqlSelect").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("SqlFrom").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("SqlGroupBy").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("SqlOrderBy").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("SqlCTE").SetIsBrowsable(!Source.IsNoSQL);

                GetProperty("PreSQL").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("PostSQL").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("IgnorePrePostError").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("BuildTimeout").SetIsBrowsable(!IsSQLModel && !Source.IsNoSQL);

                GetProperty("Alias").SetIsBrowsable(IsSQLModel);
                GetProperty("KeepColNames").SetIsBrowsable(IsSQLModel);
                GetProperty("UseRawSQL").SetIsBrowsable(IsSQLModel);

                GetProperty("JoinsToSelect").SetIsBrowsable(!IsSQLModel && !Source.IsNoSQL);

                GetProperty("HelperViewJoins").SetIsBrowsable(!IsSQLModel && !Source.IsNoSQL);
                GetProperty("HelperViewJoins").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        /// <summary>
        /// Creates a default report model
        /// </summary>
        /// <returns></returns>
        public static ReportModel Create()
        {
            return new ReportModel() { GUID = Guid.NewGuid().ToString() };
        }

        /// <summary>
        /// The source used to build the model
        /// </summary>
        [DefaultValue(null)]
        [Category("Model Definition"), DisplayName("Source"), Description("The source used to build the model."), Id(1, 1)]
        [TypeConverter(typeof(MetaSourceConverter))]
        public string SourceGUID { get; set; }

        protected string _connectionGUID = ReportSource.DefaultReportConnectionGUID;
        /// <summary>
        /// The connection used to build the model
        /// </summary>
        [DefaultValue(ReportSource.DefaultReportConnectionGUID)]
        [DisplayName("Connection"), Description("The connection used to build the model."), Category("Model Definition"), Id(2, 1)]
        [TypeConverter(typeof(SourceConnectionConverter))]
        public string ConnectionGUID
        {
            get
            {
                if (_connectionGUID != ReportSource.DefaultReportConnectionGUID && _connectionGUID != ReportSource.DefaultRepositoryConnectionGUID)
                {
                    if (Source != null && !Source.Connections.Exists(i => i.GUID == _connectionGUID) && !Source.TempConnections.Exists(i => i.GUID == _connectionGUID)) _connectionGUID = ReportSource.DefaultReportConnectionGUID;
                }
                return _connectionGUID;
            }
            set { _connectionGUID = value; }
        }

        /// <summary>
        /// The current MetaConnection of the model
        /// </summary>
        [XmlIgnore]
        public MetaConnection Connection
        {
            get
            {
                MetaConnection result = Source.Connection;
                if (_connectionGUID == ReportSource.DefaultReportConnectionGUID) result = Source.Connection;
                else if (_connectionGUID == ReportSource.DefaultRepositoryConnectionGUID) result = Source.RepositoryConnection;
                else result = Source.Connections.FirstOrDefault(i => i.GUID == _connectionGUID);
                if (result == null && Source.Connections.Count > 0)
                {
                    result = Source.Connections[0];
                }
                return result;
            }
        }

        /// <summary>
        /// List of common restrictions and values involved in the model. Common Restrictions or Values are defined in the SQL (Pre, Post, Table SQL, Where Clause, etc.) with the '{CommonRestriction_' or '{CommonValue_' keywords (e.g. {CommonRestriction_Amount} to create a common restriction named 'Amount')
        /// </summary>
        [Category("Model Definition"), DisplayName("Common Restrictions and Values"), Description("List of common restrictions and values involved in the model. Common Restrictions or Values are defined in the SQL (Pre, Post, Table SQL, Where Clause, etc.) with the '{CommonRestriction_' or '{CommonValue_' keywords (e.g. {CommonRestriction_Amount} to create a common restriction named 'Amount')"), Id(3, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<ReportRestriction> CommonRestrictions { get; set; } = new List<ReportRestriction>();
        public bool ShouldSerializeCommonRestrictions() { return CommonRestrictions.Count > 0; }

        /// <summary>
        /// Optional Razor Script to modify the result table of the model just before the database load
        /// </summary>
        [Category("Model Definition"), DisplayName("Pre Load Script"), Description("Optional Razor Script to modify the result table of the model just before the database load."), Id(4, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
        public string PreLoadScript { get; set; }

        /// <summary>
        /// The Razor Script used to load the data in the table. If empty, the load script defined in the master table is used.
        /// </summary>
        [Category("Model Definition"), DisplayName("Load Script"), Description("The Razor Script used to load the data in the table. If empty, the load script defined in the master table is used."), Id(5, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
        public string LoadScript { get; set; }

        /// <summary>
        /// Optional Razor Script to modify the model after its generation
        /// </summary>
        [Category("Model Definition"), DisplayName("Final Script"), Description("Optional Razor Script to modify the model after its generation."), Id(6, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
        public string FinalScript { get; set; }

        /// <summary>
        /// If true and the table has column values, the first line used for titles is generated in the table header
        /// </summary>
        [Category("Model Definition"), DisplayName("Show First Header Line"), Description("If true and the table has column values, the first line used for titles is generated in the table header."), Id(7, 1)]
        [DefaultValue(true)]
        public bool ShowFirstLine { get; set; } = true;

        /// <summary>
        /// Alias name used for the table defining the select
        /// </summary>
        [Category("SQL Model Options"), DisplayName("Alias name"), Description("Alias name used for the table defining the select."), Id(1, 2)]
        public string Alias
        {
            get { return Table != null ? Table.Alias : Name; }
            set { if (Table != null) Table.Alias = value; }
        }
        public bool ShouldSerializeAlias() { return IsSQLModel; }

        /// <summary>
        /// If true, the column names of the source a kept when building the metadata columns
        /// </summary>
        [Category("SQL Model Options"), DisplayName("Keep column names"), Description("If true, the column names of the source a kept when building the metadata columns."), Id(2, 2)]
        [DefaultValue(false)]
        public bool KeepColNames
        {
            get { return Table != null ? Table.KeepColumnNames : false; }
            set { if (Table != null) Table.KeepColumnNames = value; }
        }
        public bool ShouldSerializeKeepColNames() { return IsSQLModel; }

        /// <summary>
        /// If true, the raw source SQL is used to generate the result table instead of using a 'select * from (Source SQL) a' statement. In this case, aggregations, restrictions and custom SQL are not applied
        /// </summary>
        [Category("SQL Model Options"), DisplayName("Use raw SQL"), Description("If true, the raw source SQL is used to generate the result table instead of using a 'select * from (Source SQL) a' statement. In this case, aggregations, restrictions and custom SQL are not applied."), Id(3, 2)]
        [DefaultValue(false)]
        public bool UseRawSQL { get; set; } = false;
        public bool ShouldSerializeUseRawSQL() { return IsSQLModel; }

        string _sqlSelect;
        /// <summary>
        /// If not empty, overwrite the SELECT clause in the generated SQL statement (e.g 'SELECT TOP 10', 'SELECT')
        /// </summary>
        [Category("SQL"), DisplayName("Select Clause"), Description("If not empty, overwrite the SELECT clause in the generated SQL statement (e.g 'SELECT TOP 10', 'SELECT')."), Id(3, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
        public string SqlSelect
        {
            get { return (_sqlSelect == DefaultClause) ? "" : _sqlSelect; }
            set { _sqlSelect = value; }
        }

        string _sqlFrom;
        /// <summary>
        /// If not empty, overwrite the FROM clause in the generated SQL statement
        /// </summary>
        [Category("SQL"), DisplayName("From Clause"), Description("If not empty, overwrite the FROM clause in the generated SQL statement."), Id(4, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
        public string SqlFrom
        {
            get { return (_sqlFrom == DefaultClause) ? "" : _sqlFrom; }
            set { _sqlFrom = value; }
        }

        string _sqlGroupBy;
        /// <summary>
        /// If not empty, overwrite the GROUP BY clause in the generated SQL statement
        /// </summary>
        [Category("SQL"), DisplayName("Group By Clause"), Description("If not empty, overwrite the GROUP BY clause in the generated SQL statement."), Id(5, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
        public string SqlGroupBy
        {
            get { return (_sqlGroupBy == DefaultClause) ? "" : _sqlGroupBy; }
            set { _sqlGroupBy = value; }
        }

        string _sqlOrderBy;
        /// <summary>
        /// If not empty, overwrite the ORDER BY clause in the generated SQL statement
        /// </summary>
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [Category("SQL"), DisplayName("Order By Clause"), Description("If not empty, overwrite the ORDER BY clause in the generated SQL statement."), Id(6, 3)]
        [DefaultValue("")]
        public string SqlOrderBy
        {
            get { return (_sqlOrderBy == DefaultClause) ? "" : _sqlOrderBy; }
            set { _sqlOrderBy = value; }
        }

        string _sqlCTE;
        /// <summary>
        /// If not empty, overwrite the CTE (Common Table Expressions) clause in the generated SQL statement
        /// </summary>
        [Category("SQL"), DisplayName("Common Table Expressions Clause"), Description("If not empty, overwrite the CTE (Common Table Expressions) clause in the generated SQL statement."), Id(7, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
        public string SqlCTE
        {
            get { return (_sqlSelect == DefaultClause) ? "" : _sqlCTE; }
            set { _sqlCTE = value; }
        }

        /// <summary>
        /// SQL Statement executed before the main query. The statement may contain Razor script if it starts with '@'.
        /// </summary>
        [Category("SQL"), DisplayName("Pre SQL Statement"), Description("SQL Statement executed before the main query. The statement may contain Razor script if it starts with '@'."), Id(8, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
        public string PreSQL { get; set; }

        /// <summary>
        /// SQL Statement executed after the main query. The statement may contain Razor script if it starts with '@'.
        /// </summary>
        [Category("SQL"), DisplayName("Post SQL Statement"), Description("SQL Statement executed after the main query. The statement may contain Razor script if it starts with '@'."), Id(9, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
        public string PostSQL { get; set; }

        /// <summary>
        /// If true, errors occuring during the Pre or Post SQL statements are ignored and the execution continues
        /// </summary>
        [Category("SQL"), DisplayName("Ignore Pre and Post SQL Errors"), Description("If true, errors occuring during the Pre or Post SQL statements are ignored and the execution continues."), Id(10, 3)]
        [DefaultValue(false)]
        public bool IgnorePrePostError { get; set; } = false;

        /// <summary>
        /// Timeout in milliseconds to set the maximum duration used to build the SQL (may be used if many joins are defined)
        /// </summary>
        [DefaultValue(2000)]
        [Category("SQL"), DisplayName("Build Timeout (ms)"), Description("Timeout in milliseconds to set the maximum duration used to build the SQL (may be used if many joins are defined)."), Id(11, 3)]
        public int BuildTimeout { get; set; } = 2000;

        /// <summary>
        /// List of Joins used to perform the query and joins the tables involved. By default, all Joins available in the Data Source are used.
        /// </summary>
        public List<string> JoinsToUse { get; set; } = new List<string>();
        public bool ShouldSerializeJoinsToUse() { return JoinsToUse.Count > 0; }

        /// <summary>
        /// Helper to select Join Preferences
        /// </summary>
        [Category("Join Preferences"), DisplayName("Joins to use"), Description("If specified, Joins used to perform the query and joins the tables involved. By default, all Joins available in the Data Source are used."), Id(2, 4)]
        [Editor(typeof(JoinsValuesEditor), typeof(UITypeEditor))]
        [XmlIgnore]
        public string JoinsToSelect
        {
            get { return "<Click to select joins>"; }
            set { } //keep set for modification handler
        }


        /// <summary>
        /// Helper to view joins evaluated for the model
        /// </summary>
        [Category("Join Preferences"), DisplayName("View joins evaluated"), Description("List all joins evaluated for the model. This may be used to understand if a join definition is missing in the source."), Id(4,4)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperViewJoins
        {
            get { return "<Click to view the joins evaluated for the model>"; }
        }

        /// <summary>
        /// Current report source
        /// </summary>
        [XmlIgnore]
        public ReportSource Source
        {
            get
            {
                ReportSource result = _report.Sources.FirstOrDefault(i => i.GUID == SourceGUID);
                if (result == null)
                {
                    if (_report.Sources.Count == 0) throw new Exception("This report has no source defined");
                    result = _report.Sources[0];
                    SourceGUID = result.GUID;
                }
                return result;
            }
        }

        /// <summary>
        /// True if the model has series defined
        /// </summary>
        [XmlIgnore]
        public bool HasSerie
        {
            get
            {
                return Elements.Exists(i => i.SerieDefinition == SerieDefinition.Axis && (i.PivotPosition == PivotPosition.Column || i.PivotPosition == PivotPosition.Row)) && Elements.Exists(i => i.IsSerie && i.PivotPosition == PivotPosition.Data);
            }
        }

        /// <summary>
        /// True if the model has subtotals
        /// </summary>
        [XmlIgnore]
        public bool HasSubTotals
        {
            get
            {
                return Elements.Exists(i => i.PivotPosition == PivotPosition.Row && i.ShowSubTotals) && Elements.Exists(i => i.PivotPosition == PivotPosition.Data);
            }
        }

        /// <summary>
        /// True if the model has a primary axis for a serie
        /// </summary>
        [XmlIgnore]
        public bool HasPrimaryYAxis
        {
            get
            {
                return Elements.Exists(i => i.YAxisType == AxisType.Primary && i.PivotPosition == PivotPosition.Data && i.IsSerie);
            }
        }

        /// <summary>
        /// True if the model has a secondary axis for a serie
        /// </summary>
        [XmlIgnore]
        public bool HasSecondaryYAxis
        {
            get
            {
                return Elements.Exists(i => i.YAxisType == AxisType.Secondary && i.PivotPosition == PivotPosition.Data && i.IsSerie);
            }
        }

        /// <summary>
        /// True if the model has a NVD3 serie
        /// </summary>
        [XmlIgnore]
        public bool HasNVD3Serie
        {
            get
            {
                return Elements.Exists(i => i.SerieDefinition == SerieDefinition.Axis && (i.PivotPosition == PivotPosition.Column || i.PivotPosition == PivotPosition.Row)) && Elements.Exists(i => i.Nvd3Serie != NVD3SerieDefinition.None && i.PivotPosition == PivotPosition.Data);
            }
        }

        /// <summary>
        /// True if the model has a Chart JS serie
        /// </summary>
        [XmlIgnore]
        public bool HasChartJSSerie
        {
            get
            {
                return Elements.Exists(i => i.SerieDefinition == SerieDefinition.Axis && (i.PivotPosition == PivotPosition.Column || i.PivotPosition == PivotPosition.Row)) && Elements.Exists(i => i.ChartJSSerie != ChartJSSerieDefinition.None && i.PivotPosition == PivotPosition.Data);
            }
        }

        /// <summary>
        /// True if the model has a Plotly serie
        /// </summary>
        [XmlIgnore]
        public bool HasPlotlySerie
        {
            get
            {
                return Elements.Exists(i => i.SerieDefinition == SerieDefinition.Axis && (i.PivotPosition == PivotPosition.Column || i.PivotPosition == PivotPosition.Row)) && Elements.Exists(i => i.PlotlySerie != PlotlySerieDefinition.None && i.PivotPosition == PivotPosition.Data);
            }
        }

        /// <summary>
        /// True if the model has totals
        /// </summary>
        [XmlIgnore]
        public bool HasTotals
        {
            get
            {
                return Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ShowTotal != ShowTotal.No);
            }
        }

        /// <summary>
        /// True if the model has cell script defined for one of its element
        /// </summary>
        [XmlIgnore]
        public bool HasCellScript
        {
            get
            {
                return Elements.Exists(i => !string.IsNullOrWhiteSpace(i.CellScript));
            }
        }

        /// <summary>
        /// Elements selected for the model
        /// </summary>
        public List<ReportElement> Elements { get; set; } = new List<ReportElement>();
        public bool ShouldSerializeElements() { return Elements.Count > 0; }

        /// <summary>
        /// List of elements per position
        /// </summary>
        public IEnumerable<ReportElement> GetElements(PivotPosition position)
        {
            return Elements.Where(i => i.PivotPosition == position);
        }

        /// <summary>
        /// List of elements per type of axis
        /// </summary>
        public IEnumerable<ReportElement> GetXElements(AxisType xAxisType)
        {
            return Elements.Where(i => i.XAxisType == xAxisType && (i.PivotPosition == PivotPosition.Row || i.PivotPosition == PivotPosition.Column) && (i.SerieDefinition == SerieDefinition.Axis));
        }

        /// <summary>
        /// List of splitter elements per axis type
        /// </summary>
        public IEnumerable<ReportElement> GetSplitterElements(AxisType xAxisType)
        {
            return Elements.Where(i => ((i.XAxisType == xAxisType && i.SerieDefinition == SerieDefinition.Splitter) || i.SerieDefinition == SerieDefinition.SplitterBoth) && (i.PivotPosition == PivotPosition.Row || i.PivotPosition == PivotPosition.Column));
        }

        /// <summary>
        /// Force sort orders on Page elements
        /// </summary>
        public void CheckSortOrders()
        {
            for (int i = 0; i < GetElements(PivotPosition.Page).Count(); i++)
            {
                var element = GetElements(PivotPosition.Page).ElementAt(i);
                if (string.IsNullOrEmpty(element.SortOrder) || element.SortOrder == ReportElement.kNoSortKeyword) element.SortOrder = string.Format("{0} Ascendant", i + 1);
            }
        }

        //Restrictions
        private string _restriction;
        /// <summary>
        /// The restriction text of the model
        /// </summary>
        public string Restriction
        {
            get { return string.IsNullOrEmpty(_restriction) ? "" : _restriction; }
            set { _restriction = value; }
        }
        public bool ShouldSerializeRestriction() { return !string.IsNullOrEmpty(_restriction); }

        /// <summary>
        /// List of restrictions of the model
        /// </summary>
        public List<ReportRestriction> Restrictions { get; set; } = new List<ReportRestriction>();
        public bool ShouldSerializeRestrictions() { return Restrictions.Count > 0; }


        //Aggregate Restrictions
        private string _aggregateRestriction;
        /// <summary>
        /// The aggregate restriction text of the model
        /// </summary>
        public string AggregateRestriction
        {
            get { return string.IsNullOrEmpty(_aggregateRestriction) ? "" : _aggregateRestriction; }
            set { _aggregateRestriction = value; }
        }
        public bool ShouldSerializeAggregateRestriction() { return!string.IsNullOrEmpty(_aggregateRestriction); }

        /// <summary>
        /// List of aggregate restrictions of the model
        /// </summary>
        public List<ReportRestriction> AggregateRestrictions { get; set; } = new List<ReportRestriction>();
        public bool ShouldSerializeAggregateRestrictions() { return AggregateRestrictions.Count > 0; }

        /// <summary>
        /// Table definition for a SQL Model
        /// </summary>
        [Browsable(false)]
        public MetaTable Table { get; set; } = null;
        public bool ShouldSerializeTable() { return IsSQLModel; }

        /// <summary>
        /// Initialize the MetaTable for a SQL Model 
        /// </summary>
        public void RefreshMetaTable(bool init)
        {
            if (Table == null)
            {
                Table = MetaTable.Create();
                Table.DynamicColumns = true;
            }

            if (string.IsNullOrEmpty(Table.Alias)) Table.Alias = "Master";

            Table.Source = Source;
            if (!string.IsNullOrEmpty(Table.Sql))
            {
                Table.Model = this;
                Table.Refresh();

                foreach (var col in Table.Columns)
                {
                    col.Category = Name;
                    col.DisplayName = (Table.KeepColumnNames ? col.Name.Trim() : Helper.DBNameToDisplayName(col.Name.Trim()));
                }
                if (init) InitReferences();
            }
        }

        /// <summary>
        /// True is it is a SQL Model (not standard based on the metadata)
        /// </summary>
        public bool IsSQLModel
        {
            get { return Table != null; }
        }


        //Execution
        private string _sql;
        /// <summary>
        /// SELECT Sql used for the model
        /// </summary>
        [XmlIgnore]
        public string Sql
        {
            get { return _sql; }
            set { _sql = value; }
        }
        public bool ShouldSerializeSql() { return IsSQLModel; }

        /// <summary>
        /// List of tables involved in the model
        /// </summary>
        [XmlIgnore]
        public List<MetaTable> FromTables { get; private set; }

        /// <summary>
        /// Display text for the restrictions of the model
        /// </summary>
        [XmlIgnore]
        public string RestrictionText;

        /// <summary>
        /// Execution date of the model
        /// </summary>
        [XmlIgnore]
        public DateTime ExecutionDate;

        /// <summary>
        /// Execution duration of the model
        /// </summary>
        [XmlIgnore]
        public int ExecutionDuration;

        /// <summary>
        /// Result DataTable got from the SQL query
        /// </summary>
        [XmlIgnore]
        public DataTable ResultTable;

        /// <summary>
        /// Progression in percentage of the model processing
        /// </summary>
        [XmlIgnore]
        public int Progression = 0;

        /// <summary>
        /// Summary Table generated after the model execution (only if Page element are defined in the model)
        /// </summary>
        [XmlIgnore]
        public ResultTable SummaryTable;

        /// <summary>
        /// List of Pages generated after the model execution (one page per Page element values)
        /// </summary>
        [XmlIgnore]
        public List<ResultPage> Pages = new List<ResultPage>();

        /// <summary>
        /// Error messages during execution
        /// </summary>
        [XmlIgnore]
        public string ExecutionError = "";

        //Execution for charts
        /// <summary>
        /// True if chart is numeric axis
        /// </summary>
        [XmlIgnore]
        public bool ExecChartIsNumericAxis;
        
        /// <summary>
        /// True if chart is date time axis
        /// </summary>
        [XmlIgnore]
        public bool ExecChartIsDateTimeAxis;

        /// <summary>
        /// True if chart primary axis is date time
        /// </summary>
        [XmlIgnore]
        public bool ExecAxisPrimaryYIsDateTime;

        /// <summary>
        /// True if chart secondary axis is date time
        /// </summary>
        [XmlIgnore]
        public bool ExecAxisSecondaryYIsDateTime;

        /// <summary>
        /// Format for D3 primary chart axis
        /// </summary>
        [XmlIgnore]
        public string ExecD3PrimaryYAxisFormat;

        /// <summary>
        /// Format for D3 secondary chart axis
        /// </summary>
        [XmlIgnore]
        public string ExecD3SecondaryYAxisFormat;

        /// <summary>
        /// Format for D3 chart X axis
        /// </summary>
        [XmlIgnore]
        public string ExecD3XAxisFormat;

        /// <summary>
        /// Format for Moment JS chart X axis
        /// </summary>
        [XmlIgnore]
        public string ExecMomentJSXAxisFormat;

        /// <summary>
        /// NVD3 chart type
        /// </summary>
        [XmlIgnore]
        public string ExecNVD3ChartType;

        /// <summary>
        /// Plotly chart type
        /// </summary>
        [XmlIgnore]
        public string ExecPlotlyChartType;

        /// <summary>
        /// Chart JS type
        /// </summary>
        [XmlIgnore]
        public string ExecChartJSType;

        /// <summary>
        /// Check NVD3 Chart and set the ExecNVD3ChartType property
        /// </summary>
        public void CheckNVD3ChartIntegrity()
        {
            if (string.IsNullOrEmpty(ExecNVD3ChartType) && HasNVD3Serie)
            {
                bool hasArea = false, hasBar = false, hasLine = false;

                //Check and choose the right chart
                if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie == NVD3SerieDefinition.ScatterChart))
                {
                    if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie != NVD3SerieDefinition.None && i.Nvd3Serie != NVD3SerieDefinition.ScatterChart)) throw new Exception("Invalid chart configuration: Cannot mix NVD3 Scatter Serie with another type.");
                    ExecNVD3ChartType = "scatterChart";
                }
                else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie == NVD3SerieDefinition.PieChart))
                {
                    if (Elements.Count(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie != NVD3SerieDefinition.None) > 1) throw new Exception("Invalid chart configuration: Only one Pie Serie can be defined.");
                    ExecNVD3ChartType = "pieChart";
                }
                else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie == NVD3SerieDefinition.MultiBarHorizontalChart))
                {
                    if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie != NVD3SerieDefinition.None && i.Nvd3Serie != NVD3SerieDefinition.MultiBarHorizontalChart)) throw new Exception("Invalid chart configuration: Cannot mix NVD3 Horizontal Bar Serie with another type.");
                    ExecNVD3ChartType = "multiBarHorizontalChart";
                }
                else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie == NVD3SerieDefinition.LineWithFocusChart))
                {
                    if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie != NVD3SerieDefinition.None && i.Nvd3Serie != NVD3SerieDefinition.LineWithFocusChart)) throw new Exception("Invalid chart configuration: Cannot mix NVD3 Line with focus Serie with another type.");
                    ExecNVD3ChartType = "lineWithFocusChart";
                }
                else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie == NVD3SerieDefinition.DiscreteBarChart))
                {
                    if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie != NVD3SerieDefinition.None && i.Nvd3Serie != NVD3SerieDefinition.DiscreteBarChart)) throw new Exception("Invalid chart configuration: Cannot mix NVD3 Discrete Bar Serie with another type.");
                    ExecNVD3ChartType = "discreteBarChart";
                }
                else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie == NVD3SerieDefinition.CumulativeLineChart))
                {
                    if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie != NVD3SerieDefinition.None && i.Nvd3Serie != NVD3SerieDefinition.CumulativeLineChart)) throw new Exception("Invalid chart configuration: Cannot mix NVD3 Cumulative Line Serie with another type.");
                    ExecNVD3ChartType = "cumulativeLineChart";
                }
                else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie == NVD3SerieDefinition.StackedAreaChart))
                {
                    hasArea = true;
                    if (!Elements.Exists(i => i.Nvd3Serie != NVD3SerieDefinition.StackedAreaChart))
                    {
                        //if primary and secondary axis are used, keep the multi chart 
                        if (!(Elements.Exists(i => i.Nvd3Serie == NVD3SerieDefinition.StackedAreaChart && i.YAxisType == AxisType.Primary) &&
                            Elements.Exists(i => i.Nvd3Serie == NVD3SerieDefinition.StackedAreaChart && i.YAxisType == AxisType.Secondary)))
                            ExecNVD3ChartType = "stackedAreaChart";
                    }
                }
                else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie == NVD3SerieDefinition.Line))
                {
                    hasLine = true;
                    if (!Elements.Exists(i => i.Nvd3Serie != NVD3SerieDefinition.Line))
                    {
                        //if primary and secondary axis are used, keep the multi chart 
                        if (!(Elements.Exists(i => i.Nvd3Serie == NVD3SerieDefinition.Line && i.YAxisType == AxisType.Primary) &&
                            Elements.Exists(i => i.Nvd3Serie == NVD3SerieDefinition.Line && i.YAxisType == AxisType.Secondary)))
                            ExecNVD3ChartType = "lineChart";
                    }
                }
                else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.Nvd3Serie == NVD3SerieDefinition.MultiBarChart))
                {
                    hasBar = true;
                    if (!Elements.Exists(i => i.Nvd3Serie != NVD3SerieDefinition.None && i.Nvd3Serie != NVD3SerieDefinition.MultiBarChart))
                    {
                        //if primary and secondary axis are used, keep the multi chart 
                        if (!(Elements.Exists(i => i.Nvd3Serie == NVD3SerieDefinition.MultiBarChart && i.YAxisType == AxisType.Primary) &&
                            Elements.Exists(i => i.Nvd3Serie == NVD3SerieDefinition.MultiBarChart && i.YAxisType == AxisType.Secondary)))
                            ExecNVD3ChartType = "multiBarChart";
                    }
                }

                //If mix of Line, Bar and Area -> we go for multiChart
                if (string.IsNullOrEmpty(ExecNVD3ChartType) && (hasArea || hasBar || hasLine)) ExecNVD3ChartType = "multiChart";
            }
        }


        /// <summary>
        /// Check Plotly Chart and set the ExecPlotlyChartType property
        /// </summary>
        public void CheckPlotlyChartIntegrity()
        {
            //Check and choose the right chart
            if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.PlotlySerie == PlotlySerieDefinition.Pie))
            {
                if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.PlotlySerie != PlotlySerieDefinition.None && i.PlotlySerie != PlotlySerieDefinition.Pie)) throw new Exception("Invalid chart configuration: Cannot mix Plotly Pie Serie with another type.");
                ExecPlotlyChartType = "pie";
            }
            else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.PlotlySerie == PlotlySerieDefinition.Scatter))
            {
                ExecPlotlyChartType = "scatter";
            }
            else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.PlotlySerie == PlotlySerieDefinition.Bar))
            {
                ExecPlotlyChartType = "bar";
            }
        }

        /// <summary>
        /// Check ChartJS and set the ExecChartJSType property
        /// </summary>
        public void CheckChartJSIntegrity()
        {
            //Check and choose the right chart
            if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ChartJSSerie == ChartJSSerieDefinition.Pie))
            {
                if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ChartJSSerie != ChartJSSerieDefinition.None && i.ChartJSSerie != ChartJSSerieDefinition.Pie)) throw new Exception("Invalid chart configuration: Cannot mix Chart JS Pie Serie with another type.");
                ExecChartJSType = "pie";
            }
            else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ChartJSSerie == ChartJSSerieDefinition.PolarArea))
            {
                if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ChartJSSerie != ChartJSSerieDefinition.None && i.ChartJSSerie != ChartJSSerieDefinition.PolarArea)) throw new Exception("Invalid chart configuration: Cannot mix Chart JS Polar Area Serie with another type.");
                ExecChartJSType = "polarArea";
            }
            else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ChartJSSerie == ChartJSSerieDefinition.Radar))
            {
                if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ChartJSSerie != ChartJSSerieDefinition.None && i.ChartJSSerie != ChartJSSerieDefinition.Radar)) throw new Exception("Invalid chart configuration: Cannot mix Chart JS Radar Serie with another type.");
                ExecChartJSType = "radar";
            }
            if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ChartJSSerie == ChartJSSerieDefinition.Scatter))
            {
                if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ChartJSSerie != ChartJSSerieDefinition.None && i.ChartJSSerie != ChartJSSerieDefinition.Scatter)) throw new Exception("Invalid chart configuration: Cannot mix Chart JS Scatter Serie with another type.");
                ExecChartJSType = "scatter";
            }
            else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ChartJSSerie == ChartJSSerieDefinition.Bar))
            {
                ExecChartJSType = "bar";
            }
            else if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ChartJSSerie == ChartJSSerieDefinition.Line))
            {
                ExecChartJSType = "line";
            }
        }

        /// <summary>
        /// List of restrictions of the model
        /// </summary>
        [XmlIgnore]
        public List<ReportRestriction> ExecutionRestrictions
        {
            get
            {
                List<ReportRestriction> result = new List<ReportRestriction>();
                foreach (ReportRestriction restriction in Restrictions.OrderBy(i => i.DisplayOrder))
                {
                    ReportRestriction newRestriction = restriction;
                    if (Report.ForOutput && Report.OutputToExecute.UseCustomRestrictions)
                    {
                        newRestriction = Report.OutputToExecute.Restrictions.FirstOrDefault(i => i.GUID == restriction.GUID);
                        if (newRestriction == null) newRestriction = restriction;
                    }
                    result.Add(newRestriction);
                }
                return result;
            }
        }

        /// <summary>
        /// List of aggregate restrictions of the model
        /// </summary>
        [XmlIgnore]
        public List<ReportRestriction> ExecutionAggregateRestrictions
        {
            get
            {
                List<ReportRestriction> result = new List<ReportRestriction>();
                foreach (ReportRestriction restriction in AggregateRestrictions)
                {
                    ReportRestriction newRestriction = restriction;
                    if (Report.ForOutput && Report.OutputToExecute.UseCustomRestrictions)
                    {
                        newRestriction = Report.OutputToExecute.Restrictions.FirstOrDefault(i => i.GUID == restriction.GUID);
                        if (newRestriction == null) newRestriction = restriction;
                    }
                    result.Add(newRestriction);
                }
                return result;
            }
        }

        /// <summary>
        /// List of Common Restrictions of the model
        /// </summary>
        [XmlIgnore]
        public List<ReportRestriction> ExecutionCommonRestrictions
        {
            get
            {
                List<ReportRestriction> result = new List<ReportRestriction>();
                foreach (ReportRestriction restriction in CommonRestrictions)
                {
                    ReportRestriction newRestriction = restriction;
                    if (Report.ForOutput && Report.OutputToExecute.UseCustomRestrictions)
                    {
                        newRestriction = Report.OutputToExecute.Restrictions.FirstOrDefault(i => i.GUID == restriction.GUID);
                        if (newRestriction == null) newRestriction = restriction;
                    }
                    result.Add(newRestriction);
                }
                return result;
            }
        }

        /// <summary>
        /// Full SQL Select generated at execution
        /// </summary>
        [XmlIgnore]
        public string execSelect = "";

        /// <summary>
        /// SELECT Clause generated at execution
        /// </summary>
        [XmlIgnore]
        public StringBuilder execSelectClause = new StringBuilder();

        /// <summary>
        /// CTE Clause generated at execution
        /// </summary>
        [XmlIgnore]
        public string execCTEClause = "";

        /// <summary>
        /// FROM Clause generated at execution
        /// </summary>
        [XmlIgnore]
        public StringBuilder execFromClause = new StringBuilder();

        /// <summary>
        /// WHERE Clause generated at execution
        /// </summary>
        [XmlIgnore]
        public StringBuilder execWhereClause = new StringBuilder();

        /// <summary>
        /// ORDER BY Clause generated at execution
        /// </summary>
        [XmlIgnore]
        public StringBuilder execOrderByClause = new StringBuilder();

        /// <summary>
        /// ORDER BY NAME Clause generated at execution (used for No SQL Source)
        /// </summary>
        [XmlIgnore]
        public StringBuilder execOrderByNameClause = new StringBuilder();

        /// <summary>
        /// GROUP BY Clause generated at execution
        /// </summary>
        [XmlIgnore]
        public StringBuilder execGroupByClause = new StringBuilder();

        /// <summary>
        /// HAVING Clause generated at execution
        /// </summary>
        [XmlIgnore]
        public StringBuilder execHavingClause = new StringBuilder();

        /// <summary>
        /// Custom Tag the can be used at execution time to store any object
        /// </summary>
        [XmlIgnore]
        public object Tag;

        /// <summary>
        /// Init all model references: Elements, Restrictions, etc.
        /// </summary>
        public void InitReferences()
        {
            if (Source.MetaData == null) return;

            foreach (var element in Elements)
            {
                element.SetSourceReference(Source);
                element.Report= Report;
                element.Model = this;
            }

            foreach (var restriction in Restrictions)
            {
                restriction.SetSourceReference(Source);
                restriction.Report = Report;
                restriction.Model = this;
            }

            foreach (var restriction in AggregateRestrictions)
            {
                restriction.SetSourceReference(Source);
                restriction.Report = Report;
                restriction.Model = this;
            }

            if (Table != null)
            {
                Table.Source = Source;
                foreach (var column in Table.Columns)
                {
                    column.Source = Source;
                    column.MetaTable = Table;
                }
            }

            //clean up lost elements...
            ClearLostElements();

            InitCommonRestrictions();
        }

        /// <summary>
        /// Parse and replace Common Restrictions in SQL by their values
        /// </summary>
        public string ParseCommonRestrictions(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return "";

            foreach (var restr in ExecutionCommonRestrictions)
            {
                sql = sql.Replace(Repository.CommonRestrictionKeyword + restr.Name + "}", restr.SQLText);
                sql = sql.Replace(Repository.CommonValueKeyword + restr.Name + "}", restr.SQLText);
            }
            return ClearCommonRestrictions(sql);
        }

        /// <summary>
        /// Clear Common Restrictions from a SQL
        /// </summary>
        public static string ClearCommonRestrictions(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return "";

            var result = Helper.ClearSQLKeywords(sql, Repository.CommonRestrictionKeyword, "1=1");
            return Helper.ClearSQLKeywords(result, Repository.CommonValueKeyword, "NULL");
        }


        /// <summary>
        /// Init all Common Restrictions of the model and build the CommonRestrictions property
        /// </summary>
        public void InitCommonRestrictions()
        {
            //Get common restrictions
            try
            {
                var sqlToParse = new StringBuilder("\r\n");
                if (!string.IsNullOrEmpty(Restriction)) sqlToParse.AppendLine(Restriction);
                if (!string.IsNullOrEmpty(AggregateRestriction)) sqlToParse.AppendLine(AggregateRestriction);
                if (!string.IsNullOrEmpty(SqlSelect)) sqlToParse.AppendLine(SqlSelect);
                if (!string.IsNullOrEmpty(SqlFrom)) sqlToParse.AppendLine(SqlFrom);
                if (!string.IsNullOrEmpty(SqlGroupBy)) sqlToParse.AppendLine(SqlGroupBy);
                if (!string.IsNullOrEmpty(SqlOrderBy)) sqlToParse.AppendLine(SqlOrderBy);
                if (!string.IsNullOrEmpty(SqlCTE)) sqlToParse.AppendLine(SqlCTE);

                if (!string.IsNullOrEmpty(Source.PreSQL)) sqlToParse.AppendLine(Source.PreSQL);
                if (!string.IsNullOrEmpty(Source.PostSQL)) sqlToParse.AppendLine(Source.PostSQL);
                if (!string.IsNullOrEmpty(PreSQL)) sqlToParse.AppendLine(PreSQL);
                if (!string.IsNullOrEmpty(PostSQL)) sqlToParse.AppendLine(PostSQL);

                //Keywords in tables
                var fromTables = new List<MetaTable>();
                foreach (ReportElement element in Elements)
                {
                    MetaTable table = element.MetaColumn.MetaTable;
                    if (table != null && !fromTables.Contains(table)) fromTables.Add(table);
                    //Add column SQL
                    sqlToParse.AppendLine(element.SQLColumn);
                }
                foreach (ReportRestriction restriction in Restrictions.Union(AggregateRestrictions))
                {
                    MetaTable table = restriction.MetaColumn.MetaTable;
                    if (table != null && !fromTables.Contains(table)) fromTables.Add(table);
                    //Add column SQL
                    sqlToParse.AppendLine(restriction.SQLColumn);
                }

                if (IsSQLModel) fromTables.Add(Table);

                foreach (var table in fromTables)
                {
                    if (!string.IsNullOrEmpty(table.Name)) sqlToParse.AppendLine(table.Name);
                    if (!string.IsNullOrEmpty(table.PreSQL)) sqlToParse.AppendLine(table.PreSQL);
                    if (!string.IsNullOrEmpty(table.PostSQL)) sqlToParse.AppendLine(table.PostSQL);
                    if (!string.IsNullOrEmpty(table.Sql)) sqlToParse.AppendLine(table.Sql);
                    if (!string.IsNullOrEmpty(table.WhereSQL)) sqlToParse.AppendLine(table.WhereSQL);
                }

                var finalSql = sqlToParse.ToString();
                var names = Helper.GetSQLKeywordNames(finalSql, Repository.CommonRestrictionKeyword);
                var valueNames = Helper.GetSQLKeywordNames(finalSql, Repository.CommonValueKeyword);
                names.AddRange(valueNames);
                foreach (var restrictionName in names)
                {
                    var commonRestriction = CommonRestrictions.FirstOrDefault(i => i.Name == restrictionName);
                    bool isCommonValue = valueNames.Contains(restrictionName);

                    if (commonRestriction == null)
                    {
                        commonRestriction = ReportRestriction.CreateReportRestriction();
                        commonRestriction.Name = restrictionName;
                        commonRestriction.DisplayName = restrictionName;
                        commonRestriction.TypeRe = ColumnType.Text;
                        CommonRestrictions.Add(commonRestriction);

                        if (isCommonValue)
                        {
                            //Common Value: Init Operator and type
                            commonRestriction.TypeRe = ColumnType.Numeric;
                            commonRestriction.Operator = Operator.ValueOnly;
                        }
                    }
                    commonRestriction.IsCommonValue = isCommonValue;
                }

                //clean restrictions not used
                CommonRestrictions.RemoveAll(i => !finalSql.Contains(Repository.CommonRestrictionKeyword + i.Name + "}") && !finalSql.Contains(Repository.CommonValueKeyword + i.Name + "}"));

                //Set references
                foreach (var restriction in CommonRestrictions)
                {
                    restriction.SetSourceReference(Source);
                    restriction.Report = Report;
                    restriction.Model = this;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Delete elements and restrictions with no source reference
        /// </summary>
        public void ClearLostElements()
        {
            if (Source.MetaData == null) return;

            int i = Elements.Count;
            while (--i >= 0)
            {
                if (Elements[i].MetaColumn == null) Elements.RemoveAt(i);
            }
            i = Restrictions.Count;
            while (--i >= 0)
            {
                if (string.IsNullOrWhiteSpace(Restriction)) Restrictions.RemoveAt(i);
                else if (Restrictions[i].MetaColumn == null)
                {
                    if (Restriction != null) Restriction = Restriction.Replace(Restrictions[i].GUID, "(Warning) Restriction lost: " + Restrictions[i].Name);
                    Restrictions.RemoveAt(i);
                }
                else if (!Restriction.Contains(Restrictions[i].GUID)) Restrictions.RemoveAt(i);
            }
            i = AggregateRestrictions.Count;
            while (--i >= 0)
            {
                if (string.IsNullOrWhiteSpace(AggregateRestriction)) AggregateRestrictions.RemoveAt(i);
                if (AggregateRestrictions[i].MetaColumn == null)
                {
                    if (AggregateRestriction != null) AggregateRestriction = AggregateRestriction.Replace(AggregateRestrictions[i].GUID, "(Warning) Restriction lost: " + AggregateRestrictions[i].Name);
                    AggregateRestrictions.RemoveAt(i);
                }
                else if (!AggregateRestriction.Contains(AggregateRestrictions[i].GUID)) AggregateRestrictions.RemoveAt(i);
            }
        }

        /// <summary>
        /// Set column names for the elements before building the SQL
        /// </summary>
        public void SetColumnsName()
        {
            int colIndex = 0;
            foreach (ReportElement element in Elements) element.SQLColumnName = Source.IsNoSQL || UseRawSQL ? element.MetaColumn.Name : string.Format("C{0}", colIndex++);
        }

        /// <summary>
        /// Add Sub-Reports elements to the SQL
        /// </summary>
        void AddSubReportsElements()
        {
            //Add elements for sub-reports
            foreach (var el in Elements.Where(i => i.MetaColumn.SubReports.Count > 0 && i.PivotPosition != PivotPosition.Data).ToList())
            {
                foreach (var subreport in el.MetaColumn.SubReports)
                {
                    foreach (var guid in subreport.Restrictions)
                    {
                        if (!Elements.Exists(i => i.MetaColumnGUID == guid))
                        {
                            //Add the element
                            ReportElement element = ReportElement.Create();
                            element.Source = Source;
                            element.Report = Report;
                            element.Model = this;
                            element.MetaColumnGUID = guid;
                            element.PivotPosition = PivotPosition.Hidden;
                            element.IsForNavigation = true;
                            element.SortOrder = ReportElement.kNoSortKeyword;
                            Elements.Add(element);
                        }
                    }
                }
            }
        }

        [XmlIgnore]
        DateTime _buildTimer;
        [XmlIgnore]
        int _bestJoinsCount = 0;
        [XmlIgnore]
        int _directCount = 0;
        [XmlIgnore]
        int _indirectCount = 0;

        /// <summary>
        /// Description of the joins chosen to build the SQL
        /// </summary>
        [XmlIgnore]
        public StringBuilder JoinPaths = null;

        /// <summary>
        /// Build the SQL for the model
        /// </summary>
        public void BuildSQL(bool forConversion = false)
        {
            try
            {
                ExecutionError = "";
                _sql = "";

                if (IsSQLModel && UseRawSQL)
                {
                    _sql = Table.Sql;
                    _sql = ParseCommonRestrictions(_sql);
                    return;
                }

                if (Source.MetaData == null) return;

                if (!forConversion) AddSubReportsElements();

                InitReferences();

                execSelectClause = new StringBuilder();
                execCTEClause = "";
                execFromClause = new StringBuilder();
                execWhereClause = new StringBuilder(Restriction.Trim());
                execGroupByClause = new StringBuilder();
                execOrderByNameClause = new StringBuilder();
                execHavingClause = new StringBuilder(AggregateRestriction.Trim());
                execOrderByClause = new StringBuilder();

                //build restriction
                RestrictionText = "";
                foreach (ReportRestriction restriction in ExecutionRestrictions)
                {
                    if (restriction.HasValue) Helper.AddValue(ref RestrictionText, "\r\n", restriction.DisplayText);
                    execWhereClause = execWhereClause.Replace("[" + restriction.GUID + "]", restriction.SQLText);
                }
                if (Report.CheckingExecution)
                {
                    if (execWhereClause.ToString().Trim().Length == 0) execWhereClause.Append("1=0");
                    else execWhereClause.Append(" AND (1=0)");
                }

                foreach (ReportRestriction restriction in ExecutionAggregateRestrictions)
                {
                    if (restriction.HasValue) Helper.AddValue(ref RestrictionText, "\r\n", restriction.DisplayText);
                    execHavingClause = execHavingClause.Replace("[" + restriction.GUID + "]", restriction.SQLText);
                }
                foreach (ReportRestriction restriction in ExecutionCommonRestrictions)
                {
                    if (restriction.HasValue) Helper.AddValue(ref RestrictionText, "\r\n", restriction.DisplayText);
                }

                if (Elements.Count > 0)
                {
                    FromTables = new List<MetaTable>();
                    List<MetaJoin> joins = new List<MetaJoin>();
                    List<string> selectColumns = new List<string>();
                    List<string> groupByColumns = new List<string>();
                    if (!forConversion) SetColumnsName();
                    foreach (ReportElement element in Elements)
                    {
                        string sqlColumn = element.SQLColumn + " AS " + element.SQLColumnName;
                        if (!selectColumns.Contains(sqlColumn))
                        {
                            Helper.AddValue(ref execSelectClause, ",\r\n", "  " + sqlColumn);
                            selectColumns.Add(sqlColumn);
                        }

                        MetaTable table = element.MetaColumn.MetaTable;
                        if (table != null && !FromTables.Contains(table)) FromTables.Add(table);

                        if (element.PivotPosition != PivotPosition.Data && !element.IsAggregateEl && !groupByColumns.Contains(element.SQLColumn))
                        {
                            Helper.AddValue(ref execGroupByClause, ",", element.SQLColumn);
                            groupByColumns.Add(element.SQLColumn);
                        }
                    }

                    foreach (ReportRestriction restriction in ExecutionRestrictions.Union(ExecutionAggregateRestrictions))
                    {
                        MetaTable table = restriction.MetaColumn.MetaTable;
                        if (table != null && !FromTables.Contains(table) && restriction.HasValue && restriction.Operator != Operator.ValueOnly) FromTables.Add(table);
                    }

                    //Clear group by clause if not necessary
                    if (GetElements(PivotPosition.Data).Count() == 0 && Elements.Count(i => i.IsAggregateEl) == 0 &&  execHavingClause.Length == 0) execGroupByClause = new StringBuilder();

                    List<string> orderColumns = new List<string>();
                    UpdateFinalSortOrders();
                    buildOrderClause(GetElements(PivotPosition.Page), orderColumns, ref execOrderByClause, ref execOrderByNameClause);
                    buildOrderClause(GetElements(PivotPosition.Row), orderColumns, ref execOrderByClause, ref execOrderByNameClause);
                    buildOrderClause(GetElements(PivotPosition.Column), orderColumns, ref execOrderByClause, ref execOrderByNameClause);
                    buildOrderClause(GetElements(PivotPosition.Data), orderColumns, ref execOrderByClause, ref execOrderByNameClause);

                    List<MetaTable> extraWhereTables = FromTables.Where(i => !string.IsNullOrEmpty(i.WhereSQL)).ToList();
                    if (FromTables.Count == 1)
                    {
                        string CTE = "", name = "";
                        FromTables[0].GetExecSQLName(ref CTE, ref name);
                        execCTEClause = Helper.AddCTE(execCTEClause, CTE);
                        execFromClause.Append(name + "\r\n");
                        if (JoinPaths != null) JoinPaths.AppendLine("Only one table: No join required.");
                    }
                    else
                    {
                        //multiple tables, find joins...
                        List<MetaTable> tablesToUse = FromTables.ToList();
                        List<JoinPath> resultPaths = new List<JoinPath>();
                        JoinPath bestPath = null;

                        //Build the list of joins to use: for each table, joins related
                        var joinsToUse = new Dictionary<string, List<MetaJoin>>();
                        foreach (var join in Source.MetaData.Joins.Where(i => i.LeftTableGUID != null))
                        {
                            //Filter in joins to use here
                            if (JoinsToUse.Count > 0 && !JoinsToUse.Contains(join.GUID)) continue;

                            if (!joinsToUse.Keys.Contains(join.LeftTableGUID)) joinsToUse.Add(join.LeftTableGUID, new List<MetaJoin>() { join });
                            else
                            {
                                var list = joinsToUse[join.LeftTableGUID];
                                if (!list.Exists(i => i.LeftTableGUID == join.LeftTableGUID && i.RightTableGUID == join.RightTableGUID)) joinsToUse[join.LeftTableGUID].Add(join);
                            }

                            if (join.IsBiDirectional)
                            {
                                //Create a new join having the other left-right
                                var newJoin = MetaJoin.Create();
                                newJoin.IsBiDirectional = false;
                                newJoin.GUID = join.GUID;
                                newJoin.Source = join.Source;
                                newJoin.LeftTableGUID = join.RightTableGUID;
                                newJoin.RightTableGUID = join.LeftTableGUID;

                                //Bug 131: Invert left and right
                                if (join.JoinType == JoinType.LeftOuter) newJoin.JoinType = JoinType.RightOuter;
                                else if (join.JoinType == JoinType.RightOuter) newJoin.JoinType = JoinType.LeftOuter;
                                else newJoin.JoinType = join.JoinType;

                                newJoin.Clause = join.Clause;

                                if (!joinsToUse.Keys.Contains(newJoin.LeftTableGUID)) joinsToUse.Add(newJoin.LeftTableGUID, new List<MetaJoin>() { newJoin });
                                else
                                {
                                    var list = joinsToUse[newJoin.LeftTableGUID];
                                    if (!list.Exists(i => i.LeftTableGUID == newJoin.LeftTableGUID && i.RightTableGUID == newJoin.RightTableGUID)) joinsToUse[newJoin.LeftTableGUID].Add(newJoin);
                                }
                            }
                        }

                        _buildTimer = DateTime.Now;
                        _directCount = 0;
                        _indirectCount = 0;
                        _bestJoinsCount = joinsToUse.Count;

                        foreach (var leftTable in FromTables)
                        {
                            JoinPath rootPath = new JoinPath() { currentTable = leftTable, joinsToUse = new Dictionary<string, MetaJoin[]>() };
                            //Copy the list of joins to use from the reference
                            foreach (var key in joinsToUse.Keys)
                            {
                                rootPath.joinsToUse.Add(key, joinsToUse[key].Where(i => i.RightTableGUID != leftTable.GUID).ToArray());
                            }

                            rootPath.tablesToUse = new List<MetaTable>(FromTables.Where(i => i.GUID != leftTable.GUID));
                            JoinTables(rootPath, resultPaths);
                        }
                        Console.WriteLine("Direct Join: {0:F0}ms {1} {2}", (DateTime.Now - _buildTimer).TotalMilliseconds, resultPaths.Count, _directCount);

                        if (JoinPaths != null)
                        {
                            JoinPaths.AppendFormat("Time elapsed after Direct Joins: {0:F0} ms\r\n\r\n", (DateTime.Now - _buildTimer).TotalMilliseconds);
                            JoinPaths.AppendLine("DIRECT Joins found by priority order (The first one may be used if all tables are joined, maximum 100 are shown):\r\n");
                            int index = 1;
                            foreach (var path in resultPaths.OrderBy(i => i.tablesToUse.Count).ThenBy(i => i.joins.Count).Take(100))
                            {
                                JoinPaths.AppendFormat("Direct Join {0}: ", index++);
                                path.print(JoinPaths);
                            }
                        }

                        //Choose the path having all tables, then preferred, then less joins...
                        if (bestPath == null) bestPath = resultPaths.Where(i => i.tablesToUse.Count == 0).OrderBy(i => i.joins.Count).FirstOrDefault();
                        bool checkIndirectJoin = false;
                        if (bestPath == null) checkIndirectJoin = true;
                        else if (bestPath.joins.Count > tablesToUse.Count - 1) checkIndirectJoin = true;
                        // otherwise it means that a direct join with a minimum joins have been found, no need to check indirect joins 

                        if (checkIndirectJoin)
                        {
                            List<JoinPath> resultPaths2 = new List<JoinPath>();
                            //no direct joins found or more than 3 joins...try using several path...
                            foreach (var path in resultPaths.OrderBy(i => i.tablesToUse.Count))
                            {
                                JoinPath newPath = new JoinPath() { joins = new List<MetaJoin>(path.joins), tablesToUse = new List<MetaTable>(path.tablesToUse) };
                                //newPath.print();
                                foreach (var join in path.joins)
                                {
                                    if (newPath.joins.Count >= _bestJoinsCount) break;
                                    //search a path starting from RightTable and finishing by a remaining table
                                    foreach (var path2 in resultPaths.OrderBy(i => i.tablesToUse.Count).Where(i => i.startTable == join.RightTable && path.tablesToUse.Contains(i.finalTable)))
                                    {
                                        if (newPath.joins.Count >= _bestJoinsCount) break;
                                        //ok add joins to the newPath and remove tables to use
                                        foreach (var join2 in path2.joins)
                                        {
                                            if (newPath.joins.Count >= _bestJoinsCount) break;
                                            _indirectCount++;

                                            //Add the join to the path
                                            if (!newPath.joins.Exists(i => i.GUID == join2.GUID))
                                            {
                                                newPath.joins.Insert(0, join2); // Fix 108
                                                //newPath.print();
                                            }
                                            newPath.tablesToUse.Remove(join2.LeftTable);
                                            newPath.tablesToUse.Remove(join2.RightTable);
                                        }

                                        if (newPath.tablesToUse.Count == 0)
                                        {
                                            //got one
                                            resultPaths2.Add(newPath);

                                            if (newPath.joins.Count < _bestJoinsCount)
                                            {
                                                _bestJoinsCount = newPath.joins.Count;
                                            }
                                            break;
                                        }
                                    }

                                    if (newPath.tablesToUse.Count == 0) break;
                                }

                                if ((DateTime.Now - _buildTimer).TotalMilliseconds > BuildTimeout)
                                {
                                    var bestPathIndirect = resultPaths2.Where(i => i.tablesToUse.Count == 0).OrderBy(i => i.joins.Count).FirstOrDefault();
                                    if (bestPath != null || bestPathIndirect != null)
                                    {
                                        if (JoinPaths != null) JoinPaths.AppendFormat("Exiting the joins search after {0:F0} milliseconds\r\n", (DateTime.Now - _buildTimer).TotalMilliseconds);
                                        break;
                                    }
                                }
                            }

                            Console.WriteLine("Indirect Joins: {0:F0}ms {1} {2}", (DateTime.Now - _buildTimer).TotalMilliseconds, resultPaths2.Count, _indirectCount);

                            if (JoinPaths != null)
                            {
                                JoinPaths.AppendFormat("\r\nTime elapsed after Indirect Joins: {0:F0} ms\r\n\r\n", (DateTime.Now - _buildTimer).TotalMilliseconds);
                                JoinPaths.AppendLine("INDIRECT Joins found by priority order (The first one may be used if all tables are joined, maximum 100 are shown):\r\n");
                                int index = 1;
                                foreach (var path in resultPaths2.OrderBy(i => i.tablesToUse.Count).ThenBy(i => i.joins.Count).Take(100))
                                {
                                    JoinPaths.AppendFormat("Indirect Join {0}: ", index++);
                                    path.print(JoinPaths);
                                }
                            }

                            var bestPath2 = resultPaths2.Where(i => i.tablesToUse.Count == 0).OrderBy(i => i.joins.Count).FirstOrDefault();
                            if (bestPath != null && bestPath2 != null)
                            {
                                //Choose here between direct best path or indirect best path
                                if (bestPath2.joins.Count < bestPath.joins.Count) bestPath = bestPath2;
                            }
                            else if (bestPath == null) {
                                bestPath = bestPath2;
                            }
                        }

                        if (JoinPaths != null && bestPath != null)
                        {
                            JoinPaths.AppendFormat("\r\nTime elapsed: {0:F0} ms\r\n", (DateTime.Now - _buildTimer).TotalMilliseconds);
                            JoinPaths.AppendLine("\r\nAND THE WINNER IS:");
                            bestPath.print(JoinPaths);
                        }

                        if (bestPath == null)
                        {
                            var errMessage = "Unable to link all elements using the joins defined...\r\nAdd Joins to your Data Source\r\nOR remove elements or restrictions in your model\r\nOR add relevant elements or restrictions in your model.";
                            if (JoinPaths != null) JoinPaths.AppendLine("\r\n" + errMessage);
                            throw new Exception(errMessage);
                        }
                        if (bestPath.joins.Count == 0)
                        {
                            //only one table
                            string CTE = "", name = "";
                            bestPath.currentTable.GetExecSQLName(ref CTE, ref name);
                            execCTEClause = Helper.AddCTE(execCTEClause, CTE);
                            execFromClause.Append(name + "\r\n");

                            if (JoinPaths != null) JoinPaths.AppendLine("Only one table: No join required.");
                        }
                        else
                        {
                            string lastTable = null;
                            List<MetaTable> tablesUsed = new List<MetaTable>();
                            for (int i = bestPath.joins.Count - 1; i >= 0; i--)
                            {
                                MetaJoin join = bestPath.joins[i];
                                if (string.IsNullOrEmpty(lastTable))
                                {
                                    string CTE2 = "", name2 = "";
                                    join.RightTable.GetExecSQLName(ref CTE2, ref name2);
                                    execCTEClause = Helper.AddCTE(execCTEClause, CTE2);
                                    lastTable = name2 + "\r\n";
                                    tablesUsed.Add(join.RightTable);
                                }

                                //check if tables are already in the join
                                var leftTable = join.LeftTable;
                                if (tablesUsed.Contains(leftTable)) leftTable = join.RightTable;
                                if (tablesUsed.Contains(leftTable)) continue;

                                string joinClause = join.Clause.Trim();
                                //For outer join, add the extra restriction in the ON clause -> hopefully they are not defined as bi-directional
                                MetaTable extraWhereTable = null;
                                if (join.JoinType == JoinType.LeftOuter && !string.IsNullOrEmpty(join.RightTable.WhereSQL)) extraWhereTable = join.RightTable;
                                else if (join.JoinType == JoinType.RightOuter && !string.IsNullOrWhiteSpace(join.LeftTable.WhereSQL)) extraWhereTable = join.LeftTable;
                                else if (!string.IsNullOrWhiteSpace(leftTable.WhereSQL) && !extraWhereTables.Contains(leftTable))
                                {
                                    extraWhereTables.Add(leftTable);
                                }

                                if (extraWhereTable != null)
                                {
                                    string where = RazorHelper.CompileExecute(extraWhereTable.WhereSQL, extraWhereTable);
                                    if (!string.IsNullOrEmpty(where)) joinClause += " AND " + where;
                                    extraWhereTables.Remove(extraWhereTable);
                                }

                                //finally build the clause
                                string CTE = "", name = "";
                                leftTable.GetExecSQLName(ref CTE, ref name);
                                execCTEClause = Helper.AddCTE(execCTEClause, CTE);

                                if (join.JoinType != JoinType.Cross) lastTable = string.Format("\r\n({0} {1} {2} ON {3})\r\n", name, join.SQLJoinType, lastTable, joinClause);
                                else lastTable = string.Format("\r\n({0} {1} {2})\r\n", name, join.SQLJoinType, lastTable);

                                tablesUsed.Add(leftTable);
                            }
                            execFromClause = new StringBuilder(lastTable);
                        }
                        if (JoinPaths != null)
                        {
                            JoinPaths.Append("\r\nSQL Generated:");
                            JoinPaths.Append(execFromClause.ToString());
                        }
                    }

                    //add extra where clause
                    foreach (var table in extraWhereTables)
                    {
                        if (!string.IsNullOrWhiteSpace(table.WhereSQL))
                        {
                            string where = RazorHelper.CompileExecute(table.WhereSQL, table);
                            if (!string.IsNullOrWhiteSpace(where))
                            {
                                if (execWhereClause.Length != 0) execWhereClause.Append("\r\nAND ");
                                execWhereClause.AppendFormat("({0})", where);
                            }
                        }
                    }

                    //Get CTE first
                    execSelect = execGroupByClause.Length > 0 ? "SELECT\r\n" : "SELECT DISTINCT\r\n";
                    execSelect = !string.IsNullOrEmpty(SqlSelect) ? SqlSelect : execSelect;
                    _sql = !string.IsNullOrEmpty(SqlCTE) ? SqlCTE : execCTEClause;
                    _sql += execSelect;
                    _sql += string.Format("{0}\r\n", execSelectClause);
                    _sql += !string.IsNullOrEmpty(SqlFrom) ? SqlFrom : string.Format("FROM {0}", execFromClause);
                    if (execWhereClause.Length > 0) _sql += string.Format("WHERE {0}\r\n", execWhereClause);
                    if (execGroupByClause.Length > 0 || !string.IsNullOrEmpty(SqlGroupBy)) _sql += (!string.IsNullOrEmpty(SqlGroupBy) ? SqlGroupBy : string.Format("GROUP BY {0}", execGroupByClause)) + "\r\n";
                    if (execHavingClause.Length > 0) _sql += string.Format("HAVING {0}\r\n", execHavingClause);
                    if (!forConversion && (execOrderByClause.Length > 0 || !string.IsNullOrEmpty(SqlOrderBy))) _sql += (!string.IsNullOrEmpty(SqlOrderBy) ? SqlOrderBy : string.Format("ORDER BY {0}", execOrderByClause)) + "\r\n";

                    //Finally inject common restriction values
                    if (!forConversion) _sql = ParseCommonRestrictions(_sql);
                }
            }
            catch (TemplateCompilationException ex)
            {
                _sql = "";
                ExecutionError = string.Format("Got unexpected error when building the SQL statement:\r\n{0}", Helper.GetExceptionMessage(ex));
            }
            catch (Exception ex)
            {
                _sql = "";
                ExecutionError = string.Format("Got unexpected error when building the SQL statement:\r\n{0}", ex.Message);
            }
        }

        class JoinPath
        {
            public MetaTable currentTable = null;
            public MetaTable startTable = null;
            public MetaTable finalTable = null;
            public List<MetaJoin> joins = new List<MetaJoin>();
            public List<MetaTable> tablesToUse;
            public Dictionary<string, MetaJoin[]> joinsToUse;

            public string print()
            {
                var str = new StringBuilder();
                print(str);
                return str.ToString();
            }

            public void print(StringBuilder joinPaths)
            {
                if (joinPaths == null) return;
                joinPaths.AppendFormat("Tables left: {0} , Joins used:{1}\r\n", tablesToUse.Count, joins.Count);
                for (int i=0; i<joins.Count; i++)
                {
                    var join = joins[i];
                    if (i==0) joinPaths.Append(join.LeftTable.DisplayName + "->");
                    if (i > 0 && join.LeftTableGUID != joins[i-1].RightTableGUID)
                    {
                        //Break
                        joinPaths.Append("\r\n" + join.LeftTable.DisplayName + "->");
                    }
                    joinPaths.Append(join.RightTable.DisplayName);
                    if (i < joins.Count - 1 && join.RightTableGUID == joins[i+1].LeftTableGUID)
                    {
                        joinPaths.Append("->");
                    }
                }
                /*
                joinPaths.AppendLine("\r\nDetail:");
                foreach (var join in joins)
                {
                    joinPaths.AppendFormat(string.Format("{0}-{1} ({2})\r\n", join.LeftTable.DisplayName, join.RightTable.DisplayName, join.Clause.Trim()));
                }*/
                joinPaths.Append("\r\n\r\n");
            }
        }

        void JoinTables(JoinPath path, List<JoinPath> resultPath)
        {
            //no need to find another path as it will be worth...
            if (path.joins.Count >= _bestJoinsCount) return;

            //If the search is longer than xx seconds, we exit with the first path found...
            if ((DateTime.Now - _buildTimer).TotalMilliseconds > BuildTimeout)
            {
                if (resultPath.Exists(i => i.tablesToUse.Count == 0))
                {
                    if (JoinPaths != null) JoinPaths.AppendFormat("Exiting the joins search after {0:F0} milliseconds\r\n", (DateTime.Now - _buildTimer).TotalMilliseconds);
                    return;
                }
            }

            if (path.tablesToUse.Count != 0)
            {
                if (path.joinsToUse.Keys.Contains(path.currentTable.GUID))
                {
                    foreach (var join in path.joinsToUse[path.currentTable.GUID])
                    {
                        MetaTable newTable = join.RightTable;

                        JoinPath newJoinPath = new JoinPath() { joins = new List<MetaJoin>(path.joins), tablesToUse = new List<MetaTable>(path.tablesToUse), joinsToUse = new Dictionary<string, MetaJoin[]>() };
                        //copy the list of joins to use 
                        foreach (var key in path.joinsToUse.Keys)
                        {
                            if (key != path.currentTable.GUID)
                            {
                                //we take only the joins that can reach a table different than the new reached table
                                var joins = path.joinsToUse[key].Where(i => i.RightTableGUID != newTable.GUID).ToArray();
                                if (joins.Length > 0) newJoinPath.joinsToUse.Add(key, joins);
                            }
                        }

                        //add the join and continue the path
                        newJoinPath.currentTable = newTable;
                        newJoinPath.joins.Add(join);
                        newJoinPath.tablesToUse.Remove(newTable);

                        JoinTables(newJoinPath, resultPath);

                        _directCount++;
                    }
                }
            }

            if (path.joins.Count > 0 && FromTables.Contains(path.joins.Last().RightTable))
            {
                path.startTable = path.joins.First().LeftTable;
                path.finalTable = path.joins.Last().RightTable;
                resultPath.Add(path);
                if (path.tablesToUse.Count == 0 && path.joins.Count < _bestJoinsCount)
                {
                    //This is the best for the moment...
                    _bestJoinsCount = path.joins.Count;
                }
            }
        }

        /// <summary>
        /// Update the final sort orders
        /// </summary>
        public void UpdateFinalSortOrders()
        {
            updateFinalSortOrder(GetElements(PivotPosition.Page));
            updateFinalSortOrder(GetElements(PivotPosition.Row));
            updateFinalSortOrder(GetElements(PivotPosition.Column));
            updateFinalSortOrder(GetElements(PivotPosition.Data));
        }

        void updateFinalSortOrder(IEnumerable<ReportElement> elements)
        {
            for (int i = 0; i < elements.Count(); i++)
            {
                ReportElement element = elements.ElementAt(i);
                if (element.SortOrder != ReportElement.kNoSortKeyword)
                {
                    if (element.SortOrder == ReportElement.kAutomaticAscSortKeyword) element.FinalSortOrder = string.Format("{0} {1}", i, ReportElement.kAscendantSortKeyword);
                    else if (element.SortOrder == ReportElement.kAutomaticDescSortKeyword) element.FinalSortOrder = string.Format("{0} {1}", i, ReportElement.kDescendantSortKeyword);
                    else element.FinalSortOrder = element.SortOrder;
                }
            }
        }

        void buildOrderClause(IEnumerable<ReportElement> elements, List<string> orderColumns, ref StringBuilder orderClause, ref StringBuilder orderClauseName)
        {
            foreach (ReportElement element in elements.OrderBy(i => i.FinalSortOrder))
            {
                if (!orderColumns.Contains(element.SQLColumn) && element.IsSorted)
                {
                    string ascdesc = element.SortOrder.Contains(ReportElement.kAscendantSortKeyword) ? " ASC" : " DESC";
                    Helper.AddValue(ref orderClause, ",", (Source.IsNoSQL ? element.SQLColumnName : element.SQLColumn) + ascdesc); //If NoSQL, the Data View can not be sorted with aggregate...
                    Helper.AddValue(ref orderClauseName, ",", element.SQLColumnName + ascdesc);
                    orderColumns.Add(element.SQLColumn);
                }
            }
        }

        DbCommand _command;
        /// <summary>
        /// Cancel the model execution
        /// </summary>
        public void CancelCommand()
        {
            if (_commandMutex.WaitOne(1000))
            {
                try
                {
                    if (_command != null)
                    {
                        _command.Cancel();
                    }
                }
                finally
                {
                    _commandMutex.ReleaseMutex();
                }
            }
        }
        Mutex _commandMutex = new Mutex();

        void executePrePostStatement(string sql, string prefix, string name, bool ignoreErrors, object model)
        {
            if (!string.IsNullOrEmpty(sql))
            {
                try
                {
                    Report.LogMessage("Model '{0}': Executing {1}-SQL statement for '{2}'...", Name, prefix, name);
                    string finalSql = RazorHelper.CompileExecute(sql, model);
                    if (!string.IsNullOrEmpty(finalSql))
                    {
                        _command.CommandText = ParseCommonRestrictions(finalSql);
                        _command.ExecuteNonQuery();
                    }
                    else Report.LogMessage("No SQL to execute...");
                }
                catch (Exception ex)
                {
                    string error = string.Format("Model '{0}': Error got during execution of {1}-SQL statement for '{2}'\r\n{3}", Name, prefix, name, ex.Message);
                    if (ignoreErrors) Report.LogMessage(error);
                    else throw new Exception(error);
                }
            }
        }
        void executePrePostStatements(bool isPre)
        {
            if (FromTables == null) return;

            foreach (var table in FromTables)
            {
                executePrePostStatement(isPre ? table.PreSQL : table.PostSQL, isPre ? "Pre" : "Post", table.Name, table.IgnorePrePostError, table);
            }
        }

        /// <summary>
        /// Execute custom Razor script for the model: Pre Load, Post Load and Final
        /// </summary>
        public void ExecuteLoadScript(string script, string name, object model)
        {
            if (!string.IsNullOrEmpty(script))
            {
                try
                {
                    RazorHelper.CompileExecute(script, model);
                }
                catch (Exception razorException)
                {
                    throw new Exception(string.Format("Error when executing '{0}'.\r\n{1}", name, razorException.Message));
                }
            }
        }

        /// <summary>
        /// List of models being executed
        /// </summary>
        public static Dictionary<string, ReportModel> RunningModels = new Dictionary<string, ReportModel>();
        void checkRunningModels(string key)
        {
            lock (RunningModels)
            {
                if (RunningModels.ContainsKey(key))
                {
                    //check if we can reuse the current running query: same source, same connection string and same pre/Post SQL
                    ReportModel runningModel = RunningModels[key];
                    if (Source == runningModel.Source
                        && Connection.ConnectionType == runningModel.Connection.ConnectionType
                        && Connection.FullConnectionString == runningModel.Connection.FullConnectionString
                        && string.IsNullOrEmpty(runningModel.ExecutionError)
                        && ((PreSQL == null && runningModel.PreSQL == null) || (PreSQL.Trim() == runningModel.PreSQL.Trim()))
                        && ((PostSQL == null && runningModel.PostSQL == null) || (PostSQL.Trim() == runningModel.PostSQL.Trim()))
                        && ((PreLoadScript == null && runningModel.PreLoadScript == null) || (PreLoadScript.Trim() == runningModel.PreLoadScript.Trim()))
                        && ((LoadScript == null && runningModel.LoadScript == null) || (LoadScript.Trim() == runningModel.LoadScript.Trim()))
                        && ((FinalScript == null && runningModel.FinalScript == null) || (FinalScript.Trim() == runningModel.FinalScript.Trim()))
                        )
                    {
                        //we can wait to get the same data table 
                        Report.LogMessage("Model '{0}': Getting result table from '{1}'...", Name, runningModel.Name);
                        while (!Report.Cancel && !runningModel._resultTableAvailable)
                        {
                            if (!string.IsNullOrEmpty(runningModel.ExecutionError)) break;
                            Thread.Sleep(100);
                        }

                        if (runningModel.ResultTable != null)
                        {
                            //Set the result table
                            ResultTable = Source.IsNoSQL ? runningModel._rawNoSQLTable : runningModel.ResultTable;
                            ExecutionDuration = runningModel.ExecutionDuration;
                        }
                    }
                }
                else
                {
                    RunningModels.Add(key, this);
                }
            }
        }

        [XmlIgnore]
        private DataTable _rawNoSQLTable = null;
        [XmlIgnore]
        private bool _resultTableAvailable = false;

        /// <summary>
        /// Build the ResultTable for the model
        /// </summary>
        public void FillResultTable()
        {
            bool isMaster = false;
            Progression = 0;

            ExecutionDuration = 0;
            _resultTableAvailable = false;
            Pages.Clear();

            //Pre-load script
            if (!Source.IsNoSQL) ExecuteLoadScript(PreLoadScript, "Pre Load Script", this);

            ExecutionError = "";

            BuildSQL();
            Progression = 5; //5% after building SQL

            ResultTable = null;
            _command = null;

            if (Source.IsNoSQL && Elements.Count > 0 && !Report.Cancel)
            {
                //No SQL
                try
                {
                    Source.MetaData.MasterTable.Log = Report;
                    Source.MetaData.MasterTable.NoSQLModel = this;
                    string key = !string.IsNullOrEmpty(LoadScript) ? LoadScript : "_Master_" + Source.MetaData.MasterTable.LoadScript;

                    //Check if we can use a data table from another model
                    checkRunningModels(key);

                    if (ResultTable == null)
                    {
                        if (!string.IsNullOrEmpty(LoadScript))
                        {
                            ResultTable = Source.MetaData.MasterTable.BuildNoSQLTable(false);
                            RazorHelper.CompileExecute(LoadScript, this);
                        }
                        else
                        {
                            ResultTable = Source.MetaData.MasterTable.BuildNoSQLTable(true);
                        }
                        _rawNoSQLTable = ResultTable;
                    }

                    //apply filters using DataView
                    if (execWhereClause.Length > 0 || execOrderByClause.Length > 0)
                    {
                        //make a copy first
                        ResultTable = ResultTable.Copy();
                        var dataView = new DataView(ResultTable);
                        dataView.RowFilter = execWhereClause.ToString();
                        dataView.Sort = execOrderByClause.ToString();
                        ResultTable = dataView.ToTable();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Got unexpected error when building NoSQL Table:\r\n{0}\r\n", ex.Message));
                }
            }
            else if (!string.IsNullOrEmpty(_sql) && !Report.Cancel)
            {
                //Normal SQL
                try
                {
                    if (string.IsNullOrEmpty(Connection.ConnectionString) && string.IsNullOrEmpty(Connection.MSSqlServerConnectionString)) throw new Exception("The connection string is not defined for this Model.");
                    _command = null;

                    //Check if we can use a data table from another model
                    checkRunningModels(_sql);

                    if (ResultTable == null)
                    {
                        isMaster = true; //This model is the master for the Result table
                        DbConnection connection = null;
                        if (_commandMutex.WaitOne(1000))
                        {
                            try
                            {
                                connection = Connection.GetOpenConnection();
                                if (connection is OdbcConnection) _command = ((OdbcConnection)connection).CreateCommand();
                                else if (connection is SqlConnection) _command = ((SqlConnection)connection).CreateCommand();
                                else _command = ((OleDbConnection)connection).CreateCommand();
                                _command.CommandTimeout = 0;
                                ExecutionDate = DateTime.Now;
                            }
                            finally
                            {
                                _commandMutex.ReleaseMutex();
                            }
                        }
                        else
                        {
                            throw new Exception("Unable to get command mutex...");
                        }
                        executePrePostStatement(Source.PreSQL, "Pre", Source.Name, Source.IgnorePrePostError, Source);
                        executePrePostStatements(true);
                        executePrePostStatement(PreSQL, "Pre", Name, IgnorePrePostError, this);
                        Report.LogMessage("Model '{0}': Executing main query...", Name);
                        _command.CommandText = _sql;

                        ResultTable = new DataTable();
                        DbDataAdapter adapter = null;
                        if (connection is OdbcConnection) adapter = new OdbcDataAdapter((OdbcCommand)_command);
                        else if (connection is SqlConnection) adapter = new SqlDataAdapter((SqlCommand)_command);
                        else adapter = new OleDbDataAdapter((OleDbCommand)_command);
                        adapter.Fill(ResultTable);
                        executePrePostStatement(PostSQL, "Post", Name, IgnorePrePostError, this);
                        executePrePostStatements(false);
                        executePrePostStatement(Source.PostSQL, "Post", Source.Name, Source.IgnorePrePostError, Source);
                        connection.Close();
                        _command = null;
                        ExecutionDuration = Convert.ToInt32((DateTime.Now - ExecutionDate).TotalSeconds);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Unexpected error when executing the following SQL statement:\r\n{0}\r\n\r\nError detail:\r\n{1}", _sql, ex.Message));
                }

                if (Report.Cancel) return;

                //If enum, set enum values directly in the table
                if (isMaster)
                {
                    bool hasEnum = false;
                    List<ReportElement> specialSortByPositionElements = new List<ReportElement>();
                    foreach (var element in Elements)
                    {
                        if (element.IsEnum)
                        {
                            lock (ResultTable)
                            {
                                hasEnum = true;
                                DataColumn col = ResultTable.Columns[element.SQLColumnName];
                                DataColumn newcol = new DataColumn("_seal_dummy_temp_col_", typeof(string));
                                ResultTable.Columns.Add(newcol);

                                foreach (DataRow row in ResultTable.Rows)
                                {
                                    //to sort by position, we add 6 digits as a prefix
                                    if (element.IsSorted && element.EnumEL.UsePosition && !specialSortByPositionElements.Contains(element)) specialSortByPositionElements.Add(element);
                                    row[newcol] = element.GetEnumSortValue(row[col].ToString(), false);
                                }
                                ResultTable.Columns.Remove(col);
                                newcol.ColumnName = element.SQLColumnName;
                            }
                        }
                    }

                    if (Report.Cancel) return;

                    if (hasEnum)
                    {
                        //this re-sort the result with enum values...
                        DataView dv = new DataView(ResultTable, null, execOrderByNameClause.ToString(), DataViewRowState.CurrentRows);
                        ResultTable = dv.ToTable();

                        if (Report.Cancel) return;

                        //remove the 6 digits used for special sort
                        foreach (var element in specialSortByPositionElements)
                        {
                            DataColumn col = ResultTable.Columns[element.SQLColumnName];
                            foreach (DataRow row in ResultTable.Rows)
                            {
                                string newValue = row[col].ToString();
                                if (newValue.Length > 5) row[col] = newValue.Substring(6);
                            }
                        }
                    }
                }

                ExecuteLoadScript(LoadScript, "Post Load Script", this);
            }

            Progression = 70; //70% after getting result set
            _resultTableAvailable = true;
        }

        /// <summary>
        /// Invert the rows and the columns of all DataTables of the pages generated
        /// </summary>
        public void InvertDataTables()
        {
            foreach (ResultPage page in Pages)
            {
                if (Report.Cancel) break;

                if (page.DataTable.Lines.Count > 0 && !page.DataTable.InvertDone)
                {
                    ResultTable newTable = new ResultTable();
                    for (int i = 0; i < page.DataTable.Lines[0].Length; i++)
                    {
                        ResultCell[] newLine = new ResultCell[page.DataTable.Lines.Count];
                        for (int j = 0; j < page.DataTable.Lines.Count; j++)
                        {
                            newLine[j] = page.DataTable.Lines[j][i];
                        }
                        newTable.Lines.Add(newLine);
                    }

                    //Revert start row and column
                    newTable.BodyStartRow = page.DataTable.BodyStartColumn;
                    newTable.BodyStartColumn = page.DataTable.BodyStartRow;
                    //Calculate body end
                    for (int i = 0; i < page.DataTable.Lines[0].Length; i++)
                    {
                        if (!page.DataTable.Lines[0][i].IsTotal) newTable.BodyEndRow++;
                    }
                    page.DataTable = newTable;
                    page.DataTable.InvertDone = true;
                }
            }
        }

        /// <summary>
        /// Returns a model restriction from its name
        /// </summary>
        public ReportRestriction GetRestrictionByName(string name)
        {
            return Restrictions.FirstOrDefault(i => i.DisplayNameEl.ToLower() == name.ToLower());
        }


        /// <summary>
        /// HTML Navigation for the report result
        /// </summary>
        public string GetNavigation(ResultCell cell, bool serverSide = false)
        {
            string navigation = "";
            if (Report.GenerateHTMLDisplay || serverSide)
            {
                foreach (var link in cell.Links)
                {
                    if (link.Type == NavigationType.Drill && !Report.ExecutionView.GetBoolValue(Parameter.DrillEnabledParameter)) continue;
                    if (link.Type == NavigationType.SubReport && !Report.ExecutionView.GetBoolValue(Parameter.SubReportsEnabledParameter)) continue;
                    navigation += string.Format("<li nav='{0}'><a href='#'>{1}</a></li>", link.FullHref, link.Text /* Helper.ToHtml(link.Text) TODO for chart labels */);
                }
                navigation = string.IsNullOrEmpty(navigation) ? "" : (serverSide ? navigation : string.Format(" navigation=\"{0}\"", navigation));
            }
            return navigation;
        }
    }
}