//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;
using Seal.Converter;
using DynamicTypeDescriptor;
using Seal.Helpers;
using System.Drawing.Design;
using Seal.Forms;

namespace Seal.Model
{
    public class CategoryFolder : RootComponent
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Path").SetIsBrowsable(!string.IsNullOrEmpty(Name));
                GetProperty("Information").SetIsBrowsable(!string.IsNullOrEmpty(Name) || !string.IsNullOrEmpty(Information));

                //Read only
                GetProperty("Information").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        string _path;
        [DisplayName("Path"), Description("The full path of the of the category. This can be modified to change globally all the category names of the columns. The category path can be specified using the '/' character (e.g. '/Master/Name1/Name2')"), Category("Helpers"), Id(1,1)]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        #region Helpers
        string _information;
        [DisplayName("Information"), Description("Last information"), Category("Helpers"), Id(2, 1)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        public void SetInformation(string information)
        {
            _information = information;
            UpdateEditorAttributes();
        }

        #endregion
    }

}
