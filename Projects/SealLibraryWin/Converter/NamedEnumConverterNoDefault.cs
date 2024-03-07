//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.ComponentModel;
using System.Globalization;
using Seal.Helpers;
using System.Reflection;
using System.Collections.Generic;

namespace Seal.Forms
{
    public class NamedEnumConverterNoDefault : EnumConverter
    {
        private Type _enumType;
        public NamedEnumConverterNoDefault(Type type) : base(type)
        {
            _enumType = type;
        }


        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            int index = 0;
            var choices = new List<int>();

            foreach (var val in Enum.GetValues(_enumType))
            {
                if (val.ToString() != "Default") choices.Add(index);
                index++;
            }
            return new StandardValuesCollection(choices);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
        {
            return destType == typeof(string);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            return Helper.GetEnumDescription(_enumType, value);
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType)
        {
            return srcType == typeof(string);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            foreach (FieldInfo fi in _enumType.GetFields())
            {
                DescriptionAttribute dna = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
                if ((dna != null) && ((string)value == dna.Description)) return Enum.Parse(_enumType, fi.Name);
            }
            return Enum.Parse(_enumType, (string)value);
        }
    }
}
