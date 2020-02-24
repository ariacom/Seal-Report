//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Helpers;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A SecurityDashboardFolder defines the security applied to a dashboard folder for the Dashboard Manager
    /// </summary>
    public class SecurityDashboardFolder : RootEditor
    {

        /// <summary>
        /// The name of the public dashboard folder. The physical path on the disk of the dashboard folder is relative from the repository folder '\\Dashboards'
        /// </summary>
        public string Name { get; set; } = "Folder Name";

        DashboardFolderRight _right = DashboardFolderRight.Edit;
        /// <summary>
        /// The right applied on the dashboards of the folder
        /// </summary>
        [DefaultValue(DashboardFolderRight.Edit)]
        public DashboardFolderRight Right
        {
            get { return _right; }
            set
            {
                _right = value;
                
            }
        }

        /// <summary>
        /// Folder path
        /// </summary>
        [XmlIgnore]
        public string FolderPath
        {
            get
            {
                return Helper.CleanFileName(Name);
            }
        }

        /// <summary>
        /// Display name
        /// </summary>
        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                return Name;
            }
        }
    }
}

