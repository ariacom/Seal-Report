using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Seal.Model
{
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
                foreach (var schedule in _schedules.Values.Where( i => !File.Exists(i.FilePath)).ToList()) {
                    _schedules.Remove(schedule.GUID);
                }
            }
        }

        static bool Running = true;

        static SealReportScheduler _instance = null;
        /// <summary>
        /// A general static instance of the repository
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
            if (File.Exists(refSchedule.ReportPath)) report = Report.LoadFromFile(refSchedule.ReportPath, Repository.Instance);

            if (!File.Exists(refSchedule.ReportPath) || (report != null && report.GUID != refSchedule.ReportGUID))
            {
                //Report has been moved or renamed: search report from its GUID in the report folder
                report = Repository.Instance.FindReport(Repository.Instance.ReportsFolder, refSchedule.ReportGUID);
                if (report == null)
                {
                    //Remove the schedules of the report
                    var reportSchedules = _schedules.Values.Where(i => i.ReportGUID == refSchedule.ReportGUID).ToList();
                    foreach (var schedule in reportSchedules)
                    {
                        File.Delete(schedule.FilePath);
                        lock (_schedules)
                        {
                            _schedules.Remove(schedule.GUID);
                        }
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
            var schedule = param as SealSchedule;
            var report = getScheduledReport(schedule);
            var reportSchedule = getReportSchedule(report, schedule.GUID);
            ReportExecution.ExecuteReportSchedule(schedule.GUID, report, reportSchedule);

            if (File.GetLastWriteTime(schedule.FilePath) == schedule.LastModification)
            {
                schedule.CalculateNextExecution();
                SaveSchedule(schedule);
                schedule.BeingExecuted = false;
            }
            else
            {
                //Schedule modified from another editor...
                schedule.BeingExecuted = false;
                loadSchedules();
            }
        }

        public void Run()
        {
            Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Starting Report Scheduler");
            Audit.LogEventAudit(AuditType.EventServer, "Starting Report Scheduler");
            DateTime lastLoad = DateTime.MinValue;
            while(Running)
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
        public void Shutdown()
        {
            Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Ending Report Scheduler");
            Running = false;
            Thread.Sleep(4000);
        }

        public void SaveSchedule(SealSchedule schedule)
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
                schedule.SaveToFile();
            }
        }

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

        public SealSchedule CreateSchedule(string guid, string name, Report report)
        {
            var sealSchedule = new SealSchedule() { GUID = guid };
            sealSchedule.ReportPath = report.FilePath;
            sealSchedule.ReportGUID = report.GUID;
            sealSchedule.FilePath = Path.Combine(report.Repository.SchedulesFolder, name) + ".xml";
            return sealSchedule;
        }

        public List<SealSchedule> GetSchedules()
        {
            loadSchedules();
            return _schedules.Values.ToList();
        }
    }
}