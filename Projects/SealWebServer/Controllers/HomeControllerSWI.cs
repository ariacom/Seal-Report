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

                    if (!WebUser.IsAuthenticated) throw new Exception(string.IsNullOrEmpty(WebUser.Error) ? Repository.TranslateWeb("Invalid user name or password") : WebUser.Error);
                }

                //Set culture from cookie
                string culture = GetCookie(SealCultureCookieName);
                if (!string.IsNullOrEmpty(culture)) Repository.SetCultureInfo(culture);


                return Json(new SWIUserProfile() { name = WebUser.Name, group = WebUser.SecurityGroupsDisplay, culture = Repository.CultureInfo.EnglishName, folder = GetCookie(SealLastFolderCookieName) });
            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }


        [HttpPost]
        public ActionResult SWIGetRootFolders()
        {
            try
            {
                checkSWIAuthentication();
                List<SWIFolder> result = new List<SWIFolder>();
                //Personal
                if (WebUser.HasPersonalFolder)
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
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetFolders(string path)
        {
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
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetFolderDetail(string path)
        {
            try
            {
                SWIFolder folder = getFolder(path);
                var files = new List<SWIFile>();
                if (folder.right > 0)
                {
                    foreach (string newPath in Directory.GetFiles(folder.GetFullPath(), "*.*").Where(i => !FileHelper.IsSealAttachedFile(i)))
                    {
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
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWISearch(string path, string pattern)
        {
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
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIDeleteFolder(string path)
        {
            try
            {
                SWIFolder folder = getFolder(path);
                if (folder.manage != 2) throw new Exception("Error: no right to delete this folder");
                Directory.Delete(folder.GetFullPath());
                return Json(new object { });
            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWICreateFolder(string path)
        {
            try
            {
                SWIFolder folder = getFolder(path);
                if (folder.manage == 0) throw new Exception("Error: no right to create in this folder");
                Directory.CreateDirectory(folder.GetFullPath());
                return Json(new object { });
            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIRenameFolder(string source, string destination)
        {
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
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetReportDetail(string path)
        {
            try
            {
                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");

                string newPath = getFullPath(path);
                if (!System.IO.File.Exists(newPath)) throw new Exception("Report path not found");
                Repository repository = Repository;
                Report report = Report.LoadFromFile(newPath, repository);
                SWIReportDetail result = new SWIReportDetail();
                result.views = (from i in report.Views select new SWIView() { guid = i.GUID, name = i.Name, displayName = report.TranslateViewName(i.Name) }).ToArray();
                result.outputs = (from i in report.Outputs select new SWIOutput() { guid = i.GUID, name = i.Name, displayName = report.TranslateOutputName(i.Name) }).ToArray();
                return Json(result);

            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }



        [HttpPost]
        public ActionResult SWIDeleteFiles(string paths)
        {
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
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIMoveFile(string source, string destination, bool copy)
        {
            try
            {
                SWIFolder folderSource = getParentFolder(source);
                if (folderSource.right == 0) throw new Exception("Error: no right on this folder");
                SWIFolder folderDest = getParentFolder(destination);
                if ((FolderRight)folderDest.right != FolderRight.Edit) throw new Exception("Error: no right to edit on the destination folder");

                string sourcePath = getFullPath(source);
                string destinationPath = getFullPath(destination);
                if (!System.IO.File.Exists(sourcePath)) throw new Exception("Error: source path is incorrect");
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
                    else
                    {
                        report.SchedulesWithCurrentUser = true;
                    }
                    report.SynchronizeTasks();
                }
                return Json(new object { });
            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIExecuteReportToResult(string path, string viewGUID, string outputGUID, string format)
        {
            try
            {
                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");

                string filePath = getFullPath(path);
                if (!System.IO.File.Exists(filePath)) throw new Exception("Error: report does not exist");
                Repository repository = Repository.CreateFast();
                Report report = Report.LoadFromFile(filePath, repository);

                var execution = initReportExecution(report, viewGUID, outputGUID);
                execution.Execute();
                while (report.Status != ReportStatus.Executed) System.Threading.Thread.Sleep(100);

                string result = "";
                if (!string.IsNullOrEmpty(outputGUID))
                {
                    //Copy the result output to temp
                    result = publishReportResult(report);
                }
                else
                {
                    string fileResult = "";
                    if (string.IsNullOrEmpty(format)) format = "html";
                    if (format.ToLower() == "print") fileResult = execution.GeneratePrintResult();
                    else if (format.ToLower() == "pdf") fileResult = execution.GeneratePDFResult();
                    else if (format.ToLower() == "excel") fileResult = execution.GenerateExcelResult();
                    else fileResult = execution.GenerateHTMLResult();
                    result = execution.Report.WebTempUrl + Path.GetFileName(fileResult);
                }
                report.PreInputRestrictions.Clear();
                return Json(new { url = result });
            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }


        [HttpPost]
        public ActionResult SWIExecuteReport(string path, bool? render, string viewGUID, string outputGUID)
        {
            try
            {
                checkSWIAuthentication();

                Report report = null;
                Repository repository = null;

                if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");
                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");

                string filePath = getFullPath(path);
                if (!System.IO.File.Exists(filePath)) throw new Exception("Error: report does not exist");
                repository = Repository.CreateFast();
                report = Report.LoadFromFile(filePath, repository);

                var execution = initReportExecution(report, viewGUID, outputGUID);
                execution.RenderHTMLDisplayForViewer();
                return GetContentResult(report.HTMLDisplayFilePath);
            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIViewFile(string path)
        {
            try
            {
                checkSWIAuthentication();
                SWIFolder folder = getParentFolder(path);
                if (folder.right == 0) throw new Exception("Error: no right on this folder");


                string tempFolder = Path.Combine(Path.Combine(Request.PhysicalApplicationPath, "temp"));
                FileHelper.PurgeTempDirectory(tempFolder);

                string filePath = getFullPath(path);

                filePath = FileHelper.CopySealFile(filePath, tempFolder);
                int index = Request.Url.OriginalString.ToLower().IndexOf("swiviewfile");
                if (index == -1) throw new Exception("Invalid URL");
                string url = Request.Url.OriginalString.Substring(0, index) + "temp/" + HttpUtility.UrlEncode(Path.GetFileName(filePath)).Replace("+", "%20");

                return Json(new { url = url });
            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWILogout()
        {
            try
            {
                CreateWebUser();
                return Json(new { });
            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }


        [HttpPost]
        public ActionResult SWISetUserProfile(string culture)
        {
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
                return handleSWIException(ex);
            }
        }


        [HttpPost]
        public ActionResult SWIGetUserProfile()
        {
            try
            {
                checkSWIAuthentication();
                return Json(new SWIUserProfile() { name = WebUser.Name, group = WebUser.SecurityGroupsDisplay, culture = Repository.CultureInfo.EnglishName, folder = GetCookie(SealLastFolderCookieName) });
            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetCultures()
        {
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
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWITranslate(string context, string instance, string reference)
        {
            try
            {
                checkSWIAuthentication();
                if (!string.IsNullOrEmpty(instance)) return Json(new { text = Repository.RepositoryTranslate(context, instance, reference) });
                return Json(new { text = Repository.Translate(context, reference) });
            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }

        [HttpPost]
        public ActionResult SWIGetVersions()
        {
            try
            {
                return Json(new { SWIVersion = Repository.ProductVersion, SRVersion = Repository.ProductVersion });
            }
            catch (Exception ex)
            {
                return handleSWIException(ex);
            }
        }


    }
}