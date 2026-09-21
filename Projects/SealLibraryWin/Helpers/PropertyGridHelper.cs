//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using DynamicTypeDescriptor;
using Seal.Forms;
using Seal.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace Seal.Helpers
{
    public class PropertyGridHelper
    {
        /// <summary>
        /// Add the 'Reset to default value' menu to a property grid. If specified, modifiedHandler is called after a reset instead of the default modification handler.
        /// </summary>
        static public void AddResetMenu(PropertyGrid grid, Action modifiedHandler = null)
        {
            grid.ContextMenuStrip = new ContextMenuStrip();
            grid.ContextMenuStrip.Opening += new CancelEventHandler(delegate (object sender, CancelEventArgs e)
            {
                GridItem item = grid.SelectedGridItem;
                if (item != null)
                {
                    if (
                    item.PropertyDescriptor == null ||
                    item.PropertyDescriptor.IsReadOnly ||
                    (!(item.PropertyDescriptor is CustomPropertyDescriptor)) ||
                    (item.PropertyDescriptor is CustomPropertyDescriptor && ((CustomPropertyDescriptor)item.PropertyDescriptor).DefaultValue == null) ||
                    !item.PropertyDescriptor.CanResetValue(grid.SelectedObject) ||
                    !isDefaultValueAllowed(item) ||
                    (grid.SelectedObject != null && grid.SelectedObject is ReportRestriction && (item.PropertyDescriptor.Name == "Operator" || item.PropertyDescriptor.Name == "OperatorLabel")) //Case reset operator for a restriction
                    )
                    {
                        e.Cancel = true;
                    }
                }
                else e.Cancel = true;
            });

            var resetToolStripMenuItem = new ToolStripMenuItem() { Text = "Reset to default value" };
            resetToolStripMenuItem.Click += new EventHandler(delegate (object sender, EventArgs e)
            {
                GridItem item = grid.SelectedGridItem;
                if (item != null)
                {
                    if (item.PropertyDescriptor != null && item.PropertyDescriptor.CanResetValue(grid.SelectedObject))
                    {
                        grid.ResetSelectedProperty();
                        if (grid.SelectedObject is RootEditor) ((RootEditor)grid.SelectedObject).UpdateEditor();
                        if (modifiedHandler != null) modifiedHandler();
                        else if (HelperEditor.HandlerInterface != null) HelperEditor.HandlerInterface.SetModified();
                    }
                }
            });
            grid.ContextMenuStrip.Items.Add(resetToolStripMenuItem);
        }

        //For a text having a list of choices (e.g. a security provider), the default value must be one of the choices
        static bool isDefaultValueAllowed(GridItem item)
        {
            try
            {
                var descriptor = item.PropertyDescriptor;
                var context = item as ITypeDescriptorContext;
                //No reset for a collection, or if the default value cannot be a value of the property (e.g. a default value false set for a list)
                if (descriptor.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(descriptor.PropertyType)) return false;
                var defaultObject = ((CustomPropertyDescriptor)descriptor).DefaultValue;
                if (defaultObject != null && !descriptor.PropertyType.IsInstanceOfType(defaultObject) && !(descriptor.PropertyType.IsPrimitive && defaultObject.GetType().IsPrimitive && descriptor.PropertyType != typeof(bool) && defaultObject.GetType() != typeof(bool))) return false;

                if (descriptor.PropertyType == typeof(string) && descriptor.Converter != null && descriptor.Converter.GetStandardValuesSupported(context) && descriptor.Converter.GetStandardValuesExclusive(context))
                {
                    var defaultValue = ((CustomPropertyDescriptor)descriptor).DefaultValue as string;
                    foreach (var value in descriptor.Converter.GetStandardValues(context))
                    {
                        if (value as string == defaultValue) return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            return true;
        }

        static public void ResizeDescriptionArea(PropertyGrid grid, int lines)
        {
            try
            {
                var info = grid.GetType().GetProperty("Controls");
                var collection = (Control.ControlCollection)info.GetValue(grid, null);

                foreach (var control in collection)
                {
                    var type = control.GetType();

                    if ("DocComment" == type.Name)
                    {
                        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
                        var field = type.BaseType.GetField("userSized", Flags);
                        if (field != null) field.SetValue(control, true);

                        info = type.GetProperty("Lines");
                        info.SetValue(control, lines, null);

                        grid.HelpVisible = true;
                        break;
                    }
                }
            }

            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }
    }

}
