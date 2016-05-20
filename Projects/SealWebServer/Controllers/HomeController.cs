//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
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
using SealWebServer.Models;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;

namespace SealWebServer.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class HomeController : Controller
    {
        public const string SessionRepository = "SessionRepository";
        public const string SessionLastFolder = "SessionLastFolder";
        public const string SessionUser = "SessionUser";
        public const string SessionNavigationContext = "SessionNavigationContext";
        public const string SealCultureCookieName = "SealReport_Culture_Name";

        Repository Repository
        {
            get
            {
                return Session[SessionRepository] as Repository;
            }
        }

        Repository CreateRepository()
        {
            Repository repository = Repository.Create();
            repository.WebApplicationPath = Path.Combine(Request.PhysicalApplicationPath, "bin");
            Session[SessionRepository] = repository;
            return repository;
        }

        SecurityUser WebUser
        {
            get
            {
                return Session[SessionUser] as SecurityUser;
            }
        }

        SecurityUser CreateWebUser()
        {
            if (Repository == null)
            {
                CreateRepository();
            }
            var user = new SecurityUser(Repository.Security);
            Session[SessionUser] = user;
            return user;
        }

        NavigationContext NavigationContext
        {
            get
            {
                NavigationContext result = Session[SessionNavigationContext] as NavigationContext;
                if (result == null)
                {
                    result = new NavigationContext();
                    Session[SessionNavigationContext] = result;

                }
                return result;
            }
        }

        ContentResult HandleException(Exception ex)
        {
            Helper.WriteLogEntryWeb(EventLogEntryType.Error, "Unexpected error got:\r\n{0}\r\n\r\n{1}\r\n\r\n{2}", ex.Message, Request.Url.OriginalString, ex.StackTrace);
#if DEBUG
            return Content(string.Format("<b>Sorry, we got an unexpected exception.</b><br>{0}<br>{1}<br>{2}", ex.Message, Request.Url.OriginalString, ex.StackTrace));
#else
            return Content("<b>Sorry, we got an unexpected exception.</b><br>Please consult the Windows Event Log on the server machine to have more information...");
#endif
        }

        bool CheckAuthentication()
        {
            if (WebUser == null) CreateWebUser();

            if (!WebUser.IsAuthenticated && !WebUser.Security.PromptUserPassword)
            {
                Authenticate();
            }
            return WebUser.IsAuthenticated;
        }

        void Authenticate()
        {
            WebUser.WebPrincipal = User;
            WebUser.Authenticate();
            Helper.WriteLogEntryWeb(WebUser.IsAuthenticated ? EventLogEntryType.SuccessAudit : EventLogEntryType.FailureAudit, WebUser.AuthenticationSummary);
        }

        ContentResult GetContentResult(string filePath)
        {
            Response.Clear();
            Response.ClearContent();
            Response.ClearHeaders();
            Response.AppendHeader("title", Path.GetFileName(filePath));
            Response.AddHeader("content-disposition", "inline;filename=\"" + Path.GetFileName(filePath) + "\"");
            Response.ContentType = "text/html";
            Response.Cache.SetLastModified(DateTime.Now);
            return Content(System.IO.File.ReadAllText(filePath));
        }

        public ActionResult Main()
        {
            ActionResult result;
            try
            {
                if (Repository == null) CreateRepository();
                if (WebUser == null) CreateWebUser();

#if DEBUG
                if (Repository != null) Repository.FlushTranslationUsage();
#endif
                if (Repository.MustReload())
                {
                    //Repository must be updated...
                    CreateRepository();
                    WebUser.Security = Repository.Security;
                }

                if (!WebUser.IsAuthenticated)
                {
                    //User authentication
                    if (!WebUser.Security.PromptUserPassword)
                    {
                        Authenticate();
                    }
                }

                if (!WebUser.IsAuthenticated && !string.IsNullOrEmpty(WebUser.Error))
                {
                    string messagePrefix = Repository.TranslateWeb("Error got during user authentication.");
                    if (!WebUser.Error.StartsWith(messagePrefix))
                    {
                        WebUser.Error = messagePrefix + "\r\n" + WebUser.Error + "\r\n" + WebUser.Warning;
                    }
                }

                if (Request.Cookies[SealCultureCookieName] != null)
                {
                    //Set culture from cookie
                    string culture = Request.Cookies[SealCultureCookieName].Value;
                    if (!string.IsNullOrEmpty(culture)) Repository.SetCultureInfo(culture);
                }

                result = View(Repository);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return result;
        }

        public ActionResult ActionLogin(string user_name, string password)
        {
            try
            {
                CreateRepository();
                CreateWebUser();

                WebUser.WebPrincipal = User;
                WebUser.WebUserName = user_name;
                WebUser.WebPassword = password;
                Authenticate();
                return Redirect(Request.ApplicationPath == "/" ? "/" : "Main");
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        public ActionResult ActionLogout()
        {
            try
            {
                CreateWebUser();
                return Redirect(Request.ApplicationPath == "/" ? "/" : "Main");
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        string _loginContent = "<html><script>window.top.location.href='Main';</script></html>";
        string _noReportFoundMessage
        {
            get
            {
                return Repository.TranslateWeb("Sorry, this report is not in your session anymore...");
            }
        }

        public ActionResult ActionSetUserInfo(string culture)
        {
            try
            {
                var cookie = new HttpCookie(SealCultureCookieName, culture);
                cookie.Expires = DateTime.Now.AddYears(2);
                Response.Cookies.Add(cookie);
                return Redirect(Request.ApplicationPath == "/" ? "/" : "Main");
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        public ActionResult Menu()
        {
            ActionResult result;
            try
            {
                CheckAuthentication();
                result = View(new MenuModel() { Repository = Repository, User = WebUser, MainPath = (Request.ApplicationPath == "/" ? "/" : "Main") });
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return result;
        }

        public ActionResult Detail(string folder, int? mode)
        {
            ActionResult result;
            try
            {
                CheckAuthentication();

                if (string.IsNullOrEmpty(folder))
                {
                    folder = Session[SessionLastFolder] == null ? Repository.ReportsFolder : Session[SessionLastFolder] as string;
                }
                if (!folder.StartsWith(Repository.ReportsFolder) && folder != "*") folder = Repository.ReportsFolder + folder;

                SecurityFolder securityFolder = WebUser.FindSecurityFolder(folder);
                if (securityFolder != null && !string.IsNullOrEmpty(securityFolder.DescriptionFile))
                {
                    string descFile = Path.Combine(folder, securityFolder.DescriptionFile);
                    string descFileCSHTML = Path.Combine(folder, Path.GetFileNameWithoutExtension(securityFolder.DescriptionFile) + ".cshtml");
                    Session[SessionLastFolder] = folder;
                    if (System.IO.File.Exists(descFileCSHTML))
                    {
                        //Assume that it is a view...try to copy it in the Views\Home folder
                        string destFile = Path.Combine(Path.Combine(Request.PhysicalApplicationPath, "Views\\Home"), Path.GetFileName(descFileCSHTML));
                        try
                        {
                            if (!System.IO.File.Exists(destFile)) System.IO.File.Copy(descFileCSHTML, destFile, false);
                        }
                        catch { }
                        return View(Path.GetFileNameWithoutExtension(destFile), (object)folder);
                    }
                    else if (System.IO.File.Exists(descFile))
                    {
                        return new FilePathResult(descFile, "text/html");
                    }
                }

                FolderDetailModel model = new FolderDetailModel() { FolderPath = folder, Repository = Repository, User = WebUser, Mode = mode != null ? mode.Value : 0 };
                if (folder == "*")
                {
                    model.IsRecursive = true;
                    model.FolderPath = Repository.ReportsFolder;
                }
                else
                {
                    Session[SessionLastFolder] = folder;
                }
                result = View(model);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return result;
        }

        public ActionResult InitExecuteReport(string path, string viewGUID, string outputGUID)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                string filePath = Repository.ReportsFolder + path;
                if (System.IO.File.Exists(filePath))
                {
                    SecurityFolder securityFolder = WebUser.FindSecurityFolder(Path.GetDirectoryName(filePath));
                    if (securityFolder == null) throw new Exception("Error: this folder is not published");
                    if (!string.IsNullOrEmpty(outputGUID))
                    {
                        if (securityFolder.PublicationType != PublicationType.ExecuteOutput) throw new Exception("Error: outputs cannot be executed");
                    }

                    Repository repository = Repository.CreateFast();
                    Report reportToExecute = Report.LoadFromFile(filePath, repository);
                    reportToExecute.ExecutionContext = ReportExecutionContext.WebReport;
                    reportToExecute.SecurityContext = WebUser;
                    reportToExecute.CurrentViewGUID = reportToExecute.ViewGUID;

                    //Init Pre Input restrictions
                    reportToExecute.PreInputRestrictions.Clear();
                    foreach (string key in Request.Form.Keys) reportToExecute.PreInputRestrictions.Add(key, Request.Form[key]);

                    //execute to output
                    if (!string.IsNullOrEmpty(outputGUID))
                    {
                        reportToExecute.OutputToExecute = reportToExecute.Outputs.FirstOrDefault(i => i.GUID == outputGUID);
                        reportToExecute.ExecutionContext = ReportExecutionContext.WebOutput;
                        if (reportToExecute.OutputToExecute != null) reportToExecute.CurrentViewGUID = reportToExecute.OutputToExecute.ViewGUID;
                    }

                    //execute with custom view
                    if (!string.IsNullOrEmpty(viewGUID)) reportToExecute.CurrentViewGUID = viewGUID;

                    ReportExecution execution = new ReportExecution() { Report = reportToExecute };

                    Session[reportToExecute.ExecutionGUID] = execution;
                    int index = Request.Url.OriginalString.ToLower().IndexOf("initexecutereport");
                    if (index == -1) throw new Exception("Invalid URL");
                    reportToExecute.WebUrl = Request.Url.OriginalString.Substring(0, index);
                    repository.WebPublishFolder = Path.Combine(Request.PhysicalApplicationPath, "temp");
                    repository.WebApplicationPath = Path.Combine(Request.PhysicalApplicationPath, "bin");
                    if (!Directory.Exists(repository.WebPublishFolder)) Directory.CreateDirectory(repository.WebPublishFolder);
                    FileHelper.PurgeTempDirectory(repository.WebPublishFolder);

                    reportToExecute.InitForExecution();
                    execution.RenderHTMLDisplayForViewer();
                    return GetContentResult(reportToExecute.HTMLDisplayFilePath);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
            return Content("Error: Report file not found.\r\n");
        }

        public ActionResult ViewFile(string path)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                string tempFolder = Path.Combine(Path.Combine(Request.PhysicalApplicationPath, "temp"));
                FileHelper.PurgeTempDirectory(tempFolder);

                string filePath = Repository.ReportsFolder + path;
                if (System.IO.File.Exists(filePath))
                {
                    SecurityFolder securityFolder = WebUser.FindSecurityFolder(Path.GetDirectoryName(filePath));
                    if (securityFolder == null) throw new Exception("Error: this folder is not published");

                    filePath = Report.CopySealFile(filePath, tempFolder);
                    int index = Request.Url.OriginalString.ToLower().IndexOf("viewfile");
                    if (index == -1) throw new Exception("Invalid URL");
                    string url = Request.Url.OriginalString.Substring(0, index) + "temp/" + Path.GetFileName(filePath);
                    return Redirect(url);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
            return Content("Error: File not found.\r\n");
        }

        string RenderRazorViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        public ActionResult InitExecuteReportViewOutput(string report)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                string filePath = Repository.ReportsFolder + report;
                if (System.IO.File.Exists(filePath))
                {
                    SecurityFolder securityFolder = WebUser.FindSecurityFolder(Path.GetDirectoryName(filePath));
                    if (securityFolder == null) throw new Exception("Error: this folder is not published");
                    Repository repository = Repository;
                    Report reportToExecute = Report.LoadFromFile(filePath, repository);
                    reportToExecute.SecurityContext = WebUser;
                    return Content(RenderRazorViewToString("ExecuteViewOutput", reportToExecute));
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
            return Content("Error: Report file not found.\r\n");
        }

        public ActionResult InitUserPreferences()
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                return Content(RenderRazorViewToString("UserPreferences", WebUser));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        public ActionResult ActionExecuteReport(string execution_guid)
        {
            Debug.WriteLine(string.Format("ActionExecuteReport {0}", execution_guid));

            try
            {

                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    Report report = execution.Report;

                    Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Starting report {1} for user '{0}'", WebUser.Name, report.FilePath);
                    initInputRestrictions(report);
                    while (execution.IsConvertingToExcel) Thread.Sleep(100);
                    report.IsNavigating = false;
                    execution.Execute();
                    return null;
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return null;
        }

        public ActionResult ActionNavigate(string execution_guid)
        {
            Debug.WriteLine(string.Format("ActionNavigate {0}", execution_guid));

            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;

                    string nav = Request.Form[ReportExecution.HtmlId_navigation_id];
                    execution = NavigationContext.Navigate(nav, execution.RootReport);
                    Report report = execution.Report;
                    Session[report.ExecutionGUID] = execution;

                    Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Navigation report {1} for user '{0}'", WebUser.Name, report.FilePath);

                    report.ExecutionContext = ReportExecutionContext.WebReport;
                    report.SecurityContext = WebUser;
                    report.CurrentViewGUID = report.ViewGUID;

                    report.InitForExecution();
                    execution.RenderHTMLDisplayForViewer();

                    return Json(new { result_url = report.WebTempUrl + Path.GetFileName(report.HTMLDisplayFilePath) }); ;
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
            return null;
        }

        public ActionResult ActionGetNavigationLinks(string execution_guid)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    Report report = execution.Report;

                    return Json(new { links = NavigationContext.GetNavigationLinksHTML(execution.RootReport) });
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return null;
        }

        public ActionResult ActionRefreshReport(string execution_guid)
        {
            Debug.WriteLine(string.Format("ActionRefreshReport {0}", execution_guid));

            string error = "";
            try
            {
                if (!CheckAuthentication())
                {
                    error = Helper.ToHtml(_noReportFoundMessage);
                }
                else if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    Report report = execution.Report;
                    if (report.IsExecuting)
                    {
                        return Json(new { processing_message = Helper.ToHtml(report.ExecutionHeader), execution_messages = Helper.ToHtml(report.ExecutionMessages), result_url = "" });
                    }
                    else if (execution.IsConvertingToExcel)
                    {
                        return Json(new { processing_message = Helper.ToHtml(report.Translate("Executing report...")), execution_messages = Helper.ToHtml(report.ExecutionMessages), result_url = "" });
                    }
                    else if (report.Status == ReportStatus.Executed)
                    {
                        NavigationContext.SetNavigation(execution);

                        Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Viewing result of report {1} for user '{0}'", WebUser.Name, report.FilePath);
                        if (report.HasErrors) Helper.WriteLogEntryWeb(EventLogEntryType.Error, "Report {0} ({1}) execution errors:\r\n{2}", report.FilePath, WebUser.Name, report.ExecutionErrors);
                        string filePath = report.ForOutput || report.HasExternalViewer ? report.HTMLDisplayFilePath : report.ResultFilePath;
                        if (!System.IO.File.Exists(filePath)) throw new Exception("Error: Result file path does not exists...");
                        return Json(new { result_url = report.WebTempUrl + Path.GetFileName(filePath) }); ;
                    }
                    else
                    {
                        //return empty data, means result is ready
                        return Content("");
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

        public ActionResult ActionViewOutputResult(string execution_guid)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    Report report = execution.Report;
                    return Redirect(publishReportResult(report));
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
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    Report report = execution.Report;
                    execution.Report.LogMessage(report.Translate("Cancelling report..."));
                    report.Cancel = true;
                    return null;
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return null;
        }

        public ActionResult ActionUpdateViewParameter(string execution_guid, string parameter_view_id, string parameter_view_name, string parameter_view_value)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    Report report = execution.Report;
                    report.UpdateViewParameter(parameter_view_id, parameter_view_name, parameter_view_value);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }


        public ActionResult ActionViewHtmlResult(string execution_guid)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    string resultPath = execution.GenerateHTMLResult();
                    return Redirect(execution.Report.WebTempUrl + Path.GetFileName(resultPath));
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }

        public ActionResult ActionViewPrintResult(string execution_guid)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    string resultPath = execution.GeneratePrintResult();
                    return Redirect(execution.Report.WebTempUrl + Path.GetFileName(resultPath));
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }

        public ActionResult ActionViewPDFResult(string execution_guid)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    if (execution.IsConvertingToPDF) return Content(Repository.TranslateWeb("Sorry, the conversion is being in progress in another window..."));

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
                    return Redirect(execution.Report.WebTempUrl + Path.GetFileName(resultPath));
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }

        public ActionResult ActionViewExcelResult(string execution_guid)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    if (execution.IsConvertingToExcel) return Content(Repository.TranslateWeb("Sorry, the conversion is being in progress in another window..."));

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
                    return Redirect(execution.Report.WebTempUrl + Path.GetFileName(resultPath));
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return Content(_noReportFoundMessage);
        }


        /// <summary>
        /// Used to call an additional .cshtml in detail frame
        /// </summary>
        [HttpPost]
        public ActionResult DetailViewCallback()
        {
            if (!CheckAuthentication()) return Content(_loginContent);

            if (!string.IsNullOrEmpty(Request["View"])) return View(Request["View"]);
            return View();
        }

        #region Web Interface

        [HttpPost]
        public ActionResult SWILogin(string user, string password)
        {
            try
            {
                CreateRepository();
                CreateWebUser();
                WebUser.WebPrincipal = User;
                WebUser.WebUserName = user;
                WebUser.WebPassword = password;
                Authenticate();
                return Json(new SWIUserProfile() { name = WebUser.Name, group = WebUser.SecurityGroupsDisplay, culture = Repository.CultureInfo.EnglishName });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SWIGetFolders(string path)
        {
            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated) throw new Exception("Error: user is not authenticated");
                if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");

                var folder = new SWIFolder() { path = path, name = Repository.TranslateFolderName(path) };
                fillFolder(folder);
                return Json(folder);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SWIGetFolderDetail(string path)
        {
            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated) throw new Exception("Error: user is not authenticated");
                if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");

                var files = new List<SWIFile>();
                path = Repository.ReportsFolder + (path == null ? "" : path);
                SecurityFolder securityFolder = WebUser.FindSecurityFolder(path);
                if (securityFolder != null || WebUser.IsParentSecurityFolder(path))
                {
                    foreach (string newPath in Directory.GetFiles(Path.Combine(Repository.ReportsFolder, path), securityFolder.UsedSearchPattern).Where(i => !Report.IsSealAttachedFile(i)))
                    {
                        if (Path.GetFileName(newPath) != securityFolder.DescriptionFile && Path.GetFileName(newPath) != Path.GetFileNameWithoutExtension(securityFolder.DescriptionFile) + ".cshtml")
                        {
                            var filePath = newPath.Substring(Repository.ReportsFolder.Length);
                            files.Add(new SWIFile() {
                                path = filePath,
                                name = Repository.TranslateFileName(newPath),
                                isReport = Report.IsSealReportFile(newPath),
                                execOutput = securityFolder.PublicationType == PublicationType.ExecuteOutput
                            });
                        }
                    }
                }
                return Json(new SWIFolderDetail () { files = files.ToArray() });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SWIGetReportDetail(string path)
        {
            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated) throw new Exception("Error: user is not authenticated");
                if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");

                string newPath = Repository.ReportsFolder + path;
                if (!System.IO.File.Exists(newPath)) throw new Exception("Report path not found");
                SecurityFolder securityFolder = WebUser.FindSecurityFolder(Path.GetDirectoryName(newPath));
                if (securityFolder == null) throw new Exception("Error: this folder is not published");

                Repository repository = Repository;
                Report report = Report.LoadFromFile(newPath, repository);
                SWIReportDetail result = new SWIReportDetail();
                result.views = (from i in report.Views select new SWIView() { guid = i.GUID, name = i.Name, displayName = report.TranslateViewName(i.Name) }).ToArray();
                result.outputs= (from i in report.Outputs select new SWIOutput() { guid = i.GUID, name = i.Name, displayName = report.TranslateOutputName(i.Name) }).ToArray();
                return Json(result);

            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


        [HttpPost]
        public ActionResult SWIExecuteReport(string path, string viewGUID, string outputGUID, string format)
        {
            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated) throw new Exception("Error: user is not authenticated");
                if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");

                string filePath = Repository.ReportsFolder + path;
                if (System.IO.File.Exists(filePath))
                {
                    SecurityFolder securityFolder = WebUser.FindSecurityFolder(Path.GetDirectoryName(filePath));
                    if (securityFolder == null) throw new Exception("Error: this folder is not published");
                    if (!string.IsNullOrEmpty(outputGUID))
                    {
                        if (securityFolder.PublicationType != PublicationType.ExecuteOutput) throw new Exception("Error: outputs cannot be executed");
                    }

                    Repository repository = Repository.CreateFast();
                    Report report = Report.LoadFromFile(filePath, repository);
                    report.ExecutionContext = ReportExecutionContext.WebReport;
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
                        report.ExecutionContext = ReportExecutionContext.WebOutput;
                        if (report.OutputToExecute != null) report.CurrentViewGUID = report.OutputToExecute.ViewGUID;
                    }

                    //execute with custom view
                    if (!string.IsNullOrEmpty(viewGUID)) report.CurrentViewGUID = viewGUID;

                    ReportExecution execution = new ReportExecution() { Report = report };

                    Session[report.ExecutionGUID] = execution;

                    int index = Request.Url.OriginalString.ToLower().IndexOf("swiexecutereport");
                    if (index == -1) throw new Exception("Invalid URL");
                    report.WebUrl = Request.Url.OriginalString.Substring(0, index);
                    repository.WebPublishFolder = Path.Combine(Request.PhysicalApplicationPath, "temp");
                    repository.WebApplicationPath = Path.Combine(Request.PhysicalApplicationPath, "bin");
                    if (!Directory.Exists(repository.WebPublishFolder)) Directory.CreateDirectory(repository.WebPublishFolder);
                    FileHelper.PurgeTempDirectory(repository.WebPublishFolder);

                    report.InitForExecution();

                    initInputRestrictions(report);

                    execution.Execute();
                    while (report.Status != ReportStatus.Executed) System.Threading.Thread.Sleep(100);

                    string result = "";
                    if (!string.IsNullOrEmpty(outputGUID))
                    {
                        //Copy the result output to temp
                        result = publishReportResult(report);
                    }
                    else {
                        string fileResult = "";
                        if (string.IsNullOrEmpty(format)) format = "html";
                        if (format.ToLower() == "print") fileResult = execution.GeneratePrintResult();
                        else if (format.ToLower() == "pdf") fileResult = execution.GeneratePDFResult();
                        else if (format.ToLower() == "excel") fileResult = execution.GenerateExcelResult();
                        else fileResult = execution.GenerateHTMLResult();
                        result = execution.Report.WebTempUrl + Path.GetFileName(fileResult);
                    }

                    return Json(new { url = result });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
            return Content("Error: Report file not found.\r\n");
        }

        [HttpPost]
        public ActionResult SWILogout()
        {
            try
            {
                CreateWebUser();
                return Json(new {});
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


        [HttpPost]
        public ActionResult SWISetUserProfile(string culture)
        {
            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated) throw new Exception("Error: user is not authenticated");
                if (string.IsNullOrEmpty(culture)) throw new Exception("Error: culture must be supplied");

                if (!Repository.SetCultureInfo(culture)) throw new Exception("Invalid culture name:" + culture);
                return Json(new {});
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


        [HttpPost]
        public ActionResult SWIGetUserProfile()
        {
            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated) throw new Exception("Error: user is not authenticated");
                return Json(new SWIUserProfile() { name = WebUser.Name, group = WebUser.SecurityGroupsDisplay, culture = Repository.CultureInfo.EnglishName });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SWITranslate(string context, string instance, string reference)
        {
            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated) throw new Exception("Error: user is not authenticated");
                if (!string.IsNullOrEmpty(instance)) return Json(new { text = Repository.RepositoryTranslate(context, instance, reference) });
                return Json(new { text = Repository.Translate(context, reference) });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SWIGetVersions()
        {
            try
            {
                return Json(new { SWIVersion = "1.0", SRVersion = Repository.ProductVersion });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        #endregion


        #region private methods
        void fillFolder(SWIFolder folder)
        {
            List<SWIFolder> subFolders = new List<SWIFolder>();
            string folderPath = Repository.ReportsFolder + (folder.path == null ? "\\" : folder.path);
            foreach (string subFolder in Directory.GetDirectories(folderPath))
            {
                SecurityFolder securityFolder = WebUser.FindSecurityFolder(subFolder);
                if (securityFolder == null && !WebUser.IsParentSecurityFolder(subFolder)) continue;
                var subFolderPath = folderPath = subFolder.Substring(Repository.ReportsFolder.Length);
                var sub = new SWIFolder() { path = subFolderPath, name = Repository.TranslateFolderName(subFolder) };
                fillFolder(sub);
                subFolders.Add(sub);
            }
            folder.folders = subFolders.ToArray();
        }

        void initInputRestrictions(Report report)
        {
            report.InputRestrictions.Clear();
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
                                    //options
                                    key = prefix + "_enum_values";
                                    if (report.PreInputRestrictions.ContainsKey(key))
                                    {
                                        var optionValues = report.PreInputRestrictions[key];
                                        //Convert values into index of the enum...
                                        var preOptionvalues = optionValues.Split(',');
                                        for (int k = 0; k < restriction.EnumRE.Values.Count; k++)
                                        {
                                            var enumDef = restriction.EnumRE.Values[k];
                                            if (preOptionvalues.Contains(enumDef.Id))
                                            {
                                                report.InputRestrictions.Add(restriction.OptionHtmlId + k.ToString(), "true");
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
            else {
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

        string publishReportResult(Report report)
        {
            if (!string.IsNullOrEmpty(report.ResultFilePath))
            {
                string publishPath = FileHelper.GetUniqueFileName(Path.Combine(Path.GetDirectoryName(report.HTMLDisplayFilePath), Path.GetFileName(report.ResultFilePath)));
                System.IO.File.Copy(report.ResultFilePath, publishPath);
                if (!report.ForPDFConversion && !report.HasExternalViewer)
                {
                    foreach (ReportModel model in report.Models)
                    {
                        if (model.HasSerie)
                        {
                            foreach (ResultPage page in model.Pages.Where(i => i.ChartPath != null))
                            {
                                System.IO.File.Copy(page.ChartPath, Path.Combine(Path.GetDirectoryName(report.HTMLDisplayFilePath), Path.GetFileName(page.ChartPath)), true);
                            }
                        }
                    }
                }
                return report.WebTempUrl + Path.GetFileName(publishPath);
            }
            return "";
        }
        #endregion
    }
}