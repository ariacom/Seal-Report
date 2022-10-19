//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;
#if WINDOWS
using DynamicTypeDescriptor;
using Seal.Forms;
using System.Drawing.Design;
#endif

namespace Seal.Model
{
    /// <summary>
    /// A SecurityGroup defines all the security applied to a user belonging to the group
    /// </summary>
    public class SecurityGroup : RootEditor
    {
#if WINDOWS
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Name").SetIsBrowsable(true);
                GetProperty("Folders").SetIsBrowsable(true);
                GetProperty("FoldersScript").SetIsBrowsable(true);
                GetProperty("FolderDetailScript").SetIsBrowsable(true);
                GetProperty("MenuScript").SetIsBrowsable(true);
                GetProperty("Columns").SetIsBrowsable(true);
                GetProperty("SqlModel").SetIsBrowsable(true);
                GetProperty("Devices").SetIsBrowsable(true);
                GetProperty("Connections").SetIsBrowsable(true);
                GetProperty("Sources").SetIsBrowsable(true);

                GetProperty("OnStartup").SetIsBrowsable(true);
                GetProperty("StartupReport").SetIsBrowsable(true);
                GetProperty("StartupReportName").SetIsBrowsable(true);
                GetProperty("ExecutionMode").SetIsBrowsable(true);
                GetProperty("Weight").SetIsBrowsable(true);
                GetProperty("EditProfile").SetIsBrowsable(true);
                GetProperty("Culture").SetIsBrowsable(true);
                GetProperty("LogoName").SetIsBrowsable(true);
                GetProperty("PersFolderRight").SetIsBrowsable(true);
                GetProperty("ShowFoldersView").SetIsBrowsable(true);
                GetProperty("ShowAllFolders").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

#endif

        string _name = "Group";
        /// <summary>
        /// The security group name
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\t\t\tName"), Description("The security group name."), Id(1, 1)]
#endif
        public string Name
        {
            get { return _name; }
            set
            {
                if (_dctd != null && _name != value)
                {
                    //Update group names in Logins
                    foreach(var login in Repository.Instance.Security.Logins) {
                        for (var i =0; i <login.GroupNames.Count; i++)
                        {
                            if (login.GroupNames[i] == _name) login.GroupNames[i] = value;
                        }
                    }
                }
                _name = value;
            }
        }

        /// <summary>
        /// The folder configurations for this group used for Web Publication of reports. By default, repository folders have no right.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\t\tFolders"), Description("The folder configurations for this group used for Web Publication of reports. By default, repository folders have no right."), Id(2, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<SecurityFolder> Folders { get; set; } = new List<SecurityFolder>();
        public bool ShouldSerializeFolders() { return Folders.Count > 0; }

        /// <summary>
        /// Define the right of the dedicated personal folder for each user of the group
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\t\tPersonal Folder"), Description("Define the right of the dedicated personal folder for each user of the group."), Id(4, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(PersonalFolderRight.None)]
#endif
        public PersonalFolderRight PersFolderRight { get; set; } = PersonalFolderRight.None;

        /// <summary>
        /// If true, the folders view of the reports is shown
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\t\tShow folders view"), Description("If true, the folders view of the reports is shown."), Id(5, 1)]
        [DefaultValue(true)]
#endif
        public bool ShowFoldersView { get; set; } = true;

        /// <summary>
        /// If true, parent folder with no rights are also shown in the tree view
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\t\tTree view:Show all folders"), Description("If true, parent folder with no rights are also shown in the tree view."), Id(6, 1)]
        [DefaultValue(false)]
#endif
        public bool ShowAllFolders { get; set; } = false;

        /// <summary>
        /// Optional script executed to define/modify the folders published in the Web Report Server. If the user belongs to several groups, scripts are executed sequentially sorted by group name.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tFolders Script"), Description("Optional script executed to define/modify the folders published in the Web Report Server. If the user belongs to several groups, scripts are executed sequentially sorted by group name."), Id(10, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string FoldersScript { get; set; }

        /// <summary>
        /// Optional script executed to define/modify the reports published in the Web Report Server for a given folder. If the user belongs to several groups, scripts are executed sequentially sorted by group name.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Folder Detail Script"), Description("Optional script executed to define/modify the reports published in the Web Report Server for a given folder. If the user belongs to several groups, scripts are executed sequentially sorted by group name."), Id(11, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string FolderDetailScript { get; set; }

        /// <summary>
        /// Optional script executed to define/modify the reports menu of the Web Report Server. If the user belongs to several groups, scripts are executed sequentially sorted by group name.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Menu Script"), Description("Optional script executed to define/modify the reports menu of the Web Report Server. If the user belongs to several groups, scripts are executed sequentially sorted by group name."), Id(12, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string MenuScript { get; set; }

        /// <summary>
        /// For the Web Report Designer: If true, SQL Models and Custom SQL for elements or restrictions can be edited through the Web Report Designer.
        /// </summary>
#if WINDOWS
        [Category("Web Report Designer Security"), DisplayName("\t\t\tSQL Models"), Description("For the Web Report Designer: If true, SQL Models and Custom SQL for elements or restrictions can be edited through the Web Report Designer. Note that dynamic filters set for security purpose will not be applied."), Id(1, 2)]
        [DefaultValue(false)]
#endif
        public bool SqlModel { get; set; } = false;

