//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using DynamicTypeDescriptor;
using FluentFTP;
using Renci.SshNet;
using Seal.Forms;
using Seal.Helpers;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// OutputFileServerDevice is an implementation of device that save the report result to a file server (FTP, SFTP, etc.).
    /// </summary>
    public class OutputFileServerDevice : OutputDevice
    {
        static string PasswordKey = "?d_*er)wien?,édl+25.()à,";

        /// <summary>
        /// Default processing script template
        /// </summary>
        public const string ProcessingScriptTemplate = @"@using Renci.SshNet
@using FluentFTP
@using System.IO
@using System.Security.Authentication
@{
    //Upload the file to the server
    Report report = Model;
    ReportOutput output = report.OutputToExecute;
    OutputFileServerDevice device = (OutputFileServerDevice)output.Device;

    var resultFileName = (output.ZipResult ? Path.GetFileNameWithoutExtension(report.ResultFileName) + "".zip"" : report.ResultFileName);
    device.HandleZipOptions(report);

    //Put file
    var remotePath = output.FileServerFolderWithSeparators + resultFileName;

    if (device.Protocol == FileServerProtocol.FTP)
    {
        //Refer to https://github.com/robinrodricks/FluentFTP
        using (var client = new FtpClient(device.HostName, device.UserName, device.ClearPassword)) {
            if (device.PortNumber == 0) {
                client.AutoConnect();
            }
            else {
                client.Port = device.PortNumber;
                //SSL Configuration can be defined here
                /*
                client.EncryptionMode = FtpEncryptionMode.Explicit;
                client.SslProtocols = SslProtocols.Tls12;
                client.ValidateCertificate += new FtpSslValidation(delegate(FtpClient control, FtpSslValidationEventArgs e) {
                    // add logic to test if certificate is valid here
                    e.Accept = true;
                });
                */
                client.Connect();
            }
            client.UploadFile(report.ResultFilePath, remotePath);
            client.Disconnect();
        }
    }
    else if (device.Protocol == FileServerProtocol.SFTP)
    {
        //Refer to https://github.com/sshnet/SSH.NET
        using (var sftp = new SftpClient(device.HostName, device.PortNumber, device.UserName, device.ClearPassword))
        {
            sftp.Connect();
            using (Stream fileStream = File.Create(report.ResultFilePath))
            {
                sftp.UploadFile(fileStream, remotePath);
            }
            sftp.Disconnect();
        }

    }
    else if (device.Protocol == FileServerProtocol.SCP)
    {
        //Refer to https://github.com/sshnet/SSH.NET
        using (var scp = new ScpClient(device.HostName, device.PortNumber, device.UserName, device.ClearPassword))
        {
            scp.Connect();
            using (Stream fileStream = File.Create(report.ResultFilePath))
            {
                scp.Upload(fileStream, remotePath);
            }
            scp.Disconnect();
        }
    }

    output.Information = report.Translate(""Report result generated in '{0}'"", remotePath);
    report.LogMessage(""Report result generated in '{0}'"", remotePath);
}
";

        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Protocol").SetIsBrowsable(true);
                GetProperty("HostName").SetIsBrowsable(true);
                GetProperty("PortNumber").SetIsBrowsable(true);
                GetProperty("Directories").SetIsBrowsable(true);
                GetProperty("UserName").SetIsBrowsable(true);
                GetProperty("ClearPassword").SetIsBrowsable(true);
                GetProperty("ProcessingScript").SetIsBrowsable(true);

                GetProperty("HelperTestConnection").SetIsBrowsable(true);
                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

        /// <summary>
        /// Last modification date time
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification;

        /// <summary>
        /// Create a basic OutputFolderDevice
        /// </summary>
        static public OutputFileServerDevice Create()
        {

            var result = new OutputFileServerDevice() { GUID = Guid.NewGuid().ToString() };
            result.Name = "File Server Device";
            return result;
        }

        /// <summary>
        /// Full name
        /// </summary>
        [XmlIgnore]
        public override string FullName
        {
            get { return string.Format("{0} (File Server)", Name); }
        }

        /// <summary>
        /// Protocol to connect to the server
        /// </summary>
        [Category("Definition"), DisplayName("Protocol"), Description("Protocol to connect to the server."), Id(1, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(FileServerProtocol.FTP)]
        public FileServerProtocol Protocol { get; set; } = FileServerProtocol.FTP;

        /// <summary>
        /// File Server host name
        /// </summary>
        [Category("Definition"), DisplayName("Host name"), Description("Host name of the server."), Id(2, 1)]
        public string HostName { get; set; } = "127.0.0.1";

        /// <summary>
        /// Port number used to connect to the server (e.g. 21 for FTP, 22 for SFTP, 990 for FTPS, etc.)
        /// </summary>
        [Category("Definition"), DisplayName("Port number"), Description("For FTP Protocol, port number used to connect to the server (e.g. 21 for FTP or implicit FTPS, 990 for FTPS, etc.). A value of 0 means an automatic detection and connection."), Id(3, 1)]
        [DefaultValue(0)]
        public int PortNumber { get; set; } = 0;


        /// <summary>
        /// List of directories allowed on the file server. One per line or separated by semi-column.
        /// </summary>
        [Category("Definition"), DisplayName("Directories"), Description("List of directories allowed on the file server. One directory per line."), Id(6, 1)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string Directories { get; set; } = "/";

        /// <summary>
        /// Array of allowed directories.
        /// </summary>
        public string[] DirectoriesArray
        {
            get
            {
                return Directories.Trim().Replace("\r\n", "\r").Split('\r');
            }
        }

        /// <summary>
        /// The user name used to connect to the File Server
        /// </summary>
        [Category("Definition"), DisplayName("User name"), Description("The user name used to connect to the derver"), Id(7, 1)]
        public string UserName { get; set; }

        /// <summary>
        /// The password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The clear password used to connect to the File Server
        /// </summary>
        [Category("Definition"), DisplayName("Password"), Description("The password used to connect to the derver"), PasswordPropertyText(true), Id(8, 1)]
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
        /// Script executed when the output is processed. The script can be modified to change the client settings (e.g. configuring FTPS).
        /// </summary>
        [Category("Definition"), DisplayName("Output processing script"), Description("Script executed when the output is processed. The script can be modified to change the client settings (e.g. configuring FTPS)."), Id(10, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string ProcessingScript { get; set; } = "";

        /// <summary>
        /// Last information message
        /// </summary>
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(4, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(5, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error { get; set; }

        /// <summary>
        /// Check that the report result has been saved and set information
        /// </summary>
        public override void Process(Report report)
        {
            var script = string.IsNullOrEmpty(ProcessingScript) ? ProcessingScriptTemplate : ProcessingScript;
            RazorHelper.CompileExecute(script, report);
        }

        /// <summary>
        /// Load an OutputFileServerDevice from a file
        /// </summary>
        static public OutputDevice LoadFromFile(string path, bool ignoreException)
        {
            OutputFileServerDevice result = null;
            try
            {
                path = FileHelper.ConvertOSFilePath(path);
                if (!File.Exists(path)) throw new Exception("File not found: " + path);

                XmlSerializer serializer = new XmlSerializer(typeof(OutputFileServerDevice));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (OutputFileServerDevice)serializer.Deserialize(xr);
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
            XmlSerializer serializer = new XmlSerializer(typeof(OutputFileServerDevice));
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
            if (string.IsNullOrEmpty(HostName)) throw new Exception("The File Server cannot be empty.");
        }


        /// <summary>
        /// Helper to test the connection
        /// </summary>
        public void TestConnection()
        {
            try
            {
                Error = "";
                Information = "";

                if (Protocol == FileServerProtocol.FTP)
                {
                    FtpClient client = new FtpClient(HostName, UserName, ClearPassword);

                    if (PortNumber == 0)
                    {
                        client.AutoConnect();
                    }
                    else
                    {
                        client.Port = PortNumber;
                        client.Connect();
                    }
                    client.Disconnect();
                }
                else if (Protocol == FileServerProtocol.SFTP)
                {
                    using (var sftp = new SftpClient(HostName, UserName, ClearPassword))
                    {
                        sftp.Connect();
                        sftp.Disconnect();

                    }
                }
                else if (Protocol == FileServerProtocol.SCP)
                {
                    using (var scp = new ScpClient(HostName, UserName, ClearPassword))
                    {
                        scp.Connect();
                        scp.Disconnect();
                    }
                }
                Information = string.Format("The connection to '{0}:{1}' is successfull", HostName, PortNumber);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                if (ex.InnerException != null) Error += " " + ex.InnerException.Message.Trim();
                Information = "Error got testing the connection.";
            }
        }
        /// <summary>
        /// Editor Helper: Test the connection with the current configuration
        /// </summary>
        [Category("Helpers"), DisplayName("Test server connection"), Description("Test the connection with the current configuration."), Id(2, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperTestConnection
        {
            get { return "<Click to test the connection>"; }
        }  
    }
}
