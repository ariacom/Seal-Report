//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Seal.Helpers;
using Seal.Model;
using SealWebServer.Models;
using System.Security.Principal;
using System.Globalization;
using System.Threading;

namespace SealWebServer.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.ReadOnly)]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class HomeController : Controller
    {
        public const string SessionRepository = "SessionRepository";
        public const string SessionLastFolder = "SessionLastFolder";
        public const string SessionUser = "SessionUser";
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

        ContentResult HandleException(Exception ex)
        {
            Helper.WriteLogEntryWeb(EventLogEntryType.Error, "Unexpected error got:\r\n{0}\r\n\r\n{1}\r\n\r\n{2}", ex.Message, Request.Url.OriginalString, ex.StackTrace);
            return Content(string.Format("<b>Got unexpected exception:</b><br>{0}", ex.Message));
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

        public ActionResult Login(string user_name, string password)
        {
            try
            {
                CreateRepository();
                CreateWebUser();
                WebUser.WebPrincipal = User;
                WebUser.WebUserName = user_name;
                WebUser.WebPassword = password;
                Authenticate();
                return Redirect("Main");
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        public ActionResult Logout()
        {
            try
            {
                CreateWebUser();
                return Redirect("Main");
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
                if (!string.IsNullOrEmpty(culture))
                {
                    var cookie = new HttpCookie(SealCultureCookieName, culture);
                    cookie.Expires = DateTime.Now.AddYears(2);
                    Response.Cookies.Add(cookie);
                }
                return Redirect("Main");
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
                result = View(new MenuModel() { Repository = Repository, User = WebUser });
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return result;
        }

        public ActionResult Detail(string folder)
        {
            ActionResult result;
            try
            {
                CheckAuthentication();

                if (string.IsNullOrEmpty(folder))
                {
                    folder = Session[SessionLastFolder] == null ? Repository.ReportsFolder : Session[SessionLastFolder] as string;
                }

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

                FolderDetailModel model = new FolderDetailModel() { FolderPath = folder, Repository = Repository, User = WebUser };
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
                    if (!string.IsNullOrEmpty(outputGUID) || !string.IsNullOrEmpty(viewGUID))
                    {
                        if (securityFolder.PublicationType != PublicationType.ExecuteOutput) throw new Exception("Error: outputs cannot be executed");
                    }

                    Repository repository = Repository.CreateFast();
                    Report reportToExecute = Report.LoadFromFile(filePath, repository);
                    reportToExecute.ExecutionGUID = Guid.NewGuid().ToString();
                    reportToExecute.ExecutionContext = ReportExecutionContext.WebReport;
                    reportToExecute.SecurityContext = WebUser;
                    reportToExecute.CurrentViewGUID = reportToExecute.ViewGUID;

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
                    return Redirect(reportToExecute.WebTempUrl + Path.GetFileName(reportToExecute.HTMLDisplayFilePath));
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
                    if (securityFolder.PublicationType != PublicationType.ExecuteOutput) throw new Exception("Error: outputs cannot be executed");

                    Repository repository = Repository;
                    Report reportToExecute = Report.LoadFromFile(filePath, repository);
                    reportToExecute.ExecutionGUID = Guid.NewGuid().ToString();
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
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    Report report = execution.Report;


                    Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Starting report {1} for user '{0}'", WebUser.Name, report.FilePath);

                    report.InputRestrictions.Clear();
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

                    while (execution.IsConvertingToExcel) Thread.Sleep(100);
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
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    Report report = execution.Report;

                    string nav = Request.Form[ReportExecution.HtmlId_navigation_id];
                    execution = execution.Navigate(nav);
                    Session[execution_guid] = execution;

                    Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Navigation report {1} for user '{0}'", WebUser.Name, report.FilePath);

                    execution.RenderHTMLDisplayForViewer();
                    return Redirect(execution.Report.WebTempUrl + Path.GetFileName(execution.Report.HTMLDisplayFilePath));
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

                    return Json(new { links = report.GetNavigationLinksHTML() });
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
                        //Set last navigation path if any
                        report.SetLastNavigationLink();

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

        public ActionResult ViewOutputResult(string execution_guid)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    Report report = execution.Report;
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
                        return Redirect(report.WebTempUrl + Path.GetFileName(publishPath));
                    }
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
                    string resultPath = execution.GeneratePDFResult();
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
    }
}
