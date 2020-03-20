//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using DynamicTypeDescriptor;
using Seal.Forms;
using Seal.Helpers;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Serialization;
using WinSCP;

namespace Seal.Model
{
    /// <summary>
    /// OutputFileServerDevice is an implementation of device that save the report result to a file server (FTP,SFTP,etc.).
    /// Based on the WinSCP library
    /// </summary>
    public class OutputWinSCPDevice : OutputDevice
    {
        static string PasswordKey = "?d_*er)wien?,édl+25.()à,";


        public const string SessionScriptTemplate = @"@using WinSCP
@{
    //Create and open a WinSCP session 
    //Full WinSCP documentation at https://winscp.net/eng/docs/library#classes
    OutputWinSCPDevice device = Model;
    
    //https://winscp.net/eng/docs/library_sessionoptions
    SessionOptions sessionOptions = new SessionOptions
    {
        Protocol = device.Protocol,
        HostName = device.HostName,
        PortNumber = device.PortNumber,
        UserName = device.UserName,
        Password = device.ClearPassword,
        //GiveUpSecurityAndAcceptAnyTlsHostCertificate = true,  //FTPS
        //FtpSecure = FtpSecure.Implicit,                       //FTPS
        //TlsHostCertificateFingerprint = ""9d:34:41:e..."",      //FTPS
        //WebdavSecure = true,                                  //Webdav
        //SshHostKeyFingerprint = ""ssh-rsa 2048 ..."",           //SFTP, SCP
    };

    //sessionOptions.AddRawSettings(""FSProtocol"", ""2"");        //SFTP

    //Create and open the session
    device.Session = new Session();
    device.Session.Open(sessionOptions);    
}
";


        public const string ProcessingScriptTemplate = @"@using WinSCP
@using System.IO
@{
    //Upload the file to the server
    //Full WinSCP documentation at https://winscp.net/eng/docs/library#classes
    Report report = Model;
    ReportOutput output = report.OutputToExecute;
    OutputWinSCPDevice device = (OutputWinSCPDevice) output.Device;
    
    //https://winscp.net/eng/docs/library_session
    device.Session = device.GetOpenSession();    
    //Options
    TransferOptions transferOptions = new TransferOptions()
    {
        TransferMode = TransferMode.Automatic,
        OverwriteMode = OverwriteMode.Overwrite
    };
    device.Session.PutFileToDirectory(report.ResultFilePath, device.Directory, false, transferOptions);

    var path = device.DirectoryWithSeparators + report.ResultFileName;
    //Rename the file on the server if necessary
    if (Path.GetFileName(report.ResultFilePath) != report.ResultFileName)
    {
        if (device.Session.FileExists(path)) 
        {
            device.Session.RemoveFile(path);
        }
        device.Session.MoveFile(device.DirectoryWithSeparators + Path.GetFileName(report.ResultFilePath), path);
    }

    output.Information = report.Translate(""Report result generated in '{0}'"", path);
    report.LogMessage(""Report result generated in '{0}'"", path);
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
                GetProperty("Directory").SetIsBrowsable(true);
                GetProperty("UserName").SetIsBrowsable(true);
                GetProperty("ClearPassword").SetIsBrowsable(true);
                GetProperty("SessionScript").SetIsBrowsable(true);
                GetProperty("ProcessingScript").SetIsBrowsable(true);

                GetProperty("HelperTestConnection").SetIsBrowsable(true);
                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);

                //                GetProperty("CustomScript").SetIsReadOnly(!UseCustomScript);

                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

        /// <summary>
        /// Default device identifier
        /// </summary>
        public static string DefaultGUID = "c428a6ba-061b-4a47-b9bc-f3f02442ab4b";

        /// <summary>
        /// Last modification date time
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification;

        /// <summary>
        /// Create a basic OutputFolderDevice
        /// </summary>
        static public OutputWinSCPDevice Create()
        {

            var result = new OutputWinSCPDevice() { GUID = Guid.NewGuid().ToString() };
            result.Name = "File Server Device";
            return result;
        }

        /// <summary>
        /// Full name
        /// </summary>
        [XmlIgnore]
        public override string FullName
        {
            get { return string.Format("{0} (WinSCP)", Name); }
        }

