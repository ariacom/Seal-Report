//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using Seal.Model;

namespace Seal.Forms
{
    public class DataSourcesSelector : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (context.Instance is ReportView && context.PropertyDescriptor.IsReadOnly) return UITypeEditorEditStyle.None;
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var svc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if (svc != null)
            {
                MetaSource source = context.Instance as MetaSource;
                List<string> vals = value as List<string>;
                if (source != null && vals != null)
                {
                    List<MetaSource> sources = new List<MetaSource>();
                    foreach (var s in source.Repository.Sources.Where(i => i.GUID != source.GUID && i.IsNoSQL == source.IsNoSQL))
                    {
                        if (!context.PropertyDescriptor.IsReadOnly || vals.Contains(s.GUID)) sources.Add(s);
                    }

                    var frm = new MultipleSelectForm("Select the Reference Data Sources", sources, "Name");
                    //select existing values
                    for (int i = 0; i < frm.checkedListBox.Items.Count; i++)
                    {
                        if (vals.Contains(((MetaSource)frm.checkedListBox.Items[i]).GUID)) frm.checkedListBox.SetItemChecked(i, true);
                    }

                    if (context.PropertyDescriptor.IsReadOnly)
                    {
                        frm.Text = "Reference Data Sources";
                        frm.okToolStripButton.Visible = false;
                        frm.cancelToolStripButton.Text = "Close";
                        frm.checkedListBox.Enabled = false;
                        frm.selectAllToolStripButton.Visible = false;
                    }

                    if (svc.ShowDialog(frm) == DialogResult.OK)
                    {
                        vals.Clear();
                        foreach (object item in frm.CheckedItems)
                        {
                            vals.Add(((MetaSource)item).GUID);
                        }
                        source.UpdateEditor();

                        if (HelperEditor.HandlerInterface != null) HelperEditor.HandlerInterface.SetModified();

                        MessageBox.Show("Please Save and Reload the Data Source to update the References.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            return value;
        }
    }
}
