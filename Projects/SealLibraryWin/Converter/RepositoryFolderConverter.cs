//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;
using Seal.Model;
using Seal.Helpers;
using System.IO;

namespace Seal.Forms
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
            if (context.Instance is ReportOutput && ((ReportOutput)context.Instance).Device is OutputFolderDevice) limit = false;
            return limit; //true will limit to list. false will show the list, but allow free-form entry
        }


        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> choices = new List<string>();
            string prefix = "";
            if (context.Instance is ReportOutput)
            {
                var output = context.Instance as ReportOutput;
                if (output.Device is OutputFileServerDevice)
                {
                    //List of subfolders defined
                    return new StandardValuesCollection(((OutputFileServerDevice)output.Device).DirectoriesArray);
                }
                else
                {
                    prefix = Repository.SealRepositoryKeyword + Path.DirectorySeparatorChar + "Reports";
                }
            }
            choices.Add(prefix + Path.DirectorySeparatorChar);
            FileHelper.AddFolderChoices(Repository.Instance.ReportsFolder, prefix, choices);

            return new StandardValuesCollection(choices);
        }   
    }

}
