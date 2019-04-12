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
using System.Windows.Forms;
using Seal.Model;
using System.IO;
using Seal.Helpers;

namespace Seal.Forms
{
    public partial class SmartCopyForm : Form
    {
        class PropertyItem
        {
            public object Object { get; set; }
            public string Name { get; set; }
        }

        static Size? LastSize = null;
        static Point? LastLocation = null;

        const string ReportsKeyword = "[Reports]";
        const string ModelsKeyword = "[Models]";
        const string ElementsKeyword = "[Elements]";
        const string RestrictionsKeyword = "[Restrictions]";
        const string ViewsKeyword = "[Views]";
        const string OutputsKeyword = "[Outputs]";
        const string TasksKeyword = "[Tasks]";
        const string TasksFolderKeyword = "[Tasks Script]";

        object _source = null;
        Report _report = null;
        static List<PropertyItem> _lastReports = new List<PropertyItem>();
        List<PropertyItem> _destinationItems = new List<PropertyItem>();
        public bool IsReportModified = false;

        public SmartCopyForm(string title, object source, Report report)
        {
            InitializeComponent();

            Text = title;

            _source = source;
            _report = report;

            ShowIcon = true;
            Icon = Repository.ProductIcon;
        }

        private void SmartCopyForm_Load(object sender, EventArgs e)
        {

            string sourceName = "";
            if (_source is ReportModel)
            {
                sourceName = "[Model Properties] ";
                addRadioButton.Text = "Add the model to the destination reports selected";
                updateRadioButton.Text = "Update selected properties, elements or restrictions to the destination models selected";
            }
            else if (_source is ReportRestriction)
            {
                sourceName = "[Restriction Properties] ";
                addRadioButton.Text = "Add the restriction to the destination models selected";
                updateRadioButton.Text = "Update selected properties to the destination restrictions selected";
            }
            else if (_source is ReportElement)
            {
                sourceName = "[Element Properties] ";
                addRadioButton.Text = "Add the element to the destination models selected";
                updateRadioButton.Text = "Update selected properties to the destination elements selected";
            }
            else if (_source is ReportView)
            {
                sourceName = "[View Properties] ";
                addRadioButton.Text = "Add the view to the destination views selected";
                updateRadioButton.Text = "Update selected properties, parameter values to the destination views selected";
            }
            else if (_source is ReportTask)
            {
                sourceName = "[Task Properties] ";
                addRadioButton.Text = "Add the task to the destination reports selected";
                updateRadioButton.Text = "Update selected properties to the destination tasks selected";
            }
            else if (_source is ReportOutput)
            {
                sourceName = "[Output Properties] ";
                addRadioButton.Text = "Add the output to the destination reports selected";
                updateRadioButton.Text = "Update selected properties to the destination outputs selected";
            }
            else if (_source is TasksFolder)
            {
                sourceName = "[Tasks Script] ";
                addRadioButton.Visible = false;
                updateRadioButton.Text = "Update selected properties to the reports selected";
            }

            List<PropertyItem> properties = new List<PropertyItem>();
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(_source))
            {
                if (_source is ReportView && property.Name == "ChartConfigurationXml")
                {
                    properties.Add(new PropertyItem() { Name = "[View parameters] MS Chart Configuration", Object = property });
                    continue;
                }

                if (!property.IsBrowsable || property.IsReadOnly) continue;
                DisplayNameAttribute displayAtt = property.Attributes.OfType<DisplayNameAttribute>().FirstOrDefault();
                CategoryAttribute catAtt = property.Attributes.OfType<CategoryAttribute>().FirstOrDefault();
                if (displayAtt != null && catAtt != null)
                {
                    if (_source is ReportModel)
                    {
                        if (displayAtt.DisplayName.ToLower() == "source") continue;
                    }
                    else if (_source is ReportOutput)
                    {
                        if (displayAtt.DisplayName.ToLower() == "view name") continue;
                    }

                    properties.Add(new PropertyItem() { Name = string.Format("[{0}] {1}", catAtt.Category, displayAtt.DisplayName), Object = property });
                }
            }

