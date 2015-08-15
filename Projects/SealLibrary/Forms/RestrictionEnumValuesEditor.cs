//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
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
                    MultipleSelectForm frm = new MultipleSelectForm("Please restriction values", _restriction.EnumRE.Values, "Val");
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
