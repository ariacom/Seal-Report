//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
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
using System.Threading;
using RazorEngine.Templating;
using System.Diagnostics;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Npgsql;
using System.Data.SQLite;

#if WINDOWS
using Seal.Forms;
using System.Drawing.Design;
using DynamicTypeDescriptor;
#endif

namespace Seal.Model
{
    /// <summary>
    /// A ReportModel defines how to generate the Result Set (Data Table) and Series from the database.
    /// </summary>
    public class ReportModel : ReportComponent
    {
        const string DefaultClause = "<Default Clause>";
        public const string DefaultLINQScriptTemplate = "var query2 = query.Distinct();";

#if WINDOWS
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);

                //Then enable
                GetProperty("SourceGUID").SetIsBrowsable(true);
                GetProperty("SourceGUID").SetIsReadOnly(MasterModel != null);

                GetProperty("ConnectionGUID").SetIsBrowsable(true);

                GetProperty("CommonRestrictions").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("PreLoadScript").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("ReferenceModelGUID").SetIsBrowsable(true);
                GetProperty("ExecutionSet").SetIsBrowsable(true);
                GetProperty("ShareResultTable").SetIsBrowsable(true);
                GetProperty("PrintQuery").SetIsBrowsable(true);

                GetProperty("LoadScript").SetIsBrowsable(!IsSubModel);
                GetProperty("FinalScript").SetIsBrowsable(!IsSubModel);
                if (Source.IsNoSQL)
                {
                    GetProperty("LoadScript").SetDisplayName("LINQ load script");
                    GetProperty("LoadScript").SetDescription("The Razor Script used to load the data in the table. If empty, the load script defined in the master table is used.");
                }
                else
                {
                    GetProperty("LoadScript").SetDisplayName("Post load script");
                    GetProperty("LoadScript").SetDescription("Optional Razor Script to modify the result table of the model just after the database load.");
                }
                GetProperty("ShowFirstLine").SetIsBrowsable(!IsSubModel);
                GetProperty("MaxNumberOfRecords").SetIsBrowsable(!IsSubModel);

                GetProperty("UseSelectDistinct").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("SqlSelect").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("SqlFrom").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("SqlGroupBy").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("SqlOrderBy").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("SqlCTE").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("LINQQueryScript").SetIsBrowsable(Source.IsNoSQL);
                GetProperty("SubModelsSetRestr").SetIsBrowsable(Source.IsNoSQL);
                GetProperty("SubModelsSetAggr").SetIsBrowsable(Source.IsNoSQL);

                GetProperty("PreSQL").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("PostSQL").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("IgnorePrePostError").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("CommandTimeout").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("BuildTimeout").SetIsBrowsable(!IsSQLModel);

                GetProperty("Alias").SetIsBrowsable(IsSQLModel);
                GetProperty("KeepColNames").SetIsBrowsable(IsSQLModel);
                GetProperty("UseRawSQL").SetIsBrowsable(IsSQLModel);

                GetProperty("JoinsToSelect").SetIsBrowsable(!IsSQLModel);
                GetProperty("JoinHashcode").SetIsBrowsable(!IsSQLModel);

                GetProperty("HelperViewJoins").SetIsBrowsable(!IsSQLModel);
                GetProperty("HelperViewJoins").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

#endif
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
#if WINDOWS
        [DefaultValue(null)]
        [Category("Model definition"), DisplayName("Source"), Description("The source used to build the model."), Id(1, 1)]
        [TypeConverter(typeof(MetaSourceConverter))]
#endif
        public string SourceGUID { get; set; }

