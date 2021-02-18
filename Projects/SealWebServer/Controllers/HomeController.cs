//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Seal.Helpers;
using Seal.Model;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.Specialized;

namespace SealWebServer.Controllers
{
    /// <summary>
    /// Seal Web Server Controllers Objects
    /// </summary>
    internal class NamespaceDoc
    {
    }

    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public partial class HomeController : Controller
    {
        public const string SessionRepository = "SessionRepository";
        public const string SessionLastFolder = "SessionLastFolder";
        public const string SessionUser = "SessionUser";
        public const string SessionNavigationContext = "SessionNavigationContext";
        public const string SessionUploadedFiles = "SessionUploadedFiles";

        const string _loginContent = "<html><script>window.top.location.href='Main';</script></html>";
        string _noReportFoundMessage
        {
            get
            {
                return Translate("Sorry, this report is not in your session anymore...");
            }
        }

        Repository Repository
        {
            get
            {
                return getSessionValue(SessionRepository) as Repository;
            }
        }

        /// <summary>
        /// Translate text using the current Repository Locale
        /// </summary>
        public string Translate(string reference)
        {
            if (Repository == null) return reference;
            return Repository.TranslateWeb(reference);
        }

        Repository CreateRepository()
        {
            Repository repository = Repository.Create();
            setSessionValue(SessionRepository, repository);
            return repository;
        }

        SecurityUser WebUser
        {
            get
            {
                return getSessionValue(SessionUser) as SecurityUser;
            }
        }

        SecurityUser CreateWebUser()
        {
            if (Repository == null)
            {
                CreateRepository();
            }
            var user = new SecurityUser(Repository.Security);

            setSessionValue(SessionUser, user);
            //Clear previous Session variables
            setSessionValue(SessionNavigationContext, null);
#if NETCOREAPP
            user.SessionID = SessionKey;
#else
            user.SessionID = Session.SessionID;
#endif            
            return user;
        }

        NavigationContext NavigationContext
        {
            get
            {
                NavigationContext result = getSessionValue(SessionNavigationContext) as NavigationContext;
                if (result == null)
                {
                    result = new NavigationContext();
                    setSessionValue(SessionNavigationContext, result);

                }
                return result;
            }
        }

        ContentResult HandleException(Exception ex)
        {
            var detail = getContextDetail(Request, WebUser);
            Audit.LogAudit(AuditType.EventError, WebUser, null, detail, ex.Message);
            WebHelper.WriteWebException(ex, detail);
            var message = "<p style='font-family:Helvetica,Arial,sans-serif;padding-top:60px'>";
#if DEBUG
            message += string.Format("<b>Sorry, we got an unexpected exception.</b><br>{0}<br>{1}<br>{2}", ex.Message.Replace(Repository.RepositoryPath, ""), RequestUrl, ex.StackTrace);
#else
            message += "<b>Sorry, we got an unexpected exception.</b><br>Please consult log files on the server machine to have more information (Logs Repository folder and Windows Event Logs on Windows machine)...";
#endif
            message += "</p>";
            var content = Content(message);
            content.ContentType = "text/html";
            return content;
        }

        ContentResult _loginContentResult
        {
            get
            {
                var content = Content(_loginContent);
                content.ContentType = "text/html";
                return content;
            }
        }

        bool CheckAuthentication()
        {
            if (WebUser == null) CreateWebUser();

            if (!WebUser.IsAuthenticated)
            {
                Authenticate();
                if (WebUser.IsAuthenticated)
                {
                    //Load profile
                    if (System.IO.File.Exists(WebUser.ProfilePath)) WebUser.Profile = SecurityUserProfile.LoadFromFile(WebUser.ProfilePath);
                    WebUser.Profile.Path = WebUser.ProfilePath;
                }
            }
            return WebUser.IsAuthenticated;
        }

#if !EDITOR
        public string Info = ""; //Info to display in the Web Report Server

        void Authenticate()
        {
            WebUser.WebPrincipal = User;
            WebUser.Authenticate();
            WebHelper.WriteLogEntryWeb(WebUser.IsAuthenticated ? EventLogEntryType.SuccessAudit : EventLogEntryType.FailureAudit, WebUser.AuthenticationSummary);
        }
#endif

        ContentResult GetContentResult(string filePath)
        {
            Response.Clear();
#if NETCOREAPP
            Response.Headers.Clear();
#else
            Response.ClearContent();
#endif
            Response.Headers.Add("title", Path.GetFileName(filePath));
            Response.Headers.Add("content-disposition", "inline;filename=\"" + Path.GetFileName(filePath) + "\"");
            Response.ContentType = "text/html";
            Response.Cache.SetLastModified(DateTime.Now);//!NETCore
            return Content(System.IO.File.ReadAllText(filePath));
        }

        /// <summary>
        /// Main entry of the Controller
        /// </summary>
        public ActionResult Main()
        {
            ActionResult result;
            try
            {
                if (Repository == null) CreateRepository();
                var model = new MainModel() { Repository = Repository };
#if NETCOREAPP
                model.ServerPath = WebRootPath;
                model.BaseURL = Request.PathBase.Value;
#else
                model.ServerPath = Request.PhysicalApplicationPath;
                model.BaseURL = Request.ApplicationPath;
#endif

                result = View(model);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return result;
        }

        /// <summary>
        /// Execute a report initiated in a previous execution
        /// </summary>
        public ActionResult ActionExecuteReport(string execution_guid)
        {
            writeDebug("ActionExecuteReport");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;
                if (string.IsNullOrEmpty(execution_guid)) return new EmptyResult();

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    Report report = execution.Report;
                    WebHelper.WriteLogEntryWebDetail(EventLogEntryType.Information, string.Format("Starting report '{0}'", report.FilePath), getContextDetail(Request, WebUser));
                    report.IsNavigating = false;
                    report.ExecutionTriggerView = null;
                    initInputRestrictions(report);
                    while (execution.IsConvertingToExcel) Thread.Sleep(100);
                    execution.Execute();
                    return new EmptyResult();
                }
                else throw new Exception(string.Format("No report execution found in session '{0}'", execution_guid));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        /// <summary>
        /// Navigate to a new report execution: Either for Drill or SubReport
        /// </summary>
        public ActionResult ActionNavigate(string execution_guid, string navigation_target)
        {
            writeDebug("ActionNavigate");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;
                if (string.IsNullOrEmpty(execution_guid)) return new EmptyResult();

                ReportExecution execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    if (execution.RootReport == null) execution.RootReport = execution.Report;

                    string nav = Request.Form[ReportExecution.HtmlId_navigation_id];
                    if (string.IsNullOrEmpty(nav)) nav = NavigationLink.ReportScriptPrefix;
                    NameValueCollection parameters = null;
                    if (Request.Form[ReportExecution.HtmlId_navigation_parameters] != null) parameters = HttpUtility.ParseQueryString(Request.Form[ReportExecution.HtmlId_navigation_parameters]);

                    if (nav.StartsWith(NavigationLink.ReportScriptPrefix)) //Report Script
                    {
                        var data = NavigationContext.NavigateScript(nav, execution.Report, parameters, Request);
                        return Json(data);
                    }
                    else if (nav.StartsWith(NavigationLink.FileDownloadPrefix)) //File download
                    {
                        var filePath = NavigationContext.NavigateScript(nav, execution.Report, parameters, Request);
                        if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                        {
                            return getFileResult(filePath, null);
                        }
                        else
                        {
                            throw new Exception(string.Format("Invalid file path got from the navigation script: '{0}'", filePath));
                        }
                    }
                    else if (nav.StartsWith(NavigationLink.ReportExecutionPrefix)) //Report execution
                    {
                        Report report = execution.Report;
                        string path = report.Repository.ReportsFolder + nav.Substring(3);
                        var newReport = Report.LoadFromFile(path, report.Repository);
                        newReport.WebUrl = report.WebUrl;
                        execution = new ReportExecution() { Report = newReport };
                        report = newReport;
                        setSessionValue(report.ExecutionGUID, execution);

                        WebHelper.WriteLogEntryWebDetail(EventLogEntryType.Information, string.Format("Execute report '{0}'", report.FilePath), getContextDetail(Request, WebUser));

                        report.ExecutionContext = ReportExecutionContext.WebReport;
                        report.SecurityContext = WebUser;
                        report.CurrentViewGUID = report.ViewGUID;

                        report.InitForExecution();
                        execution.RenderHTMLDisplayForViewer();
                        return getFileResult(report.HTMLDisplayFilePath, report);
                    }
                    else
                    {
                        execution = NavigationContext.Navigate(nav, execution, !string.IsNullOrEmpty(navigation_target));
                        Report report = execution.Report;

                        if (string.IsNullOrEmpty(navigation_target))
                        {
                            //Same window, keep the context
                            report.OnlyBody = execution.RootReport.OnlyBody;
                        }
                        else
                        {
                            //Navigation to a new window, simple new execution
                            report.OnlyBody = false;
                            execution.RootReport = null;
                            report.IsNavigating = false;
                        }

                        //Check rights if not in subreports or personal folders
                        if (!string.IsNullOrEmpty(Path.GetDirectoryName(report.FilePath)) && !report.FilePath.StartsWith(report.Repository.SubReportsFolder) && !report.FilePath.StartsWith(Repository.PersonalFolder))
                        {
                            var path = report.FilePath.Replace(report.Repository.ReportsFolder, "");
                            SWIFolder folder = getParentFolder(path);
                            if (folder.right == 0) throw new Exception(string.Format("Error: no right to execute a report on the folder '{0}'", folder.path));
                        }

                        setSessionValue(report.ExecutionGUID, execution);

                        WebHelper.WriteLogEntryWebDetail(EventLogEntryType.Information, string.Format("Navigation report '{0}'", report.FilePath), getContextDetail(Request, WebUser));

                        report.ExecutionContext = ReportExecutionContext.WebReport;
                        report.SecurityContext = WebUser;

                        report.InitForExecution();
                        execution.RenderHTMLDisplayForViewer();
                        return getFileResult(report.HTMLDisplayFilePath, report);
                    }
                }
                else
                {
                    throw new Exception(string.Format("No report execution found in session '{0}'", execution_guid));
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        /// <summary>
        /// Return the current Navigation links for a report execution
        /// </summary>
        public ActionResult ActionGetNavigationLinks(string execution_guid)
        {
            writeDebug("ActionGetNavigationLinks");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null && execution.RootReport != null)
                {
                    Report report = execution.Report;
                    return Json(new { links = NavigationContext.GetNavigationLinksHTML(execution.RootReport) });
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return new EmptyResult();
        }

        /// <summary>
        /// Refresh the report for a report execution
        /// </summary>
        public ActionResult ActionRefreshReport(string execution_guid)
        {
            writeDebug("ActionRefreshReport");
            string error = "";
            try
            {
                var execution = getReportExecution(execution_guid);
                if (!CheckAuthentication())
                {
                    error = Helper.ToHtml(_noReportFoundMessage);
                }
                else if (execution != null)
                {
                    Report report = execution.Report;
                    Debug.WriteLine(string.Format("Report Status {0}", report.Status));
                    if (report.IsExecuting)
                    {
                        return Json(new
                        {
                            progression = report.ExecutionProgression,
                            progression_message = Helper.ToHtml(report.ExecutionProgressionMessage),
                            progression_models = report.ExecutionProgressionModels,
                            progression_models_message = Helper.ToHtml(report.ExecutionProgressionModelsMessage),
                            progression_tasks = report.ExecutionProgressionTasks,
                            progression_tasks_message = Helper.ToHtml(report.ExecutionProgressionTasksMessage),
                            execution_messages = report.ExecutionView.GetValue("messages_mode") != "disabled" ? Helper.ToHtml(report.ExecutionMessages) : null
                        });
                    }
                    else if (execution.IsConvertingToExcel)
                    {
                        return Json(new
                        {
                            progression = report.ExecutionProgression,
                            progression_message = Helper.ToHtml(report.Translate("Executing report...")),
                            execution_messages = Helper.ToHtml(report.ExecutionMessages)
                        });
                    }
                    else if (report.Status == ReportStatus.Executed)
                    {
                        return Json(new { result_ready = true }); ;
                    }
                    else if (!string.IsNullOrEmpty(report.ExecutionErrors))
                    {
                        throw new Exception(report.ExecutionErrors);
                    }
                }
                else
                {
                    error = Helper.ToHtml(_noReportFoundMessage);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
                error = ex.Message;
            }
            return Json(new { error });
        }

        /// <summary>
        /// Return the Result of a report execution
        /// </summary>
        public ActionResult Result(string execution_guid)
        {
            writeDebug("Result");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;
                if (string.IsNullOrEmpty(execution_guid)) return new EmptyResult();

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    Report report = execution.Report;
                    NavigationContext.SetNavigation(execution);

                    WebHelper.WriteLogEntryWebDetail(EventLogEntryType.Information, string.Format("Viewing result of report '{0}'", report.FilePath), getContextDetail(Request, WebUser));
                    if (report.HasErrors) WebHelper.WriteLogEntryWebDetail(EventLogEntryType.Error, string.Format("Report '{0}' execution errors:\r\n{1}", report.FilePath, report.ExecutionErrors), getContextDetail(Request, WebUser));
                    string filePath = report.ForOutput || report.HasExternalViewer ? report.HTMLDisplayFilePath : report.ResultFilePath;
                    if (!System.IO.File.Exists(filePath)) throw new Exception("Error: Result file path does not exists...");
                    return getFileResult(filePath, report);
                }
                else throw new Exception(string.Format("No report execution found in session '{0}'", execution_guid));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }


        /// <summary>
        /// Return the result output of a report execution
        /// </summary>
        public ActionResult OutputResult(string execution_guid)
        {
            writeDebug("OutputResult");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    Report report = execution.Report;
                    return getFileResult(report.ResultFilePath, report);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }


        /// <summary>
        /// Cancel a report execution
        /// </summary>
        public ActionResult ActionCancelReport(string execution_guid)
        {
            writeDebug("ActionCancelReport");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    Report report = execution.Report;
                    execution.Report.LogMessage(report.Translate("Cancelling report..."));
                    report.Cancel = true;
                    return new EmptyResult();
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return new EmptyResult();
        }

        /// <summary>
        /// Update the value of a view parameter of a report execution
        /// </summary>
        public ActionResult ActionUpdateViewParameter(string execution_guid, string parameter_view_id, string parameter_view_name, string parameter_view_value)
        {
            writeDebug("ActionUpdateViewParameter");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    //Do not allow to change sensible parameters
                    if (parameter_view_name == Parameter.EnableResultsMenuParameter) throw new Exception("Incorrect action");

                    Report report = execution.Report;
                    report.UpdateViewParameter(parameter_view_id, parameter_view_name, parameter_view_value);
                    return new EmptyResult();
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }

        /// <summary>
        /// Return the Html result of a report execution
        /// </summary>
        public ActionResult HtmlResult(string execution_guid)
        {
            writeDebug("HtmlResult");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {

                    if (!execution.Report.ExecutionView.GetBoolValue(Parameter.EnableResultsMenuParameter)) throw new Exception("Invalid operation");

                    string resultPath = execution.GenerateHTMLResult();
                    return getFileResult(resultPath, execution.Report);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }

        /// <summary>
        /// Return the Html result of a report execution as a file
        /// </summary>
        public ActionResult HtmlResultFile(string execution_guid)
        {
            writeDebug("HtmlResultFile {0}");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    if (!execution.Report.ExecutionView.GetBoolValue(Parameter.EnableResultsMenuParameter)) throw new Exception("Invalid operation");

                    return getFileResult(execution.Report.ResultFilePath, execution.Report);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }


        /// <summary>
        /// Return the Print HTML result of a report execution
        /// </summary>
        public ActionResult PrintResult(string execution_guid)
        {
            writeDebug("PrintResult");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    if (!execution.Report.ExecutionView.GetBoolValue(Parameter.EnableResultsMenuParameter)) throw new Exception("Invalid operation");

                    string resultPath = execution.GeneratePrintResult();
                    return getFileResult(resultPath, execution.Report);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }

        /// <summary>
        /// Return the PDF result of a report execution
        /// </summary>
        public ActionResult PDFResult(string execution_guid)
        {
            writeDebug("PDFResult");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    if (!execution.Report.ExecutionView.GetBoolValue(Parameter.EnableResultsMenuParameter)) throw new Exception("Invalid operation");
                    if (execution.IsConvertingToPDF) return Content(Translate("Sorry, the conversion is being in progress in another window..."));

                    string resultPath = "";
                    try
                    {
                        execution.IsConvertingToPDF = true;
                        resultPath = execution.GeneratePDFResult();
                    }
                    finally
                    {
                        execution.IsConvertingToPDF = false;
                    }
                    return getFileResult(resultPath, execution.Report);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }

        /// <summary>
        /// Return the Excel result of a report execution
        /// </summary>
        public ActionResult ExcelResult(string execution_guid)
        {
            writeDebug("ExcelResult");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    if (!execution.Report.ExecutionView.GetBoolValue(Parameter.EnableResultsMenuParameter)) throw new Exception("Invalid operation");
                    if (execution.IsConvertingToExcel) return Content(Translate("Sorry, the conversion is being in progress in another window..."));

                    string resultPath = "";
                    try
                    {
                        execution.IsConvertingToExcel = true;
                        resultPath = execution.GenerateExcelResult();
                    }
                    finally
                    {
                        execution.IsConvertingToExcel = false;
                    }
                    return getFileResult(resultPath, execution.Report);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }

        /// <summary>
        /// Return the CSV result of a report execution
        /// </summary>
        public ActionResult CSVResult(string execution_guid)
        {
            writeDebug("CSVResult");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    if (!execution.Report.ExecutionView.GetBoolValue(Parameter.EnableResultsMenuParameter)) throw new Exception("Invalid operation");
                    string resultPath = "";
                    resultPath = execution.GenerateCSVResult();
                    return getFileResult(resultPath, execution.Report);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }

        /// <summary>
        /// Return the table data of a Page (for DataTables server Pagination)
        /// </summary>
        public ActionResult ActionGetTableData(string execution_guid, string viewid, string pageid, string parameters)
        {
            writeDebug("ActionGetTableData");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                ReportExecution execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    var view = execution.Report.ExecutionView.GetView(viewid);
                    if (view != null && view.ModelView != null)
                    {
                        var page = view.ModelView.Model.Pages.FirstOrDefault(i => i.PageId == pageid);
                        if (page == null && view.ModelView.Model.Pages.Count > 0) page = view.ModelView.Model.Pages.First();
                        if (page != null)
                        {
                            return Json(page.DataTable.GetLoadTableData(view, parameters), JsonRequestBehavior.AllowGet);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }

        /// <summary>
        /// Update values chosen for an Enum in a report execution
        /// </summary>
        public ActionResult ActionUpdateEnumValues(string execution_guid, string id, string values)
        {
            writeDebug("ActionUpdateEnumValues");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    execution.UpdateEnumValues(id, values);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
            return Json(new { });
        }

        /// <summary>
        /// Return the list of values for a Enumerated list with a filter for a report execution
        /// </summary>
        public ActionResult ActionGetEnumValues(string execution_guid, string enum_id, string filter)
        {
            writeDebug("ActionGetEnumValues");
            string result = "";
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    result = execution.GetEnumValues(enum_id, filter);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
            return Json(result);
        }


        void parseViews(ReportView view, List<string> views)
        {
            views.Add(view.Parse());
            foreach (var child in view.Views)
            {
                parseViews(child, views);
            }
        }

        /// <summary>
        /// Execute a report and parse the views involved when triggered from a Restrictions View
        /// </summary>
        public ActionResult ActionExecuteFromTrigger(string execution_guid, string form_id)
        {
            writeDebug("ActionExecuteFromTrigger");
            var views = new List<string>();
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                ReportExecution execution = getReportExecution(execution_guid);
                if (execution == null) throw new Exception(string.Format("Unable to find execution id {0}", execution_guid));

                var report = execution.Report;
                report.ExecutionTriggerView = execution.Report.AllViews.FirstOrDefault(i => form_id.EndsWith(i.IdSuffix));

                if (!string.IsNullOrEmpty(form_id) && execution != null)
                {
                    lock (execution)
                    {
                        report.IsNavigating = false;
                        initInputRestrictions(report);

                        //Get all restrictions involved
                        bool hasInputValue = false;
                        foreach (ReportRestriction restriction in report.ExecutionInputValues.Where(i => i.Prompt != PromptType.None || i.AllowAPI))
                        {
                            if (!string.IsNullOrEmpty(report.GetInputRestriction(restriction.OperatorHtmlId)))
                            {
                                hasInputValue = true;
                                break;
                            }
                        }

                        var restrictions = new List<string>();
                        foreach (ReportModel model in report.ExecutionModels)
                        {
                            foreach (ReportRestriction restriction in
                                model.ExecutionRestrictions.Where(i => i.Prompt != PromptType.None || i.AllowAPI)
                                .Union(model.ExecutionAggregateRestrictions.Where(i => i.Prompt != PromptType.None || i.AllowAPI))
                                .Union(model.ExecutionCommonRestrictions.Where(i => i.Prompt != PromptType.None || i.AllowAPI))
                                )
                            {
                                if (!string.IsNullOrEmpty(report.GetInputRestriction(restriction.OperatorHtmlId)))
                                {
                                    restrictions.Add(restriction.GUID);
                                }
                            }
                        }

                        //Execute the report
                        report.IsNavigating = false;
                        execution.Execute();
                        while (report.IsExecuting && !report.Cancel) Thread.Sleep(100);

                        foreach (var view in execution.Report.AllViews.Where(i => i.Model != null || i.RestrictionsGUID.Count > 0))
                        {
                            bool parseView = hasInputValue; //Parse all if input value involved
                            if (!parseView) parseView = view.GetBoolValue(Parameter.ForceRefreshParameter);

                            if (!parseView && view.Model != null) //Parse if one restriction in the model
                            {
                                parseView = view.Model.AllExecutionRestrictions.Exists(i => restrictions.Contains(i.GUID));
                            }

                            if (!parseView && view.RestrictionsGUID != null) //Parse restriction views having the restriction                            
                            {
                                parseView = view.RestrictionsGUID.Exists(i => restrictions.Contains(i));
                            }

                            if (parseView)
                            {
                                try
                                {
                                    report.Status = ReportStatus.RenderingDisplay;
                                    report.CurrentModelView = view;
                                    views.Add(view.Parse());
                                }
                                finally
                                {
                                    report.Status = ReportStatus.Executed;
                                }
                            }
                        }

                        //parse information and messages...
                        try
                        {
                            var key = report.ExecutionView.GetPartialTemplateKey("Report.iInformation", report.ExecutionView);
                            views.Add(RazorHelper.CompileExecute(report.ExecutionView.Template.GetPartialTemplateText("Report.iInformation"), report.ExecutionView, key));
                            key = report.ExecutionView.GetPartialTemplateKey("Report.iMessages", report.ExecutionView);
                            views.Add(RazorHelper.CompileExecute(report.ExecutionView.Template.GetPartialTemplateText("Report.iMessages"), report.ExecutionView, key));
                        }
                        finally
                        {
                            report.Status = ReportStatus.Executed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
            return Json(views);
        }


        /// <summary>
        /// Execute a report in a new window when triggered from a Restrictions View
        /// </summary>
        public ActionResult ActionExecuteFromTriggerNewWindow(string execution_guid_trigger, string form_id)
        {
            writeDebug("ActionExecuteFromTriggerNewWindow");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                ReportExecution execution = getReportExecution(execution_guid_trigger);
                if (execution == null) throw new Exception(string.Format("Unable to find execution id {0}", execution_guid_trigger));

                var report = execution.Report;
                report.ExecutionTriggerView = execution.Report.AllViews.FirstOrDefault(i => form_id.EndsWith(i.IdSuffix));
                //Trigger in another window
                var rootReport = execution.RootReport;
                var triggerViewGUID = report.ExecutionTriggerView.GUID;
                report.IsNavigating = false;
                report.PreInputRestrictions.Clear();
                //Reapply restrictions
                initInputRestrictions(report);
                //Apply input restrictions if any
                if (report.InputRestrictions.Count > 0) execution.CheckInputRestrictions();

                //Reset context for navigation, remove previous, keep root
                var keys = NavigationContext.Navigations.Where(i => i.Value.Execution.RootReport.ExecutionGUID == report.ExecutionGUID).ToArray();
                foreach (var key in keys) NavigationContext.Navigations.Remove(key.Key);

                //Clone the report for a new execution
                report = report.Clone(); //New executionGUID
                report.ExecutionTriggerView = execution.Report.AllViews.FirstOrDefault(i => i.GUID == triggerViewGUID);
                //Set execution view
                if (report.ExecutionTriggerView != null) report.CurrentViewGUID = string.IsNullOrEmpty(report.ExecutionTriggerView.GetValue(Parameter.RestrictionsExecView)) ? report.ViewGUID : report.ExecutionTriggerView.GetValue(Parameter.RestrictionsExecView);

                execution = initReportExecution(report, report.CurrentViewGUID, "", false);
                report.IsNavigating = false;
                execution.Execute();
                while (report.IsExecuting && !report.Cancel) Thread.Sleep(100);

                //Set navigation context
                NavigationContext.SetNavigation(execution);

                return getFileResult(report.HTMLDisplayFilePath, report);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }


        #region private methods

        void checkSWIAuthentication()
        {
            if (WebUser == null || !WebUser.IsAuthenticated) throw new SessionLostException("Error: user is not authenticated");
        }

        JsonResult HandleSWIException(Exception ex)
        {
            if (!(ex is ValidationException) && !(ex is SessionLostException))
            {
                var detail = getContextDetail(Request, WebUser);
                Audit.LogAudit(ex is LoginException ? AuditType.LoginFailure : AuditType.EventError, WebUser, null, detail, ex.Message);
                WebHelper.WriteWebException(ex, detail);
            }
            return Json(new { error = ex.Message.Replace(Repository.RepositoryPath, ""), authenticated = (WebUser != null && WebUser.IsAuthenticated) }, JsonRequestBehavior.AllowGet);
        }

        string getFullPath(string path)
        {
            path = FileHelper.ConvertOSFilePath(path);
            if (path.StartsWith(SWIFolder.GetPersonalRoot())) return Repository.GetPersonalFolder(WebUser) + path.Substring(1);
            else return Repository.ReportsFolder + path;
        }

        SWIFolder getParentFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");
            if (path.Contains("..\\") || path.Contains("../")) throw new Exception("Error: invalid path");
            path = FileHelper.ConvertOSFilePath(path);
            return getFolder(SWIFolder.GetParentPath(path));
        }
        SWIFolder getFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");
            checkSWIAuthentication();
            if (path.Contains("..\\") || path.Contains("../")) throw new Exception("Error: invalid path");
            path = FileHelper.ConvertOSFilePath(path);

            SWIFolder result = WebUser.AllFolders.FirstOrDefault(i => i.path == path);
            if (result != null)
            {
                //Folder was already initialized
                result.SetFullPath(getFullPath(path));
                return result;
            }

            result = new SWIFolder();
            result.path = path;
            result.right = 0;
            result.sql = WebUser.SqlModel;
            result.SetFullPath(getFullPath(path));

            if (result.IsPersonal)
            {
                //Personal
                if (WebUser.PersonalFolderRight == PersonalFolderRight.None) throw new Exception("Error: this user has no personal folder");
                result.SetManageFlag(true, true, result.FinalPath == "");
                result.expand = false;
                string prefix = Repository.GetPersonalFolderName(WebUser);
                result.name = (result.FinalPath == "" ? prefix : Path.GetFileName(result.FinalPath));
                result.fullname = prefix + (result.FinalPath == "" ? Path.DirectorySeparatorChar.ToString() : "") + result.FinalPath;
                result.right = (int)FolderRight.Edit;
                result.files = (WebUser.PersonalFolderRight == PersonalFolderRight.Files);
            }
            else
            {
                result.name = (result.FinalPath == Path.DirectorySeparatorChar.ToString() ? Translate("Reports") : Repository.TranslateFolderName(path));
                result.fullname = Translate("Reports") + Repository.TranslateFolderPath(result.FinalPath);
                SecurityFolder securityFolder = WebUser.FindSecurityFolder(path);
                if (securityFolder != null)
                {
                    result.SetManageFlag(securityFolder.UseSubFolders, securityFolder.ManageFolder, securityFolder.IsDefined);
                    result.expand = securityFolder.ExpandSubFolders;
                    result.right = (int)securityFolder.FolderRight;
                    result.files = securityFolder.FilesOnly;
                }
            }
            return result;
        }

        SWIFolderDetail getFolderDetail(string path, bool refresh = false)
        {
            path = FileHelper.ConvertOSFilePath(path);
            if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");

            SWIFolderDetail folderDetail = null;
            if (refresh)
            {
                WebUser.FolderDetails.RemoveAll(i => i.folder.path == path);
            }
            else
            {
                folderDetail = WebUser.FolderDetails.FirstOrDefault(i => i.folder.path == path);
                if (folderDetail != null)
                {
                    return folderDetail;
                }
            }

            SWIFolder folder = getFolder(path);
            var files = new List<SWIFile>();
            if (folder.right > 0)
            {
                foreach (string newPath in Directory.GetFiles(folder.GetFullPath(), "*.*"))
                {
                    //check right on files only
                    if (folder.files && FileHelper.IsSealReportFile(newPath)) continue;
                    if (folder.IsPersonal && newPath.ToLower() == WebUser.ProfilePath.ToLower()) continue;

                    files.Add(new SWIFile()
                    {
                        path = FileHelper.ConvertOSFilePath(folder.Combine(Path.GetFileName(newPath))),
                        name = Repository.TranslateFileName(newPath) + (FileHelper.IsSealReportFile(newPath) ? "" : Path.GetExtension(newPath)),
                        last = System.IO.File.GetLastWriteTime(newPath).ToString("G", Repository.CultureInfo),
                        isreport = FileHelper.IsSealReportFile(newPath),
                        right = folder.right
                    });
                }
            }

            //Folder Detail script
            WebUser.FolderDetail = new SWIFolderDetail() { folder = folder, files = files };
            WebUser.ScriptNumber = 1;
            foreach (var group in WebUser.SecurityGroups.Where(i => !string.IsNullOrEmpty(i.FolderDetailScript)).OrderBy(i => i.Name))
            {
                RazorHelper.CompileExecute(group.FolderDetailScript, WebUser);
                WebUser.ScriptNumber++;
            }
            folderDetail = WebUser.FolderDetail;
            WebUser.FolderDetails.Add(folderDetail);

            return folderDetail;
        }

        SWIFile getFileDetail(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");
            path = FileHelper.ConvertOSFilePath(path);
            var folderDetail = getFolderDetail(SWIFolder.GetParentPath(path));
            var fileDetail = folderDetail.files.FirstOrDefault(i => i.path == path);
            if (fileDetail == null)
            {
                folderDetail = getFolderDetail(SWIFolder.GetParentPath(path), true);
                fileDetail = folderDetail.files.FirstOrDefault(i => i.path == path);
            }
            if (fileDetail == null)
            {
                throw new Exception("Error: file not found");
            }
            return fileDetail;
        }

        void fillFolder(SWIFolder folder)
        {
            List<SWIFolder> subFolders = new List<SWIFolder>();
            if (folder.IsPersonal && WebUser.PersonalFolderRight == PersonalFolderRight.None) return;

            string folderPath = folder.GetFullPath();
            foreach (string subFolder in Directory.GetDirectories(folderPath))
            {
                SWIFolder sub = getFolder(folder.Combine(subFolder));
                //Add if right on this folder, or a sub folder is defined with this root
                if ((sub.right > 0) || WebUser.SecurityGroups.Exists(i => i.Folders.Exists(j => j.Path.StartsWith(sub.path + (sub.path == Path.DirectorySeparatorChar.ToString() ? "" : Path.DirectorySeparatorChar.ToString())) && j.FolderRight != FolderRight.None)))
                {
                    fillFolder(sub);
                    subFolders.Add(sub);
                }
            }
            folder.folders = subFolders;
        }

        void searchFolder(SWIFolder folder, string pattern, List<SWIFile> files)
        {
            var folderDetail = getFolderDetail(folder.path, true);

            foreach (var file in (folderDetail.files.Where(i => i.name.ToLower().Contains(pattern.ToLower()))))
            {
                files.Add(new SWIFile()
                {
                    path = file.path,
                    name = folder.fullname + (folder.fullname.EndsWith(Path.DirectorySeparatorChar.ToString()) ? "" : Path.DirectorySeparatorChar.ToString()) + file.name,
                    last = file.last,
                    isreport = file.isreport,
                    right = file.right
                });

            }

            foreach (string subFolder in Directory.GetDirectories(folder.GetFullPath()))
            {
                SWIFolder sub = getFolder(folder.Combine(subFolder));
                if (sub.right > 0) searchFolder(sub, pattern, files);
            }
        }

        void initInputRestrictions(Report report)
        {
            report.InputRestrictions.Clear();

            //Do not use input restrictions for navigation...
            if (report.IsNavigating) return;

            if (report.PreInputRestrictions.Count > 0)
            {
                int i = 0;
                while (true)
                {
                    string prefix = string.Format("r{0}", i);
                    string key = prefix + "_name";
                    if (report.PreInputRestrictions.ContainsKey(key))
                    {
                        var displayName = report.PreInputRestrictions[key].ToLower();
                        foreach (ReportRestriction restriction in report.ExecutionCommonRestrictions.Where(j => j.DisplayNameEl.ToLower() == displayName))
                        {
                            //Convert values to normal input using the html id...
                            key = prefix + "_operator";
                            if (report.PreInputRestrictions.ContainsKey(key))
                            {
                                //operator
                                report.InputRestrictions.Add(restriction.OperatorHtmlId, report.PreInputRestrictions[key]);
                                if (restriction.IsEnumRE)
                                {
                                    restriction.SetEnumHtmlIds();

                                    //options
                                    key = prefix + "_enum_values";
                                    if (report.PreInputRestrictions.ContainsKey(key))
                                    {
                                        var optionValues = report.PreInputRestrictions[key];
                                        //Convert values into index of the enum...
                                        var preOptionvalues = optionValues.Split(',');
                                        foreach (var enumDef in restriction.MetaEnumValuesRE)
                                        {
                                            if (preOptionvalues.Contains(enumDef.Id))
                                            {
                                                report.InputRestrictions.Add(restriction.OptionHtmlId + enumDef.HtmlId, "true");
                                            }
                                        }
                                    }
                                }
                                else if (restriction.IsDateTime)
                                {
                                    //convert to user input format
                                    DateTime dt;
                                    key = prefix + "_value_1";
                                    if (report.PreInputRestrictions.ContainsKey(key))
                                    {
                                        if (DateTime.TryParseExact(report.PreInputRestrictions[key], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                        {
                                            report.InputRestrictions.Add(restriction.ValueHtmlId + "_1", ((IFormattable)dt).ToString(report.ExecutionView.CultureInfo.DateTimeFormat.ShortDatePattern, report.ExecutionView.CultureInfo));
                                        }
                                    }
                                    key = prefix + "_value_2";
                                    if (report.PreInputRestrictions.ContainsKey(key))
                                    {
                                        if (DateTime.TryParseExact(report.PreInputRestrictions[key], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                        {
                                            report.InputRestrictions.Add(restriction.ValueHtmlId + "_2", ((IFormattable)dt).ToString(report.ExecutionView.CultureInfo.DateTimeFormat.ShortDatePattern, report.ExecutionView.CultureInfo));
                                        }
                                    }
                                    key = prefix + "_value_3";
                                    if (report.PreInputRestrictions.ContainsKey(key))
                                    {
                                        if (DateTime.TryParseExact(report.PreInputRestrictions[key], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                        {
                                            report.InputRestrictions.Add(restriction.ValueHtmlId + "_3", ((IFormattable)dt).ToString(report.ExecutionView.CultureInfo.DateTimeFormat.ShortDatePattern, report.ExecutionView.CultureInfo));
                                        }
                                    }
                                    key = prefix + "_value_4";
                                    if (report.PreInputRestrictions.ContainsKey(key))
                                    {
                                        if (DateTime.TryParseExact(report.PreInputRestrictions[key], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                                        {
                                            report.InputRestrictions.Add(restriction.ValueHtmlId + "_4", ((IFormattable)dt).ToString(report.ExecutionView.CultureInfo.DateTimeFormat.ShortDatePattern, report.ExecutionView.CultureInfo));
                                        }
                                    }

                                }
                                else
                                {
                                    //standard values
                                    key = prefix + "_value_1";
                                    if (report.PreInputRestrictions.ContainsKey(key)) report.InputRestrictions.Add(restriction.ValueHtmlId + "_1", report.PreInputRestrictions[key]);
                                    key = prefix + "_value_2";
                                    if (report.PreInputRestrictions.ContainsKey(key)) report.InputRestrictions.Add(restriction.ValueHtmlId + "_2", report.PreInputRestrictions[key]);
                                    key = prefix + "_value_3";
                                    if (report.PreInputRestrictions.ContainsKey(key)) report.InputRestrictions.Add(restriction.ValueHtmlId + "_3", report.PreInputRestrictions[key]);
                                    key = prefix + "_value_4";
                                    if (report.PreInputRestrictions.ContainsKey(key)) report.InputRestrictions.Add(restriction.ValueHtmlId + "_4", report.PreInputRestrictions[key]);
                                }
                            }
                        }
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                //get restriction values from the form request (if any)
                foreach (string key in Request.Form.Keys)
                {
                    string value = Request.Form[key];
                    if (value != null)
                    {
                        if (key.EndsWith("_Option_Value"))
                        {
                            foreach (string optionValue in value.Split(','))
                            {
                                report.InputRestrictions.Add(optionValue, "true");
                            }
                        }
                        else
                        {
                            report.InputRestrictions.Add(key, value);
                        }
                    }
                }
            }
            report.PreInputRestrictions.Clear();
        }


        private ReportExecution initReportExecution(Report report, string viewGUID, string outputGUID, bool toResult)
        {
            Repository repository = report.Repository;

            report.ExecutionContext = toResult ? ReportExecutionContext.WebOutput : ReportExecutionContext.WebReport;
            report.SecurityContext = WebUser;
            report.CurrentViewGUID = report.ViewGUID;

            //Init Pre Input restrictions
            report.PreInputRestrictions.Clear();
            foreach (string key in Request.Form.Keys) report.PreInputRestrictions.Add(key, Request.Form[key]);
            foreach (string key in Request.QueryString.Keys) report.PreInputRestrictions.Add(key, Request.QueryString[key]);

            //execute to output
            if (!string.IsNullOrEmpty(outputGUID))
            {
                report.OutputToExecute = report.Outputs.FirstOrDefault(i => i.GUID == outputGUID);
                if (report.OutputToExecute == null) throw new Exception("Invalid report output to execute");
                if (!report.OutputToExecute.PublicExec && !string.IsNullOrEmpty(report.OutputToExecute.UserName) && WebUser.Name != report.OutputToExecute.UserName) throw new Exception("This output is not public and can only be executed by:" + report.OutputToExecute.UserName);
                report.ExecutionContext = ReportExecutionContext.WebOutput;
                report.CurrentViewGUID = report.OutputToExecute.ViewGUID;
            }

            //execute with custom view
            if (!string.IsNullOrEmpty(viewGUID)) report.CurrentViewGUID = viewGUID;

            ReportExecution execution = new ReportExecution() { Report = report };

            setSessionValue(report.ExecutionGUID, execution);
            report.WebUrl = GetWebUrl(Request, Response);

            //Purge temp files here
            FileHelper.PurgeTempApplicationDirectory();

            report.InitForExecution();
            initInputRestrictions(report);
            //Apply input restrictions if any
            if (report.InputRestrictions.Count > 0) execution.CheckInputRestrictions();

            return execution;
        }

        private ReportExecution getReportExecution(string execution_guid)
        {
            ReportExecution result = null;
            if (!string.IsNullOrEmpty(execution_guid) && getSessionValue(execution_guid) is ReportExecution)
            {
                result = getSessionValue(execution_guid) as ReportExecution;
            }
            return result;
        }

        #endregion
    }
}