//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using DynamicTypeDescriptor;
using Seal.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A MetaEV defines a value of an enumerated list
    /// </summary>
    public class MetaEV
    {
        /// <summary>
        /// The database value of the enumerated value
        /// </summary>
        [Category("Definition"), DisplayName("\tIdentifier"), Description("The database value of the enumerated value")]
        public string Id { get; set; }

        /// <summary>
        /// The optional display value
        /// </summary>
        [Category("Definition"), DisplayName("\tValue"), Description("The optional display value")]
        public string Val { get; set; }

        /// <summary>
        /// If set, the enumerated value is only valid when the connection is used. If empty, the value is valid for all connections.
        /// </summary>
        [DefaultValue("")]
        [TypeConverter(typeof(SourceConnectionConverter))]
        [Category("Definition"), DisplayName("Connection"), Description("If set, the enumerated value is only valid when the connection is used. If empty, the value is valid for all connections.")]
        public string ConnectionGUID { get; set; }

        /// <summary>
        /// The optional display value for the restriction list
        /// </summary>
        [Category("Definition"), DisplayName("Restriction Value"), Description("The optional display value for the restriction list")]
        public string ValR { get; set; }

        /// <summary>
        /// The optional CSS Style applied to the cell displayed
        /// </summary>
        [Category("Definition"), DisplayName("CSS Style"), Description("The optional CSS Style applied to the cell displayed")]
        public string Css { get; set; }

        /// <summary>
        /// The optional CSS Class applied to the cell displayed
        /// </summary>
        [Category("Definition"), DisplayName("CSS Class"), Description("The optional CSS Class applied to the cell displayed")]
        public string Class { get; set; }

        /// <summary>
        /// The final value displayed in the report
        /// </summary>
        [Category("Display"), DisplayName("\tValue"), Description("The final value displayed in the report")]
        public string DisplayValue
        {
            get { return !string.IsNullOrEmpty(Val) ? Val : Id; }
        }

        /// <summary>
        /// The final value displayed for the restriction
        /// </summary>
        [Category("Display"), DisplayName("Restriction"), Description("The final value displayed for the restriction")]
        public string DisplayRestriction
        {
            get { return !string.IsNullOrEmpty(ValR) ? ValR : DisplayValue; }
        }

        /// <summary>
        /// Id used for execution
        /// </summary>
        [XmlIgnore]
        public string HtmlId;

        /// <summary>
        /// Meta enum of the value. Used only for the editor.
        /// </summary>
        [XmlIgnore]
        public MetaEnum MetaEnum;
    }
}
