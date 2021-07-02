//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;
using Seal.Model;
using System.IO;

namespace Seal.Forms
{
    public class MenuNameConverter : StringConverter
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
            List<string> choices = new List<string>();
            if (context.Instance is ReportView)
            {
                var view = context.Instance as ReportView;
                choices.Add("");
                var path = Path.GetDirectoryName(view.Report.RelativeFilePath).Replace("\\", "/");
                choices.Add(path + "/" + view.Report.ExecutionName);
                if (view.Name != view.Report.ExecutionName) choices.Add(path + "/" + view.Name);
            }
            return new StandardValuesCollection(choices);
        }   
    }

}
