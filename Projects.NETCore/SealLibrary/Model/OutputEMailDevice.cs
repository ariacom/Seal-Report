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
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Xml;

namespace Seal.Model
{
    /// <summary>
    /// OutputEmailDevice is an implementation of a SMTP Server device to send report result by emails. 
    /// </summary>
    public class OutputEmailDevice : OutputDevice
    {
        static string PasswordKey = "qdeferlwien?,édl+25.()à,";


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
        public string Server { get; set; }

        /// <summary>
        /// SMTP Port used to connect to the server
        /// </summary>
        public int Port { get; set; } = 25;

        /// <summary>
        /// The user name used to connect to the SMTP server
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The clear password used to connect to the SMTP server
        /// </summary>
        [XmlIgnore]
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
        /// The sender email address used to send the email
        /// </summary>
        public string SenderEmail { get; set; }

        /// <summary>
        /// The reply addresses used for the email
        /// </summary>
        public string ReplyTo { get; set; }

        /// <summary>
        /// Specifies how outgoing email messages will be handled
        /// </summary>
        public SmtpDeliveryMethod DeliveryMethod { get; set; } = SmtpDeliveryMethod.Network;

        /// <summary>
        /// If true, the client uses Secure Socket Layer
        /// </summary>
        public bool EnableSsl { get; set; } = false;

        /// <summary>
        /// Amount of time in milli-seconds after which the email is not sent
        /// </summary>
        public int Timeout { get; set; } = 100000;

        /// <summary>
        /// If true, the default credentials are used
        /// </summary>
        public bool UseDefaultCredentials { get; set; } = false;

        /// <summary>
        /// If true, this email device will be chosen first to be used for notifications. (e.g. sending an email in case of error in a schedule)
        /// </summary>
        public bool UsedForNotification { get; set; } = false;

        /// <summary>
        /// If true, the Email Sender or Reply address can be changed in the Report Designer or the Web Report Designer
        /// </summary>
        public bool ChangeSender { get; set; } = true;

        /// <summary>
        /// The destination email address used to send the test email
        /// </summary>
        [XmlIgnore]
        public string TestEmailTo { get; set; }

        /// <summary>
        /// Editor Helper: Send a test email with the current configuration
        /// </summary>
        public string HelperTestEmail
        {
            get { return "<Click to send a test Email>"; }
        }

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

        /// <summary>
        /// Validate the device
        /// </summary>
        public override void Validate()
        {
            if (string.IsNullOrEmpty(Server)) throw new Exception("The SMTP Server cannot be empty for an Email device.");
            if (string.IsNullOrEmpty(SenderEmail)) throw new Exception("The Email Sender cannot be empty for an Email device.");
        }


        /// <summary>
        /// Send the report result by email using the device configuration
        /// </summary>
        public override void Process(Report report)
        {
            ReportOutput output = report.OutputToExecute;

            if (string.IsNullOrEmpty(output.EmailTo)) throw new Exception("No email address has been specified in the report output.");

            MailMessage message = new MailMessage();
            var email = SenderEmail;
            if (ChangeSender && !string.IsNullOrEmpty(output.EmailFrom)) email = output.EmailFrom;
            message.From = new MailAddress(email);
            Helper.AddEmailAddresses(message.To, output.EmailTo);
            Helper.AddEmailAddresses(message.CC, output.EmailCC);
            Helper.AddEmailAddresses(message.Bcc, output.EmailBCC);
            email = ReplyTo;
            if (ChangeSender && !string.IsNullOrEmpty(output.EmailReplyTo)) email = output.EmailReplyTo;
            Helper.AddEmailAddresses(message.ReplyToList, email);
            message.Subject = Helper.IfNullOrEmpty(output.EmailSubject, report.ExecutionName);

            //Body
            if (output.EmailHtmlBody && (output.Format == ReportFormat.html || output.Format == ReportFormat.print))
            {
                message.Body = File.ReadAllText(report.ResultFilePath);
                message.IsBodyHtml = true;
            }
            else if (output.EmailMessagesInBody || output.EmailSkipAttachments)
            {
                message.Body = string.Format("Execution messages for report '{0}':\r\n\r\n{1}", report.FilePath, report.ExecutionMessages);
            }
            else
            {
                message.Body = Helper.IfNullOrEmpty(output.EmailBody, report.Translate("Please find the report '{0}' in attachment.", report.ExecutionName));
            }

            //Attachment
            if (!message.IsBodyHtml && !output.EmailSkipAttachments)
            {
                HandleZipOptions(report);
                var attachment = new Attachment(report.ResultFilePath);
                attachment.Name = Path.GetFileNameWithoutExtension(report.ResultFileName) + Path.GetExtension(report.ResultFilePath);
                message.Attachments.Add(attachment);
            }
            SmtpClient client = SmtpClient;
            client.Send(message);
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
                var client = new SmtpClient() { 
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

                MailMessage message = new MailMessage();
                message.To.Add(TestEmailTo);
                message.From = new MailAddress(SenderEmail);
                Helper.AddEmailAddresses(message.ReplyToList, ReplyTo);
                message.Subject = "Test email";
                message.Body = "This is a test message.";
                SmtpClient.Send(message);

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

