//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Mail;
using System.Xml.Serialization;
using DynamicTypeDescriptor;
using Seal.Helpers;
using System.ComponentModel.Design;
using System.Drawing.Design;
using Ionic.Zip;
using Seal.Forms;
using System.Xml;

namespace Seal.Model
{
    public class OutputEmailDevice : OutputDevice
    {
        static string PasswordKey = "qdeferlwien?,édl+25.()à,";

        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Server").SetIsBrowsable(true);
                GetProperty("UserName").SetIsBrowsable(true);
                GetProperty("ClearPassword").SetIsBrowsable(true);
                GetProperty("Port").SetIsBrowsable(true);
                GetProperty("SenderEmail").SetIsBrowsable(true);
                GetProperty("ReplyTo").SetIsBrowsable(true);
                GetProperty("UseDefaultCredentials").SetIsBrowsable(true);
                GetProperty("DeliveryMethod").SetIsBrowsable(true);
                GetProperty("EnableSsl").SetIsBrowsable(true);
                GetProperty("ChangeSender").SetIsBrowsable(true);
                GetProperty("Timeout").SetIsBrowsable(true);
                GetProperty("UsedForNotification").SetIsBrowsable(true);

                GetProperty("HelperTestEmail").SetIsBrowsable(true);
                GetProperty("TestEmailTo").SetIsBrowsable(true);
                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

        static public OutputEmailDevice Create()
        {
            OutputEmailDevice result = new OutputEmailDevice() { GUID = Guid.NewGuid().ToString() };
            result.Name = "Email Device";
            return result;
        }

        [XmlIgnore]
        public override string FullName
        {
            get { return string.Format("{0} (EMail)", Name); }
        }

        [XmlIgnore]
        public DateTime LastModification;

        string _server;
        [Category("Definition"), DisplayName("SMTP Server"), Description("SMTP Email Server name."), Id(1, 1)]
        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        int _port = 25;
        [Category("Definition"), DisplayName("SMTP Port"), Description("SMTP Port used to connect to the server."), Id(2, 1)]
        [DefaultValue(25)]
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        string _userName;
        [Category("Definition"), DisplayName("SMTP User name"), Description("The user name used to connect to the SMTP server"), Id(3, 1)]
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        [Category("Definition"), DisplayName("SMTP Password"), Description("The password used to connect to the SMTP server"), PasswordPropertyText(true), Id(4, 1)]
        [XmlIgnore]
        public string ClearPassword
        {
            get
            {
                try
                {
                    return CryptoHelper.DecryptTripleDES(_password, PasswordKey);
                }
                catch (Exception ex)
                {
                    _error = "Error during password decryption:" + ex.Message;
                    TypeDescriptor.Refresh(this);
                    return _password;
                }
            }
            set
            {
                try
                {
                    _password = CryptoHelper.EncryptTripleDES(value, PasswordKey);
                }
                catch (Exception ex)
                {
                    _error = "Error during password encryption:" + ex.Message;
                    _password = value;
                    TypeDescriptor.Refresh(this);
                }
            }
        }

        string _senderEmail;
        [Category("Definition"), DisplayName("Sender Email"), Description("The sender email address used to send the email."), Id(5, 1)]
        public string SenderEmail
        {
            get { return _senderEmail; }
            set { _senderEmail = value; }
        }

        string _replyTo;
        [Category("Definition"), DisplayName("Reply addresses"), Description("The reply addresses used for the email."), Id(6, 1)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string ReplyTo
        {
            get { return _replyTo; }
            set { _replyTo = value; }
        }


        SmtpDeliveryMethod _deliveryMethod = SmtpDeliveryMethod.Network;
        [Category("Advanced"), DisplayName("Delivery Method"), Description("Specifies how outgoing email messages will be handled."), Id(1, 2)]
        [DefaultValue(SmtpDeliveryMethod.Network)]
        public SmtpDeliveryMethod DeliveryMethod
        {
            get { return _deliveryMethod; }
            set { _deliveryMethod = value; }
        }

        bool _enableSsl = false;
        [Category("Advanced"), DisplayName("Enable SSL"), Description("If true, the client uses Secure Socket Layer."), Id(2, 2)]
        [DefaultValue(false)]
        public bool EnableSsl
        {
            get { return _enableSsl; }
            set { _enableSsl = value; }
        }

        int _timeout = 100000;
        [Category("Advanced"), DisplayName("Time out"), Description("Amount of time in milli-seconds after which the email is not sent."), Id(3, 2)]
        [DefaultValue(100000)]
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }
        bool _useDefaultCredentials = false;
        [Category("Advanced"), DisplayName("Use Default Credentials"), Description("If true, the default credentials are used."), Id(4, 2)]
        [DefaultValue(false)]
        public bool UseDefaultCredentials
        {
            get { return _useDefaultCredentials; }
            set { _useDefaultCredentials = value; }
        }

