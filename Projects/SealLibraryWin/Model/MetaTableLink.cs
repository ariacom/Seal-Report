﻿//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// Link to a MetaTable of another MetaSource
    /// </summary>
    public class MetaTableLink
    {

        public string SourceGUID { get ; set; }
        public string TableGUID { get; set; }

        protected MetaSource _source;
        /// <summary>
        /// Current MetaSource
        /// </summary>
        [XmlIgnore, Browsable(false)]
        public MetaSource Source
        {
            get { return _source; }
            set {
                _metaTable = null;
                _source = value; 
            }
        }

        MetaTable _metaTable = null;
        /// <summary>
        /// Current MetaTable of the link
        /// </summary>
        public MetaTable MetaTable
        {
            get
            {
                if (_metaTable == null && Source != null)
                {
                    _metaTable = Source.MetaData.Tables.FirstOrDefault(i => i.GUID == TableGUID);
                }
                return _metaTable;

            }
        }

        /// <summary>
        /// True if the link is editable
        /// </summary>
        [XmlIgnore]
        public bool IsEditable = true;


        /// <summary>
        /// Display name of the link
        /// </summary>
        [XmlIgnore]
        public string DisplayName
        {
            get { return MetaTable.FullDisplayName; }
        }
    }
}
