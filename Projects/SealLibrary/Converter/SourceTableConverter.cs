//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;

namespace Seal.Converter
{
    public class SourceTableConverter : StringConverter
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
            if (context.Instance is MetaJoin)
            {
                metadata = ((MetaJoin)context.Instance).Source.MetaData;
            }
            if (context.Instance is ReportModel)
            {
                choices.Add("");
                metadata = ((ReportModel)context.Instance).Source.MetaData;
            }
            if (metadata != null)
            {
                choices.AddRange((from s in metadata.Tables select s.AliasName));
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
                if (context.Instance is MetaJoin) metadata = ((MetaJoin)context.Instance).Source.MetaData;
                if (context.Instance is ReportModel) metadata = ((ReportModel)context.Instance).Source.MetaData;
                if (metadata != null)
                {
                    MetaTable table = metadata.Tables.FirstOrDefault(i => i.GUID == value.ToString());
                    if (table != null) return table.AliasName;
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
            if (context.Instance is MetaJoin) metadata = ((MetaJoin)context.Instance).Source.MetaData;
            if (context.Instance is ReportModel) metadata = ((ReportModel)context.Instance).Source.MetaData;
            if (metadata != null)
            {
                MetaTable table = metadata.Tables.FirstOrDefault(i => i.AliasName == value.ToString());
                if (table != null) return table.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }

    }

}
