//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;
#if WINDOWS
using DynamicTypeDescriptor;
#endif

namespace Seal.Model
{
    /// <summary>
    /// Editor component to edit the properties in PropertyGrid
    /// </summary>
    public class RootEditor
    {
#if WINDOWS
        protected DynamicCustomTypeDescriptor _dctd = null;
#else
        protected object _dctd = null;
#endif
        #region Editor
        //Editor

        /// <summary>
        /// Init the editor objects and the default values
        /// </summary>
        public virtual void InitEditor()
        {
#if WINDOWS
            Init();
            InitDefaultValues();
#endif
        }

        /// <summary>
        /// Init the editor objects
        /// </summary>
        public virtual void Init()
        {
#if WINDOWS
            if (_dctd == null) _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            UpdateEditorAttributes();
#endif
        }

        /// <summary>
        /// Refresh properties attrivutes
        /// </summary>
        protected virtual void UpdateEditorAttributes()
        {
#if WINDOWS
            if (_dctd != null)
            {
                TypeDescriptor.Refresh(this);
            }
#endif
        }

        /// <summary>
        /// Update editor attributes
        /// </summary>
        public void UpdateEditor()
        {
#if WINDOWS
            UpdateEditorAttributes();
#endif
        }

        /// <summary>
        /// Set all properties to readonly
        /// </summary>
        virtual public void SetReadOnly()
        {
#if WINDOWS
            foreach (var property in Properties) property.SetIsReadOnly(true);
#endif
        }

        /// <summary>
        /// Init the default values
        /// </summary>
        public void InitDefaultValues()
        {
#if WINDOWS
            foreach (var property in Properties.Where(i => i.IsBrowsable))
            {
                if (property.DefaultValue == null && (property.GetValue(this) is string))
                {
                    property.DefaultValue = "";
                }

                property.SetValue(this, property.GetValue(this));
            }
#endif
        }

#if WINDOWS
        private List<CustomPropertyDescriptor> _properties = null;

        /// <summary>
        /// List of properties of the object
        /// </summary>
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

        /// <summary>
        /// Get a property descriptor from a property name
        /// </summary>
        protected CustomPropertyDescriptor GetProperty(string name)
        {
            return Properties.FirstOrDefault(i => i.Name == name);
        }
#endif

        #endregion
    }
}
