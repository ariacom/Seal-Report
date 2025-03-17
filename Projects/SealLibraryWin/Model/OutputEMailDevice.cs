//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
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
using Microsoft.Identity.Client;
using System.Net;
using System.Net.Http;
using Azure.Identity;
using Microsoft.Graph.Models;
using Microsoft.Graph;

using System.Collections.Generic;
using System.Threading.Tasks;






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
        public const string PasswordKeyName = "Output Email Device Password";
        public const string PasswordKeyValue = "qdeferlwien?,édl+25.()à,";
        public const string SendGridKeyName = "Output Email Device SendGrid";
        public const string SendGridKeyValue = "1d2fDFsdsdien32345,sadnD";
        public const string AzureSecretKeyName = "Output Email Device Azure Secret";
        public const string AzureSecretKeyValue = "56asdsddsdien;:eweewwcf9";

        /// <summary>
        /// Parameter used to send email by script
        /// </summary>
        public class EmailDefinition
        {
            public OutputEmailDevice device;
            public string sender;
            public string to;
            public string replyTo;
            public string cc;
            public string bcc;
            public string subject;
            public bool isHtmlBody;
            public string body;
            public string attachPath;
            public string attachName;
        }

#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Type").SetIsBrowsable(true);
                GetProperty("ProcessingScript").SetIsBrowsable(true);

                //Smtp
                GetProperty("Server").SetIsBrowsable(true);
                GetProperty("UserName").SetIsBrowsable(true);
                GetProperty("ClearPassword").SetIsBrowsable(true);
                GetProperty("Port").SetIsBrowsable(true);
                GetProperty("UseDefaultCredentials").SetIsBrowsable(true);
                GetProperty("DeliveryMethod").SetIsBrowsable(true);
                GetProperty("EnableSsl").SetIsBrowsable(true);
                GetProperty("ChangeSender").SetIsBrowsable(true);
                GetProperty("Timeout").SetIsBrowsable(true);
                GetProperty("SmtpScript").SetIsBrowsable(true);
                GetProperty("Server").SetIsReadOnly(_type != EmailServerType.SMTP);
                GetProperty("UserName").SetIsReadOnly(_type != EmailServerType.SMTP);
                GetProperty("ClearPassword").SetIsReadOnly(_type != EmailServerType.SMTP);
                GetProperty("Port").SetIsReadOnly(_type != EmailServerType.SMTP);
                GetProperty("UseDefaultCredentials").SetIsReadOnly(_type != EmailServerType.SMTP);
                GetProperty("DeliveryMethod").SetIsReadOnly(_type != EmailServerType.SMTP);
                GetProperty("EnableSsl").SetIsReadOnly(_type != EmailServerType.SMTP);
                GetProperty("ChangeSender").SetIsReadOnly(_type != EmailServerType.SMTP);
                GetProperty("Timeout").SetIsReadOnly(_type != EmailServerType.SMTP);
                GetProperty("SmtpScript").SetIsReadOnly(_type != EmailServerType.SMTP);

                //Sendgrid
                GetProperty("ClearSendGridKey").SetIsBrowsable(true);
                GetProperty("SendGridScript").SetIsBrowsable(true);
                GetProperty("ClearSendGridKey").SetIsReadOnly(_type != EmailServerType.SendGrid);
                GetProperty("SendGridScript").SetIsReadOnly(_type != EmailServerType.SendGrid);

                //MS Graph
                GetProperty("ClearAzureSecret").SetIsBrowsable(true);
                GetProperty("MSGraphScript").SetIsBrowsable(true);
                GetProperty("ClearAzureSecret").SetIsReadOnly(_type != EmailServerType.MSGraph);
                GetProperty("MSGraphScript").SetIsReadOnly(_type != EmailServerType.MSGraph);

                GetProperty("SenderEmail").SetIsBrowsable(true);
                GetProperty("ReplyTo").SetIsBrowsable(true);
                GetProperty("UsedForNotification").SetIsBrowsable(true);

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
        /// Default processing script template
        /// </summary>
        public const string ProcessingScriptTemplate = @"@using System.IO
