//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Seal.Helpers;
using System.Reflection;
using Seal.Model;

namespace Seal.Converter
{
    class SerieDefinitionConverter : NamedEnumConverter
    {
        private Type _enumType;
        public SerieDefinitionConverter(Type type)
            : base(type)
        {
            _enumType = type;
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

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            Enum.GetNames(typeof(SerieDefinition));

            ReportElement element = context.Instance as ReportElement;
            List<SerieDefinition> choices = new List<SerieDefinition>();
            choices.Add(SerieDefinition.None);
            if (element != null && element.PivotPosition != PivotPosition.Page)
            {
                if (element.PivotPosition == PivotPosition.Data)
                {
                    /* not used from v4
                    choices.Add(SerieDefinition.Serie);
                    choices.Add(SerieDefinition.NVD3Serie);
                    */
                }
                else
                {
                    choices.Add(SerieDefinition.Axis);
                    choices.Add(SerieDefinition.Splitter);
                    //FUTURE ? choices.Add(SerieDefinition.SplitterBoth);
                }
            }
            return new StandardValuesCollection(choices.ToArray());
        }
    }
}
