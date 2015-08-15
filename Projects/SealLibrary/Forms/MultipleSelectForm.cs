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
using System.Data.OleDb;
using System.Reflection;
using Seal.Model;

namespace Seal.Forms
{
    public partial class MultipleSelectForm : Form
    {
        bool _selectAll = true;
        List<object> _source = new List<object>();
        string _displayMember;
        public List<object> CheckedItems
        {
            get
            {
                List<object> checkedItems = new List<object>();
                foreach (var item in checkedListBox.CheckedItems) checkedItems.Add(item);
                return checkedItems;
            }
        }

        public MultipleSelectForm(string title, object source, string displayMember)
        {
            InitializeComponent();

            Text = title;
            _displayMember = displayMember;
            checkedListBox.DataSource = source;
            checkedListBox.DisplayMember = displayMember;
            foreach (var item in checkedListBox.Items)
            {
                _source.Add(item);
                filterToolStripTextBox.AutoCompleteCustomSource.Add(GetPropertyValue(item, _displayMember));
            }
            filterToolStripTextBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            filterToolStripTextBox.AutoCompleteSource = AutoCompleteSource.CustomSource;

            ShowIcon = true;
            Icon = Repository.ProductIcon;
        }

        private void MultipleSelectForm_Load(object sender, EventArgs e)
        {
        }

        private void MultipleSelectForm_Shown(object sender, EventArgs e)
        {
            filterToolStripTextBox.Focus();
            enableControls();
        }


        void enableControls()
        {
            selectAllToolStripButton.Text = _selectAll ? "Select All" : "Unselect All";
            toolStripStatusLabel.Text = string.Format("{0}/{1} Item(s) Selected", checkedListBox.CheckedItems.Count, checkedListBox.Items.Count);
        }

        private void okToolStripButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelToolStripButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void selectAllToolStripButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox.Items.Count; i++)
            {
                checkedListBox.SetItemChecked(i, _selectAll);
            }
            _selectAll = !_selectAll;
            enableControls();
        }

        private void filterToolStripTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                string filter = filterToolStripTextBox.Text.ToLower();
                checkedListBox.BeginUpdate();
                List<object> checkedItems = CheckedItems;
                List<object> filteredItems = new List<object>();
                for (int i = 0; i < _source.Count; i++)
                {
                    object item = _source[i];
                    if (checkedItems.Contains(item) || GetPropertyValue(item, _displayMember).ToLower().Contains(filter)) filteredItems.Add(item);
                }
                checkedListBox.DataSource = filteredItems;
                checkedListBox.DisplayMember = _displayMember;
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                {
                    checkedListBox.SetItemChecked(i, checkedItems.Contains(checkedListBox.Items[i]));
                }
            }
            finally
            {
                checkedListBox.EndUpdate();
                Cursor.Current = Cursors.Default;
            }
            enableControls();
        }

        public static string GetPropertyValue(object item, string fieldName)
        {
            PropertyInfo classProperty = GetFieldProperty(fieldName, item.GetType());
            if (classProperty == null)
            {
                if (item is DataRowView)
                {
                    object value = ((DataRowView)item)[fieldName];
                    return (value != null ? value.ToString() : "");
                }
            }

            if (classProperty != null)
            {
                object value = classProperty.GetValue(item, null);
                if (value != null) return value.ToString();
            }

            return "";
        }

        public static PropertyInfo GetFieldProperty(string fieldName, Type type)
        {
            return type.GetProperties().Where(i => i.Name == fieldName).FirstOrDefault();
        }

        private void checkedListBox_MouseUp(object sender, MouseEventArgs e)
        {
            enableControls();
        }

        private void checkedListBox_KeyUp(object sender, KeyEventArgs e)
        {
            enableControls();
        }

        private void sortToolStripButton_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                checkedListBox.BeginUpdate();
                List<object> checkedItems = CheckedItems;
                List<object> items = new List<object>();
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                {
                    items.Insert(0, checkedListBox.Items[i]);
                }
                checkedListBox.DisplayMember = _displayMember; 
                checkedListBox.DataSource = items;
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                {
                    checkedListBox.SetItemChecked(i, checkedItems.Contains(checkedListBox.Items[i]));
                }
            }
            finally
            {
                checkedListBox.EndUpdate();
                Cursor.Current = Cursors.Default;
            }
            enableControls();
        }

    }
}
