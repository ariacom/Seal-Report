//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;

namespace Seal.Forms
{
    internal class MetaEnumConverter : StringConverter
    {
        string noEnumLabel = "<No Enumerated List>";
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
            choices.Add("");

            MetaColumn column = context.Instance as MetaColumn;
            if (column != null && column.Source != null)
            {
                //Add no enum for element or restriction having a meta column
                if (column is ReportElement && ((ReportElement)column).MetaColumn != null) choices.Add(noEnumLabel);
                choices.AddRange(from s in column.Source.MetaData.Enums select s.Name);
            }
            else
            {
                var element = context.Instance as ReportElement;
                if (element != null && element.Report != null)
                {
                    foreach (var source in element.Report.Sources)
                    {
                        choices.AddRange(from s in source.MetaData.Enums select s.Name);
                    }
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
                MetaColumn column = context.Instance as MetaColumn;
                if (column != null && column.Source != null)
                {
                    if (value != null)
                    {
                        if (value.ToString() == ReportElement.kClearEnumGUID) return noEnumLabel;
                        MetaEnum enumItem = column.Source.MetaData.Enums.FirstOrDefault(i => i.GUID == value.ToString());
                        if (enumItem != null) return enumItem.Name;
                    }
                }
                else
                { //For input values
                    var element = context.Instance as ReportElement;
                    if (element != null && element.Report != null)
                    {
                        if (value != null)
                        {
                            foreach (var source in element.Report.Sources)
                            {
                                MetaEnum enumItem = source.MetaData.Enums.FirstOrDefault(i => i.GUID == value.ToString());
                                if (enumItem != null) return enumItem.Name;
                            }
                        }
                    }
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
            MetaColumn column = context.Instance as MetaColumn;
            if (column != null && column.Source != null)
            {
                if (value.ToString() == noEnumLabel) return ReportElement.kClearEnumGUID;

                MetaEnum enumItem = column.Source.MetaData.Enums.FirstOrDefault(i => i.Name == value.ToString());
                if (enumItem != null) return enumItem.GUID;

            }
            else
            { //For input values
                var element = context.Instance as ReportElement;
                if (element != null && element.Report != null)
                {
                    if (value != null)
                    {
                        foreach (var source in element.Report.Sources)
                        {
                            MetaEnum enumItem = source.MetaData.Enums.FirstOrDefault(i => i.Name == value.ToString());
                            if (enumItem != null) return enumItem.GUID;
                        }
                    }
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

    }
}
