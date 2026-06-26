//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using Seal.AI;
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;

namespace Seal.Model
{
    /// <summary>
    /// A SecurityUser defines a logged user with all security objects
    /// </summary>
    public class SecurityUser
    {
        /// <summary>
        /// Name of the user
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Security login if the authentication using Security Logins is used
        /// </summary>
        public SecurityLogin Login = null;

        /// <summary>
        /// Personal folder name
        /// </summary>
        public string PersonalFolderName = "";

        /// <summary>
        /// List of SecurityGroup of the users
        /// </summary>
        public List<SecurityGroup> SecurityGroups = new List<SecurityGroup>();

        /// <summary>
        /// Current SealSecurity
        /// </summary>
        public SealSecurity Security;

        /// <summary>
        /// Last error
        /// </summary>
        public string Error = "";

        /// <summary>
        /// Last warning
        /// </summary>
        public string Warning = "";

        /// <summary>
        /// Current list of SWIFolder of the user
        /// </summary>
        public List<SWIFolder> Folders = new List<SWIFolder>();

        /// <summary>
        /// Flat list of all SWIFolder of the user 
        /// </summary>
        public List<SWIFolder> AllFolders
        {
            get
            {
                var result = new List<SWIFolder>();
                foreach (var child in Folders)
                {
                    fillFolder(child, result);
                }
                return result;
            }
        }

        void fillFolder(SWIFolder folder, List<SWIFolder> folders)
        {
            folders.Add(folder);
            if (folder.folders != null)
            {
                foreach (var child in folder.folders)
                {
                    fillFolder(child, folders);
                }
            }
        }

        /// <summary>
        /// Returns the full physical path of a folder relative path.
        /// </summary>
        public string GetFullPath(string path)
        {
            path = FileHelper.ConvertOSFilePath(path);
            if (path.StartsWith(SWIFolder.GetPersonalRoot())) return Security.Repository.GetPersonalFolder(this) + path.Substring(1);
            else if (path.StartsWith(SWIFolder.GetRepositoryRoot())) return Security.Repository.RepositoryPath + path.Substring(1);
            else return Security.Repository.ReportsFolder + path;
        }

        /// <summary>
        /// Returns the SWIFolder for a given relative path, applying the user's security rights.
        /// </summary>
        public SWIFolder GetFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new Exception("Error: path must be supplied");
            if (!IsAuthenticated) throw new Exception("Error: user is not authenticated");
            if (path.Contains("..\\") || path.Contains("../")) throw new Exception("Error: invalid path");
            path = FileHelper.ConvertOSFilePath(path);

            SWIFolder result = AllFolders.FirstOrDefault(i => i.path == path);
            if (result != null)
            {
                //Folder was already initialized
                result.SetFullPath(GetFullPath(path));
                return result;
            }

            result = new SWIFolder();
            result.path = path;
            result.right = 0;
            result.sql = SqlModel;
            result.SetFullPath(GetFullPath(path));

