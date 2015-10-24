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

        static Size? LastSize = null;
        static Point? LastLocation = null;

        string _url;

        bool _reportDone = false;
        bool _exitOnClose = false;

        public bool CanRender
        {
            get
            {
                bool result = false;
                //check we have a report already executed...
                if (_report != null && _report.Status == ReportStatus.Executed && !_report.HasErrors)
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
                if (_report == null) return true;
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
                var parameter = _report.ExecutionView.Parameters.FirstOrDefault(i => i.Name == "force_execution");
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

        private bool processAction(string action)
        {
            bool cancelNavigation = false;
            try
            {
                switch (action)
                {
                    case ReportExecution.ActionExecuteReport:
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
                                        Debug.WriteLine("{0} {1} {2} {3}", element.Id, element.Name, element.GetAttribute("value"), element.GetAttribute("selected"));
                                    }
                                }
                            }
                        }
                        _report.IsDrilling = false;
                        Execute();
                        break;

                    case ReportExecution.ActionRefreshReport:
                        if (_report.IsExecuting)
                        {
                            cancelNavigation = true;
                            HtmlElement message = webBrowser.Document.All[ReportExecution.HtmlId_processing_message];
                            if (message != null) message.SetAttribute("innerHTML", _report.ExecutionHeader);
                            HtmlElement messages = webBrowser.Document.All[ReportExecution.HtmlId_execution_messages];
                            if (messages != null) messages.SetAttribute("innerHTML", Helper.ToHtml(_report.ExecutionMessages));
                        }
                        else if (!_reportDone)
                        {
                            //Set last drill path if any
                            if (_report.NavigationLinks.Count > 0) _report.NavigationLinks.Last().Href = _report.ResultFilePath;

                            cancelNavigation = true;
                            _reportDone = true;
                            _report.IsDrilling = false;
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
                        string resultPath = _execution.GenerateHTMLResult();
                        if (File.Exists(resultPath)) Process.Start(resultPath);
                        cancelNavigation = true;
                        break;

                    case ReportExecution.ActionViewPrintResult:
                        resultPath = _execution.GeneratePrintResult();
                        if (File.Exists(resultPath)) Process.Start(resultPath);
                        cancelNavigation = true;
                        break;

                    case ReportExecution.ActionViewPDFResult:
                        resultPath = _execution.GeneratePDFResult();
                        if (File.Exists(resultPath)) Process.Start(resultPath);
                        cancelNavigation = true;
                        break;

                    case ReportExecution.ActionViewExcelResult:
                        resultPath = _execution.GenerateExcelResult();
                        if (File.Exists(resultPath)) Process.Start(resultPath);
                        cancelNavigation = true;
                        break;

                    case ReportExecution.ActionDrillReport:
                        string nav = HeaderForm.GetAttribute(ReportExecution.HtmlId_navigation_attribute_name);
                        string src = HttpUtility.ParseQueryString(nav).Get("src");
                        string dst = HttpUtility.ParseQueryString(nav).Get("dst");
                        string val = HttpUtility.ParseQueryString(nav).Get("val");

                        string destLabel = "", srcRestriction = "";
                        bool drillDone = false;
                        foreach (var model in _report.Models)
                        {
                            ReportElement element = model.Elements.FirstOrDefault(i => i.MetaColumnGUID == src);
                            if (element != null)
                            {
                                drillDone = true;
                                element.ChangeColumnGUID(dst);
                                destLabel = element.DisplayNameElTranslated;
                                if (val != null)
                                {
                                    destLabel = "> " + destLabel;
                                    //Add restriction 
                                    ReportRestriction restriction = ReportRestriction.CreateReportRestriction();
                                    restriction.Source = model.Source;
                                    restriction.Model = model;
                                    restriction.MetaColumnGUID = src;
                                    restriction.SetDefaults();
                                    restriction.Operator = Operator.Equal;
                                    if (restriction.IsEnum) restriction.EnumValues.Add(val);
                                    else restriction.Value1 = val;
                                    model.Restrictions.Add(restriction);
                                    if (!string.IsNullOrEmpty(model.Restriction)) model.Restriction = string.Format("({0}) AND ", model.Restriction);
                                    model.Restriction += ReportRestriction.kStartRestrictionChar + restriction.GUID + ReportRestriction.kStopRestrictionChar;

                                    srcRestriction = restriction.DisplayText;
                                }
                                else
                                {
                                    destLabel = "< " + destLabel;
                                    var restrictions = model.Restrictions.Where(i => i.MetaColumnGUID == dst).ToList();
                                    foreach (var restr in restrictions)
                                    {
                                        model.Restrictions.Remove(restr);
                                        model.Restriction = model.Restriction.Replace(ReportRestriction.kStartRestrictionChar + restr.GUID + ReportRestriction.kStopRestrictionChar, "1=1");
                                    }
                                }
                            }
                        }

                        if (drillDone)
                        {
                            NavigationLink lastLink = null;
                            if (_report.NavigationLinks.Count == 0)
                            {
                                lastLink = new NavigationLink();
                                _report.NavigationLinks.Add(lastLink);
                            }
                            else lastLink = _report.NavigationLinks.Last();

                            //create HTML result for navigation -> NavigationLinks must have one link to activate the button
                            _report.IsDrilling = true;
                            string htmlPath = _execution.GenerateHTMLResult();
                            lastLink.Href = htmlPath;
                            if (string.IsNullOrEmpty(lastLink.Text)) lastLink.Text = _report.ExecutionName;

                            string linkText = string.Format("{0} {1}", _report.ExecutionName, destLabel);
                            if (!string.IsNullOrEmpty(srcRestriction)) linkText += string.Format(" [{0}]", srcRestriction);
                            _report.NavigationLinks.Add(new NavigationLink() { Href = "#", Text = linkText });
                        }

                        cancelNavigation = true;
                        _reportDone = false;
                        Execute();
                        break;

                    case ReportExecution.ActionGetNavigationLinks:
                        cancelNavigation = true;
                        HtmlElement navMenu = webBrowser.Document.All[ReportExecution.HtmlId_navigation_menu];
                        if (navMenu != null)
                        {
                            string links = "";
                            foreach (var link in _report.NavigationLinks)
                            {
                                links += string.Format("<li><a href='{0}'>{1}</a></li>", link.Href, HttpUtility.HtmlEncode(link.Text));
                            }
                            navMenu.SetAttribute("innerHTML", links);
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
        }
    }
}
