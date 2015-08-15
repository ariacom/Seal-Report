﻿//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Seal.Helpers;
using System.Data.OleDb;
using Seal.Converter;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Collections;
using System.Threading;
using RazorEngine.Templating;
using System.Diagnostics;
using System.Globalization;
using System.Net.Mail;
using RazorEngine;
using Microsoft.Win32.TaskScheduler;
using System.Windows.Forms;

namespace Seal.Model
{
    public class ReportExecution
    {
        //Actions Keywords...
        public const string ActionExecuteReport = "ActionExecuteReport";
        public const string ActionRefreshReport = "ActionRefreshReport";
        public const string ActionCancelReport = "ActionCancelReport";
        public const string ActionUpdateViewParameter = "ActionUpdateViewParameter";
        public const string ActionViewHtmlResult = "ActionViewHtmlResult";
        public const string ActionViewPrintResult = "ActionViewPrintResult";
        public const string ActionViewPDFResult = "ActionViewPDFResult";
        public const string ActionViewExcelResult = "ActionViewExcelResult";

        //Html Ids Keywords
        public const string HtmlId_header_form = "header_form";
        public const string HtmlId_processing_message = "processing_message";
        public const string HtmlId_execution_messages = "execution_messages";
        public const string HtmlId_parameter_view_id = "parameter_view_id";
        public const string HtmlId_parameter_view_name = "parameter_view_name";
        public const string HtmlId_parameter_view_value = "parameter_view_value";


        public Report Report;

        Thread _executeThread;

        public string Render()
        {
            //Render report
            Report.LogMessage("Rendering report...");

            string templateErrors = "";
            ReportView masterView = Report.ExecutionView;

            if (Report.ForPDFConversion) SetPDFRootViewHeaderCSS();

            masterView.InitTemplates(masterView, ref templateErrors);
            if (!string.IsNullOrEmpty(templateErrors))
            {
                Report.LogMessage(templateErrors);
                Report.ExecutionErrors += templateErrors;
            }

            string result = "";
            result = masterView.Parse();
            if (!string.IsNullOrEmpty(masterView.Error))
            {
                result += Helper.ToHtml(string.Format("{0}\r\nExecution errors:\r\n{1}\r\nExecution messages:\r\n{2}", masterView.Error, Report.ExecutionErrors, Report.ExecutionMessages));
            }

            return result;
        }

        public void RenderResult()
        {
            if (Report.ForOutput)
            {
                if (Report.OutputToExecute.Device is OutputFolderDevice)
                {
                    //Clean-up previous attached files...
                    foreach (string attachedFile in Directory.GetFiles(Path.GetDirectoryName(Report.ResultFilePath), Report.ResultFilePrefix + "*"))
                    {
                        try { File.Delete(attachedFile); }
                        catch { }
                    }
                }
            }

            string result = "";
            Report.Status = ReportStatus.RenderingResult;
            if (Report.HasExternalViewer && !Report.ForPDFConversion)
            {
                //use the children to render in a new extension file
                result = Report.ExecutionView.ParseChildren();
            }
            else
            {
                //normal result rendering
                result = Render();
            }

            try
            {
                File.WriteAllText(Report.ResultFilePath, result.Trim(), System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                //unable to write in the result file -> get one from temp or web publish...
                string newFolder = (Report.ExecutionContext == ReportExecutionContext.WebReport ? Report.Repository.WebPublishFolder : FileHelper.TempApplicationDirectory);
                string newPath = FileHelper.GetUniqueFileName(Path.Combine(newFolder, Report.ResultFileName), "." + Report.ExecutionView.ExternalViewerExtension);
                Report.ExecutionErrors += string.Format("Unable to write to '{0}'.\r\nChanging report result to '{1}'.\r\n{2}\r\n", Report.ResultFilePath, newPath, ex.Message);
                Report.ResultFilePath = newPath;
                File.WriteAllText(Report.ResultFilePath, result.Trim(), System.Text.Encoding.UTF8);
            }

            if (Report.ForPDFConversion)
            {
                try
                {
                    string folder = Path.GetDirectoryName(Report.ResultFilePath);
                    if (Report.ForOutput && Report.OutputToExecute.Device is OutputFolderDevice)
                    {
                        //PDF for output folder -> the HTML was converted in temp and converted in destination folder
                        folder = Report.OutputFolderDeviceResultFolder;
                    }
                    string newPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(Report.ResultFilePath)) + ".pdf";
                    Report.ExecutionView.PdfConverter.ConvertHTMLToPDF(Report.ResultFilePath, newPath);
                    Report.ResultFilePath = newPath;
                }
                catch (Exception ex)
                {
                    Report.ExecutionErrors = ex.Message;
                }
            }
            else if (Report.ForExcelConversion)
            {
                try
                {
                    string folder = Path.GetDirectoryName(Report.ResultFilePath);
                    if (Report.ForOutput && Report.OutputToExecute.Device is OutputFolderDevice)
                    {
                        //Excel for output folder -> the HTML was converted in temp and converted in destination folder
                        folder = Report.OutputFolderDeviceResultFolder;
                    }
                    string newPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(Report.ResultFilePath)) + ".xlsx";
                    Report.ResultFilePath = Report.ExecutionView.ConvertToExcel(newPath);
                }
                catch (Exception ex)
                {
                    Report.ExecutionErrors = ex.Message;
                }
            }

        }


        public void RenderHTMLDisplay()
        {
            Report.Status = ReportStatus.RenderingDisplay;
            //Case of pure HTML, we render Both as the Display and the Result file are the same
            if (Report.IsBasicHTMLWithNoOutput)
            {
                Report.ResultFilePath = Report.HTMLDisplayFilePath;
            }
            string result = Render();
            File.WriteAllText(Report.HTMLDisplayFilePath, result, System.Text.Encoding.UTF8);
        }

        public void RenderHTMLDisplayForViewer()
        {
            string result = Render();
            File.WriteAllText(Report.HTMLDisplayFilePath, result, System.Text.Encoding.UTF8);
        }


        public void Execute()
        {
            Report.ExecutionMessages = "";
            Report.ExecutionErrors = "";
            Report.HasValidationErrors = false;
            Report.ExecutionAttachedFiles.Clear();

            Report.InitForExecution();
            if (Report.HasErrors)
            {
                Report.Cancel = true;
            }
            else
            {
                if (Report.ForOutput) Report.OutputToExecute.Information = "";
                Report.LogMessage("Starting execution of '{0}'...", Path.GetFileNameWithoutExtension(Report.FilePath));
                Report.ExecutionCommonRestrictions = null;
                Report.Status = ReportStatus.Executing;
                Report.Cancel = false;
                Report.ExecutionStartDate = DateTime.Now;

                _executeThread = new Thread(ExecuteThread);
                _executeThread.Start();
            }
        }

