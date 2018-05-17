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
    public partial class HomeController : Controller
    {
        [HttpPost]
        public ActionResult SWILogin(string user, string password)
        {
            WriteDebug("SWILogin");
            try
            {
                if (WebUser == null || !WebUser.IsAuthenticated || WebUser.WebUserName != user)
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


                return Json(new SWIUserProfile() { name = WebUser.Name, group = WebUser.SecurityGroupsDisplay, culture = Repository.CultureInfo.EnglishName, folder = GetCookie(SealLastFolderCookieName) });
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
                        if (folder.files && FileHelper.IsSealReportFile(newPath)) continue;

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
                if (!string.IsNullOrEmpty(outputGUID) && (FolderRight) folder.right == FolderRight.Execute) throw new Exception("Error: no right to execute output on this folder");

                string filePath = getFullPath(path);
                if (!System.IO.File.Exists(filePath)) throw new Exception("Error: report does not exist");
                Repository repository = Repository.CreateFast();
                Report report = Report.LoadFromFile(filePath, repository);

                var execution = initReportExecution(report, viewGUID, outputGUID, true);
                execution.Execute();
                while (report.Status != ReportStatus.Executed) System.Threading.Thread.Sleep(100);

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
        public ActionResult SWISetUserProfile(string culture)
        {
            WriteDebug("SWISetUserProfile");
            try
            {
                checkSWIAuthentication();
                if (string.IsNullOrEmpty(culture)) throw new Exception("Error: culture must be supplied");

                if (!Repository.SetCultureInfo(culture)) throw new Exception("Invalid culture name:" + culture);
                SetCookie(SealCultureCookieName, culture);

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
                checkSWIAuthentication();

                return Json(new SWIUserProfile() { authenticated = true, name = WebUser.Name, group = WebUser.SecurityGroupsDisplay, culture = Repository.CultureInfo.EnglishName, folder = GetCookie(SealLastFolderCookieName) });
            }
            catch
            {
                //not authenticated
                return Json(new SWIUserProfile() { authenticated = false });
            }
        }

        [HttpPost]
        public ActionResult SWIGetCultures()
        {
            WriteDebug("SWIGetCultures");
            try
            {
                checkSWIAuthentication();

                List<SWIItem> vals = new List<SWIItem>();
                foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(i => i.EnglishName))
                {
                    vals.Add(new SWIItem() { id = culture.EnglishName, val = culture.NativeName });
                }
                return Json(vals.ToArray());
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


    }
}