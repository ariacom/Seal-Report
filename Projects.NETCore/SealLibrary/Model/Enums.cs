//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.ComponentModel;

namespace Seal.Model
{
    public enum PivotPosition
    {
        Page, 
        Row,
        Column,
        Data,
        Hidden,
    };

    public enum DatabaseType
    {
        [Description("Standard")]
        Standard,
        [Description("Oracle")]
        Oracle,
        [Description("MS Access")]
        MSAccess,
        [Description("MS Excel")]
        MSExcel,
        [Description("MS SQLServer")]
        MSSQLServer,
        [Description("MySQL")]
        MySQL,
    }

    public enum ConnectionType
    {
        [Description("OleDb")]
        OleDb,
        [Description("Odbc")]
        Odbc,
        [Description("MS SQLServer")]
        MSSQLServer,
    }

    public enum ColumnType
    {
        [Description("Default")]
        Default,
        [Description("Text")]
        Text,
        [Description("Numeric")]
        Numeric,
        [Description("Date & Time")]
        DateTime,
        [Description("Unicode Text")]
        UnicodeText,
    }

    public enum AggregateFunction
    {
        [Description("Sum")]
        Sum,
        [Description("Minimum")]
        Min,
        [Description("Maximum")]
        Max,
        [Description("Average")]
        Avg,
        [Description("Count")]
        Count,
    };

    public enum ReportFormat
    {
        html,
        print,
        csv,
        pdf,
        excel,
        custom
    }

    public enum NumericStandardFormat
    {
        [Description("Default")]
        Default,
        [Description("Custom")]
        Custom,
        [Description("General (123.456)")]
        General,
        [Description("General 2 Digits (12)")]
        General2,
        [Description("General 5 Digits (123.45)")]
        General5,
        [Description("Number 0 Decimal (1 234)")]
        Numeric0,
        [Description("Number 1 Decimal (1 234.5)")]
        Numeric1,
        [Description("Number 2 Decimals (1 234.56)")]
        Numeric2,
        [Description("Number 3 Decimals (1 234.567)")]
        Numeric3,
        [Description("Number 4 Decimals (1 234.5678)")]
        Numeric4,
        [Description("Decimal (1234)")]
        Decimal,
        [Description("Decimal 0 (1234)")]
        Decimal0,
        [Description("Decimal 1 (1234.5)")]
        Decimal1,
        [Description("Decimal 2  (1234.56)")]
        Decimal2,
        [Description("Decimal 3  (1234.567)")]
        Decimal3,
        [Description("Decimal 4  (1234.5678)")]
        Decimal4,
        [Description("Percentage 0 Decimal (12 %)")]
        Percentage0,
        [Description("Percentage 1 Decimal (12.3 %)")]
        Percentage1,
        [Description("Percentage 2 Decimals (12.34 %)")]
        Percentage2,
        [Description("Currency 0 Decimal ($123)")]
        Currency0,
        [Description("Currency 2 Decimals ($123.46)")]
        Currency2,
        [Description("Exponential (scientific) (1.052033E+003)")]
        Exponential,
        [Description("Exponential (scientific) 2 Decimals (-1.05e+003)")]
        Exponential2,
        [Description("Fixed-point (1234.57)")]
        Fixedpoint,
        [Description("Fixed-point 2 Decimals (1234.56)")]
        Fixedpoint2,
        [Description("Hexadecimal (FF)")]
        Hexadecimal,
        [Description("Hexadecimal 8 Digits")]
        Hexadecimal8,
};

    public enum DateTimeStandardFormat
    {
        [Description("Default")]
        Default,
        [Description("Custom")]
        Custom,
        [Description("General Date Short Time")]
        ShortDateTime,
        [Description("General Date Long Time")]
        LongDateTime,
        [Description("Short Date")]
        ShortDate,
        [Description("Long Date")]
        LongDate,
        [Description("Short Time")]
        ShortTime,
        [Description("Long Time")]
        LongTime,
        [Description("Full Date Short Time")]
        FullShortDateTime,
        [Description("Full Date Long Time")]
        FullLongDateTime,
    };


    public enum ShowTotal
    {
        [Description("No Total")]
        No,
        [Description("For Rows")]
        Row,
        [Description("For Columns")]
        Column,
        [Description("For Rows and Columns")]
        RowColumn,
        [Description("For Rows (Show only total column)")]
        RowHidden,
        [Description("For Rows and Columns (Show only total column)")]
        RowColumnHidden,
    };

