//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A SecurityDevice defines the security applied to a device for the Web Report Designer
    /// </summary>
    public class SecurityDevice : RootEditor
    {

        /// <summary>
        /// The name of the device
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The right applied for the device having this name
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
                return Name;
            }
        }
    }
}

