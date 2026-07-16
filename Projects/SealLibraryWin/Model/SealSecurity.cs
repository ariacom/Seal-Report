//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Data;
using System.ComponentModel;
using System.IO;
using Seal.Helpers;
using System.Xml;
#if WINDOWS
using System.Drawing.Design;
using Seal.Forms;
using DynamicTypeDescriptor;
#endif


namespace Seal.Model
{
    /// <summary>
    /// SealSecurity defines all the repository security
    /// </summary>
    public class SealSecurity : RootEditor
    {
        /// <summary>
        /// Current file path
        /// </summary>
        [XmlIgnore]
        public string FilePath;

        /// <summary>
        /// Current repository
        /// </summary>
        [XmlIgnore]
        public Repository Repository;

#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Groups").SetIsBrowsable(true);
                GetProperty("Logins").SetIsBrowsable(true);
                GetProperty("ProviderName").SetIsBrowsable(true);
                GetProperty("UseCustomScript").SetIsBrowsable(true);
                GetProperty("Script").SetIsBrowsable(true);
                GetProperty("EnableTwoFA").SetIsBrowsable(true);
                GetProperty("TwoFAGenerationScript").SetIsBrowsable(true);
                GetProperty("TwoFACheckScript").SetIsBrowsable(true);
                GetProperty("EnableResetPassword").SetIsBrowsable(true);
                GetProperty("ResetPasswordScript").SetIsBrowsable(true);
                GetProperty("ResetPasswordScript2").SetIsBrowsable(true);
                GetProperty("EnableChangePassword").SetIsBrowsable(true);
                GetProperty("ChangePasswordScript").SetIsBrowsable(true);
                GetProperty("CurrentParameters").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);
                GetProperty("TestUserName").SetIsBrowsable(true);
                GetProperty("TestPassword").SetIsBrowsable(true);
                GetProperty("TestCurrentWindowsUser").SetIsBrowsable(true);
                GetProperty("HelperSimulateLogin").SetIsBrowsable(true);


                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

#endif
        string _providerName;
        /// <summary>
        /// The security provider used for the authentication
        /// </summary>
#if WINDOWS
        [DisplayName("Security Provider"), Description("The security provider used for the authentication. Security providers are defined in the repository Security\\Providers folder."), Category("Security Provider Definition"), Id(2, 1)]
        [TypeConverter(typeof(SecurityProviderConverter))]
#endif
        public string ProviderName
        {
            get { return _providerName; }
            set
            {
                _providerName = value;
                UpdateEditorAttributes();
            }
        }

