//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.ComponentModel;
using System.Xml.Serialization;
#if WINDOWS
using System.Drawing.Design;
using Seal.Forms;
using DynamicTypeDescriptor;
#endif

namespace Seal.Model
{
    /// <summary>
    /// Defines how a join of the Data Source is changed for a model: only the values specified replace the ones of the join.
    /// </summary>
    public class JoinOverride : RootEditor
    {
        static readonly string[] JoinTypeNames = new string[] { "Inner", "Left outer", "Right outer", "Cross" };

#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("UseJoin").SetIsBrowsable(true);
                GetProperty("ModelJoinType").SetIsBrowsable(Join != null && !Join.Source.IsNoSQL);
                GetProperty("ModelIsBiDirectional").SetIsBrowsable(true);
                GetProperty("ModelWeight").SetIsBrowsable(true);
                GetProperty("AdditionalClause").SetIsBrowsable(Join != null && !Join.Source.IsNoSQL);

                GetProperty("LeftTableName").SetIsBrowsable(true);
                GetProperty("RightTableName").SetIsBrowsable(true);
                GetProperty("JoinClause").SetIsBrowsable(true);
                GetProperty("JoinDefinition").SetIsBrowsable(true);

                //The default values are the values of the join in the Data Source: a value is shown in bold when it is changed for the model
                if (Join != null && !_defaultValuesSet)
                {
                    _defaultValuesSet = true;
                    GetProperty("ModelJoinType").DefaultValue = Join.JoinType;
                    GetProperty("ModelIsBiDirectional").DefaultValue = Join.IsBiDirectional;
                    GetProperty("ModelWeight").DefaultValue = Join.Weight;
                    GetProperty("LeftTableName").DefaultValue = LeftTableName;
                    GetProperty("RightTableName").DefaultValue = RightTableName;
                    GetProperty("JoinClause").DefaultValue = JoinClause;
                    GetProperty("JoinDefinition").DefaultValue = JoinDefinition;
                }

                TypeDescriptor.Refresh(this);
            }
        }
        bool _defaultValuesSet = false;
        bool _settingWeight = false;
        #endregion

#endif

        /// <summary>
        /// GUID of the join of the Data Source
        /// </summary>
        public string JoinGUID { get; set; }

        /// <summary>
        /// Join of the Data Source
        /// </summary>
        [XmlIgnore]
        public MetaJoin Join = null;

        /// <summary>
        /// If true, the join is not used for the model
        /// </summary>
        public bool Exclude { get; set; } = false;
        /// <summary>
        /// Serialize Exclude only if true
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeExclude() { return Exclude; }

        /// <summary>
        /// If specified, the type of join used for the model. Left and right are the left and right tables defined in the join.
        /// </summary>
        public JoinType? JoinType { get; set; }
        /// <summary>
        /// Serialize JoinType only if specified
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeJoinType() { return JoinType.HasValue; }

        /// <summary>
        /// If specified, indicates if the join can be used in both directions for the model
        /// </summary>
        public bool? IsBiDirectional { get; set; }
        /// <summary>
        /// Serialize IsBiDirectional only if specified
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeIsBiDirectional() { return IsBiDirectional.HasValue; }

        /// <summary>
        /// If specified, the weight of the join for the model
        /// </summary>
        public int? Weight { get; set; }
        /// <summary>
        /// Serialize Weight only if specified
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeWeight() { return Weight.HasValue; }

        /// <summary>
        /// If specified, SQL added with an AND to the join clause for the model (SQL joins only). For an outer join, this allows to restrict the outer table without losing the rows of the other table.
        /// </summary>
