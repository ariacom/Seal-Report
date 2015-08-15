//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
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

        public ReportViewerForm()
        {
            InitializeComponent();

            ShowIcon = true;
            Icon = Repository.ProductIcon;
        }

        public void ViewReport(Report report, Repository repository, bool render, string viewGUID, string outputGUID, string originalFilePath)
        {
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
                            cancelNavigation = true;
                            _reportDone = true;
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
                string action = HeaderForm.GetAttribute("action");
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
        }

        private void ReportViewerForm_Load(object sender, EventArgs e)
        {
            if (LastSize != null) Size = LastSize.Value;
            if (LastLocation != null) Location = LastLocation.Value;
        }
    }
}