        /// <summary>
        /// The script executed to login and find the security group used to published reports
        /// </summary>
#if WINDOWS
        [Category("Security Provider Definition"), DisplayName("Provider Security Script"), Description("The script executed to login and find the security group used to published reports."), Id(4, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string ProviderScript
        {
            get { return Provider.Script; }
        }

        bool _useCustomScript = false;
        /// <summary>
        /// If true, a custom script can be used for the authentication process
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Use custom Security Script"), Description("If true, a custom script can be used for the authentication process."), Id(2, 2)]
        [DefaultValue(false)]
#endif
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
        /// The script executed to login and find the security group used to published reports. If the script is empty, the publication is done using the first security group defined.
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Custom Security Script"), Description("The script executed to login and find the security group used to published reports. If the script is empty, the publication is done using the first security group defined."), Id(3, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string Script { get; set; }

        /// <summary>
        /// If true, the Two-Factor Authentication is enabled: after the login, a security code is sent to the user (by default by email using the notification email device) and checked in the login page.
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Enable Two-Factor Authentication"), Description("If true, the Two-Factor Authentication is enabled: after the login, a security code is sent to the user (by default by email using the notification email device) and checked in the login page."), Id(4, 2)]
        [DefaultValue(false)]
#endif
        public bool EnableTwoFA { get; set; } = false;

        /// <summary>
        /// If the Two-Factor Authentication is enabled and the script is not empty, the script is executed to generate and send the security code (otherwise the default implementation is used: the code is sent by email using the notification email device).
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Two-Factor Authentication Generation Script"), Description("If the Two-Factor Authentication is enabled and the script is not empty, the script is executed to generate and send the security code (otherwise the default implementation is used: the code is sent by email using the notification email device)."), Id(5, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string TwoFAGenerationScript { get; set; }

        /// <summary>
        /// If the Two-Factor Authentication is enabled and the script is not empty, the script is executed to check the security code and validate the Two-Factor Authentication (otherwise the default implementation is used).
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Two-Factor Authentication Check Script"), Description("If the Two-Factor Authentication is enabled and the script is not empty, the script is executed to check the security code and validate the Two-Factor Authentication (otherwise the default implementation is used)."), Id(6, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string TwoFACheckScript { get; set; }

        /// <summary>
        /// If true, the Reset Password feature is enabled: a link to reset the password is displayed in the login page. The reset link is sent by email, so a notification email device must be configured.
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Enable Reset Password"), Description("If true, the Reset Password feature is enabled: a link to reset the password is displayed in the login page. The reset link is sent by email, so a notification email device must be configured."), Id(7, 2)]
        [DefaultValue(false)]
#endif
        public bool EnableResetPassword { get; set; } = false;

        /// <summary>
        /// If the Reset Password feature is enabled and the script is not empty, the script is executed when a user requests a password reset (otherwise the default implementation is used: a reset link is sent by email using the notification email device).
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Reset Password Script (Init Reset)"), Description("If the Reset Password feature is enabled and the script is not empty, the script is executed when a user requests a password reset (otherwise the default implementation is used: a reset link is sent by email using the notification email device)."), Id(8, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ResetPasswordScript { get; set; }

        /// <summary>
        /// If the Reset Password feature is enabled and the script is not empty, the script is executed to perform the password reset (otherwise the default implementation is used).
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Reset Password Script2 (Apply Reset)"), Description("If the Reset Password feature is enabled and the script is not empty, the script is executed to perform the password reset (otherwise the default implementation is used)."), Id(9, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ResetPasswordScript2 { get; set; }

        /// <summary>
        /// If true, the Change Password feature is enabled for users having a login defined in the security file.
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Enable Change Password"), Description("If true, the Change Password feature is enabled for users having a login defined in the security file."), Id(10, 2)]
        [DefaultValue(false)]
#endif
        public bool EnableChangePassword { get; set; } = false;

        /// <summary>
        /// If the Change Password feature is enabled and the script is not empty, the script is executed when a user changes his password (otherwise the default implementation is used).
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Change Password Script"), Description("If the Change Password feature is enabled and the script is not empty, the script is executed when a user changes his password (otherwise the default implementation is used)."), Id(11, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string ChangePasswordScript { get; set; }

        /// <summary>
        /// True if the Two-Factor Authentication is enabled
        /// </summary>
        [XmlIgnore]
        public bool TwoFAActive
        {
            get { return EnableTwoFA; }
        }

        /// <summary>
        /// Script executed to generate and send the security code: the custom script if defined, otherwise the default implementation
        /// </summary>
        [XmlIgnore]
        public string TwoFAGenerationScriptEffective
        {
            get { return !string.IsNullOrEmpty(TwoFAGenerationScript) ? TwoFAGenerationScript : DefaultTwoFAGenerationScript; }
        }

        /// <summary>
        /// Script executed to check the security code: the custom script if defined, otherwise the default implementation
        /// </summary>
        [XmlIgnore]
        public string TwoFACheckScriptEffective
        {
            get { return !string.IsNullOrEmpty(TwoFACheckScript) ? TwoFACheckScript : DefaultTwoFACheckScript; }
        }

        /// <summary>
        /// Default implementation used when TwoFAGenerationScript is empty: sends the security code by email using the notification email device
        /// </summary>
        public const string DefaultTwoFAGenerationScript = @"@{
    SecurityUser user = Model;

    //Generate the security code
    Random rnd = new Random();
    user.SecurityCode = rnd.Next(100000, 1000000).ToString();
    user.SecurityCodeGeneration = DateTime.Now;

    var message = user.Security.Repository.TranslateReport(""Please find your authentication code"");

    var from = """"; //Default of the device will be used
    var to = user.Login?.Email; //Destination email: could be got from database, LDAP, etc.
    if (string.IsNullOrEmpty(to)) {
        throw new Exception($""No Email Address for the user {user.WebUserName}."");
    }

    var subject = ""Seal Report"";
    var body = $""{message}: <br><b>{user.SecurityCode}</b>"";
    var isHtml = true;

    if (!user.Security.Repository.SendNotificationEmail(from, to, subject, isHtml, body)) {
        throw new Exception(""Unable to send email. Check that an Email Device is defined for notification. Restart the 'Server Manager' after changing the configuration."");
    }

    //Message displayed in the login page
    user.SecurityCodeMessage = user.Security.Repository.TranslateReport(""A security code has been sent by Email at"") + $"": {Helper.MaskEmail(to)}"";
}
";

        /// <summary>
        /// Default implementation used when TwoFACheckScript is empty: checks the security code (5 minutes validity, 3 tries)
        /// </summary>
        public const string DefaultTwoFACheckScript = @"@{
    SecurityUser user = Model;

    //Check that the code has been generated in the previous 5 minutes
    if (string.IsNullOrEmpty(user.SecurityCode) || user.SecurityCodeGeneration < DateTime.Now.AddMinutes(-5)) {
        user.SecurityCodeTries = -1; //Set it to -1 to re-force a login
    }
    else {
        user.SecurityCodeTries++;
        if (user.SecurityCode != user.WebSecurityCode)
        {
            if (user.SecurityCodeTries >= 3) user.SecurityCodeTries = -1; //Set it to -1 to re-force a login
            else throw new Exception(user.Security.Repository.TranslateWeb(""Invalid security code"")); //Allow a retry
        }
        else {
            user.SecurityCode = """"; //Check is ok
        }
    }
}
";

        /// <summary>
        /// True if the Reset Password feature is enabled
        /// </summary>
        [XmlIgnore]
        public bool ResetPasswordActive
        {
            get { return EnableResetPassword; }
        }

        /// <summary>
        /// Script executed when a user requests a password reset: the custom script if defined, otherwise the default implementation
        /// </summary>
        [XmlIgnore]
        public string ResetPasswordScriptEffective
        {
            get { return !string.IsNullOrEmpty(ResetPasswordScript) ? ResetPasswordScript : DefaultResetPasswordScript; }
        }

        /// <summary>
        /// Script executed to perform the password reset: the custom script if defined, otherwise the default implementation
        /// </summary>
        [XmlIgnore]
        public string ResetPasswordScript2Effective
        {
            get { return !string.IsNullOrEmpty(ResetPasswordScript2) ? ResetPasswordScript2 : DefaultResetPasswordScript2; }
        }

        /// <summary>
        /// Default implementation used when ResetPasswordScript is empty: sends a reset link by email using the notification email device
        /// </summary>
        public const string DefaultResetPasswordScript = @"@using Newtonsoft.Json
