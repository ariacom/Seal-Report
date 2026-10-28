//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Seal.Helpers;
using Seal.Model;

namespace SealWebServer.Controllers
{
    /// <summary>
    /// Web schedules: a user schedules a report he can execute. The schedule is stored in his personal folder (the report file is not modified)
    /// and executed by the Seal Report Scheduler with his security context (see SealSchedule.IsWebSchedule).
    /// </summary>
    public partial class HomeController
    {
        const string WebScheduleDateFormat = "yyyy-MM-ddTHH:mm";

        /// <summary>
        /// Returns the web schedules of the logged user
        /// </summary>
        public ActionResult SWIGetWebSchedules(string sessionId)
        {
            writeDebug("SWIGetWebSchedules");
            try
            {
                SetSessionId(sessionId);
                checkWebScheduleUser();
                return Json(getUserWebSchedules().OrderBy(i => i.Name).Select(i => toSWIWebSchedule(i, false)).ToList());
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Returns a web schedule to edit (a new one if guid is empty) with the output devices, folders and formats available
        /// </summary>
        public ActionResult SWIGetWebSchedule(string path, string guid, string sessionId)
        {
            writeDebug("SWIGetWebSchedule");
            try
            {
                SetSessionId(sessionId);
                checkWebScheduleUser();

                SealSchedule schedule = string.IsNullOrEmpty(guid) ? null : getUserWebSchedule(guid);
                if (schedule != null) path = schedule.WebPath;
                var report = getWebScheduleReport(path, out string execPath);

                var result = new SWIWebScheduleEdit();
                foreach (var device in Repository.Devices.Where(i => WebUser.CanAccessDevice(i)))
                {
                    var swiDevice = new SWIWebScheduleDevice() { guid = device.GUID, name = Repository.TranslateDevice(device.Name, device.Name), type = getDeviceType(device) };
                    if (device is OutputFileServerDevice fileServer && !string.IsNullOrEmpty(fileServer.Directories)) swiDevice.directories = fileServer.DirectoriesArray.ToList();
                    else if (device is OutputSharePointDevice sharePoint && !string.IsNullOrEmpty(sharePoint.Directories)) swiDevice.directories = sharePoint.DirectoriesArray.ToList();
                    result.devices.Add(swiDevice);
                }
                //Folders where the user can write the result
                foreach (var folder in WebUser.AllFolders.Where(i => (FolderRight)i.right == FolderRight.Edit && !i.IsRepository && i.type != "bin").OrderBy(i => i.IsPersonal ? 0 : 1).ThenBy(i => i.fullname))
                {
                    result.folders.Add(new SWIItem() { id = folder.path, val = folder.fullname });
                }
                foreach (var format in Repository.ResultAllowedFormats)
                {
                    result.formats.Add(new SWIItem() { id = ((int)format).ToString(), val = getFormatName(format) });
                }
                var culture = Repository.CultureInfo;
                for (int i = 0; i < 7; i++) result.weekdays.Add(new SWIItem() { id = i.ToString(), val = culture.DateTimeFormat.DayNames[i] });
                for (int i = 1; i <= 12; i++) result.months.Add(new SWIItem() { id = i.ToString(), val = culture.DateTimeFormat.MonthNames[i - 1] });

                if (schedule != null)
                {
                    result.schedule = toSWIWebSchedule(schedule, true);
                }
                else
                {
                    //New schedule with default values
                    var start = DateTime.Today.AddDays(1).AddHours(8);
                    var device = result.devices.FirstOrDefault(i => i.type == "email") ?? result.devices.FirstOrDefault();
                    result.schedule = new SWIWebSchedule()
                    {
                        name = report.ExecutionName,
                        enabled = true,
                        path = execPath,
                        reportname = report.ExecutionName,
                        deviceguid = device?.guid,
                        format = Repository.ResultAllowedFormats.Contains(report.Format) ? report.Format : Repository.ResultAllowedFormats.FirstOrDefault(),
                        folder = device?.type == "folder" ? result.folders.FirstOrDefault()?.id : device?.directories.FirstOrDefault(),
                        file = Repository.SealReportDisplayNameKeyword2,
                        to = WebUser.Email,
                        type = TriggerType.Daily,
                        start = start.ToString(WebScheduleDateFormat, CultureInfo.InvariantCulture),
                        interval = 1,
                        weekdays = new int[] { (int)start.DayOfWeek },
                        months = Enumerable.Range(1, 12).ToArray(),
                        days = new int[] { 1 }
                    };
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Creates or updates a web schedule of the logged user
        /// </summary>
        public ActionResult SWISaveWebSchedule(string path, string schedule, string sessionId)
        {
            writeDebug("SWISaveWebSchedule");
            try
            {
                SetSessionId(sessionId);
                checkWebScheduleUser();
                var swiSchedule = JsonConvert.DeserializeObject<SWIWebSchedule>(schedule);

                SealSchedule sealSchedule = string.IsNullOrEmpty(swiSchedule.guid) ? null : getUserWebSchedule(swiSchedule.guid);
                if (sealSchedule != null) path = sealSchedule.WebPath;
                var report = getWebScheduleReport(path, out string execPath);

                bool isNew = sealSchedule == null;
                if (isNew) sealSchedule = new SealSchedule() { GUID = Guid.NewGuid().ToString(), Output = new WebScheduleOutput() };
                var oldFilePath = sealSchedule.FilePath;

                setWebSchedule(sealSchedule, swiSchedule, report, execPath);

                var folder = Repository.GetWebSchedulesFolder(WebUser);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                sealSchedule.FilePath = Path.Combine(folder, FileHelper.CleanFilePath(sealSchedule.Name, "_") + " " + sealSchedule.GUID + ".xml");
                sealSchedule.SaveToFile();
                if (!string.IsNullOrEmpty(oldFilePath) && oldFilePath != sealSchedule.FilePath && System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);

                WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, $"Web schedule '{sealSchedule.Name}' {(isNew ? "created" : "updated")} for '{WebUser.WebUserName}', report '{execPath}'");
                Audit.LogAudit(AuditType.EventServer, WebUser, execPath, $"Web schedule '{sealSchedule.Name}' {(isNew ? "created" : "updated")}");
                return Json(toSWIWebSchedule(sealSchedule, false));
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        /// <summary>
        /// Deletes a web schedule of the logged user
        /// </summary>
        public ActionResult SWIDeleteWebSchedule(string guid, string sessionId)
        {
            writeDebug("SWIDeleteWebSchedule");
            try
            {
                SetSessionId(sessionId);
                checkWebScheduleUser();
                var schedule = getUserWebSchedule(guid);
                FileHelper.DeleteFile(schedule.FilePath);

                WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, $"Web schedule '{schedule.Name}' deleted for '{WebUser.WebUserName}'");
                Audit.LogAudit(AuditType.EventServer, WebUser, schedule.WebPath, $"Web schedule '{schedule.Name}' deleted");
                return Json(new { Message = Translate("The schedule has been deleted") });
            }
            catch (Exception ex)
            {
                return HandleSWIException(ex);
            }
        }

        void checkWebScheduleUser()
        {
            checkSWIAuthentication();
            //The scheduler authenticates the owner from his login name
            if (!WebUser.CanUseWebSchedules) throw new ValidationException(Translate("You are not allowed to schedule reports"));
        }

        /// <summary>
        /// True if the 'My schedules' list is shown: the user has a folder where he can schedule reports, or he has already web schedules
        /// </summary>
        bool showWebSchedules()
        {
            if (!WebUser.CanUseWebSchedules) return false;
            //At login, the folders of the user are not built yet
            if (WebUser.Folders.Count == 0) WebUser.SetFolders();
            if (WebUser.AllFolders.Any(i => (FolderRight)i.right >= FolderRight.Schedule && !i.IsRepository && i.type != "bin")) return true;
            var folder = Repository.GetWebSchedulesFolder(WebUser);
            return Directory.Exists(folder) && Directory.GetFiles(folder, "*.xml").Length > 0;
        }

        /// <summary>
        /// Web schedules of the logged user, read from his personal folder
        /// </summary>
        List<SealSchedule> getUserWebSchedules()
        {
            var result = new List<SealSchedule>();
            var folder = Repository.GetWebSchedulesFolder(WebUser);
            if (!Directory.Exists(folder)) return result;
            foreach (var file in Directory.GetFiles(folder, "*.xml"))
            {
                try
                {
                    var schedule = SealSchedule.LoadFromFile(file);
                    if (schedule.IsWebSchedule && schedule.UserName == WebUser.WebUserName) result.Add(schedule);
                }
                catch (Exception ex)
                {
                    WebHelper.WriteLogEntryWeb(EventLogEntryType.Error, ex.Message);
                }
            }
            return result;
        }

        SealSchedule getUserWebSchedule(string guid)
        {
            var result = getUserWebSchedules().FirstOrDefault(i => i.GUID == guid);
            if (result == null) throw new ValidationException(Translate("The schedule has not been found"));
            return result;
        }

        /// <summary>
        /// Returns the report to schedule after checking the right 'Edit schedules' of the user on the report (the target of a shortcut is scheduled)
        /// </summary>
        Report getWebScheduleReport(string path, out string execPath)
        {
            SWIFolder folder = getParentFolder(path);
            if (folder.right == 0) throw new Exception("Error: no right on this folder");
            //The reports of a Repository Folder are plain files that cannot be executed
            if (folder.IsRepository) throw new ValidationException(Translate("You have no right to schedule this report"));
            var file = getFileDetail(path);
            if (file.right == 0) throw new Exception("Error: no right on this report or file");
            int effRight = file.right;
            execPath = file.isshortcut ? resolveShortcut(path, out effRight) : path;
            if ((FolderRight)effRight < FolderRight.Schedule) throw new ValidationException(Translate("You have no right to schedule this report"));

            var filePath = getFullPath(execPath);
            if (!FileHelper.IsReportFile(filePath) || !System.IO.File.Exists(filePath)) throw new ValidationException(Translate("The report does not exist"));
            return Report.LoadFromFile(filePath, Repository, false);
        }

        static string getDeviceType(OutputDevice device)
        {
            if (device is OutputEmailDevice) return "email";
            if (device is OutputFolderDevice) return "folder";
            return "fileserver";
        }

        /// <summary>
        /// Folder path of a Folder device output: from the Web Report Server path to the path with the repository keywords
        /// </summary>
        string toOutputFolderPath(string swiPath)
        {
            var folder = getFolder(swiPath);
            if ((FolderRight)folder.right != FolderRight.Edit || folder.IsRepository || folder.type == "bin") throw new ValidationException(Translate("You have no right to write in this folder"));
            var physicalPath = getFullPath(folder.path).TrimEnd(Path.DirectorySeparatorChar);
            if (physicalPath.StartsWith(Repository.PersonalFolder, StringComparison.OrdinalIgnoreCase)) return Repository.SealPersonalRepositoryKeyword + physicalPath.Substring(Repository.PersonalFolder.Length);
            if (physicalPath.StartsWith(Repository.ReportsFolder, StringComparison.OrdinalIgnoreCase)) return Repository.SealReportsRepositoryKeyword + physicalPath.Substring(Repository.ReportsFolder.Length);
            throw new ValidationException(Translate("You have no right to write in this folder"));
        }

        /// <summary>
        /// Folder path of a Folder device output: from the path with the repository keywords to the Web Report Server path
        /// </summary>
        string fromOutputFolderPath(string folderPath)
        {
            var physicalPath = Repository.ReplaceRepositoryKeyword(folderPath);
            var personalFolder = Repository.GetPersonalFolder(WebUser);
            if (physicalPath.StartsWith(personalFolder, StringComparison.OrdinalIgnoreCase)) return SWIFolder.GetPersonalRoot() + physicalPath.Substring(personalFolder.Length);
            if (physicalPath.StartsWith(Repository.ReportsFolder, StringComparison.OrdinalIgnoreCase))
            {
                var result = physicalPath.Substring(Repository.ReportsFolder.Length);
                return string.IsNullOrEmpty(result) ? Path.DirectorySeparatorChar.ToString() : result;
            }
            return "";
        }

        string getFormatName(ReportFormat format)
        {
            return Repository.TranslateReport(Helper.GetEnumDescription(typeof(ReportFormat), format));
        }

        static string toDateString(DateTime date)
        {
            return date == DateTime.MinValue || date == DateTime.MaxValue ? "" : date.ToString(WebScheduleDateFormat, CultureInfo.InvariantCulture);
        }

        string toDisplayDate(DateTime date)
        {
            return date == DateTime.MinValue || date == DateTime.MaxValue ? "" : date.ToString("g", Repository.CultureInfo);
        }

        SWIWebSchedule toSWIWebSchedule(SealSchedule schedule, bool forEdit)
        {
            var output = schedule.Output;
            var device = Repository.Devices.FirstOrDefault(i => i.GUID == output.DeviceGUID);
            var result = new SWIWebSchedule()
            {
                guid = schedule.GUID,
                name = schedule.Name,
                enabled = schedule.Enabled,
                path = schedule.WebPath,
                reportname = Repository.TranslateFileName(getFullPath(schedule.WebPath)),
                deviceguid = output.DeviceGUID,
                devicename = device != null ? Repository.TranslateDevice(device.Name, device.Name) : Translate("Unknown device"),
                devicetype = device != null ? getDeviceType(device) : "",
                format = output.Format,
                formatname = getFormatName(output.Format),
                recurrence = getRecurrence(schedule),
                nextexecution = schedule.Enabled ? toDisplayDate(schedule.NextExecution) : "",
                lastexecution = toDisplayDate(schedule.LastExecution),
                lasterror = schedule.LastError
            };
            if (!forEdit) return result;

            result.folder = device is OutputFolderDevice ? fromOutputFolderPath(output.FolderPath) : output.FolderPath;
            result.file = string.IsNullOrEmpty(output.FileName) ? "" : output.FileName.Replace(Repository.SealReportDisplayNameKeyword, Repository.SealReportDisplayNameKeyword2);
            result.to = output.EmailTo;
            result.cc = output.EmailCC;
            result.bcc = output.EmailBCC;
            result.subject = output.EmailSubject;
            result.body = output.EmailBody;
            result.html = output.EmailHtmlBody;
            result.zip = output.ZipResult;
            result.haszippassword = !string.IsNullOrEmpty(output.ZipPassword);
            result.cancel = output.CancelIfNoRecords;
            result.type = schedule.Type;
            result.start = toDateString(schedule.Start);
            result.end = toDateString(schedule.End);
            result.interval = schedule.Type == TriggerType.Weekly ? schedule.WeeksInterval : schedule.DaysInterval;
            result.weekdays = schedule.Weekdays;
            result.months = schedule.Months;
            result.days = schedule.Days;
            result.repeatinterval = (int)schedule.RepeatInterval.TotalMinutes;
            result.repeatduration = (int)schedule.RepeatDuration.TotalMinutes;
            return result;
        }

        /// <summary>
        /// Short description of the recurrence of a schedule
        /// </summary>
        string getRecurrence(SealSchedule schedule)
        {
            var culture = Repository.CultureInfo;
            var time = schedule.Start.ToString("t", culture);
            string result;
            switch (schedule.Type)
            {
                case TriggerType.Time:
                    result = Translate("One time") + " " + schedule.Start.ToString("g", culture);
                    break;
                case TriggerType.Daily:
                    result = (schedule.DaysInterval > 1 ? string.Format(Translate("Every {0} days"), schedule.DaysInterval) : Translate("Daily")) + " " + time;
                    break;
                case TriggerType.Weekly:
                    var days = string.Join(", ", schedule.Weekdays.OrderBy(i => i == 0 ? 7 : i).Select(i => culture.DateTimeFormat.AbbreviatedDayNames[i]));
                    result = (schedule.WeeksInterval > 1 ? string.Format(Translate("Every {0} weeks"), schedule.WeeksInterval) : Translate("Weekly")) + " (" + days + ") " + time;
                    break;
                default:
                    var monthDays = string.Join(", ", schedule.Days.OrderBy(i => i).Select(i => i == 32 ? Translate("Last") : i.ToString()));
                    var months = schedule.Months.Length == 12 ? "" : " - " + string.Join(", ", schedule.Months.OrderBy(i => i).Select(i => culture.DateTimeFormat.AbbreviatedMonthNames[i - 1]));
                    result = Translate("Monthly") + " (" + monthDays + months + ") " + time;
                    break;
            }
            if (schedule.RepeatInterval != TimeSpan.Zero) result += ", " + string.Format(Translate("repeated every {0} minutes"), (int)schedule.RepeatInterval.TotalMinutes);
            return result;
        }

        /// <summary>
        /// Checks and sets the definition of a web schedule from the Web Report Server
        /// </summary>
        void setWebSchedule(SealSchedule schedule, SWIWebSchedule swi, Report report, string execPath)
        {
            var output = schedule.Output;
            schedule.Name = string.IsNullOrWhiteSpace(swi.name) ? report.ExecutionName : swi.name.Trim();
            schedule.Enabled = swi.enabled;

            //Output
            var device = Repository.Devices.FirstOrDefault(i => i.GUID == swi.deviceguid);
            if (device == null || !WebUser.CanAccessDevice(device)) throw new ValidationException(Translate("Please select an output device"));
            if (!Repository.ResultAllowedFormats.Contains(swi.format)) throw new ValidationException(Translate("The format is not allowed"));
            output.DeviceGUID = device.GUID;
            output.Format = swi.format;
            output.CancelIfNoRecords = swi.cancel;
            output.ZipResult = swi.zip;
            if (!swi.zip || swi.removezippassword) output.ZipPassword = null;
            else if (!string.IsNullOrEmpty(swi.zippassword)) output.ZipPassword = swi.zippassword;

            output.FolderPath = null;
            output.FileName = null;
            output.EmailTo = output.EmailCC = output.EmailBCC = output.EmailSubject = output.EmailBody = null;
            output.EmailHtmlBody = false;
            if (device is OutputEmailDevice)
            {
                if (string.IsNullOrWhiteSpace(swi.to)) throw new ValidationException(Translate("Please define the email destination"));
                output.EmailTo = swi.to.Trim();
                output.EmailCC = swi.cc;
                output.EmailBCC = swi.bcc;
                output.EmailSubject = swi.subject;
                output.EmailBody = swi.body;
                output.EmailHtmlBody = swi.html;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(swi.file)) throw new ValidationException(Translate("Please define the file name"));
                if (swi.file.IndexOfAny(new char[] { '\\', '/' }) >= 0 || swi.file.Contains("..")) throw new ValidationException(Translate("The file name is invalid"));
                output.FileName = swi.file.Trim().Replace(Repository.SealReportDisplayNameKeyword2, Repository.SealReportDisplayNameKeyword);
                //The file name is formatted with the execution date time (e.g. {0:yyyy-MM-dd})
                try
                {
                    string.Format(output.FileName, DateTime.Now);
                }
                catch
                {
                    throw new ValidationException(Translate("The format of the execution date in the file name is invalid"));
                }
                if (device is OutputFolderDevice)
                {
                    if (string.IsNullOrEmpty(swi.folder)) throw new ValidationException(Translate("Please select a folder"));
                    output.FolderPath = toOutputFolderPath(swi.folder);
                }
                else
                {
                    var directories = (device is OutputFileServerDevice fileServer && !string.IsNullOrEmpty(fileServer.Directories)) ? fileServer.DirectoriesArray
                        : (device is OutputSharePointDevice sharePoint && !string.IsNullOrEmpty(sharePoint.Directories)) ? sharePoint.DirectoriesArray : null;
                    if (directories != null && !directories.Contains(swi.folder)) throw new ValidationException(Translate("Please select a directory"));
                    output.FolderPath = directories != null ? swi.folder : "/";
                }
            }

            //Schedule
            if (!DateTime.TryParseExact(swi.start, WebScheduleDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start)) throw new ValidationException(Translate("The start date is invalid"));
            var end = DateTime.MinValue;
            if (!string.IsNullOrEmpty(swi.end))
            {
                if (!DateTime.TryParseExact(swi.end, WebScheduleDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out end)) throw new ValidationException(Translate("The end date is invalid"));
                if (end < start) throw new ValidationException(Translate("The end date is before the start date"));
            }
            if (!Enum.IsDefined(typeof(TriggerType), swi.type)) throw new ValidationException(Translate("The recurrence is invalid"));
            if (swi.interval < 1) throw new ValidationException(Translate("The interval must be greater than 0"));
            var weekdays = (swi.weekdays ?? new int[0]).Where(i => i >= 0 && i <= 6).Distinct().ToArray();
            var months = (swi.months ?? new int[0]).Where(i => i >= 1 && i <= 12).Distinct().ToArray();
            var days = (swi.days ?? new int[0]).Where(i => i >= 1 && i <= 32).Distinct().ToArray();
            if (swi.type == TriggerType.Weekly && weekdays.Length == 0) throw new ValidationException(Translate("Please select at least one day of the week"));
            if (swi.type == TriggerType.Monthly && (months.Length == 0 || days.Length == 0)) throw new ValidationException(Translate("Please select at least one month and one day"));
            if (swi.repeatinterval < 0 || swi.repeatduration < 0 || (swi.repeatduration > 0 && swi.repeatinterval > swi.repeatduration)) throw new ValidationException(Translate("Inconsistent duration or interval for the repetition"));

            schedule.Type = swi.type;
            schedule.Start = start;
            schedule.End = end;
            schedule.DaysInterval = swi.type == TriggerType.Daily ? swi.interval : 1;
            schedule.WeeksInterval = swi.type == TriggerType.Weekly ? swi.interval : 1;
            if (weekdays.Length > 0) schedule.Weekdays = weekdays;
            if (months.Length > 0) schedule.Months = months;
            if (days.Length > 0) schedule.Days = days;
            schedule.RepeatInterval = TimeSpan.FromMinutes(swi.repeatinterval);
            schedule.RepeatDuration = swi.repeatinterval > 0 ? TimeSpan.FromMinutes(swi.repeatduration) : TimeSpan.Zero;
            schedule.CalculateNextExecution();

            //Owner and report
            schedule.UserName = WebUser.WebUserName;
            schedule.WebHostName = WebUser.WebHostName;
            schedule.WebPath = execPath;
            schedule.ReportGUID = report.GUID;
            schedule.ReportPath = report.FilePath.Replace(Repository.ReportsFolder, "");
            schedule.LastError = null;
        }
    }
}
