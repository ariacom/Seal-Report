//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Drawing;
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
        public bool WarningOnError = false;

        Dictionary<int, string> _compilationErrors = new Dictionary<int, string>();

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
            sqlTextBox.IndicatorClick += SqlTextBox_IndicatorClick;
            sqlTextBox.SetSavePoint();
        }

        private void SqlTextBox_IndicatorClick(object sender, IndicatorClickEventArgs e)
        {
            if (_compilationErrors.ContainsKey(e.Position)) sqlTextBox.CallTipShow(e.Position, _compilationErrors[e.Position]);
        }

        bool CheckClose()
        {
            if (sqlTextBox.Modified)
            {
                if (MessageBox.Show("The SQL has been modified. Do you really want to exit ?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel) return false;
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
            if (sqlTextBox.Modified && WarningOnError)
            {
                checkSQL();
                if (!string.IsNullOrEmpty(errorTextBox.Text))
                {
                    if (MessageBox.Show("The SQL is incorrect. Do you really want to save this SQL and exit ?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel) return;
                }
            }

            DialogResult = DialogResult.OK;
            sqlTextBox.SetSavePoint();
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
                        if (sqlTextBox.Text.StartsWith("@")) FormHelper.CheckRazorSyntax(sqlTextBox, "", model, _compilationErrors);
                        error = model.Source.CheckSQL(RazorHelper.CompileExecute(sqlTextBox.Text, model), null, model, true);
                    }
                    else
                    {
                        var sql = !string.IsNullOrEmpty(SqlToCheck) ? SqlToCheck : sqlTextBox.Text;
                        error = model.Source.CheckSQL(sql, model.FromTables, model, false);
                    }
                }
                if (Instance is MetaEnum)
                {
                    MetaEnum anEnum = Instance as MetaEnum;
                    if (sqlTextBox.Text.StartsWith("@")) FormHelper.CheckRazorSyntax(sqlTextBox, "", anEnum, _compilationErrors);
                    error = anEnum.Source.CheckSQL(RazorHelper.CompileExecute(sqlTextBox.Text, anEnum), null, null, false);
                }
                else if (Instance is MetaSource)
                {
                    MetaSource source = Instance as MetaSource;
                    if (PropertyName == "PreSQL" || PropertyName == "PostSQL")
                    {
                        if (sqlTextBox.Text.StartsWith("@")) FormHelper.CheckRazorSyntax(sqlTextBox, "", source, _compilationErrors);
                        error = source.CheckSQL(RazorHelper.CompileExecute(sqlTextBox.Text, source), null, null, true);
                    }
                }
                else if (Instance is MetaTable)
                {
                    MetaTable table = Instance as MetaTable;
                    if (PropertyName == "PreSQL" || PropertyName == "PostSQL")
                    {
                        if (sqlTextBox.Text.StartsWith("@")) FormHelper.CheckRazorSyntax(sqlTextBox, "", table, _compilationErrors);
                        error = table.Source.CheckSQL(RazorHelper.CompileExecute(sqlTextBox.Text, table), null, null, true);
                    }
                    else
                    {
                        if (PropertyName == "WhereSQL")
                        {
                            initialSQL = table.WhereSQL;
                            if (sqlTextBox.Text.StartsWith("@")) FormHelper.CheckRazorSyntax(sqlTextBox, "", table, _compilationErrors);
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
                    if (sqlTextBox.Text.StartsWith("@")) FormHelper.CheckRazorSyntax(sqlTextBox, "", task, _compilationErrors);
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
            if (samples.Count > 0 && !mainToolStrip.Items.Contains(samplesMenuItem)) mainToolStrip.Items.Add(samplesMenuItem);
        }

        void item_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem) sqlTextBox.Text = ((ToolStripMenuItem)sender).Text;
        }
    }
}
