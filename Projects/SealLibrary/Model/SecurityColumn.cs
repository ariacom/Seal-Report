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
    /// A SecurityColumn defines the security applied to a column for the Web Report Designer
    /// </summary>
    public class SecurityColumn : RootEditor
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
                GetProperty("Tag").SetIsBrowsable(true);
                GetProperty("Category").SetIsBrowsable(true);
                GetProperty("Right").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        /// <summary>
        /// The name of the data source containing the column (optional)
        /// </summary>
        [Category("Definition"), DisplayName("\tSource Name"), Description("The name of the data source containing the column (optional)."), Id(1, 1)]
        public string Source { get; set; }

        /// <summary>
        /// The name of the security tag (must match with the tags defined in the columns)
        /// </summary>
        [Category("Definition"), DisplayName("Security Tag"), Description("The name of the security tag (must match with the tags defined in the columns)."), Id(2, 1)]
        public string Tag { get; set; }

        /// <summary>
        /// The name of the category (must match with categories defined in the columns)
        /// </summary>
        [Category("Definition"), DisplayName("Category"), Description("The name of the category (must match with categories defined in the columns)."), Id(3, 1)]
        public string Category { get; set; }

        /// <summary>
        /// The right applied for the columns having this security tag or this category
        /// </summary>
        [Category("Rights"), DisplayName("Column Right"), Description("The right applied for the columns having this security tag or this category."), Id(1, 2)]
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
                if (!string.IsNullOrEmpty(Category)) result += (result != "" ? "; " : "") + "Category:" + Category;
                if (!string.IsNullOrEmpty(Tag)) result += (result != "" ? "; " : "") + "Tag:" + Tag;
                return result;
            }
        }
    }
}
