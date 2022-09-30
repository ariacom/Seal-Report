//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Mail;
using System.Xml.Serialization;
using Seal.Helpers;
using System.Xml;
using SendGrid;
using SendGrid.Helpers.Mail;
#if WINDOWS
using DynamicTypeDescriptor;
using System.ComponentModel.Design;
using System.Drawing.Design;
using Seal.Forms;
#endif

namespace Seal.Model
{
    /// <summary>
    /// OutputEmailDevice is an implementation of a SMTP Server device to send report result by emails. 
    /// </summary>
    public class OutputEmailDevice : OutputDevice
    {
        static string PasswordKey = "qdeferlwien?,édl+25.()à,";
        static string SendGridKeyKey = "1d2fDFsdsdien32345,sadnD";

#if WINDOWS
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
                GetProperty("UseSendGrid").SetIsBrowsable(true);
                GetProperty("ClearSendGridKey").SetIsBrowsable(true);

                GetProperty("HelperTestEmail").SetIsBrowsable(true);
                GetProperty("TestEmailTo").SetIsBrowsable(true);
                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

#endif
        /// <summary>
        /// Create a basic OutputEmailDevice
        /// </summary>
        static public OutputEmailDevice Create()
        {
            var result = new OutputEmailDevice() { GUID = Guid.NewGuid().ToString() };
            result.Name = "Email Device";
            return result;
        }

        /// <summary>
        /// Full name
        /// </summary>
        [XmlIgnore]
        public override string FullName
        {
            get { return string.Format("{0} (EMail)", Name); }
        }

        /// <summary>
        /// Last modification date time
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification;

        /// <summary>
        /// SMTP Email Server name
        /// </summary>
#if WINDOWS
        [Category("SMTP Definition"), DisplayName("Server"), Description("SMTP Email Server name."), Id(1, 1)]
#endif
        public string Server { get; set; }

        /// <summary>
        /// SMTP Port used to connect to the server
        /// </summary>
#if WINDOWS
        [Category("SMTP Definition"), DisplayName("Port"), Description("SMTP Port used to connect to the server."), Id(2, 1)]
        [DefaultValue(25)]
#endif
        public int Port { get; set; } = 25;

        /// <summary>
        /// The user name used to connect to the SMTP server
        /// </summary>
#if WINDOWS
        [Category("SMTP Definition"), DisplayName("User name"), Description("The user name used to connect to the SMTP server"), Id(3, 1)]
#endif
        public string UserName { get; set; }

        /// <summary>
        /// The password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The clear password used to connect to the SMTP server
        /// </summary>
#if WINDOWS
        [Category("SMTP Definition"), DisplayName("Password"), Description("The password used to connect to the SMTP server"), PasswordPropertyText(true), Id(4, 1)]
        [XmlIgnore]
#endif
        public string ClearPassword
        {
            get
            {
                try
                {
                    return CryptoHelper.DecryptTripleDES(Password, PasswordKey);
                }
                catch (Exception ex)
                {
                    Error = "Error during password decryption:" + ex.Message;
                    TypeDescriptor.Refresh(this);
                    return Password;
                }
            }
            set
            {
                try
                {
                    Password = CryptoHelper.EncryptTripleDES(value, PasswordKey);
                }
                catch (Exception ex)
                {
                    Error = "Error during password encryption:" + ex.Message;
                    Password = value;
                    TypeDescriptor.Refresh(this);
                }
            }
        }

        /// <summary>
        /// Specifies how outgoing email messages will be handled
        /// </summary>
#if WINDOWS
        [Category("SMTP Definition"), DisplayName("Delivery Method"), Description("Specifies how outgoing email messages will be handled."), Id(5, 1)]
        [DefaultValue(SmtpDeliveryMethod.Network)]
#endif
        public SmtpDeliveryMethod DeliveryMethod { get; set; } = SmtpDeliveryMethod.Network;

        /// <summary>
        /// If true, the client uses Secure Socket Layer
        /// </summary>
#if WINDOWS
        [Category("SMTP Definition"), DisplayName("Enable SSL"), Description("If true, the client uses Secure Socket Layer."), Id(6, 1)]
        [DefaultValue(false)]
#endif
        public bool EnableSsl { get; set; } = false;

