//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Helpers;
using DynamicTypeDescriptor;
using Seal.Converter;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing.Design;
using Seal.Forms;
using System.ComponentModel.Design;
using Microsoft.Win32.TaskScheduler;
using System.IO;

namespace Seal.Model
{
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

                GetProperty("IsEnabled").SetIsBrowsable(true);
                GetProperty("LastRunTime").SetIsBrowsable(true);
                GetProperty("NextRunTime").SetIsBrowsable(true);

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
                GetProperty("HelperEditProperties").SetIsBrowsable(true);
                GetProperty("HelperRunTaskScheduler").SetIsBrowsable(true);

                GetProperty("ErrorMinutesBetweenRetries").SetIsReadOnly(ErrorNumberOfRetries<=0);
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


        public static ReportSchedule Create()
        {
            return new ReportSchedule() { GUID = Guid.NewGuid().ToString() };
        }


        string _outputGUID = "";
        public string OutputGUID
        {
            get { return _outputGUID; }
            set { _outputGUID = value; }
        }

        [XmlIgnore]
        [Category("Definition"), DisplayName("Is Schedule for Report Tasks"), Description("If true, the schedule is executed without output definition. It may be used to schedule reports having only tasks. The default view of the report is used for the execution."), Id(1, 1)]
        public bool IsTasksSchedule
        {
            get {return string.IsNullOrEmpty(_outputGUID); }
        }

        static public string GetTaskSourceDetail(string source, int index)
        {
            string[] sources = source.Split('\n');
            if (sources.Length > index) return sources[index].Trim();
            return ""; ;
        }

        [XmlIgnore]
        public string TaskSource
        {
            get
            {
                return string.Format("{0}\n{1}\n{2}\n{3}", Report.FilePath, Report.GUID, _outputGUID, GUID);
            }
        }

        [XmlIgnore]
        public string TaskName
        {
            get
            {
                return string.Format("[{0}] {1} '{2}' {3}", Helper.CleanFileName(Path.GetFileNameWithoutExtension(Report.FilePath)), Report.DisplayNameEx, Name, GUID);
            }
        }

        [XmlIgnore]
        [Category("Information"), DisplayName("Is enabled"), Description("Indicates if the task is enabled. Tasks can be enabled or disabled using the Task Scheduler Microsoft Management Console."), Id(1,2)]
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

        [XmlIgnore]
        [Category("Information"), DisplayName("Last run time"), Description("Last time the task was executed."), Id(2, 2)]
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

