//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Linq;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;

namespace Seal.Converter
{
    public class MetaSourceConverter : StringConverter
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
            string[] choices = new string[] { "No Source" };
            var component = context.Instance as ReportComponent;
            if (component != null)
            {
                //No SQL Source are not allowed for SQL Model
                bool useNoSQL = true;
                if (context.Instance is ReportModel)
                {
                    if (((ReportModel)context.Instance).IsSQLModel) useNoSQL = false;
                }
                choices = (from s in component.Report.Sources.Where(i => (i.IsNoSQL && useNoSQL) || !i.IsNoSQL) select s.Name).ToArray();
            }

            return new StandardValuesCollection(choices.OrderBy(i => i).ToList());
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
        {
            return destType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            if (context != null)
            {
                var component = context.Instance as ReportComponent;
                if (component != null && value != null)
                {
                    ReportSource source = component.Report.Sources.FirstOrDefault(i => i.GUID == value.ToString());
                    if (source != null) return source.Name;
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
            var component = context.Instance as ReportComponent;
            if (component != null && value != null)
            {
                ReportSource source = component.Report.Sources.FirstOrDefault(i => i.Name == value.ToString());
                if (source != null) return source.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }

    }

}
