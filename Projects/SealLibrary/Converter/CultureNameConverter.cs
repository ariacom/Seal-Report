//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;

namespace Seal.Converter
{
    public class CultureNameConverter : StringConverter
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
            ReportView view = context.Instance as ReportView;
            if (view != null)
            {
                choices.AddRange((from s in CultureInfo.GetCultures(CultureTypes.AllCultures) select s.EnglishName).OrderBy(i => i).ToArray());
            }

            return new StandardValuesCollection(choices);
        }
    }

}