        bool _usedForNotification = false;
        [Category("Advanced"), DisplayName("Used for notification"), Description("If true, this email device will be chosen first to be used for notifications. (e.g. sending an email in case of error in a schedule)"), Id(5, 2)]
        [DefaultValue(false)]
        public bool UsedForNotification
        {
            get { return _usedForNotification; }
            set { _usedForNotification = value; }
        }

        bool _changeSender = true;
        [Category("Advanced"), DisplayName("Allow to change Email Sender or Reply Address"), Description("If true, the Email Sender or Reply address can be changed in the Report Designer or the Web Report Designer."), Id(6, 2)]
        [DefaultValue(true)]
        public bool ChangeSender
        {
            get { return _changeSender; }
            set { _changeSender = value; }
        }

        string _emailTo;
        [XmlIgnore, Category("Helpers"), DisplayName("Email adress for the test"), Description("The destination email address used to send the test email."), Id(1, 10)]
        public string TestEmailTo
        {
            get { return _emailTo; }
            set { _emailTo = value; }
        }

        [Category("Helpers"), DisplayName("Send test Email"), Description("Send a test email with the current configuration."), Id(2, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperTestEmail
        {
            get { return "<Click to send a test Email>"; }
        }

        string _information;
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message ther column has been checked."), Id(4, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        string _error;
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(5, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }


        static public OutputDevice LoadFromFile(string path, bool ignoreException)
        {
            OutputEmailDevice result = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(OutputEmailDevice));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (OutputEmailDevice)serializer.Deserialize(xr);
                }
                result.Name = Path.GetFileNameWithoutExtension(path);
                result.FilePath = path;
                result.LastModification = File.GetLastWriteTime(path);
            }
            catch (Exception ex)
            {
                if (!ignoreException) throw new Exception(string.Format("Unable to read the file '{0}'.\r\n{1}", path, ex.Message));
            }
            return result;
        }


        public override void SaveToFile()
        {
            SaveToFile(FilePath);
        }

        public override void SaveToFile(string path)
        {
            //Check last modification
            if (LastModification != DateTime.MinValue && File.Exists(path))
            {
                DateTime lastDateTime = File.GetLastWriteTime(path);
                if (LastModification != lastDateTime)
                {
                    throw new Exception("Unable to save the Output Device file. The file has been modified by another user.");
                }
            }

            Name = Path.GetFileNameWithoutExtension(path);
            XmlSerializer serializer = new XmlSerializer(typeof(OutputEmailDevice));
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.NewLineHandling = NewLineHandling.Entitize;
            using (XmlWriter xw = XmlWriter.Create(path, ws))
            {
                serializer.Serialize(xw, this);
            }
            FilePath = path;
            LastModification = File.GetLastWriteTime(path);
        }


        public override void Validate()
        {
            if (string.IsNullOrEmpty(Server)) throw new Exception("The SMTP Server cannot be empty for an Email device.");
            if (string.IsNullOrEmpty(SenderEmail)) throw new Exception("The Email Sender cannot be empty for an Email device.");
        }

