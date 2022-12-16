using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;

namespace Tests
{
    internal class CompileFluentFTP
    {
        void Main()
        {
            FtpClient client = null;
            client = new FtpClient("Host Name", "User Name", "Password"); 
            client = new FtpClient("127.0.0.1", "tester", "password"); 
            client = new FtpClient("waws-prod-dm1-129.ftp.azurewebsites.windows.net", @"sealcore\$sealcore", "password"); //  (e.g. for Azure)

            client.Host = "localhost";
            client.Credentials.UserName = "";
            client.Credentials.Password = "";

            if (client != null)
            {
                //Default FTP configuration for Azure
                client.Config.SslProtocols = SslProtocols.Tls;
                client.Config.ValidateAnyCertificate = true;
                client.Config.DataConnectionType = FtpDataConnectionType.PASV;
                client.Config.DownloadDataType = FtpDataType.Binary;
                client.Config.RetryAttempts = 5;
                client.Config.SocketPollInterval = 1000;
                client.Config.ConnectTimeout = 2000;
                client.Config.ReadTimeout = 2000;
                client.Config.DataConnectionConnectTimeout = 2000;
                client.Config.DataConnectionReadTimeout = 2000;
                client.Port = 21;

                client.Connect();
            }
        }
    }
}
