//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using DynamicTypeDescriptor;
using Seal.Forms;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A SecurityConnection defines the security applied to a connection for the Web Report Designer
    /// </summary>
    public class SecurityConnection : RootEditor
    {
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Source").SetIsBrowsable(true);
                GetProperty("Name").SetIsBrowsable(true);
                GetProperty("Right").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        /// <summary>
        /// The name of the data source containing the connection (optional)
        /// </summary>
        [Category("Definition"), DisplayName("Source Name"), Description("The name of the data source containing the connection (optional)."), Id(1, 1)]
        public string Source { get; set; }

        /// <summary>
        /// The name of the connection
        /// </summary>
        [Category("Definition"), DisplayName("\tName"), Description("The name of the connection."), Id(2, 1)]
        public string Name { get; set; } = "";

        /// <summary>
        /// /The right applied for the connection having this name
        /// </summary>
        [Category("Rights"), DisplayName("Connection Right"), Description("The right applied for the connection having this name."), Id(2, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(EditorRight.NoSelection)]
        public EditorRight Right { get; set; } = EditorRight.NoSelection;

        /// <summary>
        /// Display name
        /// </summary>
        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                var result = "";
                if (!string.IsNullOrEmpty(Source)) result = "Source:" + Source;
                result += (result != "" ? "; " : "") + "Name:" + Name;
                return result;
            }
        }

    }
}
