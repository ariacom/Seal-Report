//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Helpers;
using DynamicTypeDescriptor;
using System.Windows.Forms;
using System.Drawing.Design;
using Seal.Forms;
using System.ComponentModel.Design;
using Microsoft.Win32.TaskScheduler;
using System.IO;
using System.Collections.Generic;

namespace Seal.Model
{
    /// <summary>
    /// A ReportSchedule defines a schedule on a ReportOutput. Schedules are using Tasks of the Windows Task Scheduler.
    /// </summary>
    public class ReportSchedule : ReportComponent
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("OutputName").SetIsBrowsable(!IsTasksSchedule);
                GetProperty("IsTasksSchedule").SetIsBrowsable(IsTasksSchedule);

                //Seal scheduler
                GetProperty("SealEnabled").SetIsBrowsable(Report.Repository.UseWebScheduler);
                GetProperty("SealStart").SetIsBrowsable(Report.Repository.UseWebScheduler);
                GetProperty("SealEnd").SetIsBrowsable(Report.Repository.UseWebScheduler);
                GetProperty("SealType").SetIsBrowsable(Report.Repository.UseWebScheduler);
                GetProperty("SealDaysInterval").SetIsBrowsable(Report.Repository.UseWebScheduler && SealType == TriggerType.Daily);
                GetProperty("SealWeeksInterval").SetIsBrowsable(Report.Repository.UseWebScheduler && SealType == TriggerType.Weekly);
                GetProperty("SealWeekdays").SetIsBrowsable(Report.Repository.UseWebScheduler && SealType == TriggerType.Weekly);
                GetProperty("SealMonths").SetIsBrowsable(Report.Repository.UseWebScheduler && SealType == TriggerType.Monthly);
                GetProperty("SealDays").SetIsBrowsable(Report.Repository.UseWebScheduler && SealType == TriggerType.Monthly);
                GetProperty("SealRepeatInterval").SetIsBrowsable(Report.Repository.UseWebScheduler);
                GetProperty("SealRepeatDuration").SetIsBrowsable(Report.Repository.UseWebScheduler);
                GetProperty("SealNextExecution").SetIsBrowsable(Report.Repository.UseWebScheduler);

                GetProperty("IsEnabled").SetIsBrowsable(!Report.Repository.UseWebScheduler);
                GetProperty("LastRunTime").SetIsBrowsable(!Report.Repository.UseWebScheduler);
                GetProperty("NextRunTime").SetIsBrowsable(!Report.Repository.UseWebScheduler);

                GetProperty("NotificationEmailSubject").SetIsBrowsable(true);
                GetProperty("NotificationEmailBody").SetIsBrowsable(true);
                GetProperty("NotificationEmailFrom").SetIsBrowsable(true);
                GetProperty("NotificationEmailTo").SetIsBrowsable(true);

                GetProperty("ErrorNumberOfRetries").SetIsBrowsable(true);
                GetProperty("ErrorMinutesBetweenRetries").SetIsBrowsable(true);
                GetProperty("ErrorEmailSubject").SetIsBrowsable(true);
                GetProperty("ErrorEmailFrom").SetIsBrowsable(true);
                GetProperty("ErrorEmailTo").SetIsBrowsable(true);
                GetProperty("ErrorEmailSendMode").SetIsBrowsable(true);

                //Helpers
                GetProperty("HelperEditProperties").SetIsBrowsable(!Report.Repository.UseWebScheduler);
                GetProperty("HelperRunTaskScheduler").SetIsBrowsable(!Report.Repository.UseWebScheduler);

                GetProperty("ErrorMinutesBetweenRetries").SetIsReadOnly(ErrorNumberOfRetries <= 0);
                GetProperty("ErrorEmailSubject").SetIsReadOnly(string.IsNullOrEmpty(ErrorEmailTo));
                GetProperty("ErrorEmailFrom").SetIsReadOnly(string.IsNullOrEmpty(ErrorEmailTo));
                GetProperty("ErrorEmailSendMode").SetIsReadOnly(string.IsNullOrEmpty(ErrorEmailTo));

