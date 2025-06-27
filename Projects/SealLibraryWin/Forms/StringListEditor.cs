//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Seal.Helpers;
using Seal.Model;

namespace Seal.Forms
{
    public class StringListEditor : UITypeEditor
    {
        class StringDisplay
        {
            public string Id { get; set; }
            public string Display { get; set; }
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (context.Instance is SecurityLogin)
                {
                    SecurityLogin login = context.Instance as SecurityLogin;
                    MultipleSelectForm frm = new MultipleSelectForm("Please select the values", Repository.Instance.Security.Groups, "Name");
                    //select existing values
                    for (int i = 0; i < frm.checkedListBox.Items.Count; i++)
                    {
                        if (login.GroupIds.Contains(((SecurityGroup)frm.checkedListBox.Items[i]).GUID)) frm.checkedListBox.SetItemChecked(i, true);
                    }

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        login.GroupIds = new List<string>();
                        foreach (object item in frm.CheckedItems)
                        {
                            login.GroupIds.Add(((SecurityGroup)item).GUID);
                        }
                        value = login.GroupIds; //indicates a modification
                    }
                }
                else if (context.Instance is SealServerConfiguration)
                {
                    SealServerConfiguration configuration = context.Instance as SealServerConfiguration;
                    if (context.PropertyDescriptor.Name == "ReportFormats")
                    {
                        var displaySource = new List<StringDisplay>();
                        foreach (var format in Repository.Instance.ResultAllFormats)
                        {
                            displaySource.Add(new StringDisplay() { Id = format.ToString(), Display = Helper.GetEnumDescription(typeof(ReportFormat), format) });
                        }


                        MultipleSelectForm frm = new MultipleSelectForm("Please select the formats", displaySource, "Display");
                        //select existing values
                        for (int i = 0; i < frm.checkedListBox.Items.Count; i++)
                        {
                            if (configuration.ReportFormats.Exists(j => j.ToString() == ((StringDisplay)frm.checkedListBox.Items[i]).Id)) frm.checkedListBox.SetItemChecked(i, true);
                        }

                        if (frm.ShowDialog() == DialogResult.OK)
                        {
                            var result = new List<string>();
                            foreach (object item in frm.CheckedItems)
                            {
                                result.Add(((StringDisplay)item).Id);
                            }
                            value = result;
                        }
                    }
                    else if (context.PropertyDescriptor.Name == "WebCultures")
                    {
                        var displaySource = new List<StringDisplay>();
                        foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(i => i.EnglishName))
                        {
                            displaySource.Add(new StringDisplay() { Id = culture.EnglishName, Display = string.Format("{0} ({1})", culture.EnglishName, culture.NativeName) });
                        }


                        MultipleSelectForm frm = new MultipleSelectForm("Please select the values", displaySource, "Display");
                        //select existing values
                        for (int i = 0; i < frm.checkedListBox.Items.Count; i++)
                        {
                            if (configuration.WebCultures.Exists(j => j == ((StringDisplay)frm.checkedListBox.Items[i]).Id)) frm.checkedListBox.SetItemChecked(i, true);
                        }

                        if (frm.ShowDialog() == DialogResult.OK)
                        {
                            var result = new List<string>();
                            foreach (object item in frm.CheckedItems)
                            {
                                result.Add(((StringDisplay)item).Id);
                            }
                            value = result;
                        }
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
