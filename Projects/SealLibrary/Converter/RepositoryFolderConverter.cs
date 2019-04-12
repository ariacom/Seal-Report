//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;
using Seal.Model;
using Seal.Helpers;

namespace Seal.Converter
{
    public class RepositoryFolderConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            bool limit = true;
            if (context.Instance is ReportOutput) limit = false;
            return limit; //true will limit to list. false will show the list, but allow free-form entry
        }


        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> choices = new List<string>();
            string prefix = (context.Instance is ReportOutput ? Repository.SealRepositoryKeyword + "\\Reports" : "");
            choices.Add(prefix + "\\");
            FileHelper.AddFolderChoices(Repository.Instance.ReportsFolder, prefix, choices);

            return new StandardValuesCollection(choices);
        }   
    }

}
