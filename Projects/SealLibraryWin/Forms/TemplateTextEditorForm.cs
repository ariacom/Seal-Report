//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Seal.Model;
using Seal.Helpers;
using ScintillaNET;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.ComponentModel;
using DiffPlex.WindowsForms;

namespace Seal.Forms
{
    public interface IReportTester
    {
        public bool CanExecute();
        public bool CanRender();
        public void TestExecute(object instance, string propertyName, string value, bool render);
    }

    public partial class TemplateTextEditorForm : Form
    {
        public object ObjectForCheckSyntax = null;

        public object ContextInstance = null;
        public PropertyDescriptor ContextPropertyDescriptor = null;
        public string ContextPropertyName = null;
        public bool IsRawCSharp = false;

        public Scintilla textBox = new Scintilla();

        ToolStripMenuItem samplesMenuItem = new ToolStripMenuItem("Samples...");
        ToolStripMenuItem samplesMenuItem2 = new ToolStripMenuItem("Samples (Notepad)");
        ToolStripMenuItem copyToolStripButton = new ToolStripMenuItem("Copy to clipboard") { };
        public ToolStripMenuItem checkSyntaxToolStripButton = new ToolStripMenuItem("F8 Check Syntax") { ShortcutKeys = Keys.F8, ShowShortcutKeys = true };
        ToolStripMenuItem testExecutionMenuItem = new ToolStripMenuItem("F5 Execute...") { ShortcutKeys = Keys.F5, ShowShortcutKeys = true };
        ToolStripMenuItem testRenderingMenuItem = new ToolStripMenuItem("F6 Render...") { ShortcutKeys = Keys.F6, ShowShortcutKeys = true };

        static Size? LastSize = null;
        static Point? LastLocation = null;
        public static IReportTester ReportTester = null;
        public DifferenceForm DifferenceViewer = null;

        Dictionary<int, string> _compilationErrors = new Dictionary<int, string>();

        public TemplateTextEditorForm()
        {
            InitializeComponent();
            mainPanel.Controls.Add(textBox);
            textBox.Dock = DockStyle.Fill;
            ScintillaHelper.Init(textBox, Lexer.Html);
            textBox.HScrollBar = true;
            textBox.VScrollBar = true;
            toolStripStatusLabel.Image = null;
            ShowIcon = true;
            Icon = Repository.ProductIcon;

            this.Load += TemplateTextEditorForm_Load;
            this.FormClosing += TemplateTextEditorForm_FormClosing;
            this.FormClosed += TemplateTextEditorForm_FormClosed;
            this.textBox.KeyDown += TextBox_KeyDown;
            this.KeyDown += TextBox_KeyDown;

            mainToolStrip.Items.Add(checkSyntaxToolStripButton);
            checkSyntaxToolStripButton.Click += checkSyntaxToolStripButton_Click;
            checkSyntaxToolStripButton.Image = global::Seal.Properties.Resources.helper;
            checkSyntaxToolStripButton.ImageTransparentColor = System.Drawing.Color.White;

            if (ReportTester != null)
            {
                mainToolStrip.Items.Add(testExecutionMenuItem);
                testExecutionMenuItem.Click += TestExecutionMenuItem_Click;
                testExecutionMenuItem.Image = global::Seal.Properties.Resources.execute;
                testExecutionMenuItem.ImageTransparentColor = System.Drawing.Color.White;
                testExecutionMenuItem.Enabled = ReportTester.CanExecute();

                mainToolStrip.Items.Add(testRenderingMenuItem);
                testRenderingMenuItem.Click += TestExecutionMenuItem_Click;
                testRenderingMenuItem.Image = global::Seal.Properties.Resources.render;
                testRenderingMenuItem.ImageTransparentColor = System.Drawing.Color.White;
                testRenderingMenuItem.Enabled = ReportTester.CanRender();
            }

            mainToolStrip.Items.Add(copyToolStripButton);
            copyToolStripButton.Click += copyToolStripButton_Click;

        }