@{
    Report report = Model;
    ReportOutput output = report.OutputToExecute;
    var device = output.Device as OutputEmailDevice;

    if (string.IsNullOrEmpty(output.EmailTo)) throw new Exception(""No email address has been specified in the report output."");
    var subject = Helper.IfNullOrEmpty(output.EmailSubject, report.ExecutionName);
    var sender = device.SenderEmail;
    if (device.ChangeSender && !string.IsNullOrEmpty(output.EmailFrom)) sender = output.EmailFrom;
    var replyTo = device.SenderEmail;
    if (device.ChangeSender && !string.IsNullOrEmpty(output.EmailReplyTo)) replyTo = output.EmailReplyTo;
    //Body
    var body = Helper.IfNullOrEmpty(output.EmailBody, report.Translate(""Please find the report '{0}' in attachment."", report.ExecutionName));
    if (output.EmailHtmlBody && (output.Format == ReportFormat.html || output.Format == ReportFormat.print))
    {
        body = File.ReadAllText(report.ResultFilePath);
    }
    else if (output.EmailMessagesInBody || output.EmailSkipAttachments)
    {
        body = string.Format(""Execution messages for report '{0}':\r\n\r\n{1}"", report.FilePath, report.ExecutionMessages);
    }
    bool isHtmlBody = (output.EmailHtmlBody && (output.Format == ReportFormat.html || output.Format == ReportFormat.print));
    //Attachment
    string attachPath = """", attachName = """";
    if (!isHtmlBody && !output.EmailSkipAttachments)
    {
        device.HandleZipOptions(report);
        attachPath = report.ResultFilePath;
        attachName = Path.GetFileName(attachPath);
        if (output.Format == ReportFormat.html || output.Format == ReportFormat.print)
        {
            //Copy the file to avoid a lock
            attachPath = FileHelper.GetTempUniqueFileName(report.ResultFilePath);
            attachName = Path.GetFileNameWithoutExtension(report.ResultFileName) + Path.GetExtension(report.ResultFilePath);
            File.Copy(report.ResultFilePath, attachPath);
        }
    }

    device.SendEmail(sender, output.EmailTo, replyTo, output.EmailCC, output.EmailBCC, subject, isHtmlBody, body, attachPath, attachName);

    output.Information = report.Translate(""Email sent to '{0}'"", output.EmailTo.Replace(""\r\n"", ""\n"").Replace(""\r"", ""\n"").Replace(""\n"", "";""));
    report.LogMessage(output.Information);      
}        
";

        /// <summary>
        /// Default script template for SMTP
        /// </summary>
        public const string SmtpScriptTemplate = @"@using System.Net
@using System.Net.Mail

@{
    OutputEmailDevice.EmailDefinition def = Model;
    var device = def.device;

	MailMessage message = new MailMessage();

	message.From = new MailAddress(def.sender);
	Helper.AddEmailAddresses(message.To, def.to);
	Helper.AddEmailAddresses(message.CC, def.cc);
	Helper.AddEmailAddresses(message.Bcc, def.bcc);
	Helper.AddEmailAddresses(message.ReplyToList, def.replyTo);
	message.Subject = def.subject;

	//Body
	message.IsBodyHtml = def.isHtmlBody;
	message.Body = def.body;

	//Attachment
	if (!string.IsNullOrEmpty(def.attachPath))
	{
		var attachment = new System.Net.Mail.Attachment(def.attachPath);
		attachment.Name = def.attachName;
		message.Attachments.Add(attachment);
	}

    //client
    var client = new SmtpClient()
    {
        Host = device.Server,
        Port = device.Port,
        DeliveryMethod = device.DeliveryMethod,
        EnableSsl = device.EnableSsl,
        Timeout = device.Timeout,
        UseDefaultCredentials = device.UseDefaultCredentials
    };
    if (!string.IsNullOrEmpty(device.UserName)) client.Credentials = new NetworkCredential(device.UserName, device.ClearPassword);
    
    //Send message
	client.Send(message);
}
";

        /// <summary>
        /// Default script template for SendGrid
        /// </summary>
        public const string SendGridScriptTemplate = @"@using System.IO
@using SendGrid
@using SendGrid.Helpers.Mail

