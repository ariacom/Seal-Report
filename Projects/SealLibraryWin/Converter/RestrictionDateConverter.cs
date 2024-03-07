//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;

namespace Seal.Forms
{
    public class RestrictionDateConverter : DateTimeConverter
    {

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (context != null && value != null)
            {
                if (context.Instance is ReportRestriction && value is DateTime && ((DateTime)value) != DateTime.MinValue)
                {
                    var restr = (ReportRestriction)context.Instance;
                    return ((DateTime) value).ToString(restr.InputDateFormat, culture);
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

}
