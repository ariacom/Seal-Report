//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Seal.Model;

namespace Seal.Converter
{
    public class ViewParameterEnumConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            if (!(context.Instance is Parameter)) return false;
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            if (!(context.Instance is Parameter)) return false;
            return ((Parameter) context.Instance).UseOnlyEnumValues; //true will limit to list. false will show the list, but allow free-form entry
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (!(context.Instance is Parameter)) return new StandardValuesCollection(new string[] {});
            Parameter parameter = context.Instance as Parameter;
            return new StandardValuesCollection(parameter.Enums);
        }
    }

}
