//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A SecurityWidget defines the security applied to a widget for the Dashboard Manager
    /// </summary>
    public class SecurityWidget : RootEditor
    {

        /// <summary>
        /// The name of the report containing the widget (optional)
        /// </summary>
        public string ReportName { get; set; }

        /// <summary>
        /// The name of the security tag (optional, must match with the tags defined in the widget)
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// The name of the widget (optional, must match with name defined in the widget)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The right applied for the widget having this security tag or this name
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
                if (!string.IsNullOrEmpty(ReportName)) result = "Report Name:" + ReportName;
                if (!string.IsNullOrEmpty(Name)) result += (result != "" ? "; " : "") + "Widget Name:" + Name;
                if (!string.IsNullOrEmpty(Tag)) result += (result != "" ? "; " : "") + "Tag:" + Tag;
                return result;
            }
        }
    }
}

