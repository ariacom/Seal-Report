//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

    public partial class HomeController : Controller
    {
        public const string SessionRepository = "SessionRepository";
        public const string SessionLastFolder = "SessionLastFolder";
        public const string SessionUser = "SessionUser";
        public const string SessionNavigationContext = "SessionNavigationContext";
        public const string SessionDashboardExecutions = "SessionDashboardExecutions";
        public const string SessionUploadedFiles = "SessionUploadedFiles";

        public const string SealCultureCookieName = "SR_Culture_Name";
        public const string SealLastViewCookieName = "SR_Last_View";
        public const string SealLastFolderCookieName = "SR_Last_Folder";
        public const string SealLastDashboardCookieName = "SR_Last_Dashboard";

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

        public string Translate(string reference)
        {
            if (Repository == null) return reference;
            return Repository.TranslateWeb(reference);
        }

        Repository CreateRepository()
        {
            Repository repository = Repository.Create();
            repository.WebApplicationPath = RequestPhysicalApplicationPath;
            //Set culture from cookie
            string culture = getCookie(SealCultureCookieName);
            if (!string.IsNullOrEmpty(culture)) repository.SetCultureInfo(culture);

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

            //Load profile
            if (System.IO.File.Exists(user.ProfilePath)) user.Profile = SecurityUserProfile.LoadFromFile(user.ProfilePath);
            user.Profile.Path = user.ProfilePath;

            setSessionValue(SessionUser, user);
            //Clear previous Session variables
            setSessionValue(SessionNavigationContext, null);
            setSessionValue(SessionDashboardExecutions, null);

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
            WebHelper.WriteWebException(ex, getContextDetail(Request, WebUser));
#if DEBUG
            var content = Content(string.Format("<b>Sorry, we got an unexpected exception.</b><br>{0}<br>{1}<br>{2}", ex.Message, RequestUrl, ex.StackTrace));
#else
            var content = Content("<b>Sorry, we got an unexpected exception.</b><br>Please consult the Windows Event Log on the server machine to have more information...");
#endif
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
            return Content(System.IO.File.ReadAllText(filePath));
        }

        public ActionResult Main()
        {
            ActionResult result;
            try
            {
                if (Repository == null) CreateRepository();
                result = View(Repository);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return result;
        }


        public ActionResult ActionExecuteReport(string execution_guid)
        {
            writeDebug("ActionExecuteReport");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    Report report = execution.Report;
                    WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, string.Format("Starting report '{0}'", report.FilePath), getContextDetail(Request, WebUser));
                    report.IsNavigating = false;
                    initInputRestrictions(execution, report);
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

        public ActionResult ActionNavigate(string execution_guid)
        {
            writeDebug("ActionNavigate");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                ReportExecution execution = getExecution(execution_guid);
                if (execution != null)
                {
                    if (execution.RootReport == null) execution.RootReport = execution.Report;

                    string nav = Request.Form[ReportExecution.HtmlId_navigation_id];
                    NameValueCollection parameters = null;
                    if (Request.Form.ContainsKey(ReportExecution.HtmlId_navigation_parameters)) parameters = HttpUtility.ParseQueryString(Request.Form[ReportExecution.HtmlId_navigation_parameters]);

                    if (nav.StartsWith(NavigationLink.FileDownloadPrefix)) //File download
                    {
                        var filePath = NavigationContext.NavigateScript(nav, execution.Report, parameters);
                        if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                        {
                            return getFileResult(filePath, null);
                        }
                        else
                        {
                            throw new Exception(string.Format("Invalid file path got from the navigation script: '{0}'", filePath));
                        }
                    }
                    else if (nav.StartsWith(NavigationLink.ReportScriptPrefix)) //Report Script
                    {
                        var data = NavigationContext.NavigateScript(nav, execution.Report, parameters);
                        return Json(data);
                    }
                    else
                    {
                        execution = NavigationContext.Navigate(nav, execution.RootReport);
                        Report report = execution.Report;
                        setSessionValue(report.ExecutionGUID, execution);

                        WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, string.Format("Navigation report '{0}'", report.FilePath), getContextDetail(Request, WebUser));

                        report.ExecutionContext = ReportExecutionContext.WebReport;
                        report.SecurityContext = WebUser;
                        report.CurrentViewGUID = report.ViewGUID;

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

        public ActionResult ActionGetNavigationLinks(string execution_guid)
        {
            writeDebug("ActionGetNavigationLinks");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
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
            return Json(new { error = error });
        }

        public ActionResult Result(string execution_guid)
        {
            writeDebug("Result");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    Report report = execution.Report;
                    NavigationContext.SetNavigation(execution);

                    WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, string.Format("Viewing result of report '{0}'", report.FilePath), getContextDetail(Request, WebUser));
                    if (report.HasErrors) WebHelper.WriteLogEntryWeb(EventLogEntryType.Error, string.Format("Report '{0}' execution errors:\r\n{1}", report.FilePath, report.ExecutionErrors), getContextDetail(Request, WebUser));
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

        public ActionResult ActionUpdateViewParameter(string execution_guid, string parameter_view_id, string parameter_view_name, string parameter_view_value)
        {
            writeDebug("ActionUpdateViewParameter");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution == null)
                {
                    execution = DashboardExecutions.FirstOrDefault(i => i.Report.ExecutionGUID == execution_guid);
                }
                if (execution != null)
                {
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


        public ActionResult HtmlResult(string execution_guid)
        {
            writeDebug("HtmlResult");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
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

        public ActionResult HtmlResultFile(string execution_guid)
        {
            writeDebug("HtmlResultFile {0}");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    return getFileResult(execution.Report.ResultFilePath, execution.Report);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }


        public ActionResult PrintResult(string execution_guid)
        {
            writeDebug("PrintResult");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
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

        public ActionResult PDFResult(string execution_guid)
        {
            writeDebug("PDFResult");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
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

        public ActionResult ExcelResult(string execution_guid)
        {
            writeDebug("ExcelResult");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
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


        public ActionResult CSVResult(string execution_guid)
        {
            writeDebug("CSVResult");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
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


        public ActionResult ActionGetTableData(string execution_guid, string viewid, string pageid, string parameters)
        {
            writeDebug("ActionGetTableData");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                ReportExecution execution = getExecution(execution_guid);
                if (execution != null)
                {
                    var view = execution.Report.ExecutionView.GetView(viewid);
                    if (view != null && view.ModelView != null)
                    {
                        var page = view.ModelView.Model.Pages.FirstOrDefault(i => i.PageId == pageid);
                        if (page != null)
                        {
                            return Json(page.DataTable.GetLoadTableData(view, parameters));
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


        public ActionResult ActionUpdateEnumValues(string execution_guid, string enum_id, string values)
        {
            writeDebug("ActionUpdateEnumValues");
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var execution = getReportExecution(execution_guid);
                if (execution != null)
                {
                    execution.UpdateEnumValues(enum_id, values);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
            return Json(new { });
        }

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


#region private methods

        void checkSWIAuthentication()
        {
            if (WebUser == null || !WebUser.IsAuthenticated) throw new Exception("Error: user is not authenticated");
        }

        JsonResult HandleSWIException(Exception ex)
        {
            WebHelper.WriteWebException(ex, getContextDetail(Request, WebUser));
            return Json(new { error = ex.Message, authenticated = (WebUser != null && WebUser.IsAuthenticated) });
        }

        ReportExecution getExecution(string execution_guid)
        {
            var execution = getReportExecution(execution_guid);
            if (execution == null)
            {
                //Navigation from dashboard, set the root report in the session
                execution = DashboardExecutions.FirstOrDefault(i => i.Report.ExecutionGUID == execution_guid);
                setSessionValue(execution_guid, execution);
            }
            return execution;
        }

        string getFullPath(string path)
        {
            path = FileHelper.ConvertOSFilePath(path);
            if (path.StartsWith(SWIFolder.GetPersonalRoot())) return Repository.GetPersonalFolder(WebUser) + path.Substring(1);
            else return Repository.ReportsFolder + path;
        }

        SWIFolder getParentFolder(string path)
        {
            path = FileHelper.ConvertOSFilePath(path);
            if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");
            return getFolder(SWIFolder.GetParentPath(path));
        }
        SWIFolder getFolder(string path)
        {
            checkSWIAuthentication();
            path = FileHelper.ConvertOSFilePath(path);
            if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");
            if (path.Contains(".." + Path.DirectorySeparatorChar.ToString())) throw new Exception("Error: invalid path");
            SWIFolder result = new SWIFolder();
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


        void fillFolder(SWIFolder folder)
        {
            List<SWIFolder> subFolders = new List<SWIFolder>();
            if (folder.IsPersonal && WebUser.PersonalFolderRight == PersonalFolderRight.None) return;

            string folderPath = folder.GetFullPath();
            foreach (string subFolder in Directory.GetDirectories(folderPath))
            {
                if (folder.IsPersonal && subFolder.ToLower() == WebUser.DashboardPersonalFolder.ToLower()) continue;

                SWIFolder sub = getFolder(folder.Combine(subFolder));
                //Add if right on this folder, or a sub folder is defined with this root
                if ((sub.right > 0) || WebUser.SecurityGroups.Exists(i => i.Folders.Exists(j => j.Path.StartsWith(sub.path + (sub.path == Path.DirectorySeparatorChar.ToString() ? "" : Path.DirectorySeparatorChar.ToString())) && j.FolderRight != FolderRight.None)))
                {
                    fillFolder(sub);
                    subFolders.Add(sub);
                }
            }
            folder.folders = subFolders.ToArray();
        }

        void searchFolder(SWIFolder folder, string pattern, List<SWIFile> files)
        {
            foreach (string newPath in Directory.GetFiles(folder.GetFullPath(), "*.*").Where(i => Path.GetFileName(i).ToLower().Contains(pattern.ToLower())))
            {
                if (folder.right > 0)
                {
                    files.Add(new SWIFile()
                    {
                        path = folder.Combine(Path.GetFileName(newPath)),
                        name = folder.fullname + Path.DirectorySeparatorChar.ToString() + Repository.TranslateFileName(newPath) + (FileHelper.IsSealReportFile(newPath) ? "" : Path.GetExtension(newPath)),
                        last = System.IO.File.GetLastWriteTime(newPath).ToString("G", Repository.CultureInfo),
                        isreport = FileHelper.IsSealReportFile(newPath),
                        right = folder.right
                    });
                }
            }

            foreach (string subFolder in Directory.GetDirectories(folder.GetFullPath()))
            {
                SWIFolder sub = getFolder(folder.Combine(subFolder));
                if (sub.right > 0) searchFolder(sub, pattern, files);
            }
        }

        void initInputRestrictions(ReportExecution execution, Report report)
        {
            report.InputRestrictions.Clear();

            //Do not use input restrictions for navigation...
            if (report.IsNavigating) return;

            // If we receive the "use_default_restrictions" field we define the field
            execution.UseDefaultRestrictions = false; //flag not set 
            if (report.PreInputRestrictions.ContainsKey("use_default_restrictions")) execution.UseDefaultRestrictions = Convert.ToBoolean(report.PreInputRestrictions["use_default_restrictions"]);

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
                                        foreach (var enumDef in restriction.EnumRE.Values)
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
            foreach (string key in Request.Query.Keys) report.PreInputRestrictions.Add(key, Request.Query[key]);

            //execute to output
            if (!string.IsNullOrEmpty(outputGUID))
            {
                report.OutputToExecute = report.Outputs.FirstOrDefault(i => i.GUID == outputGUID);
                if (report.OutputToExecute == null) throw new Exception("Invalid report output to execute");
                if (!report.OutputToExecute.PublicExec && WebUser.Name != report.OutputToExecute.UserName) throw new Exception("This output is not public and can only be executed by:" + report.OutputToExecute.UserName);
                report.ExecutionContext = ReportExecutionContext.WebOutput;
                report.CurrentViewGUID = report.OutputToExecute.ViewGUID;
            }

            //execute with custom view
            if (!string.IsNullOrEmpty(viewGUID)) report.CurrentViewGUID = viewGUID;

            ReportExecution execution = new ReportExecution() { Report = report };

            setSessionValue(report.ExecutionGUID, execution);
            int index = RequestUrl.ToLower().IndexOf("swexecutereport");
            if (index == -1) throw new Exception("Invalid URL");
            report.WebUrl = GetWebUrl(Request, Response);
            repository.WebApplicationPath = RequestPhysicalApplicationPath;

            //Purge temp files here
            FileHelper.PurgeTempApplicationDirectory();

            report.InitForExecution();
            initInputRestrictions(execution, report);
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
