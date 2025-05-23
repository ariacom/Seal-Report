//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Collections.Generic;
using System.ComponentModel;
using Seal.Model;
using Seal.Helpers;
using System.IO;
using System.Linq;

namespace Seal.Forms
{
    public class SecurityGroupNameConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> choices = (from s in Repository.Instance.Security.Groups select s.GUID).ToList();
            return new StandardValuesCollection(choices);
        }   
    }

}
