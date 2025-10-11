﻿//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Seal.Helpers;
using Seal.Model;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Web;

namespace SealWebServer.Controllers
{
    /// <summary>
    /// Main Controller of the Web Report Server
    /// </summary>
    public partial class HomeController : Controller
    {
        [Authorize]
        public async Task<IActionResult> Login()
        {
            var idToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);

            // Validating the token expiration
            try
            {
                SetWebUser(default, default, idToken);
            }
            catch (Exception ex)
            {

                return HandleSWIException(ex);
            }

            return RedirectToAction("Main");
        }

        private void SetWebUser(string user, string password, string token)
        {
            CreateWebUser();
            WebUser.WebPrincipal = User;
            WebUser.WebUserName = user;
            WebUser.WebPassword = password;
            WebUser.Token = token;
            WebUser.Request = Request;
            WebUser.WebHostName = Request.Host.Host;
            Authenticate();

            if (WebUser.SecurityGroups.Count == 0) throw new LoginException(string.IsNullOrEmpty(WebUser.Error) ? Translate("Invalid user name or password") : WebUser.Error);
            //Load profile
            if (System.IO.File.Exists(WebUser.ProfilePath)) WebUser.Profile = SecurityUserProfile.LoadFromFile(WebUser.ProfilePath);
            checkRecentFiles();
            WebUser.Profile.Path = WebUser.ProfilePath;
        }

        private object getNotAuthenticatedProfile()
        {
            return new
            {
                authenticated = false,
                showresetpassword = !string.IsNullOrEmpty(Repository.Security.ResetPasswordScript) && !string.IsNullOrEmpty(Repository.Security.ResetPasswordScript2)
            };
        }