            if (result.IsRepository)
            {
                //Repository folder (Danger Zone): published anywhere under the repository root, outside the Reports tree
                result.type = "repository";
                result.files = true; //repository folders manage files only (no report execution)
                var leaf = Path.GetFileName(result.FinalPath.TrimEnd(Path.DirectorySeparatorChar));
                result.name = string.IsNullOrEmpty(leaf) ? Security.Repository.TranslateWeb("Repository") : leaf;
                result.fullname = Security.Repository.TranslateWeb("Repository") + result.FinalPath;
                SecurityRepositoryFolder repoFolder = FindRepositorySecurityFolder(result.FinalPath);
                if (repoFolder != null)
                {
                    //Manage sub-folders only when sub-folders are shown and the folder is writable
                    bool manageFolder = repoFolder.ManageFolder && repoFolder.FolderRight == RepositoryFolderRight.ReadWrite;
                    result.SetManageFlag(repoFolder.UseSubFolders, manageFolder, repoFolder.IsDefined);
                    result.expand = false;
                    //Map the simple repository right to the internal FolderRight used by the web file management
                    result.right = (int)(repoFolder.FolderRight == RepositoryFolderRight.ReadWrite ? FolderRight.Edit : FolderRight.Execute);
                    //Repository folders never show reports, so only upload is configurable; files are always downloadable
                    result.upload = repoFolder.AllowUpload;
                    if (!string.IsNullOrEmpty(repoFolder.Icon)) result.icon = Parameter.GetFontAwesomeIcon(repoFolder.Icon);
                }
            }
            else if (result.IsPersonal)
            {
                //Personal
                if (PersonalFolderRight == PersonalFolderRight.None) throw new Exception("Error: this user has no personal folder");
                result.SetManageFlag(true, true, result.FinalPath == "");
                result.expand = false;
                string prefix = Security.Repository.GetPersonalFolderName(this);
                var folderLeafName = Path.GetFileName(result.FinalPath);
                if (folderLeafName == Repository.RecycleBinFolderName)
                {
                    result.type = "bin";
                    //The recycle bin is a system folder: forbid creating sub-folders in it, as well as renaming or deleting it (manage = 0 = not user-managed)
                    result.manage = 0;
                }
                if (result.FinalPath == "") result.type = "personal";
                result.name = (result.FinalPath == "" ? prefix : (result.type == "bin" ? Security.Repository.TranslateWeb(Repository.RecycleBinFolderName) : folderLeafName));
                result.fullname = prefix + (result.FinalPath == "" ? Path.DirectorySeparatorChar.ToString() : "") + result.FinalPath;
                result.right = (int)FolderRight.Edit;
                result.files = (PersonalFolderRight == PersonalFolderRight.Files);
                //Personal folder download/upload is configured on the user's groups (both opt-in, disabled by default)
                result.reportdownload = PersonalFolderReportDownload;
                result.upload = PersonalFolderUpload;
            }
            else
            {
                if (result.FinalPath == Path.DirectorySeparatorChar.ToString()) result.type = "reports";
                result.name = (result.FinalPath == Path.DirectorySeparatorChar.ToString() ? Security.Repository.TranslateWeb("Reports") : Security.Repository.TranslateFolderName(path));
                result.fullname = Security.Repository.TranslateWeb("Reports") + Security.Repository.TranslateFolderPath(result.FinalPath);
                SecurityFolder securityFolder = FindSecurityFolder(path);
                if (securityFolder != null)
                {
                    result.SetManageFlag(securityFolder.UseSubFolders, securityFolder.ManageFolder, securityFolder.IsDefined);
                    result.expand = securityFolder.ExpandSubFolders;
                    result.right = (int)securityFolder.FolderRight;
                    result.files = securityFolder.FilesOnly;
                    //Report download and upload rights are defined per folder in the security
                    result.reportdownload = securityFolder.AllowReportDownload;
                    result.upload = securityFolder.AllowUpload;
                    //Optional tree view icon defined on the folder (group with the highest weight wins when several define it)
                    if (!string.IsNullOrEmpty(securityFolder.Icon)) result.icon = Parameter.GetFontAwesomeIcon(securityFolder.Icon);
                }
            }
            return result;
        }

        /// <summary>
        /// Fills the sub-folders of a folder applying the user's security rights.
        /// </summary>
        public void FillFolder(SWIFolder folder)
        {
            List<SWIFolder> subFolders = new List<SWIFolder>();
            if (folder.IsPersonal && PersonalFolderRight == PersonalFolderRight.None) return;

            string folderPath = folder.GetFullPath();
            foreach (string subFolder in Directory.GetDirectories(folderPath))
            {
                // _Agents is a hidden system folder – never expose it in the browser
                if (folder.IsPersonal && Path.GetFileName(subFolder) == AgentFolders.FolderName) continue;

                SWIFolder sub = GetFolder(folder.Combine(subFolder));
                //Add if right on this folder, or a sub folder is defined with this root
                if ((sub.right > 0) || SecurityGroups.Exists(i => i.Folders.Exists(j => j.Path.StartsWith(sub.path + (sub.path == Path.DirectorySeparatorChar.ToString() ? "" : Path.DirectorySeparatorChar.ToString())) && j.FolderRight != FolderRight.None)))
                {
                    FillFolder(sub);
                    subFolders.Add(sub);
                }
            }
            folder.folders = subFolders;
        }

