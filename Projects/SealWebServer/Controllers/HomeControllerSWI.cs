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
    /// <summary>
    /// Main Controller of the Web Report Server
    /// </summary>
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
                bool newAuthentication = false;
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
                    //Load profile
                    if (System.IO.File.Exists(WebUser.ProfilePath)) WebUser.Profile = SecurityUserProfile.LoadFromFile(WebUser.ProfilePath);
                    checkRecentFiles();
                    WebUser.Profile.Path = WebUser.ProfilePath;
                    newAuthentication = true;
                }

                //Audit
                Audit.LogAudit(AuditType.Login, WebUser);
                Audit.LogEventAudit(AuditType.EventLoggedUsers, SealSecurity.LoggedUsers.Count(i => i.IsAuthenticated).ToString());

                //Set culture
                string culture = WebUser.Profile.Culture;
                if (string.IsNullOrEmpty(culture) || string.IsNullOrEmpty(WebUser.WebUserName)) culture = getCookie(SealCultureCookieName);
                if (!string.IsNullOrEmpty(culture)) Repository.SetCultureInfo(culture);

                //Set default view
                string view = getCookie(SealLastViewCookieName);
                if (string.IsNullOrEmpty(view)) view = "folders";
                //Check rights
                if (!WebUser.ShowFoldersView && view == "folders") view = "report";

                //Refresh menu reports
                if (newAuthentication) MenuReportViewsPool.ForceReload();
                
                //Check last report
                var lastReportPath = getCookie(SealLastReportPathCookieName);
                if (view == "report" && !string.IsNullOrEmpty(lastReportPath))
                {
                    if (!System.IO.File.Exists(Repository.ReportsFolder + lastReportPath))
                    {
                        lastReportPath = "";
                        setCookie(SealLastReportPathCookieName, "");
                    }
                }

                return Json(new SWIUserProfile()
                {
                    name = WebUser.Name,
                    group = WebUser.SecurityGroupsDisplay,
                    culture = Repository.CultureInfo.EnglishName,
                    folder = getCookie(SealLastFolderCookieName),
                    lastreport = new SWIMenuItem() { 
                        path = lastReportPath, 
                        viewGUID = getCookie(SealLastReportViewGUIDCookieName),
                        outputGUID = getCookie(SealLastReportOutputGUIDCookieName),
                        name = getCookie(SealLastReportNameCookieName) 
                    },
                    lastview = view,
                    showfolders = WebUser.ShowFoldersView,
                    usertag = WebUser.Tag
                }, JsonRequestBehavior.AllowGet); ;
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        SWIMenuItem getMenuFromNames(List<SWIMenuItem> parents, List<string> names, SWIMenuItem menuItem)
        {
            var result = parents.FirstOrDefault(i => i.name == names[0] && i.path == null);
            if (result == null)
            {
                result = new SWIMenuItem() { name = names[0] };
                parents.Add(result);
                parents = parents.OrderBy(i => i.name).ToList();
            }
            if (names.Count > 1)
            {
                names.RemoveAt(0);
                result = getMenuFromNames(result.items, names, menuItem);
            }
            else
            {
                result.items.Add(menuItem);
                result.items = result.items.OrderBy(i => i.name).ToList();
            }

            return result;
        }


        IEnumerable<SWIMenuItem> getWebMenu(bool personal)
        {
            var result = new List<SWIMenuItem>();
            foreach (var view in WebUser.GetMenuReportViews())
            {
                var menuNames = view.MenuPath.Split('/').Where(i => !string.IsNullOrEmpty(i)).ToList();
                if (menuNames.Count > 0)
                {
                    var menuItem = new SWIMenuItem() { 
                        path = view.Report.RelativeFilePath, 
                        name = view.MenuReportViewName, 
                        viewGUID = view.GUID 
                    };
                    menuNames.RemoveAt(menuNames.Count - 1);
                    if (menuNames.Count > 0)
                    {
                        getMenuFromNames(result, menuNames, menuItem);
                    }
                    else
                    {
                        result.Add(menuItem);
                    }
                }
            }
            return result.OrderBy(i => i.name);
        }

        void checkRecentFiles()
        {
            //Clean reports
            WebUser.Profile.RecentReports.RemoveAll(i => !System.IO.File.Exists(Repository.ReportsFolder + i.Path));
        }

        /// <summary>
        /// Returns the menu of the logged user.
        /// </summary>
        public ActionResult SWIGetRootMenu()
        {
            writeDebug("SWIGetRootMenu");
            try
            {
                checkSWIAuthentication();
                return Json(
                    new
                    {
                        recentreports = (from r in WebUser.Profile.RecentReports 
                                         select new SWIMenuItem() {
                                             path = r.Path,
                                             viewGUID = r.ViewGUID,
                                             outputGUID = r.OutputGUID,
                                             name = r.Name
                                         }
                                         ).ToArray(),
                        reports = getWebMenu(false)
                    }
                    , JsonRequestBehavior.AllowGet);
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
                checkSWIAuthentication();
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
                checkSWIAuthentication();
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
                if (!Directory.Exists(Path.GetDirectoryName(folderDest.GetFullPath()))) throw new Exception("Error: create the parent directory first");
                Directory.Move(folderSource.GetFullPath(), folderDest.GetFullPath());
                Audit.LogAudit(AuditType.FolderRename, WebUser, folderSource.GetFullPath(), string.Format("Rename to '{0}'", folderDest.GetFullPath()));

                checkRecentFiles();
                WebUser.SaveProfile();

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

                        checkRecentFiles();
                        WebUser.SaveProfile();
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

                checkRecentFiles();
                WebUser.SaveProfile();

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
                while (report.Status != ReportStatus.Executed && !report.HasErrors && !report.Cancel) Thread.Sleep(100);

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
        public ActionResult SWExecuteReport(string path, string viewGUID, string outputGUID, bool? fromMenu)
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
                report.OnlyBody = (fromMenu != null && fromMenu.Value);

                var execution = initReportExecution(report, viewGUID, outputGUID, false);
                execution.RenderHTMLDisplayForViewer();

                WebUser.Profile.SetRecentReports(report, viewGUID, outputGUID);
                WebUser.SaveProfile();

                var lastReport = WebUser.Profile.RecentReports[0];
                setCookie(SealLastReportNameCookieName, lastReport.Name);
                setCookie(SealLastReportPathCookieName, lastReport.Path);
                setCookie(SealLastReportViewGUIDCookieName, lastReport.ViewGUID);
                setCookie(SealLastReportOutputGUIDCookieName, lastReport.OutputGUID);

                if (fromMenu != null && fromMenu.Value) return Json( System.IO.File.ReadAllText(report.HTMLDisplayFilePath) );
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
                //Clear session
                NavigationContext.Navigations.Clear();
                setSessionValue(SessionUser, null);
                setSessionValue(SessionNavigationContext, null);
                setSessionValue(SessionUploadedFiles, null);
                CreateWebUser();

                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Set the culture for the logged user.
        /// </summary>
        public ActionResult SWISetUserProfile(string culture)
        {
            writeDebug("SWISetUserProfile");
            try
            {
                checkSWIAuthentication();
                if (string.IsNullOrEmpty(culture)) throw new Exception("Error: culture must be supplied");
                if (culture != Repository.CultureInfo.EnglishName)
                {
                    if (!Repository.SetCultureInfo(culture)) throw new Exception("Invalid culture name:" + culture);
                    setCookie(SealCultureCookieName, culture);
                    WebUser.Profile.Culture = culture;
                }               
                WebUser.SaveProfile();

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
                    culture = Repository.CultureInfo.EnglishName
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
                var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

                var cultures = Repository.Configuration.WebCultures;
                if (cultures.Count == 0)
                {
                    var langs = Repository.GetInstalledTranslationLanguages();
                    foreach (var lang in langs)
                    {
                        foreach (var cultureInfo in allCultures.Where(i => i.TwoLetterISOLanguageName == lang))
                        {
                            cultures.Add(cultureInfo.EnglishName);
                        }
                    }
                }

                var result = new List<SWIItem>();
                foreach (var culture in cultures)
                {
                    var cultureInfo = allCultures.FirstOrDefault(i => i.EnglishName == culture);
                    if (cultureInfo == null) cultureInfo = allCultures.FirstOrDefault(i => i.Name == culture);
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

        /// <summary>
        /// Set the last view (report or folders) of the logged user
        /// </summary>
        public ActionResult SWISetLastView(string view)
        {
            writeDebug("SWISetLastView");
            try
            {
                checkSWIAuthentication();
                setCookie(SealLastViewCookieName, view);
                return Json(new object { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }
    }
}
