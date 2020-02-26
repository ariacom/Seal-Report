//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// Dashboard definition. A Dashboard contains a list of Dashboard Items.
    /// </summary>
    public class Dashboard
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// Name of the dashboard
        /// </summary>
        public string Name { get; set; }
        public List<DashboardItem> Items { get; set; } = new List<DashboardItem>();

        /// <summary>
        /// Load a Dashboard from a file
        /// </summary>
        static public Dashboard LoadFromFile(string path)
        {
            Dashboard result = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Dashboard));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (Dashboard)serializer.Deserialize(xr);
                }
                result.Path = path;
                result.LastModification = File.GetLastWriteTime(path);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to read the file '{0}'.\r\n{1}\r\n", path, ex.Message, ex.StackTrace));
            }
            return result;
        }

        /// <summary>
        /// Re-init all GUIDs of the dashboard
        /// </summary>
        public void ReinitGUIDs()
        {
            GUID = Guid.NewGuid().ToString();
            foreach (var item in Items)
            {
                item.GUID = Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Re-init the group orders of the items
        /// </summary>
        public void ReinitGroupOrders()
        {
            int groupOrder = 1;
            var groups = Items.OrderBy(i => i.GroupOrder).ThenBy(i => i.GroupName).Select(i => i.GroupName).Distinct();

            foreach (var group in groups)
            {
                foreach (var item in Items.Where(j => j.GroupName == group)) item.GroupOrder = groupOrder;
                groupOrder++;
            }
        }

        /// <summary>
        /// Save the dashboard to its file
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(Path);
        }

        /// <summary>
        /// Save the dashboard to a file
        /// </summary>
        public void SaveToFile(string path)
        {
            try
            {
                foreach (var item in Items) item.JSonSerialization = false;

                XmlSerializer serializer = new XmlSerializer(typeof(Dashboard));
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.NewLineHandling = NewLineHandling.Entitize;
                using (XmlWriter xw = XmlWriter.Create(path, ws))
                {
                    serializer.Serialize(xw, this);
                }
            }
            finally
            {
                LastModification = File.GetLastWriteTime(path);
                Path = path;
            }
        }

        /// <summary>
        /// Order of the dashboard
        /// </summary>
        [XmlIgnore]
        public int Order = 0;

        /// <summary>
        /// True if the dashboard is editable
        /// </summary>
        [XmlIgnore]
        public bool Editable = false;

        /// <summary>
        /// True if the dashboard is personal
        /// </summary>
        [XmlIgnore]
        public bool IsPersonal = false;

        /// <summary>
        /// Current dashboard path
        /// </summary>
        [XmlIgnore]
        public string Path;

        /// <summary>
        /// Current dashboard security folder
        /// </summary>
        [XmlIgnore]
        public string Folder;

        /// <summary>
        /// Display name
        /// </summary>
        [XmlIgnore]
        public string DisplayName;

        /// <summary>
        /// Full name
        /// </summary>
        [XmlIgnore]
        public string FullName;

        /// <summary>
        /// Last modification date time
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification;

        /// <summary>
        /// Colors available for the widgets
        /// </summary>
        public static string[] Colors = new string[] { "", "default", "primary", "success", "info", "warning", "danger" };

        /// <summary>
        /// Icons available for the widgets
        /// </summary>
        public static string[] Icons = new string[] {
                    "info-sign",
                    "warning-sign",
                    "ok",
                    "remove",
                    "envelope",
                    "search",
                    "star",
                    "signal",
                    "cog",
                    "list-alt",
                    "education",
                    "thumbs-up",
                    "thumbs-down",
                    "bell",
                    "folder-close",
                    "folder-open",
                    "pencil",
                    "usd",
                    "euro",
                    "user",
                    "tag",
                    "check",
                };
    }

}
