//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms;
using Seal.Model;
using System.ComponentModel.Design;
using System.Reflection;
using System.Drawing;

namespace Seal.Forms
{

    public class EntityCollectionEditor : CollectionEditor
    {
        // Define a static event to expose the inner PropertyGrid's
        // PropertyValueChanged event args...
        public delegate void ViewParameterPropertyValueChangedEventHandler(object sender, PropertyValueChangedEventArgs e);
        public static event ViewParameterPropertyValueChangedEventHandler MyPropertyValueChanged;

        RootComponent _component = null;
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (context.Instance is RootComponent) _component = (RootComponent)context.Instance;

            if (context.PropertyDescriptor.IsReadOnly) return UITypeEditorEditStyle.None;
            return UITypeEditorEditStyle.Modal;
        }

        void SetModified()
        {
            if (HelperEditor.HandlerInterface != null && _useHandlerInterface) HelperEditor.HandlerInterface.SetModified();
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

            bool allowAdd = false;
            bool allowRemove = false;
            Form frmCollectionEditorForm = collectionForm as Form;
            frmCollectionEditorForm.HelpButton = false;
            frmCollectionEditorForm.Text = "Collection Editor";
            if (CollectionItemType == typeof(ReportRestriction))
            {
                frmCollectionEditorForm.Text = "Restrictions Collection Editor";
                if (Context.Instance is ReportModel)
                {
                    var model = Context.Instance as ReportModel;
                    model.InitCommonRestrictions();
                }
                else if (Context.Instance is ViewFolder)
                {
                    allowAdd = true;
                    allowRemove = true;
                    frmCollectionEditorForm.Text = "Report Input Values Collection Editor";
                }
            }
            else if (CollectionItemType == typeof(OutputParameter))
            {
                frmCollectionEditorForm.Text = "Custom Output Parameters Collection Editor";
            }
            else if (CollectionItemType == typeof(SecurityParameter))
            {
                frmCollectionEditorForm.Text = "Security Parameters Collection Editor";
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(Parameter))
            {
                frmCollectionEditorForm.Text = "Template Parameters Collection Editor";
            }
            else if (CollectionItemType == typeof(SecurityGroup))
            {
                frmCollectionEditorForm.Text = "Security Groups Collection Editor";
                allowAdd = true;
                allowRemove = true;
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(SecurityFolder))
            {
                frmCollectionEditorForm.Text = "Security Folders Collection Editor";
                allowAdd = true;
                allowRemove = true;
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(SecurityColumn))
            {
                frmCollectionEditorForm.Text = "Security Columns Collection Editor";
                allowAdd = true;
                allowRemove = true;
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(SecuritySource))
            {
                frmCollectionEditorForm.Text = "Security Data Sources Collection Editor";
                allowAdd = true;
                allowRemove = true;
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(SecurityDevice))
            {
                frmCollectionEditorForm.Text = "Security Devices Collection Editor";
                allowAdd = true;
                allowRemove = true;
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(SecurityConnection))
            {
                frmCollectionEditorForm.Text = "Security Connections Collection Editor";
                allowAdd = true;
                allowRemove = true;
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(SecurityWidget))
            {
                frmCollectionEditorForm.Text = "Security Widgets Collection Editor";
                allowAdd = true;
                allowRemove = true;
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(SecurityDashboardFolder))
            {
                frmCollectionEditorForm.Text = "Security Dashboard Folders Collection Editor";
                allowAdd = true;
                allowRemove = true;
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(SubReport))
            {
                frmCollectionEditorForm.Text = "Sub-Reports Collection Editor";
                allowRemove = true;
                _useHandlerInterface = true;
            }
            else if (CollectionItemType == typeof(ReportViewPartialTemplate))
            {
                frmCollectionEditorForm.Text = "Partial Templates Collection Editor";
                _useHandlerInterface = false;
            }
            else if (CollectionItemType == typeof(CommonScript))
            {
                frmCollectionEditorForm.Text = "Common Script Collection Editor";
                allowAdd = true;
                allowRemove = true;
                _useHandlerInterface = (TemplateTextEditor.CurrentEntity is Report);
            }
            else if (CollectionItemType == typeof(SealServerConfiguration.FileReplacePattern))
            {
                frmCollectionEditorForm.Text = "File Patterns Collection Editor";
                allowAdd = true;
                allowRemove = true;
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
                    propertyGrid.LineColor = SystemColors.ControlLight;
                    propertyGrid.Tag = _component;
                }
            }