        /// <summary>
        /// Adds the folders having rights (recursively) to the result list.
        /// </summary>
        void AddValidFolders(SWIFolder folder, List<SWIFolder> result)
        {
            if (folder.right == 0)
            {
                //Add only folder with rights
                foreach (var childFolder in folder.folders)
                {
                    AddValidFolders(childFolder, result);
                }
            }
            else
            {
                result.Add(folder);
            }
        }

        /// <summary>
        /// Build the current folders of the user (including Personal folders) and execute the Folders Scripts of the user's security groups.
        /// </summary>
        public void SetFolders()
        {
            List<SWIFolder> result = new List<SWIFolder>();
            //Personal
            if (PersonalFolderRight != PersonalFolderRight.None)
            {
                var personalFolder = GetFolder(SWIFolder.GetPersonalRoot());
                FillFolder(personalFolder);
                result.Add(personalFolder);
            }
            //Report
            var folder = GetFolder(Path.DirectorySeparatorChar.ToString());
            FillFolder(folder);
            if (ShowAllFolders)
            {
                result.Add(folder);
            }
            else
            {
                AddValidFolders(folder, result);
            }

            //Repository folders (Danger Zone): one top-level node per distinct configured folder, outside the Reports tree
            var repoRoots = new List<string>();
            foreach (var group in SecurityGroups)
            {
                foreach (var repoFolder in group.RepositoryFolders)
                {
                    if (repoFolder.FolderRight == RepositoryFolderRight.None) continue;
                    var norm = SecurityRepositoryFolder.Normalize(repoFolder.Path);
                    //Conflict safety net: ignore anything pointing at or inside the Reports tree
                    if (SecurityRepositoryFolder.IsUnderReports(norm)) continue;
                    if (repoRoots.Contains(norm)) continue;
                    repoRoots.Add(norm);

                    var repoNode = GetFolder(SWIFolder.GetRepositoryRoot() + norm);
                    if (repoNode.right > 0 && Directory.Exists(repoNode.GetFullPath()))
                    {
                        FillFolder(repoNode);
                        result.Add(repoNode);
                    }
                }
            }

            //Folders Script
            Folders = result;
            ScriptNumber = 1;
            foreach (var group in SecurityGroups.Where(i => !string.IsNullOrEmpty(i.FoldersScript)).OrderBy(i => i.Name))
            {
                RazorHelper.CompileExecute(group.FoldersScript, this);
                ScriptNumber++;
            }
        }

        /// <summary>
        /// Current folder detail of the user
        /// </summary>
        public SWIFolderDetail FolderDetail = new SWIFolderDetail();

        /// <summary>
        /// Current script execution number when several Folders or Folder Detail scripts are executed.
        /// </summary>
        public int ScriptNumber = 0;

        /// <summary>
        /// List of all SWIFolderDetail of the user. The list is built when the user browse the folders.
        /// </summary>
        public List<SWIFolderDetail> FolderDetails = new List<SWIFolderDetail>();

        /// <summary>
        /// Current reports web menu of the user (used for Folder Detail Scripts)
        /// </summary>
        public SWIWebMenu WebMenu = new SWIWebMenu();

        /// <summary>
        /// Current SecurityUserProfile
        /// </summary>
        public SecurityUserProfile Profile = new SecurityUserProfile();

        /// <summary>
        /// The folder path (SWI format) the user is currently browsing in the web interface.
        /// Set by SWIGetFolderDetail each time the user navigates to a folder.
        /// Empty string means the Reports root; ":" means the Personal root.
        /// </summary>
        public string CurrentFolder = "";

        /// <summary>
        /// User email if set during login process
        /// </summary>
        public string Email;

        /// <summary>
        /// Custom string got in user profile
        /// </summary>
        public string Tag;

        /// <summary>
        /// Custom string got in user profile
        /// </summary>
        public string Tag2;

