//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
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

        /// <summary>
        /// GUID of the MetaSource containing the linked table
        /// </summary>
        public string SourceGUID { get ; set; }
        /// <summary>
        /// GUID of the linked MetaTable
        /// </summary>
        public string TableGUID { get; set; }

        /// <summary>
        /// Current MetaSource
        /// </summary>
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
