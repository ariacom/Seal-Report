//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Seal.Model;
using Seal.Helpers;
using ScintillaNET;

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
                AddHelperButton("Check connection", "Check the current database connection", Keys.F7);
            }
            else if (SelectedEntity is MetaConnection)
            {
                AddHelperButton("Check connection", "Check the database connection", Keys.F7);
            }
            else if (SelectedEntity is MetaTable)
            {
                var table = (MetaTable)SelectedEntity;
                var prefix = table.IsEditable ? "Edit" : "View";
                if (!table.IsSQL) AddHelperButton(prefix + " Load Script", prefix + " the default load script", Keys.F12);
                if (table.IsEditable && !table.IsSubTable)
                {
                    ToolStripButton button = AddHelperButton("Refresh columns", "Create or update dynamic columns for this table", Keys.F9);
                    button.Enabled = table.DynamicColumns;
                }

                if (table.IsSQL) AddHelperButton(prefix + " SQL", prefix + " the SQL Select Statement", Keys.F8);
                else AddHelperButton(prefix + " Definition Script", prefix + " the definition script for the table", Keys.F8);
                AddHelperButton("Check table", "Check the table definition", Keys.F7);

            }
            else if (SelectedEntity is MetaColumn)
            {
                AddHelperButton("Show column values", "Show the first 1000 values of the column", Keys.F8);
                if (((MetaColumn)SelectedEntity).IsSQL) AddHelperButton("Check column SQL syntax", "Check the column SQL statement in the database", Keys.F7);
            }
            else if (SelectedEntity is MetaEnum)
            {
                var enumList = ((MetaEnum)SelectedEntity);
                if (enumList.IsDynamic && enumList.IsEditable)
                {
                    AddHelperButton("Refresh values", "Refresh values of this list using the SQL Statement", Keys.F9);
                    AddHelperButton("Edit Load Script", "Edit the script used to load the values", Keys.F8);
                    if (enumList.IsSQL) AddHelperButton("Edit SQL", "Edit the SQL select statement used to load the values", Keys.F7);
                }
            }
            else if (SelectedEntity is MetaJoin)
            {
                AddHelperButton("Edit Join clause", "Edit the Join SQL or LINQ clause", Keys.F8);
                AddHelperButton("Check join", "Check the join", Keys.F7);
            }
            else if (SelectedEntity is ReportModel)
            {
                var model = (ReportModel)SelectedEntity;

                if (model.IsLINQ) AddHelperButton("View and Check LINQ", "View the LINQ Query generated for the model", Keys.F8);
                else AddHelperButton("View and Check SQL", "View and check the SQL generated for the model", Keys.F8);

                if (model.IsLINQ) AddHelperButton("View LINQ", "View the LINQ Query generated for the model", Keys.F7);
                else if (model.IsSQLModel) AddHelperButton("Edit SQL", "Edit the source SQL used for the model", Keys.F7);
                else AddHelperButton("View SQL", "View the SQL generated for the model", Keys.F7);

                if (model.IsLINQ) AddHelperButton("Refresh Sub-Models and Sub-Tables", "Using the elements and restrictions selected, rebuild the Sub-Models and Sub-Tables involved in the LINQ Query", null);

            }
            else if (SelectedEntity is ReportView)
            {
                if (((ReportView)SelectedEntity).UseCustomTemplate) AddHelperButton("Edit Custom Template", "Edit the custom template texts", Keys.F8);
            }
            else if (SelectedEntity is Report)
            {
                AddHelperButton("Edit Report Input Values", "Edit the Report Input Values", Keys.F8);
                AddHelperButton("Edit Common Scripts", "Edit the Common Scripts", Keys.F7);
            }
            else if (SelectedEntity is ReportTask)
            {
                AddHelperButton("Edit SQL", "Edit the SQL Statement for the task", Keys.F8);
                AddHelperButton("Edit Script", "Edit the Razor Script for the task", Keys.F7);
            }
            else if (SelectedEntity is ReportSchedule)
            {
                if (!((ReportSchedule)SelectedEntity).Report.Repository.UseWebScheduler)
                {
                    AddHelperButton("Task Scheduler", "Run the Task Scheduler Microsoft Management Console", Keys.F9);
                    AddHelperButton("Edit Schedule", "Edit schedule properties", Keys.F8);
                }
            }
        }

        ToolStripButton AddHelperButton(string text, string tooltip, Keys? shortcut)
        {
            ToolStripButton helperButton = new ToolStripButton(shortcut != null ? shortcut.ToString() + " " + text : text);
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
                    var toolStrip = (ToolStripItem)sender;
                    Keys? key = null;
                    if (toolStrip.Tag != null) key = (Keys)toolStrip.Tag;

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
                        if (key == Keys.F7) EditProperty("SQL Select Statement");
                        if (key == Keys.F8) EditProperty("Script");
                        if (key == Keys.F9)
                        {
                            if (EntityHandler != null) EntityHandler.SetModified();
                            ((MetaEnum)SelectedEntity).RefreshEnum();
                        }
                    }
                    else if (SelectedEntity is MetaJoin)
                    {
                        if (key == Keys.F7) ((MetaJoin)SelectedEntity).CheckJoin();
                        if (key == Keys.F8) EditProperty("Join clause");
                    }
                    else if (SelectedEntity is ReportTask)
                    {
                        if (key == Keys.F7) EditProperty("Script");
                        if (key == Keys.F8) EditProperty("SQL Statement");
                    }
                    else if (SelectedEntity is ReportModel)
                    {
                        ReportModel model = SelectedEntity as ReportModel;

                        if (key == null && model.IsLINQ)
                        {
                            model.BuildQuery(false, true);
                            EntityHandler.UpdateModelNode();

                            MessageBox.Show(string.Format("The LINQ Model has been refreshed...\r\n\r\nIt contains {0} SQL Sub-Model(s) and {1} NoSQL Sub-Table(s).", model.LINQSubModels.Count, model.LINQSubTables.Count), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (key == Keys.F7 || key == Keys.F8)
                        {
                            if (model.IsLINQ)
                            {
                                var frm = new TemplateTextEditorForm();
                                frm.Text = "LINQ Editor" + (!string.IsNullOrEmpty(model.LoadScript) ? " (WARNING: Script got from the 'Load Script' property of the model)" : "");
                                frm.ObjectForCheckSyntax = model;
                                ScintillaHelper.Init(frm.textBox, Lexer.Cpp);

                                if (string.IsNullOrEmpty(model.LoadScript))
                                {
                                    model.Report.CheckingExecution = true;
                                    try
                                    {
                                        model.BuildQuery();
                                        frm.textBox.Text = model.LINQLoadScript;
                                        model.Report.CheckingExecution = false;
                                        model.BuildQuery();
                                    }
                                    finally
                                    {
                                        model.Report.CheckingExecution = false;
                                    }
                                    if (!string.IsNullOrEmpty(model.ExecutionError)) throw new Exception(model.ExecutionError);

                                    frm.textBox.Text = model.LINQLoadScript;
                                }
                                else
                                {
                                    frm.textBox.Text = model.LoadScript;
                                }

                                if (key == Keys.F8) frm.CheckSyntax();
                                frm.textBox.ReadOnly = true;
                                frm.okToolStripButton.Visible = false;
                                frm.cancelToolStripButton.Text = "Close";
                                frm.ShowDialog();
                            }
                            else
                            {
                                var frm = new SQLEditorForm();
                                frm.Instance = SelectedEntity;
                                frm.PropertyName = "";
                                frm.InitLexer(Lexer.Sql);

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
                                        model.BuildQuery();
                                        frm.SqlToCheck = model.Sql;
                                        model.Report.CheckingExecution = false;
                                        model.BuildQuery();
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
                    }
                    else if (SelectedEntity is ReportView)
                    {
                        if (key == Keys.F8) EditProperty("Custom template");
                    }
                    else if (SelectedEntity is Report)
                    {
                        if (key == Keys.F7) EditProperty("Common Scripts");
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
