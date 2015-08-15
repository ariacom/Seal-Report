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

namespace Seal.Converter
{
    public class CustomNameConverter : StringConverter
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
            ReportElement element = context.Instance as ReportElement;
            List<string> choices = new List<string>();
            choices.Add("");
            if (element != null)
            {
                choices.Add(element.DisplayNameEl);
                string choice = "";
                if (element.MetaColumn != null) choice = element.MetaColumn.DisplayName;
                if (!choices.Contains(choice)) choices.Add(choice);
                if (element.PivotPosition == PivotPosition.Data && element.MetaColumn != null) choice = element.RawDisplayName;
                if (!choices.Contains(choice)) choices.Add(choice);
            }
            return new StandardValuesCollection(choices.ToArray());
        }
    }

}
