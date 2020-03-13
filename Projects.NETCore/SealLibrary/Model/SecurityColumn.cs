//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A SecurityColumn defines the security applied to a column for the Web Report Designer
    /// </summary>
    public class SecurityColumn : RootEditor
    {

        /// <summary>
        /// The name of the data source containing the column (optional)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The name of the security tag (must match with the tags defined in the columns)
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// The name of the category (must match with categories defined in the columns)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The right applied for the columns having this security tag or this category
        /// </summary>
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

