//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
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
using System.Text;

namespace SealWebServer.Controllers
{
    public partial class HomeController : Controller
    {
        /// <summary>
        /// Start a session with the Web Report Server using the user name, password, token (may be optional according to the authentication configured on the server) and returns information of the logged user (SWIUserProfile).
        /// </summary>
        public ActionResult SWILogin(string user, string password, string token)
        {
            writeDebug("SWILogin");

            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated || (!string.IsNullOrEmpty(user) && WebUser.WebUserName != user) || (!string.IsNullOrEmpty(token) && WebUser.Token != token))
                {
                    CreateRepository();
                    CreateWebUser();
                    WebUser.WebPrincipal = User;
                    WebUser.WebUserName = user;
                    WebUser.WebPassword = password;
                    WebUser.Token = token;
                    WebUser.Request = Request;
                    Authenticate();

                    if (!WebUser.IsAuthenticated) throw new LoginException(string.IsNullOrEmpty(WebUser.Error) ? Translate("Invalid user name or password") : WebUser.Error);
                }

                //Audit
                Audit.LogAudit(AuditType.Login, WebUser);
                Audit.LogEventAudit(AuditType.EventLoggedUsers, SealSecurity.LoggedUsers.Count(i => i.IsAuthenticated).ToString());

                //Set culture from cookie
                string culture = getCookie(SealCultureCookieName);
                if (!string.IsNullOrEmpty(culture)) Repository.SetCultureInfo(culture);

                //Set default view
                string view = getCookie(SealLastViewCookieName);
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
                    folder = getCookie(SealLastFolderCookieName),
                    dashboard = getCookie(SealLastDashboardCookieName),
                    viewtype = WebUser.ViewType,
                    lastview = view,
                    dashboardfolders = WebUser.DashboardFolders.ToArray(),
                    managedashboards = WebUser.ManageDashboards,
                    usertag = WebUser.Tag
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        void addValidFolders(SWIFolder folder, List<SWIFolder> result)
        {
            if (folder.right == 0)
            {
                //Add only folder with rights
                foreach (var childFolder in folder.folders)
                {
                    addValidFolders(childFolder, result);
                }
            }
            else
            {
                result.Add(folder);
            }
        }