        private void ExecuteThread()
        {
            try
            {
                try
                {
                    if (Report.ExecutionContext != ReportExecutionContext.TaskScheduler)
                    {
                        //check input restrictions
                        CheckInputRestrictions();
                    }

                    executeTasks();
                    //Build models
                    if (!Report.Cancel) buildModels();
                }
                catch (Exception ex)
                {
                    Report.TemplateParsingErrors = ex.Message;
                }
                //Render report
                Report.ExecutionRenderingDate = DateTime.Now;
                //Render first to get the result file, necessary if new extension or for output
                if (!Report.IsBasicHTMLWithNoOutput)
                {
                    RenderResult();
                    Report.LogMessage("Report result generated in '{0}'", Report.DisplayResultFilePath);
                }

                if (Report.OutputToExecute != null)
                {
                    if (!Report.HasErrors)
                    {
                        ProcessOutput();
                    }
                    else
                    {
                        SetError("Report in error. The output is cancelled.");
                        Report.Cancel = true;
                    }
                }

                //Manage the display 
                if (Report.ExecutionContext != ReportExecutionContext.TaskScheduler)
                {
                    //Generate Display HTML
                    RenderHTMLDisplay();
                }

                //Open external result viewer
                if (!Report.HasErrors && !Report.ForOutput && Report.ExecutionContext == ReportExecutionContext.DesignerReport && !Report.CheckingExecution && Report.HasExternalViewer)
                {
                    Process.Start(Report.ResultFilePath);
                }
            }
            catch (Exception ex)
            {
                Report.TemplateParsingErrors = ex.Message;
            }
            finally
            {
                Report.Status = ReportStatus.Executed;
                Report.ExecutionEndDate = DateTime.Now;
            }
        }

        public void SetError(string error, params object[] args)
        {
            //strings are supposed to be thread-safe...
            Report.LogMessage(error, args);
            Report.ExecutionErrors += string.Format("{0} {1}\r\n", DateTime.Now.ToLongTimeString(), string.Format(error, args));
        }

        private bool validateDateKeyword(string val, string dateMessage)
        {
            string val2 = Report.TranslateDateKeywordsToEnglish(val);
            if (!ReportRestriction.HasDateKeyword(val2))
            {
                Report.HasValidationErrors = true;
                Report.ExecutionErrors += string.Format("{0}: '{1}'.\r\n{2}\r\n", Report.Translate("Invalid date"), val, dateMessage);
                return false;
            }
            return true;
        }

        private bool validateNumeric(string val)
        {
            string val2 = Report.TranslateDateKeywordsToEnglish(val);
            Double result;
            if (!Double.TryParse(val, out result))
            {
                Report.HasValidationErrors = true;
                Report.ExecutionErrors += string.Format("{0}: '{1}'\r\n", Report.Translate("Invalid numeric value"), val);
                return false;
            }
            return true;
        }

