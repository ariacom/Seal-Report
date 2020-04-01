//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Helpers;
using System.Drawing.Design;
using System.ComponentModel.Design;

namespace Seal.Model
{
    /// <summary>
    /// A ReportOutput defines the execution of a report on an OutputDevice (Folder or Email)
    /// </summary>
    public class ReportOutput : ReportComponent
    {

        /// <summary>
        /// Basic creation
        /// </summary>
        public static ReportOutput Create()
        {
            return new ReportOutput() { GUID = Guid.NewGuid().ToString() };
        }

        /// <summary>
        /// Init all references
        /// </summary>
        public void InitReferences()
        {
            var initialParameters = ViewParameters.Where(i => i.CustomValue).ToList();
            ViewParameters.Clear();
            if (Report != null && View != null)
            {
                foreach (var configParameter in View.Template.Parameters)
                {
                    OutputParameter parameter = initialParameters.FirstOrDefault(i => i.Name == configParameter.Name);
                    if (parameter == null) parameter = new OutputParameter() { Name = configParameter.Name, Value = configParameter.Value };
                    else parameter.CustomValue = true;
                    ViewParameters.Add(parameter);
                    parameter.Enums = configParameter.Enums;
                    parameter.Description = configParameter.Description;
                    parameter.Type = configParameter.Type;
                    parameter.UseOnlyEnumValues = configParameter.UseOnlyEnumValues;
                    parameter.DisplayName = configParameter.DisplayName;
                    parameter.ConfigValue = configParameter.Value;
                    parameter.EditorLanguage = configParameter.EditorLanguage;
                    parameter.TextSamples = configParameter.TextSamples;
                }
            }
        }

        private string _outputDeviceGUID;
        /// <summary>
        /// Identifier of the output device used by the output
        /// </summary>
        public string OutputDeviceGUID
        {
            get
            {
                if (string.IsNullOrEmpty(_outputDeviceGUID)) _outputDeviceGUID = OutputFolderDevice.DefaultGUID;
                return
                    _outputDeviceGUID;
            }
            set { _outputDeviceGUID = value; }
        }

        /// <summary>
        /// The device used by this output
        /// </summary>
        [XmlIgnore]
        public string DeviceName
        {
            get
            {
                return Device.FullName;
            }
        }

        /// <summary>
        /// If the models of the report do not have any record, the output generation is cancelled
        /// </summary>
        public bool CancelIfNoRecords { get; set; } = false;

        /// <summary>
        /// Optional Razor script executed before the generation. If the script returns 0, the generation is aborted.
        /// </summary>
        public string PreScript { get; set; }

        /// <summary>
        /// Optional Razor script executed after the generation
        /// </summary>
        public string PostScript { get; set; }

        /// <summary>
        /// Custom parameters used for the Root View when the output is executed
        /// </summary>
        public List<OutputParameter> ViewParameters { get; set; } = new List<OutputParameter>();
        public bool ShouldSerializeViewParameters() { return ViewParameters.Count > 0; }

        /// <summary>
        /// The view used to execute the report output
        /// </summary>
        public string ViewGUID { get; set; }

        /// <summary>
        /// Path of the folder used to generate the report result.
        /// </summary>
        public string FolderPath { get; set; }


        public string FileServerFolderWithSeparators { 
            get
            {
                if (string.IsNullOrEmpty(FolderPath) || FolderPath == "/" || FolderPath == "\\") return "/";
                return (FolderPath.StartsWith("/") ? "" : "/") + FolderPath + (FolderPath.EndsWith("/") ? "" : "/");
            }
        }

        /// <summary>
        /// The name of the file used to generate the report result
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The destination (To) email addresses used for the email. One per line or separated by semi-column.
        /// </summary>
        public string EmailTo { get; set; }

        /// <summary>
        /// The email CC (Carbon Copy) addresses used for the email. One per line or separated by semi-column.
        /// </summary>
        public string EmailCC { get; set; }

        /// <summary>
        /// The email BCC (Blind Carbon Copy) addresses used for the email. One per line or separated by semi-column.
        /// </summary>
        public string EmailBCC { get; set; }