    public enum CalculationOption
    {
        [Description("No Option")]
        No,
        [Description("% of the Row")]
        PercentageRow,
        [Description("% of the Column")]
        PercentageColumn,
        [Description("% of the Total")]
        PercentageAll,
    };

    public enum Operator
    {
        [Description("Equals")]
        Equal,
        [Description("Is different from")]
        NotEqual,
        [Description("Is greater than")]
        Greater,
        [Description("Is greater or equal than")]
        GreaterEqual,
        [Description("Is smaller than")]
        Smaller,
        [Description("Is smaller or equal than")]
        SmallerEqual,
        [Description("Is between")]
        Between,
        [Description("Is not between")]
        NotBetween,
        [Description("Contains")]
        Contains,
        [Description("Does not contain")]
        NotContains,
        [Description("Starts with")]
        StartsWith,
        [Description("Ends with")]
        EndsWith,
        [Description("Is empty")]
        IsEmpty,
        [Description("Is not empty")]
        IsNotEmpty,
        [Description("Is Null")]
        IsNull,
        [Description("Is not Null")]
        IsNotNull,
        [Description("Value only")]
        ValueOnly,
    }

    public enum DateRestrictionKeyword
    {
        [Description("Now")]
        Now,
        [Description("Today")]
        Today,
        [Description("This Week")]
        ThisWeek,
        [Description("This Month")]
        ThisMonth,
        [Description("This Quarter")]
        ThisQuarter,
        [Description("This Year")]
        ThisYear,
        [Description("This Hour")]
        ThisHour,
        [Description("This Minute")]
        ThisMinute,
        [Description("This Semester")]
        ThisSemester,
    }

    public enum AxisType
    {
        Primary = 0,
        Secondary = 1
    }

    public enum PointSortOrder
    {
        Ascending = 0,
        Descending = 1
    }

    public enum SerieDefinition
    {
        [Description("No Serie")]
        None,
        [Description("! Not used from v4: Microsoft Serie")]
        Serie,
        [Description("Axis")]
        Axis,
        [Description("Splitter")]
        Splitter,
        [Description("Splitter for Both Axes")]
        SplitterBoth,
        [Description("! Not used from v4: NVD3 serie")]
        NVD3Serie,
    };

    public enum NVD3SerieDefinition
    {
        [Description("No Serie")]
        None,
        [Description("Scatter")]
        ScatterChart,
        [Description("Line")]
        Line,
        [Description("Bar")]
        MultiBarChart,
        [Description("Pie")]
        PieChart,
        [Description("Stacked Area")]
        StackedAreaChart,
        [Description("Horizontal Bar")]
        MultiBarHorizontalChart,
        [Description("Line with focus")]
        LineWithFocusChart,
        [Description("Cumulative Line")]
        CumulativeLineChart,
       [Description("Discrete Bar")]
       DiscreteBarChart,
    };

    public enum ChartJSSerieDefinition
    {
        [Description("No Serie")]
        None,
        [Description("Scatter")]
        Scatter,
        [Description("Line")]
        Line,
        [Description("Bar")]
        Bar,
        [Description("Pie")]
        Pie,
        [Description("Polar Area")]
        PolarArea,
        [Description("Radar")]
        Radar,
    };

    public enum PlotlySerieDefinition
    {
        [Description("No Serie")]
        None,
        [Description("Scatter")]
        Scatter,
        [Description("Bar")]
        Bar,
        [Description("Pie")]
        Pie,
    };

    public enum SerieSortType
    {
        [Description("No Sort")]
        None,
        [Description("By Point")]
        Y,
        [Description("By Axis Label")]
        AxisLabel,
    };

    public enum JoinType
    {
        [Description("Inner Join")]
        Inner,
        [Description("Left Outer Join")]
        LeftOuter,
        [Description("Right Outer Join")]
        RightOuter,
        [Description("Cross Join")]
        Cross,
    };

    public enum PromptType
    {
        [Description("No prompt")]
        None,
        [Description("Prompt at execution")]
        Prompt,
        [Description("Prompt only one value")]
        PromptOneValue,
        [Description("Prompt only two values")]
        PromptTwoValues,
    }