        private void CheckInputRestrictions()
        {
            try
            {
                string dateMessage = Report.Translate("Use the date format '{0}' or one of the following keywords:", Report.CultureInfo.DateTimeFormat.ShortDatePattern) + " " + Report.DateKeywordsList;
                foreach (ReportModel model in Report.Models)
                {
                    foreach (ReportRestriction restriction in model.ExecutionRestrictions.Where(i => i.Prompt == PromptType.Prompt).Union(model.ExecutionAggregateRestrictions.Where(i => i.Prompt == PromptType.Prompt)))
                    {
                        string val = Report.GetInputRestriction(restriction.OperatorHtmlId);
                        if (!string.IsNullOrEmpty(val)) restriction.Operator = (Operator)Enum.Parse(typeof(Operator), val);

                        if (restriction.IsEnum)
                        {
                            restriction.EnumValues.Clear();
                            for (int i = 0; i < restriction.EnumRE.Values.Count; i++)
                            {
                                var enumVal = restriction.EnumRE.Values[i];
                                val = Report.GetInputRestriction(restriction.OptionHtmlId + i.ToString());
                                if (val.ToLower() == "true") restriction.EnumValues.Add(enumVal.Id);
                            }
                        }
                        else
                        {
                            DateTime dt;
                            val = Report.GetInputRestriction(restriction.ValueHtmlId + "_1");
                            if (restriction.IsDateTime)
                            {
                                if (string.IsNullOrEmpty(val))
                                {
                                    restriction.Date1 = DateTime.MinValue;
                                    restriction.Date1Keyword = "";
                                }
                                else if (DateTime.TryParse(val, Report.CultureInfo, DateTimeStyles.None, out dt))
                                {
                                    restriction.Date1Keyword = "";
                                    restriction.Date1 = dt;
                                }
                                else if (validateDateKeyword(val, dateMessage)) restriction.Date1Keyword = Report.TranslateDateKeywordsToEnglish(val);
                            }
                            else if (restriction.IsNumeric)
                            {
                                if (string.IsNullOrEmpty(val) || validateNumeric(val)) restriction.Value1 = val;
                            }
                            else restriction.Value1 = val;

                            val = Report.GetInputRestriction(restriction.ValueHtmlId + "_2");
                            if (restriction.IsDateTime)
                            {
                                if (string.IsNullOrEmpty(val))
                                {
                                    restriction.Date2 = DateTime.MinValue;
                                    restriction.Date2Keyword = "";
                                }
                                else if (DateTime.TryParse(val, Report.CultureInfo, DateTimeStyles.None, out dt))
                                {
                                    restriction.Date2Keyword = "";
                                    restriction.Date2 = dt;
                                }
                                else if (validateDateKeyword(val, dateMessage)) restriction.Date2Keyword = Report.TranslateDateKeywordsToEnglish(val);
                            }
                            else if (restriction.IsNumeric)
                            {
                                if (string.IsNullOrEmpty(val) || validateNumeric(val)) restriction.Value2 = val;
                            }
                            else restriction.Value2 = val;

                            val = Report.GetInputRestriction(restriction.ValueHtmlId + "_3");
                            if (restriction.IsDateTime)
                            {
                                if (string.IsNullOrEmpty(val))
                                {
                                    restriction.Date3 = DateTime.MinValue;
                                    restriction.Date3Keyword = "";
                                }
                                else if (DateTime.TryParse(val, Report.CultureInfo, DateTimeStyles.None, out dt))
                                {
                                    restriction.Date3Keyword = "";
                                    restriction.Date3 = dt;
                                }
                                else if (validateDateKeyword(val, dateMessage)) restriction.Date3Keyword = Report.TranslateDateKeywordsToEnglish(val);
                            }
                            else if (restriction.IsNumeric)
                            {
                                if (string.IsNullOrEmpty(val) || validateNumeric(val)) restriction.Value3 = val;
                            }
                            else restriction.Value3 = val;

                            val = Report.GetInputRestriction(restriction.ValueHtmlId + "_4");
                            if (restriction.IsDateTime)
                            {
                                if (string.IsNullOrEmpty(val))
                                {
                                    restriction.Date4 = DateTime.MinValue;
                                    restriction.Date4Keyword = "";
                                }
                                else if (DateTime.TryParse(val, Report.CultureInfo, DateTimeStyles.None, out dt))
                                {
                                    restriction.Date4Keyword = "";
                                    restriction.Date4 = dt;
                                }
                                else if (validateDateKeyword(val, dateMessage)) restriction.Date4Keyword = Report.TranslateDateKeywordsToEnglish(val);
                            }
                            else if (restriction.IsNumeric)
                            {
                                if (string.IsNullOrEmpty(val) || validateNumeric(val)) restriction.Value4 = val;
                            }
                            else restriction.Value4 = val;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Report.HasValidationErrors = true;
                Report.ExecutionErrors += ex.Message;
            }

            if (!string.IsNullOrEmpty(Report.ExecutionErrors)) Report.Cancel = true;
        }

        void buildModels()
        {
            Report.LogMessage("Starting to build models...");
            List<Thread> threads = new List<Thread>();

            ReportModel.RunningModels.Clear();
            //Build SQL and Fill Result table
            foreach (ReportModel model in Report.ExecutionModels)
            {
                Thread thread = new Thread(ModelBuildResultTableThread);
                thread.Start(model);
                threads.Add(thread);
            }

            //Check if finished
            bool stillRunning = true;
            while (stillRunning && !Report.Cancel)
            {
                stillRunning = false;
                foreach (var thread in threads)
                {
                    if (thread.IsAlive) stillRunning = true;
                }
                Thread.Sleep(50);
            }
            Report.RenderOnly = false;

            //Cancel execution
            if (Report.Cancel)
            {
                foreach (ReportModel model in Report.Models)
                {
                    model.CancelCommand();
                }

                Thread.Sleep(1000);
                //Aborting resisting threads...
                foreach (var thread in threads)
                {
                    if (thread.IsAlive) thread.Abort();
                }
            }
        }

        private void ModelBuildResultTableThread(object modelParam)
        {
            ReportModel model = modelParam as ReportModel;
            try
            {
                //Handle render only
                if (model.ResultTable == null || !Report.RenderOnly)
                {
                    Report.LogMessage("Model '{0}': Building result set from database...", model.Name);
                    model.FillResultTable();
                    if (!string.IsNullOrEmpty(model.ExecutionError)) throw new Exception(model.ExecutionError);
                }

                //For DEV: Simulate long query
                //Thread.Sleep(5000);
                model.SetColumnsName();
                Report.LogMessage("Model '{0}': Building pages...", model.Name);
                if (!Report.Cancel) buildPages(model);
                Report.LogMessage("Model '{0}': Building tables...", model.Name);
                if (!Report.Cancel) buildTables(model);
                Report.LogMessage("Model '{0}': Building totals...", model.Name);
                if (!Report.Cancel) buildTotals(model);
                //Custom scripts
                if (!Report.Cancel && model.HasCellScript) handleCustomScripts(model);
                //Series 
                if (!Report.Cancel && model.HasSerie) buildSeries(model);
                //Final sort
                if (!Report.Cancel) finalSort(model);
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(model.ExecutionError)) model.ExecutionError = ex.Message;
                SetError("Error in model '{0}': {1}", model.Name, ex.Message);
            }
        }

        void executeTasks()
        {
            if (Report.CheckingExecution) return;

            Report.LogMessage("Executing report tasks...");

            foreach (var task in Report.Tasks.Where(i => i.Enabled).OrderBy(i => i.SortOrder))
            {
                try
                {
                    Report.LogMessage("Starting task '{0}'", task.Name);
                    Thread thread = new Thread(TaskExecuteThread);
                    thread.Start(task);

                    while (!Report.Cancel)
                    {
                        if (!thread.IsAlive) break;
                        Thread.Sleep(50);
                    }
                    //Cancel execution
                    if (Report.Cancel)
                    {
                        if (thread.IsAlive)
                        {
                            task.Cancel();
                            int cnt = 5;
                            while (--cnt >= 0 && thread.IsAlive) Thread.Sleep(1000);
                            //Kill it if necessary...
                            if (thread.IsAlive) thread.Abort();
                        }
                    }
                    if (!string.IsNullOrEmpty(task.DbInfoMessage.ToString()))
                    {
                        Report.LogMessage("Database information message:\r\n{0}", task.DbInfoMessage.ToString().Trim());
                    }
                    Report.LogMessage("Ending task '{0}'\r\n", task.Name);
                }
                catch (Exception ex)
                {
                    if (task.IgnoreError)
                    {
                        Report.LogMessage("Error ignored: Got unexpected error when executing task '{0}'\r\n{1}\r\n", task.Name, ex.Message);
                    }
                    else
                    {
                        throw ex;
                    }
                }

                if (task.CancelReport) Report.Cancel = true;

                if (Report.Cancel) break;
            }
        }


        private void TaskExecuteThread(object taskParam)
        {
            ReportTask task = taskParam as ReportTask;
            try
            {
                task.Execute();
            }
            catch (Exception ex)
            {
                Report.LogMessage("Error in task '{0}': {1}\r\n{2}\r\n", task.Name, ex.Message, ex.StackTrace);
                if (!task.IgnoreError)
                {
                    Report.ExecutionErrors = ex.Message;
                    task.CancelReport = true;
                }
            }
        }

        private void buildPages(ReportModel model)
        {
            model.Pages.Clear();
            if (model.ResultTable == null) return;

            ResultCell[] currentPageValues = null;
            ResultPage currentPage = null;

            //Build pages
            foreach (DataRow row in model.ResultTable.Rows)
            {
                if (Report.Cancel) break;

                ResultCell[] pageValues = GetResultCells(PivotPosition.Page, row, model);
                ResultCell[] rowValues = GetResultCells(PivotPosition.Row, row, model);
                ResultCell[] columnValues = GetResultCells(PivotPosition.Column, row, model);
                ResultCell[] dataValues = GetResultCells(PivotPosition.Data, row, model);

                bool createPage = false;
                if (currentPageValues == null)
                {
                    createPage = true;
                }
                else
                {
                    createPage = IsDifferent(currentPageValues, pageValues);
                }
                currentPageValues = pageValues;

                if (createPage)
                {
                    //Build new page
                    currentPage = new ResultPage() { Report = Report };
                    //Create Page table
                    currentPage.Pages = pageValues;
                    model.Pages.Add(currentPage);
                }

                //Set values in page
                if (rowValues.Length > 0)
                {
                    int rowIndex = 0;
                    if (columnValues.Length == 0)
                    {
                        currentPage.Rows.Add(rowValues);
                        rowIndex = currentPage.Rows.Count - 1;
                    }
                    else
                    {
                        //At least a column, we have to find the current row
                        rowIndex = FindDimension(rowValues, currentPage.Rows);
                    }
                    rowValues = currentPage.Rows[rowIndex];
                }

                int columnIndex = 0;
                if (columnValues.Length > 0)
                {
                    columnIndex = FindDimension(columnValues, currentPage.Columns);
                    columnValues = currentPage.Columns[columnIndex];
                }

                if (dataValues.Length > 0)
                {
                    ResultData data = new ResultData() { Row = rowValues, Column = columnValues, Data = dataValues };
                    if (!currentPage.Datas.ContainsKey(rowValues))
                    {
                        currentPage.Datas.Add(rowValues, new List<ResultData>());
                    }
                    currentPage.Datas[rowValues].Add(data);
                }
            }
        }

        private void buildTables(ReportModel model)
        {
            ResultCell[] headerPageValues = GetHeaderCells(PivotPosition.Page, model);
            ResultCell[] headerRowValues = GetHeaderCells(PivotPosition.Row, model);
            ResultCell[] headerColumnValues = GetHeaderCells(PivotPosition.Column, model);
            ResultCell[] headerDataValues = GetHeaderCells(PivotPosition.Data, model);

            if (headerDataValues.Length == 0 && headerRowValues.Length > 0 && headerColumnValues.Length > 0) Report.LogMessage("WARNING for Model '{0}': Row and Column elements are set but no Data element is specified. Please add a Data element in your model.", model.Name);

            //Summary table headers
            model.SummaryTable = new ResultTable();
            model.SummaryTable.Lines.Add(headerPageValues);

            foreach (ResultPage page in model.Pages)
            {
                if (Report.Cancel) break;

                //Summary table values
                model.SummaryTable.Lines.Add(page.Pages);
                //Page table
                page.PageTable = new ResultTable();
                page.PageTable.Lines.Add(headerPageValues);
                page.PageTable.Lines.Add(page.Pages);
                //Data table
                page.DataTable = new ResultTable();

                //Calculate line width
                int width = headerRowValues.Length + Math.Max(headerColumnValues.Length, headerDataValues.Length * Math.Max(1, page.Columns.Count));
                ResultCell[] line;

                //First line, only if column values
                if (headerColumnValues.Length > 0)
                {
                    line = new ResultCell[width];
                    if (headerDataValues.Length == 1) line[0] = headerDataValues[0]; //Case 1 Data, title in first cell
                    for (int i = 0; i < headerColumnValues.Length; i++) line[headerRowValues.Length + i] = headerColumnValues[i];
                    //case cols, no rows, one data, add data title
                    if (headerColumnValues.Length > 0 && headerRowValues.Length == 0 && headerDataValues.Length == 1 && headerColumnValues.Length < width) line[headerColumnValues.Length] = headerDataValues[0];

                    //Fill empty cells
                    for (int i = 0; i < width; i++) if (line[i] == null) line[i] = new ResultCell() { IsTitle = true };

                    page.DataTable.Lines.Add(line);
                }

                //Intermediate lines, set columns values
                for (int i = 0; i < headerColumnValues.Length + 1; i++)
                {
                    line = new ResultCell[width];
                    if (i < headerColumnValues.Length)
                    {
                        //column values
                        for (int j = 0; j < page.Columns.Count; j++) line[headerRowValues.Length + headerDataValues.Length * j] = page.Columns[j][i];
                    }
                    else
                    {
                        //headers for rows
                        for (int j = 0; j < headerRowValues.Length; j++) line[j] = headerRowValues[j];
                        //headers for data
                        for (int j = 0; j < Math.Max(1, page.Columns.Count); j++)
                        {
                            int index = headerDataValues.Length * j;
                            foreach (var header in headerDataValues) line[headerRowValues.Length + index++] = header;
                        }
                    }

                    if (headerColumnValues.Length > 0 && headerDataValues.Length <= 1 && i == headerColumnValues.Length - 1)
                    {
                        //Case 1 Data with at least 1 column, or no data with 1 row and a cloumn, one line less as the titles is already set in first cell
                        //headers for rows
                        for (int j = 0; j < headerRowValues.Length; j++) line[j] = headerRowValues[j];
                        i++;
                    }

                    //Fill empty cells
                    for (int j = 0; j < width; j++) if (line[j] == null) line[j] = new ResultCell() { IsTitle = (j < headerRowValues.Length) };

                    page.DataTable.Lines.Add(line);
                }

                //Set start row and column
                page.DataTable.BodyStartRow = page.DataTable.Lines.Count;
                page.DataTable.BodyStartColumn = headerRowValues.Length;

                //Finally row and data values
                if (page.Rows.Count == 0 && page.Datas.Count > 0)
                {
                    //Case no column, force one row to display data
                    page.Rows.Add(new ResultCell[0]);
                }

                foreach (var row in page.Rows)
                {
                    if (Report.Cancel) break;

                    line = new ResultCell[width];
                    //Row values
                    for (int i = 0; i < row.Length; i++) line[i] = row[i];
                    //Data values
                    List<ResultData> datas = null;
                    if (row.Length == 0 && page.Datas.Count > 0)
                    {
                        //Case no rows
                        datas = new List<ResultData>();
                        foreach (var data0 in page.Datas) datas.AddRange(data0.Value);
                    }
                    //normal case
                    else if (page.Datas.ContainsKey(row)) datas = page.Datas[row];

                    if (datas != null)
                    {
                        foreach (var data in datas)
                        {
                            //find the index of the column values
                            int columnIndex = 0;
                            if (data.Column.Length > 0) columnIndex = FindDimension(data.Column, page.Columns);

                            for (int i = 0; i < data.Data.Length; i++) line[headerRowValues.Length + headerDataValues.Length * columnIndex + i] = data.Data[i];
                        }
                    }
                    page.DataTable.Lines.Add(line);

                    for (int i = 0; i < width; i++) if (line[i] == null) line[i] = new ResultCell() { };
                }

                //Set end row 
                page.DataTable.BodyEndRow = page.DataTable.Lines.Count;
            }
        }


        private void buildTotals(ReportModel model)
        {
            var colTotalElements = model.GetElements(PivotPosition.Data).Where(e => e.ShowTotal == ShowTotal.Column || e.ShowTotal == ShowTotal.RowColumn || e.CalculationOption == CalculationOption.PercentageColumn);
            var rowTotalElements = model.GetElements(PivotPosition.Data).Where(e => e.ShowTotal == ShowTotal.Row || e.ShowTotal == ShowTotal.RowColumn || e.CalculationOption == CalculationOption.PercentageRow || e.CalculationOption == CalculationOption.PercentageAll);
            var totalElements = colTotalElements.Union(rowTotalElements);
            Dictionary<string, string> compilationKeys = new Dictionary<string, string>();

            foreach (ResultPage page in model.Pages)
            {
                if (Report.Cancel) break;

                //First calculate the total of total cells for each element
                page.DataTable.TotalCells = new List<ResultTotalCell>();
                foreach (var element in totalElements)
                {
                    ResultTotalCell totalTotalCell = new ResultTotalCell() { Element = element, IsTotal = true, IsTotalTotal = true };
                    foreach (var rowLine in page.DataTable.Lines)
                    {
                        foreach (var cell in rowLine.Where(i => i.Element == element && !i.IsTitle && !i.IsTotal))
                        {
                            totalTotalCell.Cells.Add(cell);
                        }
                    }
                    totalTotalCell.Calculate();
                    page.DataTable.TotalCells.Add(totalTotalCell);
                }

                //Totals per columns
                if (colTotalElements.Count() > 0 && page.DataTable.Lines.Count > 0)
                {
                    //We add first one/several final lines (one per element)
                    ResultCell[] totalLine = new ResultCell[page.DataTable.Lines[0].Length];
                    for (int i = 0; i < page.DataTable.Lines[0].Length; i++)
                    {
                        if (Report.Cancel) break;
                        foreach (var element in colTotalElements)
                        {
                            ResultTotalCell totalCell = new ResultTotalCell() { Element = element, IsTotal = true, IsTotalTotal = true };
                            for (int j = 0; j < page.DataTable.Lines.Count; j++)
                            {
                                ResultCell cell = page.DataTable.Lines[j][i];
                                if (cell != null && !cell.IsTitle && element == cell.Element) totalCell.Cells.Add(cell);
                            }
                            totalCell.Calculate();

                            if (element.ShowTotal == ShowTotal.Column || element.ShowTotal == ShowTotal.RowColumn)
                            {
                                //Add titles if not a value
                                if (totalCell.Cells.Count == 0)
                                {
                                    if (i == 0)
                                    {
                                        totalCell.IsTitle = true;
                                        totalCell.Value = Report.Translate("Total");
                                    }
                                }
                                totalLine[i] = totalCell;
                            }

                            //Handle calculation
                            if (!totalCell.IsTitle)
                            {
                                if (element.CalculationOption == CalculationOption.PercentageColumn)
                                {
                                    foreach (ResultCell cell in totalCell.Cells) cell.Value = cell.DoubleValue / totalCell.DoubleValue;
                                    totalCell.Value = 1;
                                }
                            }
                            //This cell is ok for one element, can break
                            if (totalCell.Cells.Count > 0) break;
                        }
                    }

                    //Add line only if a total is set (not only calculation options)
                    if (model.GetElements(PivotPosition.Data).Count(e => e.ShowTotal == ShowTotal.Column || e.ShowTotal == ShowTotal.RowColumn) > 0) page.DataTable.Lines.Add(totalLine);
                }

                //Totals per rows
                if (rowTotalElements.Count() > 0)
                {
                    //We add first one cell for each line (one per element) -> actually we add columns
                    for (int i = page.DataTable.Lines.Count - 1; i >= 0; i--)
                    {
                        if (Report.Cancel) break;
                        bool isTotalTitleSet = false;
                        var rowLine = page.DataTable.Lines[i];
                        //Calculate the row total
                        foreach (var element in rowTotalElements)
                        {
                            ResultTotalCell totalCell = new ResultTotalCell() { Element = element, IsTotal = true, IsTotalTotal = (i == page.DataTable.Lines.Count - 1) };
                            bool isHeaderLine = false;
                            foreach (var cell in rowLine)
                            {
                                if (cell != null && !cell.IsTitle && element == cell.Element) totalCell.Cells.Add(cell);
                                else if (cell != null && cell.IsTitle && element == cell.Element) isHeaderLine = true;
                            }

                            totalCell.Calculate();

                            //Add the cell
                            if (element.ShowTotal == ShowTotal.Row || element.ShowTotal == ShowTotal.RowColumn)
                            {
                                Array.Resize<ResultCell>(ref rowLine, rowLine.Length + 1);
                                //Add titles if not a value
                                if (totalCell.Cells.Count == 0)
                                {
                                    string value = "";
                                    if (i == 0 && !isTotalTitleSet)
                                    {
                                        if (!isHeaderLine) isTotalTitleSet = true;
                                        value = Report.Translate("Total");
                                    }
                                    if (isHeaderLine && rowTotalElements.Count() > 1) Helper.AddValue(ref value, " ", Report.Repository.TranslateElement(element, element.DisplayNameEl));
                                    if (!string.IsNullOrEmpty(value)) totalCell.IsTitle = true;
                                    totalCell.Value = value;
                                }
                                rowLine[rowLine.Length - 1] = totalCell;
                                page.DataTable.Lines[i] = rowLine;
                            }

                            //Handle calculations
                            if (!totalCell.IsTitle)
                            {
                                if (element.CalculationOption == CalculationOption.PercentageRow)
                                {
                                    foreach (ResultCell cell in totalCell.Cells) cell.Value = cell.DoubleValue / totalCell.DoubleValue;
                                    totalCell.Value = 1;
                                }
                                else if (element.CalculationOption == CalculationOption.PercentageAll)
                                {
                                    ResultTotalCell totalTotalCell = page.DataTable.TotalCells.FirstOrDefault(c => c.Element == element);
                                    if (totalTotalCell != null)
                                    {
                                        foreach (ResultCell cell in totalCell.Cells) cell.Value = cell.DoubleValue / totalTotalCell.DoubleValue;
                                        if (totalCell != totalTotalCell) totalCell.Value = totalCell.DoubleValue / totalTotalCell.DoubleValue;
                                    }
                                }

                                if (i == page.DataTable.Lines.Count - 1 && element.CalculationOption != CalculationOption.No)
                                {
                                    //case of total of total cell with calc options, set value to 1
                                    totalCell.Value = 1;
                                }
                            }
                        }
                    }
                }

                //Set total totals to 1 if calculation options.
                foreach (ResultTotalCell cell in page.DataTable.TotalCells.Where(i => i.Element.CalculationOption != CalculationOption.No)) cell.Value = 1;

                //Add totals for Page and Summary tables
                if (page.PageTable != null && page.PageTable.Lines.Count == 2 && page.PageTable.Lines[0].Length > 0)
                {
                    //Add totals for pages
                    foreach (ResultCell cell in page.DataTable.TotalCells.Where(i => i.Element.ShowTotal != ShowTotal.No))
                    {
                        ResultCell[] page0 = page.PageTable.Lines[0];
                        Array.Resize<ResultCell>(ref page0, page0.Length + 1);
                        page0[page0.Length - 1] = new ResultTotalCell() { Element = cell.Element, IsTotal = true, IsTitle = true, Value = cell.Element.DisplayNameEl };
                        page.PageTable.Lines[0] = page0;

                        ResultCell[] page1 = page.PageTable.Lines[1];
                        Array.Resize<ResultCell>(ref page1, page1.Length + 1);
                        page1[page1.Length - 1] = cell;
                        page.PageTable.Lines[1] = page1;
                    }


                }
            }

            //Add totals for summary table
            if (model.Pages.Count > 1)
            {
                List<ResultTotalCell> tttCells = new List<ResultTotalCell>();

                //First titles line
                var line0 = model.SummaryTable.Lines[0];
                foreach (ResultCell cell in model.Pages[0].DataTable.TotalCells.Where(i => i.Element.ShowTotal != ShowTotal.No))
                {
                    Array.Resize<ResultCell>(ref line0, line0.Length + 1);
                    ResultTotalCell totalCell = new ResultTotalCell() { Element = cell.Element, IsTotal = true, IsTitle = true, Value = cell.Element.DisplayNameEl };
                    line0[line0.Length - 1] = totalCell;
                }
                model.SummaryTable.Lines[0] = line0;

                //Then value per page
                int index = 1;
                foreach (ResultPage page in model.Pages)
                {
                    var line = model.SummaryTable.Lines[index];
                    foreach (ResultCell cell in page.DataTable.TotalCells.Where(i => i.Element.ShowTotal != ShowTotal.No))
                    {
                        ResultTotalCell tttCell = tttCells.FirstOrDefault(i => i.Element == cell.Element);
                        if (tttCell == null)
                        {
                            tttCell = new ResultTotalCell() { Element = cell.Element, IsTotal = true, IsTotalTotal = true };
                            tttCells.Add(tttCell);
                        }

                        Array.Resize<ResultCell>(ref line, line.Length + 1);
                        line[line.Length - 1] = cell;
                        tttCell.Cells.Add(cell);
                    }
                    model.SummaryTable.Lines[index] = line;
                    index++;

                    if (index >= model.SummaryTable.Lines.Count) break;
                }

                //Final line: Total of total of total !
                ResultCell[] tttLine = new ResultCell[model.SummaryTable.Lines[0].Length];
                for (int i = 0; i < line0.Length; i++)
                {
                    if (!line0[i].IsTotal)
                    {
                        //empty cell
                        tttLine[i] = new ResultTotalCell() { Element = line0[i].Element, IsTotal = true,  Value = "" };
                    }
                    else
                    {
                        ResultTotalCell tttCell = tttCells.FirstOrDefault(cell => cell.Element == line0[i].Element);
                        if (tttCell != null)
                        {
                            tttCell.Calculate();
                            tttLine[i] = tttCell;
                        }
                    }
                }
                model.SummaryTable.Lines.Add(tttLine);
            }

        }


        private void executeCellScript(ResultCell cell, Dictionary<string, string> compilationKeys)
        {
            string script = cell.Element.CellScript;
            try
            {
                if (!compilationKeys.ContainsKey(script))
                {
                    string newKey = Guid.NewGuid().ToString();
                    Helper.CompileRazor(script, typeof(ResultCell), newKey);
                    compilationKeys.Add(script, newKey);
                }
                string key = compilationKeys[script];
                if (!string.IsNullOrEmpty(key)) Razor.Run(compilationKeys[script], cell);
            }
            catch (Exception ex)
            {
                Report.ExecutionMessages += string.Format("Error got when executing Cell Script for '{0}'\r\n{1}\r\n", cell.Element.DisplayNameEl, ex.Message);
                if (!compilationKeys.ContainsKey(script)) compilationKeys.Add(script, "");
            }
        }

        void handleCustomScripts(ReportModel model, ResultPage page, ResultTable table, Dictionary<string, string> compilationKeys)
        {
            for (int row = 0; row < table.Lines.Count; row++)
            {
                var line = table.Lines[row];
                for (int col = 0; col < line.Length; col++)
                {
                    var cell = line[col];
                    if (cell.Element != null && !string.IsNullOrWhiteSpace(cell.Element.CellScript))
                    {
                        cell.ContextRow = row;
                        cell.ContextCol = col;
                        cell.ContextTable = table;
                        cell.ContextPage = page;
                        cell.ContextModel = model;
                        executeCellScript(cell, compilationKeys);
                    }
                }
            }
        }

        Dictionary<string, string> _compilationKeys = null;

        private void handleCustomScripts(ReportModel model)
        {
            _compilationKeys = new Dictionary<string, string>();

            foreach (ResultPage page in model.Pages)
            {
                if (Report.Cancel) break;

                handleCustomScripts(model, page, page.DataTable, _compilationKeys);
               //We do not handle Page table as calculations will be done in the summary table, as the cells are shared amongst Page and Summary
                //handleCustomScripts(model, page, page.PageTable, _compilationKeys);
            }
            handleCustomScripts(model, null, model.SummaryTable, _compilationKeys);
        }


        private void buildSeries(ReportModel model)
        {
            foreach (ResultPage page in model.Pages)
            {
                if (Report.Cancel) break;

                foreach (List<ResultData> datas in page.Datas.Values)
                {
                    if (Report.Cancel) break;

                    foreach (ResultData data in datas)
                    {
                        if (Report.Cancel) break;

                        ResultCell[] xPrimaryDimensions = GetXSerieCells(AxisType.Primary, data.Row, data.Column, model);
                        int primaryIndex = FindDimension(xPrimaryDimensions, page.PrimaryXDimensions);
                        xPrimaryDimensions = page.PrimaryXDimensions[primaryIndex];
                        ResultCell[] xSecondaryDimensions = GetXSerieCells(AxisType.Secondary, data.Row, data.Column, model);
                        int secondaryIndex = FindDimension(xSecondaryDimensions, page.SecondaryXDimensions);
                        xSecondaryDimensions = page.SecondaryXDimensions[secondaryIndex];

                        string primarySplitterValues = Helper.ConcatCellValues(GetSplitterSerieCells(AxisType.Primary, data.Row, data.Column, model), ",");
                        string secondarySplitterValues = Helper.ConcatCellValues(GetSplitterSerieCells(AxisType.Secondary, data.Row, data.Column, model), ",");

                        foreach ( var dataCell in data.Data.Where(i => i.Element.IsSerie))
                        {
                            var serieElement = dataCell.Element;
                            ResultCell[] xValues = (serieElement.XAxisType == AxisType.Primary ? xPrimaryDimensions : xSecondaryDimensions);
                            string splitterValue = (serieElement.XAxisType == AxisType.Primary ? primarySplitterValues : secondarySplitterValues);
                            ResultSerie serie = page.Series.FirstOrDefault(i => i.Element == serieElement && i.SplitterValues == splitterValue);
                            if (serie == null)
                            {
                                serie = new ResultSerie() { Element = serieElement, SplitterValues = splitterValue };
                                page.Series.Add(serie);
                            }

                            ResultSerieValue serieValue = serie.Values.FirstOrDefault(i => i.XDimensionValues == xValues);
                            if (serieValue == null)
                            {
                                serieValue = new ResultSerieValue() { XDimensionValues = xValues };
                                serieValue.Yvalue = new ResultTotalCell() { Element = serieElement, IsSerie = true };
                                serie.Values.Add(serieValue);
                            }
                            serieValue.Yvalue.Cells.Add(new ResultCell() { Element = serieElement, Value = dataCell.Value, ContextRow = dataCell.ContextRow, ContextCol = dataCell.ContextCol });
                        }
                    }
                }
            }

            //Calculate the serie values
            foreach (ResultPage page in model.Pages)
            {
                if (Report.Cancel) break;
                foreach (var serie in page.Series)
                {
                    foreach (var serieValue in serie.Values)
                    {
                        //Classic calculation
                        serieValue.Yvalue.Calculate();
                        if (!string.IsNullOrEmpty(serieValue.Yvalue.Element.CellScript))
                        {
                            //Cell script option
                            serieValue.Yvalue.ProcessContext();
                            serieValue.Yvalue.ContextTable = page.DataTable;
                            serieValue.Yvalue.ContextPage = page;
                            serieValue.Yvalue.ContextModel = model;
                            executeCellScript(serieValue.Yvalue, _compilationKeys);
                        }
                    }
                }
            }
        }


        private void finalSort(ReportModel model)
        {
            foreach (var page in model.Pages)
            {
                //If we have rows and columns OR if there are cell scripts, we have to resort the rows and columns as the SQL cannot do that directly...
                if ((page.Rows.Count > 0 && page.Columns.Count > 0) || model.Elements.Count(e => !string.IsNullOrEmpty(e.CellScript) && e.IsSorted) > 0)
                {
                    page.Rows.Sort(ResultCell.CompareCells);
                    page.Columns.Sort(ResultCell.CompareCells);
                }
                //Sort also series axis
                if (model.HasSerie)
                {
                    page.PrimaryXDimensions.Sort(ResultCell.CompareCells);
                    page.SecondaryXDimensions.Sort(ResultCell.CompareCells);
                }
            }
        }


        int FindDimension(ResultCell[] valuesToFind, List<ResultCell[]> valuesList)
        {
            for (int i = valuesList.Count - 1; i >= 0; i--)
            {
                if (!IsDifferent(valuesList[i], valuesToFind))
                {
                    return i;
                }
            }
            //Not found, add it to the list
            valuesList.Add(valuesToFind);
            return valuesList.Count - 1;
        }


        bool IsDifferent(ResultCell[] values1, ResultCell[] values2)
        {
            bool result = false;
            if (values1.Length != values2.Length) result = true;
            else
            {
                for (int i = 0; i < values1.Length; i++)
                {
                    if (values1[i].Value.ToString() != values2[i].Value.ToString())
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        ResultCell[] GetHeaderCells(PivotPosition position, ReportModel model)
        {
            return
            (from element in model.Elements
             where element.PivotPosition == position
             select new ResultCell() { Element = element, IsTitle = true, Value = element.DisplayNameEl }).ToArray();
        }


        ResultCell[] GetResultCells(ReportElement[] elements, DataRow row)
        {
            ResultCell[] result = new ResultCell[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                result[i] = new ResultCell() { Element = elements[i], Value = row[elements[i].SQLColumnName] };
            }
            return result.ToArray();
        }

        ResultCell[] GetResultCells(PivotPosition position, DataRow row, ReportModel model)
        {
            ReportElement[] elements = model.Elements.Where(i => i.PivotPosition == position).ToArray();
            return GetResultCells(elements, row);
        }


        ResultCell[] GetResultCells(ReportElement[] elements, ResultCell[] row, ResultCell[] col)
        {
            ResultCell[] result = new ResultCell[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                ResultCell val = row.FirstOrDefault(v => v.Element == elements[i]);
                if (val == null) val = col.FirstOrDefault(v => v.Element == elements[i]);
                result[i] = new ResultCell() { Element = elements[i], Value = val.Value };
            }
            return result.ToArray();
        }

        ResultCell[] GetXSerieCells(AxisType xAxisType, ResultCell[] row, ResultCell[] col, ReportModel model)
        {
            ReportElement[] elements = model.GetXElements(xAxisType).ToArray();
            return GetResultCells(elements, row, col);
        }

        ResultCell[] GetSplitterSerieCells(AxisType xAxisType, ResultCell[] row, ResultCell[] col, ReportModel model)
        {
            ReportElement[] elements = model.GetSplitterElements(xAxisType).ToArray();
            return GetResultCells(elements, row, col);
        }


        public void ProcessOutput()
        {
            if (Report.OutputToExecute != null)
            {
                ReportOutput output = Report.OutputToExecute;
                try
                {
                    Report.LogMessage("Processing result file for output '{0}'", output.Name);

                    if (!string.IsNullOrEmpty(output.PreScript))
                    {
                        Report.LogMessage("Executing Pre-Execution script.");
                        string result = Helper.ParseRazor(output.PreScript, output);
                        if (result != null && result == "0")
                        {
                            output.Information = Report.Translate("Pre-execution script returns 0. The report output generation has been cancelled.");
                            Report.LogMessage("Pre-execution script returns 0. The report output generation has been cancelled.");
                            Report.Cancel = true;
                        }
                        else if (result != null)
                        {
                            Report.LogMessage("The script returns:" + result);
                        }
                    }

                    if (!Report.Cancel)
                    {
                        bool hasRecords = Report.Models.Exists(i => i.ResultTable != null && i.ResultTable.Rows.Count > 0);
                        if (!hasRecords && output.CancelIfNoRecords)
                        {
                            output.Information = Report.Translate("No records. The report output generation has been cancelled.");
                            Report.LogMessage("No records. The report output generation has been cancelled.");
                            Report.Cancel = true;
                        }
                    }

                    if (!Report.Cancel)
                    {
                        Report.LogMessage(output.Device.Process(Report));
                        if (!string.IsNullOrEmpty(output.PostScript))
                        {
                            Report.LogMessage("Executing Post-execution script.");
                            Helper.ParseRazor(output.PostScript, output);
                        }
                    }
                }
                catch (TemplateCompilationException ex)
                {
                    Report.Cancel = true;
                    SetError("Compilation error when parsing execution scripts for the output '{0}':\r\n{1}", output.Name, Helper.GetExceptionMessage(ex));
                }
                catch (Exception ex)
                {
                    Report.Cancel = true;
                    SetError("Error processing output '{0}'\r\n{1}", output.Name, ex.Message);
                }
                finally
                {
                    if (Report.Cancel) Report.ResultFilePath = null;
                }
            }
        }

        public static Report GetScheduledReport(TaskFolder taskFolder, string reportPath, string reportGUID, string scheduleGUID, Repository repository)
        {
            Report report = null;
            if (File.Exists(reportPath)) report = Report.LoadFromFile(reportPath, repository);

            if (!File.Exists(reportPath) || (report != null && report.GUID != reportGUID))
            {
                //Report has been moved or renamed: search report from its GUID in the report folder
                report = repository.FindReport(repository.ReportsFolder, reportGUID);
                if (report == null)
                {
                    //Remove the schedules of the report
                    foreach (Task oldTask in taskFolder.GetTasks().Where(i => i.Definition.RegistrationInfo.Source.EndsWith(scheduleGUID)))
                    {
                        taskFolder.DeleteTask(oldTask.Name);
                    }
                }
            }
            return report;
        }


        public static ReportSchedule GetReportSchedule(TaskFolder taskFolder, Report report, string scheduleGUID)
        {
            ReportSchedule schedule = report.Schedules.FirstOrDefault(i => i.GUID == scheduleGUID);
            if (schedule == null)
            {
                //Remove the schedule
                foreach (Task oldTask in taskFolder.GetTasks().Where(i => i.Definition.RegistrationInfo.Source.EndsWith(scheduleGUID)))
                {
                    taskFolder.DeleteTask(oldTask.Name);
                }
            }
            return schedule;
        }

        static void InitReportSchedule(string scheduleGUID, out Task task, out Report report, out ReportSchedule schedule)
        {
            if (string.IsNullOrEmpty(scheduleGUID)) throw new Exception("No schedule GUID specified !\r\n");

            Repository repository = Repository.Create();
            TaskService taskService = new TaskService();
            TaskFolder taskFolder = taskService.RootFolder.SubFolders.FirstOrDefault(i => i.Name == repository.Configuration.TaskFolderName);
            if (taskFolder == null) throw new Exception(string.Format("Unable to find schedule task folder '{0}'\r\nCheck your configuration...", repository.Configuration.TaskFolderName));

            task = taskFolder.GetTasks().FirstOrDefault(i => i.Definition.RegistrationInfo.Source.EndsWith(scheduleGUID));
            if (task == null) throw new Exception(string.Format("Unable to find schedule '{0}'\r\n", scheduleGUID));

            string reportPath = ReportSchedule.GetTaskSourceDetail(task.Definition.RegistrationInfo.Source, 0);
            string reportGUID = ReportSchedule.GetTaskSourceDetail(task.Definition.RegistrationInfo.Source, 1);
            report = GetScheduledReport(taskFolder, reportPath, reportGUID, scheduleGUID, repository);
            if (report == null) throw new Exception(string.Format("Unable to find report '{0}' for schedule '{1}'\r\nReport schedules have been deleted...", reportGUID, scheduleGUID));

            schedule = GetReportSchedule(taskFolder, report, scheduleGUID);
            if (schedule == null) throw new Exception(string.Format("Unable to find schedule '{0}' in report '{1}'.\r\nSchedule has been deleted", scheduleGUID, report.FilePath));
        }

        static void sendEmail(Report report, string to, string from, string subject, string body)
        {
            OutputEmailDevice device = report.Repository.Devices.OfType<OutputEmailDevice>().FirstOrDefault(i => i.UsedForNotification);
            if (device == null) device = report.Repository.Devices.OfType<OutputEmailDevice>().FirstOrDefault();
            if (device == null)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Error, "No email device is defined in the repository to send the error. Please use the server manager to define at least an email device.");
            }
            else
            {
                try
                {
                    MailMessage message = new MailMessage();
                    message.From = new MailAddress(Helper.IfNullOrEmpty(from, device.SenderEmail));
                    device.AddEmailAddresses(message.To, to);
                    message.Subject = subject;
                    message.Body = body;
                    SmtpClient client = device.SmtpClient;
                    client.Send(message);
                }
                catch (Exception emailEx)
                {
                    Helper.WriteLogEntryScheduler(EventLogEntryType.Error, "Error got trying sending notification email.\r\n{0}", emailEx.Message);
                }
            }
        }


        public static void ExecuteReportSchedule(string scheduleGUID)
        {
            try
            {
                Task task;
                Report report;
                ReportSchedule schedule;
                InitReportSchedule(scheduleGUID, out task, out report, out schedule);
                Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Starting execution of schedule '{0} ({1})'.\r\nReport '{2}'\r\nUser '{3}\\{4}'", schedule.Name, scheduleGUID, report.FilePath, Environment.UserDomainName, Environment.UserName);
                int retries = schedule.ErrorNumberOfRetries + 1;
                while (--retries >= 0)
                {
                    if (report == null || schedule == null || task == null)
                    {
                        InitReportSchedule(scheduleGUID, out task, out report, out schedule);
                    }
                    ReportExecution reportExecution = new ReportExecution() { Report = report };
                    report.ExecutionContext = ReportExecutionContext.TaskScheduler;
                    if (!schedule.IsTasksSchedule)
                    {
                        report.OutputToExecute = schedule.Output;
                        if (report.OutputToExecute == null) throw new Exception("No output defined for the schedule");
                        report.CurrentViewGUID = report.OutputToExecute.ViewGUID;
                    }
                    else
                    {
                        report.CurrentViewGUID = report.ViewGUID;
                    }
                    reportExecution.Execute();
                    schedule.SynchronizeTask();

                    while (report.IsExecuting)
                    {
                        Thread.Sleep(100);
                    }
                    if (report.HasErrors)
                    {
                        string errorMessage = string.Format("Error: Schedule '{0}' has been executed with errors.\r\nReport '{1}'\r\n{2}\r\n", schedule.Name, report.FilePath, report.ExecutionErrors);
                        if (schedule.ErrorNumberOfRetries > 0)
                        {
                            if (schedule.ErrorNumberOfRetries != retries) errorMessage += string.Format("Retry number {0} of {1}.\r\n", schedule.ErrorNumberOfRetries - retries, schedule.ErrorNumberOfRetries, report.ExecutionMessages);
                            else errorMessage += string.Format("The schedule will have up to {0} retries every {1} minute(s).\r\n", schedule.ErrorNumberOfRetries, schedule.ErrorMinutesBetweenRetries);
                        }
                        errorMessage += "\r\n" + report.ExecutionMessages;
                        Helper.WriteLogEntryScheduler(EventLogEntryType.Error, errorMessage);
                        if (!string.IsNullOrEmpty(schedule.ErrorEmailTo) &&
                            (schedule.ErrorEmailSendMode == FailoverEmailMode.All ||
                            (schedule.ErrorEmailSendMode == FailoverEmailMode.First && retries == schedule.ErrorNumberOfRetries) ||
                            (schedule.ErrorEmailSendMode == FailoverEmailMode.Last && retries == 0))
                            )
                        {
                            //error email
                            string subject = Helper.IfNullOrEmpty(schedule.ErrorEmailSubject, string.Format("Report Execution Error '{0}'", report.ExecutionName));
                            sendEmail(report, schedule.ErrorEmailTo, schedule.ErrorEmailFrom, subject, errorMessage);
                        }

                        if (retries > 0)
                        {
                            //wait a while before retrying
                            string message = string.Format("Retrying execution of schedule '{0}' of report '{1}'.\r\nRetry number {2} of {3}.", schedule.Name, report.FilePath, schedule.ErrorNumberOfRetries - retries + 1, schedule.ErrorNumberOfRetries);
                            var newDate = DateTime.Now.AddMinutes(schedule.ErrorMinutesBetweenRetries);
                            report = null;
                            schedule = null;
                            task = null;
                            while (DateTime.Now < newDate) Thread.Sleep(1000); //Future: waiting for a notification to die...
                            Helper.WriteLogEntryScheduler(EventLogEntryType.Information, message);
                        }
                    }
                    else
                    {
                        Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Schedule '{0}' has been executed\r\nReport '{1}\r\n{2}", schedule.Name, report.FilePath, report.ExecutionMessages);
                        if (!string.IsNullOrEmpty(schedule.NotificationEmailTo) && !report.Cancel)
                        {
                            //information email
                            string subject = Helper.IfNullOrEmpty(schedule.NotificationEmailSubject, string.Format("Report Execution '{0}'", report.ExecutionName));
                            string body = Helper.IfNullOrEmpty(schedule.NotificationEmailBody, "The report has been executed successfully.");
                            sendEmail(report, schedule.NotificationEmailTo, schedule.NotificationEmailFrom, subject, body);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Error, "Error got when executing schedule '{0}':\r\n{1}\r\n\r\n{2}", scheduleGUID, ex.Message, ex.StackTrace);
            }
        }


        public string GenerateHTMLResult()
        {
            string newPath = FileHelper.GetUniqueFileName(Path.Combine(Report.DisplayFolder, Path.GetFileName(Report.ResultFileName)));
            if (Report.IsBasicHTMLWithNoOutput)
            {
                try
                {
                    Report.Status = ReportStatus.RenderingResult;
                    string result = Render();
                    File.WriteAllText(newPath, result.Trim(), System.Text.Encoding.UTF8);
                }
                finally
                {
                    Report.Status = ReportStatus.Executed;
                }
            }
            return newPath;
        }


        public string GeneratePrintResult()
        {
            Parameter printParameter = Report.PrintLayoutParameter;
            string newPath = FileHelper.GetUniqueFileName(Path.Combine(Report.DisplayFolder, Path.GetFileName(Report.ResultFileName)));
            if (printParameter != null)
            {
                bool initialValue = printParameter.BoolValue;
                try
                {
                    printParameter.BoolValue = true;
                    Report.Status = ReportStatus.RenderingResult;
                    string result = Render();
                    File.WriteAllText(newPath, result.Trim(), System.Text.Encoding.UTF8);
                }
                finally
                {
                    printParameter.BoolValue = initialValue;
                    Report.Status = ReportStatus.Executed;
                }
            }
            return newPath;
        }

        void SetPDFRootViewHeaderCSS()
        {
            //Hide header by default for PDF...
            var headerCSS = Report.ExecutionView.CSS.FirstOrDefault(i => i.Name == "header");
            if (headerCSS == null)
            {
                headerCSS = new Parameter() { Name = "header" };
                Report.ExecutionView.CSS.Add(headerCSS);
            }
            if (string.IsNullOrEmpty(headerCSS.Value)) headerCSS.Value = "display:none;";
        }

        public string GeneratePDFResult()
        {
            SetPDFRootViewHeaderCSS();
            string source = GeneratePrintResult();
            string newPath = Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source)) + ".pdf";
            Report.ExecutionView.PdfConverter.ConvertHTMLToPDF(source, newPath);
            return newPath;
        }

        public string GenerateExcelResult()
        {
            string path = FileHelper.GetUniqueFileName(Path.Combine(Report.DisplayFolder, Path.GetFileNameWithoutExtension(Report.ResultFileName)) + ".xlsx");
            return Report.ExecutionView.ConvertToExcel(path);
        }

        public bool IsConvertingToExcel = false; //It true, do not run the report again as we are using the result tables...

        //cache management of template compilation
        public static List<string> CompiledViews = new List<string>();
        public static void CompiledViewAdd(string viewGUID)
        {
            lock (CompiledViews)
            {
                if (!CompiledViews.Exists(i => i == viewGUID)) CompiledViews.Add(viewGUID);
            }
        }
        public static bool IsViewCompiled(string viewGUID)
        {
            lock (CompiledViews)
            {
                return CompiledViews.Exists(i => i == viewGUID);
            }
        }
        public static void CompiledViewRemove(string viewGUID)
        {
            lock (CompiledViews)
            {
                if (CompiledViews.Exists(i => i == viewGUID)) CompiledViews.Remove(viewGUID);
            }
        }


    }


}