@using System.Web
@{
    SecurityUser user = Model;
    var id = user.WebUserName;
    var request = user.Request;
    var repository = user.Security.Repository;
    var security = user.Security;

    //Implementation using Logins defined in the security
    var login = security.Logins.FirstOrDefault(i => i.Id == id);
    if (login == null && id.Contains(""@"")) login = security.Logins.FirstOrDefault(i => i.Email == id);

    if (login == null) throw new Exception($""No login found for '{id}'"");
    if (string.IsNullOrEmpty(login.Email)) throw new Exception($""No Email found for '{id}'"");

    //Generate Token for reset
    var guid = Guid.NewGuid().ToString();
    var vals = new List<string>
    {
        id,
        guid,
        JsonConvert.SerializeObject(DateTime.Now)
    };
    var token = HttpUtility.UrlEncode(CryptoHelper.EncryptWithRSAContainer(string.Join(""\r"", vals), ""Reset"" + guid, false));

    var url = $""{request.Scheme}://{request.Host}{request.PathBase}?guid={guid}&ptoken={token}"";
    var message = repository.TranslateWeb(""Please find your link to reset your password (Note that this link is valid 10 minutes):"");
    var from = """"; //Default of the device will be used
    var to = login.Email;
    var subject = ""Seal Report Password Reset"";
    var linkLabel = repository.TranslateWeb(""Reset link"");
    var body = $""{ message}: <br><b><a href='{url}'>{linkLabel}</a></b><br>"";

    if (!repository.SendNotificationEmail(from, to, subject, true, body)) throw new Exception(""Unable to send email for Reset Password."");
}
";

        /// <summary>
        /// Default implementation used when ResetPasswordScript2 is empty: checks the token and changes the password of the login
        /// </summary>
        public const string DefaultResetPasswordScript2 = @"@using Newtonsoft.Json
@{
    SecurityUser user = Model;
    var repository = user.Security.Repository;
    var security = user.Security;
    var guid = user.WebUserName;

    //Implementation using Logins defined in the security
    if (!Helper.IsPasswordComplex(user.WebPassword)) throw new Exception(repository.TranslateWeb(""Your password must contain at least 8 characters, including at least one uppercase letter, one number, and one special character (e.g., !@#$%^&*).""));

    var vals = CryptoHelper.DecryptWithRSAContainer(user.Token, ""Reset"" + guid, false).Split(""\r"");
    if (vals.Length != 3 || vals[1] != guid) throw new Exception(""Invalid token"");
    var generationDate = JsonConvert.DeserializeObject<DateTime>(vals[2]);
    //Check date
    if (DateTime.Now > generationDate.AddMinutes(10)) throw new Exception(""The token is not valid anymore."");

    var id = vals[0];
    user.WebUserName = id;
    var login = security.Logins.FirstOrDefault(i => i.Id == id);
    if (login == null && id.Contains(""@"")) login = security.Logins.FirstOrDefault(i => i.Email == id);

    if (login == null) throw new Exception(""Invalid login in token."");

    login.HashedPassword = user.WebPassword;
    security.SaveToFile();

    if (!string.IsNullOrEmpty(login.Email))
    {
        var message = repository.TranslateWeb(""Your password has been changed after a Reset."");
        var from = """"; //Default of the device will be used
        var to = login.Email;
        var subject = repository.TranslateWeb(""Seal Report Password Change"");
        var body = $""{message}<br>"";
        if (!repository.SendNotificationEmail(from, to, subject, true, body)) Audit.LogEventAudit(AuditType.EventError, ""Unable to send email for Change Password afer Reset."");
    }
}
";

        /// <summary>
        /// Current SecurityProvider
        /// </summary>
        public SecurityProvider Provider
        {
            get
            {
                var result = Providers.FirstOrDefault(i => i.Name == ProviderName);
                if (result == null) result = Providers.FirstOrDefault(i => i.Name == "No Security");
                if (result == null) result = Providers.FirstOrDefault();
                if (result == null) throw new Exception("Invalid configuration: No security providers available. Check your repository configuration...");
                _providerName = result.Name;
                return result;
            }
        }


        List<SecurityProvider> _providers = null;
        /// <summary>
        /// List of SecurityProviders available in the repository
        /// </summary>
        [XmlIgnore]
        public List<SecurityProvider> Providers
        {
            get
            {
                if (_providers == null)
                {
                    _providers = SecurityProvider.LoadProviders(Repository.SecurityProvidersFolder);
                }
                return _providers;
            }
        }

        /// <summary>
        /// List of SecurityParameter used by the provider
        /// </summary>
        public List<SecurityParameter> Parameters { get; set; } = new List<SecurityParameter>();
        /// <summary>
        /// Serialize Parameters only if not empty
        /// </summary>
        public bool ShouldSerializeParameters() { return Parameters.Count > 0; }

        /// <summary>
        /// Parameter values used in the script
        /// </summary>