        private SWIUserProfile getUserProfile()
        {
            //Set repository defaults
            var defaultGroup = WebUser.DefaultGroup;
            if (!string.IsNullOrEmpty(defaultGroup.LogoName)) Repository.Configuration.LogoName = defaultGroup.LogoName;

            string culture = WebUser.Profile.Culture;
            if (!string.IsNullOrEmpty(culture)) Repository.SetCultureInfo(culture);
            else if (!string.IsNullOrEmpty(defaultGroup.Culture)) Repository.SetCultureInfo(defaultGroup.Culture);
            else if (Repository.CultureInfo.EnglishName != Repository.Instance.CultureInfo.EnglishName) Repository.SetCultureInfo(Repository.Instance.CultureInfo.EnglishName);

            string reportToExecute = "", reportToExecuteName = "";
            bool executeLast = false;
            if (defaultGroup.EditProfile && WebUser.Profile.OnStartup != StartupOptions.Default)
            {
                if (WebUser.Profile.OnStartup == StartupOptions.ExecuteLast && WebUser.Profile.RecentReports.Count > 0) executeLast = true;
                else if (WebUser.Profile.OnStartup == StartupOptions.ExecuteReport)
                {
                    reportToExecute = WebUser.Profile.StartUpReport;
                    reportToExecuteName = WebUser.Profile.StartupReportName;
                }
            }
            else
            {
                if (defaultGroup.OnStartup == StartupOptions.ExecuteLast) executeLast = true;
                else if (defaultGroup.OnStartup == StartupOptions.ExecuteReport)
                {
                    reportToExecute = defaultGroup.StartupReport;
                    reportToExecuteName = string.IsNullOrEmpty(defaultGroup.StartupReportName) ? Repository.TranslateFileName(defaultGroup.StartupReport) : Repository.TranslateReportDisplayName(defaultGroup.StartupReport, defaultGroup.StartupReportName);
                }
            }

            if (executeLast && WebUser.Profile.RecentReports.Count > 0)
            {
                reportToExecute = WebUser.Profile.RecentReports[0].Path;
                reportToExecuteName = WebUser.Profile.RecentReports[0].Name;
            }

            var profile = new SWIUserProfile()
            {
                name = WebUser.Name,
                group = WebUser.SecurityGroupsDisplay,
                culture = culture,
                language = Repository.CultureInfo.TwoLetterISOLanguageName,
                folder = WebUser.Profile.LastFolder,
                showfolders = WebUser.ShowFoldersView,
                editconfiguration = WebUser.EditConfiguration,
                editprofile = WebUser.EditProfile,
                downloadupload = Repository.Configuration.EnableDownloadUpload ? WebUser.DownloadUploadRight : DownloadUpload.None,
                usertag = WebUser.Tag,
                onstartup = WebUser.Profile.OnStartup,
                startupreport = WebUser.Profile.StartUpReport,
                startupreportname = WebUser.Profile.StartupReportName,
                report = reportToExecute,
                reportname = reportToExecuteName,
                executionmode = WebUser.Profile.ExecutionMode,
                groupexecutionmode = defaultGroup.ExecutionMode,
                sessionId = HttpContext.Session.GetString(SessionIdKey),
                changepassword = !string.IsNullOrEmpty(Repository.Security.ChangePasswordScript),
                showresetpassword = !string.IsNullOrEmpty(Repository.Security.ResetPasswordScript) && !string.IsNullOrEmpty(Repository.Security.ResetPasswordScript2)
            };

            if (!string.IsNullOrEmpty(profile.startupreport))
            {
                try
                {
                    getFileDetail(profile.startupreport);
                }
                catch
                {
                    profile.startupreport = "";
                    profile.startupreportname = "";
                }
            }
            if (!string.IsNullOrEmpty(profile.report))
            {
                try
                {
                    getFileDetail(profile.report);
                }
                catch
                {
                    profile.report = "";
                    profile.reportname = "";
                }
            }

            //Set default connections if several 
            profile.sources = new List<SWIMetaSource>();
            foreach (var source in Repository.Sources.Where(i => i.Connections.Count > 1))
            {
                var swiSource = new SWIMetaSource() { GUID = source.GUID, name = source.Name, connectionGUID = source.ConnectionGUID };
                var defaultConnection = WebUser.Profile.Connections.FirstOrDefault(i => i.SourceGUID == source.GUID);
                if (defaultConnection != null) swiSource.connectionGUID = defaultConnection.ConnectionGUID;
                else swiSource.connectionGUID = ReportSource.DefaultRepositoryConnectionGUID;

                swiSource.connections.Add(new SWIConnection() { GUID = ReportSource.DefaultRepositoryConnectionGUID, name = $"{Repository.TranslateWeb("Repository connection")} ({Repository.TranslateConnection(source.Connection)})" });

                foreach (var connection in source.Connections)
                {
                    swiSource.connections.Add(new SWIConnection() { GUID = connection.GUID, name = Repository.TranslateConnection(connection) });
                }
                profile.sources.Add(swiSource);
            }
            return profile;
        }

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
                    SetWebUser(user, password, token);
                    newAuthentication = true;
                }
#if WEBREPORTDESIGNER
                InitAI();
