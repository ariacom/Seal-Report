//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using Seal.Helpers;
using System.Data;
using System.IO;
using System.Threading;
using RazorEngine.Templating;
using System.Diagnostics;
using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using DocumentFormat.OpenXml.Bibliography;

namespace Seal.Model
{
    /// <summary>
    /// Main worker class that handles a report execution and rendering.
    /// </summary>
    public class ReportExecution
    {
        //Actions Keywords...
        public const string ActionCommand = "action";

        public const string ActionExecuteReport = "ActionExecuteReport";
        public const string ActionRefreshReport = "ActionRefreshReport";
        public const string ActionCancelReport = "ActionCancelReport";
        public const string ActionUpdateViewParameter = "ActionUpdateViewParameter";
        public const string ActionViewHtmlResult = "HtmlResult";
        public const string ActionViewHtmlResultFile = "HtmlResultFile";
        public const string ActionViewPrintResult = "PrintResult";
        public const string ActionViewPDFResult = "PDFResult";
        public const string ActionViewExcelResult = "ExcelResult";
        public const string ActionViewCSVResult = "CSVResult";
        public const string ActionNavigate = "ActionNavigate";
        public const string ActionLogin = "ActionLogin";
        public const string ActionLogout = "ActionLogout";
        public const string ActionSetUserInfo = "ActionSetUserInfo";
        public const string ActionGetNavigationLinks = "ActionGetNavigationLinks";
        public const string ActionGetTableData = "ActionGetTableData";
        public const string ActionGetEnumValues = "ActionGetEnumValues";
        public const string ActionUpdateEnumValues = "ActionUpdateEnumValues";
        public const string ActionExecuteFromTrigger = "ActionExecuteFromTrigger";

        //Html Ids Keywords
        public const string HtmlId_header_form = "header_form";
        public const string HtmlId_progress_bar = "progress_bar";
        public const string HtmlId_progress_bar_tasks = "progress_bar_tasks";
        public const string HtmlId_progress_bar_models = "progress_bar_models";
        public const string HtmlId_execution_messages = "execution_messages";
        public const string HtmlId_parameter_view_id = "parameter_view_id";
        public const string HtmlId_parameter_view_name = "parameter_view_name";
        public const string HtmlId_parameter_view_value = "parameter_view_value";
        public const string HtmlId_navigation_id = "navigation_id";
        public const string HtmlId_navigation_parameters = "navigation_parameters";
        public const string HtmlId_execution_guid = "execution_guid";
        public const string HtmlId_navigation_menu = "nav_menu";
        public const string HtmlId_parameter_tableload = "parameter_tableload";
        public const string HtmlId_viewid_tableload = "viewid_tableload";
        public const string HtmlId_pageid_tableload = "pageid_tableload";
        public const string HtmlId_id_load = "id_load";
        public const string HtmlId_values_load = "values_load";
        public const string HtmlId_filter_enumload = "filter_enumload";
        public const string HtmlId_parameter_enumload = "parameter_enumload";

        /// <summary>
        /// Current report being executed
        /// </summary>
        public Report Report = null;

        /// <summary>
        /// Root report when navigation has occured
        /// </summary>
        public Report RootReport = null;

        /// <summary>
        /// The parameter used if the execution was for a navigation
        /// </summary>
        public string NavigationParameter = null;

        /// <summary>
        /// Object that can be used at run-time for any purpose
        /// </summary>
        public object Tag;

        /// <summary>
        /// Render the report and returns the result
        /// </summary>
        public string Render()
        {
            //Render report
            Report.LogMessage("Rendering report...");

            string templateErrors = "";
            ReportView masterView = Report.ExecutionView;

            masterView.InitTemplates(masterView, ref templateErrors);
            if (!string.IsNullOrEmpty(templateErrors))
            {
                Report.LogMessage(templateErrors);
                Report.ExecutionErrors += templateErrors;
            }

            string result = masterView.Parse();
            if (!string.IsNullOrEmpty(masterView.Error))
            {
                result += Helper.ToHtml(string.Format("{0}\r\nExecution errors:\r\n{1}\r\nExecution messages:\r\n{2}", masterView.Error, Report.ExecutionErrors, Report.ExecutionMessages));
            }

            return result;
        }

