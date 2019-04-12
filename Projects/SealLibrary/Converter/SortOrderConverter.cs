//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Seal.Model;

namespace Seal.Converter
{
    public class SortOrderConverter : StringConverter
    {
        public const string kAutomaticAscSortKeyword = "Automatic Ascendant";
        public const string kAutomaticDescSortKeyword = "Automatic Descendant";
        public const string kNoSortKeyword = "Not sorted";
        public const string kAscendantSortKeyword = "Ascendant";
        public const string kDescendantSortKeyword = "Descendant";

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true; //true will limit to list. false will show the list, but allow free-form entry
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            ReportElement element = context.Instance as ReportElement;
            List<string> orders = new List<string>();
            if (element != null)
            {
                orders.Add(kAutomaticAscSortKeyword);
                orders.Add(kAutomaticDescSortKeyword);
                if (element.PivotPosition != PivotPosition.Page) orders.Add(kNoSortKeyword);
                IEnumerable<ReportElement> elements = element.Model.Elements.Where(i => i.PivotPosition == element.PivotPosition);
                for (int i = 1; i <= elements.Count(); i++)
                {
                    orders.Add(string.Format("{0} {1}", i, kAscendantSortKeyword));
                    orders.Add(string.Format("{0} {1}", i, kDescendantSortKeyword));
                }
            }
            return new StandardValuesCollection(orders.ToArray());
        }
    }
}