#endif

                if (!string.IsNullOrEmpty(WebUser.SecurityCode))
                {
                    //2FA check
                    return Json(new { securitycoderequired = true,  message=WebUser.SecurityCodeMessage});
                }

                //Audit
                Audit.LogAudit(AuditType.Login, WebUser);
                Audit.LogEventAudit(AuditType.EventLoggedUsers, SealSecurity.LoggedUsers.Count(i => i.IsAuthenticated).ToString());

                //Refresh menu reports
                if (newAuthentication) MenuReportViewsPool.ForceReload();

                return Json(getUserProfile());
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Check the security code for a 2 factors authentication
        /// </summary>
        public ActionResult SWICheckSecurityCode(string code)
        {
            writeDebug("SWICheckSecurityCode");

            try
            {
                if (string.IsNullOrEmpty(WebUser.SecurityCode))
                {
                    return Json(new { login = true });
                }

                if (string.IsNullOrEmpty(WebUser.Security.TwoFACheckScript)) throw new Exception(Translate("The 'Two-Factor Authentication Check Script' is not defined. Please check your configuration."));

                WebUser.WebSecurityCode = code;
                RazorHelper.CompileExecute(WebUser.Security.TwoFACheckScript, WebUser);

                if (WebUser.SecurityCodeTries == -1)
                {
                    //Force a re-login
                    CreateWebUser();
                    return Json(new { login = true });
                }

                if (!string.IsNullOrEmpty(WebUser.SecurityCode)) throw new Exception(Translate("Invalid code"));

                //Log and Audit
                WebHelper.WriteLogEntryWeb(EventLogEntryType.SuccessAudit, WebUser.AuthenticationSummary);
                Audit.LogAudit(AuditType.Login, WebUser);
                Audit.LogEventAudit(AuditType.EventLoggedUsers, SealSecurity.LoggedUsers.Count(i => i.IsAuthenticated).ToString());

                //Refresh menu reports
                MenuReportViewsPool.ForceReload();
                return Json(getUserProfile());
            }
            catch (Exception ex)
            {
                WebHelper.WriteLogEntryWeb(EventLogEntryType.FailureAudit, WebUser.AuthenticationSummary);
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Reset a user password
        /// </summary>
        public ActionResult SWIResetPassword(string id)
        {
            writeDebug("SWIResetPassword");
            try
            {
                if (WebUser == null) CreateWebUser();

                if (string.IsNullOrEmpty(id)) throw new Exception("Invalid argument");
                if (string.IsNullOrEmpty(WebUser.Security.ResetPasswordScript)) throw new Exception("The 'Reset Password Script' is not defined. Please check your configuration.");

                WebUser.WebUserName = id;
                WebUser.Request = Request;

                RazorHelper.CompileExecute(WebUser.Security.ResetPasswordScript, WebUser);

                //Log and Audit
                WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, $"Reset password sent for '{id}'");
                Audit.LogAudit(AuditType.Login, null, null, $"Reset password sent for '{id}'");

                return Json("");
            }
            catch (Exception ex)
            {
                HandleSWIException(ex);
                return Json(new { login = true });
            }
        }


        /// <summary>
        /// Re-init a user password from a reset password link
        /// </summary>
        public ActionResult SWIResetPassword2(string guid, string token, string password1, string password2)
        {
            writeDebug("SWIResetPassword2");
            try
            {
                if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(token)) throw new Exception("Invalid arguments");
                if (password1 != password2) throw new Exception(Translate("The two passwords do not match."));
                if (string.IsNullOrEmpty(Repository.Security.ResetPasswordScript2)) throw new Exception("The 'Reset Password Script2' is not defined. Please check your configuration.");

                if (WebUser == null) CreateWebUser();
                WebUser.WebUserName = guid; //Guid in web password
                WebUser.WebPassword = password1; //Guid in web password
                WebUser.Token = token;

                RazorHelper.CompileExecute(WebUser.Security.ResetPasswordScript2, WebUser);

                //Log and Audit
                WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, $"Password reset for '{WebUser.WebUserName}'");
                Audit.LogAudit(AuditType.EventServer, null, null, $"Password reset for '{WebUser.WebUserName}'");

                return Json("");
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }
        /// <summary>
        /// Change a user password
        /// </summary>
        public ActionResult SWIChangePassword(string password, string password1, string password2, string sessionId)
        {
            writeDebug("SWIChangePassword");
            try
            {
                SetSessionId(sessionId);
                checkSWIAuthentication();
                var user = WebUser;

                if (user.Login == null) throw new Exception("No login for the user");
                if (!user.Login.CheckPassword(password)) throw new Exception(Translate("Invalid password."));
                if (password1 != password2) throw new Exception(Translate("The two passwords do not match."));

                if (!Helper.IsPasswordComplex(password1)) throw new Exception(Translate("Your password must contain at least 8 characters, including at least one uppercase letter, one number, and one special character (e.g., !@#$%^&*)."));

                user.Login.HashedPassword = password1;
                user.Security.SaveToFile();

                if (!string.IsNullOrEmpty(user.Login.Email))
                {
                    var message = Repository.TranslateWeb("Your password has been changed.") + $" ({user.Login.Id})";
                    var from = ""; //Default of the device will be used
                    var to = user.Login.Email;
                    var subject = Repository.TranslateWeb("Seal Report Password Change");
                    var body = $"{message}<br>";
                    if (!Repository.SendNotificationEmail(from, to, subject, true, body)) throw new Exception("Unable to send email for Change Password.");
                }

                //Log and Audit
                WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, $"Password changed for '{user.Login.Id}'");
                Audit.LogAudit(AuditType.EventServer, null, null, $"Password changed for '{user.Login.Id}'");


                return Json("");
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


        IEnumerable<SWIMenuItem> getWebMenu()
        {
            var result = new List<SWIMenuItem>();
            foreach (var view in WebUser.GetMenuReportViews())
            {
                var menuNames = view.MenuPath.Split('/').Where(i => !string.IsNullOrEmpty(i)).ToList();
                if (menuNames.Count > 0)
                {
                    var relativePath = "";
                    var personalFolder = WebUser.Security.Repository.GetPersonalFolder(WebUser);
                    if (view.Report.FilePath.StartsWith(personalFolder)) relativePath = view.Report.FilePath.Replace(personalFolder, ":");
                    else relativePath = view.Report.RelativeFilePath;

                    var menuItem = new SWIMenuItem()
                    {
                        path = relativePath,
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
            WebUser.Profile.RecentReports.RemoveAll(i => i == null || !System.IO.File.Exists(getFullPath(i.Path)));
        }

        /// <summary>
        /// Returns the menu of the logged user.
        /// </summary>
        public ActionResult SWIGetRootMenu(string sessionId)
        {
            writeDebug("SWIGetRootMenu");
            try
            {
                SetSessionId(sessionId);
                checkSWIAuthentication();

                WebUser.Profile.RecentReports.RemoveAll(i => i == null || !System.IO.File.Exists(getFullPath(i.Path)));
                WebUser.Profile.Favorites.RemoveAll(i => i == null || !System.IO.File.Exists(getFullPath(i.Path)));


                WebUser.WebMenu = new SWIWebMenu()
                {
                    recentreports = (from r in WebUser.Profile.RecentReports
                                     select new SWIMenuItem()
                                     {
                                         path = r.Path,
                                         viewGUID = r.ViewGUID,
                                         outputGUID = r.OutputGUID,
                                         name = r.Name
                                     }
                                         ).ToList(),
                    reports = getWebMenu().ToList(),
                    favorites = (from r in WebUser.Profile.Favorites
                                     select new SWIMenuItem()
                                     {
                                         path = r.Path,
                                         name = r.Name
                                     }
                                         ).ToList()
                };

                var profile = WebUser.Profile;
                var startUpReport = "";
                if (profile.OnStartup == StartupOptions.Default && WebUser.DefaultGroup?.OnStartup == StartupOptions.ExecuteReport) startUpReport = WebUser.DefaultGroup.StartupReport;
                else if (profile.OnStartup == StartupOptions.ExecuteReport) startUpReport = profile.StartUpReport;
                if (!string.IsNullOrEmpty(startUpReport))
                {
                    //Remove startup report as the link is available in the Product link
                    WebUser.WebMenu.recentreports.RemoveAll(i => i.path == FileHelper.ConvertOSFilePath(startUpReport));
                }

                //Apply menu scripts
                WebUser.ScriptNumber = 1;
                foreach (var group in WebUser.SecurityGroups.Where(i => !string.IsNullOrEmpty(i.MenuScript)).OrderBy(i => i.Name))
                {
                    RazorHelper.CompileExecute(group.MenuScript, WebUser);
                    WebUser.ScriptNumber++;
                }

                return Json(WebUser.WebMenu);
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
        public ActionResult SWIGetRootFolders(string sessionId)
        {
            writeDebug("SWIGetRootFolders");
            try
            {
                SetSessionId(sessionId);
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
                return Json(WebUser.Folders);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Returns the list of file names and details contained in a folder.
        /// </summary>
        public ActionResult SWIGetFolderDetail(string path, string sessionId)
        {
            writeDebug("SWIGetFolderDetail");
            try
            {
                SetSessionId(sessionId);
                checkSWIAuthentication();
                var folderDetail = getFolderDetail(path, true);
                WebUser.Profile.LastFolder = path;
                WebUser.SaveProfile();
                return Json(folderDetail);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Returns the list of file names and details matching a search in the repository.
        /// </summary>
        public ActionResult SWISearch(string path, string pattern, string sessionId)
        {
            writeDebug("SWISearch");
            try
            {
                SetSessionId(sessionId);
                checkSWIAuthentication();
                SWIFolder folder = getFolder(path);
                var files = new List<SWIFile>();
                path = folder.GetFullPath();
                searchFolder(folder, pattern, files);

                return Json(new SWIFolderDetail() { files = files });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Delete a sub-folder in the repository. The folder must be empty.
        /// </summary>
        public ActionResult SWIDeleteFolder(string path, string sessionId)
        {
            writeDebug("SWIDeleteFolder");
            try
            {
                SetSessionId(sessionId);
                SWIFolder folder = getFolder(path);
                if (folder.manage != 2) throw new Exception("Error: no right to delete this folder");
                Directory.Delete(folder.GetFullPath());
                Audit.LogAudit(AuditType.FolderDelete, WebUser, folder.GetFullPath());
                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Create a sub-folder in the repository.
        /// </summary>
        public ActionResult SWICreateFolder(string path, string sessionId)
        {
            writeDebug("SWICreateFolder");
            try
            {
                SetSessionId(sessionId);
                SWIFolder folder = getFolder(path);
                if (folder.manage == 0) throw new Exception("Error: no right to create in this folder");
                Directory.CreateDirectory(folder.GetFullPath());
                Audit.LogAudit(AuditType.FolderCreate, WebUser, folder.GetFullPath());
                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Rename a sub-folder in the repository.
        /// </summary>
        public ActionResult SWIRenameFolder(string source, string destination, string sessionId)
        {
            writeDebug("SWIRenameFolder");
            try
            {
                SetSessionId(sessionId);
                SWIFolder folderSource = getFolder(source);
                SWIFolder folderDest = getFolder(destination);
                if (folderSource.manage != 2 || folderDest.manage != 2) throw new Exception("Error: no right to rename this folder");
                if (!Directory.Exists(Path.GetDirectoryName(folderDest.GetFullPath()))) throw new Exception("Error: create the parent directory first");
                Directory.Move(folderSource.GetFullPath(), folderDest.GetFullPath());
                Audit.LogAudit(AuditType.FolderRename, WebUser, folderSource.GetFullPath(), string.Format("Rename to '{0}'", folderDest.GetFullPath()));

                checkRecentFiles();
                WebUser.SaveProfile();

                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Returns the views and outputs of a report.
        /// </summary>
        public ActionResult SWIGetReportDetail(string path, string sessionId)
        {
            writeDebug("SWIGetReportDetail");
            try
            {
                SetSessionId(sessionId);
                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");

                var file = getFileDetail(path);
                if (file.right == 0) throw new Exception("Error: no right on this report or file");

                string newPath = getFullPath(path);
                if (!System.IO.File.Exists(newPath)) throw new Exception("Report path not found");
                Repository repository = Repository;
                Report report = Report.LoadFromFile(newPath, repository, false);
                SWIReportDetail result = new SWIReportDetail();
                result.views = (from i in report.Views.Where(i => i.WebExec && i.GUID != report.ViewGUID) select new SWIView() { guid = i.GUID, name = i.Name, displayname = report.TranslateViewName(i.Name) }).ToList();
                result.outputs = ((FolderRight)folder.right >= FolderRight.ExecuteReportOuput) ? (from i in report.Outputs.Where(j => j.PublicExec || string.IsNullOrEmpty(j.UserName) || (!j.PublicExec && j.UserName == WebUser.Name)) select new SWIOutput() { guid = i.GUID, name = i.Name, displayname = report.TranslateOutputName(i.Name) }).ToList() : new List<SWIOutput>();
                if (result.views.Count == 0 && result.outputs.Count == 0) result.views = (from i in report.Views.Where(i => i.WebExec) select new SWIView() { guid = i.GUID, name = i.Name, displayname = report.TranslateViewName(i.Name) }).ToList();

                return Json(result);

            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Delete files or reports from the repository.
        /// </summary>
        public ActionResult SWIDeleteFiles(string paths, string sessionId)
        {
            writeDebug("SWIDeleteFiles");
            try
            {
                SetSessionId(sessionId);
                checkSWIAuthentication();
                if (string.IsNullOrEmpty(paths)) throw new Exception("Error: paths must be supplied");

                foreach (var path in paths.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n'))
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        SWIFolder folder = getParentFolder(path);
                        if ((FolderRight)folder.right != FolderRight.Edit) throw new Exception("Error: no right to edit in this folder");

                        var file = getFileDetail(path);
                        if ((FolderRight)file.right != FolderRight.Edit) throw new Exception("Error: no right to edit this report or file");

                        string fullPath = getFullPath(path);
                        if (FileHelper.IsReportFile(fullPath) && FileHelper.ReportHasSchedule(fullPath))
                        {
                            //Delete schedules...
                            var report = Report.LoadFromFile(fullPath, Repository, false);
                            report.Schedules.Clear();
                            report.SynchronizeTasks();
                        }

                        FileHelper.DeleteFile(fullPath);

                        Audit.LogAudit(AuditType.FileDelete, WebUser, path);

                        checkRecentFiles();
                        WebUser.SaveProfile();
                    }
                }
                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Move a file or a report in the repository.
        /// </summary>
        public ActionResult SWIMoveFile(string source, string destination, bool copy, string sessionId)
        {
            writeDebug("SWIMoveFile");
            try
            {
                SetSessionId(sessionId);

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
                if (folderDest.files && FileHelper.IsReportFile(sourcePath)) throw new Exception(Translate("Warning: only files (and not reports) can be copied to this folder."));
                if (System.IO.File.Exists(destinationPath) && copy) destinationPath = FileHelper.GetUniqueFileName(Path.GetDirectoryName(destinationPath), Path.GetFileNameWithoutExtension(destinationPath) + " - Copy" + Path.GetExtension(destinationPath), Path.GetExtension(destinationPath));

                bool hasSchedule = (FileHelper.IsReportFile(sourcePath) && FileHelper.ReportHasSchedule(sourcePath));
                if (file.isreport)
                {
                    if (copy)
                    {
                        //Change GUIDs
                        var report = Report.LoadFromFile(sourcePath, Repository, false);
                        report.InitGUIDs();
                        report.FilePath = destinationPath;
                        report.SaveToFile();
                    }
                    else
                    {
                        //Simple report move
                        FileHelper.MoveFile(sourcePath, destinationPath, copy);
                        if (hasSchedule)
                        {
                            //Re-init schedules...
                            var report = Report.LoadFromFile(destinationPath, Repository, false);
                            report.SchedulesWithCurrentUser = false;
                            report.SynchronizeTasks();

                        }
                    }
                }
                else
                {
                    //Simple file move/copy
                    FileHelper.MoveFile(sourcePath, destinationPath, copy);
                }

                if (copy) Audit.LogAudit(AuditType.FileCopy, WebUser, sourcePath, string.Format("Copy to '{0}'", destinationPath));
                else Audit.LogAudit(AuditType.FileMove, WebUser, sourcePath, string.Format("Move to '{0}'", destinationPath));

                checkRecentFiles();
                WebUser.SaveProfile();

                return Json(new object { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Execute a report into a report result and returns the result. Check API of Seal Web Interface for more information.
        /// </summary>
        public ActionResult SWExecuteReportToResult(string path, string viewGUID, string outputGUID, string format, string sessionId)
        {
            writeDebug("SWExecuteReportToResult");
            try
            {
                if (!CheckAuthentication(sessionId)) return Content(_loginContent);

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
                    ReportFormat f;
                    if (!Enum.TryParse(format, out f)) f = ReportFormat.html;
                    fileResult = execution.GenerateResult(f);
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
        public ActionResult SWExecuteReport(string path, string viewGUID, string outputGUID, bool? fromMenu, string sessionId)
        {
            writeDebug("SWExecuteReport");
            try
            {
                if (!CheckAuthentication(sessionId)) return Content(_loginContent);

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

                WebUser.Profile.SetRecentReports(path, report, viewGUID, outputGUID);
                WebUser.SaveProfile();

                if (fromMenu != null && fromMenu.Value) return Json(System.IO.File.ReadAllText(report.HTMLDisplayFilePath));
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
        public ActionResult SWViewFile(string path, string sessionId)
        {
            writeDebug("SWViewFile");
            try
            {
                if (!CheckAuthentication(sessionId)) return Content(_loginContent);

                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");

                var file = getFileDetail(path);
                if (file.right == 0) throw new Exception("Error: no right on this report or file");
                if (file.isreport && WebUser.DownloadUploadRight == DownloadUpload.None || !Repository.Configuration.EnableDownloadUpload) throw new Exception("Error: no right to download report.");
                if (file.isreport && !Repository.Configuration.EnableDownloadUpload) throw new Exception("Error: upload and download are not allowed for the server.");
                return getFileResult(getFullPath(path), null);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }


        /// <summary>
        /// Upload a file in a repository folder.
        /// </summary>
        public ActionResult SWUploadFile(string sessionId)
        {
            writeDebug("SWUploadFile");
            try
            {
                if (!CheckAuthentication(sessionId)) return Content(_loginContent);

                var path = Request.Form["path"];
                SWIFolder folder = getFolder(path);
                if (Request.Form.Files.Count == 0) throw new Exception("No file to upload");
                if ((FolderRight)folder.right != FolderRight.Edit) throw new Exception("Error: no right to upload file on this folder");


                var file = Request.Form.Files[0];
                //Saving the file
                var finalPath = FileHelper.GetUniqueFileName(Path.Combine(folder.GetFullPath(), file.FileName));
                using (var stream = System.IO.File.Create(finalPath))
                {
                    file.CopyTo(stream);
                }
                return Json(new { Status = true, Message = Translate("The file has been uploaded.") });
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        /// <summary>
        /// Mark/Unmark report as favorite.
        /// </summary>
        public ActionResult SWMarkFavorite(string path, string sessionId)
        {
            writeDebug("SWMarkFavorite");
            try
            {
                if (!CheckAuthentication(sessionId)) return Content(_loginContent);

                SWIFolder folder = getFolder(path);
                if ((FolderRight)folder.right == FolderRight.None) throw new Exception("Error: no right on this folder");

                WebUser.Profile.MarkFavorite(path);
                WebUser.SaveProfile();

                return Json(new { Message = Translate("The favorite has been updated") });
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        /// <summary>
        /// Clear the current user session.
        /// </summary>
        public ActionResult SWILogout(string sessionId)
        {
            writeDebug("SWILogout");

            try
            {
                SetSessionId(sessionId);
                if (WebUser != null) WebUser.Logout();
                //Audit
                Audit.LogAudit(AuditType.Logout, WebUser);
                Audit.LogEventAudit(AuditType.EventLoggedUsers, SealSecurity.LoggedUsers.Count(i => i.IsAuthenticated).ToString());
                //Clear session
                NavigationContext.Navigations.Clear();
                setSessionValue(SessionUser, null);
                setSessionValue(SessionNavigationContext, null);
                setSessionValue(SessionUploadedFiles, null);
                setSessionValue(SessionAssistant, null);
                setSessionValue(SessionAssistantConfiguration, null);
                CreateWebUser();

                //SignOut
                if (AuthenticationConfig.Enabled)
                {
                    SignOut("Cookies", "oidc");
                }

                return Json(new { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Set the culture for the logged user.
        /// </summary>
        public ActionResult SWISetUserProfile(string culture, string onStartup, string startupReport, string startupReportName, string executionMode, string[] connections, string sessionId)
        {
            writeDebug("SWISetUserProfile");
            try
            {
                SetSessionId(sessionId);
                checkSWIAuthentication();
                if (!WebUser.DefaultGroup.EditProfile) throw new Exception("No right to change the profile");

                if (string.IsNullOrEmpty(culture))
                {
                    WebUser.Profile.Culture = "";
                    if (!string.IsNullOrEmpty(WebUser.DefaultGroup.Culture)) Repository.SetCultureInfo(WebUser.DefaultGroup.Culture);
                    else Repository.SetCultureInfo(Repository.Instance.CultureInfo.EnglishName);
                }
                else
                {
                    if (!Repository.SetCultureInfo(culture)) throw new Exception("Invalid culture name:" + culture);
                    WebUser.Profile.Culture = culture;
                }

                var onStartupVal = StartupOptions.Default;
                if (Enum.TryParse(onStartup, out onStartupVal))
                {
                    WebUser.Profile.OnStartup = onStartupVal;
                    WebUser.Profile.StartUpReport = startupReport;
                    WebUser.Profile.StartupReportName = startupReportName;
                }

                var execMode = ExecutionMode.Default;
                if (Enum.TryParse(executionMode, out execMode))
                {
                    WebUser.Profile.ExecutionMode = execMode;
                }

                WebUser.Profile.Connections.Clear();
                foreach (var connection in connections)
                {
                    var guids = connection.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                    if (guids.Length == 2) WebUser.Profile.Connections.Add(new DefaultConnection() { SourceGUID = guids[0], ConnectionGUID = guids[1] });

                }
                if (!WebUser.SaveProfile()) throw new Exception("Unable to save Profile. Check logs for detail.");

                return Json(new { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Returns the profile information of the logged user.
        /// </summary>
        public ActionResult SWIGetUserProfile(string sessionId)
        {
            writeDebug("SWIGetUserProfile");
            try
            {
                SetSessionId(sessionId);
                if (WebUser == null || !WebUser.IsAuthenticated) return Json(getNotAuthenticatedProfile());

                return Json(getUserProfile());
            }
            catch
            {
                //not authenticated
                return Json(getNotAuthenticatedProfile());
            }
        }

        /// <summary>
        /// Save the configuration of the Web Server (including security).
        /// </summary>
        public ActionResult SWISetConfiguration(string sessionId)
        {
            writeDebug("SWISetConfiguration");
            try
            {
                SetSessionId(sessionId);
                checkSWIAuthentication();
                if (!WebUser.DefaultGroup.EditConfiguration) throw new Exception("No right to save the configuration");

                var swiConfig = JsonConvert.DeserializeObject(Request.Form["configuration"], typeof(SWIConfiguration)) as SWIConfiguration;
                if (swiConfig == null) throw new Exception("Error: no configuration to save");

                Repository.Configuration.WebProductName = swiConfig.productname;
                Repository.Configuration.SaveToFile();

                Repository.Security.Groups = swiConfig.groups;
                Repository.Security.Logins = swiConfig.logins;
                //Check passwords
                foreach (var login in Repository.Security.Logins)
                {
                    if (login.Password != null && login.Password.StartsWith(login.HashedPassword)) login.HashedPassword = login.Password.Substring(login.HashedPassword.Length);
                }

                Repository.Security.SaveToFile();

                return Json(new { });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Returns the configuration of the web server (including security).
        /// </summary>
        public ActionResult SWIGetConfiguration(string sessionId)
        {
            writeDebug("SWIGetConfiguration");
            try
            {
                SetSessionId(sessionId);
                if (WebUser == null || !WebUser.IsAuthenticated) return Json(new { authenticated = false });

                var result = new SWIConfiguration()
                {
                    productname = Repository.Configuration.WebProductName,
                    groups = Repository.Security.Groups,
                    logins = Repository.Security.Logins,
                    downloadupload = Repository.Configuration.EnableDownloadUpload,
                };
                result.folders = SWIConfiguration.GetFolders(WebUser);

                return Json(result);
            }
            catch
            {
                //not authenticated
                return Json(new { authenticated = false });
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
                return Json(result.OrderBy(i => i.val).ToArray());
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Translate a text either from the public translations or the repository translations. If the optional parameter instance is not empty, the repository translations are used.
        /// </summary>
        public ActionResult SWITranslate(string context, string instance, string reference, string sessionId)
        {
            writeDebug("SWITranslate");
            try
            {
                SetSessionId(sessionId);
                checkSWIAuthentication();
                if (!string.IsNullOrEmpty(instance)) return Json(new { text = Repository.RepositoryTranslate(context, instance, reference) });
                return Json(new { text = Repository.Translate(context, reference) });
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
                return Json(new
                {
                    SRVersion = Repository.ProductVersion,
                    SRAdditionalVersion = Repository.ProductAdditionalInfo,
                    Info = string.IsNullOrEmpty(Repository.Instance.LicenseText) ? "Free MIT Community License\r\nNon-profit usage or small businesses" : Repository.Instance.LicenseText
                });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }
    }
}

