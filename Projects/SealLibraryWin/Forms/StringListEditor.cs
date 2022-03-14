//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms;
using Seal.Model;

namespace Seal.Forms
{
    public class StringListEditor : UITypeEditor
    {

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                SecurityLogin login = context.Instance as SecurityLogin;
                if (login != null)
                {
                    MultipleSelectForm frm = new MultipleSelectForm("Please select the values", Repository.Instance.Security.Groups, "Name");
                    //select existing values
                    for (int i = 0; i < frm.checkedListBox.Items.Count; i++)
                    {
                        if (login.GroupNames.Contains(((SecurityGroup)frm.checkedListBox.Items[i]).Name)) frm.checkedListBox.SetItemChecked(i, true); 
                    }

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        login.GroupNames = new List<string>();
                        foreach (object item in frm.CheckedItems)
                        {
                            login.GroupNames.Add(((SecurityGroup)item).Name);
                        }
                        value = login.GroupNames; //indicates a modification
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
