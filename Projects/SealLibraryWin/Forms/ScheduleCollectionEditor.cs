//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms;
using Seal.Model;
using System.Globalization;
using System.Linq;

namespace Seal.Forms
{
    public class ScheduleCollectionEditor : UITypeEditor
    {
        SealSchedule _schedule = null;

        void setContext(ITypeDescriptorContext context)
        {
            _schedule = (context.Instance as ReportSchedule).SealSchedule;
        }


        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            setContext(context);
            return UITypeEditorEditStyle.Modal;
        }

        class IntDisplay
        {
            public int Id { get; set; }
            public string Display { get; set; }
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                setContext(context);
                if (context.Instance != null)
                {
                    var displaySource = new List<IntDisplay>();
                    int[] source = null;
                    int startIndex = 0;

                    var culture = new CultureInfo("en");
                    string[] names = new string[] { };
                    if (context.PropertyDescriptor.Name == "SealWeekdays")
                    {
                        names = culture.DateTimeFormat.DayNames;
                        startIndex = 0;
                        source = _schedule.Weekdays;
                    }
                    else if (context.PropertyDescriptor.Name == "SealMonths")
                    {
                        names = new List<string>(culture.DateTimeFormat.MonthNames).Take(12).ToArray();
                        startIndex = 1;
                        source = _schedule.Months;
                    }
                    else if (context.PropertyDescriptor.Name == "SealDays")
                    {
                        List<string> names2 = new List<string>();
                        for (int i = 1; i <= 31; i++) names2.Add(i.ToString());
                        names2.Add("Last");
                        names = names2.ToArray();
                        startIndex = 1;
                        source = _schedule.Days;
                    }

                    for (int i = 0; i < names.Length; i++)
                    {
                        displaySource.Add(new IntDisplay() { Id = startIndex + i, Display = names[i] });
                    }

                    MultipleSelectForm frm = new MultipleSelectForm("Please select the values", displaySource, "Display");
                    //select existing values
                    for (int i = 0; i < frm.checkedListBox.Items.Count; i++)
                    {
                        if (source.Contains(((IntDisplay)frm.checkedListBox.Items[i]).Id)) frm.checkedListBox.SetItemChecked(i, true);
                    }

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        var result = new List<int>();
                        foreach (object item in frm.CheckedItems)
                        {
                            result.Add(((IntDisplay)item).Id);
                        }
                        _schedule.CalculateNextExecution();
                        value = result.ToArray();
                    }
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

            return value;
        }
    }
}
