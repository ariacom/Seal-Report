//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;
using Seal.Model;

namespace Seal.Forms
{
    public class CssClassConverter : StringConverter
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
                choices.Add("cell-text");
                choices.Add("cell-datetime");
                choices.Add("cell-enum");
                choices.Add("cell-numeric");
                choices.Add("cell-bold");
                choices.Add("text-right");
                choices.Add("text-danger");
                choices.Add("text-info text-center");
                choices.Add("text-danger lead");
                choices.Add("text-success text-right");
                choices.Add("text-warning h2");
                choices.Add("text-justify");
                choices.Add("text-primary small");
                choices.Add("cell-numeric-DYNAMIC");
                choices.Add("cell-numeric-positive");
                choices.Add("cell-numeric-negative");
            }
            return new StandardValuesCollection(choices.ToArray());
        }
    }

}