        public void AddEmailAddresses(MailAddressCollection collection, string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                string[] addresses = input.Replace(";", "\r\n").Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                foreach (string address in addresses)
                {
                    if (!string.IsNullOrWhiteSpace(address)) collection.Add(address);
                }
            }
        }

        public override string Process(Report report)
        {
            ReportOutput output = report.OutputToExecute;

            if (string.IsNullOrEmpty(output.EmailTo)) throw new Exception("No email address has been specified in the report output.");

            MailMessage message = new MailMessage();
            var email = SenderEmail;
            if (ChangeSender && !string.IsNullOrEmpty(output.EmailFrom)) email = output.EmailFrom;
            message.From = new MailAddress(email);
            AddEmailAddresses(message.To, output.EmailTo);
            AddEmailAddresses(message.CC, output.EmailCC);
            AddEmailAddresses(message.Bcc, output.EmailBCC);
            email = ReplyTo;
            if (ChangeSender && !string.IsNullOrEmpty(output.EmailReplyTo)) email = output.EmailReplyTo;
            AddEmailAddresses(message.ReplyToList, email);
            message.Subject = Helper.IfNullOrEmpty(output.EmailSubject, report.ExecutionName);
            if (output.EmailHtmlBody && (output.Format == ReportFormat.html || output.Format == ReportFormat.print))
            {
                message.Body = File.ReadAllText(report.ResultFilePath);
                message.IsBodyHtml = true;
            }
            else if (output.EmailMessagesInBody)
            {
                message.Body = string.Format("Execution messages for report '{0}':\r\n\r\n{1}", report.FilePath, report.ExecutionMessages);
            }
            else
            {
                if (output.EmailMessagesInBody) message.Body = string.Format("Execution messages for report '{0}':\r\n\r\n{1}", report.FilePath, report.ExecutionMessages);
                else message.Body = Helper.IfNullOrEmpty(output.EmailBody, report.Translate("Please find the report '{0}' in attachment.", report.ExecutionView.Name));

                message.Attachments.Add(new Attachment(report.ResultFilePath));
            }

            //Attachments options
            if (output.EmailSkipAttachments)
            {
                message.Attachments.Clear();
            }
            else if (output.EmailZipAttachments)
            {
                using (ZipFile zip = new ZipFile())
                {
                    if (!string.IsNullOrEmpty(output.EmailZipPassword)) zip.Password = output.EmailZipPassword;
                    foreach (var attachement in message.Attachments) zip.AddFile(Path.Combine(Path.GetDirectoryName(report.ResultFilePath), attachement.Name), ".");
                    string zipName = Path.Combine(Path.GetDirectoryName(report.ResultFilePath), Path.GetFileNameWithoutExtension(report.ResultFilePath) + ".zip");
                    zip.Save(zipName);
                    message.Attachments.Clear();
                    message.Attachments.Add(new Attachment(zipName));
                    message.Attachments[0].Name = report.ExecutionName + ".zip";
                }
            }
            SmtpClient client = SmtpClient;
            client.Send(message);
            output.Information = report.Translate("Email sent to '{0}'", output.EmailTo);
            return string.Format("Email sent to '{0}'", output.EmailTo);
        }

        public SmtpClient SmtpClient
        {
            get
            {
                var client = new SmtpClient() { Host = Server, Port = Port, DeliveryMethod = DeliveryMethod, EnableSsl = EnableSsl, Timeout = Timeout, UseDefaultCredentials = UseDefaultCredentials };
                if (!string.IsNullOrEmpty(UserName)) client.Credentials = new System.Net.NetworkCredential(UserName, ClearPassword);
                return client;
            }
        }

        public void SendTestEmail()
        {
            try
            {
                _error = "";
                _information = "";

                if (string.IsNullOrEmpty(TestEmailTo)) throw new Exception("No email address has been specified in the destination email.");
                if (string.IsNullOrEmpty(SenderEmail)) throw new Exception("No sender email address has been specified.");

                MailMessage message = new MailMessage();
                message.To.Add(TestEmailTo);
                message.From = new MailAddress(SenderEmail);
                AddEmailAddresses(message.ReplyToList, ReplyTo);
                message.Subject = "Test email";
                message.Body = "This is a test message.";
                SmtpClient.Send(message);

                _information = string.Format("A test email has been sent successfully to '{0}'. Please check your inbox...", TestEmailTo);
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                if (ex.InnerException != null) _error += " " + ex.InnerException.Message.Trim();
                _information = "Error got when sending the test Email.";
            }
        }
    }
}
