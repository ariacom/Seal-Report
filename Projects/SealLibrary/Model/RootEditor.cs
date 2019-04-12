//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
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
            Init();
            InitDefaultValues();
        }

        public virtual void Init()
        {
            if (_dctd == null) _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            UpdateEditorAttributes();
        }

        public void InitDefaultValues()
        {
            foreach (var property in Properties.Where(i => i.IsBrowsable))
            {
                if (property.DefaultValue == null && (property.GetValue(this) is String))
                {
                    property.DefaultValue = "";
                }

                property.SetValue(this, property.GetValue(this));
            }
        }

        virtual public void SetReadOnly()
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

        public void UpdateEditor()
        {
            UpdateEditorAttributes();
        }

        #endregion
    }
}
