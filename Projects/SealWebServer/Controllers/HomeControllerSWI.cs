//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Seal.Helpers;
using Seal.Model;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;

namespace SealWebServer.Controllers
{
    public partial class HomeController : Controller
    {
        [HttpPost]
        public ActionResult SWILogin(string user, string password)
        {
            WriteDebug("SWILogin");
            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated || (!string.IsNullOrEmpty(user) && WebUser.WebUserName != user))
                {
                    CreateRepository();
                    CreateWebUser();
                    WebUser.WebPrincipal = User;
                    WebUser.WebUserName = user;
                    WebUser.WebPassword = password;
                    Authenticate();

                    if (!WebUser.IsAuthenticated) throw new Exception(string.IsNullOrEmpty(WebUser.Error) ? Translate("Invalid user name or password") : WebUser.Error);
                }

                //Set culture from cookie
                string culture = GetCookie(SealCultureCookieName);
                if (!string.IsNullOrEmpty(culture)) Repository.SetCultureInfo(culture);

                //Set default view
                string view = GetCookie(SealLastViewCookieName);
                if (string.IsNullOrEmpty(view)) view = "reports";
                //Check rights
                if (WebUser.ViewType == Seal.Model.ViewType.Reports && view == "dashboards") view = "reports";
                else if (WebUser.ViewType == Seal.Model.ViewType.Dashboards && view == "reports") view = "dashboards";

                //Refresh widgets
                DashboardWidgetsPool.ForceReload();
                DashboardExecutions.Clear();

                return Json(new SWIUserProfile()
                {
                    name = WebUser.Name,
                    group = WebUser.SecurityGroupsDisplay,
                    culture = Repository.CultureInfo.EnglishName,
                    folder = GetCookie(SealLastFolderCookieName),
                    dashboard = GetCookie(SealLastDashboardCookieName),
                    viewtype = WebUser.ViewType,
                    lastview = view,
                    dashboardFolders = WebUser.DashboardFolders.ToArray(),
                    manageDashboards = WebUser.ManageDashboards
                });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }


