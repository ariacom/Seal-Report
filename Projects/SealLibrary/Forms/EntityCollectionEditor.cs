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
using System.Reflection;

namespace Seal.Forms
{

    public class EntityCollectionEditor : CollectionEditor
    {
        // Define a static event to expose the inner PropertyGrid's
        // PropertyValueChanged event args...
        public delegate void ViewParameterPropertyValueChangedEventHandler(object sender, PropertyValueChangedEventArgs e);
        public static event ViewParameterPropertyValueChangedEventHandler MyPropertyValueChanged;

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (context.PropertyDescriptor.IsReadOnly) return UITypeEditorEditStyle.None;
            return UITypeEditorEditStyle.Modal;
        }

        // Inherit the default constructor from the standard
        // Collection Editor...
        public EntityCollectionEditor(Type type) : base(type) { }
        bool _useHandlerInterface = true;

        // Override this method in order to access the containing user controls
        // from the default Collection Editor form or to add new ones...
        protected override CollectionForm CreateCollectionForm()
        {
            // Getting the default layout of the Collection Editor...
            CollectionForm collectionForm = base.CreateCollectionForm();

            bool allowAddRemove = false;
            Form frmCollectionEditorForm = collectionForm as Form;
            frmCollectionEditorForm.HelpButton = false;
            frmCollectionEditorForm.Text = "Collection Editor";
            if (CollectionItemType == typeof(ReportRestriction)) frmCollectionEditorForm.Text = "Restrictions Collection Editor";
            else if (CollectionItemType == typeof(Parameter)) frmCollectionEditorForm.Text = "Template Parameters Collection Editor";
            else if (CollectionItemType == typeof(SecurityParameter))
            {
                frmCollectionEditorForm.Text = "Security Parameters Collection Editor";
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(SecurityGroup))
            {
                frmCollectionEditorForm.Text = "Security Groups Collection Editor";
                allowAddRemove = true;
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(SecurityFolder))
            {
                frmCollectionEditorForm.Text = "Security Folders Collection Editor";
                allowAddRemove = true;
                _useHandlerInterface = false;
            }

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

            //Hide Add/Remove -> Get the forms type
            if (!allowAddRemove)
            {
                Type formType = frmCollectionEditorForm.GetType();
                FieldInfo fieldInfo = formType.GetField("removeButton", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    System.Windows.Forms.Control removeButton = (System.Windows.Forms.Control)fieldInfo.GetValue(frmCollectionEditorForm);
                    removeButton.Hide();
                }
                fieldInfo = formType.GetField("addButton", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    System.Windows.Forms.Control addButton = (System.Windows.Forms.Control)fieldInfo.GetValue(frmCollectionEditorForm);
                    addButton.Hide();
                }
            }
            return collectionForm;
        }

        void propertyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            // Fire our customized collection event...
            if (EntityCollectionEditor.MyPropertyValueChanged != null)
            {
                EntityCollectionEditor.MyPropertyValueChanged(this, e);
            }
            if (HelperEditor.HandlerInterface != null && _useHandlerInterface) HelperEditor.HandlerInterface.SetModified();
        }

        protected override string GetDisplayText(object value)
        {
            string result = "";
            if (value is RootEditor) ((RootEditor)value).InitEditor();
            if (value is ReportRestriction) result = string.Format("{0} ({1})", ((ReportRestriction)value).DisplayNameEl, ((ReportRestriction)value).Model.Name);
            else if (value is Parameter) result = ((Parameter)value).DisplayName;
            else if (value is SecurityGroup) result = ((SecurityGroup)value).Name;
            else if (value is SecurityFolder) result = ((SecurityFolder)value).Path;
            return base.GetDisplayText(string.IsNullOrEmpty(result) ? "<Empty Name>" : result);
        }

    }
}
