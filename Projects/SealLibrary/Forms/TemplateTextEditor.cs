//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using Seal.Model;

namespace Seal.Forms
{
    public class TemplateTextEditor : UITypeEditor
    {
        const string razorPreOutputTemplate = "@using Seal.Model\r\n@{\r\nReportOutput output = Model;\r\nstring result = \"1\"; //Set result to 0 to cancel the report.\r\n}\r\n@Raw(result)";
        const string razorPostOutputTemplate = "@using Seal.Model\r\n@{\r\nReportOutput output = Model;\r\n}";
        const string displayNameTemplate = "@using Seal.Model\r\n@{\r\nReport report = Model;\r\nstring result = System.IO.Path.GetFileNameWithoutExtension(report.FilePath) + \" \" + DateTime.Now.ToShortDateString();\r\n}\r\n@Raw(result)";

        const string razorTaskTemplate = @"@using Seal.Model
@{
    ReportTask task = Model;
    Report report = task.Report;
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    string result = ""1""; //Set result to 0 to cancel the report.
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
    cell.ContextIsSummaryTable indicates if the current table is the SummaryTable (and not the DataTable)
    cell.ContextCurrentLine is the current line of the table (type = ResultCell[]), null for SummaryTable
    cell.IsTotal indicates if it is a total cell
    cell.IsTotalTotal indicates if it is a total of total cell
    cell.IsTitle indicates if it is a title cell
    cell.IsSerie indicates if the cell is used for series, in this case, ContextRow and ContextCol is the common row and col used for the dimension values
	
	To customize your calculation and cell display, you can assign
	cell.Value (type = object) is the cell value
	cell.FinalValue (type = string) is the final string used for the table cell
	cell.FinalCssStyle (type = string) is the final CSS style used for the table cell
	
	e.g. to calculate a progression
	if (cell.ContextRow == cell.ContextTable.Lines.Count - 1)
	{
		//No progression for last table line (summary or data)
		cell.Value = null;		
	}
	else if (!cell.IsTitle && cell.ContextRow > 0)
	{
		var previousValue = cell.ContextTable.Lines[cell.ContextRow-1][cell.ContextCol-1].DoubleValue;
		var currentValue = cell.ContextTable.Lines[cell.ContextRow][cell.ContextCol-1].DoubleValue;
		cell.Value = 100*(currentValue - previousValue)/previousValue;
	}

	e.g. to change the cell style
	if (cell.DoubleValue < 0)
	{
		cell.FinalCssStyle = ""font-weight:bold;color:red;text-align:right;"";
	}
	else if (cell.DoubleValue >50)
	{
		cell.FinalValue = string.Format(""<span style='font-size:12pt;'>&#9786;</span> {0:N0} %"", cell.DoubleValue);
		cell.FinalCssStyle = ""color:green;text-align:right;"";
	}		

	e.g. to display the cell context
	cell.FinalValue = string.Format(""Row={0} Col={1} Title={2} Summary={3} Page={4} Data"", cell.ContextRow, cell.ContextCol, cell.IsTitle, cell.ContextIsSummaryTable);
	*/
	}
";

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
		ReportElement element = model.Elements.FirstOrDefault(i => i.DisplayNameEl == ""City"");
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
    //You can change the model restrictions or elements before the loaded
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    log.LogMessage(""Processing the model 'Pre Load Script'"");
    //restrictions[0].Value1 = ""1994""; 
    //restrictions[0].Date1 = DateTime.Now.AddYears(-20);
    //model.GetRestrictionByName(""Order Year"").Value1 = ""2015"";
}
";


