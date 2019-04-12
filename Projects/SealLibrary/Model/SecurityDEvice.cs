//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using DynamicTypeDescriptor;
using Seal.Converter;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    public class SecurityDevice : RootEditor
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
                GetProperty("Right").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        string _name = "";
        [Category("Definition"), DisplayName("Name"), Description("The name of the device."), Id(2,1)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        EditorRight _right = EditorRight.NoSelection;
        [Category("Rights"), DisplayName("Device Right"), Description("The right applied for the device having this name."), Id(2, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(EditorRight.NoSelection)]
        public EditorRight Right
        {
            get { return _right; }
            set {
                _right = value;
            }
        }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                return Name;
            }
        }
    }
}