#if WINDOWS
        [DefaultValue("")]
        [Category("Join for the model"), DisplayName("Additional clause"), Description("SQL added with an AND to the join clause for this model. For an outer join, this allows to restrict the outer table without losing the rows of the other table (e.g. Orders.OrderDate >= '2024-01-01' for a left outer join between Customers and Orders). The SQL may contain common restrictions or values with the '{CommonRestriction_' or '{CommonValue_' keywords."), Id(5, 1)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
#endif
        public string AdditionalClause
        {
            get { return _additionalClause ?? ""; }
            set { _additionalClause = value; }
        }
        string _additionalClause;
        /// <summary>
        /// Serialize AdditionalClause only if specified
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeAdditionalClause() { return !string.IsNullOrWhiteSpace(AdditionalClause); }

        #region Editor properties

        /// <summary>
        /// Editor helper: true if the join is used for the model
        /// </summary>
#if WINDOWS
        [DefaultValue(true)]
        [Category("Join for the model"), DisplayName("Use the join"), Description("If false, the join is not used to link the tables of this model."), Id(1, 1)]
#endif
        [XmlIgnore]
        public bool UseJoin
        {
            get { return !Exclude; }
            set { Exclude = !value; }
        }

        /// <summary>
        /// Editor helper: type of join for the model. The default value is the type of the join in the Data Source.
        /// </summary>
#if WINDOWS
        [Category("Join for the model"), DisplayName("Join type"), Description("The type of join used for this model. Left and right are the left and right tables of the join: a left outer join keeps all the rows of the left table. The default value is the type defined in the Data Source."), Id(2, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
#endif
        [XmlIgnore]
        public JoinType ModelJoinType
        {
            get { return JoinType ?? (Join != null ? Join.JoinType : Seal.Model.JoinType.Inner); }
            set { JoinType = (Join != null && value == Join.JoinType) ? (JoinType?)null : value; }
        }

        /// <summary>
        /// Editor helper: bi-directional flag for the model. The default value is the flag of the join in the Data Source.
        /// </summary>
#if WINDOWS
        [Category("Join for the model"), DisplayName("Is bi-directional"), Description("Indicates if the join can be used in both directions (left-right or right-left) for this model. The default value is the value defined in the Data Source."), Id(3, 1)]
#endif
        [XmlIgnore]
        public bool ModelIsBiDirectional
        {
            get { return IsBiDirectional ?? (Join != null ? Join.IsBiDirectional : true); }
            set { IsBiDirectional = (Join != null && value == Join.IsBiDirectional) ? (bool?)null : value; }
        }

        /// <summary>
        /// Editor helper: weight for the model. The default value is the weight of the join in the Data Source.
        /// </summary>
#if WINDOWS
        [Category("Join for the model"), DisplayName("Weight"), Description("Weight of the join (from 1 to 1000) for this model: the joins chosen are the ones linking all the tables of the model with the minimal total weight. A join having a higher weight is avoided when another path exists. The default value is the weight defined in the Data Source."), Id(4, 1)]
#endif
        [XmlIgnore]
        public int ModelWeight
        {
            get { return Weight ?? (Join != null ? Join.Weight : 1); }
            set
            {
                int weight = Math.Min(1000, Math.Max(1, value));
                Weight = (Join != null && weight == Join.Weight) ? (int?)null : weight;
#if WINDOWS
                //The value entered may be out of range: set the final value again as it is used to show the value in bold
                if (_dctd != null && !_settingWeight && value != weight)
                {
                    _settingWeight = true;
                    try { GetProperty("ModelWeight").SetValue(this, weight); }
                    finally { _settingWeight = false; }
                }
#endif
            }
        }

        /// <summary>
        /// Editor helper: left table of the join
        /// </summary>
#if WINDOWS
        [Category("Join of the Data Source"), DisplayName("Left table"), Description("Left table of the join."), Id(1, 2)]
#endif
        [XmlIgnore]
        public string LeftTableName
        {
            get { return Join != null && Join.LeftTable != null ? Join.LeftTable.AliasName : ""; }
        }

        /// <summary>
        /// Editor helper: right table of the join
        /// </summary>
#if WINDOWS
        [Category("Join of the Data Source"), DisplayName("Right table"), Description("Right table of the join."), Id(2, 2)]
#endif
        [XmlIgnore]
        public string RightTableName
        {
            get { return Join != null && Join.RightTable != null ? Join.RightTable.AliasName : ""; }
        }

        /// <summary>
        /// Editor helper: clause of the join
        /// </summary>
#if WINDOWS
        [Category("Join of the Data Source"), DisplayName("Join clause"), Description("Clause of the join defined in the Data Source."), Id(3, 2)]
#endif
        [XmlIgnore]
        public string JoinClause
        {
            get { return Join != null ? Join.Clause.Trim() : ""; }
        }

        /// <summary>
        /// Editor helper: type, bi-directional flag and weight of the join in the Data Source
        /// </summary>
#if WINDOWS
        [Category("Join of the Data Source"), DisplayName("Definition"), Description("Type, bi-directional flag and weight of the join defined in the Data Source: these are the default values for the model."), Id(4, 2)]
#endif
        [XmlIgnore]
        public string JoinDefinition
        {
            get { return Join != null ? string.Format("{0}, {1}, weight {2}", JoinTypeNames[(int)Join.JoinType], Join.IsBiDirectional ? "bi-directional" : "not bi-directional", Join.Weight) : ""; }
        }

        /// <summary>
        /// Editor helper: text displayed in the list of joins
        /// </summary>
        [XmlIgnore]
        public string DisplayText
        {
            get
            {
                var result = Join != null ? Join.Name : JoinGUID + " (join not found)";
                if (!IsEmpty) result += "  [" + GetDescription() + "]";
                return result;
            }
        }

        #endregion

        /// <summary>
        /// True if nothing is changed for the join
        /// </summary>
        [XmlIgnore]
        public bool IsEmpty
        {
            get { return !Exclude && !JoinType.HasValue && !IsBiDirectional.HasValue && !Weight.HasValue && string.IsNullOrWhiteSpace(AdditionalClause); }
        }

        /// <summary>
        /// Returns a copy of the join with the values changed for the model. The join of the Data Source is never modified as it is shared by all the reports.
        /// </summary>
        public MetaJoin GetJoin(MetaJoin join, bool isLINQ)
        {
            var result = MetaJoin.Create();
            result.GUID = join.GUID;
            result.Name = join.Name;
            result.Description = join.Description;
            result.Source = join.Source;
            result.IsEditable = join.IsEditable;
            result.LeftTableGUID = join.LeftTableGUID;
            result.RightTableGUID = join.RightTableGUID;
            result.JoinType = JoinType ?? join.JoinType;
            result.IsBiDirectional = IsBiDirectional ?? join.IsBiDirectional;
            result.Weight = Weight ?? join.Weight;
            result.Clause = join.Clause;
            if (!isLINQ && result.JoinType != Seal.Model.JoinType.Cross && !string.IsNullOrWhiteSpace(AdditionalClause))
            {
                result.Clause = string.IsNullOrWhiteSpace(join.Clause) ? AdditionalClause.Trim() : string.Format("({0}) AND ({1})", join.Clause.Trim(), AdditionalClause.Trim());
            }
            return result;
        }

        /// <summary>
        /// Description of the values changed
        /// </summary>
        public string GetDescription()
        {
            if (Exclude) return "not used";
            var result = "";
            if (JoinType.HasValue) result += (result != "" ? ", " : "") + JoinTypeNames[(int)JoinType.Value];
            if (IsBiDirectional.HasValue) result += (result != "" ? ", " : "") + (IsBiDirectional.Value ? "bi-directional" : "not bi-directional");
            if (Weight.HasValue) result += (result != "" ? ", " : "") + "weight " + Weight.Value;
            if (!string.IsNullOrWhiteSpace(AdditionalClause)) result += (result != "" ? ", " : "") + "additional clause";
            return result;
        }
    }
}
