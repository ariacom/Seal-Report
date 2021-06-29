//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A SecurityConnection defines the security applied to a connection for the Web Report Designer
    /// </summary>
    public class SecurityConnection : RootEditor
    {

        /// <summary>
        /// The name of the data source containing the connection (optional)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The name of the connection
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// /The right applied for the connection having this name
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
                result += (result != "" ? "; " : "") + "Name:" + Name;
                return result;
            }
        }

    }
}

