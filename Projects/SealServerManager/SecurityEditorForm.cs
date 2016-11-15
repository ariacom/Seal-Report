//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Seal.Helpers;
using Seal.Model;

namespace Seal.Forms
{
    public partial class SecurityEditorForm : Form
    {
        SealSecurity _security;

        public SecurityEditorForm(SealSecurity security)
        {
            Visible = false;
            _security = security;

            InitializeComponent();
            toolStripStatusLabel.Image = null;

            security.InitEditor();
            mainPropertyGrid.ToolbarVisible = false;
            mainPropertyGrid.SelectedObject = security;
            mainPropertyGrid.PropertySort = PropertySort.Categorized;
            mainPropertyGrid.PropertyValueChanged += mainPropertyGrid_PropertyValueChanged;

            Text = Repository.SealRootProductName + " Security Editor";

            ShowIcon = true;
            Icon = Properties.Resources.serverManager; 
        }

        private void ConfigurationEditorForm_Load(object sender, EventArgs e)
        {

                infoTextBox.Text = @"This editor allows to configure the security used to publish your reports on the Web Server.

Security groups define which repository folders are published and which columns are shown in the Web Report Designer.
The security provider performs the authentication and select the security groups of the user.
";
            
            Visible = true;
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

        private void summaryToolStripButton_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                string result = _security.GetSecuritySummary();
                ExecutionForm frm = new ExecutionForm(null);
                frm.Text = "Security summary";
                frm.cancelToolStripButton.Visible = false;
                frm.pauseToolStripButton.Visible = false;
                frm.logTextBox.Text = result;
                frm.logTextBox.SelectionStart = 0;
                frm.logTextBox.SelectionLength = 0;
                frm.ShowDialog();
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        void mainPropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (e.ChangedItem.PropertyDescriptor.Name == "ProviderName" && _security.UseCustomScript && !string.IsNullOrEmpty(_security.Script))
            {
                var result = MessageBox.Show("The custom script for this security provider will not be valid anymore, the 'Use Custom Script' property will be set to false. Do you want to continue ?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    _security.UseCustomScript = false;
                }
                else
                {
                    _security.ProviderName = e.OldValue.ToString();
                    return;
                }
            }
        }
    }
}