        /// <summary>
        /// For the Web Report Designer: Device rights for the group. Set rights to devices through their names. By default, all devices can be selected.
        /// </summary>
#if WINDOWS
        [Category("Web Report Designer Security"), DisplayName("\t\tDevices"), Description("For the Web Report Designer: Device rights for the group. Set rights to devices through their names. By default, all devices can be selected."), Id(3, 2)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<SecurityDevice> Devices { get; set; } = new List<SecurityDevice>();
        public bool ShouldSerializeDevices() { return Devices.Count > 0; }

        /// <summary>
        /// For the Web Report Designer: Data sources rights for the group. Set rights to data source through their names. By default, all sources can be selected.
        /// </summary>
#if WINDOWS
        [Category("Web Report Designer Security"), DisplayName("\t\tSources"), Description("For the Web Report Designer: Data sources rights for the group. Set rights to data source through their names. By default, all sources can be selected."), Id(4, 2)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<SecuritySource> Sources { get; set; } = new List<SecuritySource>();
        public bool ShouldSerializeSources() { return Sources.Count > 0; }

        /// <summary>
        /// For the Web Report Designer: Connections rights for the group. Set rights to connections through their names. By default, all connections can be selected.
        /// </summary>
#if WINDOWS
        [Category("Web Report Designer Security"), DisplayName("\tConnections"), Description("For the Web Report Designer: Connections rights for the group. Set rights to connections through their names. By default, all connections can be selected."), Id(5, 2)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<SecurityConnection> Connections { get; set; } = new List<SecurityConnection>();
        public bool ShouldSerializeConnections() { return Connections.Count > 0; }

        /// <summary>
        /// For the Web Report Designer: Columns rights for the group. Set rights to columns through the security tags or categories assigned. By default, all columns can be selected.
        /// </summary>
#if WINDOWS
        [Category("Web Report Designer Security"), DisplayName("Columns"), Description("For the Web Report Designer: Columns rights for the group. Set rights to columns through the security tags or categories assigned. By default, all columns can be selected."), Id(6, 2)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<SecurityColumn> Columns { get; set; } = new List<SecurityColumn>();
        public bool ShouldSerializeColumns() { return Columns.Count > 0; }

        /// <summary>
        /// Weight to select the default group when a user belongs to several groups. The options of the group having the highest weight are applied to the user.
        /// </summary>
#if WINDOWS
        [Category("Default Options"), DisplayName("\t\t\tWeight"), Description("Weight to select the default group when a user belongs to several groups. The options of the group having the highest weight are applied to the user."), Id(1, 5)]
#endif
        public int Weight { get; set; } = 1;

        /// <summary>
        /// Web Report Server: If true, the user can edit his profile (default culture, startup report, etc.).
        /// </summary>
#if WINDOWS
        [DefaultValue(true)]
        [Category("Default Options"), DisplayName("\t\tEdit profile"), Description("Web Report Server: If true, the user can edit his profile (default culture, startup report, etc.)."), Id(1, 5)]
#endif
        public bool EditProfile { get; set; } = true;

        /// <summary>
        /// The culture used for users belonging to the group. If empty, the default culture is used.
        /// </summary>
#if WINDOWS
        [Category("Default Options"), DisplayName("\tCulture"), Description("The culture used for users belonging to the group. If empty, the default culture is used."), Id(2, 5)]
        [TypeConverter(typeof(Seal.Forms.CultureInfoConverter))]
#endif
        public string Culture { get; set; }

        /// <summary>
        /// The logo file name used for to generate the reports. If empty, the default logo is used.
        /// </summary>
#if WINDOWS
        [Category("Default Options"), DisplayName("\tLogo file name"), Description("The logo file name used for to generate the reports. If empty, the default logo is used."), Id(4, 5)]
#endif
        public string LogoName { get; set; }
        /// <summary>
        /// Web Report Server: The action to take after the user logs in.
        /// </summary>
#if WINDOWS
        [DefaultValue(StartupOptions.None)]
        [TypeConverter(typeof(NamedEnumConverterNoDefault))]
        [Category("Default Options"), DisplayName("\tOn startup"), Description("Web Report Server: The action to take after the user logs in."), Id(3, 5)]
#endif
        public StartupOptions OnStartup { get; set; } = StartupOptions.None;

        /// <summary>
        /// Web Report Server: If the startup option is 'Execute a specific report', the relative report path to execute when the user logs in (e.g. '/Samples/04-Charts Gallery - Basics.srex').
        /// </summary>
#if WINDOWS
        [Category("Default Options"), DisplayName("\tReport executed on startup"), Description("Web Report Server: If the startup option is 'Execute a specific report', the relative report path to execute when the user logs in (e.g. '/Samples/04-Charts Gallery - Basics.srex')."), Id(4, 5)]
#endif
        public string StartupReport { get; set; }

        /// <summary>
        /// Web Report Server: Optional report name when the 'Report executed on startup' is set.).
        /// </summary>
#if WINDOWS
        [Category("Default Options"), DisplayName("\tReport name executed on startup"), Description("Web Report Server: Optional report name when the 'Report executed on startup' is set."), Id(5, 5)]
#endif
        public string StartupReportName { get; set; }

        /// <summary>
        /// Web Report Server: Define if reports are executed in a new window or in the same window by default.
        /// </summary>
#if WINDOWS
        [DefaultValue(ExecutionMode.NewWindow)]
        [TypeConverter(typeof(NamedEnumConverterNoDefault))]
        [Category("Default Options"), DisplayName("Execution mode"), Description("Web Report Server: Define if reports are executed in a new window or in the same window by default."), Id(8, 5)]
#endif
        public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.NewWindow;
    }
}

