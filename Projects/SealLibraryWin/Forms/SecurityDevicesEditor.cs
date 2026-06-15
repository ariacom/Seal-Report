//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// Editor used to select the output devices (stored by GUID) that the AI tools of a security group can use.
    /// An empty selection means that all output devices are allowed.
    /// </summary>
    public class SecurityDevicesEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var svc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            List<string> vals = value as List<string>;
            if (svc != null && vals != null)
            {
                var devices = new List<OutputDevice>(Repository.Instance.Devices);

                var frm = new MultipleSelectForm("Select the Output Devices allowed for the AI Tools (none selected = all allowed)", devices, "Name");
                //select existing values
                for (int i = 0; i < frm.checkedListBox.Items.Count; i++)
                {
                    if (vals.Contains(((OutputDevice)frm.checkedListBox.Items[i]).GUID)) frm.checkedListBox.SetItemChecked(i, true);
                }

                if (svc.ShowDialog(frm) == System.Windows.Forms.DialogResult.OK)
                {
                    vals.Clear();
                    foreach (object item in frm.CheckedItems)
                    {
                        vals.Add(((OutputDevice)item).GUID);
                    }
                    if (HelperEditor.HandlerInterface != null) HelperEditor.HandlerInterface.SetModified();
                }
            }
            return value;
        }
    }
}
