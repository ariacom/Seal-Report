using Seal.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Seal.Forms
{
    public class CultureCollectionEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        class StringDisplay
        {
            public string Id { get; set; }
            public string Display { get; set; }
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (context.Instance != null)
                {
                    var config = (SealServerConfiguration) context.Instance;

                    var displaySource = new List<StringDisplay>();
                    foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(i => i.EnglishName))
                    {
                        displaySource.Add(new StringDisplay() { Id = culture.EnglishName, Display = string.Format("{0} ({1})", culture.EnglishName, culture.NativeName)});
                    }


                    MultipleSelectForm frm = new MultipleSelectForm("Please select the values", displaySource, "Display");
                    //select existing values
                    for (int i = 0; i < frm.checkedListBox.Items.Count; i++)
                    {
                        if (config.WebCultures.Exists(j => j == ((StringDisplay)frm.checkedListBox.Items[i]).Id)) frm.checkedListBox.SetItemChecked(i, true);
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
            finally
            {
                Cursor.Current = Cursors.Default;
            }

            return value;
        }
    }
}

