//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;
using Seal.Model;
using Seal.Helpers;
using System.IO;
using System.Windows.Diagnostics;
using System.Linq;
using System;
using System.Globalization;

namespace Seal.Forms
{
    public class DashboardOrderConverter : StringConverter
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
            var instance = context.Instance as SecurityDashboardOrder;
            List<string> choices = new List<string>();

            //public
            foreach (var dashboard in instance.SecurityGroup.Dashboards.OrderBy(i => i.Name))
            {
                choices.Add(dashboard.FullName);
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
                var instance = context.Instance as SecurityDashboardOrder;
                if (instance != null && value != null)
                {
                    Dashboard dashboard = instance.SecurityGroup.Dashboards.FirstOrDefault(i => i.GUID == value.ToString());
                    if (dashboard != null) return dashboard.FullName;
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
            var instance = context.Instance as SecurityDashboardOrder;
            if (instance != null && value != null)
            {
                Dashboard dashboard = instance.SecurityGroup.Dashboards.FirstOrDefault(i => i.FullName == value.ToString());
                if (dashboard != null) return dashboard.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }

    }
}
