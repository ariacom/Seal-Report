//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using Seal.Model;
using System.IO;
using Seal.Helpers;
using System.Web;
using System.Threading;
using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Formats.Asn1;
using System.Text.Json;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Spreadsheet;
using static Seal.Forms.ReportViewerForm;

namespace Seal.Forms
{
    public partial class ReportViewerForm : Form
    {
        Report _report;
        ReportExecution _execution;
        NavigationContext _navigation = new NavigationContext();
        BrowserInterop _browserInterop = null;

        static Size? LastSize = null;
        static Point? LastLocation = null;

        string _url;

        bool _reportDone = false;
        bool _exitOnClose = false;
        bool _canRender = true;
        bool _openDevTool = false;

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

        public ReportViewerForm(bool exitOnClose, bool openDevTool)
        {
            WebBrowserHelper.FixBrowserVersion();

            InitializeComponent();

            ShowIcon = true;
            Icon = Repository.ProductIcon;
            _openDevTool = openDevTool;
            _exitOnClose = exitOnClose;
        }

        public void ViewReport(Report report, bool render, string viewGUID, string outputGUID, string originalFilePath, string taskGUID = null)
        {
            Show();
            Text = Path.GetFileNameWithoutExtension(originalFilePath) + " - " + Repository.SealRootProductName + " Report Viewer";
            WindowState = FormWindowState.Normal;
            BringToFront();
            TopLevel = true;
            Focus();

            Report previousReport = _report;

            _report = report;
            _report.ExecutionContext = ReportExecutionContext.DesignerReport;
            if (string.IsNullOrEmpty(_report.DisplayName)) _report.DisplayName = Path.GetFileNameWithoutExtension(originalFilePath);
            _report.CurrentViewGUID = _report.ViewGUID;

            //execute one task
            _report.TaskToExecute = null;
            if (!string.IsNullOrEmpty(taskGUID))
            {
                _report.TaskToExecute = _report.Tasks.FirstOrDefault(i => i.GUID == taskGUID);
            }
            //execute to output
            _report.OutputToExecute = null;
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
                var parameter = _report.ExecutionView.Parameters.FirstOrDefault(i => i.Name == Seal.Model.Parameter.ForceExecutionParameter);
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
            webBrowser.Source = new Uri(_url);
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
            Icon = Properties.Resources.reportDesigner;
            if (Owner != null) Owner.Icon = Icon;
            string executionGUID = getAttributeValue(ReportExecution.HtmlId_execution_guid, "value").ToString();
            if (_navigation.Navigations.ContainsKey(executionGUID))
            {
                _execution = _navigation.Navigations[executionGUID].Execution;
                _report = _execution.Report;
            }
        }

        private async void setProgressBarInformation(string id, int progression, string message, string barClass)
        {
            await webBrowser.CoreWebView2.ExecuteScriptAsync($"setProgressBarMessage('#{id}',{progression},'{message}','progress-bar-{barClass}');");
        }

        private void initFromForm(string formValues)
        {
            _report.InputRestrictions.Clear();

            var values = HttpUtility.ParseQueryString(formValues);
            foreach (var key in values.Keys)
            {
                string id = key.ToString();
                string val = values[id];

                if (!string.IsNullOrEmpty(id))
                {
                    if (id.EndsWith("_Option_Value"))
                    {
                        foreach (string optionValue in val.Split(','))
                        {
                            _report.InputRestrictions.Add(optionValue, "true");
                        }
                    }
                    else
                    {
                        _report.InputRestrictions.Add(id, val);
                    }
                }
            }
        }

        private async Task<string> getAttributeValue(string id, string attribute)
        {
            var s = await webBrowser.CoreWebView2.ExecuteScriptAsync($"$('#{id}').attr('{attribute}')");
            return JsonDocument.Parse(s).RootElement.ToString();
        }

        private async void setProperty(string id, string property, string value)
        {
            value = value.Replace("\\", "\\\\");
            await webBrowser.CoreWebView2.ExecuteScriptAsync($"$('#{id}').{property}('{value}')");
        }

        private void WebBrowser_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (_browserInterop == null)
            {
                _browserInterop = new BrowserInterop() { Container = this };
                webBrowser.CoreWebView2.AddHostObjectToScript("dotnet", _browserInterop);
                if (_openDevTool) webBrowser.CoreWebView2.OpenDevToolsWindow();
            }

            var resultPath = "";
            if (e.Uri.EndsWith(ReportExecution.ActionViewHtmlResult))
            {
                setCurrentExecution();
                resultPath = _execution.GenerateHTMLResult();
            }
            else if (e.Uri.EndsWith(ReportExecution.ActionViewCSVResult))
            {
                setCurrentExecution();
                resultPath = _execution.GenerateCSVResult();
            }
            else if (e.Uri.EndsWith(ReportExecution.ActionViewPrintResult))
            {
                setCurrentExecution();
                resultPath = _execution.GeneratePrintResult();
            }
            else if (e.Uri.EndsWith(ReportExecution.ActionViewPDFResult))
            {
                setCurrentExecution();
                resultPath = _execution.GeneratePDFResult();
            }
            else if (e.Uri.EndsWith(ReportExecution.ActionViewExcelResult))
            {
                setCurrentExecution();
                resultPath = _execution.GenerateExcelResult();
            }
            if (File.Exists(resultPath))
            {
                e.Cancel = true;
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(resultPath) { UseShellExecute = true };

                Task.Run(() => p.Start());
            }
        }

        private void ReportViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Icon = Properties.Resources.reportDesigner;
            if (Owner != null) Owner.Icon = Icon;
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

