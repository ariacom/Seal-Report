//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using DynamicTypeDescriptor;
using Seal.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// Class used to define default Dashboards view of a group
    /// </summary>
    public class SecurityDashboardOrder : RootEditor
    {
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("GUID").SetIsBrowsable(true);
                GetProperty("Order").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        private string _GUID;
        /// <summary>
        /// The Dashboard to show in the default view
        /// </summary>
        [Category("Definition"), DisplayName("Dashboard"), Description("The Dashboard to show in the default view."), Id(1, 1)]
        [TypeConverter(typeof(DashboardOrderConverter))]
        public string GUID { get => _GUID; 
            set { 
                _GUID = value;
                if (SecurityGroup != null) Dashboard = SecurityGroup.Dashboards.FirstOrDefault(i => i.GUID == _GUID);
            }
        }
        /// <summary>
        /// A numeric value to sort the Dashboards in the default view
        /// </summary>
        [Category("Definition"), DisplayName("Sort order"), Description("A numeric value to sort the Dashboards in the default view."), Id(3, 1)]
        public int Order { get; set; } = 1;

        [XmlIgnore]
        public Dashboard Dashboard;

        [XmlIgnore]
        public SecurityGroup SecurityGroup;
    }
}
