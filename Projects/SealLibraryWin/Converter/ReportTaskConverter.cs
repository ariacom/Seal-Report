//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Linq;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;
using System.Collections.Generic;

namespace Seal.Forms
{
    public class ReportTaskConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true; //true will limit to list. false will show the list, but allow free-form entry
        }

        List<ReportTask> getTaskList(PropertyDescriptor descriptor, Report report)
        {

            List<ReportTask> result = null;
            if (descriptor.Name == "ReferenceTaskGUID") result = report.AllTasks;
            else result = report.Tasks;
            return result;
        }


        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            string[] choices = new string[] { "No Task" };
            ReportTask reportTask = context.Instance as ReportTask;
            var report = reportTask?.Report;
            if (reportTask != null)
            {
                var list = getTaskList(context.PropertyDescriptor, report);
                var result = (from s in list where s.GUID != reportTask.GUID select s.Name).OrderBy(i => i).ToList();
                result.Insert(0, "");
                choices = result.ToArray();
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
                ReportTask reportTask = context.Instance as ReportTask;
                var report = reportTask?.Report;

                if (report != null && value != null)
                {
                    var list = getTaskList(context.PropertyDescriptor, report);
                    ReportTask task = list.FirstOrDefault(i => i.GUID == value.ToString());
                    if (task != null) return task.Name;
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
            ReportTask reportTask = context.Instance as ReportTask;
            var report = reportTask?.Report;

            if (report != null && value != null)
            {
                var list = getTaskList(context.PropertyDescriptor, report);
                ReportTask task = list.FirstOrDefault(i => i.Name == value.ToString());
                if (task != null) return task.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }
    }

}
