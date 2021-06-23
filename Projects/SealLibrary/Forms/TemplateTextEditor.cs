//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
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
using System.Text;
using System.Data.OleDb;
using System.Data;
using System.Diagnostics;

namespace Seal.Forms
{
    public class TemplateTextEditor : UITypeEditor
    {
        public static object CurrentEntity = null; //Hack to get the current entity

        const string razorPreOutputTemplate = @"@{
    ReportOutput output = Model;
    string result = ""1""; //Set result to 0 to cancel the report.
    //Or cancel the report with the Cancel flag
    //output.Report.Cancel = true;
}
@Raw(result)";

        const string razorPostOutputTemplate = @"@{
    ReportOutput output = Model;
}";

        const string displayNameTemplate = @"@{
    Report report = Model;
    var result = System.IO.Path.GetFileNameWithoutExtension(report.FilePath) + "" "" + DateTime.Now.ToShortDateString();
    //var result = report.ExecutionView.Name;
    //var result = report.GetReportModel(""aModelName"").GetRestrictionByName(""A restriction name"").DisplayValue1;
}
@Raw(result)";

        const string razorTaskTemplate = @"@{
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

        const string razorCellScriptTemplate = @"@{
    ResultCell cell = Model;

    ReportElement element = cell.Element;
    ReportModel reportModel = element.Model;
    Report report = reportModel.Report;
    //Script executed to modify a result cell 
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    //For performances reason, consider to process your result table in a dedicated Task with an execution step 'Models generated, before rendering' 
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
	cell.FinalValue (type = string) is the final string used for the table cell, can contain HTML tag, 
    e.g. cell.FinalValue = string.Format(""<a href='{0}' target=_blank>{0}</a>"", cell.DisplayValue);
	cell.FinalCssStyle (type = string) is the final CSS style used for the table cell
	cell.FinalCssClass (type = string) is the final CSS classes used for the table cell, could be one or many Bootstrap classes
	*/
	}
";

        static readonly Tuple<string, string>[] razorCellScriptSamples =
        {
            new Tuple<string, string>(
                "Simple format",
@"//For performances reason, consider to process your result table in a dedicated Task with an execution step 'Models generated, before rendering' 
    if (cell.IsTitle)
	{
        if (cell.ContextIsSummaryTable) {
    		cell.FinalCssStyle = ""font-weight:bold;"";
	    	cell.FinalCssClass = ""warning lead""; 
        }
        else if (cell.ContextIsPageTable) {
    		cell.FinalCssStyle = ""font-weight:bold;"";
	    	cell.FinalCssClass = ""info""; 
        }
        else {
    		cell.FinalCssStyle = ""font-weight:bold;"";
	    	cell.FinalCssClass = ""primary""; 
        }
	}
    else {
        if (cell.IsSubTotal) {
    		cell.FinalCssClass = ""primary text-center"";
        }
        else if (cell.IsTotal) {
    		cell.FinalCssClass = ""danger text-center"";
        }
        else {
            cell.FinalCssClass = ""success text-right""; //These may be Bootstrap classes: active success info warning danger etc...
        }
    }
"
                ),
            new Tuple<string, string>(
                "Display negative values in red and bold",
@"//For performances reason, consider to process your result table in a dedicated Task with an execution step 'Models generated, before rendering' 
    if (cell.DoubleValue < 0)
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
	else if (!cell.IsSerie && !cell.IsTitle && cell.ContextRow > 0)
	{
        //Normal case for DataTable and SummaryTable
		var previousValue = cell.ContextTable[cell.ContextRow-1,cell.ContextCol].DoubleValue;
		var currentValue = cell.ContextTable[cell.ContextRow,cell.ContextCol].DoubleValue;
		//Calculate the running total
		cell.Value = currentValue + (previousValue != null ? previousValue : 0);
    }	
"
                ),
            new Tuple<string, string>(
                "Add Hyperlink, Report Execution or File Download navigation",
@"cell.AddNavigationHyperLink(""https://www.google.com"", report.TranslateRepository(""GeneralText"",""CellScript"",""Visit"") + "" Google"");
    cell.AddNavigationHyperLink(cell.Value.ToString(), cell.DisplayValue);

    //File download: this requires an implementation in the 'Navigation Script' of the model
    if (!string.IsNullOrEmpty(cell.DisplayValue)) {
        cell.AddNavigationFileDownload(""Download "" + cell.DisplayValue);
        //An optional tag value (second parameter) can be set to identify the link in the 'Navigation Script', here we set ""2"" in NavigationLink.Tag
        cell.AddNavigationFileDownload(""Download 2 "" + cell.DisplayValue, ""2"");
    }

    //Report Execution in a new browser tab
    cell.AddNavigationReportExecution(""\\Search - Orders.srex"", ""View Orders"");

    //Report Execution in the navigation context
    cell.AddNavigationReportNavigation(""\\Search - Orders.srex"", ""View Orders"", ""Navigation"");
"
                ),
            new Tuple<string, string>(
                "Display the cell context",
@"cell.FinalValue = string.Format(""Row={0} Col={1} Title={2} Summary={3}"", cell.ContextRow, cell.ContextCol, cell.IsTitle, cell.ContextIsSummaryTable);
"
                ),
        };


        const string razorTableDefinitionScriptTemplate = @"@using System.Data
