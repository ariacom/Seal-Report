//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;

namespace Seal.Model
{
    /// <summary>
    /// A SecurityGroup defines all the security applied to a user belonging to the group
    /// </summary>
    public class SecurityGroup : RootEditor
    {

        /// <summary>
        /// The security group name
        /// </summary>
        public string Name { get; set; } = "Group";

        /// <summary>
        /// The folder configurations for this group used for Web Publication of reports. By default, repository folders have no right.
        /// </summary>
        public List<SecurityFolder> Folders { get; set; } = new List<SecurityFolder>();
        public bool ShouldSerializeFolders() { return Folders.Count > 0; }

        /// <summary>
        /// Define the right of the dedicated personal folder for each user of the group
        /// </summary>
        [DefaultValue(PersonalFolderRight.None)]
        public PersonalFolderRight PersFolderRight { get; set; } = PersonalFolderRight.None;

        /// <summary>
        /// Define if the group can view Reports and Dashboards
        /// </summary>
        [DefaultValue(false)]
        public bool ShowAllFolders { get; set; } = false;

        /// <summary>
        /// Define if the group can view Reports and Dashboards
        /// </summary>
        [DefaultValue(ViewType.ReportsDashboards)]
        public ViewType ViewType { get; set; } = ViewType.ReportsDashboards;

        /// <summary>
        /// For the Web Report Designer: If true, SQL Models and Custom SQL for elements or restrictions can be edited through the Web Report Designer.
        /// </summary>
        [DefaultValue(false)]
        public bool SqlModel { get; set; } = false;

        /// <summary>
        /// For the Web Report Designer: Device rights for the group. Set rights to devices through their names. By default, all devices can be selected.
        /// </summary>
        public List<SecurityDevice> Devices { get; set; } = new List<SecurityDevice>();
        public bool ShouldSerializeDevices() { return Devices.Count > 0; }

        /// <summary>
        /// For the Web Report Designer: Data sources rights for the group. Set rights to data source through their names. By default, all sources can be selected.
        /// </summary>
        public List<SecuritySource> Sources { get; set; } = new List<SecuritySource>();
        public bool ShouldSerializeSources() { return Sources.Count > 0; }

        /// <summary>
        /// For the Web Report Designer: Connections rights for the group. Set rights to connections through their names. By default, all connections can be selected.
        /// </summary>
        public List<SecurityConnection> Connections { get; set; } = new List<SecurityConnection>();
        public bool ShouldSerializeConnections() { return Connections.Count > 0; }

        /// <summary>
        /// For the Web Report Designer: Columns rights for the group. Set rights to columns through the security tags or categories assigned. By default, all columns can be selected.
        /// </summary>
        public List<SecurityColumn> Columns { get; set; } = new List<SecurityColumn>();
        public bool ShouldSerializeColumns() { return Columns.Count > 0; }

        /// <summary>
        /// If true, the user can modify his current dashboard view (e.g. add/remove dashboards in his view or change orders).
        /// </summary>
        [DefaultValue(true)]
        public bool ManageDashboards { get; set; } = true;

        /// <summary>
        /// If true, users of the group have a personal folder to create personal dashboards.
        /// </summary>
        [DefaultValue(false)]
        public bool PersonalDashboardFolder { get; set; } = false;

        /// <summary>
        /// The dashboard folder configurations for this group used for Web Publication of dashboards. By default, repository dashboard folders have no right.
        /// </summary>
        public List<SecurityDashboardFolder> DashboardFolders { get; set; } = new List<SecurityDashboardFolder>();
        public bool ShouldSerializeDashboardFolders() { return DashboardFolders.Count > 0; }

        /// <summary>
        /// For the Dashboard Manager: Widget rights for the group. Set rights to widgets through the security tags or names assigned. By default all widgets can be selected.
        /// </summary>
        public List<SecurityWidget> Widgets { get; set; } = new List<SecurityWidget>();
        public bool ShouldSerializeWidgets() { return Widgets.Count > 0; }

        /// <summary>
        /// The culture used for users belonging to the group. If empty, the default culture is used.
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// The logo file name used for to generate the reports. If empty, the default logo is used.
        /// </summary>
        public string LogoName { get; set; }
    }
}

