//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.ComponentModel;
using System.Xml.Serialization;
#if WINDOWS
using DynamicTypeDescriptor;
using Seal.Forms;
#endif

namespace Seal.Model
{
    /// <summary>
    /// A SecuritySource defines the security applied to a data source for the Web Report Designer
    /// </summary>

    public class SecuritySource : RootEditor
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
                GetProperty("Right").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

#endif
        /// <summary>
        /// The name of the data source
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Name"), Description("The name of the data source."), Id(2, 1)]
#endif
        public string Name { get; set; } = "";

        /// <summary>
        /// The right applied for the data source having this name
        /// </summary>
#if WINDOWS
        [Category("Rights"), DisplayName("Data Source Right"), Description("The right applied for the data source having this name."), Id(2, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(EditorRight.NoSelection)]
#endif
        public EditorRight Right { get; set; } = EditorRight.NoSelection;

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