@{
    MetaTable metaTable = Model;
	ReportExecutionLog log = metaTable;
    ReportModel reportModel = metaTable.NoSQLModel;
    Report report = (reportModel != null ? reportModel.Report : null);
    List<ReportRestriction> restrictions = (reportModel != null ? reportModel.Restrictions : null);

    //Script executed to define the result table columns that will be loaded by the 'Load Script'
    //NOTE 1: Other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    //NOTE 2: This script can also insert values in the table, in this case the 'Default Load Script' must remain empty/blank.
    metaTable.NoSQLTable = new DataTable();
    metaTable.NoSQLTable.Columns.Add(new DataColumn(""numberCol"", typeof(int)));
    metaTable.NoSQLTable.Columns.Add(new DataColumn(""stringCol"", typeof(string)));
    metaTable.NoSQLTable.Columns.Add(new DataColumn(""dateCol"", typeof(DateTime)));

    //OR for a full table load ('Default Load Script' is not necessary)
    //metaTable.NoSQLTable = ExcelHelper.LoadDataTableFromExcel(
    //    System.IO.Path.Combine(Repository.Instance.RepositoryPath, ""Databases"" + System.IO.Path.DirectorySeparatorChar + ""databank.worldbank.org_health_population.xlsx""),
    //    ""Data""  //Tab name in Excel
    //);

    log.LogMessage(""{0} column(s) defined"", metaTable.NoSQLTable.Columns.Count);
}
";

        const string razorTableLoadScriptTemplate = @"@using System.Data
@{
    MetaTable metaTable = Model;
	ReportExecutionLog log = metaTable;
    ReportModel reportModel = metaTable.NoSQLModel;
    Report report = (reportModel != null ? reportModel.Report : null);
    List<ReportRestriction> restrictions = (reportModel != null ? reportModel.Restrictions : null);

    //Default Script executed to fill the model result table from a non SQL source (if the model 'Load script' is empty)
    //Insert values in the table, values must match the table columns defined in 'Definition Script'
    //NOTE 1: Other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    //NOTE 2: This script should remain empty/blank if the 'Definition Script' insert the data in the table.
    log.LogMessage(""Adding table rows with the default table 'Load Script'..."");
    metaTable.NoSQLTable.Rows.Add(123, ""a string value"", DateTime.Now);
    metaTable.NoSQLTable.Rows.Add(124, ""another string value"", DateTime.Now);
    log.LogMessage(""{0} record(s) loaded"", metaTable.NoSQLTable.Rows.Count);
}
";

        const string razorModelLoadScriptTemplate = @"@using System.Data
