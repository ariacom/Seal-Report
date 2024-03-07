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
using System.Linq;

namespace Seal.Forms
{
    public class RestrictionsEditor : UITypeEditor
    {
        ReportView _view;

        void setContext(ITypeDescriptorContext context)
        {
            _view = context.Instance as ReportView;
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
                if (_view != null)
                {
                    foreach( var model in _view.Report.Models) model.InitCommonRestrictions();
                    var restrictions = _view.Report.AllRestrictions.ToList();
                    MultipleSelectForm frm = new MultipleSelectForm("Please select the Restrictions", restrictions, "DisplayNameWithModel");
                    //select existing values
                    for (int i = 0; i < frm.checkedListBox.Items.Count; i++)
                    {
                        if (_view.RestrictionsGUID.Contains(((ReportRestriction)frm.checkedListBox.Items[i]).GUID)) frm.checkedListBox.SetItemChecked(i, true); 
                    }

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        _view.RestrictionsGUID = new List<string>();
                        foreach (object item in frm.CheckedItems)
                        {
                            _view.RestrictionsGUID.Add(((ReportRestriction) item).GUID);
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
