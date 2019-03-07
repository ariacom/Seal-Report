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
    public class SecurityDashboard : RootEditor
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
                GetProperty("Published").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion


        string _name;
        [Category("Definition"), DisplayName("\tDashboard Name"), Description("The name of the public dashboard."), Id(1, 1)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }


        bool _published = true;
        [Category("Definition"), DisplayName("Is Published"), Description("Indicates if the dashboard is published or not for this group."), Id(2, 1)]
        [DefaultValue(true)]
        public bool Published
        {
            get { return _published; }
            set {
                _published = value;
            }
        }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                var result = "";
                if (!string.IsNullOrEmpty(Name)) result = "Dashboard Name:" + Name;
                return result;
            }
        }
    }
}