        Protocol _protocol = Protocol.Ftp;
        /// <summary>
        /// Protocol to connect to the server
        /// </summary>
        [Category("Definition"), DisplayName("Protocol"), Description("Protocol to connect to the server."), Id(1, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(Protocol.Ftp)]
        public Protocol Protocol
        {
            get
            {
                return _protocol;
            }
            set
            {
                _protocol = value;
                if (_protocol == Protocol.Ftp) PortNumber = 21;
                if (_protocol == Protocol.Sftp) PortNumber = 22;
                if (_protocol == Protocol.Scp) PortNumber = 22;
                if (_protocol == Protocol.S3) PortNumber = 443;
                if (_protocol == Protocol.Webdav) PortNumber = 80;
                UpdateEditor();
            }
        }

        /// <summary>
        /// For FTPS, TLS/SSL Implicit or Explicit encryption.
        /// </summary>
        [Category("Definition"), DisplayName("FTP Encryption"), Description("For FTPS, TLS/SSL Implicit or Explicit encryption."), Id(1, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(WinSCP.FtpSecure.None)]
        public WinSCP.FtpSecure FtpSecure { get; set; } = WinSCP.FtpSecure.None;

        /// <summary>
        /// File Server host name
        /// </summary>
        [Category("Definition"), DisplayName("Host name"), Description("Host name of the server."), Id(2, 1)]
        public string HostName { get; set; } = "127.0.0.1";

        /// <summary>
        /// Port number used to connect to the server (e.g. 21 for FTP, 22 for SFTP, 990 for FTPS, etc.)
        /// </summary>
        [Category("Definition"), DisplayName("Port number"), Description("Port number used to connect to the server (e.g. 21 for FTP, 22 for SFTP, 990 for FTPS, etc.)"), Id(3, 1)]
        [DefaultValue(21)]
        public int PortNumber { get; set; } = 21;

        /// <summary>
        /// The remote directory on the File Server
        /// </summary>
        [Category("Definition"), DisplayName("Directory"), Description("The remote directory on the server"), Id(4, 1)]
        public string Directory { get; set; } = "/";

        /// <summary>
        /// Retruns the directory with the / separator
        /// </summary>
        public string DirectoryWithSeparators
        {
            get
            {
                if (Directory == "/") return Directory;
                return (Directory.StartsWith("/") ? "" : "/") + Directory + (Directory.EndsWith("/") ? "" : "/");
            }
        }

        /// <summary>
        /// The user name used to connect to the File Server
        /// </summary>
        [Category("Definition"), DisplayName("User name"), Description("The user name used to connect to the derver"), Id(5, 1)]
        public string UserName { get; set; }

        /// <summary>
        /// The password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The clear password used to connect to the File Server
        /// </summary>
        [Category("Definition"), DisplayName("Password"), Description("The password used to connect to the derver"), PasswordPropertyText(true), Id(6, 1)]
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

        /*
        bool _useCustomScript = false;
        /// <summary>
        /// If true, the custom script can be modified.
        /// </summary>
        [Category("Definition"), DisplayName("Use custom script"), Description("If true, the custom script can be modified."), Id(9, 1)]
        [DefaultValue(false)]
        public bool UseCustomScript
        {
            get { return _useCustomScript; }
            set
            {
                _useCustomScript = value;
                UpdateEditorAttributes();
            }
        }

        /// <summary>
        /// Script executed when the output is processed. The default script depends on the server type.
        /// </summary>
        [Category("Definition"), DisplayName("Custom script"), Description("Script executed when the output is processed. The default script depends on the server type."), Id(10, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string CustomScript { get; set; } = "";
        */

        /// <summary>
        /// Script executed to get the open session when the output is processed
        /// </summary>
        [Category("Definition"), DisplayName("Session script"), Description("Script executed to get the open session when the output is processed."), Id(10, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string SessionScript { get; set; } = "";

        /// <summary>
        /// Script executed when the output is processed. The default script depends on the server type.
        /// </summary>
        [Category("Definition"), DisplayName("Output processing script"), Description("Script executed when the output is processed."), Id(10, 1)]
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
        /// The open session got after execution of GetOpenSession()
        /// </summary>
        [XmlIgnore]
        public Session Session;

        public Session GetOpenSession()
        {
            var script = string.IsNullOrEmpty(SessionScript) ? SessionScriptTemplate : SessionScript;
            RazorHelper.CompileExecute(script, this);
            return Session;
        }

        /// <summary>
        /// Check that the report result has been saved and set information
        /// </summary>
        public override void Process(Report report)
        {
            var output = report.OutputToExecute;
            var script = string.IsNullOrEmpty(ProcessingScript) ? ProcessingScriptTemplate : ProcessingScript;
            //RazorHelper.CompileExecute(script, report);

            if (output.ZipResult)
            {

            }
            string zipPath = Path.Combine(Path.GetDirectoryName(report.ResultFilePath), Path.GetFileNameWithoutExtension(report.ResultFilePath) + ".zip");
       //     FileHelper.CreateSample(zipPath, "popol", @"C:\_dev\Seal-Report\Repository\Devices\WinSCP");

            FileHelper.CreateZIP(report.ResultFilePath, report.ResultFileName, zipPath, "popol2");
            var resultFileName = Path.GetFileNameWithoutExtension(report.ResultFileName) + ".zip";
            report.ResultFilePath = zipPath;

            Session = GetOpenSession();
            TransferOptions transferOptions = new TransferOptions()
            {
                TransferMode = TransferMode.Automatic,
                OverwriteMode = OverwriteMode.Overwrite
            };
            Session.PutFileToDirectory(report.ResultFilePath, Directory, false, transferOptions);

            var path = DirectoryWithSeparators + resultFileName;
            //Rename the file on the server if necessary
            if (Path.GetFileName(report.ResultFilePath) != resultFileName)
            {
                if (Session.FileExists(path))
                {
                    Session.RemoveFile(path);
                }
                Session.MoveFile(DirectoryWithSeparators + Path.GetFileName(report.ResultFilePath), path);
            }
        }

        /// <summary>
        /// Load an OutputFileServerDevice from a file
        /// </summary>
        static public OutputDevice LoadFromFile(string path, bool ignoreException)
        {
            OutputWinSCPDevice result = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(OutputWinSCPDevice));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (OutputWinSCPDevice)serializer.Deserialize(xr);
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
            XmlSerializer serializer = new XmlSerializer(typeof(OutputWinSCPDevice));
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

                GetOpenSession();

                /*

                                if (Type == FileServerType.FTP)
                                {
                                    // Setup session options
                                    SessionOptions sessionOptions = new SessionOptions
                                    {
                                        Protocol = Protocol.Ftp,
                                        HostName = Server,
                                        PortNumber = Port,
                                        UserName = UserName,
                                        Password = ClearPassword,
                                        GiveUpSecurityAndAcceptAnyTlsHostCertificate = true, //FTPS
                                        FtpSecure = FtpSecure.Implicit //FTPS
                                    };

                                    using (WinSCP.Session session = new WinSCP.Session())
                                    {
                                        // Connect
                                        session.Open(sessionOptions);

                                        // Upload files
                                        TransferOptions transferOptions = new TransferOptions();
                                        transferOptions.TransferMode = TransferMode.Binary;
                                        TransferOperationResult transferResult;
                                        transferResult = session.PutFiles(@"c:\temp\ftp.png", DirectoryWithSeparators, false, transferOptions);
                                        transferResult.Check();
                                    }

                                    /*
                                    var serverPath = string.Format("{0}:{1}{2}", Server, Port, DirectoryWithSeparators);
                                    var request = (FtpWebRequest)WebRequest.Create(serverPath);

                                    request.EnableSsl = true;
                                    ServicePointManager.ServerCertificateValidationCallback = ServicePointManager_ServerCertificateValidationCallback;

                                    request.Method = WebRequestMethods.Ftp.ListDirectory;
                                    request.Credentials = new NetworkCredential(UserName, ClearPassword);
                                    request.GetResponse();*
                                }
                                else if (Type == FileServerType.SFTP)
                                {
                                    // Setup session options
                                    SessionOptions sessionOptions = new SessionOptions
                                    {
                                        Protocol = Protocol.Sftp,
                                        HostName = Server,
                                        PortNumber = Port,
                                        UserName = UserName,
                                        Password = ClearPassword,
                                        SshHostKeyFingerprint = "ssh-rsa 2048 xxxxxxxxxxx...="
                                        //                GiveUpSecurityAndAcceptAnyTlsHostCertificate = true, //FTPS
                                                                                                      //              FtpSecure = FtpSecure.Implicit //FTPS
                                    };

                                    using (WinSCP.Session session = new WinSCP.Session())
                                    {
                                        // Connect
                                        session.Open(sessionOptions);

                                        // Upload files
                                        TransferOptions transferOptions = new TransferOptions();
                                        transferOptions.TransferMode = TransferMode.Binary;
                                        TransferOperationResult transferResult;
                                        transferResult = session.PutFiles(@"c:\temp\ftp.png", DirectoryWithSeparators, false, transferOptions);
                                        transferResult.Check();
                                    }
                /*

                                    using (var client = new SftpClient(Server, Port, UserName, ClearPassword))
                                    {
                                        client.Connect();
                                        client.ChangeDirectory(Directory);
                                        client.Disconnect();
                                    }*
                                }
                                else if (Type == FileServerType.SCP)
                                {
                                    using (ScpClient client = new ScpClient(Server, Port, UserName, ClearPassword))
                                    {
                                        client.Connect();
                                        client.Disconnect();
                                    }
                                }*/
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
