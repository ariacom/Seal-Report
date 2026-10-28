//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
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