            //Hide Add/Remove -> Get the forms type
            if (!allowRemove)
            {
                Type formType = frmCollectionEditorForm.GetType();
                FieldInfo fieldInfo = formType.GetField("removeButton", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    System.Windows.Forms.Control removeButton = (System.Windows.Forms.Control)fieldInfo.GetValue(frmCollectionEditorForm);
                    removeButton.Hide();
                }
            }

            if (!allowAdd)
            {
                Type formType = frmCollectionEditorForm.GetType();
                FieldInfo fieldInfo = formType.GetField("addButton", BindingFlags.NonPublic | BindingFlags.Instance);
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
            if (MyPropertyValueChanged != null)
            {
                MyPropertyValueChanged(this, e);
            }
            SetModified();
        }


        protected override object CreateInstance(Type itemType)
        {

            object instance = Activator.CreateInstance(itemType, true);
            SetModified();
            if (_component != null) _component.UpdateEditor();

            if (instance is ReportRestriction && _component is ViewFolder)
            {
                var result = ReportRestriction.CreateReportRestriction();
                result.TypeRe = ColumnType.Text;
                result.Operator = Operator.Equal;
                result.ChangeOperator = false;
                result.Report = ((ViewFolder)_component).Report;
                instance = result;
            }

            return instance;
        }

        protected override void DestroyInstance(object instance)
        {
            base.DestroyInstance(instance);
            SetModified();
            if (_component != null) _component.UpdateEditor();
        }

        protected override string GetDisplayText(object value)
        {
            string result = "";
            if (value is RootEditor) ((RootEditor)value).InitEditor();
            if (value is ReportRestriction)
            {
                var restr = value as ReportRestriction;
                if (restr.MetaColumn == null && string.IsNullOrEmpty(restr.Name)) result = restr.DisplayNameEl; //Report input value
                else if (restr.MetaColumn == null) result = restr.Name; //Common restriction
                else if (restr.Model != null)  result = string.Format("{0} ({1})", restr.DisplayNameEl, restr.Model.Name);
            }
            else if (value is Parameter) result = ((Parameter)value).DisplayName;
            else if (value is SecurityGroup) result = ((SecurityGroup)value).Name;
            else if (value is SecurityFolder) result = ((SecurityFolder)value).Path;
            else if (value is SecurityColumn) result = ((SecurityColumn)value).DisplayName;
            else if (value is SecuritySource) result = ((SecuritySource)value).DisplayName;
            else if (value is SecurityDevice) result = ((SecurityDevice)value).DisplayName;
            else if (value is SecurityWidget) result = ((SecurityWidget)value).DisplayName;
            else if (value is SecurityDashboardFolder) result = ((SecurityDashboardFolder)value).DisplayName;
            else if (value is SecurityConnection) result = ((SecurityConnection)value).DisplayName;
            else if (value is SubReport) result = ((SubReport)value).Name;
            else if (value is ReportComponent) result = ((ReportComponent)value).Name;
            else if (value is CommonScript) result = ((CommonScript)value).Name;
            else if (value is SealServerConfiguration.FileReplacePattern) result = ((SealServerConfiguration.FileReplacePattern)value).ToString();
            return base.GetDisplayText(string.IsNullOrEmpty(result) ? "<Empty Name>" : result);
        }
    }
}
