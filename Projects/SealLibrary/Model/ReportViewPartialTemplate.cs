//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Xml.Serialization;
using Seal.Forms;
using System.Drawing.Design;

namespace Seal.Model
{
    public class ReportViewPartialTemplate : ReportComponent
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("UseCustom").SetIsBrowsable(true);
                GetProperty("Text").SetIsBrowsable(true);

                //Read only
                GetProperty("Text").SetIsReadOnly(!UseCustom);

                TypeDescriptor.Refresh(this);
            }
        }

        public override void InitEditor()
        {
            base.InitEditor();
        }

        #endregion

        bool _useCustom = false;
        [DisplayName("\tUse custom template text"), Description("If true, the partial template text for can be modified."), Category("Definition"), Id(2, 1)]
        [DefaultValue(false)]
        public bool UseCustom
        {
            get { return _useCustom; }
            set {
                _useCustom = value;
                UpdateEditorAttributes();
            }
        }

        string _text;
        [DisplayName("Custom template"), Description("The custom template text used instead of the template defined by the partial template."), Category("Definition"), Id(1, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string Text
        {
            get { return _text; }
            set {
                _text = value;
                LastTemplateModification = DateTime.Now;
            }
        }

        [XmlIgnore]
        public DateTime LastTemplateModification = DateTime.Now;
        [XmlIgnore]
        public ReportView View = null;
    }

}
