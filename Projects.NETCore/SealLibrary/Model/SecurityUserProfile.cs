//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A SecurityUserProfile stores the dashboards viewed by a user
    /// </summary>
    public class SecurityUserProfile
    {
        /// <summary>
        /// List of dashboard identifers to display to the user
        /// </summary>
        public List<string> Dashboards { get; set; } = new List<string>();

        /// <summary>
        /// Load the profile from a file path
        /// </summary>
        static public SecurityUserProfile LoadFromFile(string path)
        {
            SecurityUserProfile result = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SecurityUserProfile));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (SecurityUserProfile)serializer.Deserialize(xr);
                }
                result.Path = path;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to read the file '{0}'.\r\n{1}\r\n", path, ex.Message, ex.StackTrace));
            }
            return result;
        }

        /// <summary>
        /// Save to current file
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(Path);
        }

        /// <summary>
        /// Save to a file path
        /// </summary>
        public void SaveToFile(string path)
        {
            try
            {
                Dashboards.RemoveAll(i => string.IsNullOrEmpty(i));

                XmlSerializer serializer = new XmlSerializer(typeof(SecurityUserProfile));
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.NewLineHandling = NewLineHandling.Entitize;
                using (XmlWriter xw = XmlWriter.Create(path, ws))
                {
                    serializer.Serialize(xw, this);
                }
            }
            finally
            {
                Path = path;
            }
        }

        /// <summary>
        /// Current file path
        /// </summary>
        [XmlIgnore]
        public string Path;

    }
}

