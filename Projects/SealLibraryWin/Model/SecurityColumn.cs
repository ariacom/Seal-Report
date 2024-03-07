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
    /// A SecurityColumn defines the security applied to a column for the Web Report Designer
    /// </summary>
    public class SecurityColumn : RootEditor
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
                GetProperty("Source").SetIsBrowsable(true);
                GetProperty("Tag").SetIsBrowsable(true);
                GetProperty("Category").SetIsBrowsable(true);
                GetProperty("Right").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

#endif
        /// <summary>
        /// The name of the data source containing the column (optional)
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tSource Name"), Description("The name of the data source containing the column (optional)."), Id(1, 1)]
#endif
        public string Source { get; set; }

        /// <summary>
        /// The name of the security tag (must match with the tags defined in the columns)
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Security tag"), Description("The name of the security tag (must match with the tags defined in the columns)."), Id(2, 1)]
#endif
        public string Tag { get; set; }

        /// <summary>
        /// The name of the category (must match with categories defined in the columns)
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Category"), Description("The name of the category (must match with categories defined in the columns)."), Id(3, 1)]
#endif
        public string Category { get; set; }

        /// <summary>
        /// The right applied for the columns having this security tag or this category
        /// </summary>
#if WINDOWS
        [Category("Rights"), DisplayName("Column Right"), Description("The right applied for the columns having this security tag or this category."), Id(1, 2)]
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
                var result = "";
                if (!string.IsNullOrEmpty(Source)) result = "Source:" + Source;
                if (!string.IsNullOrEmpty(Category)) result += (result != "" ? "; " : "") + "Category:" + Category;
                if (!string.IsNullOrEmpty(Tag)) result += (result != "" ? "; " : "") + "Tag:" + Tag;
                return result;
            }
        }
    }
}

