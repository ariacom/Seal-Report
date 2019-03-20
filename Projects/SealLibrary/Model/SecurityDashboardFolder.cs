using DynamicTypeDescriptor;
using Seal.Converter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Windows.Forms.Design;
using System.Xml.Serialization;

namespace Seal.Model
{
    public class SecurityDashboardFolder : RootEditor
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
                GetProperty("Path").SetIsBrowsable(true);
                GetProperty("Right").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion


        string _name = "Folder Name";
        [Category("Definition"), DisplayName("\tDashboard Folder Name"), Description("The name of the public dashboard folder."), Id(1, 1)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        string _path = "Folder Path";
        [Category("Definition"), DisplayName("\tDashboard Folder Path"), Description("The physical path on the disk of the dashboard folder. The path is relative from the repository foder 'SpecialFolders\\Dashboards'."), Id(2, 1)]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        DashboardFolderRight _right = DashboardFolderRight.Edit;
        [Category("Definition"), DisplayName("Right"), Description("The right applied on the dashboards of the folder"), Id(3, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(DashboardFolderRight.Edit)]
        public DashboardFolderRight Right
        {
            get { return _right; }
            set
            {
                _right = value;
                UpdateEditorAttributes();
            }
        }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                var result = "";
                if (!string.IsNullOrEmpty(Name)) result = string.Format("{0} ({1})", Name, Path);
                return result;
            }
        }
    }
}
