//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
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
        ToolStripMenuItem samplesMenuItem = new ToolStripMenuItem("Samples...");

        static Size? LastSize = null;
        static Point? LastLocation = null;

        public TemplateTextEditorForm()
        {
            InitializeComponent();
            textBox.ConfigurationManager.Language = "html";
            toolStripStatusLabel.Image = null;
            ShowIcon = true;
            Icon = Repository.ProductIcon;

            this.Load += TemplateTextEditorForm_Load;
            this.FormClosed += TemplateTextEditorForm_FormClosed;
        }

        void TemplateTextEditorForm_Load(object sender, EventArgs e)
        {
            if (LastSize != null) Size = LastSize.Value;
            if (LastLocation != null) Location = LastLocation.Value;
        }

        void TemplateTextEditorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            LastSize = Size;
            LastLocation = Location;
        }

        private void cancelToolStripButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
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
                Helper.CompileRazor(textBox.Text, TypeForCheckSyntax, "acachename");
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
                ToolStripMenuItem item = new ToolStripMenuItem(sample);
                item.Click += new System.EventHandler(this.item_Click);
                samplesMenuItem.DropDownItems.Add(item);
            }
            if (!mainToolStrip.Items.Contains(samplesMenuItem)) mainToolStrip.Items.Add(samplesMenuItem);
        }

        void item_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem) textBox.Text = ((ToolStripMenuItem)sender).Text;
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
