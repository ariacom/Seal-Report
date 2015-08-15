//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicTypeDescriptor;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Seal.Model
{
    public class RootEditor
    {
        #region Editor
        //Editor
        protected DynamicCustomTypeDescriptor _dctd = null;
        public virtual void InitEditor()
        {
            if (_dctd == null) _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            UpdateEditorAttributes();
        }

        public void SetReadOnly()
        {
            foreach (var property in Properties) property.SetIsReadOnly(true);
        }

        private List<CustomPropertyDescriptor> _properties = null;

        [XmlIgnore, Browsable(false)]
        protected List<CustomPropertyDescriptor> Properties
        {
            get
            {
                if (_properties == null && _dctd != null)
                {
                    _properties = new List<CustomPropertyDescriptor>();
                    foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(this))
                    {
                        _properties.Add(pd as CustomPropertyDescriptor);
                    }
                }
                return _properties;
            }
        }

        protected CustomPropertyDescriptor GetProperty(string name)
        {
            return Properties.FirstOrDefault(i => i.Name == name);
        }

        protected virtual void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                TypeDescriptor.Refresh(this);
            }
        }

        #endregion
    }
}
