//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//

namespace Seal.Model
{
    /// <summary>
    /// When the 'Schedule Check Script' of the security is executed
    /// </summary>
    public enum ScheduleCheckMoment
    {
        /// <summary>
        /// The schedule is created or modified (web schedule or AI tool)
        /// </summary>
        Save,
        /// <summary>
        /// The scheduler is about to execute the schedule
        /// </summary>
        Execution,
        /// <summary>
        /// The owner executes his web schedule from the Web Report Server
        /// </summary>
        ExecuteNow,
        /// <summary>
        /// A new web schedule is prepared in the Web Report Server (dialog 'Schedule...' and link of the outputs panel): the script can change
        /// the default values of Schedule and set LinkLabel. The refusal is ignored, the schedule is checked again when it is saved.
        /// </summary>
        New
    }

    /// <summary>
    /// Model of the 'Schedule Check Script' of the security: the script sets Refusal to refuse the schedule
    /// </summary>
    public class ScheduleCheck
    {
        /// <summary>
        /// Current security, gives access to the repository
        /// </summary>
        public SealSecurity Security;
        /// <summary>
        /// Report scheduled
        /// </summary>
        public Report Report;
        /// <summary>
        /// The schedule, null when the AI tool checks a schedule before creating it
        /// </summary>
        public SealSchedule Schedule;
        /// <summary>
        /// When the check is done
        /// </summary>
        public ScheduleCheckMoment Moment;
        /// <summary>
        /// User saving or owning the schedule, null for a schedule of a report executed by the scheduler (the location of the report tells who owns it)
        /// </summary>
        public SecurityUser User;
        /// <summary>
        /// True if the schedule is created (Moment is Save)
        /// </summary>
        public bool IsNew;
        /// <summary>
        /// Set by the script to refuse the schedule: message shown to the user, or stored as the last error of a web schedule
        /// </summary>
        public string Refusal;
        /// <summary>
        /// Moment New: label of the link creating a web schedule of the report in the outputs panel of the Web Report Server (e.g. 'Create an alert...'), the default label if empty
        /// </summary>
        public string LinkLabel;
    }
}
