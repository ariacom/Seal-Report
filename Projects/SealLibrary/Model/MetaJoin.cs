//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Seal.Converter;
using System.Xml.Serialization;
using System.Drawing.Design;
using Seal.Forms;
using DynamicTypeDescriptor;
using Seal.Helpers;

namespace Seal.Model
{
    public class MetaJoin : RootComponent
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
                GetProperty("LeftTableGUID").SetIsBrowsable(true);
                GetProperty("RightTableGUID").SetIsBrowsable(true);
                GetProperty("Clause").SetIsBrowsable(true);
                GetProperty("IsBiDirectional").SetIsBrowsable(true);
                GetProperty("JoinType").SetIsBrowsable(true);

                GetProperty("HelperCheckJoin").SetIsBrowsable(true);
                GetProperty("Information").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);

                GetProperty("HelperCheckJoin").SetIsReadOnly(true);
                GetProperty("Information").SetIsReadOnly(true);
                GetProperty("Error").SetIsReadOnly(true);
                GetProperty("Clause").SetIsReadOnly(_joinType == JoinType.Cross);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        public static MetaJoin Create()
        {
            return new MetaJoin() { GUID = Guid.NewGuid().ToString() };
        }

        bool _isAutoName = false;

        string getAutoName()
        {
            if (RightTable != null && LeftTable != null) return LeftTable.Name + " - " + RightTable.Name;
            return Repository.JoinAutoName;
        }

        [DefaultValue(null)]
        [Category("Definition"), DisplayName("Name"), Description("Name of the join. If reset to an Empty String, the name is built using the table names."), Id(1, 1)]
        public override string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (_dctd != null && string.IsNullOrEmpty(_name)) {
                    _name = getAutoName();
                }
            }
        }

        private string _leftTableGUID;
        [DefaultValue(null)]
        [Category("Definition"), DisplayName("Left Table"), Description("Left table for the join definition."), Id(4, 1)]
        [TypeConverter(typeof(SourceTableConverter))]
        public string LeftTableGUID
        {
            get { return _leftTableGUID; }
            set
            {
                if (_dctd != null) _isAutoName = (Name == Repository.JoinAutoName || string.IsNullOrEmpty(Name) || getAutoName() == Name);

                _leftTable = null;
                _leftTableGUID = value;

                if (_dctd != null && _isAutoName) _name = getAutoName();
            }
        }


        MetaTable _leftTable = null;
        [XmlIgnore]
        public MetaTable LeftTable
        {
            get
            {
                if (_leftTable == null) _leftTable = _source.MetaData.Tables.FirstOrDefault(i => i.GUID == _leftTableGUID);
                return _leftTable;
            }
        }

        private string _rightTableGUID;
        [DefaultValue(null)]
        [Category("Definition"), DisplayName("Right Table"), Description("Right table for the join definition."), Id(5, 1)]
        [TypeConverter(typeof(SourceTableConverter))]
        public string RightTableGUID
        {
            get { return _rightTableGUID; }
            set
            {
                if (_dctd != null) _isAutoName = (Name == Repository.JoinAutoName || string.IsNullOrEmpty(Name) || getAutoName() == Name);

                _rightTable = null;
                _rightTableGUID = value;

                if (_dctd != null && _isAutoName) _name = getAutoName();
            }
        }

        MetaTable _rightTable = null;
        [XmlIgnore]
        public MetaTable RightTable
        {
            get
            {
                if (_rightTable == null) _rightTable = _source.MetaData.Tables.FirstOrDefault(i => i.GUID == _rightTableGUID);
                return _rightTable;
            }
        }

        private string _clause;
        [Category("Definition"), DisplayName("SQL Clause"), Description("SQL Clause used to define the join between the 2 tables."), Id(6, 1)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
        public string Clause
        {
            get { return _clause == null ? "" : _clause; }
            set { _clause = value; }
        }
        public bool ShouldSerializeClause() { return !string.IsNullOrEmpty(_clause); }

        private JoinType _joinType = JoinType.Inner;
        [DefaultValue(JoinType.Inner)]
        [Category("Definition"), DisplayName("Join Type"), Description("The type of join used to link the 2 tables."), Id(2, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public JoinType JoinType
        {
            get { return _joinType; }
            set { _joinType = value; UpdateEditorAttributes(); }
        }

        private bool _isBiDirectional = true;
        [DefaultValue(true)]
        [Category("Definition"), DisplayName("Is Bi-Directional"), Description("Indicates if the join can also be used in the other direction (left-right or right-left)."), Id(3, 1)]
        public bool IsBiDirectional
        {
            get { return _isBiDirectional; }
            set { _isBiDirectional = value; }
        }

        [XmlIgnore]
        public string SQLJoinType
        {
            get
            {
                if (_joinType == JoinType.Inner) return "INNER JOIN";
                else if (_joinType == JoinType.LeftOuter) return "LEFT OUTER JOIN";
                else if (_joinType == JoinType.RightOuter) return "RIGHT OUTER JOIN";
                return "CROSS JOIN";
            }
        }

        public void CheckJoin()
        {
            _error = "";
            _information = "";
            try
            {
                if (LeftTable == null) throw new Exception("Please select a Left table for the join");
                if (RightTable == null) throw new Exception("Please select a Right table for the join");
                if (_joinType != JoinType.Cross && string.IsNullOrEmpty(Clause.Trim())) throw new Exception("Please enter a SQL statement for the join");

                string CTE1 = "", name1 = "", CTE2 = "", name2 = "";
                LeftTable.GetExecSQLName(ref CTE1, ref name1);
                RightTable.GetExecSQLName(ref CTE2, ref name2);

                string CTE = Helper.AddCTE(CTE1, CTE2);
                string sql = string.Format("{0}SELECT * FROM {1}\r\n", CTE, name1);

                if (_joinType != JoinType.Cross) sql += string.Format("{0} {1} ON {2}\r\n", SQLJoinType, name2, Clause.Trim());
                else sql += string.Format("{0} {1}\r\n", SQLJoinType, name2);
                sql += "WHERE 0=1";
                _error = Source.CheckSQL(sql, new List<MetaTable>() { LeftTable, RightTable }, null, false);
                if (!string.IsNullOrEmpty(_error)) _information = "Error got when checking join. Please check the SQL:\r\n" + sql;
                else _information = "Join checked successfully.";
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _information = "Error got when checking the join.";
            }
            _information = Helper.FormatMessage(_information);
            UpdateEditorAttributes();
        }

        [XmlIgnore]
        public bool IsEditable = true;

        protected MetaSource _source;
        [XmlIgnore, Browsable(false)]
        public MetaSource Source
        {
            get { return _source; }
            set
            {
                _source = value;
            }
        }


        #region Helpers


        [Category("Helpers"), DisplayName("Check join"), Description("Check the join in the database."), Id(1, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperCheckJoin
        {
            get { return "<Click to check the join in the database>"; }
        }

        string _information;
        [XmlIgnore, Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(2, 10)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
        public string Information
        {
            get { return _information; }
            set { _information = value; }
        }

        string _error;
        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(3, 10)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        #endregion


    }
}