        /// <summary>
        /// Parameters for authentication: User name 
        /// </summary>
        public string WebUserName = "";

        /// <summary>
        /// Parameters for authentication: Password
        /// </summary>
        public string WebPassword = "";

        /// <summary>
        /// Web host name
        /// </summary>
        public string WebHostName = "";

        /// <summary>
        /// Current SessionID
        /// </summary>
        public string SessionID = "";

        /// <summary>
        /// Parameters for authentication: Token
        /// </summary>
        public string Token = null;

        /// <summary>
        /// Parameters for authentication: Security code sent by the user for 2FA
        /// </summary>
        public string WebSecurityCode = "";

        /// <summary>
        /// Parameters for authentication: Security code generated for 2FA
        /// </summary>
        public string SecurityCode = null;

        /// <summary>
        /// Parameters for authentication: Security code message generated for 2FA
        /// </summary>
        public string SecurityCodeMessage = null;

        /// <summary>
        /// Parameters for authentication: Security code generation date for 2FA
        /// </summary>
        public DateTime SecurityCodeGeneration = DateTime.MinValue;

        /// <summary>
        /// Number of tries to check the security code for 2FA
        /// </summary>
        public int SecurityCodeTries = 0;

        /// <summary>
        /// Parameters for authentication: The Request done for the login
        /// </summary>
        public HttpRequest Request = null;

        /// <summary>
        /// The current Windows IPrincipal
        /// </summary>
        public IPrincipal WebPrincipal = null;

        /// <summary>
        /// The current Windows Identity
        /// </summary>
        public WindowsIdentity Identity = null;

        /// <summary>
        /// The current UserPrincipal if connected with the AD
        /// </summary>
        public UserPrincipal UserPrincipal = null;

