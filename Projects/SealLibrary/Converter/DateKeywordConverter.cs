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
