//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                    choices.Add(SerieDefinition.Serie);
                    choices.Add(SerieDefinition.NVD3Serie);
                }
                else
                {
                    choices.Add(SerieDefinition.Axis);
                    choices.Add(SerieDefinition.Splitter);
                    choices.Add(SerieDefinition.SplitterBoth);
                }
            }
            return new StandardValuesCollection(choices.ToArray());
        }
    }
}
