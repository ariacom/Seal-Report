using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Seal.Model
{
    /// <summary>
    /// Main Scheduler to execute the report schedules defined in a repository.
    /// </summary>
    public class SealReportScheduler
    {
        Dictionary<string, SealSchedule> _schedules = null;

        void loadSchedules()
        {
            if (_schedules == null) _schedules = new Dictionary<string, SealSchedule>();
            lock (_schedules)
            {
                foreach (var file in Directory.GetFiles(Repository.Instance.SchedulesFolder, "*.xml"))
                {
                    var schedule = _schedules.Values.FirstOrDefault(i => i.FilePath == file);
                    if (schedule == null || (!schedule.BeingExecuted && File.GetLastWriteTime(file) != schedule.LastModification))
                    {
                        try
                        {
                            schedule = SealSchedule.LoadFromFile(file);
                            //Adjust report path if necessary (case of copy between Windows OS to Linux OS)    
                            if (!File.Exists(schedule.ReportPath))
                            {
                                var newReportPath = Repository.Instance.ReportsFolder + schedule.ReportPath;
                                if (Path.DirectorySeparatorChar == '/' && newReportPath.Contains("\\")) newReportPath = newReportPath.Replace("\\", "/");
                                else if (Path.DirectorySeparatorChar == '\\' && newReportPath.Contains("/")) newReportPath = newReportPath.Replace("/", "\\");
                                if (File.Exists(newReportPath)) schedule.ReportPath = newReportPath;
                            }

                            if (_schedules.ContainsKey(schedule.GUID)) _schedules[schedule.GUID] = schedule;
                            else _schedules.Add(schedule.GUID, schedule);
                        }

                        catch (Exception ex)
                        {
                            Helper.WriteLogEntryScheduler(EventLogEntryType.Error, "Error loading '{0}'.\r\n{1}", file, ex.Message);
                        }
                    }
                }

                //Remove lost schedules
                foreach (var schedule in _schedules.Values.Where(i => !File.Exists(i.FilePath)).ToList())
                {
                    _schedules.Remove(schedule.GUID);
                }
            }
        }

        /// <summary>
        /// True if the scheduler is running
        /// </summary>
        public static bool Running = true;

        static SealReportScheduler _instance = null;
        /// <summary>
        /// A general static instance of the Scheduler.
        /// </summary>
        public static SealReportScheduler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SealReportScheduler();
                }
                return _instance;
            }
        }

        Report getScheduledReport(SealSchedule refSchedule)
        {
            Report report = null;
            var reportPath = refSchedule.ReportPath;
            if (!File.Exists(reportPath)) reportPath = Repository.Instance.ReportsFolder + reportPath;

            if (File.Exists(reportPath)) report = Report.LoadFromFile(reportPath, Repository.Instance);

            if (!File.Exists(reportPath) || (report != null && report.GUID != refSchedule.ReportGUID))
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Warning, "The report of schedule '{0}' does not exists ('{1}').\r\nSearching for the report in the repository...", refSchedule.FilePath, refSchedule.ReportPath);

                //Report has been moved or renamed: search report from its GUID in the report folder
                report = Repository.Instance.FindReport(Repository.Instance.ReportsFolder, refSchedule.ReportGUID);
                if (report != null)
                {
                    Helper.WriteLogEntryScheduler(EventLogEntryType.Warning, "Report of schedule '{0}' has changed to '{1}'", refSchedule.FilePath, report.FilePath);
                }
            }

            if (report == null)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Warning, "Report of schedule '{0}' not found, removing all schedules for the report GUID '{1}'", refSchedule.FilePath, refSchedule.ReportGUID);
                //Remove the schedules of the report
                var reportSchedules = _schedules.Values.Where(i => i.ReportGUID == refSchedule.ReportGUID).ToList();
                foreach (var schedule in reportSchedules)
                {
                    if (File.Exists(schedule.FilePath)) File.Delete(schedule.FilePath);
                    lock (_schedules)
                    {
                        _schedules.Remove(schedule.GUID);
                    }
                }
            }

            return report;
        }

        ReportSchedule getReportSchedule(Report report, string scheduleGUID)
        {
            ReportSchedule reportSchedule = report.Schedules.FirstOrDefault(i => i.GUID == scheduleGUID);
            if (reportSchedule == null)
            {
                //Remove the schedule
                var reportSchedules = _schedules.Values.Where(i => i.GUID == scheduleGUID).ToList();
                foreach (var schedule in reportSchedules)
                {
                    File.Delete(schedule.FilePath);
                    lock (_schedules)
                    {
                        _schedules.Remove(schedule.GUID);
                    }
                }
            }
            return reportSchedule;
        }

        private void ExecuteThread(object param)
        {
            try
            {
                var schedule = param as SealSchedule;
                var report = getScheduledReport(schedule);
                if (report != null)
                {
                    var reportSchedule = getReportSchedule(report, schedule.GUID);
                    if (reportSchedule != null)
                    {
                        ReportExecution.ExecuteReportSchedule(schedule.GUID, report, reportSchedule);

                        if (File.GetLastWriteTime(schedule.FilePath) == schedule.LastModification)
                        {
                            schedule.CalculateNextExecution();
                            SaveSchedule(schedule, report);
                            schedule.BeingExecuted = false;
                        }
                        else
                        {
                            //Schedule modified from another editor...
                            schedule.BeingExecuted = false;
                            loadSchedules();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Unexpected Scheduler Error:\r\n" + ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Run the scheduler
        /// </summary>
        public void Run()
        {
            try
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Starting Report Scheduler");
                Audit.LogEventAudit(AuditType.EventServer, "Starting Report Scheduler");
                DateTime lastLoad = DateTime.MinValue;

                if (!Repository.Instance.Configuration.UseSealScheduler)
                {
                    Helper.WriteLogEntryScheduler(EventLogEntryType.Error, "WARNING: The current Server Configuration is not set to 'Use Seal Report Scheduler'. This Scheduler will not run any report. Please check your configuration.");
                }
                else
                {
                    while (Running)
                    {
                        try
                        {
                            if (DateTime.Now > lastLoad.AddMinutes(1))
                            {
                                loadSchedules();
                                lastLoad = DateTime.Now;
                            }

                            foreach (var schedule in _schedules.Values.Where(i => !i.BeingExecuted))
                            {
                                if (schedule.IsReached())
                                {
                                    Debug.WriteLine("Running " + schedule.FilePath);
                                    schedule.BeingExecuted = true;
                                    Thread thread = new Thread(ExecuteThread);
                                    thread.Start(schedule);
                                }
                            }
                            Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            Helper.WriteLogEntryScheduler(EventLogEntryType.Error, ex.Message);
                        }
                    }
                    Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Report Scheduler is stopped");
                    Audit.LogEventAudit(AuditType.EventServer, "Report Scheduler is stopped");
                }
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Unexpected Scheduler Error:\r\n" + ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Stop the scheduler
        /// </summary>
        public void Shutdown()
        {
            Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Ending Report Scheduler");
            Running = false;
            Thread.Sleep(4000);
        }

        /// <summary>
        /// Save a schedule in the repository
        /// </summary>
        public void SaveSchedule(SealSchedule schedule, Report report)
        {
            loadSchedules();
            lock (_schedules)
            {
                if (_schedules.ContainsKey(schedule.GUID))
                {
                    File.Delete(_schedules[schedule.GUID].FilePath);
                    _schedules[schedule.GUID] = schedule;
                }
                else
                {
                    _schedules.Add(schedule.GUID, schedule);
                }

                //Remove repository path
                schedule.ReportPath = report.FilePath.Replace(Repository.Instance.ReportsFolder, "");
                schedule.SaveToFile();
            }
        }

        /// <summary>
        /// Delete a schedule from the repository
        /// </summary>
        public void DeleteSchedule(string guid)
        {
            loadSchedules();
            lock (_schedules)
            {
                if (_schedules.ContainsKey(guid))
                {
                    File.Delete(_schedules[guid].FilePath);
                    _schedules.Remove(guid);
                }
            }
        }

        /// <summary>
        /// Get a schedule from its identifier
        /// </summary>
        public SealSchedule GetSchedule(string guid)
        {
            loadSchedules();
            lock (_schedules)
            {
                if (_schedules.ContainsKey(guid))
                {
                    return _schedules[guid];
                }
            }
            return null;
        }

        /// <summary>
        /// Create a new schedule for a report
        /// </summary>
        public SealSchedule CreateSchedule(string guid, string name, Report report)
        {
            var sealSchedule = new SealSchedule() { GUID = guid };
            sealSchedule.ReportPath = report.FilePath;
            sealSchedule.ReportGUID = report.GUID;
            sealSchedule.FilePath = Path.Combine(report.Repository.SchedulesFolder, name) + ".xml";
            return sealSchedule;
        }

        /// <summary>
        /// List of repository schedules
        /// </summary>
        public List<SealSchedule> GetSchedules()
        {
            loadSchedules();
            return _schedules.Values.ToList();
        }
    }
}