        const string razorTableFinalScriptTemplate = @"@using Seal.Model
@using System.Data
@{
    ReportModel model = Model;
 	ReportExecutionLog log = model.Report;

    //Final script executed to modify the model result tables after its generation
    //Note that other assemblies can be used by saving the .dll in the Repository 'Assemblies' sub-folder...
    log.LogMessage(""Modifying result values with the 'Final Script'..."");
    ResultTable summaryTable = model.SummaryTable;
    foreach (ResultPage page in model.Pages)
    {
        ResultTable dataTable = page.DataTable;
    	ResultTable pageTable = page.PageTable;

        /* e.g to change the last line of the Data Tables
        dataTable.Lines[dataTable.Lines.Count - 1][0].Value = ""Maximum"";
		for (int i=0;i< dataTable.Lines[0].Length; i++)
	    {
		    string style =  ""background:orange;"" + (i > 0 ? ""text-align:right;"" : """");
		    dataTable.Lines[dataTable.Lines.Count - 1][i].FinalCssStyle = style;
	    }
        */
    }

}
";

        const string razorTasksTemplate = @"@using System.Text
@functions {
    //During execution, this script will be copied at the end of all task scripts...
    public string MyConvertString(string input) {
        return input.Replace(""__"",""_"");
    }
}
";

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
                    frm.View = context.Instance as ReportView;
                    if (context.PropertyDescriptor.Name == "CustomTemplate")
                    {
                        if (string.IsNullOrEmpty(valueToEdit)) valueToEdit = frm.View.ViewTemplateText;
                        template = frm.View.ViewTemplateText.Trim();
                        frm.Text = "Edit custom template";
                        frm.TypeForCheckSyntax = frm.View.Template.ForModel ? typeof(ReportModel) : typeof(Report);
                        frm.textBox.ConfigurationManager.Language = "cs";
                    }
                    else if (context.PropertyDescriptor.Name == "CustomConfiguration")
                    {
                        if (string.IsNullOrEmpty(valueToEdit)) valueToEdit = frm.View.Template.Configuration;
                        template = frm.View.Template.Configuration.Trim();
                        frm.Text = "Edit template configuration";
                        frm.TypeForCheckSyntax = typeof(ReportViewTemplate);
                        frm.textBox.ConfigurationManager.Language = "cs";
                    }
                }
                else if (context.Instance is ReportTask)
                {
                    template = razorTaskTemplate;
                    frm.TypeForCheckSyntax = typeof(ReportTask);
                    frm.Text = "Edit task script";
                    frm.textBox.ConfigurationManager.Language = "cs";
                    frm.TextToAddForCheck = ((ReportTask)context.Instance).Report.TasksScript + "\r\n" +  ((ReportTask)context.Instance).Source.TasksScript; 
                    List<string> samples = new List<string>();
                    samples.Add("@using Seal.Model\r\n@using Seal.Helpers\r\n@{\r\n\t//Refresh Data Sources enumerated lists\r\n\tReportTask task = Model;\r\n\tvar helper = new TaskHelper(task);\r\n\thelper.RefreshRepositoryEnums();\r\n}");
                    samples.Add("@using Seal.Model\r\n@using Seal.Helpers\r\n@{\r\n\t//Load a table from an Excel file, may need ODBC Office 2007 Drivers\r\n\tReportTask task = Model;\r\n\tvar helper = new TaskHelper(task);\r\n\thelper.LoadTableFromExcel(@\"c:\\temp\\loadFolder\", @\"c:\\temp\\excelFile.xlsx\", \"ExcelTabName\", \"DestinationTableName\", false /* true to load in all connections */);\r\n}");
                    samples.Add("@using Seal.Model\r\n@using Seal.Helpers\r\n@{\r\n\t//Load a table from a CSV file\r\n\tReportTask task = Model;\r\n\tvar helper = new TaskHelper(task);\r\n\thelper.LoadTableFromCSV(@\"c:\\temp\\loadFolder\", @\"c:\\temp\\aCSVFile.csv\", \"DestinationTableName\", null /* separator may be specified here */, false /* true to load in all connections */);\r\n}");
                    samples.Add("@using Seal.Model\r\n@using Seal.Helpers\r\n@{\r\n\t//Load a table from a source table located in an external data source\r\n\tReportTask task = Model;\r\n\tvar helper = new TaskHelper(task);\r\n\thelper.LoadTableFromExternalSource(\"SourceConnectionString\", \"SourceSelectStatement\", \"DestinationTableName\", false /* true to load in all connections */, \"OptionalSourceCheckSelect\", \"OptionalDestinationCheckSelect\");\r\n}");
                    samples.Add("@using Seal.Model\r\n@using Seal.Helpers\r\n@{\r\n\t//Execute a program and display Standard Output and Errors\r\n\tReportTask task = Model;\r\n\tvar helper = new TaskHelper(task);\r\n\thelper.ExecuteProcess(@\"executablePath\");\r\n}");
                    frm.SetSamples(samples);
                }
                else if (context.Instance is ReportOutput)
                {
                    if (context.PropertyDescriptor.Name == "PreScript") template = razorPreOutputTemplate;
                    else if (context.PropertyDescriptor.Name == "PostScript") template = razorPostOutputTemplate;
                    frm.TypeForCheckSyntax = typeof(ReportOutput);
                    frm.Text = "Edit output script";
                    frm.textBox.ConfigurationManager.Language = "cs";
                }
                else if (context.Instance is Parameter)
                {
                    Parameter parameter = context.Instance as Parameter;
                    template = parameter.ConfigValue;
                    frm.Text = parameter.DisplayName;
                    frm.textBox.ConfigurationManager.Language = (string.IsNullOrEmpty(parameter.EditorLanguage) ? "" : parameter.EditorLanguage);
                }
                else if (context.Instance.GetType().ToString() == "SealPdfConverter.PdfConverter")
                {
                    string language = "cs";
                    SealPdfConverter converter = SealPdfConverter.Create("");
                    converter.ConfigureTemplateEditor(frm, context.PropertyDescriptor.Name, ref template, ref language);
                    frm.textBox.ConfigurationManager.Language = language;
                }
                else if (context.Instance.GetType().ToString() == "SealExcelConverter.ExcelConverter")
                {
                    string language = "cs";
                    SealExcelConverter converter = SealExcelConverter.Create("");
                    converter.ConfigureTemplateEditor(frm, context.PropertyDescriptor.Name, ref template, ref language);
                    frm.textBox.ConfigurationManager.Language = language;
                }
                else if (context.Instance is ViewFolder)
                {
                    if (context.PropertyDescriptor.Name == "DisplayName")
                    {
                        template = displayNameTemplate;
                        frm.TypeForCheckSyntax = typeof(Report);
                        frm.Text = "Edit display name script";
                        frm.textBox.ConfigurationManager.Language = "cs";
                    }
                }
                else if (context.Instance is ReportElement)
                {
                    ReportElement element = context.Instance as ReportElement;
                    if (context.PropertyDescriptor.Name == "CellScript")
                    {
                        frm.Text = "Edit custom script for the cell";
                        template = razorCellScriptTemplate;
                        frm.TypeForCheckSyntax = typeof(ResultCell);
                        frm.textBox.ConfigurationManager.Language = "cs";
                    }
                    else if (context.PropertyDescriptor.Name == "SQL")
                    {
                        frm.Text = "Edit custom SQL";
                        frm.textBox.ConfigurationManager.Language = "sql";
                        template = element.RawSQLColumn;
                        List<string> samples = new List<string>();
                        samples.Add(element.RawSQLColumn);
                        if (!string.IsNullOrEmpty(element.SQL) && !samples.Contains(element.SQL)) samples.Add(element.SQL);
                        frm.SetSamples(samples);
                        frm.textBox.LineWrapping.Mode = ScintillaNET.LineWrappingMode.Word;
                    }
                    else if (context.PropertyDescriptor.Name == "CellCss")
                    {
                        frm.Text = "Edit custom CSS";
                        frm.textBox.ConfigurationManager.Language = "css";
                        List<string> samples = new List<string>();
                        samples.Add("text-align:right;");
                        samples.Add("text-align:center;");
                        samples.Add("text-align:left;");
                        samples.Add("font-style:italic;");
                        samples.Add("font-weight:bold;color:red;background-color:yellow;");
                        samples.Add("color:green;text-align:right;|color:black;|font-weight:bold;color:red;text-align:right;");
                        samples.Add("white-space: nowrap;");
                        frm.SetSamples(samples);
                        frm.textBox.LineWrapping.Mode = ScintillaNET.LineWrappingMode.Word;
                    }
                }
                else if (context.Instance is MetaColumn)
                {
                    if (context.PropertyDescriptor.Name == "Name")
                    {
                        frm.Text = "Edit column name";
                        frm.textBox.ConfigurationManager.Language = "sql";
                        frm.textBox.LineWrapping.Mode = ScintillaNET.LineWrappingMode.Word;
                    }
                }
                else if (context.Instance is SealSecurity)
                {
                    if (context.PropertyDescriptor.Name == "Script" || context.PropertyDescriptor.Name == "ProviderScript")
                    {
                        template = ((SealSecurity)context.Instance).ProviderScript;
                        frm.TypeForCheckSyntax = typeof(SecurityUser);
                        frm.Text = "Edit security script";
                        frm.textBox.ConfigurationManager.Language = "cs";
                    }
                }
                else if (context.Instance is MetaTable)
                {
                    if (context.PropertyDescriptor.Name == "DefinitionScript")
                    {
                        template = razorTableDefinitionScriptTemplate;
                        frm.TypeForCheckSyntax = typeof(MetaTable);
                        frm.Text = "Edit the script to define the table";
                        frm.textBox.ConfigurationManager.Language = "cs";
                    }
                    else if (context.PropertyDescriptor.Name == "LoadScript")
                    {
                        template = razorTableLoadScriptTemplate;
                        frm.TypeForCheckSyntax = typeof(MetaTable);
                        frm.Text = "Edit the default script to load the table";
                        frm.textBox.ConfigurationManager.Language = "cs";
                    }
                }
                else if (context.Instance is ReportModel)
                {
                    if (context.PropertyDescriptor.Name == "PreLoadScript")
                    {
                        template = razorModelPreLoadScriptTemplateNoSQL;
                        frm.TypeForCheckSyntax = typeof(ReportModel);
                        frm.Text = "Edit the script executed before table load";
                        frm.textBox.ConfigurationManager.Language = "cs";
                    }
                    else if (context.PropertyDescriptor.Name == "FinalScript")
                    {
                        template = razorTableFinalScriptTemplate;
                        frm.TypeForCheckSyntax = typeof(ReportModel);
                        frm.Text = "Edit the final script executed for the model";
                        frm.textBox.ConfigurationManager.Language = "cs";
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
                        frm.TypeForCheckSyntax = typeof(ReportModel);
                        frm.textBox.ConfigurationManager.Language = "cs";
                    }
                }
                else if (context.Instance is TasksFolder || context.Instance is MetaSource)
                {
                    template = razorTasksTemplate;
                    frm.TypeForCheckSyntax = typeof(ReportTask);
                    frm.Text = "Edit the script that will be added to all task scripts";
                    frm.textBox.ConfigurationManager.Language = "cs";
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
                    frm.textBox.IsReadOnly = true;
                    frm.okToolStripButton.Visible = false;
                    frm.cancelToolStripButton.Text = "Close";
                }
                frm.checkSyntaxToolStripButton.Visible = (frm.TypeForCheckSyntax != null);

                if (svc.ShowDialog(frm) == DialogResult.OK)
                {
                    if (frm.textBox.Text.Trim() != template || string.IsNullOrEmpty(template)) value = frm.textBox.Text;
                    else if (frm.textBox.Text.Trim() == template && !string.IsNullOrEmpty(template)) value = "";
                }
            }
            return value;
        }
    }
}
