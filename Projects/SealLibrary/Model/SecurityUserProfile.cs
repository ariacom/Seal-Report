//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// Store recent file for the user's profile
    /// </summary>
    public class RecentFileItem
    {
        /// <summary>
        /// Relative path of the report
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// View GUID for the execution
        /// </summary>
        public string ViewGUID { get; set; }

        /// <summary>
        /// Output GUID for the execution
        /// </summary>
        public string OutputGUID { get; set; }

        /// <summary>
        /// Report name
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Object to store the culture and the recent reports of the user
    /// </summary>
    public class SecurityUserProfile
    {
        /// <summary>
        /// Culture of the user
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// Action to take after the user logs in
        /// </summary>
        public StartupOptions OnStartup { get; set; } = StartupOptions.Default;

        /// <summary>
        /// Relative report path to execute when the user logs in if OnStartup = 'Execute a specific report'
        /// </summary>
        public string StartUpReport { get; set; }

        /// <summary>
        /// Name of report to execute when the user logs in if OnStartup = 'Execute a specific report'
        /// </summary>
        public string StartupReportName { get; set; }

        /// <summary>
        /// Last folder of the user
        /// </summary>
        public string LastFolder { get; set; }

        /// <summary>
        /// List of recent reports
        /// </summary>
        public List<RecentFileItem> RecentReports { get; set; } = new List<RecentFileItem>();
        public void SetRecentReports(string path, Report report, string viewGUID, string outputGUID)
        {
            path = FileHelper.ConvertOSFilePath(path);
            RecentReports.RemoveAll(i => i.Path == path);
            RecentReports.Insert(0, new RecentFileItem() { Path = path, ViewGUID = viewGUID, OutputGUID = outputGUID, Name = !string.IsNullOrEmpty(report.ExecutionName) ? report.ExecutionName : report.DisplayNameEx });
            if (RecentReports.Count > 9) RecentReports.RemoveAt(RecentReports.Count - 1);
        }

        /// <summary>
        /// Load the profile from a file path
        /// </summary>
        static public SecurityUserProfile LoadFromFile(string path)
        {
            SecurityUserProfile result = null;
            try
            {
                path = FileHelper.ConvertOSFilePath(path);
                if (!File.Exists(path)) throw new Exception("File not found: " + path);

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
                XmlSerializer serializer = new XmlSerializer(typeof(SecurityUserProfile));
                using (var tw = new StreamWriter(path))
                {
                    serializer.Serialize(tw, this);
                    tw.Close();
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