        /// <summary>
        /// Render the report result and convert it if necessary in Excel or PDF format
        /// </summary>
        public void RenderResult()
        {
            string result = "";
            Report.PdfConversion = (Report.Format == ReportFormat.pdf);

            Report.Status = ReportStatus.RenderingResult;
            if (Report.HasExternalViewer && Report.Format != ReportFormat.pdf)
            {
                //use the children to render in a new extension file
                result = Report.ExecutionView.ParseChildren();
                if (Report.Format == ReportFormat.custom && !File.Exists(Report.ResultFilePath))
                {
                    File.WriteAllText(Report.ResultFilePath, "Error using Custom format: Report.ResultFilePath must be set in a custom view script.", Encoding.UTF8);
                }
            }
            else
            {
                //normal result rendering
                result = Render();
            }

            try
            {
                if (Report.Format != ReportFormat.custom) File.WriteAllText(Report.ResultFilePath, result.Trim(), Report.ResultFileEncoding);
            }
            catch (Exception ex)
            {
                //unable to write in the result file -> get one from temp or web publish...
                string newFolder = FileHelper.TempApplicationDirectory;
                string newPath = FileHelper.GetUniqueFileName(Path.Combine(newFolder, Report.ResultFileName), "." + Report.ResultExtension);
                Report.ExecutionErrors += string.Format("Unable to write to '{0}'.\r\nChanging report result to '{1}'.\r\n{2}\r\n", Report.ResultFilePath, newPath, ex.Message);
                Report.ResultFilePath = newPath;
                File.WriteAllText(Report.ResultFilePath, result.Trim(), Report.ResultFileEncoding);
            }

            if (Report.Format == ReportFormat.pdf)
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
                    if (ex.InnerException != null) Report.ExecutionErrors += "\r\n" + ex.InnerException.Message;
                }
                Report.PdfConversion = false;
            }
            else if (Report.Format == ReportFormat.excel)
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
                    if (ex.InnerException != null) Report.ExecutionErrors += "\r\n" + ex.InnerException.Message;
                }
            }
        }

        /// <summary>
        /// Render only the HTML result for display 
        /// </summary>
        public void RenderHTMLDisplay()
        {
            Report.Status = ReportStatus.RenderingDisplay;
            //Case of pure HTML, we render Both as the Display and the Result file are the same
            if (Report.IsBasicHTMLWithNoOutput)
            {
                Report.ResultFilePath = Report.HTMLDisplayFilePath;
            }
            string result = Render();
            Debug.WriteLine(string.Format("RenderHTMLDisplay {0} {1} {2}", Report.HTMLDisplayFilePath, Report.Status, Report.ExecutionGUID));
            File.WriteAllText(Report.HTMLDisplayFilePath, result, Encoding.UTF8);
        }

        /// <summary>
        /// Render the HTML display for the Web Server
        /// </summary>
        public void RenderHTMLDisplayForViewer()
        {
            string result = Render();
            Debug.WriteLine(string.Format("RenderHTMLDisplayForViewer {0} {1} {2}", Report.HTMLDisplayFilePath, Report.Status, Report.ExecutionGUID));

            File.WriteAllText(Report.HTMLDisplayFilePath, result, Encoding.UTF8);
        }

        /// <summary>
        /// Execute the current report
        /// </summary>
        public void Execute()
        {
            Report.ExecutionMessages = "";
            Report.ExecutionErrors = "";
            Report.HasValidationErrors = false;

            Report.InitForExecution();
            if (Report.HasErrors)
            {
                Report.Cancel = true;
                //Audit
                if (Report.ExecutionContext != ReportExecutionContext.TaskScheduler) Audit.LogReportAudit(Report.HasErrors ? AuditType.ReportExecutionError : AuditType.ReportExecution, Report.SecurityContext, Report, null);
                //Log files
                Report.LogExecution();
            }
            else
            {
                if (Report.ForOutput) Report.OutputToExecute.Information = "";
                string userInfo = "No security context";
                if (Report.SecurityContext != null) userInfo = string.Format("User:'{0}' Groups:'{1}'", Report.SecurityContext.Name, Report.SecurityContext.SecurityGroupsDisplay);
                Report.LogMessage("Starting execution of '{0}' ({1})...", Path.GetFileNameWithoutExtension(Report.FilePath), userInfo);
                Report.ExecutionCommonRestrictions = null;
                Report.ExecutionViewRestrictions = null;
                //Force rebuild of the lists and values shared
                _ = Report.ExecutionCommonRestrictions;
                _ = Report.ExecutionViewRestrictions;

                Report.Status = ReportStatus.Executing;
                Report.Cancel = false;
                Report.ExecutionStartDate = DateTime.Now;
                Task.Run(() => ExecuteAsync());
            }
        }

        private async void ExecuteAsync()
        {
            try
            {
                try
                {
                    if (Report.InputRestrictions.Count > 0 && Report.ExecutionContext != ReportExecutionContext.TaskScheduler && !Report.CheckingExecution && !Report.IsNavigating)
                    {
                        //check input restrictions if report has already been executed
                        CheckInputRestrictions();
                    }

                    //Init taks progression
                    foreach (var task in Report.Tasks) task.Progression = 0;

                    //Tasks before model
                    if (!Report.Cancel) executeTasks(ExecutionStep.BeforeModel);

                    //Build models
                    if (!Report.Cancel)
                    {
                        await buildModelsAsync();
                    }

                    //Tasks before rendering
                    if (!Report.Cancel) executeTasks(ExecutionStep.BeforeRendering);
                }
                catch (Exception ex)
                {
                    Report.TemplateParsingErrors = ex.Message;
                }
                //Render report
                Report.ExecutionRenderingDate = DateTime.Now;
                //Render first to get the result file, necessary if new extension or for output
                if (!Report.Cancel && !Report.IsBasicHTMLWithNoOutput)
                {
                    RenderResult();
                    Report.LogMessage("Report result generated in '{0}'", Report.DisplayResultFilePath);
                }

                if (!Report.Cancel && Report.OutputToExecute != null)
                {
                    if (!Report.HasErrors)
                    {
                        //Tasks before output
                        executeTasks(ExecutionStep.BeforeOutput);

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

                //Tasks after execution
                if (!Report.Cancel) executeTasks(ExecutionStep.AfterExecution);

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
                Debug.WriteLine(string.Format("ExecuteThread {0} {1}", Report.Status, Report.ExecutionGUID));
                Report.ExecutionEndDate = DateTime.Now;

                if (!Report.HasValidationErrors)
                {
                    //Audit
                    if (Report.ExecutionContext != ReportExecutionContext.TaskScheduler) Audit.LogReportAudit(Report.HasErrors ? AuditType.ReportExecutionError : AuditType.ReportExecution, Report.SecurityContext, Report, null);
                    //Log files
                    Report.LogExecution();
                }
            }
        }

        /// <summary>
        /// Add an execution error
        /// </summary>
        public void SetError(string error, params object[] args)
        {
            //strings are supposed to be thread-safe...
            Report.LogMessage(error, args);
            Report.ExecutionErrors += string.Format("{0} {1}\r\n", DateTime.Now.ToLongTimeString(), string.Format(error, args));
        }

        private static bool validateDateKeyword(Report report, string val, string dateMessage)
        {
            string val2 = report.TranslateDateKeywordsToEnglish(val);
            if (!ReportRestriction.HasDateKeyword(val2))
            {
                report.HasValidationErrors = true;
                report.ExecutionErrors += string.Format("{0}: '{1}'.\r\n{2}\r\n", report.Translate("Invalid date"), val, dateMessage);
                return false;
            }
            return true;
        }

        private static bool validateNumeric(Report report, string val)
        {
            foreach (var v in ReportRestriction.GetVals(val))
            {
                double d;
                if (!Helper.ValidateNumeric(v, out d))
                {
                    report.HasValidationErrors = true;
                    report.ExecutionErrors += string.Format("{0}: '{1}'\r\n", report.Translate("Invalid numeric value"), v);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Set restriction values from input 1,2,3,4
        /// </summary>
        public static void SetRestrictions(Report report, ReportRestriction restriction, string val1, string val2, string val3, string val4, bool checkRequired)
        {
            string dateMessage = report.Translate("Use the date format '{0}' or one of the following keywords:", report.CultureInfo.DateTimeFormat.ShortDatePattern) + " " + report.DateKeywordsList;

            DateTime dt;

            string val = val1;
            if (restriction.IsDateTime)
            {
                if (string.IsNullOrEmpty(val))
                {
                    restriction.Date1 = DateTime.MinValue;
                    restriction.Date1Keyword = "";
                }
                else if (DateTime.TryParse(val, report.CultureInfo, DateTimeStyles.None, out dt))
                {
                    restriction.Date1Keyword = "";
                    restriction.Date1 = dt;
                }
                else if (validateDateKeyword(report, val, dateMessage)) restriction.Date1Keyword = report.TranslateDateKeywordsToEnglish(val);
            }
            else if (restriction.IsNumeric)
            {
                if (string.IsNullOrEmpty(val) || validateNumeric(report, val)) restriction.Value1 = val;
            }
            else restriction.Value1 = val;

            //check required flag
            if (checkRequired && restriction.Required && string.IsNullOrEmpty(val))
            {
                report.HasValidationErrors = true;
                report.ExecutionErrors += string.Format("{0} '{1}'\r\n", report.Translate("A value is required for"), restriction.DisplayNameElTranslated);
            }

            if (restriction.Prompt != PromptType.PromptOneValue)
            {
                val = val2;
                //check required flag
                if (restriction.Prompt == PromptType.PromptTwoValues && checkRequired && restriction.Required && string.IsNullOrEmpty(val))
                {
                    report.HasValidationErrors = true;
                    report.ExecutionErrors += string.Format("{0} '{1}'\r\n", report.Translate("A value is required for"), restriction.DisplayNameElTranslated);
                }

                if (restriction.IsDateTime)
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        restriction.Date2 = DateTime.MinValue;
                        restriction.Date2Keyword = "";
                    }
                    else if (DateTime.TryParse(val, report.CultureInfo, DateTimeStyles.None, out dt))
                    {
                        restriction.Date2Keyword = "";
                        restriction.Date2 = dt;
                    }
                    else if (validateDateKeyword(report, val, dateMessage)) restriction.Date2Keyword = report.TranslateDateKeywordsToEnglish(val);
                }
                else if (restriction.IsNumeric)
                {
                    if (string.IsNullOrEmpty(val) || validateNumeric(report, val)) restriction.Value2 = val;
                }
                else restriction.Value2 = val;

                if (restriction.Prompt != PromptType.PromptTwoValues)
                {
                    val = val3;
                    if (restriction.IsDateTime)
                    {
                        if (string.IsNullOrEmpty(val))
                        {
                            restriction.Date3 = DateTime.MinValue;
                            restriction.Date3Keyword = "";
                        }
                        else if (DateTime.TryParse(val, report.CultureInfo, DateTimeStyles.None, out dt))
                        {
                            restriction.Date3Keyword = "";
                            restriction.Date3 = dt;
                        }
                        else if (validateDateKeyword(report, val, dateMessage)) restriction.Date3Keyword = report.TranslateDateKeywordsToEnglish(val);
                    }
                    else if (restriction.IsNumeric)
                    {
                        if (string.IsNullOrEmpty(val) || validateNumeric(report, val)) restriction.Value3 = val;
                    }
                    else restriction.Value3 = val;

                    val = val4;
                    if (restriction.IsDateTime)
                    {
                        if (string.IsNullOrEmpty(val))
                        {
                            restriction.Date4 = DateTime.MinValue;
                            restriction.Date4Keyword = "";
                        }
                        else if (DateTime.TryParse(val, report.CultureInfo, DateTimeStyles.None, out dt))
                        {
                            restriction.Date4Keyword = "";
                            restriction.Date4 = dt;
                        }
                        else if (validateDateKeyword(report, val, dateMessage)) restriction.Date4Keyword = report.TranslateDateKeywordsToEnglish(val);
                    }
                    else if (restriction.IsNumeric)
                    {
                        if (string.IsNullOrEmpty(val) || validateNumeric(report, val)) restriction.Value4 = val;
                    }
                    else restriction.Value4 = val;
                }
            }
        }


        void setRestriction(ReportRestriction restriction)
        {
            string op = Report.GetInputRestriction(restriction.OperatorHtmlId);
            if (!string.IsNullOrEmpty(op) && restriction.ChangeOperator)
            {
                //Change operator only if allowed and not value only
                if (op != Operator.ValueOnly.ToString()) restriction.Operator = (Operator)Enum.Parse(typeof(Operator), op);
            }

            //change values only if operator is specified
            if (!string.IsNullOrEmpty(op))
            {
                if (restriction.IsEnum)
                {
                    List<string> selected_enum = new List<string>();
                    foreach (var enumVal in restriction.EnumRE.Values)
                    {
                        var val = Report.GetInputRestriction(restriction.OptionHtmlId + enumVal.HtmlId);
                        if (val.ToLower() == "true")
                        {
                            selected_enum.Add(enumVal.Id);
                            //Only one restriction
                            if (restriction.Prompt == PromptType.PromptOneValue) break;
                            //Only 2 restrictions
                            if (restriction.Prompt == PromptType.PromptTwoValues && selected_enum.Count == 2) break;
                        }
                    }
                    if (selected_enum.Count > 0)
                        restriction.EnumValues = selected_enum;
                    else
                        restriction.EnumValues.Clear();

                    //check required flag
                    if (restriction.EnumValues.Count == 0 && restriction.Required)
                    {
                        Report.HasValidationErrors = true;
                        Report.ExecutionErrors += string.Format("{0} '{1}'\r\n", Report.Translate("A value is required for"), restriction.DisplayNameElTranslated);
                    }
                }
                else
                {
                    SetRestrictions(Report, restriction,
                           Report.GetInputRestriction(restriction.ValueHtmlId + "_1"),
                           Report.GetInputRestriction(restriction.ValueHtmlId + "_2"),
                           Report.GetInputRestriction(restriction.ValueHtmlId + "_3"),
                           Report.GetInputRestriction(restriction.ValueHtmlId + "_4"),
                           true
                           );
                }
            }

            //Disable Allow API to avoid reset of the values...
            if (restriction.Prompt == PromptType.None && restriction.AllowAPI) restriction.AllowAPI = false;
        }

        /// <summary>
        /// Check the current input restriction values
        /// </summary>
        public void CheckInputRestrictions()
        {
            try
            {
                foreach (ReportRestriction restriction in Report.ExecutionInputValues.Where(i => i.Prompt != PromptType.None || i.AllowAPI))
                {
                    setRestriction(restriction);
                }

                foreach (ReportModel model in Report.ExecutionModels)
                {
                    foreach (ReportRestriction restriction in model.AllExecutionRestrictions.Where(i => i.Prompt != PromptType.None || i.AllowAPI))
                    {
                        setRestriction(restriction);
                    }

                    if (model.IsLINQ)
                    {
                        foreach (ReportModel subModel in model.LINQSubModels)
                        {
                            foreach (ReportRestriction restriction in subModel.AllExecutionRestrictions.Where(i => i.Prompt != PromptType.None || i.AllowAPI))
                            {
                                setRestriction(restriction);
                            }
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

        /// <summary>
        /// List of models being executed
        /// </summary>
        Dictionary<string, ReportModel> _runningModels = new Dictionary<string, ReportModel>();

        /// <summary>
        /// List of No SQL subtables being executed
        /// </summary>
        Dictionary<string, MetaTable> _runningSubTables = new Dictionary<string, MetaTable>();

        async Task buildModelsAsync()
        {
            Report.LogMessage("Starting to build models...");

            _runningModels.Clear();
            _runningSubTables.Clear();
            //Build SQL and Fill Result table
            var sets = (from model in Report.ExecutionModels orderby model.ExecutionSet select model.ExecutionSet).Distinct();
            foreach (var set in sets)
            {
                Report.LogMessage("Build models of Execution Set {0}...", set);
                var tasks = new List<Task>();
                foreach (ReportModel model in Report.ExecutionModels.Where(i => i.ExecutionSet == set))
                {
                    tasks.Add(buildResultTables(model));
                }
                await Task.WhenAll(tasks);
            }
            Report.RenderOnly = false;
            _runningModels.Clear();
            _runningSubTables.Clear();

            //Cancel execution
            if (Report.Cancel)
            {
                foreach (ReportModel model in Report.Models)
                {
                    model.CancelCommand();
                }
            }
        }

        private async Task buildResultTables(ReportModel model)
        {
            await LoadResultTableModel(model);
            BuildResultPagesModel(model);
        }

        /// <summary>
        /// Load the result table of a report model
        /// </summary>
        public async Task LoadResultTableModel(ReportModel model)
        {
            try
            {
                if (!model.ExecResultTableLoaded)
                {
                    //Handle render only
                    if (model.ResultTable == null || !Report.RenderOnly)
                    {
                        Report.LogMessage("Model '{0}': Loading result table...", model.Name);
                        await model.FillResultTableAsync(_runningModels, _runningSubTables);

                        if (!string.IsNullOrEmpty(model.ExecutionError)) throw new Exception(model.ExecutionError);
                    }
                    model.ExecResultTableLoaded = true;
                }
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(model.ExecutionError)) model.ExecutionError = ex.Message;
                SetError("Error in model '{0}': {1}", model.Name, ex.Message);
            }
        }

        /// <summary>
        /// Build the result pages of a report model
        /// </summary>
        public void BuildResultPagesModel(ReportModel model)
        {
            try
            {
                if (!Report.Cancel && model.ResultTable == null && string.IsNullOrEmpty(model.ExecutionError)) throw new Exception("The Result Table of the model was not loaded. Call BuildResultTableModel() first...");

                model.SetColumnsName();

                if (!model.ExecResultPagesBuilt)
                {
                    if (!Report.Cancel)
                    {
                        Report.LogMessage("Model '{0}': Building pages...", model.Name);
                        buildPages(model);
                    }
                    model.Progression = 75; //75% 
                    if (!Report.Cancel)
                    {
                        Report.LogMessage("Model '{0}': Building tables...", model.Name);
                        buildTables(model);
                    }
                    model.Progression = 80; //80% 
                    if (!Report.Cancel)
                    {
                        Report.LogMessage("Model '{0}': Building totals...", model.Name);
                        buildTotals(model);
                    }
                    model.Progression = 85; //85% 
                    //Scripts
                    if (!Report.Cancel && model.HasCellScript) handleCellScript(model);
                    //Series 
                    if (!Report.Cancel && model.HasSerie) buildSeries(model);
                    //Final sort
                    if (!Report.Cancel) finalSort(model);
                    //Sub-totals
                    if (!Report.Cancel) buildSubTotals(model);
                    //Final script
                    if (!Report.Cancel) handleFinalScript(model);
                    model.Progression = 100; //100% 
                    model.ExecResultPagesBuilt = true;
                }
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(model.ExecutionError)) model.ExecutionError = ex.Message;
                SetError("Error in model '{0}': {1}", model.Name, ex.Message);
            }
        }

        void executeTasks(ExecutionStep step)
        {
            var tasks = Report.ExecutionTasks.Where(i => i.Step == step && i.Enabled).OrderBy(i => i.SortOrder).ToList();
            if (tasks.Count > 0)
            {
                Report.LogMessage("Executing report tasks for step '{0}'...", Helper.GetEnumDescription(typeof(ExecutionStep), step));
            }

            //Temp list to avoid dynamic changes during the task
            foreach (var task in tasks)
            {
                try
                {
                    if (Report.TaskToExecute != null && task != Report.TaskToExecute) continue; //Exec only one task
                    Report.LogMessage("Starting task '{0}'", task.Name);
                    task.Execution = this;

                    var threadTask = Task.Run(() => TaskExecuteAsync(task));

                    while (!Report.Cancel)
                    {
                        if (threadTask.IsCompleted) break;
                        Thread.Sleep(50);
                    }
                    //Cancel execution
                    if (Report.Cancel)
                    {
                        task.Cancel();
                        int cnt = 10; //Wait up to 5 seconds
                        while (--cnt >= 0 && !threadTask.IsCompleted) Thread.Sleep(500);
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

        private void TaskExecuteAsync(ReportTask task)
        {
            try
            {
                task.Execute();
            }
            catch (Exception ex)
            {
                var message = ex.Message + (ex.InnerException != null ? "\r\n" + ex.InnerException.Message : "");
                Report.LogMessage("Error in task '{0}': {1}\r\n", task.Name, message);
                if (!task.IgnoreError)
                {
                    Report.ExecutionErrors = message;
                    Report.ExecutionErrorStackTrace = ex.StackTrace;
                    task.CancelReport = true;
                }
            }
        }

        /*
        private async Task TaskExecuteAsync(ReportTask task, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                try
                {
                    task.Execute();
                }
                catch (Exception ex)
                {
                    var message = ex.Message + (ex.InnerException != null ? "\r\n" + ex.InnerException.Message : "");
                    Report.LogMessage("Error in task '{0}': {1}\r\n", task.Name, message);
                    if (!task.IgnoreError)
                    {
                        Report.ExecutionErrors = message;
                        Report.ExecutionErrorStackTrace = ex.StackTrace;
                        task.CancelReport = true;
                    }
                }
            });
        }*/

        private void setSubReportNavigation(ResultCell[] cellsToAssign, ResultCell[] cellValues)
        {
            for (int i = 0; i < cellsToAssign.Length && _processSubReports; i++)
            {
                if (cellsToAssign[i] != null && cellsToAssign[i].Element != null && cellsToAssign[i].Element.MetaColumn != null)
                {
                    foreach (var subreport in cellsToAssign[i].Element.MetaColumn.SubReports)
                    {
                        foreach (var guid in subreport.Restrictions)
                        {
                            bool done = false;
                            for (int j = 0; j < cellValues.Length && !done; j++)
                            {
                                if (guid == cellValues[j].Element.MetaColumnGUID)
                                {
                                    cellsToAssign[i].SubReportValues.Add(cellValues[j]);
                                    done = true;
                                }
                            }
                            //try in the cells themselves...
                            for (int j = 0; j < cellsToAssign.Length && !done; j++)
                            {
                                if (guid == cellsToAssign[j].Element.MetaColumnGUID)
                                {
                                    cellsToAssign[i].SubReportValues.Add(cellsToAssign[j]);
                                    done = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        bool _processSubReports = false;
        private void buildPages(ReportModel model)
        {
            _processSubReports = model.Elements.Exists(i => i.MetaColumn.SubReports.Count > 0);

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
                ResultCell[] hiddenValues = GetResultCells(PivotPosition.Hidden, row, model);

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
                    currentPage = new ResultPage() { Report = Report, Model = model };
                    //Create Page table
                    currentPage.Pages = pageValues;
                    model.Pages.Add(currentPage);
                    //Set navigation values if any
                    setSubReportNavigation(pageValues, hiddenValues);
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
                    //Set navigation values if any
                    setSubReportNavigation(rowValues, hiddenValues);
                }

                int columnIndex = 0;
                if (columnValues.Length > 0)
                {
                    columnIndex = FindDimension(columnValues, currentPage.Columns);
                    columnValues = currentPage.Columns[columnIndex];
                    //Set navigation values if any
                    setSubReportNavigation(columnValues, hiddenValues);
                }

                if (dataValues.Length > 0)
                {
                    ResultData data = new ResultData() { Row = rowValues, Column = columnValues, Data = dataValues, Hidden = hiddenValues };
                    if (!currentPage.Datas.ContainsKey(rowValues))
                    {
                        currentPage.Datas.Add(rowValues, new List<ResultData>());
                    }
                    currentPage.Datas[rowValues].Add(data);
                }
            }

            if (model.Pages.Count == 0)
            {
                model.Pages.Add(new ResultPage() { Report = Report, Model = model });

            }
        }

        private void buildTables(ReportModel model)
        {
            initialSort(model);

            ResultCell[] headerPageValues = GetHeaderCells(PivotPosition.Page, model);
            ResultCell[] headerRowValues = GetHeaderCells(PivotPosition.Row, model);
            ResultCell[] headerColumnValues = GetHeaderCells(PivotPosition.Column, model);
            ResultCell[] headerDataValues = GetHeaderCells(PivotPosition.Data, model);

            if (headerDataValues.Length == 0 && headerRowValues.Length > 0 && headerColumnValues.Length > 0) Report.LogMessage("WARNING for Model '{0}': Row and Column elements are set but no Data element is specified. Please add a Data element in your model.", model.Name);

            //Summary table headers
            model.SummaryTable = new ResultTable();
            if (model.Pages.Count > 1)
            {
                model.SummaryTable.Lines.Add(headerPageValues);
                model.SummaryTable.BodyStartColumn = 0;
                model.SummaryTable.BodyStartRow = 1;
            }
            foreach (ResultPage page in model.Pages)
            {
                if (Report.Cancel) break;
                //If we have rows and columns OR if there are cell scripts, we have to resort the rows and columns as the SQL cannot do that directly...
                if ((page.Rows.Count > 0 && page.Columns.Count > 0) || model.Elements.Count(e => !string.IsNullOrEmpty(e.CellScript) && e.IsSorted) > 0)
                {
                    if (ResultCell.ShouldSort(page.Rows)) page.Rows.Sort(ResultCell.CompareCells);
                    if (ResultCell.ShouldSort(page.Columns)) page.Columns.Sort(ResultCell.CompareCells);
                }

                //Summary table values
                if (model.Pages.Count > 1) model.SummaryTable.Lines.Add(page.Pages);
                //Page table
                if (page.Rows.Count > 0)
                {
                    page.PageTable = new ResultTable();
                    page.PageTable.Lines.Add(headerPageValues);
                    page.PageTable.Lines.Add(page.Pages);
                }

                //Data table
                page.DataTable = new ResultTable();

                //Calculate line width
                int width = headerRowValues.Length + Math.Max(headerColumnValues.Length, headerDataValues.Length * Math.Max(1, page.Columns.Count));
                ResultCell[] line;

                //First line, only if column values
                if (headerColumnValues.Length > 0 && model.ShowFirstLine)
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
                        //Case 1 Data with at least 1 column, or no data with 1 row and a column, one line less as the titles is already set in first cell
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
                    for (int i = 0; i < row.Length && i < width; i++) line[i] = row[i];

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

                //Handle set Zero to Null
                if (model.Elements.Exists(i => i.PivotPosition == PivotPosition.Data && i.SetNullToZero))
                {
                    for (int col = page.DataTable.BodyStartColumn; col < page.DataTable.ColumnCount; col++)
                    {
                        for (int row = 0; row < page.DataTable.RowCount; row++)
                        {
                            var cell = page.DataTable[row, col];
                            if (cell != null && !cell.IsTotal && cell.Element != null && cell.Element.PivotPosition == PivotPosition.Data && cell.Element.SetNullToZero)
                            {
                                for (int row2 = page.DataTable.BodyStartRow; row2 < page.DataTable.RowCount; row2++)
                                {
                                    var cell2 = page.DataTable[row2, col];
                                    if (cell2.Element == null && cell2.Value == null)
                                    {
                                        cell2.Element = cell.Element;
                                        cell2.Value = 0;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }

                //Handle hidden
                if (model.Elements.Exists(i => i.PivotPosition == PivotPosition.Data && (i.ShowTotal == ShowTotal.RowHidden || i.ShowTotal == ShowTotal.RowColumnHidden)))
                {
                    for (int col = 0; col < page.DataTable.ColumnCount; col++)
                    {
                        for (int row = 0; row < page.DataTable.RowCount; row++)
                        {
                            var cell = page.DataTable[row, col];
                            if (cell != null && cell.Element != null && !cell.IsTotal && !cell.IsTotalTotal && (cell.Element.ShowTotal == ShowTotal.RowHidden || cell.Element.ShowTotal == ShowTotal.RowColumnHidden))
                            {
                                page.DataTable.SetColumnHidden(col);
                                break;
                            }
                        }
                    }
                }
            }
        }


        private void buildTotals(ReportModel model)
        {
            var colTotalElements = model.GetElements(PivotPosition.Data).Where(e => e.ShowTotal == ShowTotal.Column || e.ShowTotal == ShowTotal.RowColumn || e.ShowTotal == ShowTotal.RowColumnHidden || e.CalculationOption == CalculationOption.PercentageColumn);
            var rowTotalElements = model.GetElements(PivotPosition.Data).Where(e => e.ShowTotal == ShowTotal.Row || e.ShowTotal == ShowTotal.RowColumn || e.ShowTotal == ShowTotal.RowHidden || e.ShowTotal == ShowTotal.RowColumnHidden || e.CalculationOption == CalculationOption.PercentageRow || e.CalculationOption == CalculationOption.PercentageAll);
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
                            ResultTotalCell totalCell = new ResultTotalCell() { Element = element, IsTotal = true, IsTotalTotal = false };
                            for (int j = 0; j < page.DataTable.Lines.Count; j++)
                            {
                                ResultCell cell = page.DataTable.Lines[j][i];
                                if (cell != null && !cell.IsTitle && element == cell.Element) totalCell.Cells.Add(cell);
                            }
                            totalCell.Calculate();

                            if (element.ShowTotal == ShowTotal.Column || element.ShowTotal == ShowTotal.RowColumn || element.ShowTotal == ShowTotal.RowColumnHidden)
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
                                if (element.IsNumeric && element.CalculationOption == CalculationOption.PercentageColumn)
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
                    if (model.GetElements(PivotPosition.Data).Count(e => e.ShowTotal == ShowTotal.Column || e.ShowTotal == ShowTotal.RowColumn || e.ShowTotal == ShowTotal.RowColumnHidden) > 0) page.DataTable.Lines.Add(totalLine);
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
                            ResultTotalCell totalCell = new ResultTotalCell() { Element = element, IsTotal = true, IsTotalTotal = (element.ShowTotal == ShowTotal.RowColumn && i == page.DataTable.Lines.Count - 1) };
                            bool isHeaderLine = false;
                            foreach (var cell in rowLine)
                            {
                                if (cell != null && !cell.IsTitle && element == cell.Element) totalCell.Cells.Add(cell);
                                else if (cell != null && cell.IsTitle && element == cell.Element) isHeaderLine = true;
                            }

                            totalCell.Calculate();

                            //Add the cell
                            if (element.ShowTotal == ShowTotal.Row || element.ShowTotal == ShowTotal.RowColumn || element.ShowTotal == ShowTotal.RowHidden || element.ShowTotal == ShowTotal.RowColumnHidden)
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
                                    if (isHeaderLine && rowTotalElements.Count() > 1) Helper.AddValue(ref value, " ", Report.TranslateElement(element, element.DisplayNameEl));
                                    if (!string.IsNullOrEmpty(value)) totalCell.IsTitle = true;
                                    totalCell.Value = value;
                                }
                                rowLine[rowLine.Length - 1] = totalCell;
                                page.DataTable.Lines[i] = rowLine;
                            }

                            //Handle calculations
                            if (!totalCell.IsTitle)
                            {
                                if (element.IsNumeric && element.CalculationOption == CalculationOption.PercentageRow)
                                {
                                    foreach (ResultCell cell in totalCell.Cells) cell.Value = cell.DoubleValue / totalCell.DoubleValue;
                                    totalCell.Value = 1;
                                }
                                else if (element.IsNumeric && element.CalculationOption == CalculationOption.PercentageAll)
                                {
                                    ResultTotalCell totalTotalCell = page.DataTable.TotalCells.FirstOrDefault(c => c.Element == element);
                                    if (totalTotalCell != null)
                                    {
                                        foreach (ResultCell cell in totalCell.Cells) cell.Value = cell.DoubleValue / totalTotalCell.DoubleValue;
                                        if (totalCell != totalTotalCell) totalCell.Value = totalCell.DoubleValue / totalTotalCell.DoubleValue;
                                    }
                                }

                                if (element.IsNumeric && i == page.DataTable.Lines.Count - 1 && element.CalculationOption != CalculationOption.No)
                                {
                                    //case of total of total cell with calc options, set value to 1
                                    totalCell.Value = 1;
                                }
                            }
                        }
                    }
                }

                //Set total totals to 1 if calculation options.
                foreach (ResultTotalCell cell in page.DataTable.TotalCells.Where(i => i.Element.CalculationOption != CalculationOption.No && i.Element.IsNumeric)) cell.Value = 1;

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
                        tttLine[i] = new ResultTotalCell() { Element = line0[i].Element, IsTotal = true, IsTitle = true, Value = (i == 0 ? Report.Translate("Total") : "") };
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
                model.SummaryTable.BodyEndRow = model.SummaryTable.Lines.Count;
                model.SummaryTable.Lines.Add(tttLine);
            }

        }


        private void executeCellScript(ResultCell cell)
        {
            string script = cell.Element.CellScript;
            try
            {
                RazorHelper.CompileExecute(script, cell);
            }
            catch (Exception ex)
            {
                Report.ExecutionMessages += string.Format("Error got when executing Cell Script for '{0}' in model '{1}'\r\n{2}\r\n", cell.Element.DisplayNameEl, cell.Element.Model.Name, ex.Message);
            }
        }

        void handleCustomScripts(ReportModel model, ResultPage page, ResultTable table, bool subTotalsOnly = false)
        {
            if (table == null) return;

            for (int row = 0; row < table.Lines.Count; row++)
            {
                var line = table.Lines[row];
                for (int col = 0; col < line.Length; col++)
                {
                    var cell = line[col];
                    if (cell.Element != null && !string.IsNullOrWhiteSpace(cell.Element.CellScript))
                    {
                        if (subTotalsOnly && !cell.IsSubTotal) continue;

                        cell.ContextRow = row;
                        cell.ContextCol = col;
                        cell.ContextTable = table;
                        cell.ContextPage = page;
                        cell.ContextModel = model;
                        if (!cell.ContextIsPageTable || (cell.ContextIsPageTable && cell.IsTitle))
                        {
                            //Do not execute the script for values of the page table as it will be done in the summary table (cells are referenced)
                            executeCellScript(cell);
                        }
                    }
                }
            }
        }

        private void handleCellScript(ReportModel model)
        {
            foreach (ResultPage page in model.Pages)
            {
                if (Report.Cancel) break;

                handleCustomScripts(model, page, page.DataTable);
                handleCustomScripts(model, page, page.PageTable);

            }
            handleCustomScripts(model, null, model.SummaryTable);
        }

        private void buildSubTotals(ReportModel model)
        {
            if (model.HasSubTotals)
            {
                Report.LogMessage("Processing sub-totals...");
                ResultTable summaryTable = model.SummaryTable;
                foreach (ResultPage page in model.Pages)
                {
                    if (Report.Cancel) break;

                    ResultTable dataTable = page.DataTable;
                    if (dataTable.BodyStartRow == dataTable.BodyEndRow) continue;

                    ResultTotalCell[] subTotalLine = null;
                    List<ResultTotalCell> totalCells = new List<ResultTotalCell>();
                    int i = dataTable.BodyStartRow, cols = dataTable.ColumnCount;
                    string breakValue = "";
                    while (i < dataTable.BodyEndRow)
                    {
                        string currentValue = "";
                        for (int j = 0; j < cols; j++)
                        {
                            var cell = dataTable[i, j];
                            if (cell.Element != null && cell.Element.ShowSubTotals) currentValue += cell.DisplayValue + "§";
                        }
                        if (currentValue != breakValue)
                        {
                            var newSubTotalLine = new ResultTotalCell[cols];
                            for (int j = 0; j < cols; j++)
                            {
                                var sourceCell = dataTable[i, j];
                                var newCell = new ResultTotalCell() { IsSubTotal = true };
                                newSubTotalLine[j] = newCell;
                                if (sourceCell.Element != null && sourceCell.Element.PivotPosition == PivotPosition.Data)
                                {
                                    totalCells.Add(newCell);
                                    newCell.Element = sourceCell.Element;
                                    newCell.FinalCssClass += " text-right";
                                    newCell.Cells.Add(sourceCell);
                                }
                            }
                            newSubTotalLine[0].Value = Report.Translate("Subtotal");

                            if (subTotalLine != null)
                            {
                                dataTable.Lines.Insert(i, subTotalLine);
                                dataTable.BodyEndRow++;
                                i++;
                            }
                            breakValue = currentValue;
                            subTotalLine = newSubTotalLine;
                        }
                        else
                        {
                            for (int j = 0; j < cols; j++)
                            {
                                if (subTotalLine[j].Element != null && subTotalLine[j].Element.PivotPosition == PivotPosition.Data)
                                {
                                    subTotalLine[j].Cells.Add(dataTable[i, j]);
                                }
                                else if (subTotalLine[j].Element == null && dataTable[i, j].Element != null && dataTable[i, j].Element.PivotPosition == PivotPosition.Data)
                                {
                                    var sourceCell = dataTable[i, j];
                                    totalCells.Add(subTotalLine[j]);
                                    subTotalLine[j].Element = sourceCell.Element;
                                    subTotalLine[j].FinalCssClass += " text-right";
                                    subTotalLine[j].Cells.Add(sourceCell);
                                }
                            }
                        }
                        i++;
                    }
                    dataTable.Lines.Insert(i, subTotalLine);
                    foreach (var cell in totalCells) cell.Calculate();
                    dataTable.BodyEndRow++;
                }

                foreach (ResultPage page in model.Pages)
                {
                    if (Report.Cancel) break;
                    handleCustomScripts(model, page, page.DataTable, true);
                }
            }
        }

        private void handleFinalScript(ReportModel model)
        {
            model.ExecuteLoadScript(model.FinalScript, "Final Script", model);
        }

        private void buildSeries(ReportModel model)
        {
            foreach (ResultPage page in model.Pages)
            {
                if (Report.Cancel) break;
                model.ExecNVD3ChartType = "";
                model.ExecChartJSType = "";
                model.ExecPlotlyChartType = "";
                page.ChartInitDone = false;

                foreach (List<ResultData> datas in page.Datas.Values)
                {
                    if (Report.Cancel) break;

                    foreach (ResultData data in datas)
                    {
                        if (Report.Cancel) break;

                        ResultCell[] xPrimaryDimensions = GetXSerieCells(AxisType.Primary, data.Row, data.Column, model);

                        int primaryIndex = FindDimension(xPrimaryDimensions, page.PrimaryXDimensions);
                        xPrimaryDimensions = page.PrimaryXDimensions[primaryIndex];
                        setSubReportNavigation(xPrimaryDimensions, data.Hidden);

                        ResultCell[] xSecondaryDimensions = GetXSerieCells(AxisType.Secondary, data.Row, data.Column, model);
                        int secondaryIndex = FindDimension(xSecondaryDimensions, page.SecondaryXDimensions);
                        xSecondaryDimensions = page.SecondaryXDimensions[secondaryIndex];
                        setSubReportNavigation(xSecondaryDimensions, data.Hidden);

                        ResultCell[] primarySplitterCells = GetSplitterSerieCells(AxisType.Primary, data.Row, data.Column, model);
                        string primarySplitterValues = Helper.ConcatCellValues(primarySplitterCells, ",");
                        ResultCell[] secondarySplitterCells = GetSplitterSerieCells(AxisType.Secondary, data.Row, data.Column, model);
                        string secondarySplitterValues = Helper.ConcatCellValues(secondarySplitterCells, ",");

                        foreach (var dataCell in data.Data.Where(i => i.Element.IsSerie))
                        {
                            var serieElement = dataCell.Element;
                            ResultCell[] xValues = (serieElement.XAxisType == AxisType.Primary ? xPrimaryDimensions : xSecondaryDimensions);
                            string splitterValue = (serieElement.XAxisType == AxisType.Primary ? primarySplitterValues : secondarySplitterValues);
                            ResultCell[] splitterCells = (serieElement.XAxisType == AxisType.Primary ? primarySplitterCells : secondarySplitterCells);
                            ResultSerie serie = page.Series.FirstOrDefault(i => i.Element == serieElement && i.SplitterValues == splitterValue);
                            if (serie == null)
                            {
                                serie = new ResultSerie() { Element = serieElement, SplitterValues = splitterValue, SplitterCells = splitterCells };
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
                            executeCellScript(serieValue.Yvalue);
                        }
                    }
                }
            }
        }


        private void initialSort(ReportModel model)
        {
            model.UpdateFinalSortOrders();
            foreach (var page in model.Pages)
            {
                //If we have rows and columns ...
                if (page.Rows.Count > 0 && page.Columns.Count > 0)
                {
                    if (ResultCell.ShouldSort(page.Rows)) page.Rows.Sort(ResultCell.CompareCells);
                    if (ResultCell.ShouldSort(page.Columns)) page.Columns.Sort(ResultCell.CompareCells);
                }
            }
        }


        private void finalSort(ReportModel model)
        {
            model.UpdateFinalSortOrders();
            foreach (var page in model.Pages)
            {
                //Sort also series axis
                if (model.HasSerie)
                {
                    if (ResultCell.ShouldSort(page.PrimaryXDimensions)) page.PrimaryXDimensions.Sort(ResultCell.CompareCells);
                    if (ResultCell.ShouldSort(page.SecondaryXDimensions)) page.SecondaryXDimensions.Sort(ResultCell.CompareCells);

                    if (page.Series.Count > 0 && page.Series[0].SplitterCells != null)
                    {
                        if (ResultCell.ShouldSort(page.Series[0].SplitterCells)) page.Series.Sort(ResultSerie.CompareSeries);
                    }
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
             where element.PivotPosition == position && !element.IsForNavigation
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
                        string result = RazorHelper.CompileExecute(output.PreScript, output);
                        if (result != null && result == "0")
                        {
                            output.Information = Report.Translate("Pre-execution script returns 0. The report output generation has been cancelled.");
                            Report.LogMessage("Pre-execution script returns 0. The report output generation has been cancelled.");
                            Report.Cancel = true;
                        }
                        else if (result != null)
                        {
                            Report.LogMessage("The script returns:" + result.Trim());
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
                        output.Device.Process(Report);
                        if (!string.IsNullOrEmpty(output.PostScript))
                        {
                            Report.LogMessage("Executing Post-execution script.");
                            RazorHelper.CompileExecute(output.PostScript, output);
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
                    var extraMessage = "";
                    if (ex.InnerException != null) extraMessage += "\r\n" + ex.InnerException.Message;
                    if (output.Device is OutputEmailDevice) extraMessage += string.Format("\r\n\r\nUsing the Server Manager, check the configuration of the device: '{0}'", output.Device.FullName);

                    SetError("Error processing output '{0}'\r\n{1}{2}", output.Name, ex.Message, extraMessage);
                }
                finally
                {
                    if (Report.Cancel) Report.ResultFilePath = null;
                }
            }
        }

        /// <summary>
        /// Return the report of a given schedule GUID
        /// </summary>
        public static Report GetScheduledReport(Microsoft.Win32.TaskScheduler.TaskFolder taskFolder, string reportPath, string reportGUID, string scheduleGUID, Repository repository)
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
                    foreach (var oldTask in taskFolder.GetTasks().Where(i => i.Definition.RegistrationInfo.Source.EndsWith(scheduleGUID)))
                    {
                        taskFolder.DeleteTask(oldTask.Name);
                    }
                }
            }
            return report;
        }


        /// <summary>
        /// Return the report of a given schedule GUID
        /// </summary>
        public static ReportSchedule GetReportSchedule(Microsoft.Win32.TaskScheduler.TaskFolder taskFolder, Report report, string scheduleGUID)
        {
            ReportSchedule schedule = report.Schedules.FirstOrDefault(i => i.GUID == scheduleGUID);
            if (schedule == null)
            {
                //Remove the schedule
                foreach (var oldTask in taskFolder.GetTasks().Where(i => i.Definition.RegistrationInfo.Source.EndsWith(scheduleGUID)))
                {
                    taskFolder.DeleteTask(oldTask.Name);
                }
            }
            return schedule;
        }

        static void InitReportSchedule(string scheduleGUID, out Report report, out ReportSchedule schedule)
        {
            if (string.IsNullOrEmpty(scheduleGUID)) throw new Exception("No schedule GUID specified !\r\n");
            Repository repository = Repository.Instance;

            var taskService = new Microsoft.Win32.TaskScheduler.TaskService();
            var taskFolder = taskService.RootFolder.SubFolders.FirstOrDefault(i => i.Name == repository.Configuration.TaskFolderName);
            if (taskFolder == null) throw new Exception(string.Format("Unable to find schedule task folder '{0}'\r\nCheck your configuration...", repository.Configuration.TaskFolderName));

            var task = taskFolder.GetTasks().FirstOrDefault(i => i.Definition.RegistrationInfo.Source.EndsWith(scheduleGUID));
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
                Helper.WriteLogEntryScheduler(EventLogEntryType.Error, "No email device is defined in the repository to send the error. Please use the Server Manager application to define at least an Email Device.");
            }
            else
            {
                try
                {
                    MailMessage message = new MailMessage();
                    message.From = new MailAddress(Helper.IfNullOrEmpty(from, device.SenderEmail));
                    Helper.AddEmailAddresses(message.To, to);
                    message.Subject = subject;
                    message.Body = body;
                    SmtpClient client = device.SmtpClient;
                    client.Send(message);
                }
                catch (Exception emailEx)
                {
                    Helper.WriteLogEntryScheduler(EventLogEntryType.Error, "Error got trying sending notification email using device '{0}'.\r\n{1}", device.FullName, emailEx.Message + (emailEx.InnerException != null ? "\r\n" + emailEx.InnerException.Message : ""));
                }
            }
        }

        /// <summary>
        /// Execute a report schedule
        /// </summary>
        public static void ExecuteReportSchedule(string scheduleGUID, Report refReport = null, ReportSchedule refSchedule = null)
        {
            try
            {
                Report report;
                ReportSchedule schedule;
                bool useSealScheduler = Repository.Instance.UseWebScheduler;
                if (useSealScheduler)
                {
                    report = refReport;
                    schedule = refSchedule;
                }
                else
                {
                    InitReportSchedule(scheduleGUID, out report, out schedule);
                }
                Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Starting execution of schedule '{0} ({1})'.\r\nReport '{2}'\r\nUser '{3}\\{4}'", schedule.Name, scheduleGUID, report.FilePath, Environment.UserDomainName, Environment.UserName);
                int retries = schedule.ErrorNumberOfRetries + 1;
                while (--retries >= 0)
                {
                    if (useSealScheduler)
                    {
                        report = Report.LoadFromFile(refReport.FilePath, refReport.Repository);
                        schedule = report.Schedules.FirstOrDefault(i => i.GUID == scheduleGUID);
                    }
                    else
                    {
                        InitReportSchedule(scheduleGUID, out report, out schedule);
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
                    if (!useSealScheduler) schedule.SynchronizeTask();

                    while (report.IsExecuting)
                    {
                        if (useSealScheduler && !SealReportScheduler.Running)
                        {
                            Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Schedule '{0}': Cancelling report execution...", schedule.Name);
                            report.CancelExecution();
                            break;
                        }
                        Thread.Sleep(100);
                    }

                    //Audit
                    Audit.LogReportAudit(report.HasErrors ? AuditType.ReportExecutionError : AuditType.ReportExecution, report.SecurityContext, report, schedule);

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
                            while (DateTime.Now < newDate)
                            {
                                if (useSealScheduler && !SealReportScheduler.Running)
                                {
                                    retries = 0;
                                    message = string.Format("Schedule '{0}': Cancelling report execution retries...", refSchedule.Name); ;
                                    break;
                                }
                                Thread.Sleep(1000);
                            }
                            Helper.WriteLogEntryScheduler(EventLogEntryType.Information, message);
                        }
                    }
                    else
                    {
                        Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Schedule '{0}' has been executed\r\nReport '{1}", schedule.Name, report.FilePath);

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

                FileHelper.PurgeTempApplicationDirectory();
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Error, "Error got when executing schedule '{0}':\r\n{1}\r\n\r\n{2}", scheduleGUID, ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// Generate the HTML result of the current execution
        /// </summary>
        public string GenerateHTMLResult()
        {
            Report.IsNavigating = false;
            var originalFormat = Report.Format;
            string newPath = FileHelper.GetUniqueFileName(Path.Combine(Report.GenerationFolder, Path.GetFileNameWithoutExtension(Report.ResultFileName) + ".html"));

            Parameter paginationParameter = Report.ExecutionView.Parameters.FirstOrDefault(i => i.Name == Parameter.ServerPaginationParameter);
            bool initialValue = (paginationParameter != null ? paginationParameter.BoolValue : false);
            try
            {
                Report.Format = ReportFormat.html;
                if (paginationParameter != null) paginationParameter.BoolValue = false;
                Report.Status = ReportStatus.RenderingResult;
                Report.ExecutionViewResultFormat = ReportFormat.html.ToString();
                executeTasks(ExecutionStep.BeforeRendering);
                string result = Render();
                File.WriteAllText(newPath, result.Trim(), Encoding.UTF8);
            }
            finally
            {
                Report.ExecutionViewResultFormat = "";
                Report.Format = originalFormat;
                if (paginationParameter != null) paginationParameter.BoolValue = initialValue;
                Report.Status = ReportStatus.Executed;
                Debug.WriteLine(string.Format("GenerateHTMLResult {0} {1}", Report.Status, Report.ExecutionGUID));
            }

            return newPath;
        }

        /// <summary>
        /// Generate the CSV result of the current execution
        /// </summary>
        public string GenerateCSVResult()
        {
            Report.IsNavigating = false;
            var originalFormat = Report.Format;
            string newPath = FileHelper.GetUniqueFileName(Path.Combine(Report.GenerationFolder, Path.GetFileNameWithoutExtension(Report.ResultFileName) + ".csv"));
            try
            {
                Report.Format = ReportFormat.csv;
                Report.Status = ReportStatus.RenderingResult;
                Report.ExecutionViewResultFormat = ReportFormat.csv.ToString();
                executeTasks(ExecutionStep.BeforeRendering);
                string result = Report.ExecutionView.ParseChildren();
                File.WriteAllText(newPath, result.Trim(), Report.ResultFileEncoding);
            }
            finally
            {
                Report.ExecutionViewResultFormat = "";
                Report.Format = originalFormat;
                Report.Status = ReportStatus.Executed;
                Debug.WriteLine(string.Format("GenerateCSVResult {0} {1}", Report.Status, Report.ExecutionGUID));
            }

            return newPath;
        }

        /// <summary>
        /// Generate the HTML Print result of the current execution
        /// </summary>
        public string GeneratePrintResult()
        {
            Report.IsNavigating = false;
            var originalFormat = Report.Format;
            string newPath = FileHelper.GetUniqueFileName(Path.Combine(Report.GenerationFolder, Path.GetFileName(Report.ResultFileName)));
            try
            {
                Report.Format = ReportFormat.print;
                Report.Status = ReportStatus.RenderingResult;
                Report.ExecutionViewResultFormat = ReportFormat.print.ToString();
                executeTasks(ExecutionStep.BeforeRendering);
                string result = Render();
                File.WriteAllText(newPath, result.Trim(), Encoding.UTF8);
            }
            finally
            {
                Report.ExecutionViewResultFormat = "";
                Report.Format = originalFormat;
                Report.Status = ReportStatus.Executed;
                Debug.WriteLine(string.Format("GeneratePrintResult {0} {1}", Report.Status, Report.ExecutionGUID));
            }
            return newPath;
        }

        /// <summary>
        /// Generate the PDF result of the current execution
        /// </summary>
        public string GeneratePDFResult()
        {
            string newPath = "";
            var originalFormat = Report.Format;
            Report.PdfConversion = true;
            Report.ExecutionViewResultFormat = ReportFormat.pdf.ToString();
            executeTasks(ExecutionStep.BeforeRendering);
            try
            {
                string source = GeneratePrintResult();
                newPath = Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source)) + ".pdf";
                Report.ExecutionView.PdfConverter.ConvertHTMLToPDF(source, newPath);
            }
            finally
            {
                Report.ExecutionViewResultFormat = "";
                Report.PdfConversion = false;
                Report.Format = originalFormat;
            }
            return newPath;
        }

        /// <summary>
        /// Generate the Excel result of the current execution
        /// </summary>
        public string GenerateExcelResult()
        {
            var result = "";
            try
            {
                Report.ExecutionViewResultFormat = ReportFormat.excel.ToString();
                executeTasks(ExecutionStep.BeforeRendering);
                string path = FileHelper.GetUniqueFileName(Path.Combine(Report.GenerationFolder, Path.GetFileNameWithoutExtension(Report.ResultFileName)) + ".xlsx");
                result = Report.ExecutionView.ConvertToExcel(path);
            }
            finally
            {
                Report.ExecutionViewResultFormat = "";
            }
            return result;
        }

        public bool IsConvertingToPDF = false; //If true, do not run conversion again
        public bool IsConvertingToExcel = false; //If true, do not run the report again as we are using the result tables...

        /// <summary>
        /// Dynamic enum values selected during the report execution
        /// </summary>
        public Dictionary<MetaEnum, string> CurrentEnumValues = new Dictionary<MetaEnum, string>();

        /// <summary>
        /// Update the current selected enum values during the report execution
        /// </summary>
        public ReportRestriction UpdateEnumValues(string enumId, string values)
        {
            var restriction = Report.ExecutionCommonRestrictions.FirstOrDefault(i => i.OptionValueHtmlId == enumId);

            if (restriction != null && restriction.EnumRE != null)
            {
                if (!CurrentEnumValues.ContainsKey(restriction.EnumRE)) CurrentEnumValues.Add(restriction.EnumRE, null);
                //Build the SQL value
                restriction.EnumValues.Clear();
                if (values == null) values = "";
                foreach (var v in values.Split('\n').Where(i => !string.IsNullOrEmpty(i)))
                {
                    foreach (var ev in restriction.EnumRE.Values)
                    {
                        if (restriction.OptionHtmlId + ev.HtmlId == v) restriction.EnumValues.Add(ev.Id);
                    }
                }
                CurrentEnumValues[restriction.EnumRE] = restriction.IsSQL ? restriction.EnumSQLValue : restriction.EnumLINQValue;
            }
            return restriction;
        }

        /// <summary>
        /// Return the new enum values with a filter applied
        /// </summary>
        public string GetEnumValues(string enumId, string filter)
        {
            var restriction = Report.ExecutionCommonRestrictions.FirstOrDefault(i => i.OptionValueHtmlId == enumId);
            var result = new StringBuilder();
            if (restriction != null && restriction.EnumRE != null)
            {
                var enumRE = restriction.EnumRE;

                //Set current restrictions
                foreach (var r in Report.ExecutionCommonRestrictions.Where(i => i.EnumRE != null))
                {
                    if (!CurrentEnumValues.ContainsKey(r.EnumRE)) CurrentEnumValues.Add(r.EnumRE, null);
                    CurrentEnumValues[r.EnumRE] = r.IsSQL ? r.EnumSQLValue : r.EnumLINQValue;
                }

                var values = new List<MetaEV>();
                if ((enumRE.FilterChars > 0 && filter.Length >= enumRE.FilterChars) || (enumRE.HasDependencies && CurrentEnumValues.Count > 0))
                {
                    values = enumRE.GetSubSetValues(filter, CurrentEnumValues);
                }

                //Apply auto filter if any
                if (values.Count == 0 && enumRE.FilterChars > 0 && filter.Length >= enumRE.FilterChars && string.IsNullOrEmpty(enumRE.SqlDisplay) && string.IsNullOrEmpty(enumRE.ScriptDisplay))
                {
                    foreach (var enumDef in enumRE.Values)
                    {
                        var display = restriction.GetEnumDisplayValue(enumDef.Id).ToLower();
                        if (display.Contains(filter.ToLower()))
                        {
                            values.Add(enumDef);
                        }
                    }
                }

                foreach (var enumDef in enumRE.Values.Where(i => values.Exists(j => i.Id == j.Id)))
                {
                    var display = restriction.GetEnumDisplayValue(enumDef.Id);
                    result.Append(result.Length == 0 ? "[" : ",");
                    result.AppendFormat("{{\"v\":\"{0}\",\"t\":\"{1}\"}}", restriction.OptionHtmlId + enumDef.HtmlId, display.Replace("\"", "\\\""));
                }
                result.Append(result.Length == 0 ? "[]" : "]");
            }
            return result.ToString();
        }
    }
}
