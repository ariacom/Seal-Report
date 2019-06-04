//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using DynamicTypeDescriptor;
using Seal.Converter;
using Seal.Helpers;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    public class SecurityDashboardFolder : RootEditor
    {
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Name").SetIsBrowsable(true);
                GetProperty("Right").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion


        string _name = "Folder Name";
        [Category("Definition"), DisplayName("\tDashboard Folder Name"), Description("The name of the public dashboard folder. The physical path on the disk of the dashboard folder is relative from the repository folder '\\Dashboards'"), Id(1, 1)]
        [TypeConverter(typeof(DashboardFolderConverter))]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        DashboardFolderRight _right = DashboardFolderRight.Edit;
        [Category("Definition"), DisplayName("Right"), Description("The right applied on the dashboards of the folder"), Id(3, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(DashboardFolderRight.Edit)]
        public DashboardFolderRight Right
        {
            get { return _right; }
            set
            {
                _right = value;
                UpdateEditorAttributes();
            }
        }

        [XmlIgnore]
        public string FolderPath
        {
            get
            {
                return Helper.CleanFileName(Name);
            }
        }

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
