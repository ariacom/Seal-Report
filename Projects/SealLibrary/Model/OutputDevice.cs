//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// Abstract Class to implement an OutputDevice
    /// </summary>
    public abstract class OutputDevice : RootComponent
    {
        /// <summary>
        /// Full name
        /// </summary>
        public virtual string FullName { get; set; }

        /// <summary>
        /// Process the report and send it to the device
        /// </summary>
        public abstract string Process(Report report);

        /// <summary>
        /// Validate the device
        /// </summary>
        public abstract void Validate();

        /// <summary>
        /// Current file path
        /// </summary>
        [XmlIgnore]
        public string FilePath;

        /// <summary>
        /// Save to current file
        /// </summary>
        public abstract void SaveToFile();

        /// <summary>
        /// Save the device to a file
        /// </summary>
        public abstract void SaveToFile(string path);
    }
}