            CoreWebView2Environment webView2Environment = CoreWebView2Environment.CreateAsync(null, FileHelper.TempApplicationDirectory).Result;
            webBrowser.EnsureCoreWebView2Async(webView2Environment);
        }

        private void WebBrowser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }

        public class BrowserInterop
        {
            public ReportViewerForm Container;
            bool _iconExecuting = true;

            Report Report {  get { return Container._report; } }
            ReportExecution Execution { get { return Container._execution; } }
            NavigationContext Navigation { get { return Container._navigation; } }

            public string GetTableData(string guid, string viewid, string pageid, string parameters)
            {
                string result = "";
                Report report = Navigation.Navigations.ContainsKey(guid) ? Navigation.Navigations[guid].Execution.Report : Report;
                var view = report.ExecutionView.GetView(viewid);
                if (view != null && view.ModelView != null)
                {
                    var page = view.ModelView.Model.Pages.FirstOrDefault(i => i.PageId == pageid);
                    if (page != null) result = page.DataTable.GetLoadTableData(view, parameters);
                }
                return result;
            }
            public string GetNavigationLinks()
            {
                string result = "";
                if (Execution.RootReport != null) result = Navigation.GetNavigationLinksHTML(Execution.RootReport);
                return result;
            }
            public string GetEnumValues(string enumId, string filter)
            {
                return Execution.GetEnumValues(enumId, filter);
            }

            public void UpdateEnumValues(string enumId, string values)
            {
                Execution.UpdateEnumValues(enumId, values);
            }

            public void UpdateViewParameter(string viewId, string parameterName, string parameterValue)
            {
                Report.UpdateViewParameter(viewId, parameterName, parameterValue);
            }

            public void ExecuteReport(string formValues)
            {
                _iconExecuting = true;
                Container.setCurrentExecution();
                Container.initFromForm(formValues);
                Report.ExecutionTriggerView = null;
                Report.IsNavigating = false;
                Container.Execute();
                Container._reportDone = false;
            }

            public void RefreshReport()
            {
                Container.Icon = (_iconExecuting ? Properties.Resources.reportDesigner2 : Properties.Resources.reportDesigner);
                _iconExecuting = !_iconExecuting;

                if (Container.Owner != null) Container.Owner.Icon = Container.Icon;

                if (Report.IsExecuting)
                {
                    if (Report.ExecutionView.GetValue("messages_mode") != "disabled")
                    {
                        Container.setProperty(ReportExecution.HtmlId_execution_messages, "html", Helper.ToHtml(Report.ExecutionMessages));
                        Container.webBrowser.CoreWebView2.ExecuteScriptAsync("scrollMessages(false);");
                    }
                    Container.setProgressBarInformation(ReportExecution.HtmlId_progress_bar, Report.ExecutionProgression, Report.ExecutionProgressionMessage, "success");
                    Container.setProgressBarInformation(ReportExecution.HtmlId_progress_bar_tasks, Report.ExecutionProgressionTasks, Report.ExecutionProgressionTasksMessage, "primary");
                    Container.setProgressBarInformation(ReportExecution.HtmlId_progress_bar_models, Report.ExecutionProgressionModels, Report.ExecutionProgressionModelsMessage, "info");
                }
                else if (!Container._reportDone)
                {
                    Navigation.SetNavigation(Execution);
                    Container._reportDone = true;
                    Report.IsNavigating = false;
                    Container._url = "file:///" + Report.HTMLDisplayFilePath;
                    Container.webBrowser.Source = new Uri(Container._url);
                }
            }

            public void CancelReport()
            {
                initIcon();

                Execution.Report.LogMessage(Report.Translate("Cancelling report..."));
                Report.Cancel = true;
            }

            public string Navigate(string nav, string parameter)
            {
                string result = "";
                initIcon();

                var parameters = HttpUtility.ParseQueryString(parameter);
                if (nav.StartsWith(NavigationLink.FileDownloadPrefix)) //File download
                {
                    var filePath = Navigation.NavigateScript(nav, Execution.Report, parameters);
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        var p = new Process();
                        p.StartInfo = new ProcessStartInfo(filePath) { UseShellExecute = true };
                        p.Start();
                    }
                    else
                    {
                        throw new Exception(string.Format("Invalid file path got from the navigation script: '{0}'", filePath));
                    }
                }
                else if (nav.StartsWith(NavigationLink.ReportScriptPrefix)) //Report Script
                {
                    result = Navigation.NavigateScript(nav, Execution.Report, parameters);
                }
                else //Drill or SubReport
                {
                    Container._execution = Navigation.Navigate(nav, Execution, false);
                    Container._report = Execution.Report;

                    Container._canRender = false;
                    Container._reportDone = false;
                    Container.Execute();
                }
                return result;
            }

            public void ExecuteFromTrigger(string formId, string formValues)
            {
                initIcon();
                _iconExecuting = true;
                Container.setCurrentExecution();
                lock (Execution)
                {
                    Container.initFromForm(formValues);

                    Report.ExecutionTriggerView = Report.AllViews.FirstOrDefault(i => formId.EndsWith(i.IdSuffix));
                    Report.IsNavigating = false;
                    Execution.Execute();
                    while (Report.IsExecuting && !Report.Cancel) Thread.Sleep(100);

                    Container._url = "file:///" + Report.HTMLDisplayFilePath;
                    Navigation.SetNavigation(Execution);

                    Container.webBrowser.Source = new Uri(Container._url);
                }
            }

            void initIcon()
            {
                Container.Icon = Properties.Resources.reportDesigner;
                if (Container.Owner != null) Container.Owner.Icon = Container.Icon;
            }
        }
    }
}
