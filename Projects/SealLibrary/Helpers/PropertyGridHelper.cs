using DynamicTypeDescriptor;
using Seal.Forms;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Seal.Helpers
{
    public class PropertyGridHelper
    {
        static public void AddResetMenu(PropertyGrid grid)
        {
            grid.ContextMenuStrip = new ContextMenuStrip();
            grid.ContextMenuStrip.Opening += new CancelEventHandler(delegate (object sender, CancelEventArgs e)
            {
                GridItem item = grid.SelectedGridItem;
                if (
                item.PropertyDescriptor == null ||
                item.PropertyDescriptor.IsReadOnly ||
                (item.PropertyDescriptor is CustomPropertyDescriptor && ((CustomPropertyDescriptor)item.PropertyDescriptor).DefaultValue == null) ||
                !item.PropertyDescriptor.CanResetValue(grid.SelectedObject)
                )
                {
                    e.Cancel = true;
                }
            });

            var resetToolStripMenuItem = new ToolStripMenuItem() { Text = "Reset to default value" };
            resetToolStripMenuItem.Click += new EventHandler(delegate (object sender, EventArgs e)
            {
                GridItem item = grid.SelectedGridItem;
                if (item.PropertyDescriptor != null && item.PropertyDescriptor.CanResetValue(grid.SelectedObject))
                {
                    grid.ResetSelectedProperty();
                    if (grid.SelectedObject is RootEditor) ((RootEditor)grid.SelectedObject).UpdateEditor();
                    if (HelperEditor.HandlerInterface != null) HelperEditor.HandlerInterface.SetModified();
                }
            });
            grid.ContextMenuStrip.Items.Add(resetToolStripMenuItem);
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
                        field.SetValue(control, true);

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
