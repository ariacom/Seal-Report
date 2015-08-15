//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Seal.Model;

namespace Seal.Controls
{
    public partial class ViewPanel : UserControl
    {
        public ReportView View;
        ReportDesigner MainForm;


        public ViewPanel()
        {
            InitializeComponent();
        }

        public void Init(ReportDesigner mainForm)
        {
            MainForm = mainForm;

            templateComboBox.SelectedIndexChanged -= templateComboBox_SelectedIndexChanged;
            templateComboBox.DataSource = null;
            templateComboBox.Items.Clear();
            //templateComboBox.DataSource = MainForm.Report.Templates;
            templateComboBox.DisplayMember = "Name";
            templateComboBox.Refresh();
          //  templateComboBox.SelectedItem = MainForm.Report.Templates.FirstOrDefault(i => i.Name == View.ViewName);
            templateComboBox.SelectedIndexChanged += templateComboBox_SelectedIndexChanged;

            modelComboBox.SelectedIndexChanged -= modelComboBox_SelectedIndexChanged;
            modelComboBox.DataSource = null;
            modelComboBox.Items.Clear();
            modelComboBox.DataSource = MainForm.Report.Models;
            modelComboBox.DisplayMember = "NameEx";
            modelComboBox.Refresh();
            modelComboBox.SelectedItem = MainForm.Report.Models.FirstOrDefault(i => i.GUID == View.ModelGUID); ;
            modelComboBox.SelectedIndexChanged += modelComboBox_SelectedIndexChanged;

            UpdateControls();
        }

        void UpdateControls()
        {
            modelComboBox.Visible = (View.Model != null);
            modelLabel.Visible = modelComboBox.Visible;
        }

        private void templateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
       //     View.ViewName = ((ViewTemplate)templateComboBox.SelectedItem).Name;
            MainForm.IsModified = true;
        }

        private void modelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (modelComboBox.SelectedItem != null) View.ModelGUID = ((ReportModel)modelComboBox.SelectedItem).GUID;
            MainForm.IsModified = true;
        }
    }
}
