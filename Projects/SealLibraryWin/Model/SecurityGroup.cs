//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Collections.Generic;
using System.ComponentModel;
using Seal.AI;
using Seal.Helpers;

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
                GetProperty("RepositoryFolders").SetIsBrowsable(true);
                GetProperty("FoldersScript").SetIsBrowsable(true);
                GetProperty("FolderDetailScript").SetIsBrowsable(true);
                GetProperty("MenuScript").SetIsBrowsable(true);

                GetProperty("SqlModel").SetIsBrowsable(true);
                GetProperty("DataSourceGUIDs").SetIsBrowsable(true);
                GetProperty("OutputDeviceGUIDs").SetIsBrowsable(true);

                GetProperty("OnStartup").SetIsBrowsable(true);
                GetProperty("StartupReport").SetIsBrowsable(true);
                GetProperty("StartupReportName").SetIsBrowsable(true);
                GetProperty("ExecutionMode").SetIsBrowsable(true);
                GetProperty("Weight").SetIsBrowsable(true);
                GetProperty("EditConfiguration").SetIsBrowsable(true);
                GetProperty("EditProfile").SetIsBrowsable(true);
                GetProperty("AgentGUIDs").SetIsBrowsable(true);
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

        /// <summary>
        /// The unique identifier
        /// </summary>
        public string GUID {get; set; }


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
            set { _name = value; }
        }

        /// <summary>
        /// The report folder configurations for this group used for Web Publication of reports (relative to the repository 'Reports' folder). By default, report folders have no right.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\t\t\tReport Folders"), Description("The report folder configurations for this group used for Web Publication of reports (relative to the repository 'Reports' folder). By default, report folders have no right."), Id(2, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<SecurityFolder> Folders { get; set; } = new List<SecurityFolder>();
        public bool ShouldSerializeFolders() { return Folders.Count > 0; }

        /// <summary>
        /// DANGER ZONE: The repository folder configurations for this group. Unlike Report Folders, these can publish ANY folder located under the
        /// repository root (Views, Sources, Settings, Security...), outside the 'Reports' tree. By default, repository folders have no right.
        /// </summary>
