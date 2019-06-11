//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
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
    public class Dashboard
    {
        public string GUID { get; set; }
        public string Name { get; set; }
        public List<DashboardItem> Items { get; set; } = new List<DashboardItem>();

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

        public void ReinitGUIDs()
        {
            GUID = Guid.NewGuid().ToString();
            foreach (var item in Items)
            {
                item.GUID = Guid.NewGuid().ToString();
            }
        }

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

        public void SaveToFile()
        {
            SaveToFile(Path);
        }

        public void SaveToFile(string path)
        {
            try
            {
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

        [XmlIgnore]
        public int Order = 0;
        [XmlIgnore]
        public bool Editable = false;
        [XmlIgnore]
        public bool IsPersonal = false;
        [XmlIgnore]
        public string Path;
        [XmlIgnore]
        public string Folder;
        [XmlIgnore]
        public string DisplayName;
        [XmlIgnore]
        public string FullName;
        [XmlIgnore]
        public DateTime LastModification;

        public static string[] Colors = new string[] { "", "default", "primary", "success", "info", "warning", "danger" };

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
