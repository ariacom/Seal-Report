//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Drawing.Design;

namespace Seal.Model
{
    /// <summary>
    /// A ReportViewPartialTemplate is a partial template of a report view template.
    /// </summary>
    public class ReportViewPartialTemplate : ReportComponent
    {

        bool _useCustom = false;
        /// <summary>
        /// If true, the partial template text for can be modified
        /// </summary>
        public bool UseCustom
        {
            get { return _useCustom; }
            set {
                _useCustom = value;
                
            }
        }

        string _text;
        /// <summary>
        /// The custom template text used instead of the template defined by the partial template
        /// </summary>
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

