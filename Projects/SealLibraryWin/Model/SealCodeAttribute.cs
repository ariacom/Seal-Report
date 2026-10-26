//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;

namespace Seal.Model
{
    /// <summary>
    /// Marks a property (SQL or Razor script) or a class (e.g. a connection defined in a report) with the <see cref="ReportCodeLevel"/> required to write it in a report.
    /// Checked by <see cref="Seal.Helpers.ReportCodeGuard"/> when a report is saved by a web user or an AI Agent.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SealCodeAttribute : Attribute
    {
        /// <summary>
        /// The level required to write the content
        /// </summary>
        public ReportCodeLevel Level { get; }

        /// <summary>
        /// If false, the attribute is not applied to the classes inheriting the declaring class (e.g. MetaColumn.Name for ReportElement, whose Name is not used in the SQL)
        /// </summary>
        public bool ApplyToDerived { get; set; } = true;

        /// <summary>
        /// If true, the text is rendered as HTML without encoding: active HTML content (script, frame, embedded object, event handler, script URL) requires the Scripts level
        /// </summary>
        public bool Html { get; set; } = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public SealCodeAttribute(ReportCodeLevel level)
        {
            Level = level;
        }
    }
}
