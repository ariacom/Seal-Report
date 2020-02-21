//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Linq;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;
using System.Collections.Generic;
using Seal.Forms;

namespace Seal.Forms
{
    public class ReportViewConverter : StringConverter
    {
        Report getReport(ITypeDescriptorContext context)
        {
            Report report = context.Instance as Report;
            if (report == null && context.Instance is ReportComponent) report = ((ReportComponent)context.Instance).Report;
            if (report == null && TemplateTextEditor.CurrentEntity is Report) report = (Report)TemplateTextEditor.CurrentEntity;

            return report;
        }

        List<ReportView> getViewList(PropertyDescriptor descriptor, Report report)
        {

            List<ReportView> result = null;
            if (descriptor.Name == "ReferenceViewGUID") result = report.FullViewList;
            else result = report.Views;
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
            Report report = getReport(context);
            if (report != null)
            {
                var list = getViewList(context.PropertyDescriptor, report);
                choices = (from s in list select s.Name).ToList();
                if (context.PropertyDescriptor.Name == "ReferenceViewGUID" || context.PropertyDescriptor.Name == "ExecViewGUID")
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
                Report report = getReport(context);
                if (report != null && value != null)
                {
                    var list = getViewList(context.PropertyDescriptor, report);
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
            Report report = getReport(context);
            if (report != null && value != null)
            {
                var list = getViewList(context.PropertyDescriptor, report);
                ReportView view = list.FirstOrDefault(i => i.Name == value.ToString());
                if (view != null) return view.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }

    }

}
