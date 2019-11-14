//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Linq;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;
using System.Collections.Generic;

namespace Seal.Converter
{
    public class ReportViewConverter : StringConverter
    {
        List<ReportView> getViewList(PropertyDescriptor descriptor, ReportComponent component) 
        {

            List<ReportView> result = null;
            if (descriptor.Name == "ReferenceViewGUID") result = component.Report.FullViewList.Where(i => i.GUID != component.GUID).ToList();
            else result = component.Report.Views;
            return result;
        }

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
            ReportComponent component = context.Instance as ReportComponent;
            if (component != null)
            {
                var list = getViewList(context.PropertyDescriptor, component);
                choices = (from s in list select s.Name).ToList();
                if (context.PropertyDescriptor.Name == "ReferenceViewGUID")
                {
                    choices.Insert(0, "");
                }
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
                ReportComponent component = context.Instance as ReportComponent;
                if (component != null && value != null)
                {
                    var list = getViewList(context.PropertyDescriptor, component);
                    ReportView view = list.FirstOrDefault(i => i.GUID == value.ToString());
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
                var list = getViewList(context.PropertyDescriptor, component);
                ReportView view = list.FirstOrDefault(i => i.Name == value.ToString());
                if (view != null) return view.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }

    }

}
