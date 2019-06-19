//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using Seal.Model;
using Seal.Helpers;
using ScintillaNET;

namespace Seal.Forms
{
    public class TemplateTextEditor : UITypeEditor
    {
        public static object CurrentEntity = null; //Hack to get the current entity

        const string razorPreOutputTemplate = "@using Seal.Model\r\n@{\r\nReportOutput output = Model;\r\nstring result = \"1\"; //Set result to 0 to cancel the report.\r\n}\r\n@Raw(result)";
        const string razorPostOutputTemplate = "@using Seal.Model\r\n@{\r\nReportOutput output = Model;\r\n}";
        const string displayNameTemplate = "@using Seal.Model\r\n@{\r\nReport report = Model;\r\nstring result = System.IO.Path.GetFileNameWithoutExtension(report.FilePath) + \" \" + DateTime.Now.ToShortDateString();\r\n}\r\n@Raw(result)";

        const string razorTaskTemplate = @"@using Seal.Model
@{
    ReportTask task = Model;
    Report report = task.Report;
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    string result = ""1""; //Set result to 0 to cancel the report.
    //Or cancel report with the flag CancelReport
    //task.CancelReport = true;
    //Or disable another task
    //report.Tasks[1].Enabled = false;
}
@Raw(result)
";

        const string razorCellScriptTemplate = @"@using Seal.Model
@{
    ResultCell cell = Model;

    ReportElement element = cell.Element;
    ReportModel reportModel = element.Model;
    Report report = reportModel.Report;
    //Script executed to modify a result cell 
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
	/*
    cell.ContextRow indicates the current row (type = int)
    cell.ContextCol indicates the current column (type = int)
    cell.ContextTable is the current table, summary or data (type = ResultTable)
    cell.ContextPage is the current result page (type = ResultPage), null for SummaryTable
    cell.ContextIsSummaryTable indicates if the current table is the SummaryTable
    cell.ContextIsPageTable indicates if the current table is the PageTable (only Title cells are parsed for PageTable)
    cell.ContextCurrentLine is the current line of the table (type = ResultCell[]), null for SummaryTable
    cell.IsTotal indicates if it is a total cell
    cell.IsTotalTotal indicates if it is a total of total cell
    cell.IsSubTotal indicates if it is a sub-total cell
    cell.IsTitle indicates if it is a title cell
    cell.IsSerie indicates if the cell is used for series, in this case, ContextRow and ContextCol is the common row and col used for the dimension values
	
    cell.DisplayValue (type = string) is the display value
    cell.DoubleValue (type = double?) is the cell value for numeric
    cell.DateTimeValue (type = DateTime? ) is the cell value for date time

	To customize your calculation and cell display, you can assign
	cell.Value (type = object) is the cell value: string, double or DateTime
	cell.FinalValue (type = string) is the final string used for the table cell
	cell.FinalCssStyle (type = string) is the final CSS style used for the table cell
	cell.FinalCssClass (type = string) is the final CSS classes used for the table cell, could be one or many Bootstrap classes
	*/
	}
";