        protected string _connectionGUID = ReportSource.DefaultReportConnectionGUID;
        /// <summary>
        /// The connection used to build the model
        /// </summary>
#if WINDOWS
        [DefaultValue(ReportSource.DefaultReportConnectionGUID)]
        [DisplayName("Connection"), Description("The connection used to build the model."), Category("Model definition"), Id(2, 1)]
        [TypeConverter(typeof(SourceConnectionConverter))]
#endif
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
#if WINDOWS
        [Category("Model definition"), DisplayName("Common restrictions and values"), Description("List of common restrictions and values involved in the model. Common Restrictions or Values are defined in the SQL (Pre, Post, Table SQL, Where Clause, etc.) with the '{CommonRestriction_' or '{CommonValue_' keywords (e.g. {CommonRestriction_Amount} to create a common restriction named 'Amount')"), Id(3, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<ReportRestriction> CommonRestrictions { get; set; } = new List<ReportRestriction>();
        public bool ShouldSerializeCommonRestrictions() { return CommonRestrictions.Count > 0; }

        /// <summary>
        /// If set, restrictions and elements from the referenced model are inserted to the current model. This enables the sharing of restrictions and elements among different models. The position of the element inserted can be specified in the element property 'Insert position' in the referenced model. 
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [Category("Model definition"), DisplayName("Reference model"), Description("If set, restrictions and elements from the referenced model are inserted to the current model. This enables the sharing of restrictions and elements among different models. The position of the element inserted can be specified in the element property 'Insert position' in the referenced model."), Id(4, 1)]
        [TypeConverter(typeof(ReportModelConverter))]
#endif
        public string ReferenceModelGUID { get; set; }
        public bool ShouldSerializeReferenceModelGUID() { return !string.IsNullOrEmpty(ReferenceModelGUID); }

        /// <summary>
        /// Current reference model if any
        /// </summary>
        [XmlIgnore]
        public ReportModel ReferenceModel
        {
            get
            {
                if (string.IsNullOrEmpty(ReferenceModelGUID)) return null;
                return _report.Models.FirstOrDefault(i => i.GUID == ReferenceModelGUID);
            }
        }


        /// <summary>
        /// Optional Razor Script to modify the result table of the model just before the database load
        /// </summary>
#if WINDOWS
        [Category("Model definition"), DisplayName("Pre load script"), Description("Optional Razor Script to modify the result table of the model just before the database load."), Id(5, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
#endif
        public string PreLoadScript { get; set; }

        /// <summary>
        /// If not empty, overwrites default query script template used to generate the LINQ query
        /// </summary>
#if WINDOWS
        [Category("Model definition"), DisplayName("LINQ query script template"), Description("If not empty, overwrites default query script template used to generate the LINQ query."), Id(6, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
#endif
        public string LINQQueryScript { get; set; }

        /// <summary>
        /// The Razor Script used to load the data in the table. If empty, the load script defined in the master table is used.
        /// </summary>
#if WINDOWS
        [Category("Model definition"), DisplayName("Load script"), Description("The Razor Script used to load the data in the table. If empty, the LINQ Query generated is used."), Id(7, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
#endif
        public string LoadScript { get; set; }

        private bool _subModelsSetRestr = true;
        /// <summary>
        /// If true, restrictions and theirs values defined for the LINQ model are automatically copied to the sub-models.
        /// </summary>
#if WINDOWS
        [Category("Sub-Models generation"), DisplayName("Synchronize restrictions"), Description("If true, restrictions and theirs values defined for the LINQ model are automatically copied to the sub-models."), Id(1, 3)]
        [DefaultValue(true)]
#endif
        public bool SubModelsSetRestr
        {
            get
            {
                return _subModelsSetRestr;
            }
            set
            {
                bool updateModels = (Report != null && _subModelsSetRestr != value);
                _subModelsSetRestr = value;
                if (updateModels) BuildQuery(false, true);
            }
        }
        public bool ShouldSerializeSubModelsSetRestr() { return !_subModelsSetRestr; }


        private bool _subModelsSetAggr = true;
        /// <summary>
        /// If true, aggregates are copied to sub-models elements, otherwise the sub-models elements have no aggregate. This may impact the final performances and results (especially for Count or Average aggregates). 
        /// </summary>
#if WINDOWS
        [Category("Sub-Models generation"), DisplayName("Synchronize aggregates"), Description("If true, aggregates are copied to sub-models elements, otherwise the sub-models elements have no aggregate. This may impact the final performances and results (especially for Count or Average aggregates)."), Id(5, 3)]
        [DefaultValue(true)]
#endif
        public bool SubModelsSetAggr
        {
            get
            {
                return _subModelsSetAggr;
            }
            set
            {
                bool updateModels = (Report != null && _subModelsSetAggr != value);
                _subModelsSetAggr = value;
                if (updateModels) BuildQuery(false, true);
            }
        }
        public bool ShouldSerializeSubModelsSetAggr() { return !_subModelsSetAggr; }

        /// <summary>
        /// Optional Razor Script to modify the model after its generation
        /// </summary>
#if WINDOWS
        [Category("Model definition"), DisplayName("Final script"), Description("Optional Razor Script to modify the model after its generation."), Id(7, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
#endif
        public string FinalScript { get; set; }

        /// <summary>
        /// During the models generation, the models of the same Set Number are generated in parallel at the same time. The models with Set 1 are executed first at the same time, then models with Set 2, etc. This can be used if models depends on other models.
        /// </summary>
#if WINDOWS
        [Category("Model definition"), DisplayName("Execution set"), Description("During the models generation, the models of the same Set Number are generated in parallel at the same time. The models with Set 1 are executed first at the same time, then models with Set 2, etc. This can be used if models depends on other models."), Id(10, 1)]
        [DefaultValue(1)]
#endif
        public int ExecutionSet { get; set; } = 1;

        /// <summary>
        /// If true and several models have the same SQL or Script definiton, one result table is generated and shared for those models (Optimization).
        /// </summary>
#if WINDOWS
        [Category("Model definition"), DisplayName("Share result table"), Description("If true and several models have the same SQL or Script definiton, one result table is generated and shared for those models (Optimization)."), Id(11, 1)]
        [DefaultValue(true)]
#endif
        public bool ShareResultTable { get; set; } = true;
        public bool ShouldSerializeShareResultTable() { return !ShareResultTable; }

        /// <summary>
        /// If true, the query is printed in the report messages (for debug purpose).
        /// </summary>
#if WINDOWS
        [Category("Model definition"), DisplayName("Print query"), Description("If true, the LINQ or SQL Query is printed in the report messages (for debug purpose)."), Id(12, 1)]
        [DefaultValue(false)]
#endif
        public bool PrintQuery { get; set; } = false;
        public bool ShouldSerializePrintQuery() { return PrintQuery; }

        /// <summary>
        /// If true and the table has column values, the first line used for titles is generated in the table header
        /// </summary>
#if WINDOWS
        [Category("Model definition"), DisplayName("Show first header line"), Description("If true and the table has column values, the first line used for titles is generated in the table header."), Id(13, 1)]
        [DefaultValue(true)]
#endif
        public bool ShowFirstLine { get; set; } = true;
        public bool ShouldSerializeShowFirstLine() { return !ShowFirstLine; }


        /// <summary>
        /// Alias name used for the table defining the select
        /// </summary>
#if WINDOWS
        [Category("SQL Model Options"), DisplayName("Alias name"), Description("Alias name used for the table defining the select."), Id(1, 2)]
#endif
        public string Alias
        {
            get { return Table != null ? Table.Alias : Name; }
            set { if (Table != null) Table.Alias = value; }
        }
        public bool ShouldSerializeAlias() { return IsSQLModel; }

        /// <summary>
        /// If true, the column names of the source a kept when building the metadata columns
        /// </summary>
#if WINDOWS
        [Category("SQL Model Options"), DisplayName("Keep column names"), Description("If true, the column names of the source a kept when building the metadata columns."), Id(2, 2)]
        [DefaultValue(false)]
#endif
        public bool KeepColNames
        {
            get { return Table != null ? Table.KeepColumnNames : false; }
            set { if (Table != null) Table.KeepColumnNames = value; }
        }
        public bool ShouldSerializeKeepColNames() { return IsSQLModel; }

        /// <summary>
        /// If true, the raw source SQL is used to generate the result table instead of using a 'select * from (Source SQL) a' statement. In this case, aggregations, restrictions and custom SQL are not applied
        /// </summary>
#if WINDOWS
        [Category("SQL Model Options"), DisplayName("Use raw SQL"), Description("If true, the raw source SQL is used to generate the result table instead of using a 'select * from (Source SQL) a' statement. In this case, aggregations, restrictions and custom SQL are not applied."), Id(3, 2)]
        [DefaultValue(false)]
#endif
        public bool UseRawSQL { get; set; } = false;
        public bool ShouldSerializeUseRawSQL() { return IsSQLModel; }


        /// <summary>
        /// If true, SELECT DISTINCT clause clause is used by default for the model if no aggregate is defined
        /// </summary>
#if WINDOWS
        [Category("SQL"), DisplayName("Use SELECT Distinct"), Description("If true, SELECT DISTINCT clause is used by default for the model if no aggregate is defined."), Id(2, 3)]
        [DefaultValue(true)]
#endif
        public bool UseSelectDistinct { get; set; } = true;
        public bool ShouldSerializeUseSelectDistinct() { return !UseSelectDistinct; }

        /// <summary>
        /// Limit the number of records loaded for the model. A value of 0 means no limitation. The implementation at server side is only done for MS SQLServer, Oracle and MYSQL
        /// </summary>
#if WINDOWS
        [Category("SQL"), DisplayName("Maximum number of records"), Description("Limit the number of records loaded for the model. A value of 0 means no limitation. The implementation at server side is only done for MS SQLServer, Oracle and MYSQL."), Id(3, 3)]
        [DefaultValue(0)]
#endif
        public int MaxNumberOfRecords { get; set; } = 0;
        public bool ShouldSerializeMaxNumberOfRecords() { return MaxNumberOfRecords != 0; }

        string _sqlSelect;
        /// <summary>
        /// If not empty, overwrite the SELECT clause in the generated SQL statement (e.g 'SELECT TOP 10', 'SELECT')
        /// </summary>
#if WINDOWS
        [Category("SQL"), DisplayName("Select Clause"), Description("If not empty, overwrites the SELECT clause in the generated SQL statement (e.g 'SELECT TOP 10', 'SELECT')."), Id(4, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
#endif
        public string SqlSelect
        {
            get { return (_sqlSelect == DefaultClause) ? "" : _sqlSelect; }
            set { _sqlSelect = value; }
        }

        string _sqlFrom;
        /// <summary>
        /// If not empty, overwrite the FROM clause in the generated SQL statement
        /// </summary>
#if WINDOWS
        [Category("SQL"), DisplayName("From Clause"), Description("If not empty, overwrites the FROM clause in the generated SQL statement."), Id(5, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
#endif
        public string SqlFrom
        {
            get { return (_sqlFrom == DefaultClause) ? "" : _sqlFrom; }
            set { _sqlFrom = value; }
        }

        string _sqlGroupBy;
        /// <summary>
        /// If not empty, overwrite the GROUP BY clause in the generated SQL statement
        /// </summary>
#if WINDOWS
        [Category("SQL"), DisplayName("Group By Clause"), Description("If not empty, overwrites the GROUP BY clause in the generated SQL statement."), Id(6, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
#endif
        public string SqlGroupBy
        {
            get { return (_sqlGroupBy == DefaultClause) ? "" : _sqlGroupBy; }
            set { _sqlGroupBy = value; }
        }

        string _sqlOrderBy;
        /// <summary>
        /// If not empty, overwrite the ORDER BY clause in the generated SQL statement
        /// </summary>
#if WINDOWS
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [Category("SQL"), DisplayName("Order By Clause"), Description("If not empty, overwrites the ORDER BY clause in the generated SQL statement."), Id(7, 3)]
        [DefaultValue("")]
#endif
        public string SqlOrderBy
        {
            get { return (_sqlOrderBy == DefaultClause) ? "" : _sqlOrderBy; }
            set { _sqlOrderBy = value; }
        }

        string _sqlCTE;
        /// <summary>
        /// If not empty, overwrite the CTE (Common Table Expressions) clause in the generated SQL statement
        /// </summary>
#if WINDOWS
        [Category("SQL"), DisplayName("Common Table Expressions Clause"), Description("If not empty, overwrites the CTE (Common Table Expressions) clause in the generated SQL statement."), Id(8, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
#endif
        public string SqlCTE
        {
            get { return (_sqlSelect == DefaultClause) ? "" : _sqlCTE; }
            set { _sqlCTE = value; }
        }


        /// <summary>
        /// SQL Statement executed before the main query. The statement may contain Razor script if it starts with '@'.
        /// </summary>
#if WINDOWS
        [Category("SQL"), DisplayName("Pre SQL Statement"), Description("SQL Statement executed before the main query. The statement may contain Razor script if it starts with '@'."), Id(9, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
#endif
        public string PreSQL { get; set; }

        /// <summary>
        /// SQL Statement executed after the main query. The statement may contain Razor script if it starts with '@'.
        /// </summary>
#if WINDOWS
        [Category("SQL"), DisplayName("Post SQL Statement"), Description("SQL Statement executed after the main query. The statement may contain Razor script if it starts with '@'."), Id(10, 3)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
#endif
        public string PostSQL { get; set; }

        /// <summary>
        /// If true, errors occuring during the Pre or Post SQL statements are ignored and the execution continues
        /// </summary>
#if WINDOWS
        [Category("SQL"), DisplayName("Ignore Pre and Post SQL Errors"), Description("If true, errors occuring during the Pre or Post SQL statements are ignored and the execution continues."), Id(11, 3)]
        [DefaultValue(false)]
#endif
        public bool IgnorePrePostError { get; set; } = false;

        /// <summary>
        /// "Default Timeout in seconds for the SQL Statements executed to load the model. -1 means the use of the Timeout defined in the connection. 0 means no Timeout.
        /// </summary>
#if WINDOWS
        [DefaultValue(-1)]
        [Category("SQL"), DisplayName("Command Timeout"), Description("Default Timeout in seconds for the SQL Statements executed to load the model. -1 means the use of the Timeout defined in the connection. 0 means no Timeout."), Id(12, 3)]
#endif
        public int CommandTimeout { get; set; } = -1;
        public bool ShouldSerializeCommandTimeout() { return CommandTimeout != -1; }

        /// <summary>
        /// List of Joins used to perform the query and joins the tables involved. By default, all Joins available in the Data Source are used.
        /// </summary>
        public List<string> JoinsToUse { get; set; } = new List<string>();
        public bool ShouldSerializeJoinsToUse() { return JoinsToUse.Count > 0; }

        /// <summary>
        /// Helper to select Join preferences
        /// </summary>
#if WINDOWS
        [Category("Join preferences"), DisplayName("Joins to use"), Description("If specified, Joins used to perform the query and joins the tables involved. By default, all Joins available in the Data Source are used."), Id(2, 4)]
        [Editor(typeof(JoinsEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string JoinsToSelect
        {
            get { return "<Click to select joins>"; }
            set { } //keep set for modification handler
        }

        private int _joinHashcode = 0;
        /// <summary>
        /// If specified and if possible, force the first join used to choose the dynamic SQL joins path used to perform the query.
        /// </summary>
#if WINDOWS
        [Category("Join preferences"), DisplayName("Path Hashcode to use"), Description("If different from 0, the hashcode of the join to use for the model. Hascodes can be got by using the 'View joins evaluated' helper."), Id(4, 4)]
        [DefaultValue(0)]
#endif
        public int JoinHashcode
        {
            get { return _joinHashcode; }
            set { _joinHashcode = value; }
        }

        /// <summary>
        /// Timeout in milliseconds to set the maximum duration used to build the SQL (may be used if many joins are defined)
        /// </summary>
#if WINDOWS
        [DefaultValue(2000)]
        [Category("Join preferences"), DisplayName("Build timeout (ms)"), Description("Timeout in milliseconds to set the maximum duration used to build the SQL or LINQ Query (may be used if many joins are defined)."), Id(5, 4)]
#endif
        public int BuildTimeout { get; set; } = 2000;

        /// <summary>
        /// Helper to view joins evaluated for the model
        /// </summary>
#if WINDOWS
        [Category("Join preferences"), DisplayName("View joins evaluated"), Description("List all joins evaluated for the model. This may be used to understand if a join definition is missing in the source."), Id(10, 4)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
#endif
        public string HelperViewJoins
        {
            get { return "<Click to view the joins evaluated for the model>"; }
        }

        /// <summary>
        /// List of SQL Sub-models involved in a LINQ Model 
        /// </summary>
        public List<ReportModel> LINQSubModels { get; set; } = new List<ReportModel>();
        public bool ShouldSerializeLINQSubModels() { return LINQSubModels != null && LINQSubModels.Count > 0; }

        /// <summary>
        /// List of SQL Sub-tables involved in a LINQ Model 
        /// </summary>
        public List<MetaTable> LINQSubTables { get; set; } = new List<MetaTable>();
        public bool ShouldSerializeLINQSubTables() { return LINQSubTables != null && LINQSubTables.Count > 0; }

        /// <summary>
        /// Current report source
        /// </summary>
        [XmlIgnore]
        public ReportSource Source
        {
            get
            {
                if (_report.Sources.Count == 0) throw new Exception("This report has no source defined");

                ReportSource result = _report.Sources.FirstOrDefault(i => i.GUID == SourceGUID);
                if (result == null)
                {
                    result = _report.Sources.FirstOrDefault(i => i.MetaSourceGUID == SourceGUID);
                    if (result == null)
                    {
                        result = _report.Sources[0];
                        SourceGUID = result.GUID;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Master model if the model is a sub-model for LINQ source
        /// </summary>
        [XmlIgnore]
        public ReportModel MasterModel;

        /// <summary>
        /// True if the model has series defined
        /// </summary>
        [XmlIgnore]
        public bool HasSerie
        {
            get
            {
                return Elements.Exists(i => i.IsSerie && i.PivotPosition == PivotPosition.Data);
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
        /// True if the model has elements with EmptyRepeated
        /// </summary>
        [XmlIgnore]
        public bool HasEmptyRepeated
        {
            get
            {
                return Elements.Exists(i => i.PivotPosition == PivotPosition.Row && i.EmptyRepeated);
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
                return Elements.Exists(i => i.Nvd3Serie != NVD3SerieDefinition.None && i.PivotPosition == PivotPosition.Data);
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
                return Elements.Exists(i => i.ChartJSSerie != ChartJSSerieDefinition.None && i.PivotPosition == PivotPosition.Data);
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
                return Elements.Exists(i => i.PlotlySerie != PlotlySerieDefinition.None && i.PivotPosition == PivotPosition.Data);
            }
        }

        /// <summary>
        /// True if the model has a Chart ScottPlot serie
        /// </summary>
        [XmlIgnore]
        public bool HasScottPlotSerie
        {
            get
            {
                return Elements.Exists(i => i.ScottPlotSerie != ScottPlotSerieDefinition.None && i.PivotPosition == PivotPosition.Data);
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
        public bool ShouldSerializeAggregateRestriction() { return !string.IsNullOrEmpty(_aggregateRestriction); }

        /// <summary>
        /// List of aggregate restrictions of the model
        /// </summary>
        public List<ReportRestriction> AggregateRestrictions { get; set; } = new List<ReportRestriction>();
        public bool ShouldSerializeAggregateRestrictions() { return AggregateRestrictions.Count > 0; }

        /// <summary>
        /// Table definition for a SQL Model
        /// </summary>
#if WINDOWS
        [Browsable(false)]
#endif
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

        /// <summary>
        /// True is it is a Model based on a LINQ source
        /// </summary>
        public bool IsLINQ
        {
            get { return Table == null && Source.IsNoSQL; }
        }

        /// <summary>
        /// True is it is a Sub-Model used for a LINQ Query
        /// </summary>
        public bool IsSubModel
        {
            get { return MasterModel != null; }
        }

        /// <summary>
        /// Return true if the restrictions are standards (simple list with AND)
        /// </summary>
        public bool HasStandardRestrictions
        {
            get
            {
                var restrictionText = Restriction;
                foreach (var restr in Restrictions)
                {
                    restrictionText = restrictionText.Replace("[" + restr.GUID + "]", "");
                }
                restrictionText = restrictionText.Replace((IsLINQ ? "&&" : "AND"), "").Trim();
                return restrictionText == "";
            }
        }

        /// <summary>
        /// Return true if the aggregate restrictions are standards (simple list with AND)
        /// </summary>
        public bool HasStandardAggregateRestrictions
        {
            get
            {
                var restrictionText = AggregateRestriction;
                foreach (var restr in AggregateRestrictions)
                {
                    restrictionText = restrictionText.Replace("[" + restr.GUID + "]", "");
                }
                restrictionText = restrictionText.Replace((IsLINQ ? "&&" : "AND"), "").Trim();
                return restrictionText == "";
            }
        }

        /// <summary>
        /// SELECT Sql used for the model
        /// </summary>
        [XmlIgnore]
        public string Sql { get; set; }
        public bool ShouldSerializeSql() { return IsSQLModel; }

        /// <summary>
        /// LINQ SELECT used for the model
        /// </summary>
        [XmlIgnore]
        public string LINQSelect;

        /// <summary>
        /// Default LINQ Load Script used for the model
        /// </summary>
        [XmlIgnore]
        public string LINQLoadScript
        {
            get
            {
                return string.Format(@"@using System.Data
@{{
ReportModel model = Model;

//Query
var query =
{0};

//LINQ Query Script
{1}

model.ResultTable = query2.CopyToDataTable2();
}}
", LINQSelect, string.IsNullOrEmpty(LINQQueryScript) ? DefaultLINQScriptTemplate : LINQQueryScript);
            }
        }


        /// <summary>
        /// List of tables involved in the model
        /// </summary>
        [XmlIgnore]
        public List<MetaTable> FromTables { get; private set; }

        private List<MetaTable> AdditionalFromTables = new List<MetaTable>();

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
        /// Result DataTable got from the SQL query with the original column names and without hidden columns
        /// </summary>
        [XmlIgnore]
        public DataTable ResultTableTranslated
        {
            get
            {
                DataTable result = null;
                if (ResultTable != null)
                {
                    int cnt = 0;
                    result = ResultTable.Copy();
                    foreach (var el in Elements)
                    {
                        if (el.IsForNavigation) result.Columns.Remove(el.SQLColumnName);
                        else
                        {
                            var col = result.Columns[el.SQLColumnName];
                            if (col != null)
                            {
                                var colNames = new List<string>();
                                foreach (DataColumn c in result.Columns) colNames.Add(c.ColumnName);

                                col.ColumnName = Helper.GetUniqueName(el.DisplayNameElTranslated, colNames);
                                col.SetOrdinal(cnt++);
                            }
                        }
                    }
                    result.TableName = Name;
                }
                return result;
            }
        }
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
        /// True if the source result table has been loaded
        /// </summary>
        [XmlIgnore]
        public bool ExecResultTableLoaded = false;

        /// <summary>
        /// True if the result pages have been built
        /// </summary>
        [XmlIgnore]
        public bool ExecResultPagesBuilt = false;

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
        /// Check ScottPlot and set the ExecScottPlotChartType property
        /// </summary>
        public void CheckScottPlotChartIntegrity()
        {
            bool hasValueAxis = false;
            foreach (var element in Elements.Where(i => (i.PivotPosition == PivotPosition.Row || i.PivotPosition == PivotPosition.Column) && i.IsSerie))
            {
                if (element.AxisUseValues)
                {
                    if (element.IsNumeric || element.IsDateTime)
                    {
                        hasValueAxis = true;
                        break;
                    }
                }
            }

            //Do not mix scatter and bar if the axis is used as value with 2 different axis
            if (Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ScottPlotSerie == ScottPlotSerieDefinition.Bar)
                && Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.ScottPlotSerie == ScottPlotSerieDefinition.Scatter)
                && hasValueAxis
                )
            {
                Report.LogMessage("Chart ScottPlot: Setting all elements to scatter");
                foreach (var element in Elements.Where(i => i.PivotPosition == PivotPosition.Data && i.ScottPlotSerie == ScottPlotSerieDefinition.Bar)) element.ScottPlotSerie = ScottPlotSerieDefinition.Scatter;
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
        /// List of all restrictions of the model (including aggregate and common restrictions)
        /// </summary>
        [XmlIgnore]
        public List<ReportRestriction> AllExecutionRestrictions
        {
            get
            {
                return ExecutionRestrictions.Union(ExecutionAggregateRestrictions).Union(ExecutionCommonRestrictions).ToList();
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
        /// Custom Tag the can be used at execution time to store any object
        /// </summary>
        [XmlIgnore]
        public object Tag2;

        /// <summary>
        /// Custom Tag the can be used at execution time to store any object
        /// </summary>
        [XmlIgnore]
        public object Tag3;

        /// <summary>
        /// Custom string serialized (saved with the report definition)
        /// </summary>
        public string SerializedTag { get; set; }

        /// <summary>
        /// Custom string serialized (saved with the report definition)
        /// </summary>
        public string SerializedTag2 { get; set; }

        /// <summary>
        /// Init all model references: Elements, Restrictions, etc.
        /// </summary>
        public void InitReferences()
        {
            if (Source.MetaData == null) return;

            foreach (var subModel in LINQSubModels)
            {
                subModel.MasterModel = this;
                subModel.Report = Report;
                subModel.InitReferences();
            }

            foreach (var subTable in LINQSubTables)
            {
                subTable.Model = this;
                subTable.Source = Source;
                subTable.InitParameters();
            }

            foreach (var element in Elements)
            {
                element.SetSourceReference(Source);
                element.Report = Report;
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
                Helper.WriteLogException("InitCommonRestrictions", ex);
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
                else if (AggregateRestrictions[i].MetaColumn == null)
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
            foreach (ReportElement element in Elements) element.SQLColumnName = UseRawSQL ? element.MetaColumn.Name : string.Format("C{0}", colIndex++);
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
                        addHiddenElement(guid);
                    }
                }
            }
        }

        void addHiddenElement(string MetaColumnGUID)
        {
            if (!Elements.Exists(i => i.MetaColumnGUID == MetaColumnGUID))
            {
                //Add the element
                ReportElement element = ReportElement.Create();
                element.Source = Source;
                element.Report = Report;
                element.Model = this;
                element.MetaColumnGUID = MetaColumnGUID;
                element.PivotPosition = PivotPosition.Hidden;
                element.IsForNavigation = true;
                element.SortOrder = ReportElement.kNoSortKeyword;
                Elements.Add(element);
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
        public StringBuilder JoinLogs = null;

        void filterLINQFromTables()
        {
            //For LINQ, keep only SQL tables having joins defined...
            if (IsLINQ)
            {
                foreach (var table in FromTables.Where(i => i.IsSQL && !Source.MetaData.Joins.Exists(j => j.LeftTableGUID == i.GUID || j.RightTableGUID == i.GUID)).ToList())
                {
                    //but we need at least one table per source
                    if (FromTables.Exists(i => i != table && i.Source.GUID == table.Source.GUID)) FromTables.Remove(table);
                }
            }
        }



        /// <summary>
        /// Initializes the model its reference model
        /// </summary>
        public void InitFromReferenceModel()
        {
            var refModel = ReferenceModel;
            if (refModel != null)
            {
                //First add elements to add at the end
                foreach (var element in refModel.Elements.Where(i => i.InsertPosition == 0))
                {
                    if (!Elements.Exists(i => element.PivotPosition == i.PivotPosition && element.MetaColumnGUID == i.MetaColumnGUID))
                    {
                        Elements.Add(element);
                    }
                }

                //Then elements with specified positive position
                foreach (var element in refModel.Elements.Where(i => i.InsertPosition > 0))
                {
                    if (!Elements.Exists(i => element.PivotPosition == i.PivotPosition && element.MetaColumnGUID == i.MetaColumnGUID))
                    {
                        //use insert position
                        var pos = element.InsertPosition - 1;
                        pos = Math.Max(pos, 0);
                        if (pos >= Elements.Count) Elements.Add(element);
                        else Elements.Insert(pos, element);
                    }
                }

                //Then elements with specified negative position
                foreach (var element in refModel.Elements.Where(i => i.InsertPosition < 0))
                {
                    if (!Elements.Exists(i => element.PivotPosition == i.PivotPosition && element.MetaColumnGUID == i.MetaColumnGUID))
                    {
                        //use insert position
                        var pos = Elements.Count + element.InsertPosition - 2;
                        pos = Math.Max(pos, 0);
                        if (pos >= Elements.Count) Elements.Add(element);
                        else Elements.Insert(pos, element);
                    }
                }

                if (HasStandardRestrictions)
                {
                    foreach (var restriction in refModel.Restrictions)
                    {
                        if (!Restrictions.Exists(i => restriction.MetaColumnGUID == i.MetaColumnGUID))
                        {
                            Restrictions.Add(restriction);
                            if (!string.IsNullOrEmpty(Restriction)) Restriction = Restriction.TrimEnd() + "\r\n" + (IsLINQ ? "&&" : "AND") + " ";
                            Restriction += restriction.Pattern;
                        }
                    }
                }

                if (HasStandardAggregateRestrictions)
                {
                    foreach (var restriction in refModel.AggregateRestrictions)
                    {
                        if (!AggregateRestrictions.Exists(i => restriction.MetaColumnGUID == i.MetaColumnGUID))
                        {
                            AggregateRestrictions.Add(restriction);
                            if (!string.IsNullOrEmpty(AggregateRestriction)) AggregateRestriction = AggregateRestriction.TrimEnd() + "\r\n" + (IsLINQ ? "&&" : "AND") + " ";
                            AggregateRestriction += restriction.Pattern;
                        }
                    }
                }

                InitReferences();
            }
        }


        /// <summary>
        /// Build the SQL for the model
        /// </summary>
        public void BuildQuery(bool forConversion = false, bool forceRestrictionsTables = false)
        {
            try
            {
                ExecutionError = "";
                Sql = "";

                if (IsSQLModel && UseRawSQL)
                {
                    Sql = Table.Sql;
                    Sql = ParseCommonRestrictions(Sql);
                    return;
                }

                if (Source.MetaData == null) return;

                if (!forConversion) AddSubReportsElements();

                InitReferences();

                execSelectClause = new StringBuilder();
                execCTEClause = "";
                execFromClause = new StringBuilder();
                execWhereClause = new StringBuilder(Restriction.Trim());
                var execGroupByLINQ = new StringBuilder();
                execGroupByClause = new StringBuilder();
                execOrderByNameClause = new StringBuilder();
                execHavingClause = new StringBuilder(AggregateRestriction.Trim());
                execOrderByClause = new StringBuilder();

                //build restriction
                RestrictionText = "";
                foreach (ReportRestriction restriction in ExecutionRestrictions)
                {
                    execWhereClause = execWhereClause.Replace("[" + restriction.GUID + "]", IsLINQ ? restriction.LINQText : restriction.SQLText);
                }
                if (Report.CheckingExecution && !IsLINQ)
                {
                    if (execWhereClause.ToString().Trim().Length == 0) execWhereClause.Append("1=0");
                    else execWhereClause.Append(" AND (1=0)");
                }

                foreach (ReportRestriction restriction in ExecutionAggregateRestrictions)
                {
                    execHavingClause = execHavingClause.Replace("[" + restriction.GUID + "]", IsLINQ ? restriction.LINQText : restriction.SQLText);
                }

                foreach (ReportRestriction restriction in ExecutionRestrictions.Union(ExecutionAggregateRestrictions).Union(ExecutionCommonRestrictions).OrderBy(i => i.DisplayOrderRE))
                {
                    if (restriction.HasValue) Helper.AddValue(ref RestrictionText, "\r\n", restriction.DisplayText);
                }

                bool noGroupBy = GetElements(PivotPosition.Data).Count() == 0 && Elements.Count(i => i.IsAggregateEl) == 0 && execHavingClause.Length == 0;

                //build select
                FromTables = new List<MetaTable>();
                if (Elements.Count > 0)
                {
                    List<string> selectColumns = new List<string>();
                    List<string> groupByColumns = new List<string>();
                    if (!forConversion) SetColumnsName();
                    foreach (ReportElement element in Elements)
                    {
                        string sqlColumn = string.Format("{0} AS {1}", element.SQLColumn, element.SQLColumnName);
                        if (IsLINQ)
                        {
                            sqlColumn = !noGroupBy && element.IsNotAggregate ? string.Format("{0}=g.Key.{0}", element.SQLColumnName) : string.Format("{0}={1}", element.SQLColumnName, element.LINQColumnName);
                        }

                        if (!selectColumns.Contains(sqlColumn))
                        {
                            Helper.AddValue(ref execSelectClause, ",\r\n", "  " + sqlColumn);
                            selectColumns.Add(sqlColumn);
                        }

                        MetaTable table = element.MetaColumn.MetaTable;
                        if (table != null && !FromTables.Contains(table)) FromTables.Add(table);

                        if (!noGroupBy && element.IsNotAggregate)
                        {
                            if (groupByColumns.Contains(element.SQLColumn) && !IsLINQ) continue;

                            if (IsLINQ) Helper.AddValue(ref execGroupByClause, ",\r\n", string.Format("  {0}={1}", element.SQLColumnName, element.LINQColumnName));
                            else Helper.AddValue(ref execGroupByClause, ", ", element.SQLColumn);

                            groupByColumns.Add(element.SQLColumn);
                        }
                    }

                    foreach (ReportRestriction restriction in ExecutionRestrictions.Union(ExecutionAggregateRestrictions))
                    {
                        MetaTable table = restriction.MetaColumn.MetaTable;
                        if (table != null && !FromTables.Contains(table) && (restriction.HasValue || forceRestrictionsTables) && restriction.Operator != Operator.ValueOnly) FromTables.Add(table);
                    }

                    //Clear group by clause if not necessary
                    if (noGroupBy) execGroupByClause = new StringBuilder();

                    List<string> orderColumns = new List<string>();
                    UpdateFinalSortOrders();
                    buildOrderClause(GetElements(PivotPosition.Page), orderColumns, ref execOrderByClause, ref execOrderByNameClause, noGroupBy);
                    buildOrderClause(GetElements(PivotPosition.Row), orderColumns, ref execOrderByClause, ref execOrderByNameClause, noGroupBy);
                    buildOrderClause(GetElements(PivotPosition.Column), orderColumns, ref execOrderByClause, ref execOrderByNameClause, noGroupBy);
                    buildOrderClause(GetElements(PivotPosition.Data), orderColumns, ref execOrderByClause, ref execOrderByNameClause, noGroupBy);

                    bool joinsOK = buildFromClause();
                    if (IsLINQ)
                    {
                        //For LINQ, keep only SQL tables having joins defined...
                        filterLINQFromTables();
                        //and build group by tables
                        foreach (var table in FromTables) Helper.AddValue(ref execGroupByLINQ, ",", table.LINQResultName);
                    }

                    if (!joinsOK && IsLINQ)
                    {
                        //Try to add SQL tables joins from the same source...
                        AdditionalFromTables = new List<MetaTable>();
                        foreach (var join in Source.MetaData.Joins)
                        {
                            //Filter in joins to use here
                            if (JoinsToUse.Count > 0 && !JoinsToUse.Contains(join.GUID)) continue;

                            if (!FromTables.Contains(join.LeftTable) && join.LeftTable.IsSQL && FromTables.Exists(i => i.LINQSourceGUID == join.LeftTable.LINQSourceGUID))
                            {
                                AdditionalFromTables.Add(join.LeftTable);
                            }
                            if (!FromTables.Contains(join.RightTable) && join.RightTable.IsSQL && FromTables.Exists(i => i.LINQSourceGUID == join.RightTable.LINQSourceGUID))
                            {
                                AdditionalFromTables.Add(join.RightTable);
                            }
                        }

                        //Try to join again...
                        if (AdditionalFromTables.Count > 0)
                        {
                            joinsOK = buildFromClause();
                        }
                    }
                    if (!joinsOK)
                    {
                        var errMessage = "Unable to link all elements using the joins defined...\r\nAdd Joins to your Data Source\r\nOR remove elements or restrictions in your model\r\nOR add relevant elements or restrictions in your model.";
                        if (JoinLogs != null) JoinLogs.AppendLine("\r\n" + errMessage);
                        throw new Exception(errMessage);
                    }

                    if (!IsLINQ)
                    {
                        //Get CTE first
                        var topClause = (Connection.ConnectionType == ConnectionType.MSSQLServer && MaxNumberOfRecords > 0) ? $" TOP {MaxNumberOfRecords}" : "";
                        var distinctClause = (UseSelectDistinct && execGroupByClause.Length == 0) ? " DISTINCT" : "";
                        execSelect = $"SELECT{distinctClause}{topClause}\r\n";
                        execSelect = !string.IsNullOrEmpty(SqlSelect) ? SqlSelect : execSelect;
                        Sql = !string.IsNullOrEmpty(SqlCTE) ? SqlCTE : execCTEClause;
                        Sql += execSelect;
                        Sql += string.Format("{0}\r\n", execSelectClause);
                        Sql += !string.IsNullOrEmpty(SqlFrom) ? SqlFrom : string.Format("FROM {0}", execFromClause);

                        var whereClause = execWhereClause.ToString();
                        //Limit Max number of records for Oracle
                        if (Connection.ConnectionType == ConnectionType.Oracle && MaxNumberOfRecords > 0)
                        {
                            whereClause = $"rownum <= {MaxNumberOfRecords}";
                            if (execWhereClause.Length > 0) whereClause += $" AND ({execWhereClause})";
                        }

                        if (whereClause.Length > 0) Sql += string.Format("WHERE {0}\r\n", whereClause);

                        if (execGroupByClause.Length > 0 || !string.IsNullOrEmpty(SqlGroupBy)) Sql += (!string.IsNullOrEmpty(SqlGroupBy) ? SqlGroupBy : string.Format("GROUP BY {0}", execGroupByClause)) + "\r\n";
                        if (execHavingClause.Length > 0) Sql += string.Format("HAVING {0}\r\n", execHavingClause);
                        if (!forConversion && (execOrderByClause.Length > 0 || !string.IsNullOrEmpty(SqlOrderBy))) Sql += (!string.IsNullOrEmpty(SqlOrderBy) ? SqlOrderBy : string.Format("ORDER BY {0}", execOrderByClause)) + "\r\n";

                        //Limit Max number of records for MySQL
                        if ((Connection.ConnectionType == ConnectionType.MySQL || Connection.ConnectionType == ConnectionType.PostgreSQL) && MaxNumberOfRecords > 0) Sql += $"LIMIT {MaxNumberOfRecords}\r\n";

                        //Finally inject common restriction values
                        if (!forConversion) Sql = ParseCommonRestrictions(Sql);
                    }
                    else
                    {
                        initSubModelsAndTables();

                        LINQSelect = !string.IsNullOrEmpty(SqlFrom) ? SqlFrom : string.Format("from {0}", execFromClause);
                        if (execWhereClause.Length > 0) LINQSelect += string.Format("\r\nwhere\r\n{0}\r\n", execWhereClause);

                        if (!noGroupBy)
                        {
                            LINQSelect += string.Format("\r\ngroup new {{ {0} }} by ", execGroupByLINQ);
                            if (execGroupByClause.Length == 0) LINQSelect += "1"; //Case no dimension for aggregate
                            else if (execGroupByClause.Length > 0) LINQSelect += string.Format("new {{\r\n{0}\r\n}}", execGroupByClause); //With dimensions
                            LINQSelect += " into g\r\n";
                        }

                        if (execHavingClause.Length > 0) LINQSelect += string.Format("\r\nwhere\r\n{0}\r\n", execHavingClause);
                        if (execOrderByClause.Length > 0) LINQSelect += string.Format("\r\norderby {0}\r\n", execOrderByClause);
                        LINQSelect += string.Format("\r\nselect new {{\r\n{0}\r\n}}", execSelectClause);
                    }
                }
                else
                {
                    if (IsLINQ) initSubModelsAndTables();
                }
            }
            catch (TemplateCompilationException ex)
            {
                LINQSelect = "";
                Sql = "";
                ExecutionError = string.Format("Error when building the query:\r\n{0}", Helper.GetExceptionMessage(ex));
            }
            catch (Exception ex)
            {
                LINQSelect = "";
                Sql = "";
                ExecutionError = string.Format("Error when building the query:\r\n{0}", ex.Message);
            }
        }


        bool buildFromClause()
        {
            //Handle additional from tables for LINQ joins
            if (AdditionalFromTables.Count > 0)
            {
                foreach (var newTable in AdditionalFromTables)
                {
                    if (!FromTables.Contains(newTable)) FromTables.Add(newTable);
                }
                filterLINQFromTables();
                AdditionalFromTables.Clear();
            }

            //For LINQ, remove duplicate tables given the same Data Source                    
            if (IsLINQ)
            {
                var tables = new List<MetaTable>();
                foreach (var table in FromTables)
                {
                    if (!tables.Exists(i => i.LINQResultName == table.LINQResultName)) tables.Add(table);
                }
                FromTables = tables;
            }

            List<MetaTable> extraWhereTables = FromTables.Where(i => !string.IsNullOrEmpty(i.WhereSQL)).ToList();
            ExecTableJoins = new List<MetaJoin>();
            if (FromTables.Count == 1)
            {
                if (!IsLINQ)
                {
                    string CTE = "", name = "";
                    FromTables[0].GetExecSQLName(ref CTE, ref name);
                    execCTEClause = Helper.AddCTE(execCTEClause, CTE);
                    execFromClause.Append(name + "\r\n");
                }
                else
                {
                    execFromClause.Append(FromTables[0].LINQExpressionName + "\r\n");
                }
                if (JoinLogs != null) JoinLogs.AppendLine("Only one table: No join required.");
            }
            else
            {
                //multiple tables, find joins...
                List<MetaTable> tablesToUse = FromTables.ToList();
                List<JoinPath> resultPaths = new List<JoinPath>();
                JoinPath bestPath = null, hashcodePath = null;

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
                        if (IsLINQ)
                        {
                            //invert also the clause using equals
                            var clauses = join.Clause.Split(" equals ");
                            if (clauses.Length == 2)
                            {
                                newJoin.Clause = clauses[1] + " equals " + clauses[0];
                            }
                        }

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
                _bestJoinsCount = joinsToUse.Count + 1;

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
                Debug.WriteLine("Direct Join: {0:F0}ms {1} {2}", (DateTime.Now - _buildTimer).TotalMilliseconds, resultPaths.Count, _directCount);

                if (JoinLogs != null)
                {
                    JoinLogs.AppendFormat("Time elapsed after Direct Joins: {0:F0} ms\r\n\r\n", (DateTime.Now - _buildTimer).TotalMilliseconds);
                    JoinLogs.AppendLine("DIRECT Joins found by priority order (The first one may be used if all tables are joined, maximum 100 are shown):\r\n");
                    int index = 1;
                    foreach (var path in resultPaths.OrderBy(i => i.tablesToUse.Count).ThenBy(i => i.joins.Count).Take(100))
                    {
                        JoinLogs.AppendFormat("Direct Join {0}: ", index++);
                        path.print(JoinLogs);
                    }
                }


                if (bestPath == null && JoinHashcode != 0) hashcodePath = resultPaths.FirstOrDefault(i => i.tablesToUse.Count == 0 && i.hash == JoinHashcode);
                //Choose the path having all tables, then best priority...
                if (bestPath == null) bestPath = resultPaths.Where(i => i.tablesToUse.Count == 0).OrderBy(i => i.priority).FirstOrDefault();

                bool checkIndirectJoin = false;
                if (hashcodePath != null)
                {
                    if (JoinLogs != null) JoinLogs.AppendFormat($"Path chosen using the Hashcode {JoinHashcode}.\r\n");
                    bestPath = hashcodePath;
                }
                else if (bestPath == null) checkIndirectJoin = true;
                else if (bestPath.joins.Count > tablesToUse.Count - 1) checkIndirectJoin = true;
                else if (bestPath.joins.Exists(i => i.JoinType == JoinType.LeftOuter || i.JoinType == JoinType.RightOuter)) checkIndirectJoin = true;
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
                                    newPath.startTable = newPath.joins.First().LeftTable;
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
                                if (JoinLogs != null) JoinLogs.AppendFormat("Exiting the joins search after {0:F0} milliseconds\r\n", (DateTime.Now - _buildTimer).TotalMilliseconds);
                                break;
                            }
                        }
                    }

                    Debug.WriteLine("Indirect Joins: {0:F0}ms {1} {2}", (DateTime.Now - _buildTimer).TotalMilliseconds, resultPaths2.Count, _indirectCount);

                    if (JoinLogs != null)
                    {
                        JoinLogs.AppendFormat("\r\nTime elapsed after Indirect Joins: {0:F0} ms\r\n\r\n", (DateTime.Now - _buildTimer).TotalMilliseconds);
                        JoinLogs.AppendLine("INDIRECT Joins found by priority order (The first one may be used if all tables are joined, maximum 100 are shown):\r\n");
                        int index = 1;
                        foreach (var path in resultPaths2.OrderBy(i => i.tablesToUse.Count).ThenBy(i => i.joins.Count).Take(100))
                        {
                            JoinLogs.AppendFormat("Indirect Join {0}: ", index++);
                            path.print(JoinLogs);
                        }
                    }

                    JoinPath bestPath2 = null;
                    if (JoinHashcode != 0) hashcodePath = resultPaths2.FirstOrDefault(i => i.tablesToUse.Count == 0 && i.hash == JoinHashcode);
                    if (hashcodePath != null)
                    {
                        if (JoinLogs != null) JoinLogs.AppendFormat($"Path chosen using the Hashcode {JoinHashcode}.\r\n");
                        bestPath = hashcodePath;
                    }
                    else
                    {
                        if (bestPath2 == null) bestPath2 = resultPaths2.Where(i => i.tablesToUse.Count == 0).OrderBy(i => i.priority).FirstOrDefault();
                        if (bestPath != null && bestPath2 != null)
                        {
                            //Choose here between direct best path or indirect best path
                            if (bestPath2.joins.Count < bestPath.joins.Count) bestPath = bestPath2;
                            else if (bestPath2.joins.Count == bestPath.joins.Count)
                            {
                                //here we choose the one which does not mix left outer and right outer
                                if (bestPath.hasLeftAndRightJoins && !bestPath2.hasLeftAndRightJoins)
                                {
                                    bestPath = bestPath2;
                                }
                            }
                        }
                        else if (bestPath == null)
                        {
                            bestPath = bestPath2;
                        }
                    }
                }

                if (JoinLogs != null && bestPath != null)
                {
                    JoinLogs.AppendFormat("\r\nTime elapsed: {0:F0} ms\r\n", (DateTime.Now - _buildTimer).TotalMilliseconds);
                    JoinLogs.AppendLine("\r\nAND THE WINNER IS:");
                    bestPath.print(JoinLogs);
                }

                //Handle the best path
                if (bestPath == null)
                {
                    //No link...
                    return false;
                }

                ExecTableJoins = bestPath.joins;
                if (bestPath.joins.Count == 0)
                {
                    //only one table
                    if (!IsLINQ)
                    {
                        string CTE = "", name = "";
                        bestPath.currentTable.GetExecSQLName(ref CTE, ref name);
                        execCTEClause = Helper.AddCTE(execCTEClause, CTE);
                        execFromClause.Append(name + "\r\n");
                    }
                    else
                    {
                        execFromClause.Append(bestPath.currentTable.LINQExpressionName + "\r\n");
                    }

                    if (JoinLogs != null) JoinLogs.AppendLine("Only one table: No join required.");
                }
                else
                {
                    string joinSql = null;
                    List<MetaTable> tablesUsed = new List<MetaTable>();
                    var joins = bestPath.joins;
                    //Reverse join orders for LINQ
                    if (IsLINQ) joins.Reverse();

                    //nested joins
                    for (int i = joins.Count - 1; i >= 0; i--)
                    {
                        MetaJoin join = joins[i];
                        if (string.IsNullOrEmpty(joinSql))
                        {
                            if (!IsLINQ)
                            {
                                string CTE2 = "", name2 = "";
                                join.RightTable.GetExecSQLName(ref CTE2, ref name2);
                                execCTEClause = Helper.AddCTE(execCTEClause, CTE2);
                                joinSql = name2;
                                tablesUsed.Add(join.RightTable);
                            }
                            else
                            {
                                joinSql = join.LeftTable.LINQExpressionName + "\r\n";
                            }
                        }

                        //check if tables are already in the join
                        var tableToJoin = join.LeftTable;
                        if (tablesUsed.Contains(tableToJoin))
                        {
                            //use right join
                            tableToJoin = join.RightTable;
                        }
                        if (tablesUsed.Contains(tableToJoin)) continue;

                        string joinClause = join.Clause.Trim();
                        //For outer join, add the extra restriction in the ON clause
                        MetaTable extraWhereTable = null;
                        if (join.JoinType == JoinType.LeftOuter && !string.IsNullOrEmpty(join.RightTable.WhereSQL)) extraWhereTable = join.RightTable;
                        else if (join.JoinType == JoinType.RightOuter && !string.IsNullOrWhiteSpace(join.LeftTable.WhereSQL)) extraWhereTable = join.LeftTable;
                        else if (!string.IsNullOrWhiteSpace(tableToJoin.WhereSQL) && !extraWhereTables.Contains(tableToJoin))
                        {
                            extraWhereTables.Add(tableToJoin);
                        }

                        if (extraWhereTable != null)
                        {
                            string where = RazorHelper.CompileExecute(extraWhereTable.WhereSQL, extraWhereTable);
                            if (!string.IsNullOrEmpty(where)) joinClause += " AND " + where;
                            extraWhereTables.Remove(extraWhereTable);
                        }

                        //finally build the clause
                        if (!IsLINQ)
                        {
                            string CTE = "", name = "";
                            tableToJoin.GetExecSQLName(ref CTE, ref name);
                            execCTEClause = Helper.AddCTE(execCTEClause, CTE);

                            if (join.JoinType != JoinType.Cross)
                            {
                                if (tableToJoin == join.RightTable) joinSql = $"({joinSql}\r\n{join.SQLJoinType} {name}\r\n    ON {joinClause})";
                                else joinSql = $"({name}\r\n{join.SQLJoinType} {joinSql}\r\n    ON {joinClause})";
                            }
                            else joinSql = string.Format("\r\n({0} {1} {2})\r\n", name, join.SQLJoinType, joinSql);
                        }
                        else
                        {
                            joinSql = string.Format("{0}join {1} on\r\n{2}\r\n", joinSql, join.RightTable.LINQExpressionName, joinClause);
                        }

                        tablesUsed.Add(tableToJoin);
                    }
                    execFromClause = new StringBuilder(joinSql + "\r\n");
                }
                if (JoinLogs != null)
                {
                    JoinLogs.Append("\r\n" + (IsLINQ ? "LINQ" : "SQL") + " Generated:\r\n");
                    JoinLogs.Append(execFromClause.ToString());
                }
            }

            //add extra where clause
            if (!IsLINQ)
            {
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
            }

            return true;
        }

        class JoinPath
        {
            public MetaTable currentTable = null;
            public MetaTable startTable = null;
            public MetaTable finalTable = null;
            public List<MetaJoin> joins = new List<MetaJoin>();
            public List<MetaTable> tablesToUse;
            public Dictionary<string, MetaJoin[]> joinsToUse;
            public bool hasLeftAndRightJoins
            {
                get
                {
                    return joins.Exists(i => i.JoinType == JoinType.RightOuter) && joins.Exists(i => i.JoinType == JoinType.LeftOuter);
                }
            }
            public int priority
            {
                get
                {
                    //priority first for path having less joins involved and no mix left/right
                    return 100 * joins.Count + (hasLeftAndRightJoins ? 1 : 0);
                }
            }
            public int hash
            {
                get
                {
                    return print().GetHashCode();
                }
            }

            public string print()
            {
                var str = new StringBuilder();
                for (int i = 0; i < joins.Count; i++)
                {
                    var join = joins[i];
                    if (i == 0) str.Append(join.LeftTable.DisplayName + "->");
                    if (i > 0 && join.LeftTableGUID != joins[i - 1].RightTableGUID)
                    {
                        //Break
                        str.Append("\r\n" + join.LeftTable.DisplayName + "->");
                    }
                    str.Append(join.RightTable.DisplayName);
                    if (i < joins.Count - 1 && join.RightTableGUID == joins[i + 1].LeftTableGUID)
                    {
                        str.Append("->");
                    }
                }
                return str.ToString();
            }

            public void print(StringBuilder joinPaths)
            {
                if (joinPaths == null) return;
                joinPaths.AppendFormat("Tables left: {0} , Joins used:{1}\r\n", tablesToUse.Count, joins.Count);
                /*
                joinPaths.AppendLine("\r\nDetail:");
                foreach (var join in joins)
                {
                    joinPaths.AppendFormat(string.Format("{0}-{1} ({2})\r\n", join.LeftTable.DisplayName, join.RightTable.DisplayName, join.Clause.Trim()));
                }*/
                var str = print();
                joinPaths.Append(str);
                if (tablesToUse.Count == 0) joinPaths.Append($"\r\nHash code: {str.GetHashCode()}\r\n");
                joinPaths.Append($"\r\n\r\n");
            }
        }

        void JoinTables(JoinPath path, List<JoinPath> resultPath)
        {
            //no need to find another path as it will be worth...
            if (path.tablesToUse.Count != 0 && path.joins.Count >= _bestJoinsCount) return;

            //If the search is longer than xx seconds, we exit with the first path found...
            if ((DateTime.Now - _buildTimer).TotalMilliseconds > BuildTimeout)
            {
                if (resultPath.Exists(i => i.tablesToUse.Count == 0))
                {
                    if (JoinLogs != null) JoinLogs.AppendFormat("Exiting the joins search after {0:F0} milliseconds\r\n", (DateTime.Now - _buildTimer).TotalMilliseconds);
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
                        //If LINQ, remove all tables giving the same Source
                        if (IsLINQ) newJoinPath.tablesToUse.RemoveAll(i => i.LINQResultName == newTable.LINQResultName);

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
                    if (element.SortOrder == ReportElement.kAutomaticAscSortKeyword) element.FinalSortOrder = string.Format("{0:000} {1}", 100 + i, ReportElement.kAscendantSortKeyword);
                    else if (element.SortOrder == ReportElement.kAutomaticDescSortKeyword) element.FinalSortOrder = string.Format("{0:000} {1}", 100 + i, ReportElement.kDescendantSortKeyword);
                    else element.FinalSortOrder = element.SortOrder;
                }
            }
        }

        bool hasGroupBy
        {
            get { return execGroupByClause.Length > 0; }
        }

        void buildOrderClause(IEnumerable<ReportElement> elements, List<string> orderColumns, ref StringBuilder orderClause, ref StringBuilder orderClauseName, bool noGroupBy)
        {
            foreach (ReportElement element in elements.OrderBy(i => i.FinalSortOrder))
            {
                if (!orderColumns.Contains(element.SQLColumn) && element.IsSorted)
                {
                    string SQLascdesc = element.SortOrder.Contains(ReportElement.kAscendantSortKeyword) ? " ASC" : " DESC";
                    string LINQascdesc = element.SortOrder.Contains(ReportElement.kAscendantSortKeyword) ? "" : " descending";
                    var colName = element.SQLColumn + SQLascdesc;
                    if (IsLINQ)
                    {
                        colName = string.Format("{0}{1}", (!noGroupBy && element.IsNotAggregate ? "g.Key." + element.SQLColumnName : element.LINQColumnName), LINQascdesc);
                    }

                    Helper.AddValue(ref orderClause, ",", colName);
                    Helper.AddValue(ref orderClauseName, ",", element.SQLColumnName + SQLascdesc);
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
            if (_command != null)
            {
                _command.Cancel();
            }
        }

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

        void checkRunningModels(string key, Dictionary<string, ReportModel> runningModels)
        {
            if (ShareResultTable && !Elements.Exists(i => i.IsEnum && i.ShowAllEnums)) //Do not share if show all enums as we will add rows to the result table
            {
                lock (runningModels)
                {
                    if (runningModels.ContainsKey(key))
                    {
                        //check if we can reuse the current running query: same source, same connection string and same pre/Post SQL
                        ReportModel runningModel = runningModels[key];
                        if (Source == runningModel.Source
                            && IsSubModel == runningModel.IsSubModel
                            && Connection.ConnectionType == runningModel.Connection.ConnectionType
                            && Connection.FullConnectionString == runningModel.Connection.FullConnectionString
                            && string.IsNullOrEmpty(runningModel.ExecutionError)
                            && ((PreSQL == null && runningModel.PreSQL == null) || (PreSQL.Trim() == runningModel.PreSQL.Trim()))
                            && ((PostSQL == null && runningModel.PostSQL == null) || (PostSQL.Trim() == runningModel.PostSQL.Trim()))
                            && ((PreLoadScript == null && runningModel.PreLoadScript == null) || (PreLoadScript.Trim() == runningModel.PreLoadScript.Trim()))
                            && ((LoadScript == null && runningModel.LoadScript == null) || (LoadScript.Trim() == runningModel.LoadScript.Trim()))
                            )
                        {
                            //we can wait to get the same data table 
                            var runningName = runningModel.MasterModel != null ? runningModel.MasterModel.Name + " > " + runningModel.Name : runningModel.Name;
                            if (!IsSubModel) Report.LogMessage("Model '{0}': Getting result table from model '{1}'...", Name, runningName);
                            else Report.LogMessage("Model '{0}': Getting result table of sub-model '{1}' from model '{2}'...", MasterModel.Name, Name, runningName);
                            while (!Report.Cancel && !runningModel._resultTableAvailable)
                            {
                                if (!string.IsNullOrEmpty(runningModel.ExecutionError)) break;
                                Thread.Sleep(100);
                            }

                            if (runningModel.ResultTable != null)
                            {
                                //Set the result table in the model
                                ResultTable = runningModel.ResultTable;
                            }
                        }
                    }
                    else
                    {
                        runningModels.Add(key, this);
                    }
                }
            }
        }

        void checkRunningSubTables(MetaTable subTable, Dictionary<string, MetaTable> runningSubTables)
        {
            if (ShareResultTable)
            {
                lock (runningSubTables)
                {
                    if (runningSubTables.ContainsKey(subTable.GUID))
                    {
                        //check if we can reuse the current running No SQL: same source, same connection string and same definition
                        MetaTable runningTable = runningSubTables[subTable.GUID];
                        if (subTable.Source == runningTable.Source
                            && subTable.Model.Connection.ConnectionType == runningTable.Model.Connection.ConnectionType
                            && subTable.Model.Connection.FullConnectionString == runningTable.Model.Connection.FullConnectionString
                            && string.IsNullOrEmpty(runningTable.Model.ExecutionError)
                            && subTable.IsIdentical(runningTable)
                            )
                        {
                            //we can wait to get the same data table 
                            Report.LogMessage("Model '{0}': Getting result table of No SQL table '{1}' from model '{2}'...", Name, subTable.Name, runningTable.Model.Name);
                            while (!Report.Cancel && runningTable.NoSQLTable == null)
                            {
                                if (!string.IsNullOrEmpty(runningTable.Model.ExecutionError)) break;
                                Thread.Sleep(100);
                            }

                            if (runningTable.NoSQLTable != null)
                            {
                                //Set the result table
                                subTable.NoSQLTable = runningTable.NoSQLTable;
                            }
                        }
                    }
                    else
                    {
                        runningSubTables.Add(subTable.GUID, subTable);
                    }
                }
            }
        }

        /// <summary>
        /// Current list of tables used for the join
        /// </summary>
        [XmlIgnore]
        public List<MetaJoin> ExecTableJoins = null;

        /// <summary>
        /// Current list of result tables for the model execution
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, DataTable> ExecResultTables = null;

        void initMongoStagesScript(MetaTable modelTable, MetaTable subTable)
        {
            //Mongo stages handling
            var script = "";
            //Restrictions = match stage
            var restrs = Restrictions.Where(i => i.MetaColumn != null && i.MetaColumn.MetaTable.HasSameMongoCollection(subTable)).ToList();
            if (restrs.Count > 0)
            {
                var restrStr = "";
                foreach (var restr in restrs)
                {
                    //check that the column is defined in the current table
                    if (!modelTable.Columns.Exists(i => i.Name == restr.MetaColumn.Name)) continue;

                    restrStr += restr.MongoText;
                }

                if (!string.IsNullOrEmpty(restrStr))
                {
                    script += @$"
    //Restrictions
    metaTable.MongoStages.Add(
        new BsonDocument(
            ""$match"",
            new BsonDocument({Helper.QuoteDouble(subTable.GetValue(MetaTable.ParameterNameMongoRestrictionOperator))},
                new BsonArray {{
{restrStr}                }}
    )));
";
                }
            }
            //Elements = Project stage
            var elementsToSelect = Elements.Where(i => i.MetaColumn != null && i.MetaColumn.MetaTable != null && i.MetaColumn.MetaTable.GUID == subTable.GUID).ToList();
            var colNames = new List<string>();
            if (elementsToSelect.Count > 0)
            {

                foreach (var colName in (from c in elementsToSelect select c.MetaColumn.Name).Distinct())
                {
                    colNames.Add(colName);
                }
            }
            //Add restriction elements used for LINQ
            foreach (var colName in (from c in restrs select c.MetaColumn.Name).Distinct())
            {
                colNames.Add(colName);
            }
            //elements used to perform the LINQ joins: we parse all columns defined in the sources involved
            foreach (var join in ExecTableJoins)
            {
                var sources = new List<MetaSource>();
                if (!sources.Contains(join.LeftTable.Source)) sources.Add(join.LeftTable.Source);
                if (!sources.Contains(join.RightTable.Source)) sources.Add(join.RightTable.Source);
                foreach (var s in sources)
                {
                    foreach (var t in s.MetaData.Tables)
                    {
                        foreach (var col in t.Columns)
                        {
                            if (join.Clause.Contains(string.Format("{0}[{1}]", col.MetaTable.LINQResultName, Helper.QuoteDouble(col.Name))))
                            {
                                colNames.Add(col.Name);
                            }
                        }
                    }
                }
            }
            if (colNames.Count > 0)
            {
                var elementStr = "";
                foreach (var colName in colNames.Distinct())
                {
                    //check that the column is defined in the current table
                    if (!modelTable.Columns.Exists(i => i.Name == colName)) continue;

                    elementStr += $"{{{Helper.QuoteDouble(colName)},1}},\r\n";
                }

                if (!string.IsNullOrEmpty(elementStr))
                {
                    script += @$"
    //Elements
    metaTable.MongoStages.Add(
        new BsonDocument(
            ""$project"",
            new BsonDocument {{
{elementStr}            }}
    ));
";
                }
            }

            if (!string.IsNullOrEmpty(script))
            {
                subTable.MongoStagesScript = @$"@using MongoDB.Bson
@{{
    //Script generated on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToLongTimeString()}
    MetaTable metaTable = Model;
{script}
}}";
            }
        }

        void initSubModelsAndTables()
        {
            //Build current sub-models
            if (LINQSubModels == null) LINQSubModels = new List<ReportModel>();

            var currentSubModels = LINQSubModels.ToList();
            LINQSubModels.Clear();

            var SQLtables = FromTables.Where(i => i.IsSQL).ToList();
            if (ExecTableJoins != null)
            {
                //Add other SQL tables involved in Joins...
                foreach (var join in ExecTableJoins)
                {
                    if (join.LeftTable.IsSQL && !SQLtables.Contains(join.LeftTable)) SQLtables.Add(join.LeftTable);
                }
            }

            foreach (var table in SQLtables)
            {
                var subModel = currentSubModels.FirstOrDefault(i => i.SourceGUID == table.LINQSourceGUID);
                if (subModel == null)
                {
                    subModel = new ReportModel() { SourceGUID = table.LINQSourceGUID };
                }
                subModel.Name = table.Source.Name;
                subModel.MasterModel = this;
                subModel.Report = Report;
                subModel.InitReferences();
                LINQSubModels.Add(subModel);
            }

            //Add elements involved
            foreach (var subModel in LINQSubModels)
            {
                var source = subModel.Source;

                //Elements in the select
                var elementsToSelect = Elements.Where(i => i.MetaColumn != null && i.MetaColumn.MetaTable != null && i.MetaColumn.MetaTable.LINQSourceGUID == subModel.SourceGUID).ToList();
                var currentElements = subModel.Elements.ToList();
                subModel.Elements.Clear();
                foreach (var element in elementsToSelect)
                {
                    var element2 = currentElements.FirstOrDefault(i => i.MetaColumnGUID == element.MetaColumnGUID);
                    if (element2 == null)
                    {
                        //Create the element
                        element2 = ReportElement.Create();
                        element2.MetaColumnGUID = element.MetaColumnGUID;
                    }
                    subModel.Elements.Add(element2);
                    element2.Name = element.Name;
                    element2.PivotPosition = SubModelsSetAggr ? element.PivotPosition : PivotPosition.Row;
                    element2.AggregateFunction = element.AggregateFunction;
                }

                //elements used to perform the LINQ joins: we parse all columns defined in the sources involved
                foreach (var join in ExecTableJoins)
                {
                    var sources = new List<MetaSource>();
                    if (!sources.Contains(join.LeftTable.Source)) sources.Add(join.LeftTable.Source);
                    if (!sources.Contains(join.RightTable.Source)) sources.Add(join.RightTable.Source);
                    foreach (var s in sources)
                    {
                        foreach (var t in s.MetaData.Tables)
                        {
                            foreach (var col in t.Columns)
                            {
                                if (join.Clause.Contains(string.Format("{0}[{1}]", col.MetaTable.LINQResultName, Helper.QuoteDouble(col.Name))))
                                {
                                    subModel.addHiddenElement(col.GUID);
                                }
                            }
                        }
                    }
                }

                //elements used to perform the LINQ restrictions
                foreach (var restr in Restrictions.Union(AggregateRestrictions).Where(i => i.MetaColumn != null && i.MetaColumn.MetaTable != null && i.MetaColumn.MetaTable.LINQSourceGUID == subModel.SourceGUID))
                {
                    subModel.addHiddenElement(restr.MetaColumnGUID);
                }

                if (SubModelsSetRestr)
                {
                    foreach (var restr in Restrictions.Where(i => i.MetaColumn != null && i.MetaColumn.MetaTable != null && i.MetaColumn.MetaTable.LINQSourceGUID == subModel.SourceGUID))
                    {
                        //propagate restrictions
                        var restriction = subModel.Restrictions.FirstOrDefault(i => i.MetaColumnGUID == restr.MetaColumnGUID);
                        if (restriction == null)
                        {
                            restriction = ReportRestriction.CreateReportRestriction();
                            restriction.MetaColumnGUID = restr.MetaColumnGUID;
                            subModel.Restrictions.Add(restriction);
                        }
                        if (!subModel.Restriction.Contains(restriction.Pattern))
                        {
                            if (!string.IsNullOrEmpty(subModel.Restriction)) subModel.Restriction += "\r\nAND ";
                            subModel.Restriction += restriction.Pattern;
                        }
                        restriction.DisplayNameEl = restr.DisplayNameEl;
                        restriction.PivotPosition = PivotPosition.Row;
                        restriction.CopyForPrompt(restr);
                    }
                }
                if (SubModelsSetAggr && SubModelsSetRestr)
                {
                    foreach (var restr in AggregateRestrictions.Where(i => i.MetaColumn != null && i.MetaColumn.MetaTable != null && i.MetaColumn.MetaTable.LINQSourceGUID == subModel.SourceGUID))
                    {
                        //propagate restrictions
                        var restriction = subModel.AggregateRestrictions.FirstOrDefault(i => i.MetaColumnGUID == restr.MetaColumnGUID);
                        if (restriction == null)
                        {
                            restriction = ReportRestriction.CreateReportRestriction();
                            restriction.MetaColumnGUID = restr.MetaColumnGUID;
                            subModel.AggregateRestrictions.Add(restriction);
                        }
                        if (!subModel.AggregateRestriction.Contains(restriction.Pattern))
                        {
                            if (!string.IsNullOrEmpty(subModel.AggregateRestriction)) subModel.AggregateRestriction += "\r\nAND ";
                            subModel.AggregateRestriction += restriction.Pattern;
                        }
                        restriction.DisplayNameEl = restr.DisplayNameEl;
                        restriction.PivotPosition = restr.PivotPosition;
                        restriction.CopyForPrompt(restr);
                    }
                }

                //clear sort
                foreach (var el in subModel.Elements) el.SortOrder = "";

                subModel.InitReferences();
            }

            //Current sub-tables                    
            if (LINQSubTables == null) LINQSubTables = new List<MetaTable>();
            var currentSubTables = LINQSubTables.ToList();
            LINQSubTables.Clear();

            var noSQLtables = FromTables.Where(i => !i.IsSQL).ToList();
            if (ExecTableJoins != null)
            {
                //Add other No SQL tables involved in Joins...
                foreach (var join in ExecTableJoins)
                {
                    if (!join.LeftTable.IsSQL && !noSQLtables.Contains(join.LeftTable)) noSQLtables.Add(join.LeftTable);
                }
            }

            foreach (var table in noSQLtables)
            {
                var subTable = currentSubTables.FirstOrDefault(i => i.GUID == table.GUID);
                if (subTable == null) subTable = new MetaTable() { GUID = table.GUID };

                subTable.Name = table.Name;
                subTable.Model = this;
                subTable.Source = table.Source;
                subTable.NoSQLTable = null;
                subTable.TemplateName = table.TemplateName;
                subTable.CacheDuration = table.CacheDuration;
                //Init default parameters
                subTable.InitParameters();

                if (subTable.IsMongoDb && SubModelsSetRestr && subTable.GetBoolValue(MetaTable.ParameterNameMongoSync, true))
                {
                    initMongoStagesScript(table, subTable);
                }

                LINQSubTables.Add(subTable);
            }
        }

        [XmlIgnore]
        private bool _resultTableAvailable = false;

        private async Task<DataTable> GetModelResultTableAsync(Dictionary<string, ReportModel> runningModels)
        {
            return await Task.Run(() =>
            {
                ResultTable = null;
                _resultTableAvailable = false;
                var source = Source;
                BuildQuery();
                if (FromTables != null)
                {
                    fillResultTableFromDatabase(runningModels);
                    //Rename columns
                    foreach (var element in Elements)
                    {
                        var newName = element.MetaColumn.Name.Replace("\"", "\\\"");
                        if (ResultTable.Columns.IndexOf(newName) == -1) ResultTable.Columns[element.SQLColumnName].ColumnName = newName;
                    }
                    ResultTable.TableName = FromTables[0].LINQResultName;
                    _resultTableAvailable = true;
                }
                else
                {
                    throw new Exception("No elements selected for this model");
                }
                return ResultTable;
            });
        }

        private async Task<DataTable> GetNoSQLResultTableAsync(MetaTable subTable, Dictionary<string, MetaTable> runningSubTables)
        {
            return await Task.Run(() =>
            {
                DataTable dataTable = null;

                //Check if we can use a data table from another sub-table
                checkRunningSubTables(subTable, runningSubTables);

                if (subTable.NoSQLTable == null)
                {
                    subTable.Log = Report;
                    subTable.NoSQLModel = this;
                    var table = subTable.Source.MetaData.Tables.FirstOrDefault(i => i.GUID == subTable.GUID);
                    if (table != null)
                    {
                        Report.LogMessage("Model '{0}': Building No SQL Table '{1}'", Name, subTable.Name);

                        if (subTable.NoSQLCacheTable != null && subTable.CacheDuration > 0 && DateTime.Now < subTable.LoadDate.AddSeconds(subTable.CacheDuration))
                        {
                            Report.LogMessage("Model '{0}': Using cache table loaded at {1} for '{2}'", Name, subTable.LoadDate, subTable.Name);
                            dataTable = subTable.NoSQLCacheTable;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(subTable.DefinitionScript)) subTable.DefinitionScript = table.DefinitionScript;
                            if (!string.IsNullOrEmpty(subTable.LoadScript))
                            {
                                dataTable = subTable.BuildNoSQLTable(false);
                                RazorHelper.CompileExecute(subTable.LoadScript, subTable);
                            }
                            else
                            {
                                dataTable = subTable.BuildNoSQLTable(true);
                            }
                            subTable.NoSQLCacheTable = dataTable;
                            subTable.LoadDate = DateTime.Now;
                        }

                        //Thread.Sleep(5000); //For DEV

                        dataTable.TableName = subTable.LINQResultName;
                        subTable.NoSQLTable = dataTable;
                    }
                }
                return dataTable;
            });
        }

        bool fillResultTableFromDatabase(Dictionary<string, ReportModel> runningModels)
        {
            bool isMaster = false;
            //Normal SQL
            try
            {
                if (string.IsNullOrEmpty(Connection.FullConnectionString)) throw new Exception("The connection string is not defined for this Model.");
                _command = null;

                //Check if we can use a data table from another model
                checkRunningModels(Sql, runningModels);

                if (ResultTable == null)
                {
                    isMaster = true; //This model is the master for the Result table
                    DbConnection connection = null;
                    connection = Connection.GetOpenConnection();

                    try
                    {
                        if (connection is OdbcConnection) _command = ((OdbcConnection)connection).CreateCommand();
                        else if (connection is SqlConnection) _command = ((SqlConnection)connection).CreateCommand();
                        else if (connection is Microsoft.Data.SqlClient.SqlConnection) _command = ((Microsoft.Data.SqlClient.SqlConnection)connection).CreateCommand();
                        else if (connection is MySql.Data.MySqlClient.MySqlConnection) _command = ((MySql.Data.MySqlClient.MySqlConnection)connection).CreateCommand();
                        else if (connection is OracleConnection) _command = ((OracleConnection)connection).CreateCommand();
                        else if (connection is NpgsqlConnection) _command = ((NpgsqlConnection)connection).CreateCommand();
                        else if (connection is SQLiteConnection) _command = ((SQLiteConnection)connection).CreateCommand();
                        else _command = ((OleDbConnection)connection).CreateCommand();

                        _command.CommandTimeout = (CommandTimeout == -1 ? Connection.CommandTimeout : CommandTimeout);
                        executePrePostStatement(Source.PreSQL, "Pre", Source.Name, Source.IgnorePrePostError, Source);
                        executePrePostStatements(true);
                        executePrePostStatement(PreSQL, "Pre", Name, IgnorePrePostError, this);

                        if (!IsSubModel) Report.LogMessage("Model '{0}': Executing main query...", Name);
                        else Report.LogMessage("Model '{0}': Executing query for sub-model '{1}'...", MasterModel.Name, Name);
                        _command.CommandText = Sql;

                        if (PrintQuery || Report.PrintQueries)
                        {
                            Report.LogMessage("Model '{0}' SQL Query:\r\n{1}\r\n", Name, Sql);
                        }

                        DbDataAdapter adapter = null;
                        if (connection is OdbcConnection) adapter = new OdbcDataAdapter((OdbcCommand)_command);
                        else if (connection is SqlConnection) adapter = new SqlDataAdapter((SqlCommand)_command);
                        else if (connection is Microsoft.Data.SqlClient.SqlConnection) adapter = new Microsoft.Data.SqlClient.SqlDataAdapter((Microsoft.Data.SqlClient.SqlCommand)_command);
                        else if (connection is MySql.Data.MySqlClient.MySqlConnection) adapter = new MySql.Data.MySqlClient.MySqlDataAdapter((MySql.Data.MySqlClient.MySqlCommand)_command);
                        else if (connection is OracleConnection) adapter = new OracleDataAdapter((OracleCommand)_command);
                        else if (connection is NpgsqlConnection) adapter = new NpgsqlDataAdapter((NpgsqlCommand)_command);
                        else if (connection is SQLiteConnection) adapter = new SQLiteDataAdapter((SQLiteCommand)_command);
                        else adapter = new OleDbDataAdapter((OleDbCommand)_command);
                        ResultTable = new DataTable();
                        adapter.Fill(ResultTable);

                        executePrePostStatement(PostSQL, "Post", Name, IgnorePrePostError, this);
                        executePrePostStatements(false);
                        executePrePostStatement(Source.PostSQL, "Post", Source.Name, Source.IgnorePrePostError, Source);
                    }
                    finally
                    {
                        connection.Close();
                        _command = null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unexpected error when executing the following SQL statement:\r\n{0}\r\n\r\nError detail:\r\n{1}", Sql, ex.Message));
            }

            return isMaster;
        }

        /// <summary>
        /// Build the ResultTable for the model
        /// </summary>
        public async Task FillResultTableAsync(Dictionary<string, ReportModel> runningModels, Dictionary<string, MetaTable> runningSubTables)
        {
            Progression = 0;
            ExecutionDuration = 0;
            ExecutionDate = DateTime.Now;
            Pages.Clear();

            //Pre-load script
            if (!Source.IsNoSQL) ExecuteLoadScript(PreLoadScript, "Pre load script", this);

            ExecutionError = "";

            BuildQuery();
            Progression = 5; //5% after building SQL

            ResultTable = null;
            _command = null;

            if (Source.IsNoSQL && !string.IsNullOrEmpty(LINQSelect) && !Report.Cancel)
            {
                //No SQL = LINQ
                try
                {
                    if (ResultTable == null)
                    {
                        ExecResultTables = new Dictionary<string, DataTable>();
                        var tasks = new List<Task>();
                        //Tables execution
                        foreach (var subModel in LINQSubModels)
                        {
                            //Pre-load script
                            ExecuteLoadScript(subModel.PreLoadScript, "Pre load script", subModel);

                            tasks.Add(subModel.GetModelResultTableAsync(runningModels));
                        }

                        foreach (var subTable in LINQSubTables)
                        {
                            tasks.Add(GetNoSQLResultTableAsync(subTable, runningSubTables));
                        }

                        //Wait for the result tables
                        await Task.WhenAll(tasks);

                        foreach (var subModel in LINQSubModels)
                        {
                            ExecResultTables.Add(subModel.ResultTable.TableName, subModel.ResultTable);
                        }

                        foreach (var subTable in LINQSubTables)
                        {
                            ExecResultTables.Add(subTable.NoSQLTable.TableName, subTable.NoSQLTable);
                        }

                        var loadScript = LoadScript ?? LINQLoadScript;
                        if (PrintQuery || Report.PrintQueries)
                        {
                            Report.LogMessage("Model '{0}' LINQ Query:\r\n{1}\r\n", Name, loadScript);
                        }

                        //Finally LINQ query
                        RazorHelper.CompileExecute(loadScript, this);

                        handleEnums();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Got unexpected error when building NoSQL Table:\r\n{0}\r\n{1}", ex.Message, ex.StackTrace));
                }
            }
            else if (!string.IsNullOrEmpty(Sql) && !Report.Cancel)
            {
                //Normal SQL
                ResultTable = null;
                _resultTableAvailable = false;
                var isMaster = fillResultTableFromDatabase(runningModels);

                if (Report.Cancel) return;

                //Check maximum number of records
                if (MaxNumberOfRecords > 0 && ResultTable.Rows.Count > MaxNumberOfRecords)
                {
                    while (ResultTable.Rows.Count > MaxNumberOfRecords) ResultTable.Rows.RemoveAt(MaxNumberOfRecords);
                }

                //If enum, set enum values directly in the table, only for master model
                if (isMaster && !IsSubModel) handleEnums();

                ExecuteLoadScript(LoadScript, "Post load script", this);
                _resultTableAvailable = true;
            }

            ExecutionDuration = Convert.ToInt32((DateTime.Now - ExecutionDate).TotalSeconds);
            Progression = 70; //70% after getting result set
        }

        void handleEnums()
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

        /// <summary>
        /// Translate the enum using the Enum context
        /// </summary>
        public string EnumDisplayValue(MetaEnum me, string id, bool forRestriction = false)
        {
            string result = me.GetDisplayValue(id, Connection, forRestriction);
            if (me.Translate) result = Report.TranslateEnumValue(me, result);
            return result;
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


        const string InternalLink = "<span class='external-navigation glyphicon glyphicon-log-in'></span>";
        const string ExternalLink = "<span class='external-navigation glyphicon glyphicon-new-window'></span>";
        /// <summary>
        /// HTML Navigation for the report result
        /// </summary>
        public string GetNavigation(ReportView view, ResultCell cell, bool serverSide = false)
        {
            string navigation = "";
            if (Report.GenerateHTMLDisplay || serverSide)
            {
                foreach (var link in cell.GetNavigationLinks(view))
                {
                    var secondLink = "";
                    bool newWindow = false;
                    if (Report.SecurityContext != null && !string.IsNullOrEmpty(Report.WebUrl) && (link.Type == NavigationType.Drill || link.Type == NavigationType.SubReport))
                    {
                        //Second link for drill or subreport
                        var profile = Report.SecurityContext.Profile;

                        var executionMode = profile.ExecutionMode;
                        if (executionMode == ExecutionMode.Default) executionMode = Report.SecurityContext.DefaultGroup.ExecutionMode;
                        if (executionMode != ExecutionMode.AlwaysNewWindow)
                        {
                            secondLink = (executionMode == ExecutionMode.NewWindow ? InternalLink : ExternalLink);
                            if (executionMode == ExecutionMode.NewWindow) newWindow = true;
                        }
                        else
                        {
                            newWindow = true;
                        }
                    }
                    navigation += string.Format("<li nav='{0}' nw='{3}'><a href='#'>{1}{2}</a></li>", link.FullHref, link.Text, secondLink, newWindow);
                }
                navigation = string.IsNullOrEmpty(navigation) ? "" : (serverSide ? navigation : string.Format(" navigation=\"{0}\"", navigation));
            }
            return navigation;
        }

        /// <summary>
        /// Return a result table value from a column name
        /// </summary>
        public object GetResultTableValue(int row, string columName)
        {
            object result = null;
            var element = Elements.FirstOrDefault(i => i.SQLColumn == columName);
            if (element != null && ResultTable != null) result = ResultTable.Rows[row][element.SQLColumnName];
            return result;
        }
    }
}
