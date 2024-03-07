//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.ComponentModel;
using System.Xml.Serialization;
#if WINDOWS
using Seal.Forms;
using System.Drawing.Design;
using DynamicTypeDescriptor;
#endif


namespace Seal.Model
{
    /// <summary>
    /// A ReportViewPartialTemplate is a partial template of a report view template.
    /// </summary>
    public class ReportViewPartialTemplate : ReportComponent
    {
#if WINDOWS
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

#endif
        bool _useCustom = false;
        /// <summary>
        /// If true, the partial template text for can be modified
        /// </summary>
#if WINDOWS
        [DisplayName("\tUse custom template text"), Description("If true, the partial template text for can be modified."), Category("Definition"), Id(2, 1)]
        [DefaultValue(false)]
#endif
        public bool UseCustom
        {
            get { return _useCustom; }
            set {
                _useCustom = value;
                UpdateEditorAttributes();
            }
        }

        string _text;
        /// <summary>
        /// The custom template text used instead of the template defined by the partial template
        /// </summary>
#if WINDOWS
        [DisplayName("Custom template"), Description("The custom template text used instead of the template defined by the partial template."), Category("Definition"), Id(1, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string Text
        {
            get { return _text; }
            set {
                _text = value;
                LastTemplateModification = DateTime.Now;
            }
        }

        /// <summary>
        /// Last modification date time
        /// </summary>
        [XmlIgnore]
        public DateTime LastTemplateModification = DateTime.Now;

        /// <summary>
        /// Current view
        /// </summary>
        [XmlIgnore]
        public ReportView View = null;
    }

}