                GetProperty("NotificationEmailSubject").SetIsReadOnly(string.IsNullOrEmpty(NotificationEmailTo));
                GetProperty("NotificationEmailBody").SetIsReadOnly(string.IsNullOrEmpty(NotificationEmailTo));
                GetProperty("NotificationEmailFrom").SetIsReadOnly(string.IsNullOrEmpty(NotificationEmailTo));

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        /// <summary>
        /// Create a basic ReportSchedule
        /// </summary>
        /// <returns></returns>
        public static ReportSchedule Create()
        {
            return new ReportSchedule() { GUID = Guid.NewGuid().ToString() };
        }

        /// <summary>
        /// Identifier of the output
        /// </summary>
        public string OutputGUID { get; set; }

        /// <summary>
        /// If true, the schedule is executed without output definition. It may be used to schedule reports having only tasks. The default view of the report is used for the execution.
        /// </summary>
        [XmlIgnore]
        [Category("Definition"), DisplayName("Is Schedule for Report Tasks"), Description("If true, the schedule is executed without output definition. It may be used to schedule reports having only tasks. The default view of the report is used for the execution."), Id(1, 1)]
        public bool IsTasksSchedule
        {
            get { return string.IsNullOrEmpty(OutputGUID); }
        }

        /// <summary>
        /// True if the schedule is enabled
        /// </summary>
        [Category("Definition"), DisplayName("Is enabled"), Description("True if the schedule is enabled."), Id(2, 1)]
        [XmlIgnore]
        [DefaultValue(false)]
        public bool SealEnabled
        {
            get
            {
                return SealSchedule.Enabled;
            }
            set
            {
                SealSchedule.Enabled = value;
            }
        }

        /// <summary>
        /// Start date and time of the schedule
        /// </summary>
        [Category("Definition"), DisplayName("Start date"), Description("Start date and time of the schedule."), Id(3, 1)]
        [XmlIgnore]
        public DateTime SealStart
        {
            get
            {
                return SealSchedule.Start;
            }
            set
            {
                SealSchedule.Start = value;
                SealSchedule.CalculateNextExecution();
                UpdateEditor(); //!NETCore
            }
        }

        /// <summary>
        /// End date and time of the schedule
        /// </summary>
        [Category("Definition"), DisplayName("End date"), Description("End date and time of the schedule."), Id(4, 1)]
        [XmlIgnore]
        public DateTime SealEnd
        {
            get
            {
                return SealSchedule.End;
            }
            set
            {
                SealSchedule.End = value;
                SealSchedule.CalculateNextExecution();
                UpdateEditor(); //!NETCore
            }
        }