    public enum ViewParameterType
    {
        String,
        Numeric,
        Boolean,
        Enum,
        Text,
        RootReportView
    }

    public enum ReportStatus
    {
        NotExecuted,
        Executing,
        RenderingResult,
        RenderingDisplay,
        Executed,
    }

    public enum ReportExecutionContext
    {
        DesignerReport,
        DesignerOutput,
        TaskScheduler,
        WebReport,
        WebOutput,
    }


    public enum FolderRight
    {
        [Description("No right")]
        None,
        [Description("Execute reports / View files")]
        Execute,
        [Description("Execute reports and outputs / View files")]
        ExecuteReportOuput,
        [Description("Edit schedules / View files")]
        Schedule,
        [Description("Edit reports / Manage files")]
        Edit,
    }

    public enum PersonalFolderRight
    {
        [Description("No personal folder")]
        None,
        [Description("Personal folder for files only")]
        Files,
        [Description("Personal folder for reports and files")]
        Reports,
    }


    public enum EditorRight
    {
        [Description("Cannot be selected")]
        NoSelection,
        [Description("Can be selected")]
        Selection,
    }

    public enum FailoverEmailMode
    {
        [Description("Only for the first failure")]
        First,
        [Description("For each failure")]
        All,
        [Description("Only for the last failure")]
        Last,
    }

    public enum LogMode
    {
        [Description("No log")]
        None,
        [Description("Log in one file per day")]
        OneFilePerDay,
        [Description("Log in one file per report")]
        OneFilePerReport,
        [Description("Log in one file per execution")]
        OneFilePerExecution,
    }

    public enum YesNoDefault
    {
        [Description("Default")]
        Default,
        [Description("Yes")]
        Yes,
        [Description("No")]
        No,
    }

    public enum NavigationType
    {
        Drill,
        SubReport,
        Hyperlink,
        FileDownload,
        ReportScript,
        ReportExecution
    }

    public enum ExecutionStep
    {
        [Description("Before models generation")]
        BeforeModel,
        [Description("Models generated, before rendering")]
        BeforeRendering,
        [Description("Rendering is done, before output execution")]
        BeforeOutput,
        [Description("After execution")]
        AfterExecution
    }

    public enum AuditType
    {
        Login,
        LoginFailure,
        Logout,
        ReportExecution,
        ReportExecutionError,
        ReportSave,
        FileMove,
        FileCopy,
        FileDelete,
        FolderCreate,
        FolderRename,
        FolderDelete,
        EventError,
        EventServer,
        EventLoggedUsers,
    }

    public enum TriggerType
    {
        [Description("One time")]
        Time = 1,
        [Description("Daily")]
        Daily = 2,
        [Description("Weekly")]
        Weekly = 3,
        [Description("Monthly")]
        Monthly = 4,
    }

    public enum FileServerProtocol
    {
        [Description("FTP")]
        FTP,
        [Description("SFTP")]
        SFTP,
        [Description("SCP")]
        SCP,
    }

    public enum ModelBuildStep
    {
        FillResultTable,
        BuildPages,
        BuildTables,
        BuildTotals,
        HandleCellScript,
        BuildSeries,
        FinalSort,
        BuildSubTotals,
        HandleFinalScript,
    }

    public enum FirstEnumSelection
    {
        [Description("Use selected values")]
        None,
        [Description("Select all values")]
        All,
        [Description("Select first value")]
        First,
        [Description("Select last value")]
        Last,
    }

    public enum RestrictionLayout
    {
        [Description("Select with filter")]
        SelectWithFilter,
        [Description("Select without filter")]
        SelectNoFilter,
        [Description("Radio or toggle buttons")]
        RadioToggleButton,
    }

    public enum RestrictionOperatorStyle
    {
        [Description("Visible and modifiable")]
        Visible,
        [Description("Visible with Nulls and modifiable")]
        VisibleWithNulls,
        [Description("Visible but not modifiable")]
        NotModifiable,
        [Description("Not visible")]
        NotVisible,
    }

    public enum StartupOptions
    {
        [Description("Default")]
        Default,
        [Description("Do not execute report")]
        None,
        [Description("Execute the last report")]
        ExecuteLast,
        [Description("Execute a specific report")]
        ExecuteReport,
    }
}

