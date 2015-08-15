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
using System.ComponentModel.Design;

namespace Seal.Forms
{

    public class EnumValueCollectionEditor : CollectionEditor
    {

        // Define a static event to expose the inner PropertyGrid's
        // PropertyValueChanged event args...
        public delegate void EnumPropertyValueChangedEventHandler(object sender, PropertyValueChangedEventArgs e);
        public static event EnumPropertyValueChangedEventHandler MyPropertyValueChanged;

        // Inherit the default constructor from the standard
        // Collection Editor...
        public EnumValueCollectionEditor(Type type) : base(type) { }

        // Override this method in order to access the containing user controls
        // from the default Collection Editor form or to add new ones...
        protected override CollectionForm CreateCollectionForm()
        {
            // Getting the default layout of the Collection Editor...
            CollectionForm collectionForm = base.CreateCollectionForm();

            Form frmCollectionEditorForm = collectionForm as Form;
            frmCollectionEditorForm.Text = "Enum Values Collection Editor";
            TableLayoutPanel tlpLayout = frmCollectionEditorForm.Controls[0] as TableLayoutPanel;

            if (tlpLayout != null)
            {
                // Get a reference to the inner PropertyGrid and hook
                // an event handler to it.
                if (tlpLayout.Controls[5] is PropertyGrid)
                {
                    PropertyGrid propertyGrid = tlpLayout.Controls[5] as PropertyGrid;
                    propertyGrid.HelpVisible = true;
                    propertyGrid.ToolbarVisible = false;
                    propertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(propertyGrid_PropertyValueChanged);
                }
            }

            return collectionForm;
        }

        void propertyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            // Fire our customized collection event...
            if (EnumValueCollectionEditor.MyPropertyValueChanged != null)
            {
                EnumValueCollectionEditor.MyPropertyValueChanged(this, e);
            }
            if (HelperEditor.HandlerInterface != null) HelperEditor.HandlerInterface.SetModified();
        }

        protected override string GetDisplayText(object value)
        {
            MetaEV item = (MetaEV)value;
            return base.GetDisplayText(string.IsNullOrEmpty(item.Id) ? "<Empty value>" : string.Format("{0}", item.Val));
        }
    }
}