@{
    ReportModel model = Model;
    DataTable table = model.ResultTable;
	ReportExecutionLog log = model.Report;

    //Script executed to modify the model result table after it has been loaded from the database
    //Modify values in the current result table, rows can also be added and deleted
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...

    //Table columns definition
    foreach (DataColumn col in table.Columns) {
        log.LogMessage(""Column Name:{0}, Type:{1}"", col.ColumnName, col.DataType);
    }

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

        const string razorModelLoadScriptTemplateNoSQL = @"@using System.Data
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

        const string razorModelPreLoadScriptTemplateNoSQL = @"@using System.Data
@{
    ReportModel model = Model;
	ReportExecutionLog log = model.Report;
    List<ReportElement> elements = model.Elements;
    List<ReportRestriction> restrictions = model.Restrictions;

    //Script executed before the model result table is loaded from the database
    //You can change the model restrictions or elements before the load of the table
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    log.LogMessage(""Processing the model 'Pre load script'"");
    //restrictions[0].Value1 = ""1994""; 
    //restrictions[0].Date1 = DateTime.Now.AddYears(-20);
    //model.GetRestrictionByName(""Order Year"").Value1 = ""2015"";
    //model.GetRestrictionByName(""Order Date"").Date1 = model.GetRestrictionByName(""Required Date"").Date1; 
    //model.GetRestrictionByName(""Category"").EnumValues.Add(""beverages""); 
}
";

        const string scriptEnumTemplate = @"@{
    var enumList = Model;
    enumList.Values.Clear();
    //Add enum values
    enumList.Values.Add(new MetaEV() {Id=""id1""});
    enumList.Values.Add(new MetaEV() {Id=""id2"", Val=""Display2"", ValR=""Display Restriction2"", Css=""color:red"", Class=""""});
    enumList.Values.Add(new MetaEV() {Id=""id3"", Val=""Display4 "" + DateTime.Now.ToString(), ValR=""Display Restriction4 "" + DateTime.Now.ToString(), Css="""", Class=""text-info""});
}
";

        const string scriptDisplayEnumTemplate = @"@{
    var enumList = Model;

    //NewValues is the list of values available
    enumList.NewValues.Clear();

    //The Script may contain the filter tag by using the keyword '{EnumFilter}' to build the enum with filters got from the user.
    //The script may contain dependencies with other enum values got from the user by using the keyword {EnumValues_<Name>} where <Name> is the name of the other enumerated list.
    string filter = {EnumFilter};
    List<string> enumValuesCountry = {EnumValues_Country};

    //Add enum values
    enumList.NewValues.Add(new MetaEV() {Id=""id1""});
    enumList.NewValues.Add(new MetaEV() {Id=""id2"", Val=""Display2"", ValR=""Display Restriction2"", Css=""color:red"", Class=""""});
    enumList.NewValues.Add(new MetaEV() {Id=""id3"", Val=""Display4 "" + DateTime.Now.ToString(), ValR=""Display Restriction4 "" + DateTime.Now.ToString(), Css="""", Class=""text-info""});
}
";

        const string razorModelFinalScriptTemplate = @"@using System.Data
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

        const string razorModelCellNavigationScriptTemplate = @"@using System.Data
@using System.IO
@{
    NavigationLink link = Model;
    ResultCell cell = link.Cell;
    ReportModel model = cell.ContextModel;

    //Script executed for a cell navigation...
    
    //link.ScriptResult must contain the final file path to be downloaded

    //Sample 1 to return a file from a disk path
    //We assume here that path is in the first column of the model (the column can be hidden using 'Options: Columns to hide' in the Data Table View)
    var pathCell = link.Cell.ContextCurrentLine[0];
    //pathCell = link.Cell.ContextCurrentLine.FirstOrDefault(i => i.Element.ColumnName.StartsWith(""col_name""));
    link.ScriptResult = pathCell.Value.ToString();
    //Or in the link tag set in the Cell script
    link.ScriptResult = link.Tag;


    //Sample 2 to return a file contained in a blob (to be adapted)
    var helper = new TaskDatabaseHelper();
    var command = helper.GetDbCommand(model.Connection.GetOpenConnection());

    //We assume here that the id is in the first column of the model
    var blobIdCell = link.Cell.ContextCurrentLine[0];
    //blobIdCell = link.Cell.ContextCurrentLine.FirstOrDefault(i => i.Element.ColumnName.StartsWith(""col_name""));
    var blobId = blobIdCell.Value;
    command.CommandText = string.Format(""select blob_value from blob_table where blob_id = {0}"", blobId);
    using (var reader = command.ExecuteReader())
    {
        if (reader.Read())
        {
            var blobName = link.Cell.Value.ToString(); //Name of the file containing the extension
            link.ScriptResult = FileHelper.GetTempUniqueFileName(blobName);
            File.WriteAllBytes(link.ScriptResult, (byte[]) reader[""blob_value""]);
        }
    }

    //The script will be executed for cell having the following initialization in a 'Cell script': 
    //cell.AddNavigationFileDownload(""Download"" + cell.DisplayValue);
}
";

        const string razorModelReportNavigationScriptTemplate = @"@using System.IO
@{
    NavigationLink link = Model;
    Report report = link.Report;

    //Script executed for a report navigation...
    //The report navigation is called from a link got from:
    //report.GetReportNavigationScriptLink(string text = ""download"", string linkTag = """") to get a custom result (saved in link.ScriptResult)
    //
    //report.GetReportNavigationScriptLink(string text = ""html"", string linkTag = """") to get a file to download (final file path saved in link.ScriptResult)
    
    if (link.Text == ""download"") {
        //Sample 1 to return a file from a disk path
        link.ScriptResult = ""C:\\temp\\aFile.pdf"";
        //Or in the link tag set in the Cell script
        link.ScriptResult = link.Tag;
    }
    else if (link.Text == ""html"") {
        //Sample 2 to return a custom html
        link.ScriptResult = string.Format(""<b> This is a custom HTML built in the 'Report Navigation Script' from the server at {0}.</b><hr>"", DateTime.Now);
    }

    //Parsing the link parameters or the HttpRequest
    var parameters = ""Parameters and Request:\r\n"";
    if (link.Parameters != null) {
        foreach (var k in  link.Parameters.AllKeys) {
            parameters += string.Format(""Parameter {0}={1}\r\n"", k, link.Parameters[k]);
        }
    }
    
    if (link.Request != null) {
        foreach (var k in  link.Request.Form.AllKeys) {
            parameters += string.Format(""Form {0}={1}\r\n"", k, link.Request.Form[k]);
        }
        parameters += string.Format(""{0} File(s)\r\n"", link.Request.Files.Count);
    }

    //Check implementations in the sample reports '505-Navigation - Custom buttons and report execution' and '506-Navigation - File and  fields Upload'
}
";
        const string razorInitScriptTemplate = @"@{
    Report report = Model;
	ReportExecutionLog log = report;

    //Script executed when the report is initialized
    log.LogMessage(""Executing Init Script"");
    //report.GetReportModel(""aModelName"").Restrictions[0].Value1 = ""1994""; 
    //report.GetReportModel(""aModelName"").Restrictions[0].Date1 = DateTime.Now.AddYears(-20);
    //report.GetReportModel(""aModelName"").GetRestrictionByName(""Order Year"").Value1 = ""2015"";
    //report.GetReportModel(""aModelName"").GetRestrictionByName(""Category"").EnumValues.Add(""1"");

    //report.DisplayName = System.IO.Path.GetFileNameWithoutExtension(report.FilePath) + ""-"" + DateTime.Now.ToShortDateString();

    //Get the restriction from the column name
    //var restr = report.GetReportModel(""aModelName"").Restrictions.FirstOrDefault(i =>  i.MetaColumn.Name == ""Products.CategoryID"");

    /*Remove a given restriction from all models if the user belongs to a group
    if (report.SecurityContext != null && report.SecurityContext.BelongsToGroup(""aGroupName"")) {
        foreach (var model in report.Models) {
            var restrictions = model.Restrictions.Where(i =>  i.MetaColumn.Name == ""Products.CategoryID"");
            foreach (var restriction in restrictions) {
                model.Restriction = model.Restriction.Replace(""[""+ restriction.GUID + ""]"", ""1=1"");
            }
            model.Restrictions.RemoveAll(i => restrictions.Contains(i));
        }
    }
    */

    //Set the last value of an enum
    //var restr = report.GetReportModel(""aModelName"").GetRestrictionByName(""Category"");
    //restr.EnumValues.Clear();
    //restr.EnumValues.Add(restr.EnumRE.Values[restr.EnumRE.Values.Count-1].Id);

    //Change view parameter to display the information Tab
    //report.ExecutionView.GetParameter(""information_button"").BoolValue = true;
}
";

        const string razorConfigurationReportCreationScriptTemplate = @"@{
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

        const string foldersScriptTemplate = @"@{
    SecurityUser user = Model;
    //Full documentation at https://sealreport.org/Help/Index.html
    //user.ScriptNumber is a number incremented if several scripts are executed
    
    //Sample to define folders
    user.Folders.Clear();
    user.Folders.Add(new SWIFolder() { path = @""\Samples"", name = ""New Samples"", right = 4, sql = true, manage = 0, expand = true });
    user.Folders.Add(new SWIFolder() { path = @""\"", name = ""Root"", right = 4, sql = true, manage = 0, expand = true });

    SWIFolder sampleFolder = user.AllFolders.FirstOrDefault(i => i.path ==  @""\Samples"");
    if (sampleFolder != null) {
        //Sample to add a subfolder
        sampleFolder.folders.Add(new SWIFolder() { path = @""\System"", name = ""System in a sub-folder"", right = 4, sql = false, manage = 0});
        
        //Sample to modify a folder
        sampleFolder.name = ""New Samples 2""; 
        sampleFolder.manage = 1; 
        sampleFolder.right = 4;
        sampleFolder.sql = false;

        //Sample to remove a folder
        //sampleFolder.folders.RemoveAll(i => i.path == @""\System"");
    }
}
";

        const string folderDetailScriptTemplate = @"@{
    SecurityUser user = Model;
    //Full documentation at https://sealreport.org/Help/Index.html
    //user.ScriptNumber is a number incremented if several scripts are executed

    if (user.FolderDetail.folder.path == @""\Samples"") {
        //Filter reports
        user.FolderDetail.files.RemoveAll(i => !i.name.Contains(""Charts"")); 
        
        //Sample to change a report 
        SWIFile file = user.FolderDetail.files.FirstOrDefault(i => i.name == ""04-Charts Gallery - Basics"");
        if (file != null) {
            file.name = ""04-Charts Gallery - Basics NEW"";
            file.right = 1;
        }
    }
    
    if (user.FolderDetail.folder.path == @""\"") {
        //Sample to add a report 
        user.FolderDetail.files.Add(new SWIFile() { path=@""\Samples\04-Charts Gallery - Basics.srex"", name=""Charts from Root"", last="""", isreport=true, right=1}); 
    }
 }
";

        const string menuScriptTemplate = @"@{
    SecurityUser user = Model;
    //Full documentation at https://sealreport.org/Help/Index.html
    //user.ScriptNumber is a number incremented if several scripts are executed

    //Sample to define a menu
    var reports = user.WebMenu.reports;
    reports.Clear();
    var item = new SWIMenuItem() { path = @""\Samples\04-Charts Gallery - Basics.srex"", viewGUID = null, outputGUID = null, name = ""View Charts"" };
    
    //extra classes for the menu item
    //item.classes = ""disabled"";

    reports.Add(item);
    // subMenu
    var subMenu = new SWIMenuItem() { name = ""A SubMenu"" };
    //subMenu.classes = ""disabled label-danger"";
    subMenu.items.Add(new SWIMenuItem() { path = @""\Search - Orders.srex"", viewGUID = null, outputGUID = null, name = ""Search"" });
    reports.Add(subMenu);
    
    //Clear recent reports
    user.WebMenu.recentreports.Clear();
}
";

        const string razorSourceInitScriptTemplate = @"@using System.Data
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

        const string razorConfigurationInitScriptTemplate = @"@using System.Data
@{
    Report report = Model;

    //Script executed when the report is initialized at execution

    report.LogMessage(""Executing configuration 'Init Script'"");

    // e.g. to change default css style and class of the data tables generation
    //ResultCell.DefaultCellCssClass = ""info"";
    //ResultCell.DefaultCellCssStyle = ""color:blue;"";
    //ResultCell.DefaultNumericTitleCssClass = ""danger text-right"";
    //ResultCell.DefaultDateTimeTitleCssStyle = ""font-size:20px;color:blue;"";
    //ResultCell.DefaultNumericCellCssClass = ""text-right"";

    // e.g. to change the Thousand Separator or the Decimal Separator
    //report.ExecutionView.CultureInfo.NumberFormat.NumberGroupSeparator = ""'"";
    //report.ExecutionView.CultureInfo.NumberFormat.NumberDecimalSeparator = ""."";	    

}
";

        const string sftpDeviceScriptTemplate = @"@using System.IO
@using Renci.SshNet
@{
    Report report = Model;
    ReportOutput output = report.OutputToExecute;
    OutputFileServerDevice device = (OutputFileServerDevice) output.Device;
    //Create client and connect to the server
    using (var client = new SftpClient(device.Server, device.Port, device.UserName, device.ClearPassword))
    {
        client.Connect();
        //Change directory and upload the file
        client.ChangeDirectory(device.Directory);
        using (var fileStream = new FileStream(report.ResultFilePath, FileMode.Open))
        {
            client.BufferSize = 4 * 1024;
            client.UploadFile(fileStream, report.ResultFileName);
        }
    }
}
";

        static readonly Tuple<string, string>[] tasksSamples =
        {
            new Tuple<string, string>(
                "Copy date input values to all date restrictions in the report",
@"ReportTask task = Model;
    Report report = task.Report;
    //Take the first input value that has been defined in the report
    var inputValue = report.InputValues[0];
    if (inputValue.Date1 == DateTime.MinValue) {
        inputValue.Date1 = DateTime.Now;
    }

    foreach (var model in report.Models) {
        foreach (var restriction in model.Restrictions.Where(i => i.IsDateTime /* i.SQLColumn == ""Orders.OrderDate"" */)) {
            restriction.Operator = Operator.Between;
            restriction.Date1Keyword = """";
            restriction.Date2Keyword = """";
            restriction.Date1 = inputValue.Date1.AddYears(-1);
            restriction.Date2 = inputValue.Date1;
        }
        foreach (var restriction in model.CommonRestrictions.Where(i => i.Name == ""Common_Value_Name"")) {
            restriction.TypeRe = ColumnType.DateTime;
            restriction.Date1 = inputValue.Date1;
        }
    }
"
                ),
            new Tuple<string, string>(
                "Format final result cells before rendering",
@"ReportTask task = Model;
    Report report = task.Report;   
    //Note that this Task MUST BE executed at the step: 'Models generated, before rendering'
    foreach (var model in report.Models) 
    {
        foreach (var page in model.Pages) 
        {
            //Format page result table
            foreach (var line in page.PageTable.Lines) 
            {
                foreach (var cell in line) 
                {
                    if (cell.IsTitle) {
                        cell.FinalCssStyle = ""font-size:20px;color:blue;"";
                    }
                }
            }       
            //Format data result table
            foreach (var line in page.DataTable.Lines) 
            {
                foreach (var cell in line) 
                {
                    if (cell.Element != null && cell.Element.IsNumeric && cell.DoubleValue < 0) {
                        cell.FinalCssStyle = ""font-weight:bold;color:red;"";
                    }
                }
            }       
        }
    }
"
                ),
            new Tuple<string, string>(
                "Add borders to the final result tables",
@"ReportTask task = Model;
    Report report = task.Report;   
    //Note that this Task MUST BE executed at the step: 'Models generated, before rendering'
    foreach (var model in report.Models /*.Where(i => i.Name == "") */) 
    {
        foreach (var page in model.Pages) 
        {
            //Format data result table
            string style = ""solid"", width = ""1px"";
            int rowIndex = 0;
            foreach (var line in page.DataTable.Lines) 
            {
                int colIndex = 0;                
                foreach (var cell in line) 
                {
                    cell.FinalCssStyle = """";
                    if (rowIndex == 0) { //First line
                        cell.FinalCssStyle = string.Format(""border-top-style:{0};border-top-width:{1};"", style, width);
                    }
                    if (rowIndex == page.DataTable.RowCount -1) {  //Last line
                        cell.FinalCssStyle = string.Format(""border-bottom-style:{0};border-bottom-width:{1};"", style, width);
                    }
                    if (colIndex == 0) { //First column
                        cell.FinalCssStyle += string.Format(""border-left-style:{0};border-left-width:{1};"", style, width);
                    }
                    
                    //Columns
                    int modulo = model.GetElements(PivotPosition.Data).Count();
                    if (modulo == 0 || colIndex % modulo == 0 || colIndex == page.DataTable.ColumnCount -1) {
                        cell.FinalCssStyle += string.Format(""border-right-style:{0};border-right-width:{1};"", style, width);
                    }
                    colIndex++;
                }                
                rowIndex++;
            }       
        }
    }