        /// <summary>
        /// The email address of the sender (From). If empty, the sender email address defined in the device is used. Make sure that the SMTP server allows the new address.
        /// </summary>
        public string EmailFrom { get; set; }

        /// <summary>
        /// The reply addresses used for the email. One per line or separated by semi-column. If empty, the reply addresses defined in the device are used.
        /// </summary>
        public string EmailReplyTo { get; set; }

        /// <summary>
        /// The subject of the email sent. If empty, the report name is used.
        /// </summary>
        public string EmailSubject { get; set; }


        private bool _emailHtmlBody = false;
        /// <summary>
        /// If true, the report result is copied in the email body message
        /// </summary>
        public bool EmailHtmlBody
        {
            get { return _emailHtmlBody; }
            set
            {
                _emailHtmlBody = value;
                
            }
        }

        private bool _emailMessagesInBody = false;
        /// <summary>
        /// If true, the report execution messages are copied in the email body message
        /// </summary>
        public bool EmailMessagesInBody
        {
            get { return _emailMessagesInBody; }
            set
            {
                _emailMessagesInBody = value;
            }
        }

        /// <summary>
        /// The body message of the email sent. If empty, a default text is used.
        /// </summary>
        public string EmailBody { get; set; }

        private bool _emailSkipAttachments = false;
        /// <summary>
        /// If true, the email sent will have no attachement. This may be useful if the report has only tasks.
        /// </summary>
        public bool EmailSkipAttachments
        {
            get { return _emailSkipAttachments; }
            set
            {
                _emailSkipAttachments = value;
            }
        }

        /// <summary>
        /// This property is obsolete. Use ZipResult instead. Will be removed in future version.
        /// </summary>
        public bool? EmailZipAttachments { get; set; } = null;

        /// <summary>
        /// This property is obsolete. Use ZipPassword instead. Will be removed in future version.
        /// </summary>
        public string EmailZipPassword { get; set; }

        private bool _zipResult = false;
        /// <summary>
        /// If true, the result file will be compressed in a zip file
        /// </summary>
        public bool ZipResult
        {
            get
            {

                return _zipResult;
            }
            set
            {
                _zipResult = value;
                if (EmailZipAttachments != null)
                {
                    _zipResult = EmailZipAttachments.Value;
                    EmailZipAttachments = null;
                }
            }
        }

        private string _zipPassword = "";
        /// <summary>
        /// If not empty, the Zip result file will be protected with the password
        /// </summary>
        public string ZipPassword
        {
            get
            {
                return _zipPassword;
            }
            set
            {
                _zipPassword = value;
                if (!string.IsNullOrEmpty(EmailZipPassword) && string.IsNullOrEmpty(_zipPassword))
                {
                    _zipPassword = EmailZipPassword;
                    EmailZipPassword = null;
                }
            }
        }

        /// <summary>
        /// If not empty, the output is generated with a security context having the name specified
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// If not empty, the output is generated with a security context having the groups specified. One group name per line or separated by semi-column.
        /// </summary>
        public string UserGroups { get; set; }

        /// <summary>
        /// The culture used to generate the report. If empty, the culture from the groups is used, then the default culture.
        /// </summary>
        public string UserCulture { get; set; }

        /// <summary>
        /// For the Web Report Server: If true, the output can be executed by all users having the execute right on the report. If false, only the user owner can execute the schedule.
        /// </summary>
        public bool PublicExec { get; set; } = true;

        /// <summary>
        /// For the Web Report Server Designer: If true, the output and shedule can be edited by all users having the schedule right on the report. If false, only the user owner can edit the schedule.
        /// </summary>
        public bool PublicEdit { get; set; } = true;

        /// <summary>
        /// Object that can be used at run-time for any purpose
        /// </summary>
        [XmlIgnore]
        public object Tag;

