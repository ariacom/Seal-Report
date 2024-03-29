﻿//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Globalization;

namespace Seal.Forms
{
    /// <summary>
    /// Converter Objects
    /// </summary>
    internal class NamespaceDoc
    {
    }

    public class CultureInfoConverter : StringConverter
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
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(i => i.EnglishName))
            {
                choices.Add(culture.EnglishName);
            }
            return new StandardValuesCollection(choices);
        }   
    }

}
