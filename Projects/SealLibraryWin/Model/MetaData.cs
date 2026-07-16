//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A MetaData contains the list of MetaTable, MetaJoin and MetaEnum of a data source
    /// </summary>
    public class MetaData
    {
        /// <summary>
        /// List of Tables
        /// </summary>
        [Browsable(false)]
        public List<MetaTable> Tables { get; set; } = new List<MetaTable>();
        /// <summary>
        /// Serialize Tables only if not empty
        /// </summary>
        public bool ShouldSerializeTables() { return Tables.Count > 0; }

        /// <summary>
        /// List of Table Links
        /// </summary>
        [Browsable(false)]
        public List<MetaTableLink> TableLinks { get; set; } = new List<MetaTableLink>();
        /// <summary>
        /// Serialize TableLinks only if not empty
        /// </summary>
        public bool ShouldSerializeTableLinks() { return TableLinks.Count > 0; }

        /// <summary>
        /// List of Joins
        /// </summary>
        [Browsable(false)]
        public List<MetaJoin> Joins { get; set; } = new List<MetaJoin>();
        /// <summary>
        /// Serialize Joins only if not empty
        /// </summary>
        public bool ShouldSerializeJoins() { return Joins.Count > 0; }

        /// <summary>
        /// List of Enumerated Lists
        /// </summary>
        [Browsable(false)]
        public List<MetaEnum> Enums { get; set; } = new List<MetaEnum>();
        /// <summary>
        /// Serialize Enums only if not empty
        /// </summary>
        public bool ShouldSerializeEnums() { return Enums.Count > 0; }


        /// <summary>
        /// All meta columns
        /// </summary>
        [XmlIgnore]
        public virtual List<MetaColumn> AllColumns
        {
            get
            {
                var result = new List<MetaColumn>();
                foreach (var table in AllTables)
                {
                    result.AddRange(table.Columns);
                }
                return result;
            }
        }

        /// <summary>
        /// All tables including links
        /// </summary>
        [XmlIgnore]
        public virtual List<MetaTable> AllTables
        {
            get
            {
                var result = new List<MetaTable>();
                result.AddRange(Tables);
                result.AddRange(from i in TableLinks select i.MetaTable);
                return result;
            }
        }



        /// <summary>
        /// Returns a column from its GUID
        /// </summary>
        public MetaColumn GetColumnFromGUID(string guid)
        {
            MetaColumn result = null;
            foreach (MetaTable table in AllTables)
            {
                result = table.Columns.FirstOrDefault(i => i.GUID == guid);
                if (result != null) break;
            }
            return result;
        }

        /// <summary>
        /// Returns a column from its name
        /// </summary>
        public MetaColumn GetColumnFromName(string tableName, string columnName)
        {
            MetaColumn result = null;
            MetaTable table = AllTables.FirstOrDefault(i => i.AliasName == tableName);
            if (table != null) result = table.Columns.FirstOrDefault(i => i.Name == columnName);
            return result;
        }

        /// <summary>
        /// Returns a column from its display path
        /// </summary>
        public MetaColumn GetColumnFromDisplayPath(string displayPath)
        {
            MetaColumn result = null;
            foreach (MetaTable table in AllTables)
            {
                result = table.Columns.FirstOrDefault(i => i.Category + "/" + i.DisplayName ==  displayPath);
                if (result != null) break;
            }
            return result;
        }
    }
}
