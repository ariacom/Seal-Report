//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Seal.Model;
using System.IO;
using Seal.Helpers;
using RazorEngine.Templating;
using System.Threading;
using System.Web;
using System.Globalization;

namespace Seal.Forms
{
    public partial class ReportViewerForm : Form
    {
        Report _report;
        ReportExecution _execution;
        NavigationContext _navigation = new NavigationContext();

        static Size? LastSize = null;
        static Point? LastLocation = null;

        string _url;

        bool _reportDone = false;
        bool _exitOnClose = false;
        bool _canRender = true;

        public bool CanRender
        {
            get
            {
                bool result = false;
                //check we have a report already executed...
                if (_report != null && _report.Status == ReportStatus.Executed && !_report.HasErrors && _canRender)
                {
                    result = true;
                }
                return CanExecute && result;
            }
        }

        public bool CanExecute
        {
            get
            {
                if (_report == null || !Visible) return true;
                return !_report.IsExecuting;
            }
        }

        HtmlElement HeaderForm
        {
            get
            {
                return webBrowser.Document != null ? webBrowser.Document.Forms[ReportExecution.HtmlId_header_form] : null;
            }
        }

        string GetFormValue(string id)
        {
            string result = "";
            HtmlElement htmlId = HeaderForm.All[id];
            if (htmlId != null) result = htmlId.GetAttribute("value");
            return result;
        }

        public ReportViewerForm(bool exitOnClose, bool showScriptErrors)
        {
            WebBrowserHelper.FixBrowserVersion();

            InitializeComponent();

            ShowIcon = true;
            Icon = Repository.ProductIcon;

            webBrowser.ScriptErrorsSuppressed = !showScriptErrors;
            _exitOnClose = exitOnClose;
        }

        public void ViewReport(Report report, Repository repository, bool render, string viewGUID, string outputGUID, string originalFilePath)
        {
            Show();
            Text = Path.GetFileNameWithoutExtension(originalFilePath) + " - " + Repository.SealRootProductName + " Report Viewer";
            BringToFront();

            Report previousReport = _report;

            _report = report;
            _report.ExecutionContext = ReportExecutionContext.DesignerReport;
            if (string.IsNullOrEmpty(_report.DisplayName)) _report.DisplayName = Path.GetFileNameWithoutExtension(originalFilePath);
            _report.CurrentViewGUID = _report.ViewGUID;

            //execute to output
            if (!string.IsNullOrEmpty(outputGUID))
            {
                _report.OutputToExecute = _report.Outputs.FirstOrDefault(i => i.GUID == outputGUID);
                _report.ExecutionContext = ReportExecutionContext.DesignerOutput;
                if (_report.OutputToExecute != null) _report.CurrentViewGUID = _report.OutputToExecute.ViewGUID;
            }

            //execute with custom view
            if (!string.IsNullOrEmpty(viewGUID)) _report.CurrentViewGUID = viewGUID;

            if (previousReport != null && render)
            {
                //force execution
                var parameter = _report.ExecutionView.Parameters.FirstOrDefault(i => i.Name ==Parameter.ForceExecutionParameter);
                if (parameter != null) parameter.BoolValue = true;

                //set previous data tables and restrictions
                foreach (var model in _report.Models)
                {
                    ReportModel previousModel = previousReport.Models.FirstOrDefault(i => i.GUID == model.GUID);
                    if (previousModel != null)
                    {
                        model.ResultTable = previousModel.ResultTable;
                        model.Restrictions = previousModel.Restrictions;
                        model.RestrictionText = previousModel.RestrictionText;
                        model.Sql = previousModel.Sql;
                    }
                }
                _report.RenderOnly = true;
            }

            _execution = new ReportExecution() { Report = _report };
            _report.InitForExecution();
            if (_report.HasErrors) _report.Cancel = true;
            _execution.RenderHTMLDisplayForViewer();
            _url = "file:///" + _report.HTMLDisplayFilePath;
            webBrowser.Navigate(_url);
        }