        /// <summary>
        /// Amount of time in milli-seconds after which the email is not sent
        /// </summary>
#if WINDOWS
        [Category("SMTP Definition"), DisplayName("Time out"), Description("Amount of time in milli-seconds after which the email is not sent."), Id(7, 1)]
        [DefaultValue(100000)]
#endif
        public int Timeout { get; set; } = 100000;

        /// <summary>
        /// If true, the default credentials are used
        /// </summary>
#if WINDOWS
        [Category("SMTP Definition"), DisplayName("Use Default Credentials"), Description("If true, the default credentials are used."), Id(8, 1)]
        [DefaultValue(false)]
#endif
        public bool UseDefaultCredentials { get; set; } = false;


        /// <summary>
        /// If true, the SendGrid client is used instead of the SMTP client
        /// </summary>
#if WINDOWS
        [Category("SendGrid Definition"), DisplayName("Use SendGrid"), Description("If true, the SendGrid client is used instead of the SMTP client."), Id(1, 2)]
        [DefaultValue(false)]
#endif
        public bool UseSendGrid { get; set; } = false;

        /// <summary>
        /// The API Key for SendGrid
        /// </summary>
        public string SendGridKey { get; set; }

        /// <summary>
        /// The clear API Key used for SendGrid. An API Key can be get from https://sendgrid.com/ after registrstion (Free plan).
        /// </summary>
#if WINDOWS
        [Category("SendGrid Definition"), DisplayName("API Key"), Description("The API Key used for SendGrid.Get an API Key from https://sendgrid.com/ after registration (Free plan)."), PasswordPropertyText(true), Id(2, 2)]
        [XmlIgnore]
#endif
        public string ClearSendGridKey
        {
            get
            {
                try
                {
                    return CryptoHelper.DecryptAES(SendGridKey, SendGridKeyKey);
                }
                catch (Exception ex)
                {
                    Error = "Error during SendGridKey decryption:" + ex.Message;
                    TypeDescriptor.Refresh(this);
                    return SendGridKey;
                }
            }
            set
            {
                try
                {
                    SendGridKey = CryptoHelper.EncryptAES(value, SendGridKeyKey);
                }
                catch (Exception ex)
                {
                    Error = "Error during SendGridKey encryption:" + ex.Message;
                    SendGridKey = value;
                    TypeDescriptor.Refresh(this);
                }
            }
        }


        /// <summary>
        /// The sender email address used to send the email
        /// </summary>
#if WINDOWS
        [Category("Addresses"), DisplayName("Sender Email"), Description("The sender email address used to send the email."), Id(1, 3)]
#endif
        public string SenderEmail { get; set; }