@{
    OutputEmailDevice.EmailDefinition def = Model;
    var device = def.device;

	var sendGridClient = new SendGridClient(device.ClearSendGridKey);

    //Create message
	var msg = new SendGridMessage()
	{
		From = new EmailAddress(def.sender),
		Subject = def.subject,
		PlainTextContent = def.isHtmlBody ? """" : def.body,
		HtmlContent = !def.isHtmlBody ? """" : def.body,
		ReplyTo = new EmailAddress(def.replyTo)
	};

	msg.SetSubject(def.subject);
	foreach (var addr in Helper.GetEmailAddresses(def.to)) msg.AddTo(addr);
	foreach (var addr in Helper.GetEmailAddresses(def.cc)) msg.AddCc(addr);
	foreach (var addr in Helper.GetEmailAddresses(def.bcc)) msg.AddBcc(addr);

	if (!string.IsNullOrEmpty(def.attachPath))
	{
		var bytes = File.ReadAllBytes(def.attachPath);
		var file = Convert.ToBase64String(bytes);
		msg.AddAttachment(def.attachName, file);
	}

    //Send message
	var response = sendGridClient.SendEmailAsync(msg).Result;
    
	if (!response.IsSuccessStatusCode) {
        throw new Exception($""Error sending Email through SendGrid to {def.to}\r\n{response.Body.ReadAsStringAsync().Result}."");
    }
}
";

        /// <summary>
        /// Default script template for Microsoft Graph
        /// </summary>
        public const string MSGraphScriptTemplate = @"@using Microsoft.Graph.Me.SendMail
@using Microsoft.Graph.Models
@using Microsoft.Graph
@using Azure.Identity
@using System.IO

