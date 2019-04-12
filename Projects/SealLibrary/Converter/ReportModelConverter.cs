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
    public class ReportModelConverter : StringConverter
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
            string[] choices = new string[] { "No Model" };
            ReportView view = context.Instance as ReportView;
            if (view != null)
            {
                choices = (from s in view.Report.Models select s.Name).OrderBy(i => i).ToArray();
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
                ReportView view = context.Instance as ReportView;
                if (view != null && value != null)
                {
                    ReportModel model = view.Report.Models.FirstOrDefault(i => i.GUID == value.ToString());
                    if (model != null) return model.Name;
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
            ReportView view = context.Instance as ReportView;
            if (view != null && value != null)
            {
                ReportModel model = view.Report.Models.FirstOrDefault(i => i.Name == value.ToString());
                if (model != null) return model.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }
    }

}