        /// <summary>
        /// True if the user is authenticated and part of a group
        /// </summary>
        public bool IsAuthenticated
        {
            get { return string.IsNullOrEmpty(SecurityCode) && SecurityGroups.Count > 0; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SecurityUser(SealSecurity security)
        {
            Security = security;
        }

        /// <summary>
        /// Save the user profile
        /// </summary>
        public bool SaveProfile()
        {
            try
            {
                if (DefaultGroup.EditProfile) Profile.SaveToFile();
            }
            catch (Exception ex)
            {
                Helper.WriteLogException("SaveProfile", ex);
                return false;
            }

            return true;
        }

        private PersonalFolderRight? _persFolderRight = null;
        /// <summary>
        /// Right for the personal folder
        /// </summary>
        public PersonalFolderRight PersonalFolderRight
        {
            get
            {
                if (_persFolderRight == null)
                {
                    foreach (var group in SecurityGroups)
                    {
                        if (_persFolderRight == null || _persFolderRight < group.PersFolderRight) _persFolderRight = group.PersFolderRight;
                    }
                    if (_persFolderRight == null) _persFolderRight = PersonalFolderRight.None;
                }
                return _persFolderRight.Value;
            }
        }

        /// <summary>
        /// True if the user can download the report definitions stored in his personal folder.
        /// The most permissive setting among the groups granting a personal folder applies.
        /// </summary>
        public bool PersonalFolderReportDownload
        {
            get { return SecurityGroups.Any(g => g.PersFolderRight != PersonalFolderRight.None && g.PersonalFolderReportDownload); }
        }

        /// <summary>
        /// True if the user can upload files and reports into his personal folder.
        /// The most permissive setting among the groups granting a personal folder applies.
        /// </summary>
        public bool PersonalFolderUpload
        {
            get { return SecurityGroups.Any(g => g.PersFolderRight != PersonalFolderRight.None && g.PersonalFolderUpload); }
        }

        /// <summary>
        /// True if the user has the right to edit SQL models and to query the database (raw SQL) with the AI Tools. True if at least one of the user's groups enables SQL Models.
        /// </summary>
        public bool SqlModel
        {
            get
            {
                return SecurityGroups.Any(group => group.SqlModel);
            }
        }

        /// <summary>
        /// If true, folders view is shown
        /// </summary>
        public bool ShowFoldersView
        {
            get
            {
                return SecurityGroups.Any(group => group.ShowFoldersView);
            }
        }

        /// <summary>
        /// True if folders with no right are also shown
        /// </summary>
        public bool ShowAllFolders
        {
            get
            {
                return SecurityGroups.Any(group => group.ShowAllFolders);
            }
        }

        /// <summary>
        /// True if user can edit his profile
        /// </summary>
        public bool EditProfile
        {
            get
            {
                return SecurityGroups.Any(group => group.EditProfile);
            }
        }

        /// <summary>
        /// True if user can see the detail of exceptions (error message and stack trace) in the Web Report Server.
        /// </summary>
        public bool ShowErrorDetail
        {
            get
            {
                return SecurityGroups.Any(group => group.ShowErrorDetail);
            }
        }

        bool _tryAgain = true; //Try again to get rid of Load Assemblies exceptions...
        /// <summary>
        /// Authenticate the user using the current security script
        /// </summary>
        public void Authenticate()
        {
            Error = "";
            Warning = "";
            string script = (Security.UseCustomScript && !string.IsNullOrEmpty(Security.Script) ? Security.Script : Security.ProviderScript);
            if (string.IsNullOrEmpty(script)) throw new Exception("No Security Script to execute. Check the rights to access the repository Security.xml file...");
            try
            {
                RazorHelper.CompileExecute(script, this);
            }
            catch (Exception ex)
            {
                if (_tryAgain)
                {
                    ex = null;
                    _tryAgain = false;
                    try
                    {
                        RazorHelper.CompileExecute(script, this);
                    }
                    catch (Exception ex2)
                    {
                        ex = ex2;
                    }
                }

                if (ex != null)
                {
                    SecurityGroups.Clear();
                    Error = ex.Message;
                }
            }

            if (string.IsNullOrEmpty(Error) && !string.IsNullOrEmpty(Security.TwoFAGenerationScript))
            {
                script = Security.TwoFAGenerationScript;
                try
                {
                    RazorHelper.CompileExecute(script, this);
                }
                catch (Exception ex)
                {
                    SecurityGroups.Clear();
                    Error = ex.Message;
                }
            }

            SealSecurity.AddLoggedUsed(this);
        }

        /// <summary>
        /// Logout the user
        /// </summary>
        public void Logout()
        {
            SecurityGroups.Clear();
            SealSecurity.RemoveLoggedUsed(this);
        }

        /// <summary>
        /// Default group of the user
        /// </summary>
        public SecurityGroup DefaultGroup
        {
            get
            {
                return SecurityGroups.OrderByDescending(i => i.Weight).FirstOrDefault();
            }
        }

        /// <summary>
        /// AI Agent configurations assigned to this user via the DefaultGroup AgentGUIDs.
        /// Returns an empty list if no group is set or no enabled agents are found.
        /// </summary>
        public List<AIAgentConfiguration> AgentConfigurations
        {
            get
            {
                var group = DefaultGroup;
                if (group == null) return new List<AIAgentConfiguration>();
                return group.AgentGUIDs
                    .Select(guid => Repository.Instance.AIConfiguration.AIAgents.Find(a => a.GUID == guid && a.IsEnabled))
                    .Where(a => a != null)
                    .ToList();
            }
        }

        /// <summary>
        /// First AI Agent configuration assigned to this user via the DefaultGroup AgentGUIDs.
        /// Returns null if no group is set or no enabled agents are found.
        /// </summary>
        public AIAgentConfiguration AgentConfiguration
        {
            get { return AgentConfigurations.FirstOrDefault(); }
        }

        /// <summary>
        /// Default AI Agent configuration for this user. Resolution order: the last agent the user
        /// selected (<see cref="SecurityUserProfile.LastAgentGUID"/>), then the server's
        /// <see cref="Seal.AI.AIServerConfiguration.DefaultAgentGUID"/>, then the first assigned agent.
        /// Each candidate must still be among the agents assigned to the user.
        /// </summary>
        public AIAgentConfiguration DefaultAgentConfiguration
        {
            get
            {
                var configs = AgentConfigurations;

                var lastGuid = Profile?.LastAgentGUID;
                if (!string.IsNullOrEmpty(lastGuid))
                {
                    var last = configs.FirstOrDefault(a => a.GUID == lastGuid);
                    if (last != null) return last;
                }

                var defaultGuid = Repository.Instance.AIConfiguration.DefaultAgentGUID;
                if (!string.IsNullOrEmpty(defaultGuid))
                {
                    var match = configs.FirstOrDefault(a => a.GUID == defaultGuid);
                    if (match != null) return match;
                }

                return configs.FirstOrDefault();
            }
        }

        /// <summary>
        /// Find a security folder from a given name
        /// </summary>
        public SecurityFolder FindSecurityFolder(string folder)
        {
            if (Security == null) return null;
            return Security.FindSecurityFolder(SecurityGroups, folder);
        }

        /// <summary>
        /// Find a repository security folder from a given repository-relative path (without the Repository prefix)
        /// </summary>
        public SecurityRepositoryFolder FindRepositorySecurityFolder(string finalPath)
        {
            if (Security == null) return null;
            return Security.FindRepositorySecurityFolder(SecurityGroups, finalPath);
        }

        /// <summary>
        /// True if the AI tools can access the given data source for this user.
        /// Data sources are restricted through the DataSourceGUIDs of the user's security groups.
        /// If none of the groups defines a restriction (all lists are empty), all data sources are allowed.
        /// Otherwise the source is allowed when at least one group lists its GUID (additive rights).
        /// </summary>
        public bool CanAccessSource(MetaSource source)
        {
            if (source == null) return false;
            //A group with an empty list (or no group at all) grants access to all data sources
            if (SecurityGroups.Count == 0 || SecurityGroups.Exists(g => g.DataSourceGUIDs.Count == 0)) return true;
            return SecurityGroups.Exists(g => g.DataSourceGUIDs.Contains(source.GUID));
        }

        /// <summary>
        /// True if the AI tools can use the given output device for this user.
        /// Output devices are restricted through the OutputDeviceGUIDs of the user's security groups.
        /// If none of the groups defines a restriction (all lists are empty), all output devices are allowed.
        /// Otherwise the device is allowed when at least one group lists its GUID (additive rights).
        /// </summary>
        public bool CanAccessDevice(OutputDevice device)
        {
            if (device == null) return false;
            //A group with an empty list (or no group at all) grants access to all output devices
            if (SecurityGroups.Count == 0 || SecurityGroups.Exists(g => g.OutputDeviceGUIDs.Count == 0)) return true;
            return SecurityGroups.Exists(g => g.OutputDeviceGUIDs.Contains(device.GUID));
        }

        /// <summary>
        /// Add default security groups
        /// </summary>
        public void AddDefaultSecurityGroup()
        {
            if (Security.Groups.Count > 0)
            {
                SecurityGroups.Add(Security.Groups[0]);
            }
            else
            {
                Warning += "No security group defined in the security...\r\n";
            }
        }

        /// <summary>
        /// Add a security group from a given name
        /// </summary>
        public void AddSecurityGroup(string name)
        {
            var newGroup = Security.Groups.FirstOrDefault(i => i.Name == name);
            if (newGroup != null)
            {
                SecurityGroups.Add(newGroup);
            }
            else
            {
                Warning += "Unable to add the security group: " + name + "\r\n";
            }
        }

        /// <summary>
        /// Add security groups from the current Windows group of the logged user
        /// </summary>
        public void AddWindowsGroupToSecurityGroup(bool skipDomainName, string ADcontextType)
        {
            if (_windowsGroups == null)
            {
                _windowsGroups = !string.IsNullOrEmpty(ADcontextType) ? GetWindowsGroupsUsingAD(ADcontextType) : GetWindowsGroupsUsingWindowsIdentity();
            }

            List<string> groups = new List<string>();
            foreach (var group in _windowsGroups) groups.Add(skipDomainName && group.Contains("\\") ? Path.GetFileName(group.ToLower()) : group.ToLower());

            SecurityGroups.AddRange(Security.Groups.Where(i => groups.Contains(i.Name.ToLower())));

            if (SecurityGroups.Count == 0 && _windowsGroups.Count > 0)
            {
                Warning += "Create a security group having one of the following names:\r\n";
                foreach (var group in _windowsGroups)
                {
                    Warning += group + "\r\n";
                }
            }
        }

        List<string> _windowsGroups = null;
        bool _tryAgainWindowsIdentity = true; //Try again to get rid of Load Assemblies exceptions...
        List<string> GetWindowsGroupsUsingWindowsIdentity()
        {
            var result = new List<string>();
            try
            {
                var identity = Identity;
                if (WebPrincipal != null) identity = WebPrincipal.Identity as WindowsIdentity;
                if (identity != null)
                {
                    foreach (IdentityReference group in identity.Groups)
                    {
                        result.Add(group.Translate(typeof(NTAccount)).ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                if (_tryAgainWindowsIdentity)
                {
                    _tryAgainWindowsIdentity = false;
                    result = GetWindowsGroupsUsingWindowsIdentity();
                }
                else
                {
                    Warning += "Error getting groups using WindowsIdentity, try with the AD option...\r\n" + ex.Message + "\r\n";
                }
            }
            return result;
        }

        bool _tryAgainAD = true; //Try again to get rid of Load Assemblies exceptions...
        List<string> GetWindowsGroupsUsingAD(string contextType)
        {
            var result = new List<string>();
            try
            {
                // set up domain context
                UserPrincipal = null;
                PrincipalContext context = new PrincipalContext((ContextType)Enum.Parse(typeof(ContextType), contextType));
                string name = WebUserName;
                if (WebPrincipal != null) name = WebPrincipal.Identity.Name;
                if (Identity != null) name = Identity.Name;

                var user = new UserPrincipal(context);
                user.SamAccountName = name;
                var searcher = new PrincipalSearcher(user);
                user = searcher.FindOne() as UserPrincipal;
                if (user == null) user = UserPrincipal.FindByIdentity(context, name);

                if (user != null)
                {
                    UserPrincipal = user;
                    var groups = user.GetAuthorizationGroups();
                    // enumerate over them
                    foreach (Principal p in groups)
                    {
                        result.Add(p.Name);
                    }
                }
                else throw new Exception("Unable to find user:" + name);
            }
            catch (Exception ex)
            {
                if (_tryAgainAD)
                {
                    _tryAgainAD = false;
                    result = GetWindowsGroupsUsingAD(contextType);
                }
                else
                {
                    Warning += "Error getting groups using AD...\r\n" + ex.Message + "\r\n";
                }
            }
            return result;
        }

        /// <summary>
        /// Authenticate a login with a password and set his groups
        /// </summary>
        public bool LoginAuthentication(string id, string password)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var login = Security.Logins.FirstOrDefault(i => i.Id.ToLower() == id.ToLower());
                if (login != null)
                {
                    if (login.CheckPassword(password))
                    {
                        //Add the groups defined for the login
                        foreach (var groupId in login.GroupIds)
                        {
                            var newGroup = Security.Groups.FirstOrDefault(i => i.GUID == groupId);
                            if (newGroup != null)
                            {
                                SecurityGroups.Add(newGroup);
                            }
                        }
                        Login = login;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Summary of the authentication
        /// </summary>
        public string AuthenticationSummary
        {
            get
            {
                string message = IsAuthenticated ? "SUCCESS: User is authenticated\r\n" : "ERROR: Authentication failed\r\n";
                if (!string.IsNullOrEmpty(WebUserName)) message += string.Format("User login name: {0}\r\n", WebUserName);
                if (!string.IsNullOrEmpty(Name)) message += string.Format("User display name: {0}\r\n", Name);
                if (!string.IsNullOrEmpty(SecurityCode)) message += string.Format("Security code generated: {0}\r\n", SecurityCode);
                if (!string.IsNullOrEmpty(WebSecurityCode)) message += string.Format("Security code: {0}\r\n", WebSecurityCode);
                if (SecurityCodeTries != 0) message += string.Format("Security code tries: {0}\r\n", SecurityCodeTries);
                foreach (var group in SecurityGroups)
                {
                    message += string.Format("Security group: {0}\r\n", group.Name);
                }
                if (!string.IsNullOrEmpty(Error)) message += "\r\nError:\r\n" + Error;
                if (!string.IsNullOrEmpty(Warning)) message += "\r\nWarning:\r\n" + Warning;

                return message;
            }
        }

        /// <summary>
        /// List of security group names in a string
        /// </summary>
        public string SecurityGroupsDisplay
        {
            get
            {
                string result = "";
                foreach (var group in SecurityGroups)
                {
                    if (!string.IsNullOrEmpty(result)) result += ";";
                    result += group.Name;
                }
                return result;
            }
        }

        /// <summary>
        /// Returns true if the user belongs to a group given by a name
        /// </summary>
        public bool BelongsToGroup(string groupName)
        {
            return SecurityGroups.Exists(i => i.Name == groupName);
        }

        /// <summary>
        /// Returns the personal folder name
        /// </summary>
        public string GetPersonalFolderName()
        {
            var result = Name;
            if (!string.IsNullOrEmpty(PersonalFolderName)) result = PersonalFolderName;
            else if (!string.IsNullOrEmpty(WebUserName)) result = WebUserName;
            if (string.IsNullOrEmpty(result)) result = "";
            return result;
        }

        private string _profilePath;
        /// <summary>
        /// Profile file path
        /// </summary>
        public string ProfilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_profilePath))
                {
                    _profilePath = Path.Combine(Security.Repository.GetPersonalFolder(this), "_Profile.xml");
                }
                return _profilePath;
            }
        }
        /// <summary>
        /// Profile directory
        /// </summary>
        public string ProfileDirectory
        {
            get
            {
                return Path.GetDirectoryName(ProfilePath);
            }
        }

        /// <summary>
        /// Returns true when the given physical directory is accessible to this user
        /// with at least <paramref name="minRight"/>.
        /// Mirrors SWIMoveFile: resolves physDir to a SWIFolder path, looks it up in
        /// AllFolders, then applies the same right checks (right==0 → no access;
        /// FolderRight.Edit required for write operations).
        /// </summary>
        public bool HasFolderRight(string physDir, int minRight)
        {
            var repo = Repository.Instance;
            string swiPath;
            if (physDir.StartsWith(repo.ReportsFolder, StringComparison.OrdinalIgnoreCase))
            {
                swiPath = string.Equals(physDir, repo.ReportsFolder, StringComparison.OrdinalIgnoreCase)
                    ? Path.DirectorySeparatorChar.ToString()
                    : physDir.Substring(repo.ReportsFolder.Length);
            }
            else if (physDir.StartsWith(repo.PersonalFolder, StringComparison.OrdinalIgnoreCase))
            {
                if (PersonalFolderRight == PersonalFolderRight.None) return false;
                var pud = repo.GetPersonalFolder(this);
                if (!physDir.StartsWith(pud, StringComparison.OrdinalIgnoreCase)) return false;
                swiPath = string.Equals(physDir, pud, StringComparison.OrdinalIgnoreCase)
                    ? SWIFolder.GetPersonalRoot()
                    : SWIFolder.GetPersonalRoot() + physDir.Substring(pud.Length);
            }
            else if (physDir.StartsWith(repo.RepositoryPath, StringComparison.OrdinalIgnoreCase))
            {
                //Repository folder (Danger Zone) - rooted at the repository, outside the Reports/Personal trees
                swiPath = string.Equals(physDir, repo.RepositoryPath, StringComparison.OrdinalIgnoreCase)
                    ? SWIFolder.GetRepositoryRoot()
                    : SWIFolder.GetRepositoryRoot() + physDir.Substring(repo.RepositoryPath.Length);
            }
            else return false; // unknown root – deny

            var folder = AllFolders.FirstOrDefault(f => f.path == swiPath);
            if (folder == null || folder.right == 0) return false;
            if (minRight >= (int)FolderRight.Edit) return (FolderRight)folder.right == FolderRight.Edit;
            return true;
        }

    }
}


