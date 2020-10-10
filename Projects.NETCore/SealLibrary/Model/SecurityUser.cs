//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Web;
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
        /// Current folder detail of the user (used for Folder Detail Scripts)
        /// </summary>
        public SWIFolderDetail FolderDetail = new SWIFolderDetail();

        /// <summary>
        /// Current files for a folder detail (used for Folder Detail Scripts)
        /// </summary>
        public List<SWIFile> FolderDetailFiles;

        /// <summary>
        /// Current script execution number when several Folders or Folder Detail scripts are executed.
        /// </summary>
        public int ScriptNumber = 0;

        /// <summary>
        /// List of all SWIFolderDetail of the user. The list is built when the user browse the folders.
        /// </summary>
        public List<SWIFolderDetail> FolderDetails = new List<SWIFolderDetail>();

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
        /// Current SessionID
        /// </summary>
        public string SessionID = "";

        /// <summary>
        /// Parameters for authentication: Token
        /// </summary>
        public string Token = null;

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
            get { return SecurityGroups.Count > 0; }
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
        public void SaveProfile()
        {
            //Clean ids not published anymore
            Profile.Dashboards.RemoveAll(i => !GetDashboards().Exists(j => j.GUID == i));
            Profile.SaveToFile();
        }

        /// <summary>
        /// Clear the current cache for Dashboards and Widgets 
        /// </summary>
        public void ClearCache()
        {
            //reset pointers of objects having translations
            Dashboards = null;
            _widgets = null;
        }

        private List<SecurityDashboardFolder> _securityDashboardFolders = null;
        /// <summary>
        /// List of folder for dashboard publication
        /// </summary>
        public List<SecurityDashboardFolder> SecurityDashboardFolders
        {
            get
            {
                if (_securityDashboardFolders == null)
                {
                    _securityDashboardFolders = new List<SecurityDashboardFolder>();
                    foreach (var sgroup in SecurityGroups)
                    {
                        foreach (var sdf in sgroup.DashboardFolders)
                        {
                            var current = _securityDashboardFolders.FirstOrDefault(i => i.Name == sdf.Name);
                            if (current == null)
                            {
                                _securityDashboardFolders.Add(sdf);
                                current = sdf;                           
                            }

                            //Set highest right
                            if (current.Right < sdf.Right) current.Right = sdf.Right;
                        }
                    }
                }
                return _securityDashboardFolders;
            }
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

        private bool? _sqlModel = null;
        /// <summary>
        /// True if the user has right to edit SQL models
        /// </summary>
        public bool SqlModel
        {
            get
            {
                if (_sqlModel == null)
                {
                    _sqlModel = false;
                    foreach (var group in SecurityGroups)
                    {
                        if (group.SqlModel) _sqlModel = true;
                    }
                }
                return _sqlModel.Value;
            }
        }

        private bool? _widgetPublication = null;
        /// <summary>
        /// True if the user has right to publish Dashboard Widgets
        /// </summary>
        public bool WidgetPublication
        {
            get
            {
                if (_widgetPublication == null)
                {
                    _widgetPublication = false;
                    foreach (var group in SecurityGroups)
                    {
                        if (group.WidgetPublication) _widgetPublication = true;
                    }
                }
                return _widgetPublication.Value;
            }
        }

        private ViewType? _viewType = null;
        /// <summary>
        /// Views allowed for the user: reports and/or dashboards
        /// </summary>
        public ViewType ViewType
        {
            get
            {
                if (_viewType == null)
                {
                    foreach (var group in SecurityGroups)
                    {
                        if (_viewType == null || _viewType < group.ViewType) _viewType = group.ViewType;
                    }

                    if (_viewType == null) _viewType = ViewType.Reports;
                }
                return _viewType.Value;
            }
        }

        private bool? _viewDashboardsFirst = null;
        /// <summary>
        /// If true, Dashboards are shown first when the user login
        /// </summary>
        public bool ViewDashboardsFirst
        {
            get
            {
                if (_viewDashboardsFirst == null)
                {
                    _viewDashboardsFirst = false;
                    foreach (var group in SecurityGroups)
                    {
                        if (group.ViewDashboardsFirst)
                        {
                            _viewDashboardsFirst = true;
                            break;
                        }
                    }
                }
                return _viewDashboardsFirst.Value;
            }
        }

        private bool? _showAllFolder = null;
        /// <summary>
        /// True if folders with no right are also shown
        /// </summary>
        public bool ShowAllFolders
        {
            get
            {
                if (_showAllFolder == null)
                {
                    _showAllFolder = false;
                    foreach (var group in SecurityGroups)
                    {
                        if (group.ShowAllFolders) _showAllFolder = true;
                    }
                }
                return _showAllFolder.Value;
            }
        }

        List<SWIDashboardFolder> _dashboardFolders;
        /// <summary>
        /// List of SWIDashboardFolder for the Web Report Server
        /// </summary>
        public List<SWIDashboardFolder> DashboardFolders
        {
            get
            {
                if (_dashboardFolders == null)
                {
                    _dashboardFolders = new List<SWIDashboardFolder>();
                    if (HasPersonalDashboardFolder)
                    {
                        _dashboardFolders.Add(new SWIDashboardFolder() { name = Security.Repository.TranslateWeb("Personal"), path = SWIDashboardFolder.PersonalPath });
                    }

                    //public
                    foreach (var f in SecurityDashboardFolders.Where(i => i.Right == DashboardFolderRight.Edit))
                    {
                        _dashboardFolders.Add(new SWIDashboardFolder() { name = Security.Repository.TranslateDashboardFolder(Path.DirectorySeparatorChar.ToString() + f.FolderPath, f.Name), path = f.FolderPath });
                    }
                }
                return _dashboardFolders;
            }
        }

        private bool? _manageDashboards = null;
        /// <summary>
        /// True if the user can manage dashboards
        /// </summary>
        public bool ManageDashboards
        {
            get
            {
                if (_manageDashboards == null)
                {
                    _manageDashboards = false;
                    foreach (var group in SecurityGroups)
                    {
                        if (group.ManageDashboards) _manageDashboards = true;
                    }
                }
                return _manageDashboards.Value;
            }
        }

        private List<SecurityDashboardOrder> _defaultDashboards = null;
        /// <summary>
        /// True if the usercan manage dashboards
        /// </summary>
        public List<SecurityDashboardOrder> DefaultDashboardOrders
        {
            get
            {
                if (_defaultDashboards == null)
                {
                    var allOrders = new List<SecurityDashboardOrder>();
                    foreach (var group in SecurityGroups)
                    {
                        foreach (var dOrder in group.DefaultDashboards)
                        {
                            allOrders.Add(dOrder);
                        }
                    }
                    //Unique and sort 
                    _defaultDashboards = new List<SecurityDashboardOrder>();
                    foreach (var dOrder in allOrders.OrderBy(i => i.Order))
                    {
                        if (!_defaultDashboards.Exists(i => i.GUID == dOrder.GUID)) _defaultDashboards.Add(dOrder);
                    }
                }
                return _defaultDashboards;
            }
        }


        private bool? _hasPersonalDashboardFolder = null;
        /// <summary>
        /// True if the user has a personal folder for dashboards
        /// </summary>
        public bool HasPersonalDashboardFolder
        {
            get
            {
                if (_hasPersonalDashboardFolder == null)
                {
                    _hasPersonalDashboardFolder = false;
                    foreach (var group in SecurityGroups)
                    {
                        if (group.PersonalDashboardFolder) _hasPersonalDashboardFolder = true;
                    }
                }
                return _hasPersonalDashboardFolder.Value;
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
        /// Set the default user's culture
        /// </summary>
        public void SetDefaultCulture(string culture)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                Security.Repository.SetCultureInfo(culture);
            }
        }

        /// <summary>
        /// Set the default logo name
        /// </summary>
        public void SetDefaultLogoName(string logoName)
        {
            if (!string.IsNullOrEmpty(logoName))
            {
                Security.Repository.Configuration.LogoName = logoName;
            }
        }

        /// <summary>
        /// Set defaults from a security group 
        /// </summary>
        public void SetGroupConfiguration(SecurityGroup group)
        {
            //set defaults from the group
            SetDefaultCulture(group.Culture);
            SetDefaultLogoName(group.LogoName);
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


        private List<SecurityWidget> _securityWidgets = null;
        /// <summary>
        /// List of widgets that cannot be edited with the Web Report Designer
        /// </summary>
        public List<SecurityWidget> ForbiddenWidgets
        {
            get
            {
                if (_securityWidgets == null) InitEditionRights();
                return _securityWidgets;
            }
        }

        /// <summary>
        /// True if the widget can be selected 
        /// </summary>
        public bool CanSelectWidget(DashboardWidget item)
        {
            return !ForbiddenWidgets.Exists(i =>
                (string.IsNullOrEmpty(i.ReportName) || i.ReportName == item.ReportName) &&
                (string.IsNullOrEmpty(i.Tag) || i.Tag == item.Tag) &&
                (string.IsNullOrEmpty(i.Name) || i.Name == item.Name)
                );
        }

        private void InitEditionRights()
        {
            _securityConnections = new List<SecurityConnection>();
            _securityDevices = new List<SecurityDevice>();
            _securitySources = new List<SecuritySource>();
            _securityColumns = new List<SecurityColumn>();
            _securityWidgets = new List<SecurityWidget>();

            foreach (var sgroup in SecurityGroups)
            {
                _securityConnections.AddRange(sgroup.Connections.Where(i => i.Right == EditorRight.NoSelection));
                _securityDevices.AddRange(sgroup.Devices.Where(i => i.Right == EditorRight.NoSelection));
                _securitySources.AddRange(sgroup.Sources.Where(i => i.Right == EditorRight.NoSelection));
                _securityColumns.AddRange(sgroup.Columns.Where(i => i.Right == EditorRight.NoSelection));
                _securityWidgets.AddRange(sgroup.Widgets.Where(i => i.Right == EditorRight.NoSelection));
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
                SetGroupConfiguration(Security.Groups[0]);
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
                SetGroupConfiguration(newGroup);
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
            foreach (var group in _windowsGroups) groups.Add(skipDomainName ? Path.GetFileName(group.ToLower()) : group.ToLower());

            SecurityGroups.AddRange(Security.Groups.Where(i => groups.Contains(i.Name.ToLower())));

            if (SecurityGroups.Count == 0 && _windowsGroups.Count > 0)
            {
                Warning += "Create a security group having one of the following names:\r\n";
                foreach (var group in _windowsGroups)
                {
                    Warning += group + "\r\n";
                }
            }
            else if (SecurityGroups.Count > 0)
            {
                SetGroupConfiguration(SecurityGroups[0]);
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
                    Warning += "Error getting groups using WindowsIdentity, try perhaps with the AD option...\r\n" + ex.Message + "\r\n";
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
        /// Summary of the authentication
        /// </summary>
        public string AuthenticationSummary
        {
            get
            {
                string message = IsAuthenticated ? "SUCCESS: User is authenticated\r\n" : "ERROR: Authentication failed\r\n";
                if (!string.IsNullOrEmpty(WebUserName)) message += string.Format("User login name: {0}\r\n", WebUserName);
                if (!string.IsNullOrEmpty(Name)) message += string.Format("User display name: {0}\r\n", Name);
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
        /// List of default dashboards names in a string
        /// </summary>
        public string DefaultDashboardsViewDisplay
        {
            get
            {
                string result = "";
                var dashboards = new List<Dashboard>();
                foreach (var group in SecurityGroups)
                {
                    dashboards.AddRange(group.Dashboards);
                }

                foreach (var dOrder in DefaultDashboardOrders.OrderBy(i => i.Order))
                {
                    var d = dashboards.FirstOrDefault(i => i.GUID == dOrder.GUID);
                    if (d != null)
                    {
                        if (!string.IsNullOrEmpty(result)) result += ";";
                        result += d.Name;
                    }
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


        #region Dashboard
        private Dictionary<string, DashboardWidget> _widgets = null;
        /// <summary>
        /// List of widgets available
        /// </summary>
        public Dictionary<string, DashboardWidget> Widgets
        {
            get
            {
                if (_widgets == null || _widgets.Count == 0)
                {
                    _widgets = new Dictionary<string, DashboardWidget>();
                    foreach (var widget in DashboardWidgetsPool.Widgets.Values.OrderBy(i => i.Name))
                    {
                        if (CanSelectWidget(widget))
                        {
                            DashboardWidget newWidget = (DashboardWidget) Helper.Clone(widget);
                            newWidget.ReportName = widget.ReportName;
                            newWidget.ReportPath = widget.ReportPath;
                            _widgets.Add(newWidget.GUID, newWidget);
                            var instance = newWidget.ReportPath.Replace(Security.Repository.ReportsFolder, Path.DirectorySeparatorChar.ToString());
                            newWidget.Name = Security.Repository.TranslateWidgetName(instance, newWidget.Name);
                            newWidget.Description = Security.Repository.TranslateWidgetDescription(instance, newWidget.Description);
                        }
                    }
                }
                return _widgets;
            }
        }

        /// <summary>
        /// Load a personal dashboard and its widgets
        /// </summary>
        public void LoadPersonalDashboard(string path)
        {
            loadDashboard(path, SWIDashboardFolder.PersonalPath, "", true, true);
        }

        /// <summary>
        /// Load a public dashboard and its widgets
        /// </summary>
        public void LoadPublicDashboard(string path, string folderPath, string folderName, bool editable)
        {
            loadDashboard(path, folderPath, folderName, editable, false);
        }

        void loadDashboard(string path, string folderPath, string folderName, bool editable, bool isPersonal)
        {
            try
            {
                var dashboard = Dashboards.FirstOrDefault(i => i.Path == path);
                if (dashboard == null || dashboard.LastModification != File.GetLastWriteTime(path))
                {
                    if (dashboard != null) Dashboards.Remove(dashboard);
                    dashboard = Dashboard.LoadFromFile(path);
                    Dashboards.Add(dashboard);
                }
                dashboard.IsPersonal = isPersonal;
                dashboard.Editable = editable;
                dashboard.Folder = folderPath;

                dashboard.DisplayName = Security.Repository.TranslateDashboardName(path.Replace(Security.Repository.DashboardPublicFolder, ""), dashboard.Name);
                var repositoryPath = path.Replace(Security.Repository.DashboardPublicFolder, "");

                if (dashboard.IsPersonal)
                {
                    dashboard.FullName = string.Format("{0} ({1})", dashboard.DisplayName, Security.Repository.TranslateWeb("Personal"));
                }
                else if (!string.IsNullOrEmpty(folderName))
                {
                    dashboard.FullName = string.Format("{0} ({1})", dashboard.DisplayName, Security.Repository.TranslateDashboardFolder(Path.GetDirectoryName(repositoryPath), folderName));
                }
                else
                {
                    dashboard.FullName = dashboard.DisplayName;
                }
                dashboard.ReinitGroupOrders();
                //Init items and translate labels
                foreach (var item in dashboard.Items)
                {
                    var widget = DashboardWidgetsPool.Widgets.ContainsKey(item.WidgetGUID) ? DashboardWidgetsPool.Widgets[item.WidgetGUID] : null;
                    if (widget == null) continue;
                    if (!string.IsNullOrEmpty(item.Name) && item.Name != widget.Name)
                    {
                        item.DisplayName = Security.Repository.TranslateDashboardItemName(repositoryPath, item.Name);
                    }
                    else
                    {
                        var instance = widget.ReportPath.Replace(Security.Repository.ReportsFolder, Path.DirectorySeparatorChar.ToString());
                        item.DisplayName = Security.Repository.TranslateWidgetName(instance, widget.Name);
                    }
                    item.SetWidget(widget);

                    if (!string.IsNullOrEmpty(item.GroupName)) item.DisplayGroupName = Security.Repository.TranslateDashboardItemGroupName(repositoryPath, item.GroupName);
                }
                //Remove lost widgets...
                dashboard.Items.RemoveAll(i => i.Widget == null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private string _dashboardPersonalFolder;
        /// <summary>
        /// Path of the dashboard personal folder
        /// </summary>
        public string DashboardPersonalFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_dashboardPersonalFolder))
                {
                    _dashboardPersonalFolder = Path.Combine(Security.Repository.GetPersonalFolder(this), "_Dashboards");
                    if (!Directory.Exists(_dashboardPersonalFolder)) Directory.CreateDirectory(_dashboardPersonalFolder);
                }
                return _dashboardPersonalFolder;
            }
        }

        /// <summary>
        /// List of dashboards viewed by the user
        /// </summary>
        public List<Dashboard> UserDashboards
        {
            get
            {
                var result = new List<Dashboard>();
                var dashboards = GetDashboards();

                var orders = DefaultDashboardOrders.ToList();
                if (ManageDashboards && !Profile.ViewDefaultDashboards)
                {
                    //Get dashboard from profile
                    int order = 1;
                    orders.Clear();
                    foreach (var guid in Profile.Dashboards)
                    {
                        orders.Add(new SecurityDashboardOrder() { GUID = guid, Order = order++ }); 
                    }
                }

                foreach (var dOrder in orders.OrderBy(i => i.Order))
                {
                    var d = dashboards.FirstOrDefault(i => i.GUID == dOrder.GUID);
                    if (d != null)
                    {
                        d.Order = dOrder.Order;
                        if (result.FirstOrDefault(i => i.FullName == d.FullName) != null) d.FullName += " [" + Path.GetFileNameWithoutExtension(d.Path) + "]";
                        result.Add(d);
                    }
                }

                return result;
            }
        }

        public List<Dashboard> Dashboards = null;
        /// <summary>
        /// Load all dashboards for the user
        /// </summary>
        public List<Dashboard> GetDashboards()
        {
            try
            {
                if (Dashboards == null) Dashboards = new List<Dashboard>();
                //personal
                if (HasPersonalDashboardFolder)
                {
                    foreach (var p in Directory.GetFiles(DashboardPersonalFolder, "*." + Repository.SealDashboardExtension))
                    {
                        LoadPersonalDashboard(p);
                    }
                }

                //public
                foreach (var f in SecurityDashboardFolders. Where(i => i.Right != DashboardFolderRight.None))
                {
                    var dir = Path.Combine(Security.Repository.DashboardPublicFolder, FileHelper.CleanFilePath(f.Name));
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    foreach (var p in Directory.GetFiles(dir, "*." + Repository.SealDashboardExtension))
                    {
                        LoadPublicDashboard(p, f.FolderPath, f.Name, f.Right == DashboardFolderRight.Edit);
                    }
                }

                //remove deleted
                Dashboards.RemoveAll(i => !File.Exists(i.Path));

                //Dashboards Script
                ScriptNumber = 1;
                foreach (var group in SecurityGroups.Where(i => !string.IsNullOrEmpty(i.DashboardsScript)).OrderBy(i => i.Name))
                {
                    RazorHelper.CompileExecute(group.DashboardsScript, this);
                    ScriptNumber++;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return Dashboards;
        }
        #endregion

    }
}

