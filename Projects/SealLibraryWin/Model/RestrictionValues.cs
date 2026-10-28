//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Seal.Model
{
    /// <summary>
    /// Values of a restriction (operator, values, dates, date keywords and enumerated values) without its definition.
    /// Used to restore the initial values of the prompted restrictions and to store the restriction values of a web schedule.
    /// </summary>
    public class RestrictionValues
    {
        /// <summary>
        /// Identifier of the restriction
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// Name of the restriction (information only)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Operator of the restriction
        /// </summary>
        public Operator Operator { get; set; }

        /// <summary>
        /// First value
        /// </summary>
        public string Value1 { get; set; }
        /// <summary>
        /// Second value
        /// </summary>
        public string Value2 { get; set; }
        /// <summary>
        /// Third value
        /// </summary>
        public string Value3 { get; set; }
        /// <summary>
        /// Fourth value
        /// </summary>
        public string Value4 { get; set; }

        /// <summary>
        /// First date
        /// </summary>
        public DateTime Date1 { get; set; }
        /// <summary>
        /// Second date
        /// </summary>
        public DateTime Date2 { get; set; }
        /// <summary>
        /// Third date
        /// </summary>
        public DateTime Date3 { get; set; }
        /// <summary>
        /// Fourth date
        /// </summary>
        public DateTime Date4 { get; set; }

        /// <summary>
        /// Keyword of the first date (e.g. Today-1)
        /// </summary>
        public string Date1Keyword { get; set; }
        /// <summary>
        /// Keyword of the second date
        /// </summary>
        public string Date2Keyword { get; set; }
        /// <summary>
        /// Keyword of the third date
        /// </summary>
        public string Date3Keyword { get; set; }
        /// <summary>
        /// Keyword of the fourth date
        /// </summary>
        public string Date4Keyword { get; set; }

        /// <summary>
        /// Identifiers of the enumerated values selected
        /// </summary>
        public List<string> EnumValues { get; set; } = new List<string>();
        /// <summary>
        /// Xml serialization helper: serialize EnumValues only if not empty
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeEnumValues() { return EnumValues.Count > 0; }

        /// <summary>
        /// Xml serialization helpers: serialize the dates only if set
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeDate1() { return Date1 != DateTime.MinValue; }
        /// <summary>
        /// Xml serialization helper
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeDate2() { return Date2 != DateTime.MinValue; }
        /// <summary>
        /// Xml serialization helper
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeDate3() { return Date3 != DateTime.MinValue; }
        /// <summary>
        /// Xml serialization helper
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeDate4() { return Date4 != DateTime.MinValue; }

        /// <summary>
        /// Returns the current values of a restriction
        /// </summary>
        public static RestrictionValues FromRestriction(ReportRestriction restriction)
        {
            return new RestrictionValues()
            {
                GUID = restriction.GUID,
                Name = restriction.DisplayNameEl,
                Operator = restriction.Operator,
                Value1 = restriction.Value1,
                Value2 = restriction.Value2,
                Value3 = restriction.Value3,
                Value4 = restriction.Value4,
                Date1 = restriction.Date1,
                Date2 = restriction.Date2,
                Date3 = restriction.Date3,
                Date4 = restriction.Date4,
                Date1Keyword = restriction.Date1Keyword,
                Date2Keyword = restriction.Date2Keyword,
                Date3Keyword = restriction.Date3Keyword,
                Date4Keyword = restriction.Date4Keyword,
                EnumValues = restriction.EnumValues.ToList()
            };
        }

        /// <summary>
        /// Set the values to a restriction. If onlyAllowedOperator is true, the operator is changed only if the user can change it (operator style visible).
        /// </summary>
        public void ApplyTo(ReportRestriction restriction, bool onlyAllowedOperator)
        {
            if (!onlyAllowedOperator || restriction.OperatorStyle == RestrictionOperatorStyle.Visible || restriction.OperatorStyle == RestrictionOperatorStyle.VisibleWithNulls)
            {
                restriction.Operator = Operator;
            }
            restriction.Value1 = Value1;
            restriction.Value2 = Value2;
            restriction.Value3 = Value3;
            restriction.Value4 = Value4;
            restriction.Date1 = Date1;
            restriction.Date2 = Date2;
            restriction.Date3 = Date3;
            restriction.Date4 = Date4;
            restriction.Date1Keyword = Date1Keyword;
            restriction.Date2Keyword = Date2Keyword;
            restriction.Date3Keyword = Date3Keyword;
            restriction.Date4Keyword = Date4Keyword;
            restriction.EnumValues = EnumValues.ToList();
        }
    }
}
