//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Seal.Model
{
    /// <summary>
    /// Output of a web schedule: a web schedule is stored in the personal folder of its owner (see <see cref="SealSchedule.IsWebSchedule"/>),
    /// the report file is never modified. The output is injected in the report when the schedule is executed.
    /// </summary>
    public class WebScheduleOutput
    {
        /// <summary>
        /// Identifier of the output device
        /// </summary>
        public string DeviceGUID { get; set; }

        /// <summary>
        /// Format of the result
        /// </summary>
        public ReportFormat Format { get; set; } = ReportFormat.html;

        /// <summary>
        /// Folder device: folder path (with the repository keywords), File Server and SharePoint devices: directory
        /// </summary>
        public string FolderPath { get; set; }

        /// <summary>
        /// Name of the file generated, can contain the report display name keyword and date time formats
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Email device: TO addresses separated by semi-column
        /// </summary>
        public string EmailTo { get; set; }

        /// <summary>
        /// Email device: CC addresses separated by semi-column
        /// </summary>
        public string EmailCC { get; set; }

        /// <summary>
        /// Email device: BCC addresses separated by semi-column
        /// </summary>
        public string EmailBCC { get; set; }

        /// <summary>
        /// Email device: subject, the report name is used if empty
        /// </summary>
        public string EmailSubject { get; set; }

        /// <summary>
        /// Email device: body, a default text is used if empty
        /// </summary>
        public string EmailBody { get; set; }

        /// <summary>
        /// Email device: if true, the HTML result is copied in the email body
        /// </summary>
        public bool EmailHtmlBody { get; set; } = false;

        /// <summary>
        /// If true, the result file is compressed in a zip file
        /// </summary>
        public bool ZipResult { get; set; } = false;

        /// <summary>
        /// Password of the zip file, no password if empty
        /// </summary>
        public string ZipPassword { get; set; }

        /// <summary>
        /// If true, the generation is cancelled if the models have no record
        /// </summary>
        public bool CancelIfNoRecords { get; set; } = false;

        /// <summary>
        /// Values of the prompted restrictions of the report, identified by the restriction GUID. If empty, the default values of the report are used.
        /// </summary>
        public List<RestrictionValues> Restrictions { get; set; } = new List<RestrictionValues>();
        /// <summary>
        /// Xml serialization helper: serialize Restrictions only if not empty
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeRestrictions() { return Restrictions.Count > 0; }

        /// <summary>
        /// Prompted restrictions of a report (main panel, Restriction Views and input values): the only restrictions that a web schedule can change
        /// </summary>
        public static List<ReportRestriction> GetPromptedRestrictions(Report report)
        {
            return report.AllRestrictions.Where(i => i.Prompt != PromptType.None).ToList();
        }

        /// <summary>
        /// Set the restriction values of the schedule to the prompted restrictions of the report. A value is ignored if its restriction does not exist or is not prompted anymore.
        /// </summary>
        public void ApplyRestrictions(Report report)
        {
            if (Restrictions.Count == 0) return;
            var restrictions = GetPromptedRestrictions(report);
            foreach (var values in Restrictions)
            {
                foreach (var restriction in restrictions.Where(i => i.GUID == values.GUID)) values.ApplyTo(restriction, true);
            }
        }

        /// <summary>
        /// Store the current values of the prompted restrictions of the report
        /// </summary>
        public void SetRestrictions(Report report)
        {
            Restrictions = GetPromptedRestrictions(report).GroupBy(i => i.GUID).Select(i => RestrictionValues.FromRestriction(i.First())).ToList();
        }

        /// <summary>
        /// Text of the restriction values of the schedule applied to the report (empty if the default values are used)
        /// </summary>
        public string GetRestrictionsText(Report report)
        {
            if (Restrictions.Count == 0) return "";
            ApplyRestrictions(report);
            var guids = Restrictions.Select(i => i.GUID).ToList();
            //Restrictions having a value, with the date keywords (e.g. Today-1)
            var texts = GetPromptedRestrictions(report).Where(i => guids.Contains(i.GUID) && i.HasValue).GroupBy(i => i.GUID).Select(i => i.First().DisplayRestriction).Where(i => !string.IsNullOrWhiteSpace(i));
            return string.Join("; ", texts);
        }

        /// <summary>
        /// Create the report output in the report (not saved in the report file)
        /// </summary>
        public ReportOutput CreateReportOutput(Report report, string guid, string name)
        {
            var device = report.Repository.Devices.FirstOrDefault(i => i.GUID == DeviceGUID);
            if (device == null) throw new Exception($"The output device of the schedule '{name}' does not exist anymore");

            var output = report.AddOutput(device);
            output.GUID = guid;
            output.Name = name;
            output.Format = Format;
            output.FolderPath = FolderPath;
            output.FileName = FileName;
            output.EmailTo = EmailTo;
            output.EmailCC = EmailCC;
            output.EmailBCC = EmailBCC;
            output.EmailSubject = EmailSubject;
            output.EmailBody = EmailBody;
            output.EmailHtmlBody = EmailHtmlBody;
            output.ZipResult = ZipResult;
            output.ZipPassword = ZipPassword;
            output.CancelIfNoRecords = CancelIfNoRecords;
            //The security context is the owner authenticated by the Security Provider, not a user name and groups of the output
            output.UserName = null;
            output.UserGroups = null;
            return output;
        }
    }
}
