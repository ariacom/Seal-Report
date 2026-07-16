//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System.ComponentModel;

namespace Seal.Model
{
    /// <summary>
    /// Position of an element in the cross table (pivot table) of a model
    /// </summary>
    public enum PivotPosition
    {
        /// <summary>
        /// Element is used to create result pages
        /// </summary>
        Page, 
        /// <summary>
        /// Element is displayed in rows
        /// </summary>
        Row,
        /// <summary>
        /// Element is displayed in columns
        /// </summary>
        Column,
        /// <summary>
        /// Element is aggregated in the data cells
        /// </summary>
        Data,
        /// <summary>
        /// Element is part of the model but not displayed in the result
        /// </summary>
        Hidden,
    };

    /// <summary>
    /// Type of the database used to generate the SQL statements
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// Standard database
        /// </summary>
        [Description("Standard")]
        Standard,
        /// <summary>
        /// Oracle database
        /// </summary>
        [Description("Oracle")]
        Oracle,
        /// <summary>
        /// MS Access database
        /// </summary>
        [Description("MS Access")]
        MSAccess,
        /// <summary>
        /// MS Excel file accessed as a database
        /// </summary>
        [Description("MS Excel")]
        MSExcel,
        /// <summary>
        /// MS SQLServer database
        /// </summary>
        [Description("MS SQLServer")]
        MSSQLServer,
        /// <summary>
        /// MySQL database
        /// </summary>
        [Description("MySQL")]
        MySQL,
        /// <summary>
        /// PostgreSQL database
        /// </summary>
        [Description("PostgreSQL")]
        PostgreSQL,
        /// <summary>
        /// SQLite database
        /// </summary>
        [Description("SQLite")]
        SQLite
    }

    /// <summary>
    /// Type of the connection (driver/provider) used to access the database
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// OleDb connection
        /// </summary>
        [Description("OleDb")]
        OleDb,
        /// <summary>
        /// Odbc connection
        /// </summary>
        [Description("Odbc")]
        Odbc,
        /// <summary>
        /// MS SQLServer connection using System.Data (deprecated, use Microsoft.Data instead)
        /// </summary>
        [Description("MS SQLServer (System.Data) (Deprecated = Microsoft.Data)")]
        MSSQLServer,
        /// <summary>
        /// MS SQLServer connection using Microsoft.Data
        /// </summary>
        [Description("MS SQLServer (Microsoft.Data)")]
        MSSQLServerMicrosoft,
        /// <summary>
        /// Mongo DB connection
        /// </summary>
        [Description("Mongo DB")]
        MongoDB,
        /// <summary>
        /// MySQL connection
        /// </summary>
        [Description("MySQL")]
        MySQL,
        /// <summary>
        /// Oracle connection
        /// </summary>
        [Description("Oracle")]
        Oracle,
        /// <summary>
        /// PostgreSQL connection
        /// </summary>
        [Description("PostgreSQL")]
        PostgreSQL,
        /// <summary>
        /// SQLite connection
        /// </summary>
        [Description("SQLite")]
        SQLite
    }

    /// <summary>
    /// Data type of a column or element
    /// </summary>
    public enum ColumnType
    {
        /// <summary>
        /// Default type from the database column
        /// </summary>
        [Description("Default")]
        Default,
        /// <summary>
        /// Text type
        /// </summary>
        [Description("Text")]
        Text,
        /// <summary>
        /// Numeric type
        /// </summary>
        [Description("Numeric")]
        Numeric,
        /// <summary>
        /// Date and Time type
        /// </summary>
        [Description("Date & Time")]
        DateTime,
        /// <summary>
        /// Unicode Text type
        /// </summary>
        [Description("Unicode Text")]
        UnicodeText,
    }

    /// <summary>
    /// Aggregate function applied to an element with a Data pivot position
    /// </summary>
    public enum AggregateFunction
    {
        /// <summary>
        /// Value is aggregated with a Sum
        /// </summary>
        [Description("Sum")]
        Sum,
        /// <summary>
        /// Value is aggregated with a Minimum
        /// </summary>
        [Description("Minimum")]
        Min,
        /// <summary>
        /// Value is aggregated with a Maximum
        /// </summary>
        [Description("Maximum")]
        Max,
        /// <summary>
        /// Value is aggregated with an Average
        /// </summary>
        [Description("Average")]
        Avg,
        /// <summary>
        /// Value is aggregated with a Count
        /// </summary>
        [Description("Count")]
        Count,
        /// <summary>
        /// Value is aggregated with a Count Distinct
        /// </summary>
        [Description("Count Distinct")]
        CountDistinct,
    };

    /// <summary>
    /// Format of the report result generated
    /// </summary>
    public enum ReportFormat
    {
        /// <summary>
        /// HTML format
        /// </summary>
        [Description("HTML")]
        html,
        /// <summary>
        /// HTML Print format
        /// </summary>
        [Description("HTML Print")]
        print,
        /// <summary>
        /// Excel format
        /// </summary>
        [Description("Excel")]
        Excel,
        /// <summary>
        /// PDF format
        /// </summary>
        [Description("PDF")]
        PDF,
        /// <summary>
        /// PDF format generated from the HTML result
        /// </summary>
        [Description("HTML to PDF")]
        HTML2PDF,
        /// <summary>
        /// CSV format
        /// </summary>
        [Description("CSV")]
        csv,
        /// <summary>
        /// Text format
        /// </summary>
        [Description("Text")]
        Text,
        /// <summary>
        /// XML format
        /// </summary>
        [Description("XML")]
        XML,
        /// <summary>
        /// Json format
        /// </summary>
        [Description("Json")]
        Json,
    }

    /// <summary>
    /// Standard display format for a numeric value
    /// </summary>
    public enum NumericStandardFormat
    {
        /// <summary>
        /// Default format
        /// </summary>
        [Description("Default")]
        Default,
        /// <summary>
        /// Custom format string
        /// </summary>
        [Description("Custom")]
        Custom,
        /// <summary>
        /// General format (123.456)
        /// </summary>
        [Description("General (123.456)")]
        General,
        /// <summary>
        /// General format with 2 digits (12)
        /// </summary>
        [Description("General 2 Digits (12)")]
        General2,
        /// <summary>
        /// General format with 5 digits (123.45)
        /// </summary>
        [Description("General 5 Digits (123.45)")]
        General5,
        /// <summary>
        /// Number format with 0 decimal (1 234)
        /// </summary>
        [Description("Number 0 Decimal (1 234)")]
        Numeric0,
        /// <summary>
        /// Number format with 1 decimal (1 234.5)
        /// </summary>
        [Description("Number 1 Decimal (1 234.5)")]
        Numeric1,
        /// <summary>
        /// Number format with 2 decimals (1 234.56)
        /// </summary>
        [Description("Number 2 Decimals (1 234.56)")]
        Numeric2,
        /// <summary>
        /// Number format with 3 decimals (1 234.567)
        /// </summary>
        [Description("Number 3 Decimals (1 234.567)")]
        Numeric3,
        /// <summary>
        /// Number format with 4 decimals (1 234.5678)
        /// </summary>
        [Description("Number 4 Decimals (1 234.5678)")]
        Numeric4,
        /// <summary>
        /// Decimal format (1234)
        /// </summary>
        [Description("Decimal (1234)")]
        Decimal,
        /// <summary>
        /// Decimal format with 0 decimal (1234)
        /// </summary>
        [Description("Decimal 0 (1234)")]
        Decimal0,
        /// <summary>
        /// Decimal format with 1 decimal (1234.5)
        /// </summary>
        [Description("Decimal 1 (1234.5)")]
        Decimal1,
        /// <summary>
        /// Decimal format with 2 decimals (1234.56)
        /// </summary>
        [Description("Decimal 2  (1234.56)")]
        Decimal2,
        /// <summary>
        /// Decimal format with 3 decimals (1234.567)
        /// </summary>
        [Description("Decimal 3  (1234.567)")]
        Decimal3,
        /// <summary>
        /// Decimal format with 4 decimals (1234.5678)
        /// </summary>
        [Description("Decimal 4  (1234.5678)")]
        Decimal4,
        /// <summary>
        /// Percentage format with 0 decimal (12 %)
        /// </summary>
        [Description("Percentage 0 Decimal (12 %)")]
        Percentage0,
        /// <summary>
        /// Percentage format with 1 decimal (12.3 %)
        /// </summary>
        [Description("Percentage 1 Decimal (12.3 %)")]
        Percentage1,
        /// <summary>
        /// Percentage format with 2 decimals (12.34 %)
        /// </summary>
        [Description("Percentage 2 Decimals (12.34 %)")]
        Percentage2,
        /// <summary>
        /// Currency format with 0 decimal ($123)
        /// </summary>
        [Description("Currency 0 Decimal ($123)")]
        Currency0,
        /// <summary>
        /// Currency format with 2 decimals ($123.46)
        /// </summary>
        [Description("Currency 2 Decimals ($123.46)")]
        Currency2,
        /// <summary>
        /// Exponential (scientific) format (1.052033E+003)
        /// </summary>
        [Description("Exponential (scientific) (1.052033E+003)")]
        Exponential,
        /// <summary>
        /// Exponential (scientific) format with 2 decimals (-1.05e+003)
        /// </summary>
        [Description("Exponential (scientific) 2 Decimals (-1.05e+003)")]
        Exponential2,
        /// <summary>
        /// Fixed-point format (1234.57)
        /// </summary>
        [Description("Fixed-point (1234.57)")]
        Fixedpoint,
        /// <summary>
        /// Fixed-point format with 0 decimals (1234)
        /// </summary>
        [Description("Fixed-point 0 Decimals (1234)")]
        Fixedpoint0,
        /// <summary>
        /// Fixed-point format with 2 decimals (1234.56)
        /// </summary>
        [Description("Fixed-point 2 Decimals (1234.56)")]
        Fixedpoint2,
        /// <summary>
        /// Hexadecimal format (FF)
        /// </summary>
        [Description("Hexadecimal (FF)")]
        Hexadecimal,
        /// <summary>
        /// Hexadecimal format with 8 digits
        /// </summary>
        [Description("Hexadecimal 8 Digits")]
        Hexadecimal8,
};

    /// <summary>
    /// Standard display format for a date time value
    /// </summary>
    public enum DateTimeStandardFormat
    {
        /// <summary>
        /// Default format
        /// </summary>
        [Description("Default")]
        Default,
        /// <summary>
        /// Custom format string
        /// </summary>
        [Description("Custom")]
        Custom,
        /// <summary>
        /// General Date Short Time format
        /// </summary>
        [Description("General Date Short Time")]
        ShortDateTime,
        /// <summary>
        /// General Date Long Time format
        /// </summary>
        [Description("General Date Long Time")]
        LongDateTime,
        /// <summary>
        /// Short Date format
        /// </summary>
        [Description("Short Date")]
        ShortDate,
        /// <summary>
        /// Long Date format
        /// </summary>
        [Description("Long Date")]
        LongDate,
        /// <summary>
        /// Short Time format
        /// </summary>
        [Description("Short Time")]
        ShortTime,
        /// <summary>
        /// Long Time format
        /// </summary>
        [Description("Long Time")]
        LongTime,
        /// <summary>
        /// Full Date Short Time format
        /// </summary>
        [Description("Full Date Short Time")]
        FullShortDateTime,
        /// <summary>
        /// Full Date Long Time format
        /// </summary>
        [Description("Full Date Long Time")]
        FullLongDateTime,
    };


    /// <summary>
    /// Defines if totals are displayed for the element in the cross table
    /// </summary>
    public enum ShowTotal
    {
        /// <summary>
        /// No total is displayed
        /// </summary>
        [Description("No Total")]
        No,
        /// <summary>
        /// Totals are displayed for rows
        /// </summary>
        [Description("For Rows")]
        Row,
        /// <summary>
        /// Totals are displayed for columns
        /// </summary>
        [Description("For Columns")]
        Column,
        /// <summary>
        /// Totals are displayed for rows and columns
        /// </summary>
        [Description("For Rows and Columns")]
        RowColumn,
        /// <summary>
        /// Totals are displayed for rows, only the total column is shown
        /// </summary>
        [Description("For Rows (Show only total column)")]
        RowHidden,
        /// <summary>
        /// Totals are displayed for rows and columns, only the total column is shown
        /// </summary>
        [Description("For Rows and Columns (Show only total column)")]
        RowColumnHidden,
    };

    /// <summary>
    /// Additional calculation applied to a data cell value
    /// </summary>
    public enum CalculationOption
    {
        /// <summary>
        /// No calculation option
        /// </summary>
        [Description("No Option")]
        No,
        /// <summary>
        /// Value is displayed as a percentage of the row
        /// </summary>
        [Description("% of the Row")]
        PercentageRow,
        /// <summary>
        /// Value is displayed as a percentage of the column
        /// </summary>
        [Description("% of the Column")]
        PercentageColumn,
        /// <summary>
        /// Value is displayed as a percentage of the total
        /// </summary>
        [Description("% of the Total")]
        PercentageAll,
    };

    /// <summary>
    /// Operator used for a restriction
    /// </summary>
    public enum Operator
    {
        /// <summary>
        /// Equals one of the values
        /// </summary>
        [Description("Equals")]
        Equal,
        /// <summary>
        /// Is different from the values
        /// </summary>
        [Description("Is different from")]
        NotEqual,
        /// <summary>
        /// Is greater than the value
        /// </summary>
        [Description("Is greater than")]
        Greater,
        /// <summary>
        /// Is greater than or equal to the value
        /// </summary>
        [Description("Is greater or equal than")]
        GreaterEqual,
        /// <summary>
        /// Is smaller than the value
        /// </summary>
        [Description("Is smaller than")]
        Smaller,
        /// <summary>
        /// Is smaller than or equal to the value
        /// </summary>
        [Description("Is smaller or equal than")]
        SmallerEqual,
        /// <summary>
        /// Is between the two values
        /// </summary>
        [Description("Is between")]
        Between,
        /// <summary>
        /// Is not between the two values
        /// </summary>
        [Description("Is not between")]
        NotBetween,
        /// <summary>
        /// Contains the value
        /// </summary>
        [Description("Contains")]
        Contains,
        /// <summary>
        /// Does not contain the value
        /// </summary>
        [Description("Does not contain")]
        NotContains,
        /// <summary>
        /// Starts with the value
        /// </summary>
        [Description("Starts with")]
        StartsWith,
        /// <summary>
        /// Ends with the value
        /// </summary>
        [Description("Ends with")]
        EndsWith,
        /// <summary>
        /// Is empty (empty string)
        /// </summary>
        [Description("Is empty")]
        IsEmpty,
        /// <summary>
        /// Is not empty (not an empty string)
        /// </summary>
        [Description("Is not empty")]
        IsNotEmpty,
        /// <summary>
        /// Is Null
        /// </summary>
        [Description("Is Null")]
        IsNull,
        /// <summary>
        /// Is not Null
        /// </summary>
        [Description("Is not Null")]
        IsNotNull,
        /// <summary>
        /// Value only: the prompted value is used directly without operator
        /// </summary>
        [Description("Value only")]
        ValueOnly,
        /// <summary>
        /// Contains any of the values
        /// </summary>
        [Description("Contains any")]
        ContainsAny,
        /// <summary>
        /// Does not contain any of the values
        /// </summary>
        [Description("Does not contain any")]
        NotContainsAny,
        /// <summary>
        /// Contains all the values
        /// </summary>
        [Description("Contains all")]
        ContainsAll,
        /// <summary>
        /// Does not contain all the values
        /// </summary>
        [Description("Does not contain all")]
        NotContainsAll,
    }

    /// <summary>
    /// Keyword that can be used in a date restriction to define a dynamic date
    /// </summary>
    public enum DateRestrictionKeyword
    {
        /// <summary>
        /// Current date and time
        /// </summary>
        [Description("Now")]
        Now,
        /// <summary>
        /// Current day
        /// </summary>
        [Description("Today")]
        Today,
        /// <summary>
        /// First day of the current week
        /// </summary>
        [Description("This Week")]
        ThisWeek,
        /// <summary>
        /// First day of the current month
        /// </summary>
        [Description("This Month")]
        ThisMonth,
        /// <summary>
        /// First day of the current quarter
        /// </summary>
        [Description("This Quarter")]
        ThisQuarter,
        /// <summary>
        /// First day of the current year
        /// </summary>
        [Description("This Year")]
        ThisYear,
        /// <summary>
        /// Start of the current hour
        /// </summary>
        [Description("This Hour")]
        ThisHour,
        /// <summary>
        /// Start of the current minute
        /// </summary>
        [Description("This Minute")]
        ThisMinute,
        /// <summary>
        /// First day of the current semester
        /// </summary>
        [Description("This Semester")]
        ThisSemester,
    }

    /// <summary>
    /// Axis used for a chart serie
    /// </summary>
    public enum AxisType
    {
        /// <summary>
        /// Primary axis
        /// </summary>
        Primary = 0,
        /// <summary>
        /// Secondary axis
        /// </summary>
        Secondary = 1
    }

    /// <summary>
    /// Sort order of the points in a chart serie
    /// </summary>
    public enum PointSortOrder
    {
        /// <summary>
        /// Points are sorted in ascending order
        /// </summary>
        Ascending = 0,
        /// <summary>
        /// Points are sorted in descending order
        /// </summary>
        Descending = 1
    }

    /// <summary>
    /// Defines how the element is used in the chart series
    /// </summary>
    public enum SerieDefinition
    {
        /// <summary>
        /// Element is not used in a serie
        /// </summary>
        [Description("No Serie")]
        None,
        /// <summary>
        /// Microsoft serie (not used from v4)
        /// </summary>
        [Description("! Not used from v4: Microsoft Serie")]
        Serie,
        /// <summary>
        /// Element is used as the chart axis
        /// </summary>
        [Description("Axis")]
        Axis,
        /// <summary>
        /// Element is used as a splitter to create several series
        /// </summary>
        [Description("Splitter")]
        Splitter,
        /// <summary>
        /// Element is used as a splitter for both axes
        /// </summary>
        [Description("Splitter for Both Axes")]
        SplitterBoth,
        /// <summary>
        /// NVD3 serie (not used from v4)
        /// </summary>
        [Description("! Not used from v4: NVD3 serie")]
        NVD3Serie,
    };

    /// <summary>
    /// Type of serie generated for an ECharts chart
    /// </summary>
    public enum EChartsSerieDefinition
    {
        /// <summary>
        /// No serie
        /// </summary>
        [Description("No Serie")]
        None,
        /// <summary>
        /// Line serie
        /// </summary>
        [Description("Line")]
        Line,
        /// <summary>
        /// Area serie
        /// </summary>
        [Description("Area")]
        Area,
        /// <summary>
        /// Bar serie
        /// </summary>
        [Description("Bar")]
        Bar,
        /// <summary>
        /// Pie serie
        /// </summary>
        [Description("Pie")]
        Pie,
        /// <summary>
        /// Scatter serie
        /// </summary>
        [Description("Scatter")]
        Scatter,
        /// <summary>
        /// Radar serie
        /// </summary>
        [Description("Radar")]
        Radar,
    };

    /// <summary>
    /// Type of serie generated for a Chart JS chart
    /// </summary>
    public enum ChartJSSerieDefinition
    {
        /// <summary>
        /// No serie
        /// </summary>
        [Description("No Serie")]
        None,
        /// <summary>
        /// Scatter serie
        /// </summary>
        [Description("Scatter")]
        Scatter,
        /// <summary>
        /// Line serie
        /// </summary>
        [Description("Line")]
        Line,
        /// <summary>
        /// Bar serie
        /// </summary>
        [Description("Bar")]
        Bar,
        /// <summary>
        /// Pie serie
        /// </summary>
        [Description("Pie")]
        Pie,
        /// <summary>
        /// Polar Area serie
        /// </summary>
        [Description("Polar Area")]
        PolarArea,
        /// <summary>
        /// Radar serie
        /// </summary>
        [Description("Radar")]
        Radar,
    };

    /// <summary>
    /// Type of serie generated for a Plotly chart
    /// </summary>
    public enum PlotlySerieDefinition
    {
        /// <summary>
        /// No serie
        /// </summary>
        [Description("No Serie")]
        None,
        /// <summary>
        /// Scatter serie
        /// </summary>
        [Description("Scatter")]
        Scatter,
        /// <summary>
        /// Bar serie
        /// </summary>
        [Description("Bar")]
        Bar,
        /// <summary>
        /// Pie serie
        /// </summary>
        [Description("Pie")]
        Pie,
    };

    /// <summary>
    /// Type of serie generated for a ScottPlot chart
    /// </summary>
    public enum ScottPlotSerieDefinition
    {
        /// <summary>
        /// No serie
        /// </summary>
        [Description("No Serie")]
        None,
        /// <summary>
        /// Scatter serie
        /// </summary>
        [Description("Scatter")]
        Scatter,
        /// <summary>
        /// Bar serie
        /// </summary>
        [Description("Bar")]
        Bar,
        /// <summary>
        /// Pie serie
        /// </summary>
        [Description("Pie")]
        Pie,
    };

    /// <summary>
    /// Defines how the points of a chart serie are sorted
    /// </summary>
    public enum SerieSortType
    {
        /// <summary>
        /// No sort is applied
        /// </summary>
        [Description("No Sort")]
        None,
        /// <summary>
        /// Points are sorted by point value
        /// </summary>
        [Description("By Point")]
        Y,
        /// <summary>
        /// Points are sorted by axis label
        /// </summary>
        [Description("By Axis Label")]
        AxisLabel,
    };

    /// <summary>
    /// Type of the SQL join used to link two tables
    /// </summary>
    public enum JoinType
    {
        /// <summary>
        /// Inner Join
        /// </summary>
        [Description("Inner Join")]
        Inner,
        /// <summary>
        /// Left Outer Join
        /// </summary>
        [Description("Left Outer Join")]
        LeftOuter,
        /// <summary>
        /// Right Outer Join
        /// </summary>
        [Description("Right Outer Join")]
        RightOuter,
        /// <summary>
        /// Cross Join
        /// </summary>
        [Description("Cross Join")]
        Cross,
    };

    /// <summary>
    /// Defines if the restriction is prompted to the user at execution
    /// </summary>
    public enum PromptType
    {
        /// <summary>
        /// No prompt
        /// </summary>
        [Description("No prompt")]
        None,
        /// <summary>
        /// Restriction is prompted at execution
        /// </summary>
        [Description("Prompt at execution")]
        Prompt,
        /// <summary>
        /// Restriction is prompted at execution for one value only
        /// </summary>
        [Description("Prompt only one value")]
        PromptOneValue,
        /// <summary>
        /// Restriction is prompted at execution for two values only
        /// </summary>
        [Description("Prompt only two values")]
        PromptTwoValues,
    }

    /// <summary>
    /// Type of a view parameter value
    /// </summary>
    public enum ViewParameterType
    {
        /// <summary>
        /// String parameter
        /// </summary>
        String,
        /// <summary>
        /// Numeric (integer) parameter
        /// </summary>
        Numeric,
        /// <summary>
        /// Boolean parameter
        /// </summary>
        Boolean,
        /// <summary>
        /// Enumerated list parameter
        /// </summary>
        Enum,
        /// <summary>
        /// Multi-line text parameter
        /// </summary>
        Text,
        /// <summary>
        /// Parameter of the root report view
        /// </summary>
        RootReportView,
        /// <summary>
        /// Double (floating point) parameter
        /// </summary>
        Double,
    }

    /// <summary>
    /// Status of a report execution
    /// </summary>
    public enum ReportStatus
    {
        /// <summary>
        /// Report has not been executed
        /// </summary>
        NotExecuted,
        /// <summary>
        /// Report is being executed (models are processed)
        /// </summary>
        Executing,
        /// <summary>
        /// Report result is being rendered
        /// </summary>
        RenderingResult,
        /// <summary>
        /// Report display is being rendered
        /// </summary>
        RenderingDisplay,
        /// <summary>
        /// Report has been executed
        /// </summary>
        Executed,
    }

    /// <summary>
    /// Context in which the report execution takes place
    /// </summary>
    public enum ReportExecutionContext
    {
        /// <summary>
        /// Report executed from the Report Designer
        /// </summary>
        DesignerReport,
        /// <summary>
        /// Report output executed from the Report Designer
        /// </summary>
        DesignerOutput,
        /// <summary>
        /// Report executed from the Task Scheduler
        /// </summary>
        TaskScheduler,
        /// <summary>
        /// Report executed from the Web Report Server
        /// </summary>
        WebReport,
        /// <summary>
        /// Report output executed from the Web Report Server
        /// </summary>
        WebOutput,
    }

    /// <summary>
    /// Kind of the report: standard report or tasks-only report
    /// </summary>
    public enum ReportKind
    {
        /// <summary>
        /// Standard report
        /// </summary>
        [Description("Report")]
        Report,
        /// <summary>
        /// Report containing only tasks
        /// </summary>
        [Description("Task")]
        Task,
    }


    /// <summary>
    /// Right applied on a report or files folder for a security group
    /// </summary>
    public enum FolderRight
    {
        /// <summary>
        /// No right
        /// </summary>
        [Description("No right")]
        None,
        /// <summary>
        /// Execute reports / View files
        /// </summary>
        [Description("Execute reports / View files")]
        Execute,
        /// <summary>
        /// Execute reports and outputs / View files
        /// </summary>
        [Description("Execute reports and outputs / View files")]
        ExecuteReportOuput,
        /// <summary>
        /// Edit schedules / View files
        /// </summary>
        [Description("Edit schedules / View files")]
        Schedule,
        /// <summary>
        /// Edit reports / Manage files
        /// </summary>
        [Description("Edit reports / Manage files")]
        Edit,
    }

    /// <summary>
    /// Type of personal folder allowed for a user
    /// </summary>
    public enum PersonalFolderRight
    {
        /// <summary>
        /// No personal folder
        /// </summary>
        [Description("No personal folder")]
        None,
        /// <summary>
        /// Personal folder for files only
        /// </summary>
        [Description("Personal folder for files only")]
        Files,
        /// <summary>
        /// Personal folder for reports and files
        /// </summary>
        [Description("Personal folder for reports and files")]
        Reports,
    }

    /// <summary>
    /// Right applied on a Repository Folder (a folder published anywhere under the repository root, outside the Reports tree).
    /// </summary>
    public enum RepositoryFolderRight
    {
        /// <summary>
        /// No right
        /// </summary>
        [Description("No right")]
        None,
        /// <summary>
        /// Read only (view and download files)
        /// </summary>
        [Description("Read only (view and download files)")]
        ReadOnly,
        /// <summary>
        /// Read write (manage files)
        /// </summary>
        [Description("Read write (manage files)")]
        ReadWrite,
    }


    /// <summary>
    /// Defines when the failover email is sent in case of schedule failures
    /// </summary>
    public enum FailoverEmailMode
    {
        /// <summary>
        /// Email is sent only for the first failure
        /// </summary>
        [Description("Only for the first failure")]
        First,
        /// <summary>
        /// Email is sent for each failure
        /// </summary>
        [Description("For each failure")]
        All,
        /// <summary>
        /// Email is sent only for the last failure
        /// </summary>
        [Description("Only for the last failure")]
        Last,
    }

    /// <summary>
    /// Defines how the log files are generated
    /// </summary>
    public enum LogMode
    {
        /// <summary>
        /// No log
        /// </summary>
        [Description("No log")]
        None,
        /// <summary>
        /// Log in one file per day
        /// </summary>
        [Description("Log in one file per day")]
        OneFilePerDay,
        /// <summary>
        /// Log in one file per report
        /// </summary>
        [Description("Log in one file per report")]
        OneFilePerReport,
        /// <summary>
        /// Log in one file per execution
        /// </summary>
        [Description("Log in one file per execution")]
        OneFilePerExecution,
    }

    /// <summary>
    /// Boolean value with a Default option
    /// </summary>
    public enum YesNoDefault
    {
        /// <summary>
        /// Use the default value
        /// </summary>
        [Description("Default")]
        Default,
        /// <summary>
        /// Yes
        /// </summary>
        [Description("Yes")]
        Yes,
        /// <summary>
        /// No
        /// </summary>
        [Description("No")]
        No,
    }

    /// <summary>
    /// Type of navigation link in a report result
    /// </summary>
    public enum NavigationType
    {
        /// <summary>
        /// Drill down or up navigation
        /// </summary>
        Drill,
        /// <summary>
        /// Navigation to a sub-report
        /// </summary>
        SubReport,
        /// <summary>
        /// Navigation to a hyperlink
        /// </summary>
        Hyperlink,
        /// <summary>
        /// Navigation to download a file
        /// </summary>
        FileDownload,
        /// <summary>
        /// Navigation executing a report navigation script
        /// </summary>
        ReportScript,
        /// <summary>
        /// Navigation executing another report
        /// </summary>
        ReportExecution
    }

    /// <summary>
    /// Step of the report execution when a task or script is processed
    /// </summary>
    public enum ExecutionStep
    {
        /// <summary>
        /// Before the models generation
        /// </summary>
        [Description("Before models generation")]
        BeforeModel,
        /// <summary>
        /// After the models generation, before the rendering
        /// </summary>
        [Description("Models generated, before rendering")]
        BeforeRendering,
        /// <summary>
        /// After the rendering
        /// </summary>
        [Description("After rendering")]
        AfterRendering,
        /// <summary>
        /// Output execution: before the output execution
        /// </summary>
        [Description("Output Execution: Before output execution")]
        BeforeOutput,
        /// <summary>
        /// After the execution
        /// </summary>
        [Description("After execution")]
        AfterExecution
    }

    /// <summary>
    /// Type of the event stored in the audit database
    /// </summary>
    public enum AuditType
    {
        /// <summary>
        /// User login
        /// </summary>
        Login,
        /// <summary>
        /// User login failure
        /// </summary>
        LoginFailure,
        /// <summary>
        /// User logout
        /// </summary>
        Logout,
        /// <summary>
        /// Report execution
        /// </summary>
        ReportExecution,
        /// <summary>
        /// Report execution error
        /// </summary>
        ReportExecutionError,
        /// <summary>
        /// Report saved
        /// </summary>
        ReportSave,
        /// <summary>
        /// File moved
        /// </summary>
        FileMove,
        /// <summary>
        /// File copied
        /// </summary>
        FileCopy,
        /// <summary>
        /// File deleted
        /// </summary>
        FileDelete,
        /// <summary>
        /// Shortcut created
        /// </summary>
        ShortcutCreate,
        /// <summary>
        /// Folder created
        /// </summary>
        FolderCreate,
        /// <summary>
        /// Folder renamed
        /// </summary>
        FolderRename,
        /// <summary>
        /// Folder deleted
        /// </summary>
        FolderDelete,
        /// <summary>
        /// Server event error
        /// </summary>
        EventError,
        /// <summary>
        /// Server event
        /// </summary>
        EventServer,
        /// <summary>
        /// Logged users event
        /// </summary>
        EventLoggedUsers,
        /// <summary>
        /// AI chat exchange
        /// </summary>
        AIChat,
        /// <summary>
        /// AI chat error
        /// </summary>
        AIChatError,
    }

    /// <summary>
    /// Type of the schedule trigger
    /// </summary>
    public enum TriggerType
    {
        /// <summary>
        /// Trigger one time
        /// </summary>
        [Description("One time")]
        Time = 1,
        /// <summary>
        /// Daily trigger
        /// </summary>
        [Description("Daily")]
        Daily = 2,
        /// <summary>
        /// Weekly trigger
        /// </summary>
        [Description("Weekly")]
        Weekly = 3,
        /// <summary>
        /// Monthly trigger
        /// </summary>
        [Description("Monthly")]
        Monthly = 4,
    }

    /// <summary>
    /// Protocol used by the File Server output device
    /// </summary>
    public enum FileServerProtocol
    {
        /// <summary>
        /// FTP protocol
        /// </summary>
        [Description("FTP")]
        FTP,
        /// <summary>
        /// SFTP protocol
        /// </summary>
        [Description("SFTP")]
        SFTP,
        /// <summary>
        /// SCP protocol
        /// </summary>
        [Description("SCP")]
        SCP,
    }

    /// <summary>
    /// Authentication type used by the SharePoint output device
    /// </summary>
    public enum SharePointAuthenticationType
    {
        /// <summary>
        /// Authentication with a Client Secret
        /// </summary>
        [Description("Client Secret")]
        ClientSecret,
        /// <summary>
        /// Authentication with a Certificate
        /// </summary>
        [Description("Certificate")]
        Certificate,
    }

    /// <summary>
    /// Behavior of the SharePoint upload when the file already exists
    /// </summary>
    public enum SharePointConflictBehavior
    {
        /// <summary>
        /// Replace the existing file
        /// </summary>
        [Description("Replace the existing file")]
        Replace,
        /// <summary>
        /// Rename the new file
        /// </summary>
        [Description("Rename the new file")]
        Rename,
        /// <summary>
        /// Fail the upload
        /// </summary>
        [Description("Fail the upload")]
        Fail,
    }

    /// <summary>
    /// Step of the model result build during execution
    /// </summary>
    public enum ModelBuildStep
    {
        /// <summary>
        /// The result table is being filled
        /// </summary>
        FillResultTable,
        /// <summary>
        /// The result pages are being built
        /// </summary>
        BuildPages,
        /// <summary>
        /// The result tables are being built
        /// </summary>
        BuildTables,
        /// <summary>
        /// The totals are being built
        /// </summary>
        BuildTotals,
        /// <summary>
        /// The cell scripts are being executed
        /// </summary>
        HandleCellScript,
        /// <summary>
        /// The chart series are being built
        /// </summary>
        BuildSeries,
        /// <summary>
        /// The final sort is being applied
        /// </summary>
        FinalSort,
        /// <summary>
        /// The sub-totals are being built
        /// </summary>
        BuildSubTotals,
        /// <summary>
        /// The final script is being executed
        /// </summary>
        HandleFinalScript,
    }

    /// <summary>
    /// Defines which enum values are selected by default when the restriction is prompted for the first time
    /// </summary>
    public enum FirstEnumSelection
    {
        /// <summary>
        /// Use the selected values
        /// </summary>
        [Description("Use selected values")]
        None,
        /// <summary>
        /// Select all the values
        /// </summary>
        [Description("Select all values")]
        All,
        /// <summary>
        /// Select the first value
        /// </summary>
        [Description("Select first value")]
        First,
        /// <summary>
        /// Select the last value
        /// </summary>
        [Description("Select last value")]
        Last,
    }

    /// <summary>
    /// Layout of the restriction values when prompted in the report
    /// </summary>
    public enum RestrictionLayout
    {
        /// <summary>
        /// Select control with a filter
        /// </summary>
        [Description("Select with filter")]
        SelectWithFilter,
        /// <summary>
        /// Select control without filter
        /// </summary>
        [Description("Select without filter")]
        SelectNoFilter,
        /// <summary>
        /// Radio or toggle buttons
        /// </summary>
        [Description("Radio or toggle buttons")]
        RadioToggleButton,
    }

    /// <summary>
    /// Display style of the restriction operator when prompted in the report
    /// </summary>
    public enum RestrictionOperatorStyle
    {
        /// <summary>
        /// Operator is visible and modifiable
        /// </summary>
        [Description("Visible and modifiable")]
        Visible,
        /// <summary>
        /// Operator is visible with Null operators and modifiable
        /// </summary>
        [Description("Visible with Nulls and modifiable")]
        VisibleWithNulls,
        /// <summary>
        /// Operator is visible but not modifiable
        /// </summary>
        [Description("Visible but not modifiable")]
        NotModifiable,
        /// <summary>
        /// Operator is not visible
        /// </summary>
        [Description("Not visible")]
        NotVisible,
    }

    /// <summary>
    /// Defines how Blank values can be selected in an enum restriction
    /// </summary>
    public enum RestrictionBlanksOptions
    {
        /// <summary>
        /// Default behavior
        /// </summary>
        [Description("Default")]
        Default,
        /// <summary>
        /// Allow selection of Blank values
        /// </summary>
        [Description("Select Blank values")]
        BlankValues,
        /// <summary>
        /// Allow selection of Not Blank values
        /// </summary>
        [Description("Select Not Blank values")]
        NotBlankValues,
    }

    /// <summary>
    /// Defines which report is executed at startup for the user
    /// </summary>
    public enum StartupOptions
    {
        /// <summary>
        /// Default behavior
        /// </summary>
        [Description("Default")]
        Default,
        /// <summary>
        /// Do not execute a report at startup
        /// </summary>
        [Description("Do not execute report")]
        None,
        /// <summary>
        /// Execute the last report at startup
        /// </summary>
        [Description("Execute the last report")]
        ExecuteLast,
        /// <summary>
        /// Execute a specific report at startup
        /// </summary>
        [Description("Execute a specific report")]
        ExecuteReport,
    }

    /// <summary>
    /// Defines how the report is executed in the Web Report Server (window used)
    /// </summary>
    public enum ExecutionMode
    {
        /// <summary>
        /// Default behavior
        /// </summary>
        [Description("Default")]
        Default,
        /// <summary>
        /// Execute the report in a new Window
        /// </summary>
        [Description("Execute in a new Window")]
        NewWindow,
        /// <summary>
        /// Execute the report in the same Window
        /// </summary>
        [Description("Execute in the same Window")]
        SameWindow,
        /// <summary>
        /// Allow only execution in a new Window
        /// </summary>
        [Description("Allow only execution in a new Window")]
        AlwaysNewWindow,
    }

    /// <summary>
    /// Scheduler used to execute the report schedules
    /// </summary>
    public enum SchedulerMode
    {
        /// <summary>
        /// Windows Task Scheduler (Windows only)
        /// </summary>
        [Description("Windows Task Scheduler (Windows only)")]
        Windows,
        /// <summary>
        /// Scheduler Service (Windows only) or Worker (all platforms)
        /// </summary>
        [Description("Scheduler Service (Windows only) or Worker (All platforms)")]
        Service,
        /// <summary>
        /// Scheduler run by the Web Report Server (all platforms)
        /// </summary>
        [Description("Web Report Server (All platforms)")]
        WebServer,
    }

    /// <summary>
    /// Mode used to encrypt and decrypt the password values
    /// </summary>
    public enum EncryptionMode
    {
        /// <summary>
        /// Use the key values defined in the configuration file (default)
        /// </summary>
        [Description("Use the key values defined in the configuration file (default)")]
        Default,
        /// <summary>
        /// Use the Machine RSA Key Container (works only for the current machine)
        /// </summary>
        [Description("Use the Machine RSA Key Container (Works only for the current machine)")]
        MachineRSAContainer,
        /// <summary>
        /// Use the User RSA Key Container (works only for the current user)
        /// </summary>
        [Description("Use the User RSA Key Container (Works only for the current user)")]
        UserRSAContainer,
    }

    /// <summary>
    /// Defines when a new sheet is created during the Excel result generation
    /// </summary>
    public enum ExcelNewSheetMode
    {
        /// <summary>
        /// New sheet per 'Tab Page' view
        /// </summary>
        [Description("New Sheet per 'Tab Page' View")]
        PerTabPage,
        /// <summary>
        /// New sheet per 'Model' view
        /// </summary>
        [Description("New Sheet per 'Model' View")]
        PerModel,
        /// <summary>
        /// Keep the current sheet
        /// </summary>
        [Description("Keep the current Sheet")]
        KeepSheet,
    }

    /// <summary>
    /// Type of identifier generated in a result page for a chart canvas
    /// </summary>
    public enum ResultPageIdentifierType
    {
        /// <summary>
        /// Identifier of a Chart JS canvas
        /// </summary>
        ChartJSCanvas,
        /// <summary>
        /// Identifier of an ECharts canvas
        /// </summary>
        ChartEChartsCanvas,
        /// <summary>
        /// Identifier of a Plotly canvas
        /// </summary>
        ChartPlotlyCanvas,
        /// <summary>
        /// Identifier of a Gauge canvas
        /// </summary>
        GaugeCanvas,
    }

    /// <summary>
    /// Type of the server used to send emails
    /// </summary>
    public enum EmailServerType
    {
        /// <summary>
        /// SMTP server using System.Net.Mail
        /// </summary>
        [Description("SMTP (System.Net.Mail)")]
        SMTP,
        /// <summary>
        /// SMTP server using MimeKit
        /// </summary>
        [Description("SMTP (MimeKit)")]
        SMTPMimeKit,
        /// <summary>
        /// SendGrid service
        /// </summary>
        [Description("SendGrid")]
        SendGrid,
        /// <summary>
        /// Microsoft Graph API
        /// </summary>
        [Description("Microsoft Graph")]
        MSGraph,
    }
}