"
                ),
            new Tuple<string, string>(
                "Keep only xx rows in the result tables",
@"ReportTask task = Model;
    Report report = task.Report;   
    //Note that this Task MUST BE executed at the step: 'Models generated, before rendering'
    foreach (var model in report.Models /*.Where(i => i.Name == "") */) 
    {
        foreach (var page in model.Pages) 
        {
            var dataTable = page.DataTable;
            var rowsToKeep = 10; //Number of body rows to keep
            if (dataTable != null && dataTable.Lines.Count > rowsToKeep) {
                var newLines = dataTable.Lines.Take(dataTable.BodyStartRow + rowsToKeep).ToList();
                //Add totals
                for (int i = dataTable.BodyEndRow; i < dataTable.RowCount; i++) {
                    newLines.Add(dataTable.Lines[i]);
                }
                dataTable.BodyEndRow = dataTable.BodyStartRow + rowsToKeep;
                dataTable.Lines = newLines;
            }
        }
    }
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
"
            ),
            new Tuple<string, string>(
                "Set a view parameter value",
@"ReportTask task = Model;
    Report report = task.Report;
    
    //Select a view by name
    var view = report.AllViews.FirstOrDefault(i => i.Name == ""View Name"");
    if (view != null) {
        //Set the parameter value: here for a ChartJS View, the chart title is got from the restriction value 
        view.SetParameter(""chartjs_title"", report.Models.First(i => i.Name.StartsWith(""Model Name Prefix"")).GetRestrictionByName(""Restriction Name"").EnumDisplayValue);
    }
"
            ),
            new Tuple<string, string>(
                "Update the navigation link text",
@"ReportTask task = Model;
    Report report = task.Report;   
    //Note that this Task MUST BE executed at the step: 'Models generated, before rendering'
    
    foreach (var model in report.Models) 
    {
        foreach (var page in model.Pages) 
        {
            foreach (var line in page.DataTable.Lines) 
            {
                ResultCell cellFirstName = line.FirstOrDefault(i => i.Element != null && i.Element.Name == ""Employees.FirstName"");
                ResultCell cellLastName = line.FirstOrDefault(i => i.Element != null && i.Element.Name == ""Employees.LastName"");
                if (cellFirstName != null && cellLastName != null) {
                    //Change the sub-report link generic text
                    var link = cellLastName.Links.FirstOrDefault(i => i.Href.Contains(""Employee+Detail.srex""));
                    if (link != null) {
                        link.Text = ""View "" + cellFirstName.Value + "" "" + cellLastName.Value;
                    }
                }
            }       
        }
    }
"
            ),
            new Tuple<string, string>(
                "Change the connections for a specific report output execution",
@"ReportTask task = Model;
    Report report = task.Report;
    if (report.OutputToExecute != null && report.OutputToExecute.Name.StartsWith(""Your output name..."")) {
        //Execution for a given output, we can modify the connections of the models
        
        //Select a model to modify
        var model = report.Models.FirstOrDefault(i => i.Name.StartsWith(""Your model name...""));
        if (model != null) {
            //Select the new connection
            var connection = model.Source.Connections.FirstOrDefault(i => i.Name.StartsWith(""Your connection name...""));
            if (connection != null) {
                report.LogMessage(""Setting connection '{0}' to '{1}'"", connection.Name, model.Name);
                //Set it to the model
                model.ConnectionGUID = connection.GUID;
           },
        }
    }
"
                ),
            new Tuple<string, string>(
                "Load a report model Result Table and/or build its Result Pages",
@"ReportTask task = Model;
    Report report = task.Report;
    var model = report.GetReportModel(""aModelName"");
    
    //If loaded in a task, the model will not be loaded during the standard models generation process
    report.LogMessage(""Loading model Result Table for model '{0}'"", model.Name);
    task.Execution.LoadResultTableModel(model);
    while (!model.ExecResultTableLoaded) {
        System.Threading.Thread.Sleep(100);
    }

    report.LogMessage(""Building model Pages Result for model '{0}'"", model.Name);
    task.Execution.BuildResultPagesModel(model);       

    //If the task is executed after the standard models generation, these 2 flags may be reset before calling the methods
    //model.ExecResultTableLoaded = false; for LoadResultTableModel() to force the Load
    //model.ExecResultPagesBuilt = false; for BuildResultPagesModel to force the build
"
                ),
            new Tuple<string, string>(
                "Refresh Data Sources enumerated lists",
@"ReportTask task = Model;
    var helper = new TaskHelper(task);
    helper.RefreshRepositoryEnums();
"
                ),
            new Tuple<string, string>(
                "Load a table from an Excel file",
@"ReportTask task = Model;
	var helper = new TaskHelper(task);
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
        ""DataSourceName"", //the name of the Report Data Source
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
    helper.ExecuteProcess(@""executablePath"",""argument"",@""workingDirectory"");
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

    //Execute just one file
    helper.ExecuteMSSQLFile(
        @""C:\temp\aScript.sql"",
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
        var result = new System.Text.StringBuilder();
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
        var result = new System.Text.StringBuilder();
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
        var result = new System.Text.StringBuilder();
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

    dbHelper.MyLoadDataTable = new CustomLoadDataTable(delegate(ConnectionType connectionType, string connectionString, string sql) {
        return new DataTable(); //Check current source implementation in TaskDatabaseHelper.cs
    });

    dbHelper.MyLoadDataTableFromExcel = new CustomLoadDataTableFromExcel(delegate(string excelPath, string tabName, int startRow, int startCol, int endColIndex) {
        return new DataTable(); //Check current source implementation in TaskDatabaseHelper.cs
    });

    dbHelper.MyLoadDataTableFromCSV = new CustomLoadDataTableFromCSV(delegate(string csvPath, char? separator) {
        return new DataTable(); //Check current source implementation in TaskDatabaseHelper.cs
    });
"
                ),
        };


        const string sqlConnectionString = @"Server=myServerAddress;Database=myDatabase;Trusted_Connection=True";
        const string odbcConnectionString = @"DSN=myDataSourceName;DATABASE=myDatabase";
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (context != null && context.Instance is ReportView && context.PropertyDescriptor.IsReadOnly) return UITypeEditorEditStyle.None;
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
                else if (context.Instance is ReportViewPartialTemplate || context.Instance is PartialTemplatesEditor)
                {
                    var pt = context.Instance is ReportViewPartialTemplate ? context.Instance as ReportViewPartialTemplate : ((PartialTemplatesEditor)context.Instance).GetPartialTemplate(context.PropertyDescriptor.Name);
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
                        samples.Add("@using System.Data\r\n@{\r\n    //" + sample.Item1 + "\r\n    " + sample.Item2 + "}\r\n|" + sample.Item1);
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
                else if (context.Instance.GetType().ToString() == "Seal.Converter.PdfConverter")
                {
                    string language = "cs";
                    SealPdfConverter converter = (SealPdfConverter)context.Instance;
                    converter.ConfigureTemplateEditor(frm, context.PropertyDescriptor.Name, ref template, ref language);
                    ScintillaHelper.Init(frm.textBox, language);
                }
                else if (context.Instance.GetType().ToString() == "Seal.Converter.ExcelConverter")
                {
                    string language = "cs";
                    SealExcelConverter converter = (SealExcelConverter)context.Instance;
                    converter.ConfigureTemplateEditor(frm, context.PropertyDescriptor.Name, ref template, ref language);
                    ScintillaHelper.Init(frm.textBox, language);
                }
                else if (context.Instance is Report)
                {
                    if (context.PropertyDescriptor.Name == "DisplayName")
                    {
                        template = displayNameTemplate;
                        frm.ObjectForCheckSyntax = (Report)context.Instance;
                        frm.Text = "Edit display name script";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "InitScript")
                    {
                        template = razorInitScriptTemplate;
                        frm.ObjectForCheckSyntax = (Report)context.Instance;
                        frm.Text = "Edit the script executed when the report is initialized";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "NavigationScript")
                    {
                        template = razorModelReportNavigationScriptTemplate;
                        frm.ObjectForCheckSyntax = new NavigationLink() { Report = (Report)context.Instance };
                        frm.Text = "Edit the navigation script executed for the report";
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
                            samples.Add("@{\r\n\t//" + sample.Item1 + "\r\n\tResultCell cell=Model;\r\n\tReportElement element = cell.Element;\r\n\tReportModel reportModel = element.Model;\r\n\tReport report = reportModel.Report;\r\n\t" + sample.Item2 + "}\r\n|" + sample.Item1);
                        }
                        frm.SetSamples(samples);

                        frm.ObjectForCheckSyntax = new ResultCell() { Element = element };
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "NavigationScript")
                    {
                        template = razorModelCellNavigationScriptTemplate;
                        frm.ObjectForCheckSyntax = new NavigationLink() { Cell = new ResultCell() { Element = element }, Report = element.Report };
                        frm.Text = "Edit the navigation script executed for the model";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "SQL")
                    {
                        frm.Text = element.Source.IsNoSQL ? "Edit custom Expression" : "Edit custom SQL";
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
                else if (context.Instance is MetaEnum)
                {
                    if (context.PropertyDescriptor.Name == "Script")
                    {
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                        frm.ObjectForCheckSyntax = context.Instance;
                        template = scriptEnumTemplate;
                        frm.Text = "Edit the script to load the enumerated list values";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "ScriptDisplay")
                    {
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                        frm.ObjectForCheckSyntax = context.Instance;
                        template = scriptDisplayEnumTemplate;
                        frm.Text = "Edit the script to load dynamically the enumerated list values";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                }
                else if (context.Instance is MetaConnection)
                {
                    if (context.PropertyDescriptor.Name == "ConnectionString")
                    {
                        template = "";

                        List<string> samples = new List<string>();
                        try
                        {
                            OleDbEnumerator enumerator = new OleDbEnumerator();
                            DataTable table = enumerator.GetElements();
                            foreach (DataRow row in table.Rows)
                            {
                                var sourceName = row["SOURCES_NAME"].ToString();
                                if (!sourceName.ToLower().Contains("enumerator"))
                                {
                                    samples.Add(string.Format("Provider={0};Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=Northwind;Data Source=localhost;|{1}", sourceName, row["SOURCES_DESCRIPTION"]));
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            samples.Add("Provider=SQLNCLI11;Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=Northwind;Data Source=localhost;|SQLServer Native Client");
                        }
                        frm.SetSamples(samples);


                        frm.Text = "Edit the MS SQLServer Connection script";
                        ScintillaHelper.Init(frm.textBox, Lexer.Null);
                    }
                    if (context.PropertyDescriptor.Name == "MSSqlServerConnectionString")
                    {
                        template = sqlConnectionString;
                        frm.Text = "Edit the MS SQLServer Connection script";
                        ScintillaHelper.Init(frm.textBox, Lexer.Null);
                    }
                    if (context.PropertyDescriptor.Name == "OdbcConnectionString")
                    {
                        template = odbcConnectionString;
                        frm.Text = "Edit the ODBC Connection script";
                        ScintillaHelper.Init(frm.textBox, Lexer.Null);
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
                        var table = context.Instance as MetaTable;
                        template = table.TableTemplate != null ? table.DefaultDefinitionScript : razorTableDefinitionScriptTemplate;

                        frm.ObjectForCheckSyntax = context.Instance;
                        frm.Text = "Edit the script to define the table";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);

                        if (context.PropertyDescriptor.IsReadOnly && string.IsNullOrEmpty(valueToEdit)) valueToEdit = template;
                    }
                    else if (context.PropertyDescriptor.Name == "LoadScript")
                    {
                        var table = context.Instance as MetaTable;
                        template = table.TableTemplate != null ? table.DefaultLoadScript : razorTableLoadScriptTemplate;

                        frm.ObjectForCheckSyntax = context.Instance;
                        frm.Text = "Edit the default script to load the table";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);

                        if (context.PropertyDescriptor.IsReadOnly && string.IsNullOrEmpty(valueToEdit)) valueToEdit = template;
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
                        template = razorModelFinalScriptTemplate;
                        frm.ObjectForCheckSyntax = context.Instance;
                        frm.Text = "Edit the final script executed for the model";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "LoadScript")
                    {
                        var model = (ReportModel)context.Instance;
                        if (!model.Source.IsNoSQL)
                        {
                            frm.Text = "Edit the script executed after table load";
                            template = razorModelLoadScriptTemplateNoSQL;
                        }
                        else
                        {
                            frm.Text = "Edit the script to load the table";
                            template = razorModelLoadScriptTemplate;
                            model.BuildQuery();
                            template = model.LINQLoadScript;
                        }
                        frm.ObjectForCheckSyntax = context.Instance;
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "LINQQueryScript")
                    {
                        template = ReportModel.DefaultLINQScriptTemplate;
                        frm.Text = "Edit the LINQ Query Script template used to generate the model (assign query2 from query)";
                        List<string> samples = new List<string>();
                        samples.Add("var query2 = query.Take(3); //Take the first 3 rows");
                        samples.Add("var query2 = query.Skip(2); //Skip the first 2 rows");
                        samples.Add("var query2 = query.TakeWhile(i => !string.IsNullOrEmpty(i.C0));");
                        samples.Add("var query2 = query.SkipWhile(i => i.C0 + i.C1 > 0);");
                        frm.SetSamples(samples);

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
                else if (context.Instance is OutputFileServerDevice && context.PropertyDescriptor.Name == "ProcessingScript")
                {
                    template = OutputFileServerDevice.ProcessingScriptTemplate;
                    frm.ObjectForCheckSyntax = new Report();
                    frm.Text = "Edit the script executed when the output is processed";
                    ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                }
                else if (context.Instance is CommonScript)
                {
                    template = CommonScript.RazorTemplate;
                    frm.Text = "Edit the script that will be added to all scripts executed for the report.";
                    if (CurrentEntity is SealServerConfiguration)
                    {
                        //common script from configuration, nothing to include, we rely on @Include
                    }
                    if (CurrentEntity is Report)
                    {
                        //common script from report
                        frm.ScriptHeader = ((Report)CurrentEntity).GetCommonScriptsHeader((CommonScript)context.Instance);
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
                    else if (context.PropertyDescriptor.Name == "ReportCreationScript")
                    {
                        template = razorConfigurationReportCreationScriptTemplate;
                        frm.ObjectForCheckSyntax = report;
                        frm.Text = "Edit the script executed when a new report is created";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "AuditScript")
                    {
                        template = Audit.AuditScriptTemplate;
                        frm.ObjectForCheckSyntax = new Audit();
                        frm.Text = "Edit the script executed when a an audit event occurs";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                }
                else if (context.Instance is SecurityGroup)
                {
                    if (context.PropertyDescriptor.Name == "FoldersScript")
                    {
                        template = foldersScriptTemplate;
                        frm.ObjectForCheckSyntax = new SecurityUser(null);
                        frm.Text = "Edit the Folders Script";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "FolderDetailScript")
                    {
                        template = folderDetailScriptTemplate;
                        frm.ObjectForCheckSyntax = new SecurityUser(null);
                        frm.Text = "Edit the Folder Detail Script";
                        ScintillaHelper.Init(frm.textBox, Lexer.Cpp);
                    }
                    else if (context.PropertyDescriptor.Name == "MenuScript")
                    {
                        template = menuScriptTemplate;
                        frm.ObjectForCheckSyntax = new SecurityUser(null);
                        frm.Text = "Edit the Menu Script";
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
