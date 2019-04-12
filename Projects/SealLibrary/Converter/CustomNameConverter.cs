//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;
using Seal.Model;

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
