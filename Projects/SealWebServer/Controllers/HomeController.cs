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
using System.Threading;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Seal.Forms;

namespace SealWebServer.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public partial class HomeController : Controller
    {
        public const string SessionRepository = "SessionRepository";
        public const string SessionLastFolder = "SessionLastFolder";
        public const string SessionUser = "SessionUser";
        public const string SessionNavigationContext = "SessionNavigationContext";

        public const string SealCultureCookieName = "SR_Culture_Name";
        public const string SealLastFolderCookieName = "SR_Last_Folder";

        const string _loginContent = "<html><script>window.top.location.href='Main';</script></html>";
        string _noReportFoundMessage
        {
            get
            {
                return Repository.TranslateWeb("Sorry, this report is not in your session anymore...");
            }
        }

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

        public string GetCookie(string name)
        {
            string result = "";
            if (Request.Cookies[name] != null)
            {
                result = Request.Cookies[name].Value;
            }
            return result;
        }
        public void SetCookie(string name, string value)
        {
            var cookie = new HttpCookie(name, value);
            cookie.Expires = DateTime.Now.AddYears(2);
            Response.Cookies.Add(cookie);
        }

        public ActionResult Main()
        {

            ActionResult result;
            try
            {
                if (Repository == null) CreateRepository();
                //Set culture from cookie
                string culture = GetCookie(SealCultureCookieName);
                if (!string.IsNullOrEmpty(culture)) Repository.SetCultureInfo(culture);
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


        public ActionResult ActionGetTableData(string execution_guid, string viewid, string pageid, string parameters)
        {
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                if (!string.IsNullOrEmpty(execution_guid) && Session[execution_guid] is ReportExecution)
                {
                    ReportExecution execution = Session[execution_guid] as ReportExecution;
                    var view = execution.Report.ExecutionView.GetView(viewid);
                    if (view != null)
                    {
                        var page = view.Model.Pages.FirstOrDefault(i => i.PageId == pageid);
                        if (page != null)
                        {
                            return Json(page.DataTable.GetLoadTableData(view.Model, parameters), JsonRequestBehavior.AllowGet);
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


        #region private methods

        void checkSWIAuthentication()
        {
            if (WebUser == null || !WebUser.IsAuthenticated) throw new Exception("Error: user is not authenticated");
        }

        JsonResult handleSWIException(Exception ex)
        {
            return Json(new { error = ex.Message, authenticated = (WebUser != null && WebUser.IsAuthenticated) });
        }

        string getFullPath(string path)
        {
            if (path.StartsWith(SWIFolder.GetPersonalRoot())) return Repository.GetPersonalFolder(WebUser) + path.Substring(1);
            else return Repository.ReportsFolder + path;
        }

        SWIFolder getParentFolder(string path)
        {
            return getFolder(SWIFolder.GetParentPath(path));
        }
        SWIFolder getFolder(string path)
        {
            checkSWIAuthentication();
            if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");
            if (path.Contains("..\\")) throw new Exception("Error: invalid path");
            SWIFolder result = new SWIFolder();
            result.path = path;
            result.right = 0;
            result.SetFullPath(getFullPath(path));

            if (result.IsPersonal)
            {
                //Personal
                if (!WebUser.HasPersonalFolder) throw new Exception("Error: this user has no personal folder");
                result.SetManageFlag(true, true, result.Path == "");
                result.expand = false;
                string prefix = Repository.GetPersonalFolderName(WebUser);
                result.name = (result.Path == "" ? prefix : Path.GetFileName(result.Path));
                result.fullname = prefix + (result.Path == "" ? "\\" : "") + result.Path;
                result.right = (int)FolderRight.Edit;
            }
            else
            {
                result.name = (result.Path == "\\" ? Repository.TranslateWeb("Reports") : Repository.TranslateFolderName(path));
                result.fullname = Repository.TranslateWeb("Reports") + Repository.TranslateFolderPath(result.Path);
                SecurityFolder securityFolder = WebUser.FindSecurityFolder(path);
                if (securityFolder != null)
                {
                    result.SetManageFlag(securityFolder.UseSubFolders, securityFolder.ManageFolder, securityFolder.IsDefined);
                    result.expand = securityFolder.ExpandSubFolders;
                    result.right = (int)securityFolder.FolderRight;
                }
            }
            return result;
        }


        void fillFolder(SWIFolder folder)
        {
            List<SWIFolder> subFolders = new List<SWIFolder>();
            if (folder.IsPersonal && !WebUser.HasPersonalFolder) return;

            string folderPath = folder.GetFullPath();
            foreach (string subFolder in Directory.GetDirectories(folderPath))
            {
                //Hack: do not display images sub-directory when the directory contains the ui-* files (for jquery)
                if (Path.GetFileName(subFolder).ToLower() == "images")
                {
                    if (Directory.GetFiles(subFolder, "ui-*.png").Length > 10) continue;
                }

                SWIFolder sub = getFolder(folder.Combine(subFolder));
                //Add if right on this folder, or a sub folder is defined with this root
                if ((sub.right > 0) || WebUser.SecurityGroups.Exists(i => i.Folders.Exists(j => j.Path.StartsWith(sub.path + (sub.path == "\\" ? "" : "\\")) && j.FolderRight != FolderRight.None)))
                {
                    fillFolder(sub);
                    subFolders.Add(sub);
                }
            }
            folder.folders = subFolders.ToArray();
        }

        void searchFolder(SWIFolder folder, string pattern, List<SWIFile> files)
        {
            foreach (string newPath in Directory.GetFiles(folder.GetFullPath(), "*.*").Where(i => !FileHelper.IsSealAttachedFile(i) && Path.GetFileName(i).ToLower().Contains(pattern.ToLower())))
            {
                files.Add(new SWIFile()
                {
                    path = folder.Combine(Path.GetFileName(newPath)),
                    name = folder.fullname + "\\" + Repository.TranslateFileName(newPath) + (FileHelper.IsSealReportFile(newPath) ? "" : Path.GetExtension(newPath)),
                    last = System.IO.File.GetLastWriteTime(newPath).ToString("G", Repository.CultureInfo),
                    isReport = FileHelper.IsSealReportFile(newPath),
                    right = folder.right
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

        private ReportExecution initReportExecution(Report report, string viewGUID, string outputGUID)
        {
            Repository repository = report.Repository;

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
            //Apply input restrictions if any
            if (report.InputRestrictions.Count > 0) execution.CheckInputRestrictions();

            return execution;
        }

        #endregion
    }
}