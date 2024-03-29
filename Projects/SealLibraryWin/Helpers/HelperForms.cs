﻿//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Collections.Generic;
using Seal.Model;
using System.Windows.Forms;

namespace Seal.Helpers
{
    /// <summary>
    /// Helper Objects
    /// </summary>
    public partial class Helper
    {

        public static bool CanDragAndDrop(DragEventArgs e)
        {
            return (e.Data.GetDataPresent(typeof(TreeNode)) && ((TreeNode)e.Data.GetData(typeof(TreeNode))).Tag is MetaColumn) || e.Data.GetDataPresent(typeof(Button));
        }


        public static List<GridItem> GetAllGridEntries(PropertyGrid grid)
        {
            var result = new List<GridItem>();
            GridItem root = grid.SelectedGridItem;
            //Get the parent
            while (root != null && root.Parent != null) root = root.Parent;
            if (root != null)
            {
                foreach (GridItem item in root.GridItems)
                {
                    foreach (GridItem item2 in item.GridItems)
                    {
                        result.Add(item2);
                    }
                }
            }

            return root != null ? result : null;
        }

        public static GridItem GetGridEntry(PropertyGrid grid, string label)
        {
            var entries = GetAllGridEntries(grid);
            if (entries != null)
            {
                foreach (GridItem item in entries)
                {
                    if (!string.IsNullOrEmpty(item.Label))
                    {
                        string label2 = item.Label.Replace("\t", "").ToLower();
                        if (label2 == label) return item;
                    }
                }
            }
            return null;
        }

        static bool _checkTaskSchedulerOSDone = false;
        public static bool CheckTaskSchedulerOS()
        {
            if (!IsValidOS() && !_checkTaskSchedulerOSDone)
            {
                if (MessageBox.Show("The Task Scheduler works only with Windows Vista, 7, 2008 or above...\r\nDo you want to continue ?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                {
                    return false;
                }
                _checkTaskSchedulerOSDone = true;
            }
            return true;
        }

        static bool _checkWebServerOSDone = false;
        public static bool CheckWebServerOS()
        {
            if (!IsValidOS() && !_checkWebServerOSDone)
            {
                if (MessageBox.Show("The Web Server works only with Windows Vista, 7, 2008 or above...\r\nDo you want to continue ?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                {
                    return false;
                }
                _checkWebServerOSDone = true;
            }
            return true;
        }

        static bool _checkOleDBDone = false;
        public static bool CheckOLEDBOS()
        {
            if (!IsValidOS() && !_checkOleDBDone)
            {
                if (MessageBox.Show("The OLEDB Data Link Editor works only with Windows Vista, 7, 2008 or above...\r\nDo you want to continue ?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                {
                    return false;
                }
                _checkOleDBDone = true;
                return false;
            }
            return true;
        }

    }
}
