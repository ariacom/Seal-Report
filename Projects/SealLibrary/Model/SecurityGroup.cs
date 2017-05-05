using DynamicTypeDescriptor;
using Seal.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;

namespace Seal.Model
{
    public class SecurityGroup : RootEditor
    {
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Name").SetIsBrowsable(true);
                GetProperty("Folders").SetIsBrowsable(true);
                GetProperty("Columns").SetIsBrowsable(true);
                GetProperty("Devices").SetIsBrowsable(true);
                GetProperty("Connections").SetIsBrowsable(true);
                GetProperty("Sources").SetIsBrowsable(true);
                GetProperty("Culture").SetIsBrowsable(true);
                GetProperty("Theme").SetIsBrowsable(true);
                GetProperty("LogoName").SetIsBrowsable(true);
                GetProperty("PersonalFolder").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        string _name = "Group";
        [Category("Definition"), DisplayName("\tName"), Description("The security group name."), Id(1, 1)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private List<SecurityFolder> _folders = new List<SecurityFolder>();
        [Category("Definition"), DisplayName("Folders"), Description("The folder configurations for this group used for Web Publication. By default repository folders have no right."), Id(2, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<SecurityFolder> Folders
        {
            get { return _folders; }
            set { _folders = value; }
        }

        bool _personalFolder = true;
        [Category("Definition"), DisplayName("Personal folder"), Description("If true, each user of the group has a dedicated personal folder with full rights."), Id(3, 1)]
        public bool PersonalFolder
        {
            get { return _personalFolder; }
            set { _personalFolder = value; }
        }

        private List<SecurityDevice> _devices = new List<SecurityDevice>();
        [Category("Web Report Designer Security"), DisplayName("\t\tDevices"), Description("For the Web Report Designer: Device rights for the group. Set rights to devices through their names. By default all devices can be selected."), Id(1, 2)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<SecurityDevice> Devices
        {
            get { return _devices; }
            set { _devices = value; }
        }

        private List<SecuritySource> _sources = new List<SecuritySource>();
        [Category("Web Report Designer Security"), DisplayName("\t\tSources"), Description("For the Web Report Designer: Data sources rights for the group. Set rights to data source through their names. By default all sources can be selected."), Id(2, 2)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<SecuritySource> Sources
        {
            get { return _sources; }
            set { _sources = value; }
        }

        private List<SecurityConnection> _connections = new List<SecurityConnection>();
        [Category("Web Report Designer Security"), DisplayName("\tConnections"), Description("For the Web Report Designer: Connections rights for the group. Set rights to connections through their names. By default all devices can be selected."), Id(3, 2)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<SecurityConnection> Connections
        {
            get { return _connections; }
            set { _connections = value; }
        }

        private List<SecurityColumn> _columns = new List<SecurityColumn>();
        [Category("Web Report Designer Security"), DisplayName("Columns"), Description("For the Web Report Designer: Columns rights for the group. Set rights to columns through the security tags or categories assigned. By default all columns can be selected."), Id(4, 2)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<SecurityColumn> Columns
        {
            get { return _columns; }
            set { _columns = value; }
        }

        string _culture = "";
        [Category("Options"), DisplayName("Culture"), Description("The culture used for users belonging to the group. If empty, the default culture is used."), Id(1, 3)]
        [TypeConverter(typeof(Seal.Converter.CultureInfoConverter))]
        public string Culture
        {
            get { return _culture; }
            set { _culture = value; }
        }

        string _theme;
        [Category("Options"), DisplayName("Report Theme"), Description("The default report theme used for to generate the reports. If empty, the default theme is used."), Id(2, 3)]
        [TypeConverter(typeof(Seal.Converter.ThemeConverter))]
        public string Theme
        {
            get { return _theme; }
            set { _theme = value; }
        }

        string _logoName;
        [Category("Options"), DisplayName("Logo file name"), Description("The logo file name used for to generate the reports. If empty, the default logo is used."), Id(3, 3)]
        public string LogoName
        {
            get { return _logoName; }
            set { _logoName = value; }
        }
    }
}