        static readonly Tuple<string, string>[] razorCellScriptSamples =
        {
            new Tuple<string, string>(
                "Simple format",
@"if (cell.IsTitle)
	{
        if (cell.ContextIsSummaryTable) {
    		cell.FinalCssStyle = ""font-weight:bold;"";
	    	cell.FinalCssClass = ""warning lead""; 
        }
        else if (cell.ContextIsPageTable) {
	    	cell.FinalCssClass = ""info""; 
        }
        else {
    		cell.FinalCssStyle = ""font-weight:bold;"";
	    	cell.FinalCssClass = ""warning""; 
        }
	}
    else {
        if (cell.IsSubTotal) {
    		cell.FinalCssClass = ""danger"";
        }
        else if (cell.IsTotal) {
    		cell.FinalCssClass = ""info"";
        }
        else {
            cell.FinalCssClass = ""success right""; //These may be Bootstrap classes: active success info warning danger etc...
        }
    }
"
                ),
            new Tuple<string, string>(
                "Display negative values in red and bold",
@"if (cell.DoubleValue < 0)
	{
		cell.FinalCssStyle = ""font-weight:bold;"";
		cell.FinalCssClass = ""danger lead text-right""; //These are Bootstrap classes
	}
"
                ),
            new Tuple<string, string>(
                "Calculate a progression",
@"if (cell.IsSerie && cell.ContextRow > 0 && cell.ContextCol == -1)
	{
		//For serie, ContextRow and ContextCol is the common row and col used for the dimension values
		//In this case, we use the values of the Total (column before last)
		var colIndex = cell.ContextCurrentLine.Length - 3;		
		var previousValue = cell.ContextTable[cell.ContextRow-1,colIndex].DoubleValue;
		var currentValue = cell.ContextTable[cell.ContextRow,colIndex].DoubleValue;
		//Calculate the progression
		cell.Value = (currentValue - previousValue)/previousValue;
	}
    else if (cell.ContextRow == cell.ContextTable.RowCount - 1)
	{
		//No progression for last table line (summary or data)
		cell.Value = null;		
	}
	else if (!cell.IsTitle && cell.ContextRow > 0)
	{
        //Normal case for DataTable and SummaryTable
		var previousValue = cell.ContextTable[cell.ContextRow-1,cell.ContextCol-1].DoubleValue;
		var currentValue = cell.ContextTable[cell.ContextRow,cell.ContextCol-1].DoubleValue;
		cell.Value = 100*(currentValue - previousValue)/previousValue;
	}
"
                ),
            new Tuple<string, string>(
                "Calculate a running total",
@"if (cell.IsSerie && cell.ContextRow > 0 && cell.ContextCol == -1)
	{
		//For serie, ContextRow and ContextCol is the common row and col used for the dimension values
		//In this case, we use the values of the Total (column before last)
		var colIndex = cell.ContextCurrentLine.Length - 3;		
		var previousValue = cell.ContextTable[cell.ContextRow-1,colIndex].DoubleValue;
		var currentValue = cell.ContextTable[cell.ContextRow,colIndex].DoubleValue;
		//Calculate the running total
		cell.Value = currentValue + (previousValue != null ? previousValue : 0);
	}
    else if (cell.ContextRow == cell.ContextTable.RowCount - 1)
	{
		//No running totals for last table line (summary or data)
		cell.Value = null;		
	}
	else if (!cell.IsTitle && cell.ContextRow > 0)
	{
        //Normal case for DataTable and SummaryTable
		var previousValue = cell.ContextTable[cell.ContextRow-1,cell.ContextCol].DoubleValue;
		var currentValue = cell.ContextTable[cell.ContextRow,cell.ContextCol-2].DoubleValue;
		//Calculate the running total
		cell.Value = currentValue + (previousValue != null ? previousValue : 0);
    }	
"
                ),
            new Tuple<string, string>(
                "Display the cell context",
@"cell.FinalValue = string.Format(""Row={0} Col={1} Title={2} Summary={3}"", cell.ContextRow, cell.ContextCol, cell.IsTitle, cell.ContextIsSummaryTable);
"
                ),
        };


        const string razorTableDefinitionScriptTemplate = @"@using Seal.Model
@using System.Data
@{
    MetaTable metaTable = Model;
	ReportExecutionLog log = metaTable;

    //Script executed to define the result table columns that will be loaded by the 'Load Script'
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    DataTable table = new DataTable();
    table.Columns.Add(new DataColumn(""numberCol"", typeof(int)));
    table.Columns.Add(new DataColumn(""stringCol"", typeof(string)));
    table.Columns.Add(new DataColumn(""dateCol"", typeof(DateTime)));
    metaTable.NoSQLTable = table;
    log.LogMessage(""{0} column(s) defined"", table.Columns.Count);
}
";

        const string razorTableLoadScriptTemplate = @"@using Seal.Model
@using System.Data
@{
    MetaTable metaTable = Model;
    DataTable table = metaTable.NoSQLTable;
	ReportExecutionLog log = metaTable;
    ReportModel reportModel = metaTable.NoSQLModel;
    Report report = (reportModel != null ? reportModel.Report : null);
    List<ReportRestriction> restrictions = (report != null ? report.ReportRestrictions : null);

    //Default Script executed to fill the model result table from a non SQL source (if the model 'Load Script' is empty)
    //Insert values in the table, values must match the table columns defined in 'Definition Script'
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    log.LogMessage(""Adding table rows with the default table 'Load Script'..."");
    table.Rows.Add(123, ""a string value"", DateTime.Now);
    table.Rows.Add(124, ""another string value"", DateTime.Now);
    log.LogMessage(""{0} record(s) loaded"", table.Rows.Count);
}
";

        const string razorModelLoadScriptTemplate = @"@using Seal.Model
@using System.Data
@{
    ReportModel model = Model;
    DataTable table = model.ResultTable;
	ReportExecutionLog log = model.Report;

    //Script executed to modify the model result table after it has been loaded from the database
    //Modify values in the current result table, rows can also be added and deleted.
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    log.LogMessage(""Modifying table with the model 'Post Load Script'..."");
    foreach (DataRow row in table.Rows)
    {
        /* e.g. Change the values of the column displaying the City element
		ReportElement element = model.GetRestrictionByName(""City"");
		if (element != null && !row.IsNull(element.SQLColumnName)) {
			row[element.SQLColumnName] = string.Format(""New value for '{0}'"", row[element.SQLColumnName]);
		}
        */
    }
}
";

        const string razorModelLoadScriptTemplateNoSQL = @"@using Seal.Model
@using System.Data
@{
    ReportModel model = Model;
    DataTable table = model.ResultTable;
	ReportExecutionLog log = model.Report;

    //Script executed to fill the model result table from a non SQL source
    //Insert values in the table, values must match the table columns defined in the source table 'Definition Script'
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    log.LogMessage(""Adding table rows with the model 'Load Script'..."");
    table.Rows.Add(123, ""a string value"", DateTime.Now);
    table.Rows.Add(124, ""another string value"", DateTime.Now);
    log.LogMessage(""{0} record(s) loaded"", table.Rows.Count);
}
";

