//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;

namespace Seal.Forms
{
    public class MetaJoinConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true; //true will limit to list. false will show the list, but allow free-form entry
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> choices = new List<string>();
            MetaData metadata = null;
            if (context.Instance is ReportModel)
            {
                choices.Add("");
                metadata = ((ReportModel)context.Instance).Source.MetaData;
            }
            if (metadata != null)
            {
                choices.AddRange((from s in metadata.Joins select s.Name));
            }
            else
            {
                choices.Add("No Connection");
            }

            return new StandardValuesCollection(choices.OrderBy(i => i).ToList());
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
        {
            return destType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            if (context != null && value != null)
            {
                MetaData metadata = null;
                if (context.Instance is ReportModel) metadata = ((ReportModel)context.Instance).Source.MetaData;
                if (metadata != null)
                {
                    var join = metadata.Joins.FirstOrDefault(i => i.GUID == value.ToString());
                    if (join != null) return join.Name;
                }
            }
            return base.ConvertTo(context, culture, value, destType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType)
        {
            return srcType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            MetaData metadata = null;
            if (context.Instance is ReportModel) metadata = ((ReportModel)context.Instance).Source.MetaData;
            if (metadata != null)
            {
                var join = metadata.Joins.FirstOrDefault(i => i.Name == value.ToString());
                if (join != null) return join.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }

    }

}