        void Execute()
        {
            if (!_report.IsExecuting)
            {
                _execution.Execute();
            }
        }


        void setCurrentExecution()
        {
            string executionGUID = webBrowser.Document.All[ReportExecution.HtmlId_execution_guid].GetAttribute("value");
            if (_navigation.Navigations.ContainsKey(executionGUID))
            {
                _execution = _navigation.Navigations[executionGUID].Execution;
                _report = _execution.Report;
            }
        }

        private void setProgressBarInformation(string id, int progression, string message)
        {
            HtmlElement progress = webBrowser.Document.All[id];
            if (progress != null)
            {
                progress.SetAttribute("aria-valuenow", progression.ToString());
                progress.Style = string.Format("width:{0}%;min-width:140px;", progression);
                progress.SetAttribute("innerHTML", message);
            }

        }

        private bool processAction(string action)
        {
            bool cancelNavigation = false;
            try
            {
                switch (action)
                {
                    case ReportExecution.ActionExecuteReport:
                        setCurrentExecution();
                        cancelNavigation = true;
                        _reportDone = false;
                        if (webBrowser.Document != null)
                        {
                            _report.InputRestrictions.Clear();
                            if (HeaderForm != null)
                            {
                                foreach (HtmlElement element in HeaderForm.All)
                                {
                                    if (element.Id != null)
                                    {
                                        _report.InputRestrictions.Add(element.Id, element.TagName.ToLower() == "option" ? element.GetAttribute("selected") : element.GetAttribute("value"));
                                        //Debug.WriteLine("{0} {1} {2} {3}", element.Id, element.Name, element.GetAttribute("value"), element.GetAttribute("selected"));
                                    }
                                }
                            }
                        }
                        _report.IsNavigating = false;
                        Execute();
                        break;

                    case ReportExecution.ActionRefreshReport:
                        if (_report.IsExecuting)
                        {
                            cancelNavigation = true;
                            HtmlElement messages = webBrowser.Document.All[ReportExecution.HtmlId_execution_messages];
                            if (_report.ExecutionView.GetValue("messages_mode") != "disabled" && messages != null)
                            {
                                messages.SetAttribute("innerHTML", Helper.ToHtml(_report.ExecutionMessages));
                                messages.ScrollTop = messages.ScrollRectangle.Height;
                            }
                            setProgressBarInformation(ReportExecution.HtmlId_progress_bar, _report.ExecutionProgression, _report.ExecutionProgressionMessage);
                            setProgressBarInformation(ReportExecution.HtmlId_progress_bar_tasks, _report.ExecutionProgressionTasks, _report.ExecutionProgressionTasksMessage);
                            setProgressBarInformation(ReportExecution.HtmlId_progress_bar_models, _report.ExecutionProgressionModels, _report.ExecutionProgressionModelsMessage);
                        }
                        else if (!_reportDone)
                        {
                            _navigation.SetNavigation(_execution);
                            cancelNavigation = true;
                            _reportDone = true;
                            _report.IsNavigating = false;
                            _url = "file:///" + _report.HTMLDisplayFilePath;
                            webBrowser.Navigate(_url);
                        }
                        break;

                    case ReportExecution.ActionCancelReport:
                        _execution.Report.LogMessage(_report.Translate("Cancelling report..."));
                        cancelNavigation = true;
                        _report.Cancel = true;
                        break;

                    case ReportExecution.ActionUpdateViewParameter:
                        cancelNavigation = true;
                        _report.UpdateViewParameter(GetFormValue(ReportExecution.HtmlId_parameter_view_id), GetFormValue(ReportExecution.HtmlId_parameter_view_name), GetFormValue(ReportExecution.HtmlId_parameter_view_value));
                        break;

                    case ReportExecution.ActionViewHtmlResult:
                        setCurrentExecution();
                        string resultPath = _execution.GenerateHTMLResult();
                        if (File.Exists(resultPath)) Process.Start(resultPath);
                        cancelNavigation = true;
                        break;

                    case ReportExecution.ActionViewCSVResult:
                        setCurrentExecution();
                        resultPath = _execution.GenerateCSVResult();
                        if (File.Exists(resultPath)) Process.Start(resultPath);
                        cancelNavigation = true;
                        break;

                    case ReportExecution.ActionViewPrintResult:
                        setCurrentExecution();
                        resultPath = _execution.GeneratePrintResult();
                        if (File.Exists(resultPath)) Process.Start(resultPath);
                        cancelNavigation = true;
                        break;

                    case ReportExecution.ActionViewPDFResult:
                        setCurrentExecution();
                        resultPath = _execution.GeneratePDFResult();
                        if (File.Exists(resultPath)) Process.Start(resultPath);
                        cancelNavigation = true;
                        break;

                    case ReportExecution.ActionViewExcelResult:
                        setCurrentExecution();
                        resultPath = _execution.GenerateExcelResult();
                        if (File.Exists(resultPath)) Process.Start(resultPath);
                        cancelNavigation = true;
                        break;

                    case ReportExecution.ActionNavigate:
                        string nav = webBrowser.Document.All[ReportExecution.HtmlId_navigation_id].GetAttribute("value");
                        _execution = _navigation.Navigate(nav, _execution.RootReport);
                        _report = _execution.Report;

                        _canRender = false;
                        cancelNavigation = true;
                        _reportDone = false;
                        Execute();
                        break;

                    case ReportExecution.ActionGetNavigationLinks:
                        cancelNavigation = true;
                        HtmlElement navMenu = webBrowser.Document.All[ReportExecution.HtmlId_navigation_menu];
                        if (navMenu != null) navMenu.SetAttribute("innerHTML", _navigation.GetNavigationLinksHTML(_execution.RootReport));
                        break;

                    case ReportExecution.ActionGetTableData:
                        cancelNavigation = true;
                        string guid = webBrowser.Document.All[ReportExecution.HtmlId_execution_guid].GetAttribute("value");
                        if (_navigation.Navigations.ContainsKey(guid))
                        {
                            var report = _navigation.Navigations[guid].Execution.Report;
                            string viewid = webBrowser.Document.All[ReportExecution.HtmlId_viewid_tableload].GetAttribute("value");
                            string pageid = webBrowser.Document.All[ReportExecution.HtmlId_pageid_tableload].GetAttribute("value");
                            HtmlElement dataload = webBrowser.Document.All[ReportExecution.HtmlId_parameter_tableload];
                            var view = report.ExecutionView.GetView(viewid);
                            if (view != null && view.Model != null)
                            {
                                var page = view.Model.Pages.FirstOrDefault(i => i.PageId == pageid);
                                if (page != null)
                                {
                                    dataload.InnerText = page.DataTable.GetLoadTableData(view, dataload.InnerText);
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                cancelNavigation = true;
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return cancelNavigation;
        }

        private void closeToolStripButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void webBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (HeaderForm != null)
            {
                //Get action from the form
                string action = HeaderForm.GetAttribute(ReportExecution.ActionCommand);
                if (e.Url.AbsolutePath.EndsWith(action)) e.Cancel = processAction(action);
            }
        }

        private void ReportViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
#if DEBUG
            if (_report != null) _report.Repository.FlushTranslationUsage();
#endif
            if (_report != null && _report.IsExecuting) _report.CancelExecution();
            LastSize = Size;
            LastLocation = Location;

            if (_exitOnClose) Application.Exit();
        }

        private void ReportViewerForm_Load(object sender, EventArgs e)
        {
            if (LastSize != null) Size = LastSize.Value;
            if (LastLocation != null) Location = LastLocation.Value;
            this.KeyDown += TextBox_KeyDown;
            this.webBrowser.PreviewKeyDown += WebBrowser_PreviewKeyDown;
        }

        private void WebBrowser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }
    }
}
