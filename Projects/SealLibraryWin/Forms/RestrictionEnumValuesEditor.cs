﻿//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms;
using Seal.Model;

namespace Seal.Forms
{
    public class RestrictionEnumValuesEditor : UITypeEditor
    {
        ReportRestriction _restriction;

        void setContext(ITypeDescriptorContext context)
        {
            _restriction = context.Instance as ReportRestriction;
        }


        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            setContext(context);
            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                setContext(context);
                if (_restriction != null && _restriction.IsEnum)
                {
                    MultipleSelectForm frm = new MultipleSelectForm("Please select the restriction values", _restriction.MetaEnumValuesRE, "DisplayRestriction");
                    //select existing values
                    for (int i = 0; i < frm.checkedListBox.Items.Count; i++)
                    {
                        if (_restriction.EnumValues.Contains(((MetaEV)frm.checkedListBox.Items[i]).Id)) frm.checkedListBox.SetItemChecked(i, true); 
                    }

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        _restriction.EnumValues = new List<string>();
                        foreach (object item in frm.CheckedItems)
                        {
                            _restriction.EnumValues.Add(((MetaEV) item).Id);
                        }
                        value = ""; //indicates a modification
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