        /// <summary>
        /// The reply addresses used for the email
        /// </summary>
#if WINDOWS
        [Category("Addresses"), DisplayName("Reply addresses"), Description("The reply addresses used for the email."), Id(2, 3)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
#endif
        public string ReplyTo { get; set; }

        /// <summary>
        /// If true, this email device will be chosen first to be used for notifications. (e.g. sending an email in case of error in a schedule)
        /// </summary>
#if WINDOWS
        [Category("Advanced"), DisplayName("Used for notification"), Description("If true, this email device will be chosen first to be used for notifications. (e.g. sending an email in case of error in a schedule)"), Id(1, 5)]
        [DefaultValue(false)]
#endif
        public bool UsedForNotification { get; set; } = false;

        /// <summary>
        /// If true, the Email Sender or Reply address can be changed in the Report Designer or the Web Report Designer
        /// </summary>
#if WINDOWS
        [Category("Advanced"), DisplayName("Allow to change Email Sender or Reply Address"), Description("If true, the Email Sender or Reply address can be changed in the Report Designer or the Web Report Designer."), Id(2, 5)]
        [DefaultValue(true)]
#endif
        public bool ChangeSender { get; set; } = true;

        /// <summary>
        /// The destination email address used to send the test email
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Email adress for the test"), Description("The destination email address used to send the test email."), Id(1, 10)]
#endif
        [XmlIgnore]
        public string TestEmailTo { get; set; }

        /// <summary>
        /// Editor Helper: Send a test email with the current configuration
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Send test Email"), Description("Send a test email with the current configuration."), Id(2, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
#endif
        public string HelperTestEmail
        {
            get { return "<Click to send a test Email>"; }
        }

        /// <summary>
        /// Last information message
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(4, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string Information { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(5, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string Error { get; set; }

        /// <summary>
        /// Load an OutputEmailDevice from a file
        /// </summary>
        static public OutputDevice LoadFromFile(string path, bool ignoreException)
        {
            OutputEmailDevice result = null;
            try
            {
                path = FileHelper.ConvertOSFilePath(path);
                if (!File.Exists(path)) throw new Exception("File not found: " + path);

                XmlSerializer serializer = new XmlSerializer(typeof(OutputEmailDevice));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (OutputEmailDevice)serializer.Deserialize(xr);
                    xr.Close();
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

        /// <summary>
        /// Save to current file
        /// </summary>
        public override void SaveToFile()
        {
            SaveToFile(FilePath);
        }

        /// <summary>
        /// Save to a file
        /// </summary>
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
            Helper.Serialize(path, this);
            FilePath = path;
            LastModification = File.GetLastWriteTime(path);
        }

        /// <summary>
        /// Validate the device
        /// </summary>
        public override void Validate()
        {
            if (!UseSendGrid && string.IsNullOrEmpty(Server)) throw new Exception("The SMTP Server cannot be empty for an Email device.");
            if (string.IsNullOrEmpty(SenderEmail)) throw new Exception("The Email Sender cannot be empty for an Email device.");
        }

        /// <summary>
        /// Send an Email either through SMTP or SendGrid
        /// </summary>
        public void SendEmail(string sender, string to, string subject, bool isHtmlBody, string body)
        {
            SendEmail(sender, to, "", "", "", subject, isHtmlBody, body, "", "");
        }


        /// <summary>
        /// Send an Email either through SMTP or SendGrid
        /// </summary>
        public void SendEmail(string sender, string to, string replyTo, string cc, string bcc, string subject, bool isHtmlBody, string body, string attachPath, string attachName) 
        {
            if (string.IsNullOrEmpty(sender)) sender = SenderEmail;
            if (string.IsNullOrEmpty(replyTo)) replyTo = sender;

            if (UseSendGrid)
            {
                var sendGridClient = new SendGridClient(ClearSendGridKey);

                    var msg = new SendGridMessage()
                {
                    From = new EmailAddress(sender),
                    Subject = subject,
                    PlainTextContent = isHtmlBody ? "" : body,
                    HtmlContent = !isHtmlBody ? "" : body,
                    ReplyTo = new EmailAddress(replyTo)
                };


                msg.SetSubject(subject);
                foreach (var addr in Helper.GetEmailAddresses(to)) msg.AddTo(addr);
                foreach (var addr in Helper.GetEmailAddresses(cc)) msg.AddCc(addr);
                foreach (var addr in Helper.GetEmailAddresses(bcc)) msg.AddBcc(addr);

                if (!string.IsNullOrEmpty(attachPath))
                {
                    var bytes = File.ReadAllBytes(attachPath);
                    var file = Convert.ToBase64String(bytes);
                    msg.AddAttachment(attachName, file);
                }

                var response = sendGridClient.SendEmailAsync(msg).Result;
                if (!response.IsSuccessStatusCode) throw new Exception($"Error sending Email through SendGrid to {to}\r\n{response.Body.ReadAsStringAsync().Result}.");
            }
            else
            {
                MailMessage message = new MailMessage();

                message.From = new MailAddress(sender);
                Helper.AddEmailAddresses(message.To, to);
                Helper.AddEmailAddresses(message.CC, cc);
                Helper.AddEmailAddresses(message.Bcc, bcc);
                Helper.AddEmailAddresses(message.ReplyToList, replyTo);
                message.Subject = subject;

                //Body
                message.IsBodyHtml = isHtmlBody;
                message.Body = body;

                //Attachment
                if (!string.IsNullOrEmpty(attachPath))
                {
                    message.Attachments.Add(new System.Net.Mail.Attachment(attachPath, attachName));
                }
                SmtpClient client = SmtpClient;
                client.Send(message);
            }
        }

        /// <summary>
        /// Send the report result by email using the device configuration
        /// </summary>
        public override void Process(Report report)
        {
            ReportOutput output = report.OutputToExecute;

            if (string.IsNullOrEmpty(output.EmailTo)) throw new Exception("No email address has been specified in the report output.");
            var subject = Helper.IfNullOrEmpty(output.EmailSubject, report.ExecutionName);
            var sender = SenderEmail;
            if (ChangeSender && !string.IsNullOrEmpty(output.EmailFrom)) sender = output.EmailFrom;
            var replyTo = SenderEmail;
            if (ChangeSender && !string.IsNullOrEmpty(output.EmailReplyTo)) replyTo = output.EmailReplyTo;
            //Body
            var body = Helper.IfNullOrEmpty(output.EmailBody, report.Translate("Please find the report '{0}' in attachment.", report.ExecutionName));
            if (output.EmailHtmlBody && (output.Format == ReportFormat.html || output.Format == ReportFormat.print))
            {
                body = File.ReadAllText(report.ResultFilePath);
            }
            else if (output.EmailMessagesInBody || output.EmailSkipAttachments)
            {
                body = string.Format("Execution messages for report '{0}':\r\n\r\n{1}", report.FilePath, report.ExecutionMessages);
            }
            bool isHtmlBody = (output.EmailHtmlBody && (output.Format == ReportFormat.html || output.Format == ReportFormat.print));
            //Attachment
            string attachPath = "", attachName = "";
            if (!isHtmlBody && !output.EmailSkipAttachments)
            {
                HandleZipOptions(report);
                attachPath = report.ResultFilePath;
                if (output.Format == ReportFormat.html || output.Format == ReportFormat.print)
                {
                    //Copy the file to avoid a lock
                    attachPath = FileHelper.GetTempUniqueFileName(report.ResultFilePath);
                    attachName = Path.GetFileNameWithoutExtension(report.ResultFileName) + Path.GetExtension(report.ResultFilePath);
                    File.Copy(report.ResultFilePath, attachPath);
                }
            }

            SendEmail(sender, output.EmailTo, replyTo, output.EmailCC, output.EmailBCC, subject, isHtmlBody, body, attachPath, attachName);

            output.Information = report.Translate("Email sent to '{0}'", output.EmailTo.Replace("\r\n", ";"));
            report.LogMessage("Email sent to '{0}'", output.EmailTo.Replace("\r\n", ";"));
        }

        /// <summary>
        /// Current SmtpClient
        /// </summary>
        public SmtpClient SmtpClient
        {
            get
            {
                var client = new SmtpClient()
                {
                    Host = Server,
                    Port = Port,
                    DeliveryMethod = DeliveryMethod,
                    EnableSsl = EnableSsl,
                    Timeout = Timeout,
                    UseDefaultCredentials = UseDefaultCredentials
                };
                if (!string.IsNullOrEmpty(UserName)) client.Credentials = new System.Net.NetworkCredential(UserName, ClearPassword);
                return client;
            }
        }

        /// <summary>
        /// Helper to send a test email
        /// </summary>
        public void SendTestEmail()
        {
            try
            {
                Error = "";
                Information = "";

                if (string.IsNullOrEmpty(TestEmailTo)) throw new Exception("No email address has been specified in the destination email.");
                if (string.IsNullOrEmpty(SenderEmail)) throw new Exception("No sender email address has been specified.");

                if (UseSendGrid)
                {
                    var sendGridClient = new SendGridClient(ClearSendGridKey);
                    var msg = new SendGridMessage()
                    {
                        From = new EmailAddress(SenderEmail),
                        Subject = "Test email",
                        PlainTextContent = "This is a test message.",
                        HtmlContent = "This is a test message."
                    };
                    msg.AddTo(new EmailAddress(TestEmailTo));
                    var response = sendGridClient.SendEmailAsync(msg).Result;
                    if (!response.IsSuccessStatusCode) throw new Exception($"Error sending Email through SendGrid.\r\n{response.Body.ReadAsStringAsync().Result}");

                }
                else
                {
                    MailMessage message = new MailMessage();
                    message.To.Add(TestEmailTo);
                    message.From = new MailAddress(SenderEmail);
                    Helper.AddEmailAddresses(message.ReplyToList, ReplyTo);
                    message.Subject = "Test email";
                    message.Body = "This is a test message.";
                    SmtpClient.Send(message);
                }
                Information = string.Format("A test email has been sent successfully to '{0}'. Please check your inbox...", TestEmailTo);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                if (ex.InnerException != null) Error += " " + ex.InnerException.Message.Trim();
                Information = "Error got when sending the test Email.";
            }
        }
    }
}
