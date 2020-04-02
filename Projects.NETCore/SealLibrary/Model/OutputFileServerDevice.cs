//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Renci.SshNet;
using Seal.Helpers;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// OutputFileServerDevice is an implementation of device that save the report result to a file server (FTP,SFTP,etc.).
    /// </summary>
    public class OutputFileServerDevice : OutputDevice
    {
        static string PasswordKey = "?d_*er)wien?,édl+25.()à,";

        public const string ProcessingScriptTemplate = @"@using Renci.SshNet
@using System.IO
@using System.Net
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
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(string.Format(""ftp://{0}:{1}{2}"", device.HostName, device.PortNumber, remotePath));
        request.KeepAlive = true;
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(device.UserName, device.ClearPassword);

        //SSL Management: Accept all certificates or add the certificate to the request
        //request.EnableSsl = true;
        //ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        //request.ClientCertificates = new X509CertificateCollection() { X509Certificate.CreateFromCertFile(@""C:\_dev\Tests\FileZillaKeys\c1.crt"") } ;

        byte[] fileContents = File.ReadAllBytes(report.ResultFilePath);
        using (Stream requestStream = request.GetRequestStream())
        {
            requestStream.Write(fileContents, 0, fileContents.Length);
        }
        request.GetResponse();
    }
    else if (device.Protocol == FileServerProtocol.SFTP)
    {
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
        public FileServerProtocol Protocol { get; set; } = FileServerProtocol.FTP;

        /// <summary>
        /// For FTPS, TLS/SSL Implicit or Explicit encryption.
        /// </summary>
/*        [Category("Definition"), DisplayName("FTP Encryption"), Description("For FTPS, TLS/SSL Implicit or Explicit encryption."), Id(1, 1)]
        public FtpSecure FtpSecure { get; set; } = FtpSecure.None;
        */
        /// <summary>
        /// File Server host name
        /// </summary>
        public string HostName { get; set; } = "127.0.0.1";

        /// <summary>
        /// Port number used to connect to the server (e.g. 21 for FTP, 22 for SFTP, 990 for FTPS, etc.)
        /// </summary>
        public int PortNumber { get; set; } = 21;


        /// <summary>
        /// List of directories allowed on the file server. One per line or separated by semi-column.
        /// </summary>
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
        public string UserName { get; set; }

        /// <summary>
        /// The password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The clear password used to connect to the File Server
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
        /// Script executed when the output is processed. The script can be modified to change the client settings (e.g. configuring FTPS).
        /// </summary>
        public string ProcessingScript { get; set; } = "";

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
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(string.Format("ftp://{0}:{1}", HostName, PortNumber));
                    request.Method = WebRequestMethods.Ftp.PrintWorkingDirectory;
                    request.Credentials = new NetworkCredential(UserName, ClearPassword);
                    request.GetResponse();
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
        public string HelperTestConnection
        {
            get { return "<Click to test the connection>"; }
        }

    }
}

