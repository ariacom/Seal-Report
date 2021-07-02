//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;
using Seal.Model;

namespace Seal.Forms
{
    public class ScheduleRepeatConverter : StringConverter
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
            ReportSchedule schedule = context.Instance as ReportSchedule;
            List<string> choices = new List<string>();
            if (context.PropertyDescriptor.Name == "SealRepeatInterval")
            {
                choices.Add("None");
                choices.Add("20 seconds");
                choices.Add("5 minutes");
                choices.Add("10 minutes");
                choices.Add("30 minutes");
                choices.Add("1 hour");
                choices.Add("6 hours");
            }
            else if (context.PropertyDescriptor.Name == "SealRepeatDuration")
            {
                choices.Add("Indefinitely");
                choices.Add("15 minutes");
                choices.Add("30 minutes");
                choices.Add("1 hour");
                choices.Add("12 hours");
                choices.Add("1 day");
            }
            return new StandardValuesCollection(choices.ToArray());
        }
    }

}
