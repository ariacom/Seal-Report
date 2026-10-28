//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// SealSchedule defines a schedule when the Seal Report Scheduler is active.
    /// </summary>
    public class SealSchedule
    {
        /// <summary>
        /// Unique identifier of the schedule
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// File path of the report scheduled 
        /// </summary>
        public string ReportPath { get; set; }

        /// <summary>
        /// GUID of the report scheduled
        /// </summary>
        public string ReportGUID { get; set; }

        /// <summary>
        /// Next execution date time of the schedule
        /// </summary>
        public DateTime NextExecution { get; set; } = DateTime.MaxValue;

        /// <summary>
        /// True if the schedule is enable
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Start date time of the schedule
        /// </summary>
        public DateTime Start { get; set; } = DateTime.Now;

        /// <summary>
        /// End date time of the schedule
        /// </summary>
        public DateTime End { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Trigger type of the schedule
        /// </summary>
        public TriggerType Type { get; set; } = TriggerType.Daily;

        /// <summary>
        /// Number of days for the interval
        /// </summary>
        public int DaysInterval { get; set; } = 1;

        /// <summary>
        /// Number of weeks for the interval
        /// </summary>
        public int WeeksInterval { get; set; } = 1;

        /// <summary>
        /// Weekdays of the schedule
        /// </summary>
        public int[] Weekdays { get; set; } = new int[] { 1 };

        /// <summary>
        /// Months of the schedule
        /// </summary>
        public int[] Months { get; set; } = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        /// <summary>
        /// Days of the schedule
        /// </summary>
        public int[] Days { get; set; } = new int[] { 1 };

        /// <summary>
        /// Repeat interval in TimeSpan
        /// </summary>
        [XmlIgnore]
        public TimeSpan RepeatInterval { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Repeat interval in ticks
        /// </summary>
        public long RepeatIntervalTicks
        {
            get { return RepeatInterval.Ticks; }
            set { RepeatInterval = new TimeSpan(value); }
        }

        /// <summary>
        /// Repeat duration in TimeSpan
        /// </summary>
        [XmlIgnore]
        public TimeSpan RepeatDuration { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Repeat duration in ticks
        /// </summary>
        public long RepeatDurationTicks
        {
            get { return RepeatDuration.Ticks; }
            set { RepeatDuration = new TimeSpan(value); }
        }

        /// <summary>
        /// True if the schedule is beign executed by the scheduler
        /// </summary>
        [XmlIgnore]
        public bool BeingExecuted = false;

        /// <summary>
        /// Current file path of the web schedule
        /// </summary>
        [XmlIgnore]
        public string FilePath = "";

        /// <summary>
        /// Last modification date of the seal schedule
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification = DateTime.MinValue;

        /// <summary>
        /// Meta information that can be used for any purpose
        /// </summary>
        public List<string> MetaInfo { get; set; } = new List<string>();
        /// <summary>
        /// Xml serialization helper: serialize MetaInfo only if not empty
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeMetaInfo() { return MetaInfo.Count > 0; }

        #region Web schedule

        /// <summary>
        /// Web schedule: output of the schedule. A web schedule is created from the Web Report Server and stored in the personal folder of its owner: the report file is not modified, the output is injected in the report when the schedule is executed.
        /// </summary>
        public WebScheduleOutput Output { get; set; } = null;

        /// <summary>
        /// True if the schedule is a web schedule (stored in the personal folder of its owner)
        /// </summary>
        [XmlIgnore]
        public bool IsWebSchedule
        {
            get { return Output != null; }
        }

        /// <summary>
        /// Web schedule: name of the schedule
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Web schedule: name of the owner, authenticated by the Security Provider without password when the schedule is executed
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Web schedule: host name of the owner when the schedule was saved (used for the personal folder if 'Use host name for Personal Folder' is set)
        /// </summary>
        public string WebHostName { get; set; }

        /// <summary>
        /// Web schedule: path of the report in the Web Report Server
        /// </summary>
        public string WebPath { get; set; }

        /// <summary>
        /// Web schedule: date time of the last execution
        /// </summary>
        public DateTime LastExecution { get; set; } = DateTime.MinValue;
        /// <summary>
        /// Xml serialization helper: serialize LastExecution only if set
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeLastExecution() { return LastExecution != DateTime.MinValue; }

        /// <summary>
        /// Web schedule: error of the last execution, empty if the execution was successful
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// Web schedule: add the output and the schedule in the report loaded for the execution (the report is not saved)
        /// </summary>
        public ReportSchedule InjectWebSchedule(Report report)
        {
            var output = Output.CreateReportOutput(report, GUID, Name);
            var schedule = report.AddSchedule(output);
            schedule.GUID = GUID;
            schedule.Name = Name;
            schedule.SealSchedule = this;
            report.SchedulesModified = false;
            return schedule;
        }

        /// <summary>
        /// Web schedule: authenticate the owner with the Security Provider (without password) and check that he can still schedule the report.
        /// The schedule file must be located in the personal folder of the owner. Throws an exception if the owner cannot execute the schedule.
        /// </summary>
        public SecurityUser AuthenticateOwner(Report report)
        {
            var repository = report.Repository;
            var user = new SecurityUser(repository.Security) { WebUserName = UserName, WebHostName = WebHostName, ForSchedule = true };
            user.Authenticate();
            if (!user.IsAuthenticated) throw new Exception($"The owner '{UserName}' of the web schedule '{Name}' is not authenticated by the Security Provider" + (string.IsNullOrEmpty(user.Error) ? "" : ": " + user.Error));
            if (!string.Equals(Path.GetFullPath(Path.GetDirectoryName(FilePath)), Path.GetFullPath(repository.GetWebSchedulesFolder(user)), StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"The web schedule file '{FilePath}' is not located in the personal folder of its owner '{UserName}'");
            }
            user.SetFolders();
            if (!user.HasFolderRight(Path.GetDirectoryName(report.FilePath), (int)FolderRight.Schedule)) throw new Exception($"The owner '{UserName}' of the web schedule '{Name}' has no right to schedule the report '{report.FilePath}'");
            var device = repository.Devices.FirstOrDefault(i => i.GUID == Output.DeviceGUID);
            if (device == null || !user.CanAccessDevice(device)) throw new Exception($"The owner '{UserName}' of the web schedule '{Name}' cannot use the output device");
            if (device is OutputFolderDevice && !user.HasFolderRight(repository.ReplaceRepositoryKeyword(Output.FolderPath), (int)FolderRight.Edit)) throw new Exception($"The owner '{UserName}' of the web schedule '{Name}' has no right to write in the folder '{Output.FolderPath}'");
            return user;
        }

        #endregion

        /// <summary>
        /// Load a web schedule from a file
        /// </summary>
        static public SealSchedule LoadFromFile(string path)
        {
            SealSchedule result = null;
            try
            {
                path = FileHelper.ConvertOSFilePath(path);
                if (!File.Exists(path)) throw new Exception("File not found: " + path);

                XmlSerializer serializer = new XmlSerializer(typeof(SealSchedule));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (SealSchedule)serializer.Deserialize(xr);
                    xr.Close();
                }
                result.FilePath = path;
                result.LastModification = File.GetLastWriteTime(path);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to read the file '{0}'.\r\n{1}\r\n", path, ex.Message, ex.StackTrace));
            }
            return result;
        }

        /// <summary>
        /// Save the current web schedule to its file
        /// </summary>
        public void SaveToFile()
        {
            if (File.Exists(FilePath) && LastModification != File.GetLastWriteTime(FilePath))
            {
                throw new Exception("Unable to save the schedule file. The file has been modified by another user.");
            }
            Helper.Serialize(FilePath, this);
            LastModification = File.GetLastWriteTime(FilePath);
        }

        [XmlIgnore]
        DateTime EndFinal
        {
            get
            {
                return End == DateTime.MinValue ? DateTime.MaxValue : End;
            }
        }

        /// <summary>
        /// Calculate and update the next schedule execution
        /// </summary>
        public void CalculateNextExecution()
        {
            NextExecution = Start;

            switch (Type)
            {
                case TriggerType.Time:
                    if (RepeatInterval != TimeSpan.Zero)
                    {
                        //Handle repeat
                        var endRepeatDate = (RepeatDuration != TimeSpan.Zero ? NextExecution + RepeatDuration : DateTime.MaxValue);
                        if (endRepeatDate < DateTime.Now) break;
                        while (true)
                        {
                            if (NextExecution > DateTime.Now || NextExecution > EndFinal || NextExecution > endRepeatDate)
                            {
                                //Found within repeat
                                break;
                            }
                            NextExecution += RepeatInterval;
                        }
                    }
                    break;

                case TriggerType.Daily:
                    while (true)
                    {
                        if (NextExecution > DateTime.Now || NextExecution > EndFinal)
                        {
                            //Found
                            break;
                        }

                        if (RepeatInterval != TimeSpan.Zero)
                        {
                            //Handle repeat
                            var nextRepeatExecution = NextExecution;
                            var endRepeatDate = (RepeatDuration != TimeSpan.Zero ? nextRepeatExecution + RepeatDuration : DateTime.MaxValue);
                            while (endRepeatDate > DateTime.Now)
                            {
                                if (nextRepeatExecution > DateTime.Now || nextRepeatExecution > EndFinal || nextRepeatExecution > endRepeatDate)
                                {
                                    break;
                                }
                                nextRepeatExecution += RepeatInterval;
                            }

                            if (nextRepeatExecution > DateTime.Now || nextRepeatExecution > EndFinal)
                            {
                                //Found within repeat
                                NextExecution = nextRepeatExecution;
                                break;
                            }
                        }

                        NextExecution = NextExecution.AddDays(DaysInterval);
                    }
                    break;

                case TriggerType.Weekly:
                    while (true)
                    {
                        if (Weekdays.Length == 0) break;

                        //Check day of week
                        for (int i = 0; i < 6; i++)
                        {
                            var nextWeekExecution = NextExecution.AddDays(i);
                            if (!Weekdays.Contains((int)nextWeekExecution.DayOfWeek))
                            {
                                continue;
                            }

                            if (nextWeekExecution > DateTime.Now || nextWeekExecution > EndFinal)
                            {
                                //Found
                                NextExecution = nextWeekExecution;
                                break;
                            }

                            if (RepeatInterval != TimeSpan.Zero)
                            {
                                //Handle repeat
                                var nextRepeatExecution = nextWeekExecution;
                                var endRepeatDate = (RepeatDuration != TimeSpan.Zero ? nextRepeatExecution + RepeatDuration : DateTime.MaxValue);
                                while (endRepeatDate > DateTime.Now)
                                {
                                    if (nextRepeatExecution > DateTime.Now || nextRepeatExecution > EndFinal || nextRepeatExecution > endRepeatDate)
                                    {
                                        break;
                                    }
                                    nextRepeatExecution += RepeatInterval;
                                }

                                if (nextRepeatExecution > DateTime.Now || nextRepeatExecution > EndFinal)
                                {
                                    //Found within repeat
                                    NextExecution = nextRepeatExecution;
                                    break;
                                }
                            }
                        }

                        if (NextExecution > DateTime.Now || NextExecution > EndFinal)
                        {
                            //Found
                            break;
                        }

                        NextExecution = NextExecution.AddDays(7 * WeeksInterval);
                    }
                    break;

                case TriggerType.Monthly:
                    while (true)
                    {
                        if (Months.Length == 0) break;
                        if (Days.Length == 0) break;

                        //Check days and months
                        bool isLastDayOfMonth = (Days.Contains(32) && Months.Contains(NextExecution.Month) && NextExecution.Day == DateTime.DaysInMonth(NextExecution.Year, NextExecution.Month));
                        if (!isLastDayOfMonth)
                        {
                            if (!Days.Contains(NextExecution.Day) || !Months.Contains(NextExecution.Month))
                            {
                                NextExecution = NextExecution.AddDays(1);
                                continue;
                            }
                        }

                        if (NextExecution > DateTime.Now || NextExecution > EndFinal)
                        {
                            //Found
                            break;
                        }

                        if (RepeatInterval != TimeSpan.Zero)
                        {
                            //Handle repeat
                            var nextRepeatExecution = NextExecution;
                            var endRepeatDate = (RepeatDuration != TimeSpan.Zero ? nextRepeatExecution + RepeatDuration : DateTime.MaxValue);
                            while (endRepeatDate > DateTime.Now)
                            {
                                if (nextRepeatExecution > DateTime.Now || nextRepeatExecution > EndFinal || nextRepeatExecution > endRepeatDate)
                                {
                                    break;
                                }
                                nextRepeatExecution += RepeatInterval;
                            }

                            if (nextRepeatExecution > DateTime.Now || nextRepeatExecution > EndFinal)
                            {
                                //Found within repeat
                                NextExecution = nextRepeatExecution;
                                break;
                            }
                        }

                        //Check next day
                        NextExecution = NextExecution.AddDays(1);
                    }
                    break;
            }

            if (NextExecution < DateTime.Now) NextExecution = DateTime.MaxValue;
        }

        /// <summary>
        /// True is the next execution is reached
        /// </summary>
        /// <returns></returns>
        public bool IsReached()
        {
            bool result = (
                    Enabled &&
                    Type == TriggerType.Time &&
                    NextExecution <= DateTime.Now
                    )
                 || (
                    Enabled &&
                    Type != TriggerType.Time &&
                    NextExecution <= DateTime.Now &&
                    Start <= DateTime.Now &&
                    EndFinal >= DateTime.Now
                    );

            return result;
        }
    }
}