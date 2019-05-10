//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Helpers;
using DynamicTypeDescriptor;
using Seal.Converter;
using System.Drawing.Design;
using Seal.Forms;
using System.ComponentModel.Design;

namespace Seal.Model
{
    public class ReportOutput : ReportComponent
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("DeviceName").SetIsBrowsable(true);
                GetProperty("UseCustomRestrictions").SetIsBrowsable(true);
                GetProperty("Restrictions").SetIsBrowsable(true);
                GetProperty("ViewGUID").SetIsBrowsable(true);
                GetProperty("CancelIfNoRecords").SetIsBrowsable(true);
                GetProperty("PreScript").SetIsBrowsable(true);
                GetProperty("PostScript").SetIsBrowsable(true);
                GetProperty("ViewParameters").SetIsBrowsable(true);

                GetProperty("FolderPath").SetIsBrowsable(Device is OutputFolderDevice);
                GetProperty("FileName").SetIsBrowsable(Device is OutputFolderDevice);

                GetProperty("EmailSubject").SetIsBrowsable(Device is OutputEmailDevice);
                GetProperty("EmailBody").SetIsBrowsable(Device is OutputEmailDevice);
                GetProperty("EmailHtmlBody").SetIsBrowsable(Device is OutputEmailDevice);
                GetProperty("EmailTo").SetIsBrowsable(Device is OutputEmailDevice);
                GetProperty("EmailCC").SetIsBrowsable(Device is OutputEmailDevice);
                GetProperty("EmailBCC").SetIsBrowsable(Device is OutputEmailDevice);
                GetProperty("EmailMessagesInBody").SetIsBrowsable(Device is OutputEmailDevice);
                GetProperty("EmailZipAttachments").SetIsBrowsable(Device is OutputEmailDevice);
                GetProperty("EmailZipPassword").SetIsBrowsable(Device is OutputEmailDevice);
                GetProperty("EmailSkipAttachments").SetIsBrowsable(Device is OutputEmailDevice);
                GetProperty("EmailFrom").SetIsBrowsable(Device is OutputEmailDevice && ((OutputEmailDevice)Device).ChangeSender);
                GetProperty("EmailReplyTo").SetIsBrowsable(Device is OutputEmailDevice && ((OutputEmailDevice)Device).ChangeSender);

                GetProperty("UserName").SetIsBrowsable(true);
                GetProperty("UserGroups").SetIsBrowsable(true);
                GetProperty("UserCulture").SetIsBrowsable(true);
                GetProperty("PublicExec").SetIsBrowsable(true);
                GetProperty("PublicEdit").SetIsBrowsable(true);

                //Helpers
                //GetProperty("Information").SetIsBrowsable(true);
                //GetProperty("Error").SetIsBrowsable(true);

                //Readonly
                GetProperty("Restrictions").SetIsReadOnly(!UseCustomRestrictions);
                GetProperty("EmailBody").SetIsReadOnly(EmailHtmlBody || EmailMessagesInBody);
                GetProperty("EmailHtmlBody").SetIsReadOnly(EmailMessagesInBody);
                GetProperty("EmailMessagesInBody").SetIsReadOnly(EmailHtmlBody);

                GetProperty("EmailSkipAttachments").SetIsReadOnly(EmailZipAttachments);
                GetProperty("EmailZipAttachments").SetIsReadOnly(EmailSkipAttachments);
                GetProperty("EmailZipPassword").SetIsReadOnly(!EmailZipAttachments);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion


        public static ReportOutput Create()
        {
            return new ReportOutput() { GUID = Guid.NewGuid().ToString() };
        }