        [HttpPost]
        public ActionResult SWIGetRootFolders()
        {
            WriteDebug("SWIGetRootFolders");
            try
            {
                checkSWIAuthentication();
                List<SWIFolder> result = new List<SWIFolder>();
                //Personal
                if (WebUser.PersonalFolderRight != PersonalFolderRight.None)
                {
                    var personalFolder = getFolder(SWIFolder.GetPersonalRoot());
                    fillFolder(personalFolder);
                    result.Add(personalFolder);
                }
                //Report
                var folder = getFolder("\\");
                fillFolder(folder);
                result.Add(folder);

                WebUser.Folders = result;
                return Json(result.ToArray());
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetFolders(string path)
        {
            WriteDebug("SWIGetFolders");
            try
            {
                checkSWIAuthentication();
                if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");

                var folder = getFolder(path);
                fillFolder(folder);
                return Json(folder);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetFolderDetail(string path)
        {
            WriteDebug("SWIGetFolderDetail");
            try
            {
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
                            path = folder.Combine(Path.GetFileName(newPath)),
                            name = Repository.TranslateFileName(newPath) + (FileHelper.IsSealReportFile(newPath) ? "" : Path.GetExtension(newPath)),
                            last = System.IO.File.GetLastWriteTime(newPath).ToString("G", Repository.CultureInfo),
                            isReport = FileHelper.IsSealReportFile(newPath),
                            right = folder.right
                        });
                    }
                }
                SetCookie(SealLastFolderCookieName, path);

                return Json(new SWIFolderDetail() { folder = folder, files = files.ToArray() });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWISearch(string path, string pattern)
        {
            WriteDebug("SWISearch");
            try
            {
                SWIFolder folder = getFolder(path);
                var files = new List<SWIFile>();
                path = folder.GetFullPath();
                searchFolder(folder, pattern, files);
                return Json(new SWIFolderDetail() { files = files.ToArray() });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIDeleteFolder(string path)
        {
            WriteDebug("SWIDeleteFolder");
            try
            {
                SWIFolder folder = getFolder(path);
                if (folder.manage != 2) throw new Exception("Error: no right to delete this folder");
                Directory.Delete(folder.GetFullPath());
                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWICreateFolder(string path)
        {
            WriteDebug("SWICreateFolder");
            try
            {
                SWIFolder folder = getFolder(path);
                if (folder.manage == 0) throw new Exception("Error: no right to create in this folder");
                Directory.CreateDirectory(folder.GetFullPath());
                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIRenameFolder(string source, string destination)
        {
            WriteDebug("SWIRenameFolder");
            try
            {
                SWIFolder folderSource = getFolder(source);
                SWIFolder folderDest = getFolder(destination);
                if (folderSource.manage != 2 || folderDest.manage != 2) throw new Exception("Error: no right to rename this folder");
                Directory.Move(folderSource.GetFullPath(), folderDest.GetFullPath());
                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetReportDetail(string path)
        {
            WriteDebug("SWIGetReportDetail");
            try
            {
                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");

                string newPath = getFullPath(path);
                if (!System.IO.File.Exists(newPath)) throw new Exception("Report path not found");
                Repository repository = Repository;
                Report report = Report.LoadFromFile(newPath, repository);
                SWIReportDetail result = new SWIReportDetail();
                result.views = (from i in report.Views.Where(i => i.WebExec && i.GUID != report.ViewGUID) select new SWIView() { guid = i.GUID, name = i.Name, displayName = report.TranslateViewName(i.Name) }).ToArray();
                result.outputs = ((FolderRight)folder.right >= FolderRight.ExecuteReportOuput) ? (from i in report.Outputs.Where(j => j.PublicExec || (!j.PublicExec && j.UserName == WebUser.Name)) select new SWIOutput() { guid = i.GUID, name = i.Name, displayName = report.TranslateOutputName(i.Name) }).ToArray() : new SWIOutput[] { };
                if (result.views.Length == 0 && result.outputs.Length == 0) result.views = (from i in report.Views.Where(i => i.WebExec) select new SWIView() { guid = i.GUID, name = i.Name, displayName = report.TranslateViewName(i.Name) }).ToArray();

                return Json(result);

            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }



        [HttpPost]
        public ActionResult SWIDeleteFiles(string paths)
        {
            WriteDebug("SWIDeleteFiles");
            try
            {
                checkSWIAuthentication();
                if (string.IsNullOrEmpty(paths)) throw new Exception("Error: paths must be supplied");

                foreach (var path in paths.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        SWIFolder folder = getParentFolder(path);
                        if ((FolderRight)folder.right != FolderRight.Edit) throw new Exception("Error: no right to edit in this folder");
                        string fullPath = getFullPath(path);
                        if (FileHelper.IsSealReportFile(fullPath) && FileHelper.ReportHasSchedule(fullPath))
                        {
                            //Delete schedules...
                            var report = Report.LoadFromFile(fullPath, Repository);
                            report.Schedules.Clear();
                            report.SynchronizeTasks();
                        }

                        FileHelper.DeleteSealFile(fullPath);
                    }
                }
                return Json(new object { });

            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIMoveFile(string source, string destination, bool copy)
        {
            WriteDebug("SWIMoveFile");
            try
            {
                SWIFolder folderSource = getParentFolder(source);
                if (folderSource.right == 0) throw new Exception("Error: no right on this folder");
                SWIFolder folderDest = getParentFolder(destination);
                if ((FolderRight)folderDest.right != FolderRight.Edit) throw new Exception("Error: no right to edit on the destination folder");

                string sourcePath = getFullPath(source);
                string destinationPath = getFullPath(destination);
                if (!System.IO.File.Exists(sourcePath)) throw new Exception("Error: source path is incorrect");
                if (folderDest.files && FileHelper.IsSealReportFile(sourcePath)) throw new Exception(Translate("Warning: only files (and not reports) can be copied to this folder."));
                if (System.IO.File.Exists(destinationPath) && copy) destinationPath = FileHelper.GetUniqueFileName(Path.GetDirectoryName(destinationPath), Path.GetFileNameWithoutExtension(destinationPath) + " - Copy", Path.GetExtension(destinationPath));

                bool hasSchedule = (FileHelper.IsSealReportFile(sourcePath) && FileHelper.ReportHasSchedule(sourcePath));
                FileHelper.MoveSealFile(sourcePath, destinationPath, copy);
                if (hasSchedule)
                {
                    //Re-init schedules...
                    var report = Report.LoadFromFile(destinationPath, Repository);
                    if (copy)
                    {
                        //remove schedules
                        report.Schedules.Clear();
                        report.SaveToFile();
                    }
                    report.SchedulesWithCurrentUser = false;
                    report.SynchronizeTasks();
                }
                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWExecuteReportToResult(string path, string viewGUID, string outputGUID, string format)
        {
            WriteDebug("SWExecuteReportToResult");
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");
                if (!string.IsNullOrEmpty(outputGUID) && (FolderRight)folder.right == FolderRight.Execute) throw new Exception("Error: no right to execute output on this folder");

                string filePath = getFullPath(path);
                if (!System.IO.File.Exists(filePath)) throw new Exception("Error: report does not exist");
                Repository repository = Repository.CreateFast();
                Report report = Report.LoadFromFile(filePath, repository);

                var execution = initReportExecution(report, viewGUID, outputGUID, true);
                execution.Execute();
                while (report.Status != ReportStatus.Executed && !report.HasErrors) Thread.Sleep(100);

                ActionResult result = null;
                if (!string.IsNullOrEmpty(outputGUID))
                {
                    result = getFileResult(report.ResultFilePath, report);
                }
                else
                {
                    string fileResult = "";
                    if (string.IsNullOrEmpty(format)) format = "html";
                    if (format.ToLower() == "print") fileResult = execution.GeneratePrintResult();
                    else if (format.ToLower() == "pdf") fileResult = execution.GeneratePDFResult();
                    else if (format.ToLower() == "excel") fileResult = execution.GenerateExcelResult();
                    else if (format.ToLower() == "csv") fileResult = execution.GenerateCSVResult();
                    else fileResult = execution.GenerateHTMLResult();
                    result = getFileResult(fileResult, report);
                }
                report.PreInputRestrictions.Clear();
                return result;
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }


        [HttpPost]
        public ActionResult SWExecuteReport(string path, bool? render, string viewGUID, string outputGUID)
        {
            WriteDebug("SWExecuteReport");
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                Report report = null;
                Repository repository = null;

                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");
                if (!string.IsNullOrEmpty(outputGUID) && (FolderRight)folder.right == FolderRight.Execute) throw new Exception("Error: no right to execute output on this folder");

                string filePath = getFullPath(path);
                if (!System.IO.File.Exists(filePath)) throw new Exception("Error: report does not exist");
                repository = Repository.CreateFast();
                report = Report.LoadFromFile(filePath, repository);

                var execution = initReportExecution(report, viewGUID, outputGUID, false);
                execution.RenderHTMLDisplayForViewer();
                return getFileResult(report.HTMLDisplayFilePath, report);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }


        [HttpPost]
        public ActionResult SWViewFile(string path)
        {
            WriteDebug("SWViewFile");
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");

                return getFileResult(getFullPath(path), null);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWILogout()
        {
            WriteDebug("SWILogout");
            try
            {
                if (WebUser != null) WebUser.Logout();
                CreateWebUser();
                return Json(new { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }


        [HttpPost]
        public ActionResult SWISetUserProfile(string culture, string defaultView)
        {
            WriteDebug("SWISetUserProfile");
            try
            {
                checkSWIAuthentication();
                if (string.IsNullOrEmpty(culture)) throw new Exception("Error: culture must be supplied");
                if (culture != Repository.CultureInfo.EnglishName)
                {
                    if (!Repository.SetCultureInfo(culture)) throw new Exception("Invalid culture name:" + culture);
                    WebUser.ClearCache();
                    SetCookie(SealCultureCookieName, culture);
                }

                if (!string.IsNullOrEmpty(defaultView)) SetCookie(SealLastViewCookieName, defaultView);

                return Json(new { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }


        [HttpPost]
        public ActionResult SWIGetUserProfile()
        {
            WriteDebug("SWIGetUserProfile");
            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated) return Json(new { authenticated = false });

                return Json(new
                {
                    authenticated = true,
                    name = WebUser.Name,
                    group = WebUser.SecurityGroupsDisplay,
                    culture = Repository.CultureInfo.EnglishName,
                    viewtype = WebUser.ViewType
                });
            }
            catch
            {
                //not authenticated
                return Json(new { authenticated = false });
            }
        }

        static object _culturesLock = new object();
        static List<SWIItem> _cultures = null;
        static List<SWIItem> Cultures
        {
            get
            {
                lock (_culturesLock)
                {
                    if (_cultures == null)
                    {
                        _cultures = new List<SWIItem>();
                        foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(i => i.EnglishName))
                        {
                            _cultures.Add(new SWIItem() { id = culture.EnglishName, val = culture.NativeName });
                        }
                    }
                }
                return _cultures;
            }
        }

        [HttpPost]
        public ActionResult SWIGetCultures()
        {
            WriteDebug("SWIGetCultures");
            try
            {
                checkSWIAuthentication();
                return Json(Cultures.ToArray());
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWITranslate(string context, string instance, string reference)
        {
            WriteDebug("SWITranslate");
            try
            {
                checkSWIAuthentication();
                if (!string.IsNullOrEmpty(instance)) return Json(new { text = Repository.RepositoryTranslate(context, instance, reference) });
                return Json(new { text = Repository.Translate(context, reference) });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetVersions()
        {
            WriteDebug("SWIGetVersions");
            try
            {
                return Json(new { SWIVersion = Repository.ProductVersion, SRVersion = Repository.ProductVersion, Info = Info });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        #region Dashboard

        Dashboard getDashboard(string guid)
        {
            if (string.IsNullOrEmpty(guid)) throw new Exception("Error: guid must be supplied");
            var d = WebUser.UserDashboards.FirstOrDefault(i => i.GUID == guid);
            if (d == null) throw new Exception("Error: The dashboard does not exist");

            return d;
        }

        Dashboard checkDashboardEditRight(string guid)
        {
            var d = getDashboard(guid);
            if (!d.Editable) throw new Exception("Error: no right to edit this dashboard");
            return d;
        }

        [HttpPost]
        public ActionResult SWIGetUserDashboards()
        {
            WriteDebug("SWIGetUserDashboards");
            try
            {
                checkSWIAuthentication();

                return Json(WebUser.UserDashboards.OrderBy(i => i.Order).ToArray());
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetDashboards()
        {
            WriteDebug("SWIGetDashboards");
            try
            {
                checkSWIAuthentication();

                //Public Dashboards not selected 
                return Json(WebUser.GetDashboards().Where(i => !WebUser.Profile.Dashboards.Contains(i.GUID)).OrderBy(i => i.Order).ToArray());
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetDashboardItems(string guid)
        {
            WriteDebug("SWIGetDashboardItems");
            try
            {
                checkSWIAuthentication();

                if (string.IsNullOrEmpty(guid)) throw new Exception("Error: guid must be supplied");

                var dashboard = WebUser.UserDashboards.FirstOrDefault(i => i.GUID == guid);
                if (dashboard == null) throw new Exception("Error: The dashboard does not exist");

                return Json(dashboard.Items.OrderBy(i => i.GroupOrder).ThenBy(i => i.GroupName).ThenBy(i => i.Order).ToArray());
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetDashboardItem(string guid, string itemguid)
        {
            WriteDebug("SWIGetDashboardItems");
            try
            {
                checkSWIAuthentication();

                if (string.IsNullOrEmpty(guid)) throw new Exception("Error: guid must be supplied");

                var dashboard = WebUser.UserDashboards.FirstOrDefault(i => i.GUID == guid);
                if (dashboard == null) throw new Exception("Error: The dashboard does not exist");

                var item = dashboard.Items.FirstOrDefault(i => i.GUID == itemguid);
                if (item == null) throw new Exception("Error: The dashboard item does not exist");

                return Json(item);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        List<ReportExecution> DashboardExecutions
        {
            get
            {
                List<ReportExecution> result = (List<ReportExecution>)Session[SessionDashboardExecutions];
                if (result == null)
                {
                    result = new List<ReportExecution>();
                    Session[SessionDashboardExecutions] = result;
                }
                return result;
            }
        }

        [HttpPost]
        public ActionResult SWIAddDashboard(string[] guids)
        {
            WriteDebug("SWIAddDashboard");
            try
            {
                checkSWIAuthentication();

                if (!CheckAuthentication()) return Content(_loginContent);

                if (!WebUser.ManageDashboards) throw new Exception("No right to add dashboards");

                foreach (var guid in guids) WebUser.Profile.Dashboards.Add(guid);
                WebUser.SaveProfile();

                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIRemoveDashboard(string guid)
        {
            WriteDebug("SWIRemoveDashboard");
            try
            {
                checkSWIAuthentication();

                if (!CheckAuthentication()) return Content(_loginContent);

                if (!WebUser.ManageDashboards) throw new Exception("No right to remove dashboard");

                if (WebUser.Profile.Dashboards.Contains(guid)) WebUser.Profile.Dashboards.Remove(guid);
                WebUser.SaveProfile();

                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWISwapDashboardOrder(string guid1, string guid2)
        {
            WriteDebug("SWISwapDashboardOrder");
            try
            {
                checkSWIAuthentication();

                if (!WebUser.ManageDashboards) throw new Exception("No right to swap  dashboard");

                if (WebUser.Profile.Dashboards.Contains(guid1) && WebUser.Profile.Dashboards.Contains(guid2))
                {
                    var newDashboards = new List<string>();
                    foreach (var guid in WebUser.Profile.Dashboards)
                    {
                        if (guid == guid1) newDashboards.Add(guid2);
                        else if (guid == guid2) newDashboards.Add(guid1);
                        else newDashboards.Add(guid);
                    }
                    WebUser.Profile.Dashboards = newDashboards;
                    WebUser.SaveProfile();
                }
                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }


        [HttpPost]
        public ActionResult SWISetLastDashboard(string guid)
        {
            WriteDebug("SWISetLastDashboard");
            try
            {
                checkSWIAuthentication();
                SetCookie(SealLastDashboardCookieName, guid);
                return Json(new object { });

            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetDashboardResult(string guid, string itemguid, bool force)
        {
            WriteDebug("SWIGetDashboardResult");
            try
            {
                checkSWIAuthentication();

                if (!CheckAuthentication()) return Content(_loginContent);

                Report report = null;
                ReportExecution execution = null;
                Repository repository = null;

                var dashboard = WebUser.UserDashboards.FirstOrDefault(i => i.GUID == guid);
                if (dashboard == null)
                {
                    return Json(new object { });
                }

                var item = dashboard.Items.FirstOrDefault(i => i.GUID == itemguid);
                if (item == null) throw new Exception("Error: The item does not exist");

                var widget = DashboardWidgetsPool.Widgets.ContainsKey(item.WidgetGUID) ? DashboardWidgetsPool.Widgets[item.WidgetGUID] : null;
                if (widget == null) throw new Exception("Error: the widget does not exist");

                /* No check on exec rights for widgets...
                SWIFolder folder = getParentFolder(widget.ReportPath);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");
                */
                var filePath = Repository.ReportsFolder + widget.ReportPath;
                if (!System.IO.File.Exists(filePath)) throw new Exception("Error: the report does not exist");

                var executions = DashboardExecutions;
                lock (executions)
                {
                    //remove executions older than 2 hours
                    executions.RemoveAll(i => i.Report.ExecutionEndDate < DateTime.Now.AddHours(-2));

                    execution = executions.FirstOrDefault(i => i.Report.FilePath == filePath);
                    if (execution != null && execution.Report.LastModification != System.IO.File.GetLastWriteTime(filePath))
                    {
                        executions.RemoveAll(i => i.Report.FilePath == filePath);
                        execution = null;
                    }
                }

                if (execution != null)
                {
                    report = execution.Report;
                }
                else
                {
                    repository = Repository.CreateFast();
                    report = Report.LoadFromFile(filePath, repository);

                    report.ExecutionContext = ReportExecutionContext.WebReport;
                    //Disable basics
                    report.ExecutionView.InitParameters(false);
                    report.ExecutionView.SetParameter(Parameter.DrillEnabledParameter, false);
                    report.ExecutionView.SetParameter(Parameter.SubReportsEnabledParameter, false);
                    report.ExecutionView.SetParameter(Parameter.ServerPaginationParameter, false);
                    //set HTML Format
                    report.ExecutionView.SetParameter(Parameter.ReportFormatParameter, ReportFormat.html.ToString());
                    //Force load of all models
                    report.ExecutionView.SetParameter(Parameter.ForceModelsLoad, true);
                }

                string content = "";
                ReportView view = null, modelView = null;
                report.GetWidgetViewToParse(report.Views, widget.GUID, ref view, ref modelView);
                var rootAutoRefresh = 0;

                if (view == null) throw new Exception("Error: the widget does not exist");

                //Init parameters if the root view is different from the one executed...
                var rootView = report.GetRootView(view);
                if (rootView != null && rootView != report.ExecutionView)
                {
                    string templateErrors = "";
                    rootView.InitTemplates(rootView, ref templateErrors);
                    rootAutoRefresh = rootView.GetNumericValue("refresh_rate");
                }
                else rootAutoRefresh = report.ExecutionView.GetNumericValue("refresh_rate");

                if (execution != null)
                {
                    if (!report.IsExecuting && (force || report.ExecutionEndDate < DateTime.Now.AddSeconds(-1 * report.WidgetCache)))
                    {
                        execution.Execute();
                        while (report.IsExecuting) Thread.Sleep(100);
                    }
                }
                else
                {
                    execution = new ReportExecution() { Report = report };
                    lock (executions)
                    {
                        executions.Add(execution);
                    }
                    execution.Execute();
                    while (report.IsExecuting) Thread.Sleep(100);
                }
                if (report.HasErrors)
                {
                    Helper.WriteWebException(new Exception(report.FilePath + ":\r\n" + report.ExecutionErrors), Request, WebUser);
                    throw new Exception("Error: the model has errors");
                }
                //Reset pointers and parse
                lock (report)
                {
                    report.CurrentModelView = modelView;
                    if (modelView != null && modelView.Model != null && modelView.Model.Pages.Count > 0)
                    {
                        report.CurrentPage = modelView.Model.Pages[0];
                        report.CurrentPage.PageId = null; //Reset page id
                    }
                    content = view.Parse();
                }

                var result = new
                {
                    dashboardguid = guid,
                    itemguid = itemguid,
                    path = widget.Exec ? widget.ReportPath : "",
                    lastexec = Translate("Last execution at") + " " + report.ExecutionEndDate.ToString("G", Repository.CultureInfo),
                    description = Repository.TranslateWidgetDescription(widget.ReportPath.Replace(Repository.ReportsFolder, "\\"), widget.Description),
                    dynamic = item.Dynamic,
                    content = content,
                    refresh = (item.Refresh == -1 ? rootAutoRefresh : item.Refresh)
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                var result = new
                {
                    dashboardguid = guid,
                    itemguid = itemguid,
                    content = "<b>" + Translate("This Widget has an error. Please consider to remove it from your Dashboard...") + "</b><br><br>" + Helper.ToHtml(ex.Message)
                };

                return Json(result);
            }
        }

        #endregion
    }
}