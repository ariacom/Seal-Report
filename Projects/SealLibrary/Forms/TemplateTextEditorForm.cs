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
using Seal.Model;
using RazorEngine;
using RazorEngine.Templating;
using Seal.Helpers;

namespace Seal.Forms
{
    public partial class TemplateTextEditorForm : Form
    {
        public ReportView View;
        public Type TypeForCheckSyntax = null;
        public string TextToAddForCheck = "";

        ToolStripMenuItem samplesMenuItem = new ToolStripMenuItem("Samples...");

        static Size? LastSize = null;
        static Point? LastLocation = null;

        public TemplateTextEditorForm()
        {
            InitializeComponent();
            textBox.ConfigurationManager.Language = "html";
            textBox.EndOfLine.Mode = ScintillaNET.EndOfLineMode.Crlf;
            toolStripStatusLabel.Image = null;
            ShowIcon = true;
            Icon = Repository.ProductIcon;

            this.Load += TemplateTextEditorForm_Load;
            this.FormClosed += TemplateTextEditorForm_FormClosed;
            this.textBox.KeyDown += TextBox_KeyDown;
            this.KeyDown += TextBox_KeyDown;
        }

        bool CheckClose()
        {
            if (textBox.Modified)
            {
                if (MessageBox.Show("The text has been modified. Do you really want to exit ?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel) return false;
            }
            return true;
        }

        private void TemplateTextEditorForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape) cancelToolStripButton_Click(sender, e);
        }

        void TemplateTextEditorForm_Load(object sender, EventArgs e)
        {
            if (LastSize != null) Size = LastSize.Value;
            if (LastLocation != null) Location = LastLocation.Value;
            textBox.Modified = false;
        }

        void TemplateTextEditorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            LastSize = Size;
            LastLocation = Location;
        }

        private void cancelToolStripButton_Click(object sender, EventArgs e)
        {
            if (CheckClose())
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) cancelToolStripButton_Click(sender, e);
        }

        private void okToolStripButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }


        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox.Text);
            toolStripStatusLabel.Text = "Text copied to clipboard";
            toolStripStatusLabel.Image = null;
        }

        private void checkSyntaxToolStripButton_Click(object sender, EventArgs e)
        {
            string error = "";
            try
            {
                string script = textBox.Text;
                if (!string.IsNullOrEmpty(TextToAddForCheck)) script += "\r\n" + TextToAddForCheck;
                RazorHelper.Compile(script, TypeForCheckSyntax, Guid.NewGuid().ToString());
            }
            catch (TemplateCompilationException ex)
            {
                error = string.Format("Compilation error:\r\n{0}", Helper.GetExceptionMessage(ex));
                if (ex.InnerException != null) error += "\r\n" + ex.InnerException.Message;
                if (error.ToLower().Contains("are you missing an assembly reference")) error += string.Format("\r\nNote that you can add assemblies to load by copying your .dll files in the Assemblies Repository folder:'{0}'", Repository.Instance.AssembliesFolder);
            }
            catch (Exception ex)
            {
                error = string.Format("Compilation error:\r\n{0}", ex.Message);
                if (ex.InnerException != null) error += "\r\n" + ex.InnerException.Message;
            }

            if (!string.IsNullOrEmpty(error))
            {
                toolStripStatusLabel.Text = "Compilation error";
                toolStripStatusLabel.Image = global::Seal.Properties.Resources.error2;

                MessageBox.Show(error, "Check syntax", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                toolStripStatusLabel.Text = "Razor Syntax is OK";
                toolStripStatusLabel.Image = global::Seal.Properties.Resources.checkedGreen;
            }
        }

        public void SetSamples(List<string> samples)
        {
            foreach (string sample in samples)
            {
                string title = sample, value = sample;
                if (sample.Contains("|"))
                {
                    var index = sample.LastIndexOf('|');
                    title = sample.Substring(index+1);
                    value = sample.Substring(0, index);
                }
                ToolStripMenuItem item = new ToolStripMenuItem(title);
                item.Click += new System.EventHandler(this.item_Click);
                item.Tag = value;
                item.ToolTipText = value.Length > 250 ? value.Substring(0,250) + "..." : value;
                samplesMenuItem.DropDownItems.Add(item);
            }
            if (!mainToolStrip.Items.Contains(samplesMenuItem)) mainToolStrip.Items.Add(samplesMenuItem);
        }

        void item_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem) textBox.Text = ((ToolStripMenuItem) sender).Tag.ToString();
        }

        public void SetResetText(string resetText)
        {
            if (!string.IsNullOrEmpty(resetText))
            {
                var resetButton = new ToolStripButton("Reset script");
                resetButton.Click += new EventHandler(delegate(object sender2, EventArgs e2) { textBox.Text = resetText; });
                mainToolStrip.Items.Add(resetButton);
            }
        }

    }
}