            //Manual sort by category...
            List<PropertyItem> sortedProperties = new List<PropertyItem>();
            sortedProperties.Add(new PropertyItem() { Name = sourceName + "Select/Unselect All", Object = null });
            if (_source is ReportModel)
            {
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Model Definition]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[SQL]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Join Preferences]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Options]")));
            }
            else if (_source is ReportRestriction)
            {
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Definition]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Restriction Values]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Advanced]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Data Options]")));
            }
            else if (_source is ReportElement)
            {
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Definition]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Chart]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Options]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Data Options]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Advanced]")));
            }
            else if (_source is ReportView)
            {
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Definition]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Custom template configuration]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Custom template text]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[View parameters]")));
            }
            else if (_source is ReportTask)
            {
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Definition]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Options]")));
            }
            else if (_source is ReportOutput)
            {
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Definition]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Email Addresses]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Email Subject]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Email Body]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Email Attachments]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Folder]")));
                sortedProperties.AddRange(properties.Where(i => i.Name.StartsWith("[Restrictions]")));
            }

            propertiesCheckedListBox.DataSource = sortedProperties;
            propertiesCheckedListBox.DisplayMember = "Name";
            propertiesCheckedListBox.ClearSelected();

            source2CheckedListBox.Visible = false;
            source3CheckedListBox.Visible = false;

            if (_source is ReportModel)
            {
                ReportModel model = _source as ReportModel;
                source2CheckedListBox.Visible = true;
                source3CheckedListBox.Visible = true;

                properties = new List<PropertyItem>();
                properties.Add(new PropertyItem() { Name = "[Elements] Select/Unselect All", Object = null });
                foreach (ReportElement element in model.Elements)
                {
                    properties.Add(new PropertyItem() { Name = element.DisplayNameEl, Object = element });
                }
                source2CheckedListBox.DataSource = properties;
                source2CheckedListBox.DisplayMember = "Name";
                source2CheckedListBox.ClearSelected();

                properties = new List<PropertyItem>();
                properties.Add(new PropertyItem() { Name = "[Restriction] Select/Unselect All", Object = null });
                properties.Add(new PropertyItem() { Name = "Restriction text", Object = null });
                properties.Add(new PropertyItem() { Name = "[Aggregate] Restriction text", Object = null });
                foreach (ReportElement restriction in model.Restrictions)
                {
                    properties.Add(new PropertyItem() { Name = restriction.DisplayNameEl, Object = restriction });
                }
                foreach (ReportElement restriction in model.AggregateRestrictions)
                {
                    properties.Add(new PropertyItem() { Name = string.Format("[Aggregate] {0}", restriction.DisplayNameEl), Object = restriction });
                }
                source3CheckedListBox.DataSource = properties;
                source3CheckedListBox.DisplayMember = "Name";
                source3CheckedListBox.ClearSelected();
            }
            else if (_source is ReportView)
            {
                ReportView view = _source as ReportView;
                source2CheckedListBox.Visible = true;

                properties = new List<PropertyItem>();
                properties.Add(new PropertyItem() { Name = "[Parameters] Select/Unselect All", Object = null });
                foreach (var item in view.Parameters)
                {
                    properties.Add(new PropertyItem() { Name = item.DisplayName, Object = item });
                }
                source2CheckedListBox.DataSource = properties;
                source2CheckedListBox.DisplayMember = "Name";
                source2CheckedListBox.ClearSelected();
            }

            reportsListBox.DisplayMember = "Name";
            _lastReports.RemoveAll(i => ((Report)i.Object).FilePath == _report.FilePath);
            foreach (var item in _lastReports) reportsListBox.Items.Add(item);

            destinationCheckedListBox.DisplayMember = "Name";

            buildDestinationList();

            if (LastSize != null) Size = LastSize.Value;
            if (LastLocation != null) Location = LastLocation.Value;
        }


        string convertFileName(string fileName)
        {
            string newFileName = fileName.Replace(_report.Repository.ReportsFolder, "");
            if (newFileName.StartsWith("\\")) newFileName = newFileName.Substring(1);
            if (newFileName.EndsWith(Repository.SealReportFileExtension)) newFileName = newFileName.Substring(0, newFileName.Length - 5);
            return newFileName;
        }

        string getDestinationName()
        {
            if (addRadioButton.Checked)
            {
                if (_source is ReportModel) return ReportsKeyword;
                else if (_source is ReportRestriction) return ModelsKeyword;
                else if (_source is ReportElement) return ModelsKeyword;
                else if (_source is ReportView) return ViewsKeyword;
                else if (_source is ReportTask) return ReportsKeyword;
                else if (_source is ReportOutput) return ReportsKeyword;
            }
            else
            {
                if (_source is ReportModel) return ModelsKeyword;
                else if (_source is ReportRestriction) return RestrictionsKeyword;
                else if (_source is ReportElement) return ElementsKeyword;
                else if (_source is ReportView) return ViewsKeyword;
                else if (_source is ReportTask) return TasksKeyword;
                else if (_source is ReportOutput) return OutputsKeyword;
                else if (_source is TasksFolder) return TasksFolderKeyword;
            }
            return "";
        }

        void buildDestinationList()
        {
            string destinationName = getDestinationName();
            _destinationItems.Clear();
            _destinationItems.Add(new PropertyItem() { Name = destinationName + " Select/Unselect All", Object = null });
            addToDestinationList(_report);
            foreach (var item in reportsListBox.Items.OfType<PropertyItem>())
            {
                addToDestinationList((Report)item.Object);
            }
            applyFilter();
        }

        void addToDestinationList(Report report)
        {
            string destinationName = getDestinationName();
            string fileName = convertFileName(report.FilePath);
            if (destinationName.StartsWith(ReportsKeyword))
            {
                if (_destinationItems.FirstOrDefault(i => i.Name == fileName) == null)
                {
                    _destinationItems.Add(new PropertyItem() { Name = fileName, Object = report });
                }
            }
            else if (destinationName.StartsWith(ModelsKeyword))
            {
                foreach (var item in report.Models.Where(i => i != _source))
                {
                    string name = string.Format("[{0}] {1}", fileName, item.Name);
                    if (_destinationItems.FirstOrDefault(i => i.Name == name) == null)
                    {
                        _destinationItems.Add(new PropertyItem() { Name = name, Object = item });
                    }
                }
            }
            else if (destinationName.StartsWith(ElementsKeyword))
            {
                foreach (var model in report.Models)
                {
                    foreach (var item in model.Elements.Where(i => i != _source))
                    {
                        string name = string.Format("[{0}/{1}] {2} ", fileName, model.Name, item.DisplayNameEl);
                        if (_destinationItems.FirstOrDefault(i => i.Object == item) == null)
                        {
                            _destinationItems.Add(new PropertyItem() { Name = name, Object = item });
                        }
                    }
                }
            }
            else if (destinationName.StartsWith(RestrictionsKeyword))
            {
                bool isAggregate = !((ReportRestriction)_source).Model.Restrictions.Contains(_source);
                foreach (var model in report.Models)
                {
                    var restrictions = (isAggregate ? model.AggregateRestrictions : model.Restrictions);
                    foreach (var item in restrictions.Where(i => i != _source))
                    {
                        string name = string.Format("[{0}/{1}] {2} ", fileName, model.Name, item.DisplayNameEl);
                        if (_destinationItems.FirstOrDefault(i => i.Object == item) == null)
                        {
                            _destinationItems.Add(new PropertyItem() { Name = name, Object = item });
                        }
                    }
                }
            }
            else if (destinationName.StartsWith(ViewsKeyword))
            {
                //add a dummy view for report itself
                string reportName = string.Format("[{0}] ", fileName);
                if (addRadioButton.Checked && ((ReportView)_source).TemplateName == "Report")
                {
                    _destinationItems.Add(new PropertyItem() { Name = reportName, Object = new ReportView() { Report = report } });
                }
                foreach (var view in report.Views)
                {
                    addViewsToDestinations(view, reportName);
                }
            }
            else if (destinationName.StartsWith(TasksKeyword))
            {
                foreach (var item in report.Tasks.Where(i => i != _source))
                {
                    string name = string.Format("[{0}] {1} ", fileName, item.Name);
                    if (_destinationItems.FirstOrDefault(i => i.Object == item) == null)
                    {
                        _destinationItems.Add(new PropertyItem() { Name = name, Object = item });
                    }
                }
            }
            else if (destinationName.StartsWith(OutputsKeyword))
            {
                foreach (var item in report.Outputs.Where(i => i != _source))
                {
                    string name = string.Format("[{0}] {1} ", fileName, item.Name);
                    if (_destinationItems.FirstOrDefault(i => i.Object == item) == null)
                    {
                        _destinationItems.Add(new PropertyItem() { Name = name, Object = item });
                    }
                }
            }
            else if (destinationName.StartsWith(TasksFolderKeyword))
            {
                if (report != _report)
                {
                    string name = string.Format("[{0}] Tasks Script ", fileName);
                    _destinationItems.Add(new PropertyItem() { Name = name, Object = report });
                }
            }
        }

        void addViewsToDestinations(ReportView parentView, string prefix)
        {
            string name = string.Format("{0}/{1}", prefix, parentView.Name);
            if (_destinationItems.FirstOrDefault(i => i.Object == parentView) == null)
            {
                if (parentView != _source)
                {
                    if (!addRadioButton.Checked || ((ReportView)_source).Template.ParentNames.Contains(parentView.Template.Name))
                    {
                        _destinationItems.Add(new PropertyItem() { Name = name, Object = parentView });
                    }
                }
                foreach (var view in parentView.Views) addViewsToDestinations(view, name);
            }
        }

        public List<object> CheckedItems
        {
            get
            {
                List<object> checkedItems = new List<object>();
                foreach (var item in destinationCheckedListBox.CheckedItems) checkedItems.Add(item);
                return checkedItems;
            }
        }

        void applyFilter()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                string filter = filterTextBox.Text.ToLower();
                destinationCheckedListBox.BeginUpdate();
                List<object> checkedItems = CheckedItems;
                List<object> filteredItems = new List<object>();
                for (int i = 0; i < _destinationItems.Count; i++)
                {
                    PropertyItem item = _destinationItems[i];
                    if (i == 0 || checkedItems.Contains(item) || item.Name.ToLower().Contains(filter)) filteredItems.Add(item);
                }
                destinationCheckedListBox.Items.Clear();
                foreach (var item in filteredItems)
                {
                    destinationCheckedListBox.Items.Add(item);
                }
                for (int i = 0; i < destinationCheckedListBox.Items.Count; i++)
                {
                    destinationCheckedListBox.SetItemChecked(i, checkedItems.Contains(destinationCheckedListBox.Items[i]));
                }
            }
            finally
            {
                destinationCheckedListBox.EndUpdate();
                Cursor.Current = Cursors.Default;
            }
        }

        void deleteFromDestinationList(Report report)
        {
            string destinationName = getDestinationName();
            List<PropertyItem> destinations = new List<PropertyItem>();
            if (destinationName.StartsWith(ReportsKeyword))
            {
                _destinationItems.RemoveAll(i => i.Object == report);
            }
            else if (destinationName.StartsWith(ModelsKeyword))
            {
                _destinationItems.RemoveAll(i => i.Object != null && ((ReportModel)i.Object).Report == report);
            }
            else if (destinationName.StartsWith(ElementsKeyword))
            {
                _destinationItems.RemoveAll(i => i.Object != null && ((ReportElement)i.Object).Model.Report == report);
            }
            else if (destinationName.StartsWith(RestrictionsKeyword))
            {
                _destinationItems.RemoveAll(i => i.Object != null && ((ReportRestriction)i.Object).Model.Report == report);
            }
            else if (destinationName.StartsWith(ViewsKeyword))
            {
                _destinationItems.RemoveAll(i => i.Object != null && ((ReportView)i.Object).Report == report);
            }
            else if (destinationName.StartsWith(TasksKeyword))
            {
                _destinationItems.RemoveAll(i => i.Object != null && ((ReportTask)i.Object).Report == report);
            }
            else if (destinationName.StartsWith(TasksFolderKeyword))
            {
                _destinationItems.RemoveAll(i => i.Object == report);
            }
            else if (destinationName.StartsWith(OutputsKeyword))
            {
                _destinationItems.RemoveAll(i => i.Object != null && ((ReportOutput)i.Object).Report == report);
            }
            applyFilter();
        }

        private void cancelToolStripButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }


        private void propertiesCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index == 0 && sender is CheckedListBox)
            {
                CheckedListBox box = sender as CheckedListBox;
                bool select = (e.CurrentValue != CheckState.Checked);
                for (int i = 1; i < box.Items.Count; i++)
                {
                    box.SetItemChecked(i, select);
                }
            }
        }

        private void addReportButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = string.Format(Repository.SealRootProductName + " Reports files (*.{0})|*.{0}|All files (*.*)|*.*", Repository.SealReportFileExtension);
            dlg.Title = "Select reports to add";
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            dlg.Multiselect = true;
            if (_report != null) dlg.InitialDirectory = Path.GetDirectoryName(_report.FilePath);
            if (string.IsNullOrEmpty(dlg.InitialDirectory)) dlg.InitialDirectory = _report.Repository.ReportsFolder;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                foreach (var fileName in dlg.FileNames.Where(i => i != _report.FilePath))
                {
                    string newFileName = convertFileName(fileName);
                    if (!reportsListBox.Items.Contains(newFileName))
                    {
                        try
                        {
                            Report report = Report.LoadFromFile(fileName, _report.Repository);
                            filterTextBox_TextChanged(null, null);
                            reportsListBox.Items.Add(new PropertyItem() { Name = newFileName, Object = report });
                            buildDestinationList();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(string.Format("Error loading report {0}:\r\n{1}", fileName, ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void addRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (addRadioButton.Checked)
            {
                sourcesGroupBox.Enabled = false;
            }
            else
            {
                sourcesGroupBox.Enabled = true;
            }
            //Rebuild list
            buildDestinationList();
        }

        private void SmartCopyForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _lastReports = reportsListBox.Items.OfType<PropertyItem>().ToList();

            LastSize = Size;
            LastLocation = Location;
        }


        private void removeReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var items = reportsListBox.SelectedItems.OfType<PropertyItem>().ToList();
            foreach (var item in items.OfType<PropertyItem>().Where(i => i.Object != null))
            {
                deleteFromDestinationList((Report)item.Object);
            }

            foreach (var item in items) reportsListBox.Items.Remove(item);
        }

        private void removeReportContextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            removeReportToolStripMenuItem.Enabled = (reportsListBox.SelectedItems.Count > 0);
        }


        private void doItButton_Click(object sender, EventArgs e)
        {
            if (!addRadioButton.Checked && propertiesCheckedListBox.CheckedItems.Count == 0 && source2CheckedListBox.CheckedItems.Count == 0 && source3CheckedListBox.CheckedItems.Count == 0) throw new Exception("No source selected");
            if (destinationCheckedListBox.CheckedItems.Count == 0) throw new Exception("No destination selected");

            int reportCnt = 0, itemCnt = 0;
            List<Report> reportsToSave = new List<Report>();
            foreach (var destinationObject in destinationCheckedListBox.CheckedItems.OfType<PropertyItem>().Where(i => i.Object != null))
            {
                itemCnt++;

                //Source is Report Model
                if (_source is ReportModel)
                {
                    ReportModel modelSource = _source as ReportModel;
                    if (addRadioButton.Checked)
                    {
                        //Add models to reports
                        Report report = destinationObject.Object as Report;
                        ReportModel modelDestination = (ReportModel)Helper.Clone(modelSource);
                        report.Models.Add(modelDestination);
                        ReportSource newSource = report.Sources.FirstOrDefault(i => i.MetaSourceGUID == modelSource.Source.MetaSourceGUID);
                        if (newSource != null) modelDestination.SourceGUID = newSource.GUID;
                        report.InitReferences();
                        modelDestination.GUID = Guid.NewGuid().ToString();
                        modelDestination.Name = Helper.GetUniqueName(modelSource.Name, (from i in report.Models select i.Name).ToList());
                        foreach (var item in modelDestination.Elements) item.GUID = Guid.NewGuid().ToString();
                        foreach (var item in modelDestination.Restrictions)
                        {
                            string oldGUID = item.GUID;
                            item.GUID = Guid.NewGuid().ToString();
                            modelDestination.Restriction = modelDestination.Restriction.Replace(oldGUID, item.GUID);
                        }
                        if (!reportsToSave.Contains(report)) reportsToSave.Add(report);
                    }
                    else
                    {
                        //Update models
                        ReportModel model = destinationObject.Object as ReportModel;
                        if (!reportsToSave.Contains(model.Report)) reportsToSave.Add(model.Report);
                        foreach (var property in propertiesCheckedListBox.CheckedItems.OfType<PropertyItem>().Where(i => i.Object != null))
                        {
                            PropertyDescriptor descriptor = property.Object as PropertyDescriptor;
                            if (descriptor != null) descriptor.SetValue(model, descriptor.GetValue(_source));
                            model.InitReferences();
                        }

                        //elements
                        foreach (var elementItem in source2CheckedListBox.CheckedItems.OfType<PropertyItem>().Where(i => i.Object != null))
                        {
                            ReportElement elementSource = elementItem.Object as ReportElement;
                            //Select the best element to replace...
                            ReportElement elementBefore = model.Elements.FirstOrDefault(i => i.MetaColumnGUID == elementSource.MetaColumnGUID && i.PivotPosition == elementSource.PivotPosition && i.DisplayNameEl == elementSource.DisplayNameEl);
                            if (elementBefore == null) elementBefore = model.Elements.FirstOrDefault(i => i.MetaColumnGUID == elementSource.MetaColumnGUID && i.PivotPosition == elementSource.PivotPosition);
                            if (elementBefore == null) elementBefore = model.Elements.FirstOrDefault(i => i.MetaColumnGUID == elementSource.MetaColumnGUID);
                            int position = -1;
                            if (elementBefore != null)
                            {
                                position = model.Elements.IndexOf(elementBefore);
                                model.Elements.Remove(elementBefore);
                            }
                            ReportElement elementDestination = (ReportElement)Helper.Clone(elementSource);
                            elementDestination.GUID = (elementBefore != null ? elementBefore.GUID : Guid.NewGuid().ToString());
                            if (position != -1) model.Elements.Insert(position, elementDestination);
                            else model.Elements.Add(elementDestination);
                            model.InitReferences();
                        }

                        //restrictions
                        bool restrictionTextCopied = source3CheckedListBox.CheckedIndices.Contains(1);
                        bool aggRestrictionTextCopied = source3CheckedListBox.CheckedIndices.Contains(2);

                        //copy restriction texts...
                        if (restrictionTextCopied) model.Restriction = ((ReportModel)_source).Restriction;
                        if (aggRestrictionTextCopied) model.AggregateRestriction = ((ReportModel)_source).AggregateRestriction;

                        foreach (var restrictionItem in source3CheckedListBox.CheckedItems.OfType<PropertyItem>().Where(i => i.Object != null))
                        {
                            ReportRestriction restrictionSource = restrictionItem.Object as ReportRestriction;
                            bool isAggregate = !((ReportModel)_source).Restrictions.Contains(restrictionSource);
                            List<ReportRestriction> restrictions = (isAggregate ? model.AggregateRestrictions : model.Restrictions);

                            //Select the best restriction to replace...
                            ReportRestriction restrictionBefore = restrictions.FirstOrDefault(i => i.MetaColumnGUID == restrictionSource.MetaColumnGUID && i.PivotPosition == restrictionSource.PivotPosition && i.DisplayNameEl == restrictionSource.DisplayNameEl);
                            if (restrictionBefore == null) restrictionBefore = restrictions.FirstOrDefault(i => i.MetaColumnGUID == restrictionSource.MetaColumnGUID && i.PivotPosition == restrictionSource.PivotPosition);
                            if (restrictionBefore == null) restrictionBefore = restrictions.FirstOrDefault(i => i.MetaColumnGUID == restrictionSource.MetaColumnGUID);
                            if (restrictionBefore != null) restrictions.Remove(restrictionBefore);
                            ReportRestriction restrictionDestination = (ReportRestriction)Helper.Clone(restrictionSource);
                            restrictionDestination.GUID = (restrictionBefore != null ? restrictionBefore.GUID : Guid.NewGuid().ToString());
                            restrictions.Add(restrictionDestination);

                            string restrictionText = (isAggregate ? model.AggregateRestriction : model.Restriction);
                            if (restrictionText == null) restrictionText = "";
                            if (aggRestrictionTextCopied || restrictionTextCopied) restrictionText = restrictionText.Replace(restrictionSource.GUID, restrictionDestination.GUID);
                            else if (restrictionBefore != null) restrictionText = restrictionText.Replace(restrictionBefore.GUID, restrictionDestination.GUID);
                            if (!restrictionText.Contains(restrictionDestination.GUID))
                            {
                                if (!string.IsNullOrWhiteSpace(restrictionText)) restrictionText += "\r\nAND ";
                                restrictionText += string.Format("[{0}]", restrictionDestination.GUID);
                            }

                            if (isAggregate) model.AggregateRestriction = restrictionText;
                            else model.Restriction = restrictionText;
                            model.InitReferences();
                        }
                    }
                }

                //Source is Report Restriction
                else if (_source is ReportRestriction)
                {
                    ReportModel modelSource = _source as ReportModel;
                    if (addRadioButton.Checked)
                    {
                        //Add restriction to model
                        ReportModel model = destinationObject.Object as ReportModel;
                        bool isAggregate = !((ReportRestriction)_source).Model.Restrictions.Contains(_source);

                        if (!reportsToSave.Contains(model.Report)) reportsToSave.Add(model.Report);
                        ReportRestriction restrictionDestination = (ReportRestriction)Helper.Clone(_source);
                        restrictionDestination.GUID = Guid.NewGuid().ToString();

                        if (isAggregate) model.AggregateRestrictions.Add(restrictionDestination);
                        else model.Restrictions.Add(restrictionDestination);

                        string restrictionText = (isAggregate ? model.AggregateRestriction : model.Restriction);
                        if (!string.IsNullOrWhiteSpace(restrictionText)) restrictionText += "\r\nAND ";
                        restrictionText += string.Format("[{0}]", restrictionDestination.GUID);

                        if (isAggregate) model.AggregateRestriction = restrictionText;
                        else model.Restriction = restrictionText;

                        model.InitReferences();
                    }
                    else
                    {
                        //Update restrictions
                        ReportRestriction restriction = destinationObject.Object as ReportRestriction;
                        if (!reportsToSave.Contains(restriction.Model.Report)) reportsToSave.Add(restriction.Model.Report);
                        foreach (var property in propertiesCheckedListBox.CheckedItems.OfType<PropertyItem>().Where(i => i.Object != null))
                        {
                            PropertyDescriptor descriptor = property.Object as PropertyDescriptor;
                            if (descriptor != null)
                            {
                                //Special case for enum values
                                if (restriction.IsEnum && descriptor.Name == "EnumValue")
                                {
                                    restriction.EnumValues = ((ReportRestriction)_source).EnumValues.ToList();
                                }
                                else descriptor.SetValue(restriction, descriptor.GetValue(_source));
                            }
                        }
                    }
                }


                //Source is Report Element
                else if (_source is ReportElement)
                {
                    ReportModel modelSource = _source as ReportModel;
                    if (addRadioButton.Checked)
                    {
                        //Add elements to model
                        ReportModel model = destinationObject.Object as ReportModel;
                        if (!reportsToSave.Contains(model.Report)) reportsToSave.Add(model.Report);
                        ReportElement elementDestination = (ReportElement)Helper.Clone(_source);
                        elementDestination.GUID = Guid.NewGuid().ToString();
                        model.Elements.Add(elementDestination);
                        model.InitReferences();
                    }
                    else
                    {
                        //Update elements
                        ReportElement element = destinationObject.Object as ReportElement;
                        if (!reportsToSave.Contains(element.Model.Report)) reportsToSave.Add(element.Model.Report);
                        foreach (var property in propertiesCheckedListBox.CheckedItems.OfType<PropertyItem>().Where(i => i.Object != null))
                        {
                            PropertyDescriptor descriptor = property.Object as PropertyDescriptor;
                            if (descriptor != null) descriptor.SetValue(element, descriptor.GetValue(_source));
                        }
                    }
                }


                //Source is Report View
                else if (_source is ReportView)
                {
                    ReportView viewSource = _source as ReportView;
                    if (addRadioButton.Checked)
                    {
                        //Add views to a view
                        ReportView view = destinationObject.Object as ReportView;
                        if (!reportsToSave.Contains(view.Report)) reportsToSave.Add(view.Report);
                        ReportView viewDestination = (ReportView)Helper.Clone(_source);
                        var views = view.Views;
                        if (string.IsNullOrEmpty(view.Name))
                        {
                            //dummy view, add it to the report
                            views = view.Report.Views;
                        }
                        viewDestination.Name = Helper.GetUniqueName(viewSource.Name, (from i in views select i.Name).ToList());
                        views.Add(viewDestination);
                        view.Report.InitReferences();
                        viewDestination.GUID = Guid.NewGuid().ToString();
                        viewDestination.ReinitGUIDChildren();
                    }
                    else
                    {
                        //Update views
                        ReportView view = destinationObject.Object as ReportView;
                        if (!reportsToSave.Contains(view.Report)) reportsToSave.Add(view.Report);
                        foreach (var property in propertiesCheckedListBox.CheckedItems.OfType<PropertyItem>().Where(i => i.Object != null))
                        {
                            PropertyDescriptor descriptor = property.Object as PropertyDescriptor;
                            if (descriptor != null)
                            {
                                if (descriptor.Name == "GeneralParameters")
                                {
                                    foreach (var item in viewSource.Parameters)
                                    {
                                        var param = view.Parameters.FirstOrDefault(i => i.Name == item.Name);
                                        if (param != null) param.Value = item.Value;
                                        else view.Parameters.Add(new Parameter() { Name = item.Name, Value = item.Value });
                                    }
                                }
                                else if (descriptor.Name == "PdfConverter")
                                {
                                    view.PdfConverter = null;
                                    view.PdfConfigurations = viewSource.PdfConfigurations.ToList();
                                }
                                else if (descriptor.Name == "ExcelConverter")
                                {
                                    view.ExcelConverter = null;
                                    view.ExcelConfigurations = viewSource.ExcelConfigurations.ToList();
                                }
                                else
                                {
                                    descriptor.SetValue(view, descriptor.GetValue(_source));
                                }
                            }
                        }

                        //parameters
                        foreach (var item in source2CheckedListBox.CheckedItems.OfType<PropertyItem>().Where(i => i.Object != null))
                        {
                            Parameter sourceParameter = item.Object as Parameter;
                            Parameter parameter = view.Parameters.FirstOrDefault(i => i.Name == sourceParameter.Name);
                            if (parameter != null) parameter.Value = sourceParameter.Value;
                            else view.Parameters.Add(new Parameter() { Name = sourceParameter.Name, Value = sourceParameter.Value });
                        }
                    }
                }

                //Source is Report Task
                else if (_source is ReportTask)
                {
                    ReportTask taskSource = _source as ReportTask;
                    if (addRadioButton.Checked)
                    {
                        //Add tasks to report
                        Report report = destinationObject.Object as Report;
                        if (!reportsToSave.Contains(report)) reportsToSave.Add(report);
                        ReportTask taskDestination = (ReportTask)Helper.Clone(_source);
                        taskDestination.GUID = Guid.NewGuid().ToString();
                        taskDestination.Name = Helper.GetUniqueName(taskSource.Name, (from i in report.Tasks select i.Name).ToList());
                        report.Tasks.Add(taskDestination);
                        report.InitReferences();
                    }
                    else
                    {
                        //Update tasks
                        ReportTask task = destinationObject.Object as ReportTask;
                        if (!reportsToSave.Contains(task.Report)) reportsToSave.Add(task.Report);
                        foreach (var property in propertiesCheckedListBox.CheckedItems.OfType<PropertyItem>().Where(i => i.Object != null))
                        {
                            PropertyDescriptor descriptor = property.Object as PropertyDescriptor;
                            if (descriptor != null)
                            {
                                descriptor.SetValue(task, descriptor.GetValue(_source));
                            }
                        }
                    }
                }

                //Source is Report Output
                else if (_source is ReportOutput)
                {
                    ReportOutput outputSource = _source as ReportOutput;
                    if (addRadioButton.Checked)
                    {
                        //Add outputs to report
                        Report report = destinationObject.Object as Report;
                        if (!reportsToSave.Contains(report)) reportsToSave.Add(report);
                        ReportOutput outputDestination = (ReportOutput)Helper.Clone(_source);
                        outputDestination.GUID = Guid.NewGuid().ToString();
                        outputDestination.Name = Helper.GetUniqueName(outputSource.Name, (from i in report.Outputs select i.Name).ToList());
                        report.Outputs.Add(outputDestination);
                        //assign view to the report, by name ??
                        var newView = report.Views.FirstOrDefault(i => i.Name == outputSource.View.Name);
                        if (newView == null) newView = report.Views.FirstOrDefault();
                        if (newView != null) outputDestination.ViewGUID = newView.GUID;
                        report.InitReferences();
                    }
                    else
                    {
                        //Update outputs
                        ReportOutput output = destinationObject.Object as ReportOutput;
                        if (!reportsToSave.Contains(output.Report)) reportsToSave.Add(output.Report);
                        foreach (var property in propertiesCheckedListBox.CheckedItems.OfType<PropertyItem>().Where(i => i.Object != null))
                        {
                            PropertyDescriptor descriptor = property.Object as PropertyDescriptor;
                            if (descriptor != null)
                            {
                                if (descriptor.Name == "Restrictions")
                                {
                                    output.Restrictions = (List<ReportRestriction>)Helper.Clone(outputSource.Restrictions);
                                }
                                else
                                {
                                    descriptor.SetValue(output, descriptor.GetValue(_source));
                                }
                            }
                        }
                    }
                }

                //Source is Report TasksFolder
                else if (_source is TasksFolder)
                {
                    //Update report tasksScript
                    Report reportDestination = destinationObject.Object as Report;
                    if (!reportsToSave.Contains(reportDestination)) reportsToSave.Add(reportDestination);
                    reportDestination.TasksScript = _report.TasksScript;
                }
            }

            foreach (var report in reportsToSave)
            {
                if (report == _report) IsReportModified = true;
                else
                {
                    try
                    {
                        report.SaveToFile();
                        reportCnt++;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("Error saving report {0}:\r\n{1}", report.FilePath, ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            DialogResult = DialogResult.OK;
            Close();

            string message = string.Format("{0} item(s) modified.", itemCnt);
            if (reportCnt > 0) message += string.Format("\r\n{0} report(s) modified and saved.", reportCnt);
            MessageBox.Show(message, "Smart Copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void filterTextBox_TextChanged(object sender, EventArgs e)
        {
            applyFilter();
        }
    }
}