        const string razorModelPreLoadScriptTemplateNoSQL = @"@using Seal.Model
@using System.Data
@{
    ReportModel model = Model;
	ReportExecutionLog log = model.Report;
    List<ReportElement> elements = model.Elements;
    List<ReportRestriction> restrictions = model.Restrictions;

    //Script executed before the model result table is loaded from the database
    //You can change the model restrictions or elements before the load of the table
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    log.LogMessage(""Processing the model 'Pre Load Script'"");
    //restrictions[0].Value1 = ""1994""; 
    //restrictions[0].Date1 = DateTime.Now.AddYears(-20);
    //model.GetRestrictionByName(""Order Year"").Value1 = ""2015"";
    //model.GetRestrictionByName(""Order Date"").Date1 = model.GetRestrictionByName(""Required Date"").Date1; 
    //model.GetRestrictionByName(""Category"").EnumValues.Add(""beverages""); 
}
";


        const string razorTableFinalScriptTemplate = @"@using Seal.Model
@using System.Data
@{
    ReportModel model = Model;
 	ReportExecutionLog log = model.Report;

    //Final script executed to modify the model result tables after their generations
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    log.LogMessage(""Modifying result values with the 'Final Script'..."");
    ResultTable summaryTable = model.SummaryTable;
    foreach (ResultPage page in model.Pages)
    {
        ResultTable dataTable = page.DataTable;
    	ResultTable pageTable = page.PageTable;

        /* e.g to change the last line of the Data Tables
        dataTable[dataTable.RowCount - 1, 0].Value = ""Maximum"";
		for (int i=0;i< dataTable.ColumnCount; i++)
	    {
		    string cssclass =  ""danger;"" + (i > 0 ? ""text-align:right;"" : """");
		    dataTable[dataTable.Lines.Count - 1, i].FinalCssClass = cssclass;
	    }
        */
    }

}
";

        const string razorTasksTemplate = @"@using System.Text
@functions {
    //Before execution, this script will be added at the end of all task scripts...
    public string MyConvertString(string input) {
        return input.Replace(""__"",""_"");
    }
}
";

        const string razorInitScriptTemplate = @"@using Seal.Model
@{
    Report report = Model;
	ReportExecutionLog log = report;

    //Script executed when the report is initialized
    log.LogMessage(""Executing Init Script"");
    //report.Models[0].Restrictions[0].Value1 = ""1994""; 
    //report.Models[0].Restrictions[0].Date1 = DateTime.Now.AddYears(-20);
    //report.Models[0].GetRestrictionByName(""Order Year"").Value1 = ""2015"";
    //report.Models[0].GetRestrictionByName(""Category"").EnumValues.Add(""1"");

    //report.DisplayName = System.IO.Path.GetFileNameWithoutExtension(report.FilePath) + ""-"" + DateTime.Now.ToShortDateString();
    
    //Set the first value of an enum
    //var enums = report.Models[0].Source.MetaData.Enums.FirstOrDefault(i=>i.Name == ""Category"");
    //report.Models[0].GetRestrictionByName(""Category"").EnumValues.Add(enums.Values[0].Id);

    //Change view parameter to display the information Tab
    //report.ExecutionView.GetParameter(""information_button"").BoolValue = true;
}
";

        const string razorConfigurationReportCreationScriptTemplate = @"@using Seal.Model
@{
    Report report = Model;

    //Script executed when the report is created
    
    //Remove the default CSV view
    //if (report.Views.Count > 1) {
	//   report.Views.Remove(report.Views[1]);
    //}
   
    //Change view parameter to display the information Tab
	//report.ExecutionView.InitParameters(false);
    //var parameter = report.ExecutionView.GetParameter(""information_button"");
	//if (parameter != null) {
	//	parameter.BoolValue = true;	
	//}	
}
";


        const string razorSourceInitScriptTemplate = @"@using Seal.Model
@using System.Data
@{
    MetaSource source = Model;
	ReportExecutionLog log = null;
    Report report = null;
    if (source is ReportSource) {
        log = ((ReportSource) source).Report;
        report = ((ReportSource) source).Report;
    }

    //Script executed when the report is initialized at execution
    if (log != null) {
        log.LogMessage(""Executing 'Init Script' for source: "" + source.Name +"" ..."");

        //...
    }
}
";

