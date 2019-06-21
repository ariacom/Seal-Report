//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
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

namespace Seal.Model
{
    public class SecurityUser
    {
        public string Name = "";
        public string PersonalFolderName = "";
        public List<SecurityGroup> SecurityGroups = new List<SecurityGroup>();
        public SealSecurity Security;
        public string Error = "";
        public string Warning = "";
        public List<SWIFolder> Folders = new List<SWIFolder>();
        public SecurityUserProfile Profile = new SecurityUserProfile();

        //Parameters to authenticate
        public string WebUserName = "";
        public string WebPassword = "";
        public IPrincipal WebPrincipal = null;
        public WindowsIdentity Identity = null;

        public bool IsAuthenticated
        {
            get { return SecurityGroups.Count > 0; }
        }

        public SecurityUser(SealSecurity security)
        {
            Security = security;
        }

        public void SaveProfile()
        {
            //Clean ids not published anymore
            Profile.Dashboards.RemoveAll(i => !GetDashboards().Exists(j => j.GUID == i));
            Profile.SaveToFile();
        }

        public void ClearCache()
        {
            //reset pointers of objects having translations
            _dashboards = null;
            _widgets = null;
        }

        private List<SecurityDashboardFolder> _securityDashboardFolders = null;
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
                }
                return _persFolderRight.Value;
            }
        }

        private bool? _sqlModel = null;
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

        private ViewType? _viewType = null;
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
                }
                return _viewType.Value;
            }
        }

        List<SWIDashboardFolder> _dashboardFolders;
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
                        _dashboardFolders.Add(new SWIDashboardFolder() { name = Security.Repository.TranslateDashboardFolder("\\" + f.FolderPath, f.Name), path = f.FolderPath });
                    }
                }
                return _dashboardFolders;
            }
        }

        private bool? _manageDashboards = null;
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

        private bool? _hasPersonalDashboardFolder = null;
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
        public void Authenticate()
        {
            Error = "";
            Warning = "";
            string script = (Security.UseCustomScript && !string.IsNullOrEmpty(Security.Script) ? Security.Script : Security.ProviderScript);
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

        public void Logout()
        {
            SecurityGroups.Clear();
            SealSecurity.RemoveLoggedUsed(this);
        }


        public void SetDefaultCulture(string culture)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                Security.Repository.SetCultureInfo(culture);
            }
        }

        public void SetDefaultLogoName(string logoName)
        {
            if (!string.IsNullOrEmpty(logoName))
            {
                Security.Repository.Configuration.LogoName = logoName;
            }
        }

        public void SetGroupConfiguration(SecurityGroup group)
        {
            //set defaults from the group
            SetDefaultCulture(group.Culture);
            SetDefaultLogoName(group.LogoName);
        }

        public SecurityFolder FindSecurityFolder(string folder)
        {
            if (Security == null) return null;
            return Security.FindSecurityFolder(SecurityGroups, folder);
        }


        private List<SecurityColumn> _securityColumns = null;
        public List<SecurityColumn> ForbiddenColumns
        {
            get
            {
                if (_securityColumns == null) InitEditionRights();
                return _securityColumns;
            }
        }

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
        public List<SecuritySource> ForbiddenSources
        {
            get
            {
                if (_securitySources == null) InitEditionRights();
                return _securitySources;
            }
        }

        public bool CanSelectSource(MetaSource item)
        {
            var sourceName = item.Name;
            if (item is ReportSource) sourceName = ((ReportSource)item).MetaSourceName;
            return !ForbiddenSources.Exists(i =>
                (string.IsNullOrEmpty(i.Name) || i.Name == sourceName)
                );
        }


        private List<SecurityDevice> _securityDevices = null;
        public List<SecurityDevice> ForbiddenDevices
        {
            get
            {
                if (_securityDevices == null) InitEditionRights();
                return _securityDevices;
            }
        }

        public bool CanSelectDevice(OutputDevice item)
        {
            return !ForbiddenDevices.Exists(i =>
                (string.IsNullOrEmpty(i.Name) || i.Name == item.Name)
                );
        }


        private List<SecurityConnection> _securityConnections = null;
        public List<SecurityConnection> ForbiddenConnections
        {
            get
            {
                if (_securityConnections == null) InitEditionRights();
                return _securityConnections;
            }
        }

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
        public List<SecurityWidget> ForbiddenWidgets
        {
            get
            {
                if (_securityWidgets == null) InitEditionRights();
                return _securityWidgets;
            }
        }

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
                        result.Add(group.Translate(typeof(NTAccount)).ToString().ToLower());
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
                    // find the roles....
                    var roles = user.GetGroups();
                    // enumerate over them
                    foreach (Principal p in roles)
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

        public bool BelongsToGroup(string groupName)
        {
            return SecurityGroups.Exists(i => i.Name == groupName);
        }

        public string GetPersonalFolderName()
        {
            var result = Name;
            if (!string.IsNullOrEmpty(PersonalFolderName)) result = PersonalFolderName;
            else if (!string.IsNullOrEmpty(WebUserName)) result = WebUserName;
            return result;
        }

        private string _profilePath;
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
                            var instance = newWidget.ReportPath.Replace(Security.Repository.ReportsFolder, "\\");
                            newWidget.Name = Security.Repository.TranslateWidgetName(instance, newWidget.Name);
                            newWidget.Description = Security.Repository.TranslateWidgetDescription(instance, newWidget.Description);
                        }
                    }
                }
                return _widgets;
            }
        }


        void LoadDashboard(string path, string folderPath, string folderName, bool editable, bool isPersonal)
        {
            try
            {
                var dashboard = _dashboards.FirstOrDefault(i => i.Path == path);
                if (dashboard == null || dashboard.LastModification != File.GetLastWriteTime(path))
                {
                    if (dashboard != null) _dashboards.Remove(dashboard);
                    dashboard = Dashboard.LoadFromFile(path);
                    _dashboards.Add(dashboard);
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
                        var instance = widget.ReportPath.Replace(Security.Repository.ReportsFolder, "\\");
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

        public List<Dashboard> UserDashboards
        {
            get
            {
                var result = new List<Dashboard>();
                var dashboards = GetDashboards();
                int order = 1;
                foreach (var guid in Profile.Dashboards)
                {
                    var d = dashboards.FirstOrDefault(i => i.GUID == guid);
                    if (d != null)
                    {
                        d.Order = order++;
                        if (result.FirstOrDefault(i => i.FullName == d.FullName) != null) d.FullName += " [" + Path.GetFileNameWithoutExtension(d.Path) + "]";
                        result.Add(d);
                    }
                }
                return result;
            }
        }

        private List<Dashboard> _dashboards = null;
        public List<Dashboard> GetDashboards()
        {
            try
            {
                if (_dashboards == null) _dashboards = new List<Dashboard>();
                //personal
                if (HasPersonalDashboardFolder)
                {
                    foreach (var p in Directory.GetFiles(DashboardPersonalFolder, "*." + Repository.SealDashboardExtension))
                    {
                        LoadDashboard(p, SWIDashboardFolder.PersonalPath, "", true, true);
                    }
                }

                //public
                foreach (var f in SecurityDashboardFolders. Where(i => i.Right != DashboardFolderRight.None))
                {
                    var dir = Path.Combine(Security.Repository.DashboardPublicFolder, FileHelper.CleanFilePath(f.Name));
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    foreach (var p in Directory.GetFiles(dir, "*." + Repository.SealDashboardExtension))
                    {
                        LoadDashboard(p, f.FolderPath, f.Name, f.Right == DashboardFolderRight.Edit, false);
                    }
                }

                //remove deleted
                _dashboards.RemoveAll(i => !File.Exists(i.Path));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return _dashboards;
        }
        #endregion

    }
}
