//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Seal.Model;

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
