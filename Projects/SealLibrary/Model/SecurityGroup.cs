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
                GetProperty("Culture").SetIsBrowsable(true);
                GetProperty("Theme").SetIsBrowsable(true);
                GetProperty("LogoName").SetIsBrowsable(true);

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
        [Category("Definition"), DisplayName("Folders"), Description("The folder configurations for this group. Only one path folder can be configured per security security group."), Id(2, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<SecurityFolder> Folders
        {
            get { return _folders; }
            set { _folders = value; }
        }

        string _culture = "";
        [Category("Options"), DisplayName("Culture"), Description("The culture used for users belonging to the group. If empty, the default culture is used."), Id(1, 2)]
        [TypeConverter(typeof(Seal.Converter.CultureInfoConverter))]
        public string Culture
        {
            get { return _culture; }
            set { _culture = value; }
        }

        string _theme;
        [Category("Options"), DisplayName("Report Theme"), Description("The default report theme used for to generate the reports. If empty, the default theme is used."), Id(1, 2)]
        [TypeConverter(typeof(Seal.Converter.ThemeConverter))]
        public string Theme
        {
            get { return _theme; }
            set { _theme = value; }
        }

        string _logoName;
        [Category("Options"), DisplayName("Logo file name"), Description("The logo file name used for to generate the reports. If empty, the default logo is used."), Id(1, 2)]
        public string LogoName
        {
            get { return _logoName; }
            set { _logoName = value; }
        }
    }
}