#if WINDOWS
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        [Category("Security Provider Definition"), DisplayName("Parameters"), Description("Parameter values used in the script."), Id(3, 1)]
#endif
        [XmlIgnore]
        public List<SecurityParameter> CurrentParameters
        {
            get
            {
                //Copy existing parameter values
                for (int i = 0; i < Parameters.Count; i++)
                {
                    var param = Parameters[i];
                    var paramProvider = Provider.Parameters.FirstOrDefault(j => j.Name == param.Name);
                    if (paramProvider != null) paramProvider.Value = param.Value;
                }
                //Then use the parameters of the provider
                Parameters.AddRange(Provider.Parameters.Where(i => !Parameters.Exists(j => j.Name == i.Name)));
                return Provider.Parameters;
            }
            set { Parameters = value; }
        }

        /// <summary>
        /// The groups defines how are published folders and reports in the Web Report Server. At least one group must exist.
        /// </summary>
#if WINDOWS
        [Category("Groups and logins"), DisplayName("Security Groups"), Description("The groups defines how are published folders and reports in the Web Report Server. At least one group must exist."), Id(1, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<SecurityGroup> Groups { get; set; } = new List<SecurityGroup>();
        /// <summary>
        /// Serialize Groups only if not empty
        /// </summary>
        public bool ShouldSerializeGroups() { return Groups.Count > 0; }

        /// <summary>
        /// The groups defines how are published folders and reports in the Web Report Server. At least one group must exist.
        /// </summary>
#if WINDOWS
        [Category("Groups and logins"), DisplayName("Security Logins"), Description("The logins defined in the security. Depending on the Security Provider defined, logins may be used during the authentication process. This is the case for the 'Basic Autentication' Security provider"), Id(2, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<SecurityLogin> Logins { get; set; } = new List<SecurityLogin>();
        /// <summary>
        /// Serialize Logins only if not empty
        /// </summary>
        public bool ShouldSerializeLogins() { return Logins.Count > 0; }

        /// <summary>
        /// Returns a SecurityFolder from a given name
        /// </summary>
        public SecurityFolder FindSecurityFolder(List<SecurityGroup> groups, string folder)
        {
            SecurityFolder result = null;
            //Weight of the group that provided the current tree view icon (to break ties when several groups define the folder)
            int iconWeight = int.MinValue;
            foreach (var group in groups)
            {
                string folderRoot = folder;
                SecurityFolder current = null;
                while (current == null && !string.IsNullOrEmpty(folderRoot))
                {
                    current = group.Folders.FirstOrDefault(i => i.Path == folderRoot);
                    folderRoot = Path.GetDirectoryName(folderRoot);
                }
                if (current != null && current.Path != folder && !current.UseSubFolders)
                {
                    //cannot use this parent
                    current = null;
                }

                if (current != null)
                {
                    if (result != null)
                    {
                        //Merge the groupFolder find in this group with the current result
                        //Highest right is applied..
                        result.FolderRight = (FolderRight)Math.Max((int)result.FolderRight, (int)current.FolderRight);
                        result.AllowReportDownload = result.AllowReportDownload || current.AllowReportDownload;
                        result.AllowUpload = result.AllowUpload || current.AllowUpload;
                        result.ExpandSubFolders = result.ExpandSubFolders || current.ExpandSubFolders;
                        result.ManageFolder = result.ManageFolder && current.ManageFolder;
                    }
                    else
                    {
                        result = (SecurityFolder)Helper.Clone(current);
                        result.Icon = null;
                    }

                    //The tree view icon is the one set on the folder by the group having the highest weight.
                    //It applies only to the folder where it is explicitly defined; sub-folders inherited via
                    //UseSubFolders keep the default icon.
                    if (current.Path == folder && !string.IsNullOrEmpty(current.Icon) && group.Weight > iconWeight)
                    {
                        result.Icon = current.Icon;
                        iconWeight = group.Weight;
                    }

                    //set IsDefined flag
                    if (current.Path == folder) result.IsDefined = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the SecurityRepositoryFolder applying to a given repository-relative path (normalized, leading separator,
        /// no Repository prefix), merging the rights of all matching groups. Folders pointing at or inside the 'Reports' tree
        /// are ignored (conflict with the Report Folders configuration). Returns null when no group grants a right.
        /// </summary>
        public SecurityRepositoryFolder FindRepositorySecurityFolder(List<SecurityGroup> groups, string finalPath)
        {
            finalPath = SecurityRepositoryFolder.Normalize(finalPath);
            if (SecurityRepositoryFolder.IsUnderReports(finalPath)) return null;

            SecurityRepositoryFolder result = null;
            int iconWeight = int.MinValue;
            foreach (var group in groups)
            {
                string folderRoot = finalPath;
                SecurityRepositoryFolder current = null;
                while (current == null && !string.IsNullOrEmpty(folderRoot))
                {
                    current = group.RepositoryFolders.FirstOrDefault(i => i.FolderRight != RepositoryFolderRight.None && SecurityRepositoryFolder.Normalize(i.Path) == folderRoot);
                    folderRoot = Path.GetDirectoryName(folderRoot);
                }
                if (current != null && SecurityRepositoryFolder.Normalize(current.Path) != finalPath && !current.UseSubFolders)
                {
                    //cannot use this parent
                    current = null;
                }

                if (current != null)
                {
                    bool isExact = SecurityRepositoryFolder.Normalize(current.Path) == finalPath;
                    if (result != null)
                    {
                        //Highest right is applied
                        result.FolderRight = (RepositoryFolderRight)Math.Max((int)result.FolderRight, (int)current.FolderRight);
                        result.AllowUpload = result.AllowUpload || current.AllowUpload;
                        result.ManageFolder = result.ManageFolder || current.ManageFolder;
                    }
                    else
                    {
                        result = (SecurityRepositoryFolder)Helper.Clone(current);
                        result.Icon = null;
                    }

                    //The tree view icon is the one set on the folder by the group having the highest weight (only where explicitly defined).
                    if (isExact && !string.IsNullOrEmpty(current.Icon) && group.Weight > iconWeight)
                    {
                        result.Icon = current.Icon;
                        iconWeight = group.Weight;
                    }

                    if (isExact) result.IsDefined = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Init security defaults and integrities
        /// </summary>
        public void InitSecurity()
        {
            //init at least a security group
            if (Groups.Count == 0)
            {
                SecurityGroup group = new SecurityGroup() { Name = "Default Group" };
                Groups.Add(group);
                group.Folders.Add(new SecurityFolder());
            }
            //Move to GUIDs if necessary (from version 8.4)
            bool save = false;
            foreach (var g in Groups.Where(i => string.IsNullOrEmpty(i.GUID)))
            {
                save = true;
                g.GUID = Helper.NewGUID();
                foreach (var l in Logins.Where(i => i.GroupNames.Contains(g.Name)))
                {
                    l.GroupNames.Remove(g.Name);
                    l.GroupIds.Add(g.GUID);
                }
            }
            foreach (var l in Logins.Where(i => string.IsNullOrEmpty(i.GUID)))
            {
                save = true;
                l.GUID = Helper.NewGUID();
            }
            //Remove deleted groups in logins
            foreach (var login in Logins)
            {
                login.GroupNames.Clear();
                login.GroupIds.RemoveAll(i => !Groups.Exists(j => j.GUID == i));
            }
            if (save) SaveToFile();
        }

        /// <summary>
        /// Last modification date time
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification;

        /// <summary>
        /// Load a SealSecurity from a file
        /// </summary>
        static public SealSecurity LoadFromFile(string path, bool ignoreException)
        {
            SealSecurity result = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SealSecurity));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (SealSecurity)serializer.Deserialize(xr);
                }
                result.FilePath = path;
                result.LastModification = File.GetLastWriteTime(path);
                result.InitSecurity();
            }
            catch (Exception ex)
            {
                if (!ignoreException) throw new Exception(string.Format("Unable to read the security file '{0}'.\r\n{1}", path, ex.Message));
                result = new SealSecurity();
                result.InitSecurity();
            }
            return result;
        }


        /// <summary>
        /// Save to the current file
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(FilePath);
        }

        /// <summary>
        /// Save to a destination file path
        /// </summary>
        /// <param name="path"></param>
        public void SaveToFile(string path)
        {
            //Check last modification
            if (LastModification != DateTime.MinValue && File.Exists(path))
            {
                DateTime lastDateTime = File.GetLastWriteTime(path);
                if (LastModification != lastDateTime)
                {
                    throw new Exception("Unable to save the Security file. The file has been modified by another user.");
                }
            }
            Helper.Serialize(path, this);
            FilePath = path;
            LastModification = File.GetLastWriteTime(path);
        }

        /// <summary>
        /// Last error message
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(7, 11)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string Error
        {
            get
            {
                string result = "";
                foreach (var provider in Providers.Where(i => !string.IsNullOrEmpty(i.Error)))
                {
                    result += string.Format("{0}: {1}\r\n", provider.Name, provider.Error);
                }
                return result;
            }
        }

        /// <summary>
        /// Editor Helper: User name to test a login
        /// </summary>
#if WINDOWS
        [Category("Test a login"), DisplayName("User name for test"), Description("User name to test a login."), Id(8, 10)]
#endif
        [XmlIgnore]
        public string TestUserName { get; set; }

        /// <summary>
        /// Editor Helper: Password to test a login
        /// </summary>
#if WINDOWS
        [Category("Test a login"), PasswordPropertyText(true), DisplayName("Password for test"), Description("Password to test a login."), Id(9, 10)]
#endif
        [XmlIgnore]
        public string TestPassword { get; set; }

        /// <summary>
        /// Editor Helper: If true, the current user will be use as IPrincipal to test the Integrated Windows authentication
        /// </summary>
#if WINDOWS
        [Category("Test a login"), DisplayName("Use current Windows User to test authentication"), Description("If true, the current user will be use as IPrincipal to test the Integrated Windows authentication."), Id(11, 10)]
#endif
        [XmlIgnore]
        public bool TestCurrentWindowsUser { get; set; } = true;

        /// <summary>
        /// Editor Helper: Test a login using the test user name and password or the current windows user
        /// </summary>
#if WINDOWS
        [Category("Test a login"), DisplayName("Test a login"), Description("Test a login using the test user name and password or the current windows user."), Id(12, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
#endif
        public string HelperSimulateLogin
        {
            get { return "<Click to test a login>"; }
        }

        /// <summary>
        /// True if a parameter with a given name has a value
        /// </summary>
        public bool HasValue(string name)
        {
            return !string.IsNullOrEmpty(GetValue(name));
        }

        /// <summary>
        /// Parameter value of a given name
        /// </summary>
        public string GetValue(string name)
        {
            SecurityParameter parameter = CurrentParameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? "" : parameter.Value;
        }

        /// <summary>
        /// Parameter boolean value of a given name
        /// </summary>
        public bool GetBoolValue(string name)
        {
            SecurityParameter parameter = CurrentParameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? false : parameter.BoolValue;
        }

        /// <summary>
        /// Parameter numeric value of a given name
        /// </summary>
        public int GetNumericValue(string name)
        {
            SecurityParameter parameter = CurrentParameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? 0 : parameter.NumericValue;
        }

        /// <summary>
        /// Returns a parameter double value
        /// </summary>
        public double GetDoubleValue(string name)
        {
            Parameter parameter = Parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? 0 : parameter.DoubleValue;
        }

        /// <summary>
        /// List of logged users
        /// </summary>
        static public List<SecurityUser> LoggedUsers = new List<SecurityUser>();

        /// <summary>
        /// Add a logged user
        /// </summary>
        static public void AddLoggedUsed(SecurityUser user)
        {
            lock (LoggedUsers)
            {
                if (!LoggedUsers.Contains(user)) LoggedUsers.Add(user);
            }
        }

        /// <summary>
        /// Remove a logged user
        /// </summary>
        static public void RemoveLoggedUsed(SecurityUser user)
        {
            lock (LoggedUsers)
            {
                if (LoggedUsers.Contains(user)) LoggedUsers.Remove(user);
            }
        }

        /// <summary>
        /// Number of authenticated logged users
        /// </summary>
        static public int AuthenticatedLoggedUserCount
        {
            get
            {
                lock (LoggedUsers)
                {
                    return LoggedUsers.Count(i => i.IsAuthenticated);
                }
            }
        }
    }
}
