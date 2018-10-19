//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Seal.Helpers;
using Seal.Model;
using ScintillaNET;

namespace Seal.Forms
{
    public partial class SQLEditorForm : Form
    {
        public object Instance;
        public string PropertyName;
        public string SqlToCheck = null;

        static Size? LastSize = null;
        static Point? LastLocation = null;

        ToolStripMenuItem samplesMenuItem = new ToolStripMenuItem("Samples...");

        public SQLEditorForm()
        {
            InitializeComponent();
            ScintillaHelper.Init(sqlTextBox, Lexer.Sql);
            toolStripStatusLabel.Image = null;

            ShowIcon = true;
            Icon = Repository.ProductIcon;

            this.Load += SQLEditorForm_Load;
            this.FormClosed += SQLEditorForm_FormClosed;
            this.FormClosing += SQLEditorForm_FormClosing;
            this.sqlTextBox.KeyDown += TextBox_KeyDown;
            this.KeyDown += TextBox_KeyDown;
        }

        void SQLEditorForm_Load(object sender, EventArgs e)
        {
            if (LastSize != null) Size = LastSize.Value;
            if (LastLocation != null) Location = LastLocation.Value;
            sqlTextBox.SetSavePoint();
        }

        bool CheckClose()
        {
            if (sqlTextBox.Modified)
            {
                if (MessageBox.Show("The text has been modified. Do you really want to exit ?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel) return false;
            }
            return true;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) cancelToolStripButton_Click(sender, e);
        }

        void SQLEditorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            LastSize = Size;
            LastLocation = Location;
        }

        private void SQLEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CheckClose())
            {
                e.Cancel = true;
            }
        }

        public void SetReadOnly()
        {
            sqlTextBox.ReadOnly = true;
            okToolStripButton.Visible = false;
            cancelToolStripButton.Text = "Close";
            clearToolStripButton.Visible = false;

            checkSQLToolStripButton.Visible = !string.IsNullOrEmpty(sqlTextBox.Text);
            copyToolStripButton.Visible = !string.IsNullOrEmpty(sqlTextBox.Text);
        }

        private void cancelToolStripButton_Click(object sender, EventArgs e)
        {
            if (CheckClose())
            {
                sqlTextBox.SetSavePoint();
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void okToolStripButton_Click(object sender, EventArgs e)
        {
            sqlTextBox.SetSavePoint();
            DialogResult = DialogResult.OK;
            Close();
        }

        public void checkSQLToolStripButton_Click(object sender, EventArgs e)
        {
            checkSQL();
        }

        public void checkSQL()
        {
            string initialSQL = "", error = "";
            try
            {
                if (Instance is ReportModel)
                {
                    ReportModel model = Instance as ReportModel;
                    if (PropertyName == "PreSQL" || PropertyName == "PostSQL")
                    {
                        error = model.Source.CheckSQL(RazorHelper.CompileExecute(sqlTextBox.Text, model), null, model, true);
                    }
                    else
                    {
                        error = model.Source.CheckSQL(!string.IsNullOrEmpty(SqlToCheck) ? SqlToCheck : sqlTextBox.Text, model.FromTables, model, false);
                    }
                }
                if (Instance is MetaEnum)
                {
                    MetaEnum anEnum = Instance as MetaEnum;
                    error = anEnum.Source.CheckSQL(RazorHelper.CompileExecute(sqlTextBox.Text, anEnum), null, null, false);
                }
                else if (Instance is MetaSource)
                {
                    MetaSource source = Instance as MetaSource;
                    if (PropertyName == "PreSQL" || PropertyName == "PostSQL")
                    {
                        error = source.CheckSQL(RazorHelper.CompileExecute(sqlTextBox.Text, source), null, null, true);
                    }
                }
                else if (Instance is MetaTable)
                {
                    MetaTable table = Instance as MetaTable;
                    if (PropertyName == "PreSQL" || PropertyName == "PostSQL")
                    {
                        error = table.Source.CheckSQL(RazorHelper.CompileExecute(sqlTextBox.Text, table), null, null, true);
                    }
                    else
                    {
                        if (PropertyName == "WhereSQL")
                        {
                            initialSQL = table.WhereSQL;
                            table.WhereSQL = RazorHelper.CompileExecute(sqlTextBox.Text, table);
                        }
                        else
                        {
                            initialSQL = table.Sql;
                            table.Sql = sqlTextBox.Text;
                        }
                        try
                        {
                            table.CheckTable(null);
                            if (sqlTextBox.ReadOnly) table.SetReadOnly();
                            error = table.Error;
                        }
                        finally
                        {
                            if (PropertyName == "WhereSQL")
                            {
                                table.WhereSQL = initialSQL;
                            }
                            else
                            {
                                table.Sql = initialSQL;
                            }
                        }
                    }
                }
                else if (Instance is MetaJoin)
                {
                    MetaJoin join = Instance as MetaJoin;
                    initialSQL = join.Clause;
                    try
                    {
                        join.Clause = sqlTextBox.Text;
                        join.CheckJoin();
                        if (sqlTextBox.ReadOnly) join.SetReadOnly();
                        error = join.Error;
                    }
                    finally
                    {
                        join.Clause = initialSQL;
                    }
                }
                else if (Instance is ReportTask)
                {
                    ReportTask task = Instance as ReportTask;
                    error = task.Source.CheckSQL(RazorHelper.CompileExecute(sqlTextBox.Text, task), null, null, false);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            errorTextBox.Text = error;
            if (!string.IsNullOrEmpty(error))
            {
                toolStripStatusLabel.Text = "Error";
                toolStripStatusLabel.Image = global::Seal.Properties.Resources.error2;
            }
            else
            {
                toolStripStatusLabel.Text = "SQL checked successfully";
                toolStripStatusLabel.Image = global::Seal.Properties.Resources.checkedGreen;
            }
        }

        private void clearToolStripButton_Click(object sender, EventArgs e)
        {
            sqlTextBox.Text = "";
        }

        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(sqlTextBox.Text))
            {
                Clipboard.SetText(sqlTextBox.Text);
                toolStripStatusLabel.Text = "Text copied to clipboard";
                toolStripStatusLabel.Image = null;
            }
        }


        public void SetSamples(List<string> samples)
        {
            foreach (string sample in samples)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(sample);
                item.Click += new System.EventHandler(this.item_Click);
                samplesMenuItem.DropDownItems.Add(item);
            }
            if (!mainToolStrip.Items.Contains(samplesMenuItem)) mainToolStrip.Items.Add(samplesMenuItem);
        }

        void item_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem) sqlTextBox.Text = ((ToolStripMenuItem)sender).Text;
        }
    }
}