        [XmlIgnore]
        [Category("Information"), DisplayName("Next run time"), Description("Next time the task will be executed."), Id(3, 2)]
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
        [Category("Email Notification in case of success"), DisplayName("TO addresses"), Description("The destination (To) email addresses used for the email notification in case of success. One per line or separated by semi-column."), Id(1, 3)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string NotificationEmailTo
        {
            get { return _notificationEmailTo; }
            set { _notificationEmailTo = value; UpdateEditorAttributes(); }
        }

        private string _notificationEmailSubject;
        [Category("Email Notification in case of success"), DisplayName("Subject"), Description("The subject of the email sent in case of success. If empty, the report name is used."), Id(2, 3)]
        public string NotificationEmailSubject
        {
            get { return _notificationEmailSubject; }
            set { _notificationEmailSubject = value; }
        }

        private string _notificationEmailBody;
        [Category("Email Notification in case of success"), DisplayName("Body"), Description("The body of the email sent in case of success. If empty, a default text is used."), Id(3, 3)]
        public string NotificationEmailBody
        {
            get { return _notificationEmailBody; }
            set { _notificationEmailBody = value; }
        }

        private string __notificationEmailFrom;
        [Category("Email Notification in case of success"), DisplayName("Sender address"), Description("The sender (From) email address used to send the email in case of success. If empty the default address configured in the device is used. Make sure that the SMTP server allows the new address."), Id(4, 3)]
        public string NotificationEmailFrom
        {
            get { return __notificationEmailFrom; }
            set { __notificationEmailFrom = value; }
        }



        int _errorNumberOfRetries = 0;
        [Category("Failover: Retries"), DisplayName("Number of retries"), Description("The maximum number of retries in case of error."), Id(1, 5)]
        public int ErrorNumberOfRetries
        {
            get { return Math.Max(_errorNumberOfRetries,0); }
            set { _errorNumberOfRetries = Math.Max(value,0); UpdateEditorAttributes(); }
        }

        int _errorMinutesBetweenRetries = 10;
        [Category("Failover: Retries"), DisplayName("Minutes between each retry"), Description("The number of minutes elapsed between a retry."), Id(2, 5)]
        public int ErrorMinutesBetweenRetries
        {
            get { return _errorMinutesBetweenRetries; }
            set { _errorMinutesBetweenRetries = value; }
        }

        private string _errorEmailTo;
        [Category("Failover: Email Notification in case of error"), DisplayName("TO addresses"), Description("The destination (To) email addresses used for the email in case of error. One per line or separated by semi-column."), Id(1, 7)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string ErrorEmailTo
        {
            get { return _errorEmailTo; }
            set { _errorEmailTo = value; UpdateEditorAttributes(); }
        }

        private FailoverEmailMode _errorEmailSendMode = FailoverEmailMode.All;
        [Category("Failover: Email Notification in case of error"), DisplayName("Send Email mode"), Description("Specify if the email is sent for the first, the last or for each failure."), Id(2, 7)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public FailoverEmailMode ErrorEmailSendMode
        {
            get { return _errorEmailSendMode; }
            set { _errorEmailSendMode = value; }
        }

        private string _errorEmailSubject;
        [Category("Failover: Email Notification in case of error"), DisplayName("Subject"), Description("The subject of the email sent in case of error. If empty, the report name is used."), Id(3, 7)]
        public string ErrorEmailSubject
        {
            get { return _errorEmailSubject; }
            set { _errorEmailSubject = value; }
        }

        private string _errorEmailFrom;
        [Category("Failover: Email Notification in case of error"), DisplayName("Sender address"), Description("The sender (From) email address used to send the email in case of error. If empty the default address configured in the device is used. Make sure that the SMTP server allows the new address."), Id(4, 7)]
        public string ErrorEmailFrom
        {
            get { return _errorEmailFrom; }
            set { _errorEmailFrom = value; }
        }

        public void SynchronizeTask()
        {
            string description = string.Format("Schedule for the Tasks. Report '{0}'", Report.FilePath);
            if (!IsTasksSchedule) description = string.Format("Schedule for the output '{0}'. Report '{1}'", Output.Name, Report.FilePath);

            TaskDefinition definition = Task.Definition;
            if (definition.RegistrationInfo.Source != TaskSource || definition.RegistrationInfo.Description != description || TaskName != Task.Name)
            {
                definition.RegistrationInfo.Source = TaskSource;
                definition.RegistrationInfo.Description = description;
                //If name has changed, we have to delete then insert it again...
                if (!string.IsNullOrEmpty(Task.Name) && TaskName != Task.Name)
                {
                    Report.TaskFolder.DeleteTask(Task.Name);
                    _task = Report.TaskFolder.RegisterTaskDefinition(TaskName, definition, TaskCreation.CreateOrUpdate, null);
                }
                else
                {
                    _task.RegisterChanges();
                }
            }
        }


        public Task FindTask()
        {
            Task result = Report.TaskFolder.GetTasks().FirstOrDefault(i => i.Definition.RegistrationInfo.Source == TaskSource);
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
                        string schedulerPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), Repository.SealTaskScheduler);
#if DEBUG
                        schedulerPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) + @"\..\..\..\SealTaskScheduler\bin\Debug", Repository.SealTaskScheduler);
#endif
                        taskDefinition.Actions.Add(new ExecAction(string.Format("\"{0}\"", schedulerPath), GUID, Application.StartupPath));
                        if (!Report.SchedulesWithCurrentUser)
                        {
                            //By default we use system account...
                            taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
                            _task = Report.TaskFolder.RegisterTaskDefinition(TaskName, taskDefinition, TaskCreation.CreateOrUpdate, "SYSTEM", null, TaskLogonType.ServiceAccount);
                        }
                        else
                        {
                            //default user
                            _task = Report.TaskFolder.RegisterTaskDefinition(TaskName, taskDefinition);
                        }
                    }
                    SynchronizeTask();
                }
                return _task;
            }
            set { _task = value; }
        }


        [XmlIgnore]
        public ReportOutput Output
        {
            get
            {
                return Report.Outputs.FirstOrDefault(i => i.GUID == OutputGUID);
            }
        }

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

        [Category("Helpers"), DisplayName("Edit schedule properties"), Description("Edit the report schedule properties. Warning: Schedules may also be edited and managed using the Task Scheduler Microsoft Management Console."), Id(1, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperEditProperties
        {
            get { return "<Click to edit the schedule properties>"; }
        }

        [Category("Helpers"), DisplayName("Run Task Scheduler MMC"), Description("Run the Task Scheduler Microsoft Management Console to manage schedule using the Windows interface."), Id(2, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperRunTaskScheduler
        {
            get { return "<Click to run the Task Scheduler Microsoft Management Console>"; }
        }


        #endregion

    }


}
