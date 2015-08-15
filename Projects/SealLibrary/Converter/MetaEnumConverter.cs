//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Seal.Model;
using Seal.Helpers;
using System.Globalization;

namespace Seal.Converter
{
    internal class MetaEnumConverter : StringConverter
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
            choices.Add("");
            MetaColumn column = context.Instance as MetaColumn;
            if (column != null)
            {
                choices.AddRange(from s in column.Source.MetaData.Enums select s.Name);
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
                MetaColumn column = context.Instance as MetaColumn;
                if (column != null)
                {
                    if (value != null)
                    {
                        MetaEnum enumItem = column.Source.MetaData.Enums.FirstOrDefault(i => i.GUID == value.ToString());
                        if (enumItem != null) return enumItem.Name;
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
            if (column != null)
            {
                MetaEnum enumItem = column.Source.MetaData.Enums.FirstOrDefault(i => i.Name == value.ToString());
                if (enumItem != null) return enumItem.GUID;

            }
            return base.ConvertFrom(context, culture, value);
        }

    }
}
