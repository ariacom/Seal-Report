//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Seal.Model;
using Seal.Helpers;

namespace Seal.Forms
{
    public class ToolStripEditorHelper
    {
        public ToolStrip MainToolStrip;
        public PropertyGrid MainPropertyGrid;
        public TreeView MainTreeView;
        public IEntityHandler EntityHandler;

        object SelectedEntity;

        public void SetHelperButtons(object selectEntity)
        {
            SelectedEntity = selectEntity;
            int i = MainToolStrip.Items.Count;
            while (--i >= 0)
            {
                if (MainToolStrip.Items[i].Alignment == ToolStripItemAlignment.Right) MainToolStrip.Items.RemoveAt(i);
            }

            if (SelectedEntity is MetaSource)
            {
                if (((MetaSource)SelectedEntity).MetaData.MasterTable != null)
                {
                    if (!((MetaSource)SelectedEntity).MetaData.MasterTable.IsSQL) AddHelperButton("Edit Master Load Script", "Edit the default load script", Keys.F12);
                }
                AddHelperButton("Check connection", "Check the current database connection", Keys.F7);
            }
            else if (SelectedEntity is MetaConnection)
            {
                AddHelperButton("Check connection", "Check the database connection", Keys.F7);
            }
            else if (SelectedEntity is MetaTable)
            {
                if (!((MetaTable)SelectedEntity).IsSQL) AddHelperButton("Edit Load Script", "Edit the default load script", Keys.F12);
                if (((MetaTable)SelectedEntity).IsEditable)
                {
                    ToolStripButton button = AddHelperButton("Refresh columns", "Create or update dynamic columns for this table", Keys.F9);
                    button.Enabled = ((MetaTable)SelectedEntity).DynamicColumns;
                }

                if (((MetaTable)SelectedEntity).IsSQL) AddHelperButton("Edit SQL", "Edit the SQL Select Statement", Keys.F8);
                else AddHelperButton("Edit Definition Script", "Edit the definition script for the table", Keys.F8);
                AddHelperButton("Check table", "Check the table definition", Keys.F7);

            }
            else if (SelectedEntity is MetaColumn)
            {
                AddHelperButton("Show column values", "Show the first 1000 values of the column", Keys.F8);
                if (((MetaColumn)SelectedEntity).IsSQL) AddHelperButton("Check column SQL syntax", "Check the column SQL statement in the database", Keys.F7);
            }
            else if (SelectedEntity is MetaEnum)
            {
                if (((MetaEnum)SelectedEntity).IsDynamic && ((MetaEnum)SelectedEntity).IsEditable)
                {
                    AddHelperButton("Refresh values", "Refresh values of this list using the SQL Statement", Keys.F9);
                    AddHelperButton("Edit SQL", "Edit the select statement", Keys.F8);
                }
            }
            else if (SelectedEntity is MetaJoin)
            {
                AddHelperButton("Edit SQL", "Edit the join SQL clause", Keys.F8);
                AddHelperButton("Check join", "Check the join in the database", Keys.F7);
            }
            else if (SelectedEntity is ReportModel)
            {
                var model = (ReportModel)SelectedEntity;

                if (!model.Source.IsNoSQL)
                {
                    AddHelperButton("View and Check SQL", "View and check the SQL generated for the model", Keys.F8);
                    if (!model.IsSQLModel)
                    {
                        AddHelperButton("View SQL", "View the SQL generated for the model", Keys.F7);
                    }
                    else
                    {
                        AddHelperButton("Edit SQL", "Edit the source SQL used for the model", Keys.F7);
                    }
                }
            }
            else if (SelectedEntity is ReportView)
            {
                if (((ReportView)SelectedEntity).UseCustomTemplate) AddHelperButton("Edit Custom Template", "Edit the custom template texts", Keys.F8);
            }
            else if (SelectedEntity is ViewFolder)
            {
                AddHelperButton("Edit Report Input Values", "Edit the Report Input Values", Keys.F8);
            }
            else if (SelectedEntity is TasksFolder)
            {
                AddHelperButton("Edit Common Scripts", "Edit the Common Scripts", Keys.F7);
            }
            else if (SelectedEntity is ReportTask)
            {
                AddHelperButton("Edit SQL", "Edit the SQL Statement for the task", Keys.F8);
                AddHelperButton("Edit Script", "Edit the Razor Script for the task", Keys.F7);
            }
            else if (SelectedEntity is ReportSchedule)
            {
                AddHelperButton("Task Scheduler", "Run the Task Scheduler Microsoft Management Console", Keys.F9);
                AddHelperButton("Edit Schedule", "Edit schedule properties", Keys.F8);
            }
        }

