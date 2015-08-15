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
using Seal.Helpers;
using System.Globalization;
using System.Reflection;

namespace Seal.Converter
{
    public class RestrictionOperatorConverter : NamedEnumConverter
    {
        public RestrictionOperatorConverter(Type type)
            : base(type)
        {
        }

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            ReportRestriction restriction = context.Instance as ReportRestriction;
            List<Operator> choices = new List<Operator>();

            if (restriction != null)
            {
                choices = restriction.AllowedOperators;
            }
            return new StandardValuesCollection(choices.ToArray());
        }
    }

}