        /// <summary>
        /// Format of the output
        /// </summary>
        [XmlIgnore]
        public ReportFormat Format
        {
            get
            {
                var param = ViewParameters.FirstOrDefault(i => i.Name == Parameter.ReportFormatParameter && i.CustomValue);
                if (param != null) return (ReportFormat)Enum.Parse(typeof(ReportFormat), param.Value);
                return Report.Format;
            }
            set
            {
                var param = ViewParameters.FirstOrDefault(i => i.Name == Parameter.ReportFormatParameter);
                if (param != null)
                {
                    param.CustomValue = true;
                    param.Value = value.ToString();
                }
            }
        }

        /// <summary>
        /// Current OutputDevice
        /// </summary>
        [XmlIgnore]
        public OutputDevice Device
        {
            get
            {
                return Report.Repository.Devices.FirstOrDefault(i => i.GUID == OutputDeviceGUID);
            }
        }

        /// <summary>
        /// Current View to execute
        /// </summary>
        [XmlIgnore]
        public ReportView View
        {
            get
            {
                if (Report != null) return Report.FindView(Report.Views, ViewGUID);
                return null;
            }
        }

        void SynchronizeRestrictions()
        {
            if (Report == null || !_useCustomRestrictions) return;

            List<ReportRestriction> allRestrictions = Report.AllRestrictions;

            //Check the existing restrictions
            int index = _restrictions.Count;
            while (--index >= 0)
            {
                ReportRestriction restriction = allRestrictions.FirstOrDefault(i => i.GUID == _restrictions[index].GUID);
                if (restriction == null) _restrictions.RemoveAt(index);
                else
                {
                    _restrictions[index].SetSourceReference(restriction.Source);
                    _restrictions[index].Report = Report;
                    _restrictions[index].Model = restriction.Model;
                }
            }

            //Add new restrictions
            foreach (var restriction in allRestrictions)
            {
                ReportRestriction outputRestriction = _restrictions.FirstOrDefault(i => i.GUID == restriction.GUID);
                if (outputRestriction == null)
                {
                    outputRestriction = ReportRestriction.CreateReportRestriction();
                    Helper.CopyProperties(restriction, outputRestriction);
                    _restrictions.Add(outputRestriction);
                }
                outputRestriction.PivotPosition = restriction.PivotPosition;
            }
        }

        bool _useCustomRestrictions = false;
        /// <summary>
        /// If true, custom restrictions can be defined for this output
        /// </summary>
        public bool UseCustomRestrictions
        {
            get { return _useCustomRestrictions; }
            set
            {
                _useCustomRestrictions = value;
                
            }
        }

        List<ReportRestriction> _restrictions = new List<ReportRestriction>();
        /// <summary>
        /// Custom restrictions applied to this output
        /// </summary>
        public List<ReportRestriction> Restrictions
        {
            get
            {
                SynchronizeRestrictions();
                return _restrictions;
            }
            set { _restrictions = value; }
        }
        public bool ShouldSerializeRestrictions() { return _restrictions.Count > 0; }

        #region Helpers
        /// <summary>
        /// Last information message
        /// </summary>
        [XmlIgnore]
        public string Information { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
        [XmlIgnore]
        public string Error { get; set; }


        //Temporary variables to help for report serialization...
        private List<OutputParameter> _tempParameters;

        /// <summary>
        /// Function called before the serialization
        /// </summary>
        public void BeforeSerialization()
        {
            _tempParameters = ViewParameters.ToList();
            //Remove parameters not used
            ViewParameters.RemoveAll(i => !i.CustomValue);
        }

        /// <summary>
        /// Copy the current parameter values to a parameter list
        /// </summary>
        public void CopyParameters(List<Parameter> destination)
        {
            foreach (var parameter in ViewParameters.Where(i => i.CustomValue))
            {
                var destParameter = destination.FirstOrDefault(i => i.Name == parameter.Name);
                if (destParameter != null) destParameter.Value = parameter.Value;
            }
        }

        /// <summary>
        /// Function called after the serialization
        /// </summary>
        public void AfterSerialization()
        {
            ViewParameters = _tempParameters;
        }


        #endregion

    }


}