#if WINDOWS
        [Category("Danger Zone"), DisplayName("Repository Folders"), Description("⚠ DANGER ZONE: The repository folders published for this group. Unlike Report Folders, these can publish ANY folder located under the repository root (Views, Sources, Settings, Security...), outside the 'Reports' tree. Exposing such folders - especially with Upload enabled - can leak sensitive data or allow remote code execution (uploaded server-executed templates). A folder pointing at or inside the 'Reports' tree is forbidden here and ignored at runtime; use 'Report Folders' for that. By default, repository folders have no right."), Id(1, 9)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<SecurityRepositoryFolder> RepositoryFolders { get; set; } = new List<SecurityRepositoryFolder>();
        public bool ShouldSerializeRepositoryFolders() { return RepositoryFolders.Count > 0; }

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
        /// If true: SQL Models and Custom SQL for elements or restrictions can be edited, and the AI Tools can query the database (raw SQL) and create reports from raw SQL. If false, these capabilities are denied.
        /// </summary>
#if WINDOWS
        [Category("Tools Security"), DisplayName("\t\t\tSQL Models"), Description("If true: SQL Models and Custom SQL for elements or restrictions can be edited, and the AI Tools can query the database (raw SQL) and create reports from raw SQL. If false, these capabilities are denied. Note that dynamic filters set for security purpose will not be applied."), Id(1, 2)]
        [DefaultValue(false)]
#endif
        public bool SqlModel { get; set; } = false;

        /// <summary>
        /// For the AI Tools: The data sources (identified by their GUID) that the AI tools can access for this group. If the list is empty, all data sources are allowed.
        /// </summary>
#if WINDOWS
        [Category("Tools Security"), DisplayName("\t\tAI Tools Data Sources"), Description("The data sources that the AI tools can access for this group. If the list is empty, all data sources are allowed."), Id(2, 2)]
        [Editor(typeof(SecurityDataSourcesEditor), typeof(UITypeEditor))]
#endif
        public List<string> DataSourceGUIDs { get; set; } = new List<string>();
        public bool ShouldSerializeDataSourceGUIDs() { return DataSourceGUIDs.Count > 0; }

        /// <summary>
        /// For the AI Tools: The output devices (identified by their GUID) that the AI tools can use for this group (e.g. to configure email or file server outputs). If the list is empty, all output devices are allowed.
        /// </summary>
#if WINDOWS
        [Category("Tools Security"), DisplayName("\tAI Tools Output Devices"), Description("The output devices that the AI tools can use for this group (e.g. to configure email or file server outputs). If the list is empty, all output devices are allowed."), Id(3, 2)]
        [Editor(typeof(SecurityDevicesEditor), typeof(UITypeEditor))]
#endif
        public List<string> OutputDeviceGUIDs { get; set; } = new List<string>();
        public bool ShouldSerializeOutputDeviceGUIDs() { return OutputDeviceGUIDs.Count > 0; }

        /// <summary>
        /// Weight to select the default group when a user belongs to several groups. The options of the group having the highest weight are applied to the user.
        /// </summary>
#if WINDOWS
        [Category("Default Options"), DisplayName("\t\t\tWeight"), Description("Weight to select the default group when a user belongs to several groups. The options of the group having the highest weight are applied to the user."), Id(1, 5)]
#endif
        public int Weight { get; set; } = 1;

        /// <summary>
        /// Web Report Server: For Administrators of the Web Server, the user can edit the configuration and security of the Web Server.
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Default Options"), DisplayName("\t\tEdit configuration (Administrator)"), Description("Web Report Server: For Administrators of the Web Server, the user can edit the configuration and security of the Web Server."), Id(2, 5)]
#endif
        public bool EditConfiguration { get; set; } = false;

        /// <summary>
        /// Web Report Server: If true, the user can edit his profile (default culture, startup report, etc.).
        /// </summary>
#if WINDOWS
        [DefaultValue(true)]
        [Category("Default Options"), DisplayName("\t\tEdit profile"), Description("Web Report Server: If true, the user can edit his profile (default culture, startup report, etc.)."), Id(3, 5)]
#endif
        public bool EditProfile { get; set; } = true;

        /// <summary>
        /// Web Report Server: The GUIDs of the AI Agents assigned to users of this group. Leave empty to disable the AI Agent for this group.
        /// </summary>
#if WINDOWS
        [Category("Default Options"), DisplayName("\t\tAI Agents"), Description("Web Report Server: The AI Agents assigned to users of this group. Leave empty to disable the AI Agent for this group."), Id(4, 5)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
#endif
        public List<string> AgentGUIDs { get; set; } = new List<string>();
        public bool ShouldSerializeAgentGUIDs() { return AgentGUIDs.Count > 0; }

        /// <summary>
        /// The culture used for users belonging to the group. If empty, the default culture is used.
        /// </summary>
#if WINDOWS
        [Category("Default Options"), DisplayName("\tCulture"), Description("The culture used for users belonging to the group. If empty, the default culture is used."), Id(6, 5)]
        [TypeConverter(typeof(Seal.Forms.CultureInfoConverter))]
#endif
        public string Culture { get; set; }

        /// <summary>
        /// The logo file name used for to generate the reports. If empty, the default logo is used.
        /// </summary>
#if WINDOWS
        [Category("Default Options"), DisplayName("\tLogo file name"), Description("The logo file name used for to generate the reports. If empty, the default logo is used."), Id(7, 5)]
#endif
        public string LogoName { get; set; }
        /// <summary>
        /// Web Report Server: The action to take after the user logs in.
        /// </summary>
#if WINDOWS
        [DefaultValue(StartupOptions.None)]
        [TypeConverter(typeof(NamedEnumConverterNoDefault))]
        [Category("Default Options"), DisplayName("\tOn startup"), Description("Web Report Server: The action to take after the user logs in."), Id(8, 5)]
#endif
        public StartupOptions OnStartup { get; set; } = StartupOptions.None;

        /// <summary>
        /// Web Report Server: If the startup option is 'Execute a specific report', the relative report path to execute when the user logs in (e.g. '/Samples/40-Startup Report.srex').
        /// </summary>
#if WINDOWS
        [Category("Default Options"), DisplayName("\tReport executed on startup"), Description("Web Report Server: If the startup option is 'Execute a specific report', the relative report path to execute when the user logs in (e.g. '/Samples/40-Startup Report.srex')."), Id(9, 5)]
#endif
        public string StartupReport { get; set; }

        /// <summary>
        /// Web Report Server: Optional report name when the 'Report executed on startup' is set.).
        /// </summary>
#if WINDOWS
        [Category("Default Options"), DisplayName("\tReport name executed on startup"), Description("Web Report Server: Optional report name when the 'Report executed on startup' is set."), Id(10, 5)]
#endif
        public string StartupReportName { get; set; }

        /// <summary>
        /// Web Report Server: Define if reports are executed in a new window or in the same window by default.
        /// </summary>
#if WINDOWS
        [DefaultValue(ExecutionMode.NewWindow)]
        [TypeConverter(typeof(NamedEnumConverterNoDefault))]
        [Category("Default Options"), DisplayName("Execution mode"), Description("Web Report Server: Define if reports are executed in a new window or in the same window by default."), Id(11, 5)]
#endif
        public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.NewWindow;
    }
}