        public void InitReferences()
        {
            var initialParameters  = _viewParameters.Where(i=>i.CustomValue).ToList();
            _viewParameters.Clear();
            if (Report != null && View != null)
            {
                foreach (var configParameter in View.Template.Parameters)
                {
                    OutputParameter parameter = initialParameters.FirstOrDefault(i => i.Name == configParameter.Name);
                    if (parameter == null) parameter = new OutputParameter() { Name = configParameter.Name, Value = configParameter.Value };
                    else parameter.CustomValue = true;
                    _viewParameters.Add(parameter);
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

        [XmlIgnore]
        [Category("Definition"), DisplayName("Output device"), Description("The device used by this output."), Id(1, 1)]
        public string DeviceName
        {
            get
            {
                return Device.FullName;
            }
        }

        bool _cancelIfNoRecords = false;
        [DefaultValue(false)]
        [Category("Definition"), DisplayName("Cancel generation if no records"), Description("If the models of the report do not have any record, the output generation is cancelled."), Id(3, 1)]
        public bool CancelIfNoRecords
        {
            get { return _cancelIfNoRecords; }
            set { _cancelIfNoRecords = value; }
        }


        string _preScript;
        [Category("Definition"), DisplayName("Pre-generation script"), Description("Optional Razor script executed before the generation. If the script returns 0, the generation is aborted."), Id(4, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string PreScript
        {
            get { return _preScript; }
            set { _preScript = value; }
        }

        string _postScript;
        [Category("Definition"), DisplayName("Post-generation script"), Description("Optional Razor script executed after the generation."), Id(5, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string PostScript
        {
            get { return _postScript; }
            set { _postScript = value; }
        }

        List<OutputParameter> _viewParameters = new List<OutputParameter>();
        [Category("Definition"), DisplayName("Custom view parameters"), Description("Custom parameters used for the Root View when the output is executed."), Id(6, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<OutputParameter> ViewParameters
        {
            get {
                return _viewParameters;
            }
            set {
                _viewParameters = value;
            }
        }
        public bool ShouldSerializeViewParameters() { return _viewParameters.Count > 0; }

        private string _viewGUID;
        [Category("Definition"), DisplayName("View name"), Description("The view used to execute the report output."), Id(2, 1)]
        [TypeConverter(typeof(ReportViewConverter))]
        public string ViewGUID
        {
            get { return _viewGUID; }
            set { _viewGUID = value; }
        }


        string _folderPath;
        [Category("Folder"), DisplayName("Folder path"), Description("Path of the folder used to generate the report result. The path can contain the keyword " + Repository.SealRepositoryKeyword + " to specify the repository root folder."), Id(2, 2)]
        [TypeConverter(typeof(RepositoryFolderConverter))]
        public string FolderPath
        {
            get { return _folderPath; }
            set { _folderPath = value; }
        }


        private string _fileName;
        [Category("Folder"), DisplayName("File name"), Description("The name of the file used to generate the report result. The file name can be formatted with the execution date time and can contain the keyword " + Repository.SealReportDisplayNameKeyword + " to specify the current report display name."), Id(3, 2)]
        [TypeConverter(typeof(CustomFormatConverter))]
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        private string _emailTo;
        [Category("Email Addresses"), DisplayName("TO"), Description("The destination (To) email addresses used for the email. One per line or separated by semi-column."), Id(2, 2)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string EmailTo
        {
            get { return _emailTo; }
            set { _emailTo = value; }
        }

        private string _emailCC;
        [Category("Email Addresses"), DisplayName("CC"), Description("The email CC (Carbon Copy) addresses used for the email. One per line or separated by semi-column."), Id(3, 2)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string EmailCC
        {
            get { return _emailCC; }
            set { _emailCC = value; }
        }

        private string _emailBCC;
        [Category("Email Addresses"), DisplayName("BCC"), Description("The email BCC (Blind Carbon Copy) addresses used for the email. One per line or separated by semi-column."), Id(4, 2)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string EmailBCC
        {
            get { return _emailBCC; }
            set { _emailBCC = value; }
        }

        private string _emailFrom;
        [Category("Email Addresses"), DisplayName("Sender"), Description("The email address of the sender (From). If empty, the sender email address defined in the device is used. Make sure that the SMTP server allows the new address."), Id(11, 2)]
        public string EmailFrom
        {
            get { return _emailFrom; }
            set { _emailFrom = value; }
        }

        private string _emailReplyTo;
        [Category("Email Addresses"), DisplayName("Reply"), Description("The reply addresses used for the email. One per line or separated by semi-column. If empty, the reply addresses defined in the device are used."), Id(12, 2)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string EmailReplyTo
        {
            get { return _emailReplyTo; }
            set { _emailReplyTo = value; }
        }

        private string _emailSubject;
        [Category("Email Subject"), DisplayName("Subject"), Description("The subject of the email sent. If empty, the report name is used."), Id(2, 3)]
        public string EmailSubject
        {
            get { return _emailSubject; }
            set { _emailSubject = value; }
        }


        private bool _emailHtmlBody = false;
        [DefaultValue(false)]
        [Category("Email Body"), DisplayName("Use HTML result for email body"), Description("If true, the report result is copied in the email body message."), Id(2, 4)]
        public bool EmailHtmlBody
        {
            get { return _emailHtmlBody; }
            set { _emailHtmlBody = value; UpdateEditorAttributes(); }
        }

        private bool _emailMessagesInBody = false;
        [DefaultValue(false)]
        [Category("Email Body"), DisplayName("Use execution messages for email body"), Description("If true, the report execution messages are copied in the email body message."), Id(3, 4)]
        public bool EmailMessagesInBody
        {
            get { return _emailMessagesInBody; }
            set { _emailMessagesInBody = value; UpdateEditorAttributes(); }
        }

        private string _emailBody;
        [Category("Email Body"), DisplayName("Body message"), Description("The body message of the email sent. If empty, a default text is used."), Id(4, 4)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string EmailBody
        {
            get { return _emailBody; }
            set { _emailBody = value; }
        }

        private bool _emailZipAttachments = false;
        [DefaultValue(false)]
        [Category("Email Attachments"), DisplayName("Zip attachements"), Description("If true, the email sent will have an attachement with all files zipped."), Id(2, 5)]
        public bool EmailZipAttachments
        {
            get { return _emailZipAttachments; }
            set { _emailZipAttachments = value; UpdateEditorAttributes(); }
        }

        private string _emailZipPassword;
        [Category("Email Attachments"), DisplayName("Zip password"), Description("If not empty, the Zip file attached will be protected with the password."), Id(3, 5)]
        public string EmailZipPassword
        {
            get { return _emailZipPassword; }
            set { _emailZipPassword = value; }
        }

        private bool _emailSkipAttachments = false;
        [DefaultValue(false)]
        [Category("Email Attachments"), DisplayName("Skip attachements"), Description("If true, the email sent will have no attachement. This may be useful if the report has only tasks."), Id(4, 5)]
        public bool EmailSkipAttachments
        {
            get { return _emailSkipAttachments; }
            set { _emailSkipAttachments = value; UpdateEditorAttributes(); }
        }

        private string _userName;
        [Category("Security and Publication"), DisplayName("User name"), Description("If not empty, the output is generated with a security context having the name specified."), Id(1, 6)]
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        private string _userGroups;
        [Category("Security and Publication"), DisplayName("User groups"), Description("If not empty, the output is generated with a security context having the groups specified. One group name per line or separated by semi-column."), Id(2, 6)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string UserGroups
        {
            get { return _userGroups; }
            set { _userGroups = value; }
        }

        string _userCulture;
        [Category("Security and Publication"), DisplayName("Culture"), Description("The culture used to generate the report. If empty, the culture from the groups is used, then the default culture."), Id(3, 6)]
        [TypeConverter(typeof(Seal.Converter.CultureInfoConverter))]
        public string UserCulture
        {
            get { return _userCulture; }
            set { _userCulture = value; }
        }

        private bool _publicExec = true;
        [DefaultValue(true)]
        [Category("Security and Publication"), DisplayName("Public Execution"), Description("For the Web Report Server: If true, the output can be executed by all users having the execute right on the report. If false, only the user owner can execute the schedule."), Id(4, 6)]
        public bool PublicExec
        {
            get { return _publicExec; }
            set { _publicExec = value; }
        }

        private bool _publicEdit = true;
        [DefaultValue(true)]
        [Category("Security and Publication"), DisplayName("Public Edit"), Description("For the Web Report Server Designer: If true, the output and shedule can be edited by all users having the schedule right on the report. If false, only the user owner can edit the schedule."), Id(4, 6)]
        public bool PublicEdit
        {
            get { return _publicEdit; }
            set { _publicEdit = value; }
        }


        [XmlIgnore]
        public ReportFormat Format
        {
            get {
                var param = ViewParameters.FirstOrDefault(i => i.Name == Parameter.ReportFormatParameter && i.CustomValue);
                if (param != null) return (ReportFormat)Enum.Parse(typeof(ReportFormat), param.Value);
                return Report.Format;
            }
            set {
                var param = ViewParameters.FirstOrDefault(i => i.Name == Parameter.ReportFormatParameter);
                if (param != null) {
                    param.CustomValue = true;
                    param.Value = value.ToString();
                }
            }
        }

        [XmlIgnore]
        public OutputDevice Device
        {
            get
            {
                return Report.Repository.Devices.FirstOrDefault(i => i.GUID == OutputDeviceGUID);
            }
        }

        [XmlIgnore]
        public ReportView View
        {
            get
            {
                if (Report != null) return Report.FindView(Report.Views, _viewGUID);
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
        [DefaultValue(false)]
        [Category("Restrictions"), DisplayName("Use Custom restrictions"), Description("If true, custom restrictions can be defined for this output."), Id(2, 6)]
        public bool UseCustomRestrictions
        {
            get { return _useCustomRestrictions; }
            set
            {
                _useCustomRestrictions = value;
                UpdateEditorAttributes();
            }
        }

        List<ReportRestriction> _restrictions = new List<ReportRestriction>();
        [Category("Restrictions"), DisplayName("Custom restrictions"), Description("Custom restrictions applied to this output."), Id(3, 6)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
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
        string _information;
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(2, 20)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        string _error;
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(3, 20)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }


        //Temporary variables to help for report serialization...
        private List<OutputParameter> _tempParameters;

        public void BeforeSerialization()
        {
            _tempParameters = _viewParameters.ToList();
            //Remove parameters not used
            _viewParameters.RemoveAll(i => !i.CustomValue);
        }

        public void CopyParameters(List<Parameter> destination)
        {
            foreach (var parameter in ViewParameters.Where(i => i.CustomValue))
            {
                var destParameter = destination.FirstOrDefault(i => i.Name == parameter.Name);
                if (destParameter != null) destParameter.Value = parameter.Value;
            }
        }


        public void AfterSerialization()
        {
            _viewParameters = _tempParameters;
        }


        #endregion

    }


}
