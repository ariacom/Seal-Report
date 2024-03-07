//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Seal.Model;

namespace Seal.Forms
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
