using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Seal.Model
{
    public class Dashboard
    {
        private string _GUID;
        private string _name;
        private List<DashboardItem> _items = new List<DashboardItem>();

        public string GUID { get => _GUID; set => _GUID = value; }
        public string Name { get => _name; set => _name = value; }
        public List<DashboardItem> Items { get => _items; set => _items = value; }

        static public Dashboard LoadFromFile(string path)
        {
            Dashboard result = null;
            try
            {
                StreamReader sr = new StreamReader(path);
                XmlSerializer serializer = new XmlSerializer(typeof(Dashboard));
                result = (Dashboard)serializer.Deserialize(sr);
                result.Path = path;
                sr.Close();
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
                foreach (var item2 in Items.Where(j => j.GroupName == group)) item2.GroupOrder = groupOrder;
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
                StreamWriter sw = new StreamWriter(path);
                serializer.Serialize(sw, this);
                sw.Close();
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
    }

}