        /// <summary>
        /// Returns all the folders of the user (including Personal folders).
        /// </summary>
        public ActionResult SWIGetRootFolders()
        {
            writeDebug("SWIGetRootFolders");
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
                var folder = getFolder(Path.DirectorySeparatorChar.ToString());
                fillFolder(folder);
                if (WebUser.ShowAllFolders)
                {
                    result.Add(folder);
                }
                else
                {
                    addValidFolders(folder, result);
                }

                //Folders Script
                WebUser.Folders = result;
                WebUser.ScriptNumber = 1;
                foreach (var group in WebUser.SecurityGroups.Where(i => !string.IsNullOrEmpty(i.FoldersScript)).OrderBy(i => i.Name))
                {
                    RazorHelper.CompileExecute(group.FoldersScript, WebUser);
                    WebUser.ScriptNumber++;

                }
                return Json(WebUser.Folders.ToArray(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Returns the list of file names and details contained in a folder.
        /// </summary>
        public ActionResult SWIGetFolderDetail(string path)
        {
            writeDebug("SWIGetFolderDetail");
            try
            {
                var folderDetail = getFolderDetail(path, true);
                setCookie(SealLastFolderCookieName, path);
                return Json(folderDetail, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Returns the list of file names and details matching a search in the repository.
        /// </summary>
        public ActionResult SWISearch(string path, string pattern)
        {
            writeDebug("SWISearch");
            try
            {
                SWIFolder folder = getFolder(path);
                var files = new List<SWIFile>();
                path = folder.GetFullPath();
                searchFolder(folder, pattern, files);

                return Json(new SWIFolderDetail() { files = files.ToArray() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Delete a sub-folder in the repository. The folder must be empty.
        /// </summary>
        public ActionResult SWIDeleteFolder(string path)
        {
            writeDebug("SWIDeleteFolder");
            try
            {
                SWIFolder folder = getFolder(path);
                if (folder.manage != 2) throw new Exception("Error: no right to delete this folder");
                Directory.Delete(folder.GetFullPath());
                Audit.LogAudit(AuditType.FolderDelete, WebUser, folder.GetFullPath());
                return Json(new object { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Create a sub-folder in the repository.
        /// </summary>
        public ActionResult SWICreateFolder(string path)
        {
            writeDebug("SWICreateFolder");
            try
            {
                SWIFolder folder = getFolder(path);
                if (folder.manage == 0) throw new Exception("Error: no right to create in this folder");
                Directory.CreateDirectory(folder.GetFullPath());
                Audit.LogAudit(AuditType.FolderCreate, WebUser, folder.GetFullPath());
                return Json(new object { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Rename a sub-folder in the repository.
        /// </summary>
        public ActionResult SWIRenameFolder(string source, string destination)
        {
            writeDebug("SWIRenameFolder");
            try
            {
                SWIFolder folderSource = getFolder(source);
                SWIFolder folderDest = getFolder(destination);
                if (folderSource.manage != 2 || folderDest.manage != 2) throw new Exception("Error: no right to rename this folder");
                Directory.Move(folderSource.GetFullPath(), folderDest.GetFullPath());
                Audit.LogAudit(AuditType.FolderRename, WebUser, folderSource.GetFullPath(), string.Format("Rename to '{0}'", folderDest.GetFullPath()));
                return Json(new object { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Returns the views and outputs of a report.
        /// </summary>
        public ActionResult SWIGetReportDetail(string path)
        {
            writeDebug("SWIGetReportDetail");
            try
            {
                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");

                var file = getFileDetail(path);
                if (file.right == 0) throw new Exception("Error: no right on this report or file");

                string newPath = getFullPath(path);
                if (!System.IO.File.Exists(newPath)) throw new Exception("Report path not found");
                Repository repository = Repository;
                Report report = Report.LoadFromFile(newPath, repository, false);
                SWIReportDetail result = new SWIReportDetail();
                result.views = (from i in report.Views.Where(i => i.WebExec && i.GUID != report.ViewGUID) select new SWIView() { guid = i.GUID, name = i.Name, displayname = report.TranslateViewName(i.Name) }).ToArray();
                result.outputs = ((FolderRight)folder.right >= FolderRight.ExecuteReportOuput) ? (from i in report.Outputs.Where(j => j.PublicExec || string.IsNullOrEmpty(j.UserName) || (!j.PublicExec && j.UserName == WebUser.Name)) select new SWIOutput() { guid = i.GUID, name = i.Name, displayname = report.TranslateOutputName(i.Name) }).ToArray() : new SWIOutput[] { };
                if (result.views.Length == 0 && result.outputs.Length == 0) result.views = (from i in report.Views.Where(i => i.WebExec) select new SWIView() { guid = i.GUID, name = i.Name, displayname = report.TranslateViewName(i.Name) }).ToArray();

                return Json(result, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Delete files or reports from the repository.
        /// </summary>
        public ActionResult SWIDeleteFiles(string paths)
        {
            writeDebug("SWIDeleteFiles");
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

                        var file = getFileDetail(path);
                        if ((FolderRight)file.right != FolderRight.Edit) throw new Exception("Error: no right to edit this report or file");

                        string fullPath = getFullPath(path);
                        if (FileHelper.IsSealReportFile(fullPath) && FileHelper.ReportHasSchedule(fullPath))
                        {
                            //Delete schedules...
                            var report = Report.LoadFromFile(fullPath, Repository, false);
                            report.Schedules.Clear();
                            report.SynchronizeTasks();
                        }

                        FileHelper.DeleteSealFile(fullPath);

                        Audit.LogAudit(AuditType.FileDelete, WebUser, path);
                    }
                }
                return Json(new object { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Move a file or a report in the repository.
        /// </summary>
        public ActionResult SWIMoveFile(string source, string destination, bool copy)
        {
            writeDebug("SWIMoveFile");
            try
            {
                SWIFolder folderSource = getParentFolder(source);
                if (folderSource.right == 0) throw new Exception("Error: no right on this folder");
                if (!copy && (FolderRight)folderSource.right != FolderRight.Edit) throw new Exception("Error: no edit right on this folder");

                var file = getFileDetail(source);
                if (file.right == 0) throw new Exception("Error: no right on this report or file");
                if (!copy && (FolderRight)file.right != FolderRight.Edit) throw new Exception("Error: no right to edit this report or file");

                SWIFolder folderDest = getParentFolder(destination);
                if ((FolderRight)folderDest.right != FolderRight.Edit) throw new Exception("Error: no right to edit on the destination folder");


                string sourcePath = getFullPath(source);
                string destinationPath = getFullPath(destination);
                if (!System.IO.File.Exists(sourcePath)) throw new Exception("Error: source path is incorrect");
                if (folderDest.files && FileHelper.IsSealReportFile(sourcePath)) throw new Exception(Translate("Warning: only files (and not reports) can be copied to this folder."));
                if (System.IO.File.Exists(destinationPath) && copy) destinationPath = FileHelper.GetUniqueFileName(Path.GetDirectoryName(destinationPath), Path.GetFileNameWithoutExtension(destinationPath) + " - Copy" + Path.GetExtension(destinationPath), Path.GetExtension(destinationPath));

                bool hasSchedule = (FileHelper.IsSealReportFile(sourcePath) && FileHelper.ReportHasSchedule(sourcePath));
                FileHelper.MoveSealFile(sourcePath, destinationPath, copy);
                if (copy) Audit.LogAudit(AuditType.FileCopy, WebUser, sourcePath, string.Format("Copy to '{0}'", destinationPath));
                else Audit.LogAudit(AuditType.FileMove, WebUser, sourcePath, string.Format("Move to '{0}'", destinationPath));
                if (hasSchedule)
                {
                    //Re-init schedules...
                    var report = Report.LoadFromFile(destinationPath, Repository, false);
                    if (copy)
                    {
                        //remove schedules
                        report.InitGUIDAndSchedules();
                        report.SaveToFile();
                    }
                    report.SchedulesWithCurrentUser = false;
                    report.SynchronizeTasks();
                }
                return Json(new object { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Execute a report into a report result and returns the result. Check API of Seal Web Interface for more information.
        /// </summary>
        public ActionResult SWExecuteReportToResult(string path, string viewGUID, string outputGUID, string format)
        {
            writeDebug("SWExecuteReportToResult");
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");
                if (!string.IsNullOrEmpty(outputGUID) && (FolderRight)folder.right == FolderRight.Execute) throw new Exception("Error: no right to execute output on this folder");

                var file = getFileDetail(path);
                if (file.right == 0) throw new Exception("Error: no right on this report or file");
                if (!string.IsNullOrEmpty(outputGUID) && (FolderRight)file.right == FolderRight.Execute) throw new Exception("Error: no right to execute output on this report");

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

        /// <summary>
        /// Execute a report and returns the report html display result content (e.g. html with prompted restrictions). Check API of Seal Web Interface for more information.
        /// </summary>
        public ActionResult SWExecuteReport(string path, bool? render, string viewGUID, string outputGUID)
        {
            writeDebug("SWExecuteReport");
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                Report report = null;
                Repository repository = null;

                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");
                if (!string.IsNullOrEmpty(outputGUID) && (FolderRight)folder.right == FolderRight.Execute) throw new Exception("Error: no right to execute output on this folder");

                var file = getFileDetail(path);
                if (file.right == 0) throw new Exception("Error: no right on this report or file");
                if (!string.IsNullOrEmpty(outputGUID) && (FolderRight)file.right == FolderRight.Execute) throw new Exception("Error: no right to execute output on this report");

                string filePath = getFullPath(path);
                if (!System.IO.File.Exists(filePath)) throw new Exception("Error: report or file does not exist");
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

        /// <summary>
        /// View a file published in the repository.
        /// </summary>
        public ActionResult SWViewFile(string path)
        {
            writeDebug("SWViewFile");
            try
            {
                if (!CheckAuthentication()) return Content(_loginContent);

                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");

                var file = getFileDetail(path);
                if (file.right == 0) throw new Exception("Error: no right on this report or file");

                return getFileResult(getFullPath(path), null);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        /// <summary>
        /// Clear the current user session.
        /// </summary>
        public ActionResult SWILogout()
        {
            writeDebug("SWILogout");

            try
            {
                if (WebUser != null) WebUser.Logout();
                //Audit
                Audit.LogAudit(AuditType.Logout, WebUser);
                Audit.LogEventAudit(AuditType.EventLoggedUsers, SealSecurity.LoggedUsers.Count(i => i.IsAuthenticated).ToString());
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Set the culture and the default view (reports or dashboards) for the logged user.
        /// </summary>
        public ActionResult SWISetUserProfile(string culture, string defaultView)
        {
            writeDebug("SWISetUserProfile");
            try
            {
                checkSWIAuthentication();
                if (string.IsNullOrEmpty(culture)) throw new Exception("Error: culture must be supplied");
                if (culture != Repository.CultureInfo.EnglishName)
                {
                    if (!Repository.SetCultureInfo(culture)) throw new Exception("Invalid culture name:" + culture);
                    WebUser.ClearCache();
                    setCookie(SealCultureCookieName, culture);
                }

                if (!string.IsNullOrEmpty(defaultView)) setCookie(SealLastViewCookieName, defaultView);

                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Returns the profile information of the logged user.
        /// </summary>
        public ActionResult SWIGetUserProfile()
        {
            writeDebug("SWIGetUserProfile");
            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated) return Json(new { authenticated = false });

                return Json(new
                {
                    authenticated = true,
                    name = WebUser.Name,
                    group = WebUser.SecurityGroupsDisplay,
                    culture = Repository.CultureInfo.EnglishName,
                    viewtype = WebUser.ViewType,
                    managedashboards = WebUser.ManageDashboards
                });
            }
            catch
            {
                //not authenticated
                return Json(new { authenticated = false }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Return the list of Cultures available
        /// </summary>
        /// <returns></returns>
        public ActionResult SWIGetCultures()
        {
            writeDebug("SWIGetCultures");
            try
            {
                checkSWIAuthentication();

                var cultures = Repository.Configuration.WebCultures;
                if (cultures.Count == 0) cultures = Repository.GetInstalledTranslationCultures();
                var result = new List<SWIItem>();
                foreach (var culture in cultures)
                {
                    var cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(i => i.EnglishName == culture);
                    if (cultureInfo == null) cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(i => i.Name == culture);
                    if (cultureInfo != null) result.Add(new SWIItem() { id = cultureInfo.EnglishName, val = string.Format("{0} - {1}", cultureInfo.NativeName, cultureInfo.EnglishName) });
                }
                return Json(result.OrderBy(i => i.val).ToArray(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Translate a text either from the public translations or the repository translations. If the optional parameter instance is not empty, the repository translations are used.
        /// </summary>
        public ActionResult SWITranslate(string context, string instance, string reference)
        {
            writeDebug("SWITranslate");
            try
            {
                checkSWIAuthentication();
                if (!string.IsNullOrEmpty(instance)) return Json(new { text = Repository.RepositoryTranslate(context, instance, reference) });
                return Json(new { text = Repository.Translate(context, reference) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Returns the version of the Seal Web Interface and the version of the Seal Library.
        /// </summary>
        public ActionResult SWIGetVersions()
        {
            writeDebug("SWIGetVersions");
            try
            {
                return Json(new { SWIVersion = Repository.ProductVersion, SRVersion = Repository.ProductVersion, Info = Info }, JsonRequestBehavior.AllowGet);
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

        /// <summary>
        /// Return the dashboards in the current view of the logged user
        /// </summary>
        public ActionResult SWIGetUserDashboards()
        {
            writeDebug("SWIGetUserDashboards");
            try
            {
                checkSWIAuthentication();

                return Json(WebUser.UserDashboards.OrderBy(i => i.Order).ToArray(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Return the dashboards available for the logged user
        /// </summary>
        public ActionResult SWIGetDashboards()
        {
            writeDebug("SWIGetDashboards");
            try
            {
                checkSWIAuthentication();

                //Public Dashboards not selected 
                return Json(WebUser.GetDashboards().Where(i => !WebUser.Profile.Dashboards.Contains(i.GUID)).OrderBy(i => i.Order).ToArray(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Return the list of dashboard items for a dashboard
        /// </summary>

        public ActionResult SWIGetDashboardItems(string guid)
        {
            writeDebug("SWIGetDashboardItems");
            try
            {
                checkSWIAuthentication();

                if (string.IsNullOrEmpty(guid)) throw new Exception("Error: guid must be supplied");

                var dashboard = WebUser.UserDashboards.FirstOrDefault(i => i.GUID == guid);
                if (dashboard == null) throw new Exception("Error: The dashboard does not exist");

                foreach (var item in dashboard.Items) item.JSonSerialization = true;
                return Json(dashboard.Items.OrderBy(i => i.GroupOrder).ThenBy(i => i.GroupName).ThenBy(i => i.Order).ToArray(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Return a dashboard item
        /// </summary>
        public ActionResult SWIGetDashboardItem(string guid, string itemguid)
        {
            writeDebug("SWIGetDashboardItems");
            try
            {
                checkSWIAuthentication();

                if (string.IsNullOrEmpty(guid)) throw new Exception("Error: guid must be supplied");

                var dashboard = WebUser.UserDashboards.FirstOrDefault(i => i.GUID == guid);
                if (dashboard == null) throw new Exception("Error: The dashboard does not exist");

                var item = dashboard.Items.FirstOrDefault(i => i.GUID == itemguid);
                if (item == null) throw new Exception("Error: The dashboard item does not exist");

                return Json(item, JsonRequestBehavior.AllowGet);
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
                List<ReportExecution> result = (List<ReportExecution>)getSessionValue(SessionDashboardExecutions);
                if (result == null)
                {
                    result = new List<ReportExecution>();
                    setSessionValue(SessionDashboardExecutions, result);
                }
                return result;
            }
        }

        /// <summary>
        /// Add Dashboards to the current logged user
        /// </summary>
        public ActionResult SWIAddDashboard(string[] guids)
        {
            writeDebug("SWIAddDashboard");
            try
            {
                checkSWIAuthentication();

                if (!CheckAuthentication()) return Content(_loginContent);

                if (!WebUser.ManageDashboards) throw new Exception("No right to add dashboards");

                if (guids != null)
                {
                    foreach (var guid in guids) WebUser.Profile.Dashboards.Add(guid);
                    WebUser.SaveProfile();
                }

                return Json(new object { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Remove the dashboard from the logged user view
        /// </summary>
        public ActionResult SWIRemoveDashboard(string guid)
        {
            writeDebug("SWIRemoveDashboard");
            try
            {
                checkSWIAuthentication();

                if (!CheckAuthentication()) return Content(_loginContent);

                if (!WebUser.ManageDashboards) throw new Exception("No right to remove dashboard");

                if (WebUser.Profile.Dashboards.Contains(guid))
                {
                    WebUser.Profile.Dashboards.Remove(guid);
                    WebUser.SaveProfile();
                }

                return Json(new object { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Change the order between two dashboards in the current logged user view
        /// </summary>
        public ActionResult SWISwapDashboardOrder(string guid1, string guid2)
        {
            writeDebug("SWISwapDashboardOrder");
            try
            {
                checkSWIAuthentication();

                if (!WebUser.ManageDashboards) throw new Exception("No right to swap dashboard");

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
                return Json(new object { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Set the last dashboard viewed by the logged user
        /// </summary>
        public ActionResult SWISetLastDashboard(string guid)
        {
            writeDebug("SWISetLastDashboard");
            try
            {
                checkSWIAuthentication();
                setCookie(SealLastDashboardCookieName, guid);
                return Json(new object { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        ReportExecution getWidgetViews(DashboardWidget widget, out Report report, ref ReportView view, ref ReportView modelView)
        {
            ReportExecution execution = null;
            var filePath = Repository.ReportsFolder + widget.ReportPath;
            if (!System.IO.File.Exists(filePath)) throw new Exception("Error: the report does not exist");

            var executions = DashboardExecutions;
            lock (executions)
            {
                //remove executions older than 2 hours
                executions.RemoveAll(i => i.Report.ExecutionEndDate != DateTime.MinValue && i.Report.ExecutionEndDate < DateTime.Now.AddHours(-2));
                //check if report has been modified
                var lastDateTime = System.IO.File.GetLastWriteTime(filePath);
                executions.RemoveAll(i => i.Report.FilePath == filePath && i.Report.LastModification != lastDateTime);

                //find existing execution
                foreach (var exec in executions.Where(i => i.Report.FilePath == filePath))
                {
                    exec.Report.GetWidgetViewToParse(exec.Report.ExecutionView.Views, widget.GUID, ref view, ref modelView);
                    if (view != null)
                    {
                        execution = exec;
                        break;
                    }
                }

                if (execution == null)
                {
                    //create execution
                    var repository = Repository.CreateFast();
                    report = Report.LoadFromFile(filePath, repository);

                    report.ExecutionContext = ReportExecutionContext.WebReport;
                    report.SecurityContext = WebUser;
                    //Set url
                    report.WebUrl = GetWebUrl(Request, Response);

                    execution = new ReportExecution() { Report = report };
                    executions.Add(execution);
                }
                else
                {
                    report = execution.Report;
                }

                if (view == null)
                {
                    report.GetWidgetViewToParse(report.Views, widget.GUID, ref view, ref modelView);
                }

                if (view == null) throw new Exception("Error: the widget does not exist");

                //Set execution view from the new root...
                report.CurrentViewGUID = report.GetRootView(view).GUID;
            }

            return execution;
        }

        /// <summary>
        /// Return the result of a dashboard item
        /// </summary>
        public ActionResult SWIGetDashboardResult(string guid, string itemguid, bool force, string format)
        {
            writeDebug("SWIGetDashboardResult");
            try
            {
                checkSWIAuthentication();

                if (!CheckAuthentication()) return Content(_loginContent);

                var dashboard = WebUser.UserDashboards.FirstOrDefault(i => i.GUID == guid);
                if (dashboard == null)
                {
                    return Json(new object { }, JsonRequestBehavior.AllowGet);
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

                ReportView view = null, modelView = null;
                Report report = null;
                ReportExecution execution = getWidgetViews(widget, out report, ref view, ref modelView);

                //Execute if necessary
                lock (execution)
                {
                    if (!report.IsExecuting && (force || report.ExecutionEndDate == DateTime.MinValue || report.ExecutionEndDate < DateTime.Now.AddSeconds(-1 * report.WidgetCache)))
                    {
                        //Disable basics
                        report.ExecutionView.InitParameters(false);
                        //Set HTML Format
                        report.ExecutionView.SetParameter(Parameter.ReportFormatParameter, ReportFormat.html.ToString());
                        execution.Execute();
                    }
                    while (report.IsExecuting) Thread.Sleep(100);
                }

                if (report.HasErrors)
                {
                    throw new Exception(report.Translate("This report has execution errors. Please check details in the Repository Logs Files or in the Event Viewer..."));
                }

                //Reset pointers and parse
                string content = "";
                lock (execution)
                {
                    try
                    {
                        report.Status = ReportStatus.RenderingDisplay;
                        if (!string.IsNullOrEmpty(format) && format == "htmlprint")
                        {
                            //Only html print is supported
                            report.Format = ReportFormat.print;
                            report.Status = ReportStatus.RenderingResult;
                            report.ExecutionViewResultFormat = ReportFormat.print.ToString();
                        }

                        report.CurrentModelView = modelView;
                        if (modelView != null && modelView.Model != null && modelView.Model.Pages.Count > 0)
                        {
                            report.CurrentPage = modelView.Model.Pages[0];
                        }
                        content = view.Parse();
                    }
                    finally
                    {
                        report.Status = ReportStatus.Executed;
                    }
                }

                //Set context for navigation, remove previous, keep root
                var keys = NavigationContext.Navigations.Where(i => i.Value.Execution.RootReport.ExecutionGUID == report.ExecutionGUID && i.Value.Execution.RootReport != i.Value.Execution.Report).ToArray();
                foreach (var key in keys) NavigationContext.Navigations.Remove(key.Key);
                NavigationContext.SetNavigation(execution);

                var execReportPath = widget.ExecReportPath;
                if (string.IsNullOrEmpty(execReportPath)) execReportPath = widget.ReportPath;

                var result = new
                {
                    dashboardguid = guid,
                    itemguid,
                    executionguid = execution.Report.ExecutionGUID,
                    path = !string.IsNullOrEmpty(widget.ExecViewGUID) ? execReportPath : "",
                    viewGUID = widget.ExecViewGUID,
                    lastexec = Translate("Last execution at") + " " + report.ExecutionEndDate.ToString("G", Repository.CultureInfo),
                    description = Repository.TranslateWidgetDescription(widget.ReportPath.Replace(Repository.ReportsFolder, Path.DirectorySeparatorChar.ToString()), widget.Description),
                    dynamic = item.Dynamic,
                    content,
                    refresh = (item.Refresh == -1 ? report.ExecutionView.GetNumericValue("refresh_rate") : item.Refresh)
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


        #region Dashboards Export

        static Dictionary<string, MainModel> _pdfToExport = new Dictionary<string, MainModel>();
        MainModel getHtmlMainModel(string dashboards)
        {
            var model = new MainModel() { Repository = Repository, Format = "htmlprint", DashboardIds = dashboards };

#if NETCOREAPP
                    model.ServerPath = WebRootPath;
                    model.BaseURL = Request.PathBase.Value;
#else
            model.ServerPath = Request.PhysicalApplicationPath;
            model.BaseURL = Request.ApplicationPath;
#endif
            return model;
        }


        /// <summary>
        /// Export the dashboard into a file: PDF, XLSX
        /// </summary>
        public ActionResult SWExportDashboards(string dashboards, string format)
        {
            writeDebug("SWExportDashboards");
            ActionResult result = null;
            try
            {
                if (!CheckAuthentication()) return _loginContentResult;

                var model = getHtmlMainModel(dashboards);
                if (!string.IsNullOrEmpty(format) && dashboards != null && dashboards.Length > 0)
                {
                    if (format == "pdf" || format == "pdflandscape")
                    {
                        var reference = Helper.NewGUID();
                        lock (_pdfToExport)
                        {
                            model.Tag = WebUser;
                            _pdfToExport.Add(reference, model);
                        }
                        var pdfConverter = Repository.Configuration.DashboardPdfConverter;
                        pdfConverter.SourceFormat = format;
                        string destinationPath = FileHelper.GetUniqueFileName(Path.Combine(FileHelper.TempApplicationDirectory, "Dashboard.pdf"));
#if NETCOREAPP
                    var uri =  Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(Request);
#else
                        var uri = Request.Url.AbsoluteUri;
                        model.BaseURL = Request.ApplicationPath;
#endif
                        pdfConverter.ConvertHTMLToPDF(uri + "2?reference=" + reference, destinationPath);
                        return getFileResult(destinationPath, null);
                    }
                    else if (format == "excel")
                    {
                        var dashboardsToExport = new Dictionary<Dashboard, List<ReportView>>();
                        var ids = dashboards.Split(',');
                        foreach (var dashboard in WebUser.UserDashboards.Where(i => ids.Contains(i.GUID)).OrderBy(i => i.Order))
                        {
                            var views = new List<ReportView>();
                            foreach (var item in dashboard.Items.OrderBy(i => i.GroupOrder).ThenBy(i => i.GroupName).ThenBy(i => i.Order))
                            {
                                if (item.Widget != null)
                                {
                                    ReportView view = null, modelView = null;
                                    Report report = null;
                                    ReportExecution execution = getWidgetViews(item.Widget, out report, ref view, ref modelView);
                                    if (report.Cancel) break;
                                    if (modelView != null) 
                                    {
                                        views.Add(modelView);
                                        modelView.Tag = item;
                                    }
                                    else if (view != null)
                                    {
                                        views.Add(view);
                                        view.Tag = item;
                                    }
                                }
                            }
                            dashboardsToExport.Add(dashboard, views);
                        }

                        if (dashboardsToExport.Count == 0) throw new Exception("No dashboard to export...");

                        var excelConverter = Repository.Configuration.DashboardExcelConverter;
                        excelConverter.Dashboards = dashboardsToExport;
                        string path = FileHelper.GetUniqueFileName(Path.Combine(FileHelper.TempApplicationDirectory, "Dashboard.xlsx"));
                        excelConverter.ConvertToExcel(path);
                        return getFileResult(path, null);
                    }

                    result = View("Main", model);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return result;
        }

        /// <summary>
        /// Generate the dashboard in HTML Print for PDF conversion
        /// </summary>
        public ActionResult SWExportDashboards2(string reference)
        {
            writeDebug("SWExportDashboards2");
            ActionResult result = new EmptyResult();
            try
            {
                MainModel model = null;
                lock (_pdfToExport)
                {
                    if (_pdfToExport.ContainsKey(reference))
                    {
                        model = _pdfToExport[reference];
                        _pdfToExport.Remove(reference);
                    }
                }

                if (model != null)
                {

                    setSessionValue(SessionRepository, model.Repository);
                    setSessionValue(SessionUser, model.Tag);
                    if (!CheckAuthentication()) return _loginContentResult;

                    result = View("Main", model);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            return result;
        }
        #endregion
    }
}
