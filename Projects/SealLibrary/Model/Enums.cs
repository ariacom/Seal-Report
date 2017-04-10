//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        [Description("Percentage 0 Decimal (100 %)")]
        Percentage0,
        [Description("Percentage 2 Decimals (100.00 %)")]
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
        [Description("=")]
        Equal,
        [Description("!=")]
        NotEqual,
        [Description(">")]
        Greater,
        [Description(">=")]
        GreaterEqual,
        [Description("<")]
        Smaller,
        [Description("<=")]
        SmallerEqual,
        [Description("Between")]
        Between,
        [Description("Not Between")]
        NotBetween,
        [Description("Contains")]
        Contains,
        [Description("Not Contains")]
        NotContains,
        [Description("Starts With")]
        StartsWith,
        [Description("Ends With")]
        EndsWith,
        [Description("Is Empty")]
        IsEmpty,
        [Description("Is Not Empty")]
        IsNotEmpty,
        [Description("Is Null")]
        IsNull,
        [Description("Is Not Null")]
        IsNotNull,
        [Description("Value Only")]
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
        [Description("This Year")]
        ThisYear,
    }

    public enum SerieDefinition
    {
        [Description("No Serie")]
        None,
        [Description("Microsoft Serie")]
        Serie,
        Axis,
        Splitter,
        [Description("Splitter for Both Axes")]
        SplitterBoth,
        [Description("NVD3 Serie")]
        NVD3Serie,
    };

    public enum NVD3SerieDefinition
    {
        [Description("Point")]
        ScatterChart,
        [Description("Pie")]
        PieChart,
        [Description("Line")]
        Line,
        [Description("Bar")]
        MultiBarChart,
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
    }

    public enum ViewParameterType
    {
        String,
        Numeric,
        Boolean,
        Enum,
        Text,
    }

    public enum ViewParameterCategory
    {
        General,
        DataTables,
        NVD3,
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

    public enum PublicationType
    {
        [Description("Execute only reports (no output)")]
        ExecuteOnly,
        [Description("Execute reports and outputs")]
        ExecuteOutput,
    }

    public enum FolderRight
    {
        [Description("No right")]
        None,
        [Description("Execute reports")]
        Execute,
        [Description("Schedule reports")]
        Schedule,
        [Description("Edit reports")]
        Edit,
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

}