@{
    OutputEmailDevice.EmailDefinition def = Model;
    var device = def.device;

    //Sample to be modified to send an Email via MS Graph

    var scopes = new[] { "".default"" };
    // TenantId
    var tenant = ""23798d76-74cd-4bfb-9f52-fb3495af6b8b"";
    // ClientId
    var client = ""07399128-13f4-4523-8989-3d141598b658"";
    // UserId
    var user = ""e5c47ab9-54c7-4e51-8626-0f02a72b6dd7"";
    
    // Credential
    var credentials = new ClientSecretCredential(tenant, client,  device.ClearAzureSecret);

    // Graph client instance
    var graphClient = new GraphServiceClient(credentials, scopes);

    // Recipients
    var to = new List<Recipient>();
    var cc = new List<Recipient>();
    var bcc = new List<Recipient>();
    foreach (var addr in Helper.GetEmailAddresses(def.to)) to.Add(new Recipient() { EmailAddress = new Microsoft.Graph.Models.EmailAddress() { Address = addr } });
    foreach (var addr in Helper.GetEmailAddresses(def.cc)) cc.Add(new Recipient() { EmailAddress = new Microsoft.Graph.Models.EmailAddress() { Address = addr } });
    foreach (var addr in Helper.GetEmailAddresses(def.bcc)) bcc.Add(new Recipient() { EmailAddress = new Microsoft.Graph.Models.EmailAddress() { Address = addr } });

    // Body
    var messageBody = new ItemBody
    {
        Content = def.body,
        ContentType = def.isHtmlBody ? BodyType.Html : BodyType.Text
    };

    // Message
    var message = new Microsoft.Graph.Models.Message
    {
        From = new Recipient() { EmailAddress = new Microsoft.Graph.Models.EmailAddress() { Address = def.sender } },
        Subject = def.subject,
        Body = messageBody,
        ToRecipients = to,
        CcRecipients = cc,
        BccRecipients = bcc,
    };

    // Attachment
    if (File.Exists(def.attachPath))
    {
        // Create the message with attachment.
        byte[] contentBytes = File.ReadAllBytes(def.attachPath);
        message.Attachments = new List<Attachment>();
        message.Attachments.Add(new FileAttachment
        {
            OdataType = ""#microsoft.graph.fileAttachment"",
            ContentBytes = contentBytes,
            Name = def.attachName
        });
    }

    var body = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
    {
        Message = message,
        SaveToSentItems = true,
    };

    // PostAsync method call
    await graphClient.Users[user].SendMail.PostAsync(body);
}
";

        override public string GetProcessingScriptTemplate()
        {
            return ProcessingScriptTemplate;
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

        EmailServerType _type = EmailServerType.SMTP;

        /// <summary>
        /// Type of Email server used
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Server type"), Description("Type of Email server used."), Id(1, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
#endif
        public EmailServerType Type
        {
            get => _type;
            set
            {
                _type = value; UpdateEditorAttributes();
            }
        }
        /// <summary>
        /// Script executed when the output is processed. The script can be customized.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Output processing script"), Description("Script executed when the output is processed. The script can be customized."), Id(2, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ProcessingScript { get; set; } = "";

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
#endif
        [XmlIgnore]
        public string ClearPassword
        {
            get
            {
                try
                {
                    return Repository.Instance.DecryptValue(Password, PasswordKeyName);
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
                    Password = Repository.Instance.EncryptValue(value, PasswordKeyName);
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
        [Category("SMTP Definition"), DisplayName("Delivery method"), Description("Specifies how outgoing email messages will be handled."), Id(5, 1)]
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
        [Category("SMTP Definition"), DisplayName("Use default credentials"), Description("If true, the default credentials are used."), Id(8, 1)]
        [DefaultValue(false)]
#endif
        public bool UseDefaultCredentials { get; set; } = false;

        /// <summary>
        /// Script used to send the Email via SMTP. The script can be customized.
        /// </summary>
#if WINDOWS
        [Category("SMTP Definition"), DisplayName("SMTP script"), Description("Script used to send the Email via SMTP. The script can be customized."), Id(9, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string SmtpScript { get; set; } = "";

        /// <summary>
        /// The API Key for SendGrid
        /// </summary>
        public string SendGridKey { get; set; }

        /// <summary>
        /// The clear API Key used for SendGrid. An API Key can be get from https://sendgrid.com/ after registration (Free plan).
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
                    return Repository.Instance.DecryptValue(SendGridKey, SendGridKeyName, true);
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
                    SendGridKey = Repository.Instance.EncryptValue(value, SendGridKeyName, true);
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
        /// Script used to send the Email via SendGrid. The script can be customized.
        /// </summary>
#if WINDOWS
        [Category("SendGrid Definition"), DisplayName("SendGrid script"), Description("Script used to send the Email via SendGrid. The script can be customized."), Id(3, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string SendGridScript { get; set; } = "";

        /// <summary>
        /// The Azure Secret for MS Graph
        /// </summary>
        public string AzureSecret { get; set; }

        /// <summary>
        /// Secret used for the authentication before sending the Email
        /// </summary>
#if WINDOWS
        [Category("Microsoft Graph Definition"), DisplayName("Azure secret"), Description("Secret used for the authentication before sending the Email."), PasswordPropertyText(true), Id(1, 3)]
#endif
        [XmlIgnore]
        public string ClearAzureSecret
        {
            get
            {
                try
                {
                    return Repository.Instance.DecryptValue(AzureSecret, AzureSecretKeyName, true);
                }
                catch (Exception ex)
                {
                    Error = "Error during Azure Secret decryption:" + ex.Message;
                    TypeDescriptor.Refresh(this);
                    return AzureSecret;
                }
            }
            set
            {
                try
                {
                    AzureSecret = Repository.Instance.EncryptValue(value, AzureSecretKeyName, true);
                }
                catch (Exception ex)
                {
                    Error = "Error during Azure Secret encryption:" + ex.Message;
                    AzureSecret = value;
                    TypeDescriptor.Refresh(this);
                }
            }
        }


        /// <summary>
        /// Script used to send the Email via SendGrid. The script can be customized.
        /// </summary>
#if WINDOWS
        [Category("Microsoft Graph Definition"), DisplayName("MS Graph script"), Description("Script used to send the Email via Microsoft Graph. The script can be customized."), Id(2, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string MSGraphScript { get; set; } = "";

        /// <summary>
        /// The sender email address used to send the email
        /// </summary>
#if WINDOWS
        [Category("Addresses"), DisplayName("Sender Email"), Description("The sender email address used to send the email."), Id(1, 5)]
#endif
        public string SenderEmail { get; set; }

        /// <summary>
        /// The reply addresses used for the email
        /// </summary>
#if WINDOWS
        [Category("Addresses"), DisplayName("Reply addresses"), Description("The reply addresses used for the email."), Id(2, 5)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
#endif
        public string ReplyTo { get; set; }

        /// <summary>
        /// If true, this email device will be chosen first to be used for notifications. (e.g. sending an email in case of error in a schedule)
        /// </summary>
#if WINDOWS
        [Category("Advanced"), DisplayName("Used for notification"), Description("If true, this email device will be chosen first to be used for notifications. (e.g. sending an email in case of error in a schedule)"), Id(1, 6)]
        [DefaultValue(false)]
#endif
        public bool UsedForNotification { get; set; } = false;

        /// <summary>
        /// If true, the Email Sender or Reply address can be changed in the Report Designer
        /// </summary>
#if WINDOWS
        [Category("Advanced"), DisplayName("Allow to change Email Sender or Reply Address"), Description("If true, the Email Sender or Reply address can be changed in the Report Designer."), Id(2, 6)]
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
            if (Type == EmailServerType.SMTP && string.IsNullOrEmpty(Server)) throw new Exception("The SMTP Server cannot be empty for a SMTP Email device.");
            if (Type == EmailServerType.SMTP && string.IsNullOrEmpty(SenderEmail)) throw new Exception("The Email Sender cannot be empty for a SMTP Email device.");
        }

        /// <summary>
        /// Send an Email either through SMTP or SendGrid or Graph API (used for notification emails)
        /// </summary>
        public void SendEmail(string sender, string to, string subject, bool isHtmlBody, string body)
        {
            SendEmail(sender, to, "", "", "", subject, isHtmlBody, body, "", "");
        }


        /// <summary>
        /// Send an Email either through SMTP or SendGrid or Graph API 
        /// </summary>
        public void SendEmail(string sender, string to, string replyTo, string cc, string bcc, string subject, bool isHtmlBody, string body, string attachPath, string attachName)
        {
            if (string.IsNullOrEmpty(sender)) sender = SenderEmail;
            if (string.IsNullOrEmpty(replyTo)) replyTo = sender;

            var emailDefinition = new EmailDefinition()
            {
                device = this,
                sender = sender,
                to = to,
                replyTo = replyTo,
                cc = cc,
                bcc = bcc,
                subject = subject,
                isHtmlBody = isHtmlBody,
                body = body,
                attachPath = attachPath,
                attachName = attachName
            };

            var script = string.IsNullOrWhiteSpace(SmtpScript) ? SmtpScriptTemplate : SmtpScript;
            if (Type == EmailServerType.SendGrid) script = string.IsNullOrWhiteSpace(SendGridScript) ? SendGridScriptTemplate : SendGridScript;
            if (Type == EmailServerType.MSGraph) script = string.IsNullOrWhiteSpace(MSGraphScript) ? MSGraphScriptTemplate : MSGraphScript;

            RazorHelper.CompileExecute(script, emailDefinition);
        }

        /// <summary>
        /// Send the report result by email using the device configuration
        /// </summary>
        public override void Process(Report report)
        {
            var script = string.IsNullOrEmpty(ProcessingScript) ? ProcessingScriptTemplate : ProcessingScript;
            RazorHelper.CompileExecute(script, report);
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

                SendEmail(SenderEmail, TestEmailTo, "Test email", false, "This is a test message.");

                Information = string.Format("A test email has been sent successfully to '{0}'. Please check your inbox...", TestEmailTo);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                if (ex.InnerException != null) Error += " " + ex.InnerException.Message.Trim();
                Information = "Error got when sending the test Email.";
            }
        }


        async Task SandBoxAsync()
        {
            OutputEmailDevice.EmailDefinition def = new EmailDefinition();
            var device = def.device;

            var scopes = new[] { ".default" };
            // TenantId
            string tenant = "";
            // ClientId
            string client = "";
            // Secret
            string secret = "";
            // UserId
            string userId = "";

            // Credential
            ClientSecretCredential credentials = new ClientSecretCredential(tenant, client, secret);

            // Graph client instance
            GraphServiceClient graphClient = new GraphServiceClient(credentials, scopes);

            // Recipients
            var to = new List<Recipient>();
            var cc = new List<Recipient>();
            var bcc = new List<Recipient>();
            foreach (var addr in Helper.GetEmailAddresses(def.to)) to.Add(new Recipient() { EmailAddress = new Microsoft.Graph.Models.EmailAddress() { Address = addr } });
            foreach (var addr in Helper.GetEmailAddresses(def.cc)) cc.Add(new Recipient() { EmailAddress = new Microsoft.Graph.Models.EmailAddress() { Address = addr } });
            foreach (var addr in Helper.GetEmailAddresses(def.bcc)) bcc.Add(new Recipient() { EmailAddress = new Microsoft.Graph.Models.EmailAddress() { Address = addr } });

            // Body
            var messageBody = new ItemBody
            {
                Content = def.body,
                ContentType = def.isHtmlBody ? BodyType.Html : BodyType.Text
            };

            // Message
            var message = new Microsoft.Graph.Models.Message
            {
                From = new Recipient() { EmailAddress = new Microsoft.Graph.Models.EmailAddress() { Address = def.sender } },
                Subject = def.subject,
                Body = messageBody,
                ToRecipients = to,
                CcRecipients = cc,
                BccRecipients = bcc,
            };

            // Attachment
            if (File.Exists(def.attachPath))
            {
                // Create the message with attachment.
                byte[] contentBytes = File.ReadAllBytes(def.attachPath);
                message.Attachments.Add(new FileAttachment
                {
                    OdataType = "#microsoft.graph.fileAttachment",
                    ContentBytes = contentBytes,
                    Name = def.attachName
                });
            }

            var body = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true,
            };

            //awaiting PostAsync method call
            graphClient.Users[userId].SendMail.PostAsync(body);
        }
    }
}
