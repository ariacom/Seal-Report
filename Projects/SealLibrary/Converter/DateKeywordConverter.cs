//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Seal.Model;

namespace Seal.Converter
{
    public class DateKeywordConverter : StringConverter
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
            return new StandardValuesCollection(
                new string[] { "", DateRestrictionKeyword.Now.ToString(), 
                    DateRestrictionKeyword.Today.ToString(), DateRestrictionKeyword.Today.ToString() + "-1", 
                    DateRestrictionKeyword.ThisWeek.ToString(), DateRestrictionKeyword.ThisWeek.ToString() + "-1", 
                    DateRestrictionKeyword.ThisMonth.ToString(), DateRestrictionKeyword.ThisMonth.ToString() + "-1", 
                    DateRestrictionKeyword.ThisYear.ToString(), DateRestrictionKeyword.ThisYear.ToString() + "-1" });
        }
    }

}