        private void TestExecutionMenuItem_Click(object sender, EventArgs e)
        {
            string error = CheckSyntax();
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "Check syntax", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                mainTimer.Enabled = false;
                testRenderingMenuItem.Enabled = false;
                testExecutionMenuItem.Enabled = false;
                if (ContextInstance != null)
                {
                    var instance = ContextInstance;
                    var propertyName = !string.IsNullOrEmpty(ContextPropertyName) ? ContextPropertyName : ContextPropertyDescriptor.Name;
                    var value = textBox.Text;

                    var functionsEditor = ContextInstance as FunctionsEditor;
                    if (functionsEditor != null)
                    {
                        //Handle case if edited in functions editor
                        if (functionsEditor.SourceObject is ReportTask)
                        {
                            var task = (ReportTask)functionsEditor.SourceObject;
                            value = functionsEditor.ReplaceFunction(task.Script, ContextPropertyDescriptor.DisplayName, value);
                            propertyName = "Script";
                            instance = task;
                        }
                        else if (functionsEditor.SourceObject is MetaTable)
                        {
                            var metaTable = (MetaTable)functionsEditor.SourceObject;
                            value = functionsEditor.ReplaceFunction(metaTable.LoadScript, ContextPropertyDescriptor.DisplayName, value);
                            propertyName = "LoadScript";
                            instance = metaTable;
                        }
                    }

                    ReportTester?.TestExecute(instance, propertyName, value, sender == testRenderingMenuItem);
                }
                mainTimer.Enabled = true;
            }
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
            textBox.IndicatorClick += TextBox_IndicatorClick;
            textBox.SetSavePoint();
        }

