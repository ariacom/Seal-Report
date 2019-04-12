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
    public class ReportViewConverter : StringConverter
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
            string[] choices = new string[] { "No View" };
            ReportComponent component = context.Instance as ReportComponent;
            if (component != null)
            {
                choices = (from s in component.Report.Views select s.Name).ToArray();
            }

            return new StandardValuesCollection(choices);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
        {
            return destType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            if (context != null)
            {
                ReportComponent component = context.Instance as ReportComponent;
                if (component != null && value != null)
                {
                    ReportView view = component.Report.Views.FirstOrDefault(i => i.GUID == value.ToString());
                    if (view != null) return view.Name;
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
            ReportComponent component = context.Instance as ReportComponent;
            if (component != null && value != null)
            {
                ReportView view = component.Report.Views.FirstOrDefault(i => i.Name == value.ToString());
                if (view != null) return view.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }

    }

}
