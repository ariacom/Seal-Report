//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using Seal.Model;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Web;
using RazorEngine.Templating;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Xml.Serialization;
using System.Globalization;
using System.Net.Mail;
using System.Data.SqlClient;

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


        public static GridItemCollection GetAllGridEntries(PropertyGrid grid)
        {
            object view = grid.GetType().GetField("gridView", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(grid);
            return (GridItemCollection)view.GetType().InvokeMember("GetAllGridEntries", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, view, null);
        }

        public static GridItem GetGridEntry(PropertyGrid grid, string label)
        {
            var entries = Helper.GetAllGridEntries(grid);
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
