//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;

namespace Seal.Converter
{
    public class ViewParameterEnumConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            if (!(context.Instance is ParametersEditor) && !(context.Instance is Parameter)) return false;
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            if (context.Instance is Parameter) return ((Parameter)context.Instance).UseOnlyEnumValues;
            if (context.Instance is ParametersEditor) return ((ParametersEditor)context.Instance).GetParameter(context.PropertyDescriptor.Name).UseOnlyEnumValues;
            return false;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if ((context.Instance is Parameter)) return new StandardValuesCollection(((Parameter)context.Instance).EnumValues);
            if ((context.Instance is ParametersEditor)) return new StandardValuesCollection(((ParametersEditor)context.Instance).GetParameter(context.PropertyDescriptor.Name).EnumValues);         
            return new StandardValuesCollection(new string[] { });
        }


        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
        {
            return destType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            if (context != null && value != null)
            {
                Parameter parameter = null;
                if (context.Instance is Parameter) parameter = (Parameter)context.Instance;
                else parameter = ((ParametersEditor)context.Instance).GetParameter(context.PropertyDescriptor.Name);
                if (parameter != null) return (parameter.EnumGetDisplayFromValue(value.ToString()));
            }
            return base.ConvertTo(context, culture, value, destType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType)
        {
            return srcType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            Parameter parameter = null;
            if (context.Instance is Parameter) parameter = (Parameter)context.Instance;
            else parameter = ((ParametersEditor)context.Instance).GetParameter(context.PropertyDescriptor.Name);
            if (parameter != null) return (parameter.EnumGetValueFromDisplay(value.ToString()));
            return base.ConvertFrom(context, culture, value);
        }

    }

}