        /// <summary>
        /// Type of schedule trigger
        /// </summary>
        [Category("Definition"), DisplayName("Trigger type"), Description("Type of schedule trigger."), Id(5, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(TriggerType.Daily)]
        [XmlIgnore]
        public TriggerType SealType
        {
            get
            {
                return SealSchedule.Type;
            }
            set
            {
                SealSchedule.Type = value;
                SealSchedule.CalculateNextExecution();
                UpdateEditor(); //!NETCore
            }
        }

        /// <summary>
        /// Number of days
        /// </summary>
        [Category("Definition"), DisplayName("Recur every (days)"), Description("Number of days."), Id(6, 1)]
        [XmlIgnore]
        public int SealDaysInterval
        {
            get
            {
                return SealSchedule.DaysInterval;
            }
            set
            {
                SealSchedule.DaysInterval = value;
                SealSchedule.CalculateNextExecution();
                UpdateEditor(); //!NETCore
            }
        }

        /// <summary>
        /// Number of weeks
        /// </summary>
        [Category("Definition"), DisplayName("Recur every (weeks)"), Description("Number of weeks."), Id(7, 1)]
        [XmlIgnore]
        public int SealWeeksInterval
        {
            get
            {
                return SealSchedule.WeeksInterval;
            }
            set
            {
                SealSchedule.WeeksInterval = value;
                SealSchedule.CalculateNextExecution();
                UpdateEditor(); //!NETCore
            }
        }

        /// <summary>
        /// Days of the week to execute the schedule
        /// </summary>
        [Category("Definition"), DisplayName("Week days"), Description("Days of the week to execute the schedule."), Id(8, 1)]
        [Editor(typeof(ScheduleCollectionEditor), typeof(UITypeEditor))]
        [XmlIgnore]
        public int[] SealWeekdays
        {
            get
            {
                return SealSchedule.Weekdays;
            }
            set
            {
                SealSchedule.Weekdays = value;
                SealSchedule.CalculateNextExecution();
                UpdateEditor(); //!NETCore
            }
        }

        /// <summary>
        /// Months to execute the schedule
        /// </summary>
        [Category("Definition"), DisplayName("Months"), Description("Months to execute the schedule."), Id(9, 1)]
        [Editor(typeof(ScheduleCollectionEditor), typeof(UITypeEditor))]
        [XmlIgnore]
        public int[] SealMonths
        {
            get
            {
                return SealSchedule.Months;
            }
            set
            {
                SealSchedule.Months = value;
                SealSchedule.CalculateNextExecution();
                UpdateEditor(); //!NETCore
            }
        }

        /// <summary>
        /// Days of the month to execute the schedule
        /// </summary>
        [Category("Definition"), DisplayName("Days of the month"), Description("Days of the month to execute the schedule."), Id(10, 1)]
        [Editor(typeof(ScheduleCollectionEditor), typeof(UITypeEditor))]
        [XmlIgnore]
        public int[] SealDays
        {
            get
            {
                return SealSchedule.Days;
            }
            set
            {
                SealSchedule.Days = value;
                SealSchedule.CalculateNextExecution();
                UpdateEditor(); //!NETCore
            }
        }

        /// <summary>
        /// Interval of the schedule repetition
        /// </summary>
        [Category("Definition"), DisplayName("Repeat schedule every"), Description("Interval of the schedule repetition."), Id(11, 1)]
        [XmlIgnore]
        [DefaultValue("None")]
        [TypeConverter(typeof(ScheduleRepeatConverter))]
        public string SealRepeatInterval
        {
            get
            {
                return Helper.FromTimeSpan(SealSchedule.RepeatInterval, "None", null);
            }
            set
            {
                SealSchedule.RepeatInterval = Helper.ToTimeSpan(value, null);
                SealSchedule.CalculateNextExecution();
                UpdateEditor(); //!NETCore
            }
        }

        /// <summary>
        /// Duration of the schedule repetition
        /// </summary>
        [Category("Definition"), DisplayName("For a duration of"), Description("Duration of the schedule repetition."), Id(12, 1)]
        [XmlIgnore]
        [DefaultValue("Indefinitely")]
        [TypeConverter(typeof(ScheduleRepeatConverter))]
        public string SealRepeatDuration
        {
            get
            {
                return Helper.FromTimeSpan(SealSchedule.RepeatDuration, "Indefinitely", null);
            }
            set
            {
                SealSchedule.RepeatDuration = Helper.ToTimeSpan(value, null);
                SealSchedule.CalculateNextExecution();
                UpdateEditor(); //!NETCore
            }
        }

        /// <summary>
        /// Next execution planned for the schedule
        /// </summary>
        [Category("Definition"), DisplayName("Next execution"), Description("Next execution planned for the schedule."), Id(13, 1)]
        [XmlIgnore]
        public DateTime? SealNextExecution
        {
            get
            {
                if (SealSchedule.NextExecution == DateTime.MaxValue) return null;
                return SealSchedule.NextExecution;
            }
        }

        /// <summary>
        /// Current schedule if the Seal Scheduler is used
        /// </summary>
        SealSchedule _sealSchedule = null;
        [XmlIgnore]
        public SealSchedule SealSchedule
        {
            get
            {
                if (_sealSchedule == null)
                {
                    _sealSchedule = SealReportScheduler.Instance.GetSchedule(GUID);
                    if (_sealSchedule == null) _sealSchedule = SealReportScheduler.Instance.CreateSchedule(GUID, TaskName, Report);
                }
                return _sealSchedule;
            }
            set
            {
                _sealSchedule = value;
            }
        }

        /// <summary>
        /// Returns a given line from the Task Source Detail
        /// </summary>
        static public string GetTaskSourceDetail(string source, int index)
        {
            string[] sources = source.Split('\n');
            if (sources.Length > index) return sources[index].Trim();
            return ""; ;
        }

        /// <summary>
        /// Days of the Seal Schedule in a text
        /// </summary>
        [XmlIgnore]
        public string SealDaysString
        {
            get
            {
                var result = "";
                if (SealType == TriggerType.Monthly) foreach (var day in SealDays) Helper.AddValue(ref result, ",", day == 32 ? "Last" : day.ToString());
                return result;
            }
        }

        /// <summary>
        /// Weekdays of the Seal Schedule a text
        /// </summary>
        [XmlIgnore]
        public string SealWeekdaysString
        {
            get
            {
                var result = "";

                if (SealType == TriggerType.Weekly) foreach (var day in SealWeekdays) Helper.AddValue(ref result, ",", Report.CultureInfo.DateTimeFormat.DayNames[day]);
                
                return result;
            }
        }

        /// <summary>
        /// Months of the Seal Schedule a text
        /// </summary>
        [XmlIgnore]
        public string SealMonthsString
        {
            get
            {
                var result = "";
                if (SealType == TriggerType.Monthly) foreach (var m in SealMonths) Helper.AddValue(ref result, ",", Report.CultureInfo.DateTimeFormat.MonthNames[m]);
                return result;
            }
        }

        /// <summary>
        /// Task source name
        /// </summary>
        [XmlIgnore]
        public string TaskSource
        {
            get
            {
                return string.Format("{0}\n{1}\n{2}\n{3}", Report.FilePath, Report.GUID, OutputGUID, GUID);
            }
        }

        /// <summary>
        /// Task name as used in the Windows Task Scheduler or for the Seal Schedule file name.
        /// </summary>
        [XmlIgnore]
        public string TaskName
        {
            get
            {
                var result = Helper.CleanFileName(string.Format("[{0}] {1} {2} {3}", Path.GetFileNameWithoutExtension(Report.FilePath), Report.DisplayNameEx, Name, GUID));
                if (result.Length > 120) result = Helper.CleanFileName(string.Format("[{0}] {1} {2}", Path.GetFileNameWithoutExtension(Report.FilePath), Name, GUID));
                if (result.Length > 120) result = Helper.CleanFileName(string.Format("[{0}] {1}", Path.GetFileNameWithoutExtension(Report.FilePath), GUID));
                return result.Replace("'", "");
            }
        }

        /// <summary>
        /// Indicates if the task is enabled. Tasks can be enabled or disabled using the Task Scheduler Microsoft Management Console.
        /// </summary>
        [XmlIgnore]
        [Category("Information"), DisplayName("Is enabled"), Description("Indicates if the task is enabled. Tasks can be enabled or disabled using the Task Scheduler Microsoft Management Console."), Id(2, 2)]
        public bool IsEnabled
        {
            get
            {
                try
                {
                    return Task.Enabled;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Last time the task was executed
        /// </summary>
        [XmlIgnore]
        [Category("Information"), DisplayName("Last run time"), Description("Last time the task was executed."), Id(3, 2)]
        public DateTime? LastRunTime
        {
            get
            {
                try
                {
                    return Task.LastRunTime;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Next time the task will be executed
        /// </summary>
        [XmlIgnore]
        [Category("Information"), DisplayName("Next run time"), Description("Next time the task will be executed."), Id(4, 2)]
        public DateTime? NextRunTime
        {
            get
            {
                try
                {
                    return Task.NextRunTime;
                }
                catch
                {
                    return null;
                }
            }
        }

        private string _notificationEmailTo;
        /// <summary>
        /// The destination (To) email addresses used for the email notification in case of success. One per line or separated by semi-column.
        /// </summary>
        [Category("Email notification in case of success"), DisplayName("TO addresses"), Description("The destination (To) email addresses used for the email notification in case of success. One per line or separated by semi-column."), Id(2, 3)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string NotificationEmailTo
        {
            get { return _notificationEmailTo; }
            set
            {
                _notificationEmailTo = value;
                UpdateEditorAttributes();  //!NETCore 
            }
        }

        /// <summary>
        /// The subject of the email sent in case of success. If empty, the report name is used.
        /// </summary>
        [Category("Email notification in case of success"), DisplayName("Subject"), Description("The subject of the email sent in case of success. If empty, the report name is used."), Id(3, 3)]
        public string NotificationEmailSubject { get; set; }

        /// <summary>
        /// The body of the email sent in case of success. If empty, a default text is used.
        /// </summary>
        [Category("Email notification in case of success"), DisplayName("Body"), Description("The body of the email sent in case of success. If empty, a default text is used."), Id(4, 3)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string NotificationEmailBody { get; set; }

        /// <summary>
        /// The sender (From) email address used to send the email in case of success. If empty the default address configured in the device is used. Make sure that the SMTP server allows the new address.
        /// </summary>
        [Category("Email notification in case of success"), DisplayName("Sender address"), Description("The sender (From) email address used to send the email in case of success. If empty the default address configured in the device is used. Make sure that the SMTP server allows the new address."), Id(5, 3)]
        public string NotificationEmailFrom { get; set; }

        int _errorNumberOfRetries = 0;
        /// <summary>
        /// The maximum number of retries in case of error
        /// </summary>
        [DefaultValue(0)]
        [Category("Failover: Retries"), DisplayName("Number of retries"), Description("The maximum number of retries in case of error."), Id(2, 5)]
        public int ErrorNumberOfRetries
        {
            get { return Math.Max(_errorNumberOfRetries, 0); }
            set
            {
                _errorNumberOfRetries = Math.Max(value, 0);
                UpdateEditorAttributes();
            }
        }

        /// <summary>
        /// The number of minutes elapsed between a retry
        /// </summary>
        [DefaultValue(10)]
        [Category("Failover: Retries"), DisplayName("Minutes between each retry"), Description("The number of minutes elapsed between a retry."), Id(3, 5)]
        public int ErrorMinutesBetweenRetries { get; set; } = 10;

        private string _errorEmailTo;
        /// <summary>
        /// The destination (To) email addresses used for the email in case of error. One per line or separated by semi-column.
        /// </summary>
        [Category("Failover: Email notification in case of error"), DisplayName("TO addresses"), Description("The destination (To) email addresses used for the email in case of error. One per line or separated by semi-column."), Id(2, 7)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string ErrorEmailTo
        {
            get { return _errorEmailTo; }
            set { _errorEmailTo = value; UpdateEditorAttributes(); }
        }

        /// <summary>
        /// Specify if the email is sent for the first, the last or for each failure
        /// </summary>
        [Category("Failover: Email notification in case of error"), DisplayName("Send Email mode"), Description("Specify if the email is sent for the first, the last or for each failure."), Id(3, 7)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public FailoverEmailMode ErrorEmailSendMode { get; set; } = FailoverEmailMode.All;
        public bool ShouldSerializeErrorEmailSendMode() { return !string.IsNullOrEmpty(ErrorEmailTo); }

        /// <summary>
        /// The subject of the email sent in case of error. If empty, the report name is used.
        /// </summary>
        [Category("Failover: Email notification in case of error"), DisplayName("Subject"), Description("The subject of the email sent in case of error. If empty, the report name is used."), Id(4, 7)]
        public string ErrorEmailSubject { get; set; }

        /// <summary>
        /// The sender (From) email address used to send the email in case of error. If empty the default address configured in the device is used. Make sure that the SMTP server allows the new address.
        /// </summary>
        [Category("Failover: Email notification in case of error"), DisplayName("Sender address"), Description("The sender (From) email address used to send the email in case of error. If empty the default address configured in the device is used. Make sure that the SMTP server allows the new address."), Id(5, 7)]
        public string ErrorEmailFrom { get; set; }

        /// <summary>
        /// Synchronize the task with the Windows Task Scheduler
        /// </summary>
        public void SynchronizeTask()
        {
            if (Report.Repository.UseWebScheduler)
            {
                if (File.Exists(SealSchedule.FilePath))
                {
                    //Check if the name has changed
                    if (TaskName != Path.GetFileNameWithoutExtension(SealSchedule.FilePath))
                    {
                        var newPath = Path.Combine(Report.Repository.SchedulesFolder, TaskName) + ".xml";
                        File.Move(SealSchedule.FilePath, newPath);
                        SealSchedule.FilePath = newPath;
                    }
                }
                SealReportScheduler.Instance.SaveSchedule(SealSchedule, Report);
            }
            else
            {
                string description = string.Format("Schedule for the Tasks. Report '{0}'", Report.FilePath);
                if (!IsTasksSchedule) description = string.Format("Schedule for the output '{0}'. Report '{1}'", Output.Name, Report.FilePath);

                TaskDefinition definition = Task.Definition;
                if (definition.RegistrationInfo.Source != TaskSource || definition.RegistrationInfo.Description != description || TaskName != Task.Name)
                {
                    definition.RegistrationInfo.Source = TaskSource;
                    definition.RegistrationInfo.Description = description;
                    //If name has changed, we have to delete then insert it again...
                    string oldName = Task.Name;
                    if (!string.IsNullOrEmpty(oldName) && TaskName != oldName)
                    {
                        Report.TaskFolder.DeleteTask(oldName);
                        RegisterTaskDefinition(definition);
                    }
                    else
                    {
                        _task.RegisterChanges();
                    }
                }
            }
        }

        void RegisterTaskDefinition(TaskDefinition definition)
        {
            if (!Report.SchedulesWithCurrentUser)
            {
                definition.Principal.RunLevel = TaskRunLevel.Highest;
                _task = Report.TaskFolder.RegisterTaskDefinition(TaskName, definition, TaskCreation.CreateOrUpdate, "SYSTEM", null, TaskLogonType.ServiceAccount);
            }
            else
            {
                //default user
                //FUTURE required the user password...., 
                //definition.Principal.LogonType = TaskLogonType.InteractiveTokenOrPassword;
                _task = Report.TaskFolder.RegisterTaskDefinition(TaskName, definition);
            }
        }

        /// <summary>
        /// Find the Task from the Windows Task Scheduler
        /// </summary>
        public Task FindTask()
        {
            Task result = Report.TaskFolder.GetTasks().FirstOrDefault(i => i.Definition.RegistrationInfo.Source == TaskSource);
            foreach (var task in Report.TaskFolder.GetTasks())
            {
                if (!string.IsNullOrEmpty(task.Definition.RegistrationInfo.Source) && task.Definition.RegistrationInfo.Source.ToLower().Trim() == TaskSource.ToLower().Trim()) result = task;
            }

            if (result == null)
            {
                //check if the task is still existing (typically if the report was moved or renamed)
                foreach (Task task in Report.TaskFolder.GetTasks().Where(i => i.Name.EndsWith(GUID) && i.Definition.RegistrationInfo.Source.EndsWith(GUID)))
                {
                    bool ok = true;
                    string reportPath = GetTaskSourceDetail(task.Definition.RegistrationInfo.Source, 0);
                    if (File.Exists(reportPath))
                    {
                        try
                        {
                            //probably a report copy, the task should stay attached on the initial report
                            Report report = Report.LoadFromFile(reportPath, Report.Repository);
                            if (report.GUID == GetTaskSourceDetail(task.Definition.RegistrationInfo.Source, 1) && report.Schedules.Exists(i => i.GUID == GUID)) ok = false;
                        }
                        catch { }
                    }
                    if (ok)
                    {
                        //take this task 
                        result = task;
                        break;
                    }
                }
            }
            return result;
        }

        Task _task;
        /// <summary>
        /// The current Windows Task
        /// </summary>
        [XmlIgnore]
        public Task Task
        {
            get
            {
                if (_task == null)
                {
                    _task = FindTask();
                    if (_task == null)
                    {
                        Report.SchedulesModified = true;
                        if (Report.TaskFolder.GetTasks().FirstOrDefault(i => i.Name.EndsWith(GUID) && i.Definition.RegistrationInfo.Source.EndsWith(GUID)) != null)
                        {
                            //change my GUID as another schedule exists with this GUID
                            GUID = Guid.NewGuid().ToString();
                        }
                        //create task
                        TaskDefinition taskDefinition = (new TaskService()).NewTask();
                        taskDefinition.Triggers.Add(new DailyTrigger() { StartBoundary = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 9, 0, 0), Enabled = false });
                        string schedulerPath = Path.Combine(Report.Repository.Configuration.InstallationDirectory, Repository.SealTaskScheduler);
#if DEBUG
                        schedulerPath = Path.Combine(@"C:\_dev\Seal-Report\Projects\SealTaskScheduler\bin\x64\Debug", Repository.SealTaskScheduler);
#endif
                        taskDefinition.Actions.Add(new ExecAction(string.Format("\"{0}\"", schedulerPath), GUID, Helper.GetApplicationDirectory()));
                        RegisterTaskDefinition(taskDefinition);
                    }
                    SynchronizeTask();
                }
                return _task;
            }
            set { _task = value; }
        }

        /// <summary>
        /// Object that can be used at run-time for any purpose
        /// </summary>
        [XmlIgnore]
        public object Tag;

        /// <summary>
        /// ReportOutput of the schedule
        /// </summary>
        [XmlIgnore]
        public ReportOutput Output
        {
            get
            {
                return Report.Outputs.FirstOrDefault(i => i.GUID == OutputGUID);
            }
        }

        /// <summary>
        /// The report output name of this schedule
        /// </summary>
        [XmlIgnore]
        [Category("Definition"), DisplayName("Report output"), Description("The report output of this schedule."), Id(1, 1)]
        public string OutputName
        {
            get
            {
                return !IsTasksSchedule ? Output.Name : "";
            }
        }


        #region Helpers
        /// <summary>
        /// Editor Helper: Edit the report schedule properties
        /// </summary>
        [Category("Helpers"), DisplayName("Edit schedule properties"), Description("Edit the report schedule properties. Warning: Schedules may also be edited and managed using the Task Scheduler Microsoft Management Console."), Id(2, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperEditProperties
        {
            get { return "<Click to edit the schedule properties>"; }
        }

        /// <summary>
        /// Editor Helper: Run Task Scheduler MMC
        /// </summary>
        [Category("Helpers"), DisplayName("Run Task Scheduler MMC"), Description("Run the Task Scheduler Microsoft Management Console to manage schedule using the Windows interface."), Id(3, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperRunTaskScheduler
        {
            get { return "<Click to run the Task Scheduler Microsoft Management Console>"; }
        }

        #endregion

    }


}
