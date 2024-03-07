//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Collections.Generic;
using System.ComponentModel;
using Seal.Model;

namespace Seal.Forms
{
    public class CssStyleConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false; //true will limit to list. false will show the list, but allow free-form entry
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            MetaColumn element = context.Instance as MetaColumn;
            List<string> choices = new List<string>();
            choices.Add("");
            if (element != null)
            {
                choices.Add("color:red;");
                choices.Add("font-size:20px;");
                choices.Add("font-weight:bold;color:red;");
            }
            return new StandardValuesCollection(choices.ToArray());
        }
    }

}