        ToolStripButton AddHelperButton(string text, string tooltip, Keys shortcut)
        {
            ToolStripButton helperButton = new ToolStripButton(shortcut.ToString() + " " + text);
            helperButton.ToolTipText = tooltip;
            helperButton.Alignment = ToolStripItemAlignment.Right;
            helperButton.Click += helperButton_Click;
            helperButton.Image = global::Seal.Properties.Resources.helper;
            helperButton.ImageTransparentColor = System.Drawing.Color.White;
            helperButton.Tag = shortcut;
            MainToolStrip.Items.Add(helperButton);
            return helperButton;
        }

        void helperButton_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                if (sender is ToolStripItem)
                {
                    Keys key = (Keys)((ToolStripItem)sender).Tag;

                    if (SelectedEntity is MetaSource)
                    {
                        if (key == Keys.F7)
                        {
                            MetaSource source = ((MetaSource)SelectedEntity);
                            source.Connection.CheckConnection();
                            source.Information = source.Connection.Information;
                            source.Error = source.Connection.Error;
                            source.InitEditor();
                        }
                        if (key == Keys.F12)
                        {
                            MetaTable table = ((MetaSource)SelectedEntity).MetaData.MasterTable;
                            if (table != null)
                            {
                                TreeViewHelper.SelectNode(MainTreeView, MainTreeView.SelectedNode.Nodes, table);
                                EditProperty("Default Load Script");
                            }
                        }
                    }
                    else if (SelectedEntity is MetaConnection)
                    {
                        if (key == Keys.F7) ((MetaConnection)SelectedEntity).CheckConnection();
                    }
                    else if (SelectedEntity is MetaTable)
                    {
                        if (key == Keys.F7) ((MetaTable)SelectedEntity).CheckTable(null);
                        if (key == Keys.F8)
                        {
                            if (((MetaTable)SelectedEntity).IsSQL) EditProperty("SQL Select Statement");
                            else EditProperty("Definition Script");
                        }
                        if (key == Keys.F9 && ((MetaTable)SelectedEntity).DynamicColumns)
                        {
                            ((MetaTable)SelectedEntity).Refresh();
                            if (EntityHandler != null)
                            {
                                EntityHandler.SetModified();
                                EntityHandler.InitEntity(SelectedEntity);
                            }
                        }
                        if (key == Keys.F12)
                        {
                            EditProperty("Default Load Script");
                        }
                    }
                    else if (SelectedEntity is MetaColumn)
                    {
                        if (key == Keys.F7) EditProperty("Check column SQL syntax");
                        if (key == Keys.F8) EditProperty("Show column values");
                    }
                    else if (SelectedEntity is MetaEnum)
                    {
                        if (key == Keys.F8) EditProperty("SQL Select Statement");
                        if (key == Keys.F9)
                        {
                            if (EntityHandler != null) EntityHandler.SetModified();
                            ((MetaEnum)SelectedEntity).RefreshEnum();
                        }
                    }
                    else if (SelectedEntity is MetaJoin)
                    {
                        if (key == Keys.F7) ((MetaJoin)SelectedEntity).CheckJoin();
                        if (key == Keys.F8) EditProperty("SQL Clause");
                    }
                    else if (SelectedEntity is TasksFolder)
                    {
                        if (key == Keys.F7) EditProperty("Common Scripts");
                    }
                    else if (SelectedEntity is ReportTask)
                    {
                        if (key == Keys.F7) EditProperty("Script");
                        if (key == Keys.F8) EditProperty("SQL Statement");
                    }
                    else if (SelectedEntity is ReportModel)
                    {
                        if (key == Keys.F7 || key == Keys.F8)
                        {
                            var frm = new SQLEditorForm();
                            frm.Instance = SelectedEntity;
                            frm.PropertyName = "";

                            ReportModel model = SelectedEntity as ReportModel;
                            if (model.IsSQLModel && key == Keys.F7)
                            {
                                frm.Text = "SQL Editor: Edit the SQL Select Statement";
                                frm.SetSamples(new List<string>() {
                                    "SELECT * FROM Orders", "SELECT *\r\nFROM Employees\r\nWHERE {CommonRestriction_LastName}",
                                    "SELECT * FROM Orders", "SELECT *\r\nFROM Employees\r\nWHERE EmployeeID > {CommonValue_ID}"
                                });
                                frm.WarningOnError = true;
                                frm.sqlTextBox.Text = model.Table.Sql;
                                if (frm.ShowDialog() == DialogResult.OK)
                                {
                                    try
                                    {
                                        Cursor.Current = Cursors.WaitCursor;
                                        model.Table.Sql = frm.sqlTextBox.Text;
                                        model.RefreshMetaTable(true);
                                    }
                                    finally
                                    {
                                        Cursor.Current = Cursors.Default;
                                    }

                                    if (EntityHandler != null)
                                    {
                                        EntityHandler.SetModified();
                                        EntityHandler.RefreshModelTreeView();
                                    }

                                    if (!string.IsNullOrEmpty(model.Table.Error)) throw new Exception("Error when building columns from the SQL Select Statement:\r\n" + model.Table.Error);
                                }
                            }
                            else
                            {
                                model.Report.CheckingExecution = true;
                                try
                                {
                                    model.BuildSQL();
                                    frm.SqlToCheck = model.Sql;
                                    model.Report.CheckingExecution = false;
                                    model.BuildSQL();
                                }
                                finally
                                {
                                    model.Report.CheckingExecution = false;
                                }
                                if (!string.IsNullOrEmpty(model.ExecutionError))
                                {
                                    throw new Exception("Error building the SQL Statement...\r\nPlease fix these errors first.\r\n" + model.ExecutionError);
                                }
                                frm.sqlTextBox.Text = model.Sql;
                                frm.SetReadOnly();
                                if (key == Keys.F8) frm.checkSQL();
                                frm.ShowDialog();
                            }
                        }
                    }
                    else if (SelectedEntity is ReportView)
                    {
                        if (key == Keys.F8) EditProperty("Custom template");
                    }
                    else if (SelectedEntity is ViewFolder)
                    {
                        if (key == Keys.F8) EditProperty("Report Input Values");
                    }
                    else if (SelectedEntity is ReportSchedule)
                    {
                        if (key == Keys.F8) EditProperty("Edit schedule properties");
                        if (key == Keys.F9) EditProperty("Run Task Scheduler MMC");
                    }
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        void EditProperty(string label)
        {
            MainPropertyGrid.Focus();
            GridItemCollection gridItems = Helper.GetAllGridEntries(MainPropertyGrid);
            foreach (GridItem gridItem in gridItems)
            {
                if (gridItem.Label.Replace("\t", "") == label)
                {
                    gridItem.Select();
                    Application.DoEvents();
                    break;
                }
            }
            MainPropertyGrid.Focus();
            Application.DoEvents();
            SendKeys.Send("{F4}");
            Application.DoEvents();
        }

        public void HandleShortCut(KeyEventArgs e)
        {
            foreach (ToolStripItem button in MainToolStrip.Items)
            {
                if (button.Tag is Keys && ((Keys)button.Tag) == e.KeyCode)
                {
                    helperButton_Click(button, e);
                    break;
                }
            }
        }
    }
}
