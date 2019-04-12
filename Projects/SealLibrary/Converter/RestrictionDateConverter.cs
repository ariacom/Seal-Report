//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;

namespace Seal.Converter
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
