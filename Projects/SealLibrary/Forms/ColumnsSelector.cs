//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
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
    public class ColumnsSelector : UITypeEditor
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
                MetaColumn column = context.Instance as MetaColumn;
                List<string> vals = value as List<string>;
                if (column != null && vals != null)
                {
                    List<MetaColumn> cols = new List<MetaColumn>();
                    foreach (var table in column.Source.MetaData.Tables)
                    {
                        foreach (var col in table.Columns.Where(i => i != column))
                        {
                            if (!context.PropertyDescriptor.IsReadOnly || vals.Contains(col.GUID)) cols.Add(col);
                        }
                    }

                    var frm = new MultipleSelectForm("Select the child columns", cols, "FullDisplayName");
                    //select existing values
                    for (int i = 0; i < frm.checkedListBox.Items.Count; i++)
                    {
                        if (vals.Contains(((MetaColumn)frm.checkedListBox.Items[i]).GUID)) frm.checkedListBox.SetItemChecked(i, true);
                    }

                    if (context.PropertyDescriptor.IsReadOnly)
                    {
                        frm.Text = "Child Columns defined for Drill";
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
                            vals.Add(((MetaColumn)item).GUID);
                        }
                        column.UpdateEditor();

                        if (HelperEditor.HandlerInterface != null) HelperEditor.HandlerInterface.SetModified();
                    }
                }
            }
            return value;
        }
    }
}
