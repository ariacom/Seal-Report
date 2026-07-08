//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
#if WINDOWS
using DynamicTypeDescriptor;
using System.ComponentModel.Design;
using System.Drawing.Design;
using Seal.Forms;
#endif


namespace Seal.Model
{
    /// <summary>
    /// OutputSharePointDevice is an implementation of device that saves the report result to a SharePoint Online document library using the Microsoft Graph API.
    /// </summary>
    public class OutputSharePointDevice : OutputDevice
    {
        public const string SecretKeyName = "Output SharePoint Device Secret";
        public const string SecretKeyValue = "?d8s(Pz!k3RwB_0eL)m5qU+dV,7c";

        /// <summary>
        /// Default processing script template
        /// </summary>
        public const string ProcessingScriptTemplate = @"@using System.IO
@using System.Net.Http
@using System.Net.Http.Headers
@using System.Text
@using System.Text.Json
@{
    //Upload the report result to a SharePoint Online document library using the Microsoft Graph API.
    //The authentication (client secret or certificate) is handled by the device with device.GetAccessToken().
    //The script can be modified for specific needs (e.g. setting metadata columns on the uploaded file).
    Report report = Model;
    ReportOutput output = report.OutputToExecute;
    var device = output.Device as OutputSharePointDevice;

    var resultFileName = (output.ZipResult ? Path.GetFileNameWithoutExtension(report.ResultFileName) + "".zip"" : Path.GetFileNameWithoutExtension(report.ResultFileName) + Path.GetExtension(report.ResultFilePath));
    device.HandleZipOptions(report);

    //Destination path in the document library
    var remotePath = output.FileServerFolderWithSeparators + resultFileName;

    using (var client = new HttpClient())
    {
        //Authentication
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(""Bearer"", device.GetAccessToken());

        //Resolve the drive of the document library
        var driveId = device.GetDriveId(client);

        //Create an upload session (works for any file size, conflict behavior taken from the device)
        var sessionUrl = string.Format(""{0}/v1.0/drives/{1}/root:{2}:/createUploadSession"", device.GraphUrlNormalized, driveId, device.EscapePath(remotePath));
        var sessionBody = ""{\""item\"":{\""@microsoft.graph.conflictBehavior\"":\"""" + device.ConflictBehavior.ToString().ToLower() + ""\""}}"";
        var sessionResponse = client.PostAsync(sessionUrl, new StringContent(sessionBody, Encoding.UTF8, ""application/json"")).Result;
        var sessionJson = sessionResponse.Content.ReadAsStringAsync().Result;
        if (!sessionResponse.IsSuccessStatusCode) throw new Exception(""SharePoint upload session failed: "" + sessionJson);
        var uploadUrl = JsonDocument.Parse(sessionJson).RootElement.GetProperty(""uploadUrl"").GetString();

        //Upload the file in chunks (the chunk size must be a multiple of 320 KB)
        var fileBytes = File.ReadAllBytes(report.ResultFilePath);
        const int chunkSize = 16 * 320 * 1024;
        for (int pos = 0; pos < fileBytes.Length; pos += chunkSize)
        {
            int size = Math.Min(chunkSize, fileBytes.Length - pos);
            var chunk = new ByteArrayContent(fileBytes, pos, size);
            chunk.Headers.ContentRange = new ContentRangeHeaderValue(pos, pos + size - 1, fileBytes.Length);
            var chunkResponse = client.PutAsync(uploadUrl, chunk).Result;
            if (!chunkResponse.IsSuccessStatusCode) throw new Exception(""SharePoint upload failed: "" + chunkResponse.Content.ReadAsStringAsync().Result);
        }
    }

    output.Information = report.Translate(""Report result generated in '{0}'"", remotePath);
    report.LogMessage(""Report result generated in '{0}'"", remotePath);
}
";

        override public string GetProcessingScriptTemplate()
        {
            return ProcessingScriptTemplate;
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
                GetProperty("TenantId").SetIsBrowsable(true);
                GetProperty("ClientId").SetIsBrowsable(true);
                GetProperty("AuthenticationType").SetIsBrowsable(true);
                GetProperty("ClearClientSecret").SetIsBrowsable(true);
                GetProperty("CertificatePath").SetIsBrowsable(true);
                GetProperty("CertificateThumbprint").SetIsBrowsable(true);
                GetProperty("ClearCertificatePassword").SetIsBrowsable(true);
                GetProperty("SiteUrl").SetIsBrowsable(true);
                GetProperty("LibraryName").SetIsBrowsable(true);
                GetProperty("Directories").SetIsBrowsable(true);
                GetProperty("ConflictBehavior").SetIsBrowsable(true);
                GetProperty("ProcessingScript").SetIsBrowsable(true);

                GetProperty("LoginUrl").SetIsBrowsable(true);
                GetProperty("GraphUrl").SetIsBrowsable(true);

                GetProperty("ClearClientSecret").SetIsReadOnly(AuthenticationType != SharePointAuthenticationType.ClientSecret);
                GetProperty("CertificatePath").SetIsReadOnly(AuthenticationType != SharePointAuthenticationType.Certificate);
                GetProperty("CertificateThumbprint").SetIsReadOnly(AuthenticationType != SharePointAuthenticationType.Certificate);
                GetProperty("ClearCertificatePassword").SetIsReadOnly(AuthenticationType != SharePointAuthenticationType.Certificate);

                GetProperty("HelperTestConnection").SetIsBrowsable(true);
                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

#endif
        /// <summary>
        /// Last modification date time
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification;

        /// <summary>
        /// Create a basic OutputSharePointDevice
        /// </summary>
        static public OutputSharePointDevice Create()
        {
            var result = new OutputSharePointDevice() { GUID = Guid.NewGuid().ToString() };
            result.Name = "SharePoint Device";
            return result;
        }

        /// <summary>
        /// Full name
        /// </summary>
        [XmlIgnore]
        public override string FullName
        {
            get { return string.Format("{0} (SharePoint)", Name); }
        }

        /// <summary>
        /// The Microsoft Entra ID (Azure AD) tenant identifier (GUID or domain name)
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Tenant Id"), Description("The Microsoft Entra ID (Azure AD) tenant identifier: either the tenant GUID or a verified domain name (e.g. 'contoso.onmicrosoft.com')."), Id(1, 1)]
#endif
        public string TenantId { get; set; }

        /// <summary>
        /// The application (client) identifier of the Entra ID application registration
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Client Id"), Description("The application (client) identifier of the Entra ID application registration. The application must be granted the 'Sites.ReadWrite.All' or 'Sites.Selected' Microsoft Graph application permission with admin consent."), Id(2, 1)]
#endif
        public string ClientId { get; set; }

        /// <summary>
        /// Authentication used to get the Microsoft Graph access token
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Authentication"), Description("Authentication used to get the Microsoft Graph access token: either a client secret or a certificate (from a file or the Windows certificate store)."), Id(3, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(SharePointAuthenticationType.ClientSecret)]
#endif
        public SharePointAuthenticationType AuthenticationType { get; set; } = SharePointAuthenticationType.ClientSecret;

        /// <summary>
        /// The client secret (encrypted)
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// The clear client secret of the application registration
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Client secret"), Description("The client secret of the application registration (for the 'Client Secret' authentication)."), PasswordPropertyText(true), Id(4, 1)]
#endif
        [XmlIgnore]
        public string ClearClientSecret
        {
            get
            {
                try
                {
                    return Repository.Instance.DecryptValue(ClientSecret, SecretKeyName);
                }
                catch (Exception ex)
                {
                    Error = "Error during client secret decryption:" + ex.Message;
                    return ClientSecret;
                }
            }
            set
            {
                try
                {
                    ClientSecret = Repository.Instance.EncryptValue(value, SecretKeyName);
                }
                catch (Exception ex)
                {
                    Error = "Error during client secret encryption:" + ex.Message;
                    ClientSecret = value;
                }
            }
        }

        /// <summary>
        /// Path of the certificate file used for the Certificate authentication
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Certificate file path"), Description("For the 'Certificate' authentication, path of the certificate file (.pfx or .p12) containing the private key. If empty, the certificate is searched in the Windows certificate store using the thumbprint."), Id(5, 1)]
#endif
        public string CertificatePath { get; set; }

        /// <summary>
        /// Thumbprint of the certificate in the Windows certificate store
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Certificate thumbprint"), Description("For the 'Certificate' authentication, thumbprint of the certificate located in the Windows certificate store (Personal store of the current user or the local machine). Used when the certificate file path is empty."), Id(6, 1)]
#endif
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// The certificate file password (encrypted)
        /// </summary>
        public string CertificatePassword { get; set; }

        /// <summary>
        /// The clear password of the certificate file
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Certificate password"), Description("The password of the certificate file (for the 'Certificate' authentication with a certificate file)."), PasswordPropertyText(true), Id(7, 1)]
#endif
        [XmlIgnore]
        public string ClearCertificatePassword
        {
            get
            {
                try
                {
                    return Repository.Instance.DecryptValue(CertificatePassword, SecretKeyName);
                }
                catch (Exception ex)
                {
                    Error = "Error during certificate password decryption:" + ex.Message;
                    return CertificatePassword;
                }
            }
            set
            {
                try
                {
                    CertificatePassword = Repository.Instance.EncryptValue(value, SecretKeyName);
                }
                catch (Exception ex)
                {
                    Error = "Error during certificate password encryption:" + ex.Message;
                    CertificatePassword = value;
                }
            }
        }

        /// <summary>
        /// URL of the SharePoint site
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Site URL"), Description("URL of the SharePoint site (e.g. 'https://contoso.sharepoint.com/sites/Reports', or 'https://contoso.sharepoint.com' for the root site)."), Id(8, 1)]
#endif
        public string SiteUrl { get; set; } = "https://contoso.sharepoint.com/sites/Reports";

        /// <summary>
        /// Name of the document library of the site
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Document library"), Description("Name of the document library of the site (e.g. 'Documents' for the default library)."), Id(9, 1)]
#endif
        public string LibraryName { get; set; } = "Documents";

        /// <summary>
        /// List of folders allowed in the document library. One per line or separated by semi-column.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Directories"), Description("List of folders allowed in the document library. One folder per line (e.g. '/' for the library root, '/Monthly Reports')."), Id(10, 1)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
#endif
        public string Directories { get; set; } = "/";

        /// <summary>
        /// Array of allowed directories.
        /// </summary>
        public string[] DirectoriesArray
        {
            get
            {
                return Directories.Trim().Replace("\r\n", "\r").Replace("\n", "\r").Split('\r');
            }
        }

        /// <summary>
        /// Behavior when a file with the same name already exists in the destination folder
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("If the file exists"), Description("Behavior when a file with the same name already exists in the destination folder: replace it (a new version is created if the library has versioning enabled), rename the new file, or fail the upload."), Id(11, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(SharePointConflictBehavior.Replace)]
#endif
        public SharePointConflictBehavior ConflictBehavior { get; set; } = SharePointConflictBehavior.Replace;

        /// <summary>
        /// Script executed when the output is processed
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Output processing script"), Description("Script executed when the output is processed. The script can be modified for specific needs (e.g. setting metadata columns on the uploaded file)."), Id(12, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ProcessingScript { get; set; } = "";

        /// <summary>
        /// URL of the Microsoft login endpoint
        /// </summary>
#if WINDOWS
        [Category("Advanced"), DisplayName("Login URL"), Description("URL of the Microsoft login endpoint. Change it only for a national cloud (e.g. 'https://login.microsoftonline.us' for US Government)."), Id(1, 2)]
        [DefaultValue("https://login.microsoftonline.com")]
#endif
        public string LoginUrl { get; set; } = "https://login.microsoftonline.com";

        /// <summary>
        /// URL of the Microsoft Graph endpoint
        /// </summary>
#if WINDOWS
        [Category("Advanced"), DisplayName("Microsoft Graph URL"), Description("URL of the Microsoft Graph endpoint. Change it only for a national cloud (e.g. 'https://graph.microsoft.us' for US Government)."), Id(2, 2)]
        [DefaultValue("https://graph.microsoft.com")]
#endif
        public string GraphUrl { get; set; } = "https://graph.microsoft.com";

        /// <summary>
        /// Microsoft Graph URL without trailing separator
        /// </summary>
        [XmlIgnore]
        public string GraphUrlNormalized
        {
            get { return GraphUrl.TrimEnd('/'); }
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
        /// Returns a Microsoft Graph access token using the device authentication configuration (client secret or certificate)
        /// </summary>
        public string GetAccessToken()
        {
            var tokenEndpoint = string.Format("{0}/{1}/oauth2/v2.0/token", LoginUrl.TrimEnd('/'), TenantId);
            var parameters = new Dictionary<string, string>() {
                { "client_id", ClientId },
                { "scope", GraphUrlNormalized + "/.default" },
                { "grant_type", "client_credentials" }
            };
            if (AuthenticationType == SharePointAuthenticationType.ClientSecret)
            {
                parameters.Add("client_secret", ClearClientSecret);
            }
            else
            {
                parameters.Add("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
                parameters.Add("client_assertion", GetClientAssertion(tokenEndpoint));
            }

            using (var client = new HttpClient())
            {
                var response = client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(parameters)).Result;
                var json = response.Content.ReadAsStringAsync().Result;
                if (!response.IsSuccessStatusCode) throw new Exception("SharePoint authentication failed: " + json);
                return JsonDocument.Parse(json).RootElement.GetProperty("access_token").GetString();
            }
        }

        static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// Builds the JWT client assertion signed with the certificate for the Certificate authentication
        /// </summary>
        string GetClientAssertion(string audience)
        {
            var certificate = LoadCertificate();
            var now = DateTimeOffset.UtcNow;
            var header = JsonSerializer.Serialize(new Dictionary<string, object>() {
                { "alg", "RS256" },
                { "typ", "JWT" },
                { "x5t", Base64UrlEncode(certificate.GetCertHash()) }
            });
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>() {
                { "aud", audience },
                { "iss", ClientId },
                { "sub", ClientId },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", now.ToUnixTimeSeconds() },
                { "exp", now.AddMinutes(10).ToUnixTimeSeconds() }
            });
            var data = Base64UrlEncode(Encoding.UTF8.GetBytes(header)) + "." + Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
            using (var rsa = certificate.GetRSAPrivateKey())
            {
                if (rsa == null) throw new Exception("The certificate does not contain an RSA private key.");
                return data + "." + Base64UrlEncode(rsa.SignData(Encoding.UTF8.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            }
        }

        /// <summary>
        /// Loads the certificate for the Certificate authentication, either from the certificate file or from the Windows certificate store
        /// </summary>
        public X509Certificate2 LoadCertificate()
        {
            if (!string.IsNullOrEmpty(CertificatePath))
            {
                var path = FileHelper.ConvertOSFilePath(Repository.Instance.ReplaceRepositoryKeyword(CertificatePath));
                if (!File.Exists(path)) throw new Exception("Certificate file not found: " + path);
                return X509CertificateLoader.LoadPkcs12FromFile(path, ClearCertificatePassword);
            }

            if (!string.IsNullOrEmpty(CertificateThumbprint))
            {
                var thumbprint = CertificateThumbprint.Replace(" ", "").ToUpper();
                foreach (var location in new StoreLocation[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
                {
                    using (var store = new X509Store(StoreName.My, location))
                    {
                        store.Open(OpenFlags.ReadOnly);
                        var found = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                        if (found.Count > 0) return found[0];
                    }
                }
                throw new Exception("No certificate found in the Windows certificate store with the thumbprint: " + thumbprint);
            }

            throw new Exception("For the Certificate authentication, either the certificate file path or the certificate thumbprint must be set.");
        }

        /// <summary>
        /// Resolves the Microsoft Graph site identifier from the Site URL. The HttpClient must have a valid Bearer token.
        /// </summary>
        public string GetSiteId(HttpClient client)
        {
            var uri = new Uri(SiteUrl);
            var sitePath = uri.AbsolutePath.TrimEnd('/');
            var url = string.IsNullOrEmpty(sitePath) ? string.Format("{0}/v1.0/sites/{1}", GraphUrlNormalized, uri.Host) : string.Format("{0}/v1.0/sites/{1}:{2}", GraphUrlNormalized, uri.Host, sitePath);
            var response = client.GetAsync(url).Result;
            var json = response.Content.ReadAsStringAsync().Result;
            if (!response.IsSuccessStatusCode) throw new Exception(string.Format("SharePoint site '{0}' not found: {1}", SiteUrl, json));
            return JsonDocument.Parse(json).RootElement.GetProperty("id").GetString();
        }

        /// <summary>
        /// Resolves the drive identifier of the document library. The HttpClient must have a valid Bearer token.
        /// </summary>
        public string GetDriveId(HttpClient client)
        {
            var siteId = GetSiteId(client);
            var response = client.GetAsync(string.Format("{0}/v1.0/sites/{1}/drives?$select=id,name", GraphUrlNormalized, siteId)).Result;
            var json = response.Content.ReadAsStringAsync().Result;
            if (!response.IsSuccessStatusCode) throw new Exception("Unable to list the document libraries of the site: " + json);
            var drives = JsonDocument.Parse(json).RootElement.GetProperty("value");
            foreach (var drive in drives.EnumerateArray())
            {
                if (string.Equals(drive.GetProperty("name").GetString(), LibraryName, StringComparison.OrdinalIgnoreCase)) return drive.GetProperty("id").GetString();
            }
            var names = string.Join(", ", drives.EnumerateArray().Select(i => "'" + i.GetProperty("name").GetString() + "'"));
            throw new Exception(string.Format("Document library '{0}' not found in the site. Libraries available: {1}", LibraryName, names));
        }

        /// <summary>
        /// Escapes a document library path for its use in a Microsoft Graph URL (each segment is escaped, separators are kept)
        /// </summary>
        public string EscapePath(string path)
        {
            return string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
        }

        /// <summary>
        /// Process the report result for the output
        /// </summary>
        public override void Process(Report report)
        {
            var script = string.IsNullOrEmpty(ProcessingScript) ? ProcessingScriptTemplate : ProcessingScript;
            RazorHelper.CompileExecute(script, report);
        }

        /// <summary>
        /// Load an OutputSharePointDevice from a file
        /// </summary>
        static public OutputDevice LoadFromFile(string path, bool ignoreException)
        {
            OutputSharePointDevice result = null;
            try
            {
                path = FileHelper.ConvertOSFilePath(path);
                if (!File.Exists(path)) throw new Exception("File not found: " + path);

                XmlSerializer serializer = new XmlSerializer(typeof(OutputSharePointDevice));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (OutputSharePointDevice)serializer.Deserialize(xr);
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
            if (string.IsNullOrEmpty(TenantId)) throw new Exception("The Tenant Id cannot be empty.");
            if (string.IsNullOrEmpty(ClientId)) throw new Exception("The Client Id cannot be empty.");
            if (string.IsNullOrEmpty(SiteUrl)) throw new Exception("The Site URL cannot be empty.");
            if (string.IsNullOrEmpty(LibraryName)) throw new Exception("The Document library cannot be empty.");
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

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetAccessToken());
                    GetDriveId(client);
                }
                Information = string.Format("The connection to the site '{0}' is successfull. The document library '{1}' was found.", SiteUrl, LibraryName);
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
#if WINDOWS
        [Category("Helpers"), DisplayName("Test SharePoint connection"), Description("Test the connection with the current configuration."), Id(2, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
#endif
        public string HelperTestConnection
        {
            get { return "<Click to test the connection>"; }
        }
    }
}
