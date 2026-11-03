//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.ComponentModel;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// Show total choices: a Row element only supports the total row
    /// </summary>
    class ShowTotalConverter : NamedEnumConverter
    {
        public ShowTotalConverter(Type type) : base(type)
        {
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (context?.Instance is ReportElement element && element.PivotPosition == PivotPosition.Row)
            {
                return new StandardValuesCollection(new ShowTotal[] { ShowTotal.No, ShowTotal.Column });
            }
            return base.GetStandardValues(context);
        }
    }
}