        const string razorConfigurationInitScriptTemplate = @"@using Seal.Model
@using System.Data
@{
    Report report = Model;
	ReportExecutionLog log = Model;

    //Script executed when the report is initialized at execution
    if (log != null) {
        log.LogMessage(""Executing configuration 'Init Script'"");

        //...
    }

    // e.g. to change the Thousand Separator or the Decimal Separator
    	//report.ExecutionView.CultureInfo.NumberFormat.NumberGroupSeparator = ""'"";
        //report.ExecutionView.CultureInfo.NumberFormat.NumberDecimalSeparator = ""."";	    
}
";
        static readonly Tuple<string, string>[] tasksSamples =
        {
            new Tuple<string, string>(
                "Refresh Data Sources enumerated lists",
@"ReportTask task = Model;
    var helper = new TaskHelper(task);
    helper.RefreshRepositoryEnums();
"
                ),
            new Tuple<string, string>(
                "Display the report input values",
@"ReportTask task = Model;
    Report report = task.Report;
    foreach (ReportRestriction restr in report.InputValues) {
        report.LogMessage(""[{0}]={1} Value={2}"", restr.DisplayNameEl, restr.DisplayText, restr.FirstValue); //You can use restr.Value1, restr.DisplayValue1, restr.FinalDate1, restr.EnumValues[0], restr.EnumDisplayValue, restr.FirstStringValue, restr.FirstNumericValue, restr.FirstDateValue
    }
    //Use also:
    //ReportRestriction restr = report.GetInputValueByName(""AnInputName"");
}
"
            ),
            new Tuple<string, string>(
                "Load a table from an Excel file, may need ODBC Office 2007 Drivers",
@"ReportTask task = Model;
	var helper = new TaskHelper(task);
	//helper.DatabaseHelper.ExcelOdbcDriver = ""Driver={{Microsoft Excel Driver (*.xls, *.xlsx, *.xlsm, *.xlsb)}}; DBQ={0}"";
	//helper.DatabaseHelper.ExcelOdbcDriver = ""Driver={{Microsoft Excel Driver (*.xls)}};DBQ={0}"";
	helper.LoadTableFromExcel(
        @""c:\temp\loadFolder"", //Folder used to store the files processed 
        @""c:\temp\excelFile.xlsx"", //source Excel file path
        ""ExcelTabName"", //source Excel Tab name
        ""DestinationTableName"", //destination table name
        false //if true, the table is loaded for all connections defined in the Source
    );

    //Several Tabs can be loaded using array of strings and LoadTablesFromExcel()
	helper.LoadTablesFromExcel(
        @""c:\temp\loadFolder"", //Folder used to store the files processed 
        @""c:\temp\excelFile.xlsx"", //source Excel file path
        new string[] {""ExcelTabName1"", ""ExcelTabName2"", ""ExcelTabName3""}, //array of source Excel Tab Name
        new string[] {""DestinationTableName1"", ""DestinationTableName2"", ""DestinationTableName3""}, //array of destination table name
        false /* true to load in all connections */);
    }
"
                ),
            new Tuple<string, string>(
                "Load a table from a CSV file",
@"ReportTask task = Model;
    var helper = new TaskHelper(task);
	helper.LoadTableFromCSV(
        @""c:\temp\loadFolder"", //Folder used to store the files processed 
        @""c:\temp\aCSVFile.csv"", //source CSV file path
        ""DestinationTableName"", //destination table name
        null, //optional CSV separator (e.g. ',') 
        false, //optional, if true, the table is loaded for all connections defined in the Source
        true //optional, if true, the MS Visual Basic Parser is used (to be used if values contain new line characters) otherwise the standard parser is used
    );
"
                ),
            new Tuple<string, string>(
                "Load a table from a source table located in another source defined in the Repository",
@"ReportTask task = Model;
    var helper = new TaskHelper(task);
	helper.LoadTableFromDataSource(
        ""DataSourceName"", //the name of the source Data Source defined in the Repository
        ""SourceSelectStatement"", //SQL Select Statement to get the source table
        ""DestinationTableName"", //destination table name
        false, //if true, the table is loaded for all connections defined in the Source
        """", //optional SQL Select Statement to get a Check table from the source connection
        """" //optional SQL Select Statement to get a Check table from the destination connection, 
        //if both source and destination Check tables are identicals, the table is not loaded
    );
"
                ),
            new Tuple<string, string>(
                "Load a table from a source table located in an external data source defined with a connection string",
@"ReportTask task = Model;
    var helper = new TaskHelper(task);
	helper.LoadTableFromExternalSource(
        ""SourceConnectionString"", //full connection string used to load the source table
        ""SourceSelectStatement"", //SQL Select Statement to get the source table
        ""DestinationTableName"", //destination table name
        false, //if true, the table is loaded for all connections defined in the Source
        """", //optional SQL Select Statement to get a Check table from the source connection
        """" //optional SQL Select Statement to get a Check table from the destination connection, 
        //if both source and destination Check tables are identicals, the table is not loaded
    );
"
                ),
            new Tuple<string, string>(
                "Query or update the database",
@"ReportTask task = Model;
    var helper = new TaskHelper(task);
	string name = (string) helper.ExecuteScalar(""select LastName from employees"");
    helper.LogMessage(""Name="" + name);
    helper.ExecuteNonQuery(
        ""update employees set LastName = '' where 1=0"", //SQL statement to execute 
        false //if true, the statement is executed for all connections defined in the Source
    );
"
                ),
            new Tuple<string, string>(
                "Execute a program and display Standard Output and Errors",
@"ReportTask task = Model;
    var helper = new TaskHelper(task);
	helper.ExecuteProcess(@""executablePath"");
"
                ),
            new Tuple<string, string>(
                "Execute MS SQLServer Scripts (files *.sql) located in a directory",
@"ReportTask task = Model;
    var helper = new TaskHelper(task);
	helper.ExecuteMSSQLScripts(
        @""scriptsDirectory"",
        false, //if true, the scripts are executed for all connections defined in the Source
        true, //if false, the execution continues even if an error occurs
        11 //error class level to consider an error versus information/warning
    );
"
                ),
            new Tuple<string, string>(
                "Database Helper configurations...",
@"ReportTask task = Model;
    var helper = new TaskHelper(task);
    var dbHelper = helper.DatabaseHelper;
	// configuration of the database helper may be changed to control the table creation and load...	
	dbHelper.ColumnCharType = """"; //type of table created when text is detected
	dbHelper.ColumnIntegerType = """"; //type of table created when integer is detected
	dbHelper.ColumnNumericType = """"; //type of table created when numeric is detected
	dbHelper.ColumnDateTimeType = """"; //type of table created when datetime is detected
	dbHelper.ColumnCharLength = 0; //char length, 0 means auto size (or max for SQLServer)
	dbHelper.InsertBurstSize = 500; //number of insert per SQL command when inserting records in the destination table
    dbHelper.MaxDecimalNumber = -1; //if >= 0, the maximum number of decimals for numeric values in the INSERT command
	dbHelper.LoadBurstSize = 0; //number of records to load from the table (to be used with LoadSortColumn), 0 means to load all records in one query, otherwise several queries are performed
	dbHelper.LoadSortColumn = """"; //name of the column used to sort if LoadBurstSize is specified, 
    dbHelper.UseDbDataAdapter = false; //If true, the DbDataAdapter.Fill() is used instead of the DataReader
	dbHelper.ExcelOdbcDriver = ""Driver={{Microsoft Excel Driver (*.xls, *.xlsx, *.xlsm, *.xlsb)}}; DBQ={0}""; //Excel ODBC Driver used to load table from Excel
	dbHelper.DefaultEncoding = System.Text.Encoding.Default; //encoding used to read the CSV file 
	dbHelper.TrimText = true; //if true, all texts are trimmed when inserted in the destination table
	dbHelper.RemoveCrLf = false; //if true, the CrLf are removed from text when inserted in the destination table
	dbHelper.DebugMode = false; //if true, full debug traces are logged in dbHelper.DebugLog
	dbHelper.SelectTimeout = 0; //timeout for the select statement, 0 means no timeout
	dbHelper.InsertStartCommand = """"; //prefix text for the insert command
	dbHelper.InsertEndCommand = """"; //suffix for the insert command
"
                ),
            new Tuple<string, string>(
                "Database Helper methods overwrites...",
@"ReportTask task = Model;
    var helper = new TaskHelper(task);
    var dbHelper = helper.DatabaseHelper;
	// methods of the helper can be modified before the load...	

    dbHelper.MyGetTableCreateCommand = new CustomGetTableCreateCommand(delegate(DataTable table) {
        //return RootGetTableCreateCommand(table);
        //Root implementation may be the following...
        StringBuilder result = new StringBuilder();
        foreach (DataColumn col in table.Columns)
        {
            if (result.Length > 0) result.Append(',');
            result.AppendFormat(""{0} "", dbHelper.GetTableColumnName(col));
            result.Append(dbHelper.GetTableColumnType(col));
            result.Append("" NULL"");
        }
        return string.Format(""CREATE TABLE {0} ({1})"", dbHelper.CleanName(table.TableName), result);
    });

    dbHelper.MyGetTableColumnNames = new CustomGetTableColumnNames(delegate(DataTable table) {
        //return dbHelper.RootGetTableColumnNames(table);
        //Root implementation may be the following...
        StringBuilder result = new StringBuilder();
        foreach (DataColumn col in table.Columns)
        {
            if (result.Length > 0) result.Append(',');
            result.AppendFormat(""{0}"", dbHelper.GetTableColumnName(col));
        }
        return result.ToString();
    });

    dbHelper.MyGetTableColumnName = new CustomGetTableColumnName(delegate(DataColumn col) {
        //return dbHelper.RootGetTableColumnName(col);
        //Root implementation may be the following...
        var result = dbHelper.CleanName(col.ColumnName);
        return (dbHelper.DatabaseType == DatabaseType.MSSQLServer) ? ""["" + result + ""]"" : result;
    });

    dbHelper.MyGetTableColumnType = new CustomGetTableColumnType(delegate(DataColumn col) {
        //if (col.ColumnName==""aColName"") return ""bigint""; 
        return dbHelper.RootGetTableColumnType(col);
    });

    dbHelper.MyGetTableColumnValues = new CustomGetTableColumnValues(delegate(DataRow row, string dateTimeFormat) {
        return dbHelper.RootGetTableColumnValues(row, dateTimeFormat);
    });

	dbHelper.MyGetTableColumnValue = new CustomGetTableColumnValue(delegate(DataRow row, DataColumn col, string dateTimeFormat) {
        //return dbHelper.RootGetTableColumnValue(row, col, datetimeFormat);
        //Root implementation may be the following...
        StringBuilder result = new StringBuilder();
        if (row.IsNull(col))
        {
            result.Append(""NULL"");
        }
        else if (dbHelper.IsNumeric(col))
        {
            result.AppendFormat(row[col].ToString().Replace(',', '.'));
        }
        else if (col.DataType.Name == ""DateTime"" || col.DataType.Name == ""Date"")
        {
            result.Append(Helper.QuoteSingle(((DateTime) row[col]).ToString(dateTimeFormat)));
        }
        else
        {
            string res = row[col].ToString();
            if (dbHelper.TrimText) res = res.Trim();
            if (dbHelper.RemoveCrLf) res = res.Replace(""\r"", "" "").Replace(""\n"", "" "");
            result.Append(Helper.QuoteSingle(res));
        }
        return result.ToString();
    });

    dbHelper.MyLoadDataTable = new CustomLoadDataTable(delegate(string connectionString, string sql) {
        return new DataTable(); //Check current source implementation in TaskDatabaseHelper.cs
    });

    dbHelper.MyLoadDataTableFromExcel = new CustomLoadDataTableFromExcel(delegate(string excelPath, string tabName) {
        return new DataTable(); //Check current source implementation in TaskDatabaseHelper.cs
    });

    dbHelper.MyLoadDataTableFromCSV = new CustomLoadDataTableFromCSV(delegate(string csvPath, char? separator) {
        return new DataTable(); //Check current source implementation in TaskDatabaseHelper.cs
    });
"
                ),
        };

        
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (context.Instance is ReportView && context.PropertyDescriptor.IsReadOnly) return UITypeEditorEditStyle.None;
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var svc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if (svc != null)
            {
                var frm = new TemplateTextEditorForm();

                string template = "";
                string valueToEdit = (value == null ? "" : value.ToString());
                if (context.Instance is ReportView)
                {
                    var view = context.Instance as ReportView;
                    if (context.PropertyDescriptor.Name == "CustomTemplate")
                    {
                        if (string.IsNullOrEmpty(valueToEdit)) valueToEdit = view.ViewTemplateText;
                        template = view.Template.Text.Trim();
                        frm.Text = "Edit custom template";
                        frm.ObjectForCheckSyntax = view.Report;
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "CustomConfiguration")
                    {
                        if (string.IsNullOrEmpty(valueToEdit)) valueToEdit = view.Template.Configuration;
                        template = view.Template.Configuration.Trim();
                        frm.Text = "Edit template configuration";
                        frm.ObjectForCheckSyntax = view.Template;
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                }
                else if (context.Instance is ReportViewPartialTemplate)
                {
                    var pt = context.Instance as ReportViewPartialTemplate;
                    var templateText = pt.View.Template.GetPartialTemplateText(pt.Name);
                    if (string.IsNullOrEmpty(valueToEdit)) valueToEdit = templateText;
                    template = templateText;
                    frm.Text = "Edit custom partial template";
                    frm.ObjectForCheckSyntax = pt.View;
                    ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                }
                else if (context.Instance is ReportTask)
                {
                    template = razorTaskTemplate;
                    frm.ObjectForCheckSyntax = context.Instance;
                    frm.Text = "Edit task script";
                    ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    List<string> samples = new List<string>();
                    foreach (var sample in tasksSamples)
                    {
                        samples.Add("@using Seal.Model\r\n@using Seal.Helpers\r\n@using System.Data\r\n@{\r\n\t//" + sample.Item1 + "\r\n\t" + sample.Item2 + "}\r\n|" + sample.Item1);
                    }
                    frm.SetSamples(samples);
                }
                else if (context.Instance is ReportOutput)
                {
                    if (context.PropertyDescriptor.Name == "PreScript") template = razorPreOutputTemplate;
                    else if (context.PropertyDescriptor.Name == "PostScript") template = razorPostOutputTemplate;
                    frm.ObjectForCheckSyntax = context.Instance;
                    frm.Text = "Edit output script";
                    ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                }
                else if (context.Instance is Parameter || context.Instance is ParametersEditor)
                {
                    Parameter parameter = context.Instance is Parameter ? context.Instance as Parameter : ((ParametersEditor)context.Instance).GetParameter(context.PropertyDescriptor.Name);
                    if (parameter != null)
                    {
                        template = parameter.ConfigValue;
                        frm.Text = parameter.DisplayName;
                        ScintillaHelper.Init(frm.textBox, (string.IsNullOrEmpty(parameter.EditorLanguage) ? "" : parameter.EditorLanguage));
                        if (parameter.TextSamples != null) frm.SetSamples(parameter.TextSamples.ToList());
                    }
                }
                else if (context.Instance.GetType().ToString() == "SealPdfConverter.PdfConverter")
                {
                    string language = "cs";
                    SealPdfConverter converter = (SealPdfConverter)context.Instance;
                    converter.ConfigureTemplateEditor(frm, context.PropertyDescriptor.Name, ref template, ref language);
                    ScintillaHelper.Init(frm.textBox, language);
                }
                else if (context.Instance.GetType().ToString() == "SealExcelConverter.ExcelConverter")
                {
                    string language = "cs";
                    SealExcelConverter converter = (SealExcelConverter)context.Instance;
                    converter.ConfigureTemplateEditor(frm, context.PropertyDescriptor.Name, ref template, ref language);
                    ScintillaHelper.Init(frm.textBox, language);
                }
                else if (context.Instance is ViewFolder)
                {
                    if (context.PropertyDescriptor.Name == "DisplayName")
                    {
                        template = displayNameTemplate;
                        frm.ObjectForCheckSyntax = ((ReportComponent)context.Instance).Report;
                        frm.Text = "Edit display name script";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "InitScript")
                    {
                        template = razorInitScriptTemplate;
                        frm.ObjectForCheckSyntax = ((ReportComponent)context.Instance).Report;
                        frm.Text = "Edit the script executed when the report is initialized";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                }
                else if (context.Instance is ReportElement)
                {
                    ReportElement element = context.Instance as ReportElement;
                    if (context.PropertyDescriptor.Name == "CellScript")
                    {
                        frm.Text = "Edit custom script for the cell";
                        template = razorCellScriptTemplate;
                        List<string> samples = new List<string>();
                        foreach (var sample in razorCellScriptSamples)
                        {
                            samples.Add("@using Seal.Model\r\n@{\r\n\t//" + sample.Item1 + "\r\n\tResultCell cell=Model;\r\n\tReportElement element = cell.Element;\r\n\tReportModel reportModel = element.Model;\r\n\tReport report = reportModel.Report;\r\n\t" + sample.Item2 + "}\r\n|" + sample.Item1);
                        }
                        frm.SetSamples(samples);

                        frm.ObjectForCheckSyntax = new ResultCell();
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "SQL")
                    {
                        frm.Text = "Edit custom SQL";
                        ScintillaHelper.Init(frm.textBox, Lexer.Sql);
                        template = element.RawSQLColumn;
                        List<string> samples = new List<string>();
                        samples.Add(element.RawSQLColumn);
                        if (!string.IsNullOrEmpty(element.SQL) && !samples.Contains(element.SQL)) samples.Add(element.SQL);
                        frm.SetSamples(samples);
                        frm.textBox.WrapMode = WrapMode.Word;
                    }
                    else if (context.PropertyDescriptor.Name == "CellCss")
                    {
                        frm.Text = "Edit custom CSS";
                        ScintillaHelper.Init(frm.textBox, Lexer.Css);
                        List<string> samples = new List<string>();
                        samples.Add("text-align:right;");
                        samples.Add("text-align:center;");
                        samples.Add("text-align:left;");
                        samples.Add("font-style:italic;");
                        samples.Add("font-weight:bold;color:red;background-color:yellow;");
                        samples.Add("color:green;text-align:right;|color:black;|font-weight:bold;color:red;text-align:right;");
                        samples.Add("white-space: nowrap;");
                        frm.SetSamples(samples);
                        frm.textBox.WrapMode = WrapMode.Word;
                    }
                }
                else if (context.Instance is MetaColumn)
                {
                    if (context.PropertyDescriptor.Name == "Name")
                    {
                        frm.Text = "Edit column name";
                        ScintillaHelper.Init(frm.textBox, Lexer.Sql);
                        frm.textBox.WrapMode = WrapMode.Word;
                    }
                }
                else if (context.Instance is SealSecurity)
                {
                    if (context.PropertyDescriptor.Name == "Script" || context.PropertyDescriptor.Name == "ProviderScript")
                    {
                        template = ((SealSecurity)context.Instance).ProviderScript;
                        frm.ObjectForCheckSyntax = new SecurityUser(null);
                        frm.Text = "Edit security script";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                }
                else if (context.Instance is MetaTable)
                {
                    if (context.PropertyDescriptor.Name == "DefinitionScript")
                    {
                        template = razorTableDefinitionScriptTemplate;
                        frm.ObjectForCheckSyntax = context.Instance;
                        frm.Text = "Edit the script to define the table";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "LoadScript")
                    {
                        template = razorTableLoadScriptTemplate;
                        frm.ObjectForCheckSyntax = context.Instance;
                        frm.Text = "Edit the default script to load the table";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                }
                else if (context.Instance is ReportModel)
                {
                    if (context.PropertyDescriptor.Name == "PreLoadScript")
                    {
                        template = razorModelPreLoadScriptTemplateNoSQL;
                        frm.ObjectForCheckSyntax = context.Instance;
                        frm.Text = "Edit the script executed before table load";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "FinalScript")
                    {
                        template = razorTableFinalScriptTemplate;
                        frm.ObjectForCheckSyntax = context.Instance;
                        frm.Text = "Edit the final script executed for the model";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "LoadScript")
                    {
                        if (((ReportModel)context.Instance).Source.IsNoSQL)
                        {
                            frm.Text = "Edit the script executed after table load";
                            template = razorModelLoadScriptTemplateNoSQL;
                        }
                        else
                        {
                            frm.Text = "Edit the script to load the table";
                            template = razorModelLoadScriptTemplate;
                        }
                        frm.ObjectForCheckSyntax = context.Instance;
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                }
                else if (context.Instance is MetaSource && context.PropertyDescriptor.Name == "InitScript")
                {
                    template = razorSourceInitScriptTemplate;
                    frm.ObjectForCheckSyntax = context.Instance;
                    frm.Text = "Edit the init script of the source";
                    ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                }
                else if (context.Instance is TasksFolder)
                {
                    if (context.PropertyDescriptor.Name == "TasksScript")
                    {
                        template = razorTasksTemplate;
                        frm.ObjectForCheckSyntax = new ReportTask();
                        if (CurrentEntity is Report)
                        {
                            frm.ScriptHeader = ((Report)CurrentEntity).Repository.Configuration.CommonScriptsHeader;
                            frm.ScriptHeader += ((Report)CurrentEntity).Repository.Configuration.TasksScript;
                            frm.ScriptHeader += ((Report)CurrentEntity).CommonScriptsHeader;
                        }
                        frm.Text = "Edit the script that will be added to all task scripts";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                }
                else if (context.Instance is CommonScript)
                {
                    template = CommonScript.RazorTemplate;
                    frm.Text = "Edit the script that will be added to all scripts executed for the report.";
                    if (CurrentEntity is SealServerConfiguration)
                    {
                        //common script from configuration
                        frm.ScriptHeader = ((SealServerConfiguration)CurrentEntity).GetCommonScriptsHeader((CommonScript)context.Instance);
                    }
                    if (CurrentEntity is Report)
                    {
                        //common script from report
                        frm.ScriptHeader = ((Report)CurrentEntity).Repository.Configuration.CommonScriptsHeader;
                        frm.ScriptHeader += ((Report)CurrentEntity).GetCommonScriptsHeader((CommonScript)context.Instance);

                    }
                    frm.ObjectForCheckSyntax = CurrentEntity;
                    ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                }
                else if (context.Instance is SealServerConfiguration)
                {
                    //use report tag to store current config
                    var report = new Report();
                    report.Tag = context.Instance;
                    if (context.PropertyDescriptor.Name == "InitScript")
                    {
                        template = razorConfigurationInitScriptTemplate;
                        frm.ObjectForCheckSyntax = report;
                        frm.Text = "Edit the root init script";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "TasksScript")
                    {
                        template = razorTasksTemplate;
                        frm.ScriptHeader = ((SealServerConfiguration)context.Instance).CommonScriptsHeader;
                        frm.ObjectForCheckSyntax = new ReportTask();
                        frm.Text = "Edit the script that will be added to all task scripts";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "ReportCreationScript")
                    {
                        template = razorConfigurationReportCreationScriptTemplate;
                        frm.ObjectForCheckSyntax = report;
                        frm.Text = "Edit the script executed when a new report is created";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                }

                if (!string.IsNullOrEmpty(template) && string.IsNullOrWhiteSpace(valueToEdit) && !context.PropertyDescriptor.IsReadOnly)
                {
                    valueToEdit = template;
                }

                //Reset button
                if (!string.IsNullOrEmpty(template) && !context.PropertyDescriptor.IsReadOnly) frm.SetResetText(template);

                frm.textBox.Text = valueToEdit.ToString();

                if (context.PropertyDescriptor.IsReadOnly)
                {
                    frm.textBox.ReadOnly = true;
                    frm.okToolStripButton.Visible = false;
                    frm.cancelToolStripButton.Text = "Close";
                }
                frm.checkSyntaxToolStripButton.Visible = (frm.ObjectForCheckSyntax != null);

                if (svc.ShowDialog(frm) == DialogResult.OK)
                {
                    if (string.IsNullOrEmpty(template)) template = "";

                    if (frm.textBox.Text.Trim() != template.Trim() || string.IsNullOrEmpty(template)) value = frm.textBox.Text;
                    else if (frm.textBox.Text.Trim() == template.Trim() && !string.IsNullOrEmpty(template)) value = "";
                }
            }
            return value;
        }
    }
}