        void TemplateTextEditorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            LastSize = Size;
            LastLocation = Location;
            if (DifferenceViewer != null) DifferenceViewer.Close();
        }

        private void TemplateTextEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CheckClose())
            {
                e.Cancel = true;
            }
        }


        private void cancelToolStripButton_Click(object sender, EventArgs e)
        {
            if (CheckClose())
            {
                textBox.SetSavePoint();
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
            if (textBox.Modified && ObjectForCheckSyntax != null)
            {
                var error = CheckSyntax();
                if (!string.IsNullOrEmpty(error))
                {
                    if (MessageBox.Show("The Razor syntax is incorrect. Do you really want to save this script and exit ?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel) return;
                }
            }

            DialogResult = textBox.Modified ? DialogResult.OK : DialogResult.Cancel;
            textBox.SetSavePoint();
            Close();
        }


        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox.Text);
            toolStripStatusLabel.Text = "Text copied to clipboard";
            toolStripStatusLabel.Image = null;
        }

        public string CheckSyntax()
        {
            string error = "";
            if (IsRawCSharp) ObjectForCheckSyntax = new object(); //Dummy object


            if (ObjectForCheckSyntax != null)
            {
                try
                {
                    var finalScript = "";
                    if (IsRawCSharp)
                    {
                        if (!textBox.Text.Contains("namespace ") || !textBox.Text.Contains("class "))
                        {
                            throw new Exception("C# code expected: it must contain 'namespace' and 'class' keywords.");
                        }

                        //case of Raw C# (for dynamics), convert to a Razor script
                        var lines = textBox.Text.Replace("\r\n", "\r").Replace("\n", "\r").Split("\r");
                        finalScript = "";
                        foreach(var line in lines)
                        {
                            var newLine = line;
                            if (line.Trim().StartsWith("using ")) newLine = line.Replace("using", "@using");
                            if (line.Trim().StartsWith("namespace "))
                            {
                                newLine = "@functions "  + (line.Contains("{") ? "{" : "");
                            }
                            finalScript += newLine + "\r\n";
                        }
                    }
                    else
                    {
                        if (ContextInstance is FunctionsEditor)
                        {
                            var editor = (FunctionsEditor)ContextInstance;
                            var script = "";
                            if (ObjectForCheckSyntax is ReportTask) script = ((ReportTask)ObjectForCheckSyntax).Script;
                            if (ObjectForCheckSyntax is MetaTable) script = ((MetaTable)ObjectForCheckSyntax).LoadScript;

                            finalScript = editor.ReplaceFunction(script, ContextPropertyDescriptor.DisplayName, textBox.Text);
                        }
                    }

                    FormHelper.CheckRazorSyntax(textBox, ObjectForCheckSyntax, _compilationErrors, finalScript);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }

                if (!string.IsNullOrEmpty(error))
                {
                    toolStripStatusLabel.Text = "Compilation error";
                    toolStripStatusLabel.Image = global::Seal.Properties.Resources.error2;
                }
                else
                {
                    toolStripStatusLabel.Text = IsRawCSharp ? "C# Syntax is OK" : "Razor Syntax is OK";
                    toolStripStatusLabel.Image = global::Seal.Properties.Resources.checkedGreen;
                }
            }

            return error;
        }

        private void TextBox_IndicatorClick(object sender, IndicatorClickEventArgs e)
        {
            if (_compilationErrors.ContainsKey(e.Position)) textBox.CallTipShow(e.Position, _compilationErrors[e.Position]);
        }


        private void checkSyntaxToolStripButton_Click(object sender, EventArgs e)
        {
            string error = CheckSyntax();
            if (!string.IsNullOrEmpty(error))
            {
                if (error.Length > 2000) error = error.Substring(0, 2000) + "...";
                MessageBox.Show(error, "Check syntax", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    title = sample.Substring(index + 1);
                    value = sample.Substring(0, index);
                }
                ToolStripMenuItem item = new ToolStripMenuItem(title);
                item.ToolTipText = value.Length > 900 ? value.Substring(0, 900) + "..." : value;
                item.Click += new System.EventHandler(this.item_Click);
                item.Tag = value;
                samplesMenuItem.DropDownItems.Add(item);

                ToolStripMenuItem item2 = new ToolStripMenuItem(title);
                item2.ToolTipText = item.ToolTipText;
                item2.Click += new System.EventHandler(this.item_Click2);
                item2.Tag = value;
                samplesMenuItem2.DropDownItems.Add(item2);
            }
            if (samples.Count > 0 && !mainToolStrip.Items.Contains(samplesMenuItem)) mainToolStrip.Items.Add(samplesMenuItem);
            if (samples.Count > 0 && !mainToolStrip.Items.Contains(samplesMenuItem2)) mainToolStrip.Items.Add(samplesMenuItem2);
        }

        void item_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                textBox.Text = ((ToolStripMenuItem)sender).Tag.ToString();
            }
        }

        void item_Click2(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                var path = FileHelper.GetTempUniqueFileName("sample.txt");
                File.WriteAllText(path, ((ToolStripMenuItem)sender).Tag.ToString(), Encoding.UTF8);
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(path) { UseShellExecute = true };
                p.Start();
            }
        }

        public void SetResetText(string resetText)
        {
            if (!string.IsNullOrEmpty(resetText))
            {
                var resetButton = new ToolStripButton("Reset script");
                resetButton.ToolTipText = "Reset to reference script";
                resetButton.Click += new EventHandler(delegate (object sender2, EventArgs e2) {
                    textBox.Text = resetText; 
                });
                mainToolStrip.Items.Add(resetButton);
                var differenceButton = new ToolStripButton("Differences");
                differenceButton.ToolTipText = "Show differences with the reference script";
                differenceButton.Click += new EventHandler(delegate (object sender2, EventArgs e2) {
                    if (DifferenceViewer != null) DifferenceViewer.Init();
                    else DifferenceViewer = new DifferenceForm(textBox, resetText);
                    DifferenceViewer.Show();
                });
                mainToolStrip.Items.Add(differenceButton);
            }
        }

        private void mainTimer_Tick(object sender, EventArgs e)
        {
            if (ReportTester != null)
            {
                testExecutionMenuItem.Enabled = ReportTester.CanExecute();
                testRenderingMenuItem.Enabled = ReportTester.CanRender();
            }
        }
    }
}
