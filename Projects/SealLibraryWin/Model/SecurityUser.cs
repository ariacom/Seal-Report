﻿//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
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
        /// Custom string got in user profile
        /// </summary>
        public string Tag;

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
            catch(Exception ex)
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

        private DownloadUpload? _downloadUpload = null;
        /// <summary>
        /// Right for the personal folder
        /// </summary>
        public DownloadUpload DownloadUploadRight
        {
            get
            {
                if (_downloadUpload == null)
                {
                    foreach (var group in SecurityGroups)
                    {
                        if (_downloadUpload == null || _downloadUpload < group.DownloadUpload) _downloadUpload = group.DownloadUpload;
                    }
                    if (_downloadUpload == null) _downloadUpload = DownloadUpload.None;
                }
                return _downloadUpload.Value;
            }
        }

        /// <summary>
        /// True if the user has right to edit SQL models
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
        /// True if user can edit the configuration (is administrator)
        /// </summary>
        public bool EditConfiguration
        {
            get
            {
                return SecurityGroups.Any(group => group.EditConfiguration);
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
        /// Find a security folder from a given name
        /// </summary>
        public SecurityFolder FindSecurityFolder(string folder)
        {
            if (Security == null) return null;
            return Security.FindSecurityFolder(SecurityGroups, folder);
        }

        private List<SecurityColumn> _securityColumns = null;
        /// <summary>
        /// List of columns that cannot be edited with the Web Report Designer
        /// </summary>
        public List<SecurityColumn> ForbiddenColumns
        {
            get
            {
                if (_securityColumns == null) InitEditionRights();
                return _securityColumns;
            }
        }

        /// <summary>
        /// True if the column can be selected 
        /// </summary>
        public bool CanSelectColumn(MetaColumn column)
        {
            var sourceName = column.Source.Name;
            if (column.Source is ReportSource) sourceName = ((ReportSource)column.Source).MetaSourceName;
            return !ForbiddenColumns.Exists(i =>
                (string.IsNullOrEmpty(i.Source) || i.Source == sourceName) &&
                (string.IsNullOrEmpty(i.Tag) || i.Tag == column.Tag) &&
                (string.IsNullOrEmpty(i.Category) || i.Category == column.Category)
                );
        }


        private List<SecuritySource> _securitySources = null;
        /// <summary>
        /// List of data source that cannot be edited with the Web Report Designer
        /// </summary>
        public List<SecuritySource> ForbiddenSources
        {
            get
            {
                if (_securitySources == null) InitEditionRights();
                return _securitySources;
            }
        }

        /// <summary>
        /// True if the data source can be selected 
        /// </summary>
        public bool CanSelectSource(MetaSource item)
        {
            var sourceName = item.Name;
            if (item is ReportSource) sourceName = ((ReportSource)item).MetaSourceName;
            return !ForbiddenSources.Exists(i =>
                (string.IsNullOrEmpty(i.Name) || i.Name == sourceName)
                );
        }


        private List<SecurityDevice> _securityDevices = null;
        /// <summary>
        /// List of devices that cannot be edited with the Web Report Designer
        /// </summary>
        public List<SecurityDevice> ForbiddenDevices
        {
            get
            {
                if (_securityDevices == null) InitEditionRights();
                return _securityDevices;
            }
        }

        /// <summary>
        /// True if the device can be selected 
        /// </summary>
        public bool CanSelectDevice(OutputDevice item)
        {
            return !ForbiddenDevices.Exists(i =>
                (string.IsNullOrEmpty(i.Name) || i.Name == item.Name)
                );
        }


        private List<SecurityConnection> _securityConnections = null;
        /// <summary>
        /// List of connections that cannot be edited with the Web Report Designer
        /// </summary>
        public List<SecurityConnection> ForbiddenConnections
        {
            get
            {
                if (_securityConnections == null) InitEditionRights();
                return _securityConnections;
            }
        }

        /// <summary>
        /// True if the connection can be selected 
        /// </summary>
        public bool CanSelectConnection(MetaConnection item)
        {
            var sourceName = item.Source.Name;
            if (item.Source is ReportSource) sourceName = ((ReportSource)item.Source).MetaSourceName;
            return !ForbiddenConnections.Exists(i =>
                (string.IsNullOrEmpty(i.Source) || i.Source == sourceName) &&
                (string.IsNullOrEmpty(i.Name) || i.Name == item.Name)
                );
        }



        private void InitEditionRights()
        {
            _securityConnections = new List<SecurityConnection>();
            _securityDevices = new List<SecurityDevice>();
            _securitySources = new List<SecuritySource>();
            _securityColumns = new List<SecurityColumn>();

            foreach (var sgroup in SecurityGroups)
            {
                _securityConnections.AddRange(sgroup.Connections.Where(i => i.Right == EditorRight.NoSelection));
                _securityDevices.AddRange(sgroup.Devices.Where(i => i.Right == EditorRight.NoSelection));
                _securitySources.AddRange(sgroup.Sources.Where(i => i.Right == EditorRight.NoSelection));
                _securityColumns.AddRange(sgroup.Columns.Where(i => i.Right == EditorRight.NoSelection));
            }
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
        /// List reports menu
        /// </summary>
        public List<ReportView> GetMenuReportViews()
        {
            var result = new List<ReportView>();
            foreach (var view in MenuReportViewsPool.MenuReportViews)
            {
                bool addIt = false;
                if (view.Report.FilePath.StartsWith(Security.Repository.GetPersonalFolder(this)) && PersonalFolderRight != PersonalFolderRight.None)
                {
                    addIt = true;
                }
                else
                {
                    //Public folders
                    var folder = FindSecurityFolder(Path.GetDirectoryName(FileHelper.ConvertOSFilePath(view.Report.RelativeFilePath)));
                    if (folder != null && folder.FolderRight != FolderRight.None) addIt = true;
                }

                if (addIt) result.Add(view);                
            }
            return result;
        }
    }
}


