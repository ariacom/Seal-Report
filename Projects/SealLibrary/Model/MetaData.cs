//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    public class MetaData
    {
        public static string MasterTableName = "SealMasterTable";

        private List<MetaTable> _tables = new List<MetaTable>();
        [Browsable(false)]
        public List<MetaTable> Tables
        {
            get { return _tables; }
            set { _tables = value; }
        }
        public bool ShouldSerializeTables() { return _tables.Count > 0; }

        private List<MetaJoin> _joins = new List<MetaJoin>();
        [Browsable(false)]
        public List<MetaJoin> Joins
        {
            get { return _joins; }
            set { _joins = value; }
        }
        public bool ShouldSerializeJoins() { return _joins.Count > 0; }

        private List<MetaEnum> _enums = new List<MetaEnum>();
        [Browsable(false)]
        public List<MetaEnum> Enums
        {
            get { return _enums; }
            set { _enums = value; }
        }
        public bool ShouldSerializeEnums() { return _enums.Count > 0; }

        public MetaColumn GetColumnFromGUID(string guid)
        {
            MetaColumn result = null;
            foreach (MetaTable table in Tables)
            {
                result = table.Columns.FirstOrDefault(i => i.GUID == guid);
                if (result != null) break;
            }
            return result;
        }

        public MetaColumn GetColumnFromName(string tableName, string columnName)
        {
            MetaColumn result = null;
            MetaTable table = Tables.FirstOrDefault(i => i.AliasName == tableName);
            if (table != null) result = table.Columns.FirstOrDefault(i => i.Name == columnName);
            return result;
        }

        public MetaColumn GetColumnFromDisplayPath(string displayPath)
        {
            MetaColumn result = null;
            foreach (MetaTable table in Tables)
            {
                result = table.Columns.FirstOrDefault(i => i.Category + "/" + i.DisplayName ==  displayPath);
                if (result != null) break;
            }
            return result;
        }

        [XmlIgnore]
        public MetaTable MasterTable
        {
            get
            {
                MetaTable result = Tables.FirstOrDefault(i => i.Alias == MasterTableName);
                return result;
            }
        }
    }
}
