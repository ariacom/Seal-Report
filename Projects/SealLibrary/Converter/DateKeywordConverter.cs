//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
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
                    DateRestrictionKeyword.Today.ToString(), DateRestrictionKeyword.Today.ToString() + "-1D", 
                    DateRestrictionKeyword.ThisWeek.ToString(), DateRestrictionKeyword.ThisWeek.ToString() + "-1W", 
                    DateRestrictionKeyword.ThisMonth.ToString(), DateRestrictionKeyword.ThisMonth.ToString() + "-1M",
                    DateRestrictionKeyword.ThisYear.ToString(), DateRestrictionKeyword.ThisYear.ToString() + "-1Y",
                    DateRestrictionKeyword.ThisQuarter.ToString(), DateRestrictionKeyword.ThisQuarter.ToString() + "-1Q",
                    DateRestrictionKeyword.ThisSemester.ToString(), DateRestrictionKeyword.ThisSemester.ToString() + "-1S",
                    DateRestrictionKeyword.ThisMinute.ToString(), DateRestrictionKeyword.ThisMinute.ToString() + "-1m", DateRestrictionKeyword.ThisMinute.ToString() + "-30s",
                    DateRestrictionKeyword.ThisHour.ToString(), DateRestrictionKeyword.ThisHour.ToString() + "-1h",
                    DateRestrictionKeyword.Now.ToString() + "+1.5s -2m +3h -4D +5W -6M +7Q -8S +9Y"
                });
        }
    }

}
