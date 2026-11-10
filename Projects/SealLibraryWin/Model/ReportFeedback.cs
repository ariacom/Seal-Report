//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Seal.Model
{
    /// <summary>
    /// Kind of a feedback sent on a report result
    /// </summary>
    public enum ReportFeedbackKind
    {
        /// <summary>
        /// A value or a measure seems wrong
        /// </summary>
        Data,
        /// <summary>
        /// A presentation is missing or could be better (table, chart, map...)
        /// </summary>
        Presentation,
        /// <summary>
        /// The report or a value is hard to understand
        /// </summary>
        Understanding,
        /// <summary>
        /// Opinion on the usefulness of the report (see Rating)
        /// </summary>
        Usefulness,
        /// <summary>
        /// Other feedback
        /// </summary>
        Other
    }

    /// <summary>
    /// Model of the 'Report Feedback Script' of the server configuration: a feedback sent by a user (or a guest) on a report result from the Web Report Server.
    /// The context (report, restrictions, rows, user) is set by the server from the report execution, the other values are typed by the user.
    /// The script stores or forwards the feedback, it may set Refusal to refuse it and Reply to change the message shown to the user.
    /// </summary>
    public class ReportFeedback
    {
        /// <summary>
        /// Maximum length of the text of a feedback, longer texts are truncated
        /// </summary>
        public const int MaxTextLength = 2000;
        /// <summary>
        /// Maximum length of the other values typed by the user (expected value, source, e-mail, target), longer values are truncated
        /// </summary>
        public const int MaxValueLength = 500;

        /// <summary>
        /// Current security, gives access to the repository
        /// </summary>
        public SealSecurity Security;
        /// <summary>
        /// Report executed, its current result is the one commented
        /// </summary>
        public Report Report;
        /// <summary>
        /// User sending the feedback, User.IsGuest is true for a guest
        /// </summary>
        public SecurityUser User;

        //Values typed by the user
        /// <summary>
        /// Kind of the feedback
        /// </summary>
        public ReportFeedbackKind Kind = ReportFeedbackKind.Other;
        /// <summary>
        /// Text of the feedback (MaxTextLength characters at most)
        /// </summary>
        public string Text;
        /// <summary>
        /// Value expected by the user for a Data feedback
        /// </summary>
        public string ExpectedValue;
        /// <summary>
        /// Source of the expected value given by the user (e.g. an address of a web site)
        /// </summary>
        public string SourceUrl;
        /// <summary>
        /// E-mail given by the user to be informed, optional
        /// </summary>
        public string Email;
        /// <summary>
        /// Rating of a Usefulness feedback: 1 (useful), -1 (not useful) or 0 (no rating)
        /// </summary>
        public int Rating;

        //Element of the result commented, empty for a feedback on the whole report
        /// <summary>
        /// Identifier of the view commented (attribute 'viewid' of the result)
        /// </summary>
        public string ViewId;
        /// <summary>
        /// Name of the view commented, set by the server from ViewId
        /// </summary>
        public string ViewName;
        /// <summary>
        /// Label of the element commented, e.g. the header of a column or the title of a chart
        /// </summary>
        public string TargetLabel;
        /// <summary>
        /// Value displayed in the element commented, e.g. the text of a cell
        /// </summary>
        public string TargetValue;
        /// <summary>
        /// Label of the row of the element commented, e.g. the text of the first cell of the row
        /// </summary>
        public string RowLabel;

        //Context set by the server
        /// <summary>
        /// Path of the report without the location of the repository (see Repository.GetAuditPath)
        /// </summary>
        public string ReportPath;
        /// <summary>
        /// Name of the report as executed
        /// </summary>
        public string ReportName;
        /// <summary>
        /// Restrictions prompted having a value at the execution, e.g. 'Country In France ; Order Date >= 01/01/2026'
        /// </summary>
        public string Restrictions;
        /// <summary>
        /// Version of the report commented: last modification date of the report file when it was loaded for the execution
        /// </summary>
        public DateTime? ReportModification;
        /// <summary>
        /// Version of the report commented: start of the SHA-256 hash of the content of the report file (12 hexadecimal characters), null if the file was modified since the execution.
        /// Unlike the date, it does not change when the file is copied without modification (e.g. deployment).
        /// </summary>
        public string ReportHash;
        /// <summary>
        /// Version of Seal Report running the server with its build (git commit of the sources), e.g. '10.3.0+ca3e9862' (see Repository.ProductVersionWithBuild)
        /// </summary>
        public string ApplicationVersion;
        /// <summary>
        /// Number of rows of the result (sum of the rows of the models executed), null if the report was not executed
        /// </summary>
        public int? RowCount;
        /// <summary>
        /// Start date of the last execution of the report, null if the report was not executed
        /// </summary>
        public DateTime? ExecutionDate;
        /// <summary>
        /// Date of the feedback
        /// </summary>
        public DateTime Date = DateTime.Now;
        /// <summary>
        /// Culture of the user, e.g. 'fr-FR'
        /// </summary>
        public string Culture;
        /// <summary>
        /// IP address of the client
        /// </summary>
        public string ClientAddress;
        /// <summary>
        /// Browser of the client: HTTP header 'User-Agent'
        /// </summary>
        public string ClientApplication;

        //Set by the script
        /// <summary>
        /// Set by the script to refuse the feedback: message shown to the user
        /// </summary>
        public string Refusal;
        /// <summary>
        /// Set by the script to replace the default message shown to the user when the feedback is accepted
        /// </summary>
        public string Reply;
        /// <summary>
        /// Optional identifier of the feedback set by the script (e.g. the key of the row inserted)
        /// </summary>
        public string FeedbackId;

        /// <summary>
        /// Truncates the values typed by the user to their maximum length (MaxTextLength, MaxValueLength) and trims them
        /// </summary>
        public void Normalize()
        {
            Text = Truncate(Text, MaxTextLength);
            ExpectedValue = Truncate(ExpectedValue, MaxValueLength);
            SourceUrl = Truncate(SourceUrl, MaxValueLength);
            Email = Truncate(Email, MaxValueLength);
            ViewId = Truncate(ViewId, MaxValueLength);
            TargetLabel = Truncate(TargetLabel, MaxValueLength);
            TargetValue = Truncate(TargetValue, MaxValueLength);
            RowLabel = Truncate(RowLabel, MaxValueLength);
            if (Rating > 0) Rating = 1;
            else if (Rating < 0) Rating = -1;
        }

        static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        /// <summary>
        /// True if the feedback has something to process: a text, an expected value or a rating
        /// </summary>
        public bool HasContent
        {
            get { return !string.IsNullOrEmpty(Text) || !string.IsNullOrEmpty(ExpectedValue) || Rating != 0; }
        }

        /// <summary>
        /// Short description of the feedback for the audit (event_detail): kind, rating and element commented, without the text typed
        /// </summary>
        public string AuditDetail
        {
            get
            {
                var result = Kind.ToString();
                if (Rating != 0) result += Rating > 0 ? " +1" : " -1";
                var target = string.Join(" | ", new string[] { ViewName, TargetLabel, RowLabel }.Where(i => !string.IsNullOrEmpty(i)));
                if (!string.IsNullOrEmpty(target)) result += ": " + target;
                return result;
            }
        }

        /// <summary>
        /// Start of the SHA-256 hash of the report file (12 hexadecimal characters), null if the file does not exist or was modified since the report was loaded
        /// </summary>
        public static string GetReportHash(Report report)
        {
            try
            {
                if (report == null || string.IsNullOrEmpty(report.FilePath) || !File.Exists(report.FilePath)) return null;
                if (report.LastModification != DateTime.MinValue && File.GetLastWriteTime(report.FilePath) != report.LastModification) return null;
                using var stream = File.OpenRead(report.FilePath);
                var hash = SHA256.HashData(stream);
                return Convert.ToHexString(hash).Substring(0, 12).ToLowerInvariant();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the context of the feedback from the report execution: path, name, restrictions, rows, date, view name
        /// </summary>
        public void SetReportContext(Report report)
        {
            Report = report;
            if (report == null) return;
            Security = report.Repository.Security;
            ReportPath = report.Repository.GetAuditPath(report.FilePath);
            ReportName = report.ExecutionName;
            Restrictions = Audit.GetExecutionRestrictions(report);
            RowCount = Audit.GetExecutionRows(report);
            if (report.ExecutionStartDate != DateTime.MinValue) ExecutionDate = report.ExecutionStartDate;
            ApplicationVersion = Repository.ProductVersionWithBuild;
            if (report.LastModification != DateTime.MinValue) ReportModification = report.LastModification;
            ReportHash = GetReportHash(report);
            if (!string.IsNullOrEmpty(ViewId))
            {
                try
                {
                    ViewName = report.ExecutionView?.GetView(ViewId)?.Name;
                }
                catch { }
            }
        }
    }
}
