//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
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
using Seal.Controls;
using Seal.Helpers;
using Seal.Forms;
using System.Diagnostics;
using System.Collections;
using Microsoft.Win32.TaskScheduler;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Seal
{
    public partial class ReportDesigner : Form, IEntityHandler
    {

        #region Members
        public static ReportDesigner Instance { get; private set; }

        TreeViewEditorHelper treeViewHelper;
        ToolStripEditorHelper toolStripHelper;
        ToolsHelper toolsHelper;

        Report _report = null;
        public Report Report
        {
            get { return _report; }
        }

        bool _canRender = false;
        public void CannotRenderAnymore()
        {
            _canRender = false;
        }

        bool _isModified = false;
        public bool IsModified
        {
            get { return _isModified; }
            set
            {
                _isModified = value;
                //Modification of a meta item, a model...means that we can not render anymore
                if (selectedEntity is ReportSource || selectedEntity is MetaColumn || selectedEntity is MetaConnection || selectedEntity is MetaJoin || selectedEntity is MetaTable || selectedEntity is MetaEnum) _canRender = false;
                enableControls();
            }
        }

        ModelPanel modelPanel = new ModelPanel();
        Repository _repository;
        ReportViewerForm _reportViewer = null;
        ToolStripMenuItem nextWidgetViewMenuItem = new ToolStripMenuItem() { Text = "Go to next Widget View", ToolTipText = "Select the next Widget view in the report", AutoToolTip = true, ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W))), ShowShortcutKeys = true };
        ToolStripMenuItem nextModelViewMenuItem = new ToolStripMenuItem() { Text = "Go to next Model View", ToolTipText = "Select the next Model view in the report", AutoToolTip = true, ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G))), ShowShortcutKeys = true };

        public ReportDesigner()
        {
            Instance = this;
            if (Properties.Settings.Default.CallUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.CallUpgrade = false;
                Properties.Settings.Default.Save();
            }

            InitializeComponent();
            mainPropertyGrid.PropertySort = PropertySort.Categorized;
            mainPropertyGrid.LineColor = SystemColors.ControlLight;
            PropertyGridHelper.AddResetMenu(mainPropertyGrid);

            treeViewHelper = new TreeViewEditorHelper()
            {
                entityHandler = this,
                Report = _report,
                resetDisplayOrderToolStripMenuItem = resetDisplayOrderToolStripMenuItem,
                sortColumnAlphaOrderToolStripMenuItem = sortColumnAlphaOrderToolStripMenuItem,
                sortColumnSQLOrderToolStripMenuItem = sortColumnSQLOrderToolStripMenuItem,
                addFromToolStripMenuItem = addFromToolStripMenuItem,
                addToolStripMenuItem = addToolStripMenuItem,
                removeToolStripMenuItem = removeToolStripMenuItem,
                copyToolStripMenuItem = copyToolStripMenuItem,
                removeRootToolStripMenuItem = removeRootToolStripMenuItem,
                treeContextMenuStrip = treeContextMenuStrip,
                mainTreeView = mainTreeView,
                ForReport = true
            };
            mainTreeView.AfterSelect += treeViewHelper.AfterSelect;

            toolStripHelper = new ToolStripEditorHelper() { MainToolStrip = mainToolStrip, MainPropertyGrid = mainPropertyGrid, EntityHandler = this, MainTreeView = mainTreeView };
            toolsHelper = new ToolsHelper() { EntityHandler = this };
            toolsHelper.InitHelpers(toolsToolStripMenuItem, true);

            toolsToolStripMenuItem.DropDownItems.Insert(4, new ToolStripSeparator());
            toolsToolStripMenuItem.DropDownItems.Insert(5, nextModelViewMenuItem);
            nextModelViewMenuItem.Click += nextView_Click;
            toolsToolStripMenuItem.DropDownItems.Insert(5, nextWidgetViewMenuItem);
            nextWidgetViewMenuItem.Click += nextView_Click;

            HelperEditor.HandlerInterface = this;

            mainSplitContainer.Panel2.Controls.Add(modelPanel);
            modelPanel.Dock = DockStyle.Fill;

            ShowIcon = true;
            Icon = Properties.Resources.reportDesigner;
        }

        private void ReportDesigner_Load(object sender, EventArgs e)
        {
            KeyPreview = true;

            //Set event handler for sub-property grids...
            EntityCollectionEditor.MyPropertyValueChanged += mainPropertyGrid_PropertyValueChanged;

            //handle program args
            string[] args = Environment.GetCommandLineArgs();
            bool open = (args.Length >= 2 && args[1].ToLower() == "/o");
            string reportToOpen = null;
            if (args.Length >= 3 && File.Exists(args[2])) reportToOpen = args[2];

            //MRU = most recent used reports
            if (Properties.Settings.Default.MRU == null) Properties.Settings.Default.MRU = new System.Collections.Specialized.StringCollection();
            if (!open && Properties.Settings.Default.MRU.Count > 0 && File.Exists(Properties.Settings.Default.MRU[0]))
            {
                open = true;
                reportToOpen = Properties.Settings.Default.MRU[0];
            }

            showScriptErrorsToolStripMenuItem.Checked = Properties.Settings.Default.ShowScriptErrors;
            if (!Helper.IsMachineAdministrator()) Properties.Settings.Default.SchedulesWithCurrentUser = true;
            schedulesWithCurrentUserToolStripMenuItem.Checked = Properties.Settings.Default.SchedulesWithCurrentUser;

            if (open)
            {
                if (!string.IsNullOrEmpty(reportToOpen)) openReport(reportToOpen);
                else newToolStripMenuItem_Click(null, null);
            }
            else
            {
                _repository = Repository.Create();
                IsModified = false;
                init();
            }
            if (_repository == null)
            {
                _repository = new Repository();
                MessageBox.Show("No repository has been defined or found for this installation. Reports will not be rendered. Please modify the .config file to set a RepositoryPath containing at least a Views subfolder", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            InstallHelper.InstallConverter(helpToolStripMenuItem, Repository.Instance.AssembliesFolder);
        }

        //EntityHandlerInterface
        public void SetModified()
        {
            IsModified = true;
        }

        public void InitEntity(object entity)
        {
            init(entity);
        }

        public void RefreshModelTreeView()
        {
            modelPanel.ReinitSource();
        }

        #endregion

        #region Helpers

        void initTreeNodeViews(TreeNode node, ReportView view)
        {
            if (view != null)
            {
                int index = 1;
                foreach (var childView in view.Views.OrderBy(i => i.SortOrder))
                {
                    childView.SortOrder = index++;
                    var imageIndex = childView.Enabled ? 8 : 20;
                    if (childView.TemplateName == "Model" || childView.TemplateName == "Model Detail") imageIndex = childView.Enabled ? 18 : 21;
                    else if (childView.TemplateName == "Widget") imageIndex = childView.Enabled ? 19 : 22;
                    TreeNode childNode = new TreeNode(childView.Name) { ImageIndex = imageIndex, SelectedImageIndex = imageIndex };
                    childNode.Tag = childView;
                    node.Nodes.Add(childNode);
                    initTreeNodeViews(childNode, childView);
                }
            }
        }

        void initTreeNodeTasks(TreeNode node, ReportTask task)
        {
            if (task != null)
            {
                int index = 1;
                foreach (var childTask in task.Tasks.OrderBy(i => i.SortOrder))
                {
                    childTask.SortOrder = index++;
                    var imageIndex = childTask.Enabled ? 12 : 14;
                    TreeNode childNode = new TreeNode(childTask.Name) { ImageIndex = imageIndex, SelectedImageIndex = imageIndex };
                    childNode.Tag = childTask;
                    node.Nodes.Add(childNode);
                    initTreeNodeTasks(childNode, childTask);
                }
                node.Expand();
            }
        }

        void updateTreeNodeViewNames(TreeNode node)
        {
            var view = node.Tag as ReportView;
            if (view != null) node.Text = view.Name;
            foreach (TreeNode subNode in node.Nodes) updateTreeNodeViewNames(subNode);
        }

        private TreeNode _reportTN;
        private TreeNode _sourceTN;
        private TreeNode _viewTN;
        private TreeNode _tasksTN;
        private TreeNode _outputsTN;
        private TreeNode _schedulesTN;

        void init(object entityToSelect = null)
        {
            try
            {
                mainTreeView.BeginUpdate();
                if (entityToSelect == null && mainTreeView.SelectedNode != null) entityToSelect = mainTreeView.SelectedNode.Tag;

                treeViewHelper.Report = _report;
                if (_report == null)
                {
                    mainSplitContainer.Visible = false;
                }
                else
                {
                    mainSplitContainer.Visible = true;
                    mainTreeView.Nodes.Clear();


                    _sourceTN = new TreeNode("Sources") { Tag = SourceFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
                    mainTreeView.Nodes.Add(_sourceTN);
                    foreach (var source in _report.Sources)
                    {
                        treeViewHelper.addSource(_sourceTN.Nodes, source, 13);
                    }
                    _sourceTN.Expand();

                    TreeNode modelTN = new TreeNode("Models") { Tag = ModelFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
                    mainTreeView.Nodes.Add(modelTN);
                    foreach (var model in _report.Models)
                    {
                        var index = model.IsLINQ ? 17 : (model.IsSQLModel ? 15 : 10);
                        TreeNode tn = new TreeNode(model.Name) { Tag = model, ImageIndex = index, SelectedImageIndex = index };
                        tn.Tag = model;
                        modelTN.Nodes.Add(tn);
                        UpdateModelNode(tn);
                    }
                    modelTN.Expand();

                    _viewTN = new TreeNode("Views") { Tag = ViewFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
                    mainTreeView.Nodes.Add(_viewTN);
                    foreach (ReportView view in _report.Views)
                    {
                        var imageIndex = view.Enabled ? 8 : 20;
                        if (view.TemplateName == "Model") imageIndex = view.Enabled ? 18 : 21;
                        else if (view.TemplateName == "Widget") imageIndex = view.Enabled ? 19 : 22;
                        TreeNode reportViewTN = new TreeNode(view.Name) { ImageIndex = imageIndex, SelectedImageIndex = imageIndex };
                        reportViewTN.Tag = view;
                        _viewTN.Nodes.Add(reportViewTN);
                        initTreeNodeViews(reportViewTN, view);
                    }
                    _viewTN.ExpandAll();

                    _tasksTN = new TreeNode("Tasks") { Tag = TasksFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
                    mainTreeView.Nodes.Add(_tasksTN);
                    foreach (var task in _report.Tasks)
                    {
                        var imageIndex = task.Enabled ? 12 : 14;
                        TreeNode taskTN = new TreeNode(task.Name) { Tag = task, ImageIndex = imageIndex, SelectedImageIndex = imageIndex };
                        _tasksTN.Nodes.Add(taskTN);
                        initTreeNodeTasks(taskTN, task);
                    }
                    _tasksTN.Expand();

                    _outputsTN = new TreeNode("Outputs") { Tag = OutputFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
                    mainTreeView.Nodes.Add(_outputsTN);
                    foreach (var output in _report.Outputs)
                    {
                        TreeNode outputTN = new TreeNode(output.Name) { Tag = output, ImageIndex = 9, SelectedImageIndex = 9 };
                        _outputsTN.Nodes.Add(outputTN);
                    }
                    _outputsTN.Expand();

                    _schedulesTN = new TreeNode("Schedules") { Tag = ScheduleFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
                    mainTreeView.Nodes.Add(_schedulesTN);
                    foreach (var schedule in _report.Schedules)
                    {
                        TreeNode scheduleTN = new TreeNode(schedule.Name) { Tag = schedule, ImageIndex = 11, SelectedImageIndex = 11 };
                        _schedulesTN.Nodes.Add(scheduleTN);
                    }
                    _schedulesTN.Expand();

                    _reportTN = new TreeNode("General") { Tag = Report, ImageIndex = 16, SelectedImageIndex = 16 };
                    mainTreeView.Nodes.Add(_reportTN);

                    if (mainTreeView.SelectedNode == null)
                    {
                        mainTreeView.SelectedNode = _sourceTN;
                    }
                }
                if (entityToSelect != null) selectNode(entityToSelect);

                enableControls();
                buildMRUMenus();
            }
            finally
            {
                mainTreeView.EndUpdate();
            }
        }

        void enableControls()
        {
            Text = Repository.SealRootProductName + " Report Designer";
            if (_report != null)
            {
                if (_report.SchedulesModified) _isModified = true;
                Text = Path.GetFileNameWithoutExtension(_report.FilePath) + (IsModified ? "*" : "") + " - " + Text;
            }

            saveToolStripMenuItem.Enabled = (_report != null);
            saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled;
            saveAsToolStripMenuItem.Enabled = (_report != null);
            reloadToolStripMenuItem.Enabled = (_report != null && !string.IsNullOrEmpty(Path.GetDirectoryName(_report.FilePath)));
            MRUToolStripMenuItem.Enabled = (MRUToolStripMenuItem.DropDownItems.Count > 0);
            closeToolStripMenuItem.Enabled = (_report != null);
            executeToolStripMenuItem.Enabled = (_report != null && (_reportViewer == null || (_reportViewer != null && _reportViewer.CanExecute)));
            executeToolStripButton.Enabled = executeToolStripMenuItem.Enabled;
            renderToolStripMenuItem.Enabled = (_canRender && _report != null && _reportViewer != null && _reportViewer.Visible && _reportViewer.CanRender);
            renderToolStripButton.Enabled = renderToolStripMenuItem.Enabled;
            nextModelViewMenuItem.Enabled = (_report != null);

            bool showViewOutput = (selectedEntity is ReportView || selectedEntity is ReportOutput);
            executeViewOutputToolStripMenuItem.Visible = showViewOutput;
            renderViewOutputToolStripMenuItem.Visible = showViewOutput;
            executeViewOutputToolStripButton.Visible = showViewOutput;
            renderViewOutputToolStripButton.Visible = showViewOutput;

            executeViewOutputToolStripMenuItem.Text = (selectedEntity is ReportView ? "Execute View" : "Execute Output");
            renderViewOutputToolStripMenuItem.Text = (selectedEntity is ReportView ? "Render View" : "Render Output");

            executeViewOutputToolStripButton.Text = executeViewOutputToolStripMenuItem.Text;
            executeViewOutputToolStripButton.ToolTipText = (selectedEntity is ReportView ? "F10 Execute the report using the selected view" : "F10 Execute the selected report output"); ;
            renderViewOutputToolStripButton.Text = renderViewOutputToolStripMenuItem.Text;
            renderViewOutputToolStripButton.ToolTipText = (selectedEntity is ReportView ? "F11 Execute the report using the models of the previous execution and the selected view" : "F11 Execute the selected report output using the models of the previous execution");

            executeViewOutputToolStripMenuItem.Enabled = executeToolStripMenuItem.Enabled;
            renderViewOutputToolStripMenuItem.Enabled = renderToolStripMenuItem.Enabled;
            executeViewOutputToolStripButton.Enabled = executeToolStripMenuItem.Enabled;
            renderViewOutputToolStripButton.Enabled = renderToolStripMenuItem.Enabled;

            toolsHelper.EnableControls();
        }

        bool checkModified()
        {
            bool result = true;
            if (_report != null && IsModified)
            {
                DialogResult dlgResult = MessageBox.Show("The report has been modified, do you want to save it ?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (dlgResult == DialogResult.Cancel) result = false;
                else if (dlgResult == DialogResult.Yes) saveToolStripMenuItem_Click(null, null);
            }
            return result;
        }

        bool checkRunning()
        {
            bool result = true;
            if (_report != null && _reportViewer != null && !_reportViewer.CanExecute)
            {
                DialogResult dlgResult = MessageBox.Show("The report is being executed, do you want to continue ?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dlgResult == DialogResult.No) result = false;
            }
            return result;
        }

        object selectedEntity
        {
            get
            {
                object result = null;
                if (mainTreeView.SelectedNode != null) result = mainTreeView.SelectedNode.Tag;
                return result;
            }
        }

        bool isChildNodeSelected
        {
            get
            {
                return (mainTreeView.SelectedNode != null && mainTreeView.SelectedNode.Parent != null);
            }
        }

        public void selectNode(object entity)
        {
            TreeViewHelper.SelectNode(mainTreeView, mainTreeView.Nodes, entity);
        }

        public void RefreshNode()
        {
            var currentNode = mainTreeView.SelectedNode;
            mainTreeView.SelectedNode = null;
            mainTreeView.SelectedNode = currentNode;
        }

        public void UpdateModelNode(TreeNode currentNode = null)
        {
            if (currentNode == null) currentNode = mainTreeView.SelectedNode;
            if (currentNode == null) return;

            var model = currentNode.Tag as ReportModel;
            if (model == null) return;

            var index = model.IsLINQ ? 17 : (model.IsSQLModel ? 15 : 10);
            currentNode.ImageIndex = index;
            currentNode.SelectedImageIndex = index;

            if (model != null && model.IsLINQ)
            {
                foreach (var subModel in model.LINQSubModels)
                {
                    TreeNode node = null;
                    foreach (TreeNode subNode in currentNode.Nodes)
                    {
                        if (subNode.Tag == subModel)
                        {
                            node = subNode;
                            break;
                        }
                    }

                    if (node != null && node.Name != subModel.Name)
                    {
                        currentNode.Nodes.Remove(node);
                        node = null;
                    }

                    if (node == null)
                    {
                        node = new TreeNode(subModel.Name) { Tag = subModel, ImageIndex = 10, SelectedImageIndex = 10 };
                        currentNode.Nodes.Add(node);
                    }
                }


                foreach (var subTable in model.LINQSubTables)
                {
                    TreeNode node = null;
                    foreach (TreeNode subNode in currentNode.Nodes)
                    {
                        if (subNode.Tag == subTable)
                        {
                            node = subNode;
                            break;
                        }
                    }

                    if (node != null && node.Name != subTable.Name)
                    {
                        currentNode.Nodes.Remove(node);
                        node = null;
                    }

                    if (node == null)
                    {
                        node = new TreeNode(subTable.Name) { Tag = subTable, ImageIndex = 4, SelectedImageIndex = 4 };
                        currentNode.Nodes.Add(node);
                    }
                }

                //remove unused nodes
                int j = currentNode.Nodes.Count;
                while (--j >= 0)
                {
                    if (!model.LINQSubModels.Exists(i => i == currentNode.Nodes[j].Tag) && !model.LINQSubTables.Exists(i => i == currentNode.Nodes[j].Tag)) currentNode.Nodes.RemoveAt(j);
                }

                currentNode.Expand();
            }
            else if (model != null && !model.IsLINQ)
            {
                currentNode.Nodes.Clear();
            }

            toolStripHelper.SetHelperButtons(model);
        }

        #endregion

        #region Main Form Handlers

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ReportDesigner_FormClosing(object sender, FormClosingEventArgs e)
        {
#if DEBUG
            if (_repository != null) _repository.FlushTranslationUsage();
#endif
            Properties.Settings.Default.Save();
            if (!checkModified()) e.Cancel = true;
            if (!checkRunning()) e.Cancel = true;
        }

        private void buildMRUMenus()
        {
            //Check and clean up MRUs
            int i = Properties.Settings.Default.MRU.Count;
            while (--i >= 0)
            {
                if (i >= 10 || !File.Exists(Properties.Settings.Default.MRU[i])) Properties.Settings.Default.MRU.RemoveAt(i);
            }

            MRUToolStripMenuItem.DropDownItems.Clear();
            foreach (var mru in Properties.Settings.Default.MRU)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(mru);
                item.Click += new EventHandler(delegate (object sender, EventArgs e)
                {
                    if (!checkModified()) return;
                    openReport(((ToolStripMenuItem)sender).Text);
                });
                MRUToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        private void addMRU(string fileName)
        {
            Properties.Settings.Default.MRU.Remove(fileName);
            Properties.Settings.Default.MRU.Insert(0, fileName);
            Properties.Settings.Default.Save();
            buildMRUMenus();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!checkModified()) return;
            if (!checkRunning()) return;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = string.Format(Repository.SealRootProductName + " Reports files (*.{0})|*.{0}|All files (*.*)|*.*", Repository.SealReportFileExtension);
            dlg.Title = "Open a report";
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            if (_report != null) dlg.InitialDirectory = Path.GetDirectoryName(_report.FilePath);
            if (string.IsNullOrEmpty(dlg.InitialDirectory)) dlg.InitialDirectory = _repository.ReportsFolder;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (_reportViewer != null && _reportViewer.Visible) _reportViewer.Close();
                openReport(dlg.FileName);
            }
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_report != null)
            {
                if (IsModified)
                {
                    DialogResult dlgResult = MessageBox.Show("The report has been modified, are you sure you to reload it ?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                    if (dlgResult == DialogResult.Cancel) return;
                }
                if (_reportViewer != null && _reportViewer.Visible) _reportViewer.Close();

                string path = _report.FilePath;
                _report = null;
                IsModified = false;
                closeToolStripMenuItem_Click(sender, e);
                if (File.Exists(path)) openReport(path);
            }
        }

        private void selectAfterLoad()
        {
            if (_report.Models.Count > 0 && _report.Models.OrderBy(i => i.Name).First().Elements.Count > 0) selectNode(_report.Models.OrderBy(i => i.Name).First());
            else if (_report.Tasks.Count > 0) selectNode(_report.Tasks.OrderBy(i => i.SortOrder).First());
            else if (_report.Models.Count > 0) selectNode(_report.Models[0]);
            else if (_report.Sources.Count > 0) selectNode(_report.Sources[0]);
            else if (_report.Views.Count > 0) selectNode(_report.Views[0]);
        }

        private void openReport(string path)
        {
            //refresh repository
            _repository = Repository.Create();
            _report = Report.LoadFromFile(path, _repository, true, true);
            if (_report != null)
            {
                addMRU(path);
                IsModified = false;
                init();
                selectAfterLoad();

                if (!string.IsNullOrEmpty(_report.LoadErrors))
                {
                    MessageBox.Show(string.Format("Error loading the report:\r\n{0}", _report.LoadErrors), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                if (!string.IsNullOrEmpty(_report.UpgradeWarnings))
                {
                    MessageBox.Show(_report.UpgradeWarnings, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            if (_reportViewer != null && _reportViewer.Visible) _reportViewer.Close();
            toolsHelper.Report = _report;
            TemplateTextEditor.CurrentEntity = _report;
            _report.SchedulesWithCurrentUser = Properties.Settings.Default.SchedulesWithCurrentUser;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                if (!checkModified()) return;
                if (!checkRunning()) return;

                if (_reportViewer != null && _reportViewer.Visible) _reportViewer.Close();

                if (_repository == null || _repository.MustReload()) _repository = Repository.Create();
                _report = Report.Create(_repository);
                IsModified = true;
                mainTreeView.SelectedNode = null;
                init();
                selectAfterLoad();

                toolsHelper.Report = _report;
                TemplateTextEditor.CurrentEntity = _report;
                _report.SchedulesWithCurrentUser = Properties.Settings.Default.SchedulesWithCurrentUser;

                if (!string.IsNullOrEmpty(_report.ExecutionErrors)) MessageBox.Show(_report.ExecutionErrors, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_report != null)
            {
                if (sender == saveAsToolStripMenuItem || string.IsNullOrEmpty(Path.GetDirectoryName(_report.FilePath)))
                {
                    SaveFileDialog dlg = new SaveFileDialog();
                    dlg.Filter = string.Format(Repository.SealRootProductName + " Reports files (*.{0})|*.{0}|All files (*.*)|*.*", Repository.SealReportFileExtension);
                    if (_report != null) dlg.InitialDirectory = Path.GetDirectoryName(_report.FilePath);
                    if (string.IsNullOrEmpty(dlg.InitialDirectory)) dlg.InitialDirectory = _repository.ReportsFolder;
                    dlg.FileName = Path.GetFileName(_report.FilePath);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        if (_reportViewer != null && _reportViewer.Visible) _reportViewer.Close();
                        if (sender == saveAsToolStripMenuItem)
                        {
                            //Save as -> new GUID and no schedule copy...
                            _report.InitGUIDAndSchedules();
                        }
                        _report.FilePath = dlg.FileName;
                        init();
                    }
                    else return;
                    _report.LastModification = DateTime.MinValue;
                }
                //commit panels
                if (selectedEntity is ReportModel) modelPanel.Commit();

                _report.SaveToFile();
                addMRU(_report.FilePath);
                IsModified = false;
                enableControls();
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!checkModified()) return;
            if (!checkRunning()) return;

            if (_reportViewer != null && _reportViewer.Visible) _reportViewer.Close();
            _report = null;
            toolStripHelper.SetHelperButtons(null);
            IsModified = false;
            init();
            toolsHelper.Report = _report;
            TemplateTextEditor.CurrentEntity = _report;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBoxForm frm = new AboutBoxForm();
            frm.ShowDialog(this);
        }

        private void showScriptErrorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showScriptErrorsToolStripMenuItem.Checked = !showScriptErrorsToolStripMenuItem.Checked;
            Properties.Settings.Default.ShowScriptErrors = showScriptErrorsToolStripMenuItem.Checked;
            Properties.Settings.Default.Save();
        }

        private void schedulesWithCurrentUserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            schedulesWithCurrentUserToolStripMenuItem.Checked = !schedulesWithCurrentUserToolStripMenuItem.Checked;
            Properties.Settings.Default.SchedulesWithCurrentUser = schedulesWithCurrentUserToolStripMenuItem.Checked;
            if (_report != null) _report.SchedulesWithCurrentUser = Properties.Settings.Default.SchedulesWithCurrentUser;
            Properties.Settings.Default.Save();
        }

        #endregion

        #region Tree View Handlers

        private void mainTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right) mainTreeView.SelectedNode = e.Node;
        }


        private void mainTreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Clicks == 1 && e.Button == MouseButtons.Right)
            {
                // Select the clicked node
                var newNode = mainTreeView.GetNodeAt(e.X, e.Y);
                if (newNode != mainTreeView.SelectedNode) mainTreeView.SelectedNode = newNode;

                if (mainTreeView.SelectedNode != null)
                {
                    treeContextMenuStrip.Show(mainTreeView, e.Location);
                }
            }
        }

        bool _adminWarningDone = false;
        private void mainTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (selectedEntity != null && isChildNodeSelected)
            {
                object newEntity = e.Node.Tag;
                if (!_report.SchedulesWithCurrentUser && !_adminWarningDone && (newEntity is ReportSchedule || newEntity is ScheduleFolder) && !Helper.IsMachineAdministrator())
                {
                    MessageBox.Show("We recommend to execute the 'Report Designer' application with the option 'Run as administrator' to edit the Schedules (part of the Windows Tasks Scheduler)...", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _adminWarningDone = true;
                    e.Cancel = true;
                    return;
                }

                if ((newEntity is ReportSchedule || newEntity is ScheduleFolder) && !Helper.CheckTaskSchedulerOS())
                {
                    e.Cancel = true;
                    return;
                }

                //commit panels
                if (selectedEntity is ReportModel)
                {
                    modelPanel.Commit();
                }
            }
        }


        bool _pdfExpanded = false, _excelExpanded = false, _configurationExpanded = true;
        private void mainTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_lastDragOverNode != null) return;

            modelPanel.Visible = false;
            mainPropertyGrid.Visible = false;

            //refresh source
            foreach (var source in _report.Sources)
            {
                source.Refresh();
            }

            var entry = Helper.GetGridEntry(mainPropertyGrid, "pdf configuration");
            if (entry != null) _pdfExpanded = entry.Expanded;
            entry = Helper.GetGridEntry(mainPropertyGrid, "excel configuration");
            if (entry != null) _excelExpanded = entry.Expanded;
            entry = Helper.GetGridEntry(mainPropertyGrid, "template configuration");
            if (entry != null) _configurationExpanded = entry.Expanded;

            mainPropertyGrid.SelectedObject = null;
            if (selectedEntity is ReportModel)
            {
                modelPanel.Visible = true;
                modelPanel.Model = (ReportModel)selectedEntity;
                modelPanel.Init(this);
            }
            else if (selectedEntity is Report)
            {
                Report entity = (Report)selectedEntity;
                mainPropertyGrid.Visible = true;
                entity.InitEditor();
                mainPropertyGrid.SelectedObject = selectedEntity;
            }
            else if (selectedEntity is RootComponent)
            {
                RootComponent entity = (RootComponent)selectedEntity;
                mainPropertyGrid.Visible = true;
                entity.InitEditor();
                mainPropertyGrid.SelectedObject = selectedEntity;
                //Do not allow edition of repository objects
                if (selectedEntity is MetaConnection && !((MetaConnection)selectedEntity).IsEditable) entity.SetReadOnly();
                if (selectedEntity is MetaTable && !((MetaTable)selectedEntity).IsEditable) entity.SetReadOnly();
                if (selectedEntity is MetaJoin && !((MetaJoin)selectedEntity).IsEditable) entity.SetReadOnly();
                if (selectedEntity is MetaColumn && !((MetaColumn)selectedEntity).MetaTable.IsEditable) entity.SetReadOnly();
                if (selectedEntity is MetaEnum && !((MetaEnum)selectedEntity).IsEditable) entity.SetReadOnly();
            }
            //Set default expanded
            entry = Helper.GetGridEntry(mainPropertyGrid, "pdf configuration");
            if (entry != null) entry.Expanded = _pdfExpanded;
            entry = Helper.GetGridEntry(mainPropertyGrid, "excel configuration");
            if (entry != null) entry.Expanded = _excelExpanded;
            entry = Helper.GetGridEntry(mainPropertyGrid, "template configuration");
            if (entry != null) entry.Expanded = _configurationExpanded;
            entry = Helper.GetGridEntry(mainPropertyGrid, "custom partial template texts");
            if (entry != null) entry.Expanded = true;
            entry = Helper.GetGridEntry(mainPropertyGrid, "schedule definition");
            if (entry != null) entry.Expanded = true;
            entry = Helper.GetGridEntry(mainPropertyGrid, "table parameters");
            if (entry != null) entry.Expanded = true;
            entry = Helper.GetGridEntry(mainPropertyGrid, "task parameters");
            if (entry != null) entry.Expanded = true;

            toolStripHelper.SetHelperButtons(selectedEntity);
            //init shortcuts
            initTreeContextMenuStrip();

            enableControls();
        }

        private void mainTreeView_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            var selected = mainTreeView.SelectedNode;
            e.CancelEdit = true;

            //Disable shortcuts
            foreach (var ts in treeContextMenuStrip.Items)
            {
                var ts2 = ts as ToolStripMenuItem;
                if (ts2 != null)
                {
                    if (ts2.ShortcutKeys == (Keys.Control | Keys.C)) ts2.ShortcutKeys = Keys.None;
                    if (ts2.ShortcutKeys == Keys.Delete) ts2.ShortcutKeys = Keys.None;
                }
            }

            if (selected == null) return;
            object entity = selected.Tag;

            if (entity is ReportView)
            {
                var view = entity as ReportView;
                if (view.Template.ForReportModel && view.UseModelName) return;
            }

            if (entity is MetaSource || entity is ReportModel || entity is ReportTask || entity is ReportView || entity is ReportOutput || entity is ReportSchedule)
            {
                e.CancelEdit = false;
            }
        }

        private void mainTreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            object entity = mainTreeView.SelectedNode.Tag;
            var selected = e.Node;
            if (!string.IsNullOrEmpty(e.Label))
            {
                e.CancelEdit = true;
                if (entity is MetaSource)
                {
                    e.Node.Text = Helper.GetUniqueName(e.Label, (from i in Report.Sources select i.Name).ToList());
                }
                else if (entity is ReportModel)
                {
                    e.Node.Text = Helper.GetUniqueName(e.Label, (from i in Report.Models select i.Name).ToList());
                }
                else if (entity is ReportView)
                {
                    e.Node.Text = e.Label;
                }
                else if (entity is ReportTask)
                {
                    var task = entity as ReportTask;
                    var tasks = task.ParentTask == null ? Report.Tasks : task.ParentTask.Tasks;
                    e.Node.Text = Helper.GetUniqueName(e.Label, (from i in tasks select i.Name).ToList());
                }
                else if (entity is ReportOutput)
                {
                    e.Node.Text = Helper.GetUniqueName(e.Label, (from i in Report.Outputs select i.Name).ToList());
                }
                else if (entity is ReportSchedule)
                {
                    e.Node.Text = Helper.GetUniqueName(e.Label, (from i in Report.Schedules select i.Name).ToList());
                    Report.SchedulesModified = true;
                }

                if (entity is RootComponent)
                {
                    ((RootComponent)entity).Name = e.Node.Text;
                    if (!(entity is ReportView || entity is ReportTask))
                    {
                        mainTreeView.SelectedNode = selected.Parent;
                        //Resort children to avoid full resort
                        List<TreeNode> l = new List<TreeNode>();
                        var parent = e.Node.Parent;
                        foreach (var node in parent.Nodes) l.Add((TreeNode)node);
                        l.Sort(new NodeSorter().Compare);
                        parent.Nodes.Clear();
                        parent.Nodes.AddRange(l.ToArray());
                    }
                    if (entity is ReportSchedule) ((ReportSchedule)entity).SynchronizeTask();
                    if (entity is ReportModel) updateTreeNodeViewNames(_viewTN);

                    SetModified();
                }
            }

            //Enable shortcuts
            //        copyToolStripMenuItem.ShortcutKeys = (Keys.Control | Keys.C);
            //      removeRootToolStripMenuItem.ShortcutKeys = Keys.Delete;
        }

        #endregion

        #region Context Menu Handlers

        void addRemoveItem(string text)
        {
            if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
            removeToolStripMenuItem.Text = text;
            treeContextMenuStrip.Items.Add(removeToolStripMenuItem);

            string displayName = "";
            IList selectSource = treeViewHelper.getRemoveSource(ref displayName);
            removeToolStripMenuItem.Enabled = (selectSource.Count > 0);
            removeToolStripMenuItem.ShortcutKeys = (text != "Remove Views..." ? Keys.Delete : Keys.None);
        }

        void addAddItem(string text, object tag, string toolTip = "")
        {
            ToolStripMenuItem ts = new ToolStripMenuItem();
            ts.Click += new System.EventHandler(this.addToolStripMenuItem_Click);
            ts.Tag = tag;
            ts.Text = text;
            if (!string.IsNullOrEmpty(toolTip)) ts.ToolTipText = toolTip;
            treeContextMenuStrip.Items.Add(ts);
        }

        void addRemoveRootItem(string text, object tag)
        {
            ToolStripMenuItem ts = new ToolStripMenuItem();
            ts.Click += new System.EventHandler(this.removeRootToolStripMenuItem_Click);
            ts.Tag = tag;
            ts.Text = text;
            ts.ShortcutKeys = Keys.Delete;
            if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
            treeContextMenuStrip.Items.Add(ts);
        }

        void addCopyItem(string text, object tag)
        {
            ToolStripMenuItem ts = new ToolStripMenuItem();
            ts.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            ts.Tag = tag;
            ts.Text = text;
            ts.ShortcutKeys = (Keys.Control | Keys.C);
            if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
            treeContextMenuStrip.Items.Add(ts);
        }

        void addSmartCopyItem(string text, object tag)
        {
            ToolStripMenuItem ts = new ToolStripMenuItem();
            ts.Click += new System.EventHandler(this.smartCopyToolStripMenuItem_Click);
            ts.Tag = tag;
            ts.Text = text;
            if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
            treeContextMenuStrip.Items.Add(ts);
        }

        void addExecuteRenderContextItem(string name, string name2 = "")
        {
            if (executeToolStripMenuItem.Enabled)
            {
                ToolStripMenuItem ts = new ToolStripMenuItem();
                ts.Click += new System.EventHandler(this.executeToolStripMenuItem_Click);
                ts.Tag = false;
                ts.Text = "Execute " + Helper.QuoteSingle(name) + name2;
                if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                treeContextMenuStrip.Items.Add(ts);
                if (renderToolStripMenuItem.Enabled)
                {
                    ts = new ToolStripMenuItem();
                    ts.Click += new System.EventHandler(this.executeToolStripMenuItem_Click);
                    ts.Tag = true;
                    ts.Text = "Render " + Helper.QuoteSingle(name);
                    treeContextMenuStrip.Items.Add(ts);
                }
            }
        }

        private void initTreeContextMenuStrip()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                object entity = mainTreeView.SelectedNode.Tag;
                treeContextMenuStrip.Items.Clear();
                if (entity is SourceFolder)
                {
                    addAddItem("Add a new SQL Data Source", null);
                    addAddItem("Add a new LINQ Data Source", null);
                    foreach (var source in _repository.Sources)
                    {
                        addAddItem(string.Format("Add {0} (Repository)", source.Name), source);
                    }
                    addRemoveItem("Remove Data Sources...");
                }
                else if (entity is ViewFolder)
                {
                    if (RepositoryServer.ViewTemplates.Exists(i => i.Name == ReportViewTemplate.ModelName))
                    {
                        addAddItem("Add a View", null);
                    }
                    addRemoveItem("Remove Views...");
                }
                else if (entity is ReportView)
                {
                    var view = (ReportView)entity;
                    foreach (var template in view.ReportViewTemplateChildren)
                    {
                        addAddItem("Add a " + template.Name + " View", template, template.Description);
                    }
                    addRemoveItem("Remove Views...");
                    addCopyItem("Copy " + Helper.QuoteSingle(((RootComponent)entity).Name), entity);
                    addRemoveRootItem("Remove " + Helper.QuoteSingle(((RootComponent)entity).Name), entity);
                    treeViewHelper.addMoveUpDown(entity);

                    if (view.Model != null)
                    {
                        treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                        var item = new ToolStripMenuItem("Go to the Model");
                        item.Click += new EventHandler(delegate (object sender2, EventArgs e2)
                        {
                            selectNode(view.Model);
                        });
                        treeContextMenuStrip.Items.Add(item);
                    }
                    addSmartCopyItem("Smart copy...", entity);
                    if (mainTreeView.SelectedNode.Parent.Tag is ViewFolder) addExecuteRenderContextItem(((RootComponent)entity).Name);
                }
                else if (entity is TasksFolder)
                {
                    foreach (var template in RepositoryServer.TaskTemplates)
                    {
                        addAddItem("Add a " + template.Name + " Task", template);
                    }
                    addRemoveItem("Remove Tasks...");
                }
                else if (entity is ReportTask)
                {
                    var task = (ReportTask)entity;
                    foreach (var template in RepositoryServer.TaskTemplates)
                    {
                        addAddItem("Add a " + template.Name + " Task", template);
                    }

                    addRemoveItem("Remove Tasks...");
                    addCopyItem("Copy " + Helper.QuoteSingle(((RootComponent)entity).Name), entity);
                    addRemoveRootItem("Remove " + Helper.QuoteSingle(((RootComponent)entity).Name), entity);
                    addSmartCopyItem("Smart copy...", entity);
                    if (task.Enabled && task.ParentTask == null) addExecuteRenderContextItem(((RootComponent)entity).Name, " (this task only)");
                    treeViewHelper.addMoveUpDown(entity);
                }
                else if (entity is OutputFolder)
                {
                    foreach (var device in _repository.Devices)
                    {
                        addAddItem("Add Output for " + device.FullName, device);
                    }
                    addRemoveItem("Remove Outputs...");
                }
                else if (entity is ScheduleFolder)
                {
                    foreach (var output in _report.Outputs.OrderBy(i => i.Name))
                    {
                        addAddItem("Add Schedule for " + Helper.QuoteSingle(output.Name), output);
                    }
                    if (_report.Tasks.Count > 0)
                    {
                        if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                        addAddItem("Add Schedule for the Report Tasks", null);
                    }

                    addRemoveItem("Remove Schedules...");
                }
                else if (entity is ModelFolder)
                {
                    addAddItem("Add a MetaData Model", 1);
                    addAddItem("Add a SQL Model", 2);
                    addRemoveItem("Remove Models...");
                }
                else if (entity is ReportModel)
                {
                    var reportModel = entity as ReportModel;
                    if (reportModel.MasterModel != null)
                    {
                        return;
                    }

                    addCopyItem("Copy " + Helper.QuoteSingle(((RootComponent)entity).Name), entity);
                    addRemoveRootItem("Remove " + Helper.QuoteSingle(((RootComponent)entity).Name), entity);

                    var view = _report.AllViews.FirstOrDefault(i => i.Model == reportModel);
                    if (view != null)
                    {
                        treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                        var item = new ToolStripMenuItem("Go to the first model View");
                        item.Click += new EventHandler(delegate (object sender2, EventArgs e2)
                        {
                            selectNode(view);
                        });
                        treeContextMenuStrip.Items.Add(item);
                    }
                    addSmartCopyItem("Smart copy...", entity);

                    ToolStripMenuItem ts = new ToolStripMenuItem();
                    ts.Click += new System.EventHandler(convertModel);
                    ts.Text = reportModel.IsSQLModel ? "Convert SQL Model to a MetaData Model" : "Convert MetaData Model to a SQL Model";
                    if (!reportModel.IsSQLModel || (reportModel.IsSQLModel && !string.IsNullOrEmpty(reportModel.Table.Sql)))
                    {
                        if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                        treeContextMenuStrip.Items.Add(ts);
                    }
                }
                else if (entity is ReportOutput)
                {
                    addCopyItem("Copy " + Helper.QuoteSingle(((RootComponent)entity).Name), entity);
                    addRemoveRootItem("Remove " + Helper.QuoteSingle(((RootComponent)entity).Name), entity);
                    addSmartCopyItem("Smart copy...", entity);
                    addExecuteRenderContextItem(((RootComponent)entity).Name);
                }
                else if (entity is ReportSchedule)
                {
                    if (Report.Repository.UseSealScheduler)
                    {
                        addSmartCopyItem("Smart copy...", entity);
                    }

                    addRemoveRootItem("Remove " + Helper.QuoteSingle(((RootComponent)entity).Name), entity);
                }
                else if (entity is ReportSource)
                {
                    addRemoveRootItem("Remove " + Helper.QuoteSingle(((RootComponent)entity).Name), entity);

                    if (_report.Sources.Count > 1)
                    {
                        var item = new ToolStripMenuItem(string.Format("Keep {0} only", Helper.QuoteSingle(((RootComponent)entity).Name)));
                        item.Click += new EventHandler(delegate (object sender2, EventArgs e2)
                        {
                            var referenceSource = entity as ReportSource;
                            //Remove first source with table links
                            foreach (var source in _report.Sources.Where(i => i.GUID != referenceSource.GUID && i.MetaData.TableLinks.Count > 0).ToList())
                            {
                                try
                                {
                                    _report.RemoveSource(source);
                                }
                                catch { }
                            }
                            foreach (var source in _report.Sources.Where(i => i.GUID != referenceSource.GUID).ToList())
                            {
                                try
                                {
                                    _report.RemoveSource(source);
                                }
                                catch { }
                            }
                            IsModified = true;
                            init();
                        });
                        treeContextMenuStrip.Items.Add(item);
                    }

                    if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                    ToolStripMenuItem ts = new ToolStripMenuItem();
                    ts.Click += new System.EventHandler(convertReportSourceAsRepositorySource);
                    ts.Text = "Convert Report Source to a Repository Source...";
                    ts.Enabled = (((ReportSource)entity).MetaSourceGUID == null);
                    treeContextMenuStrip.Items.Add(ts);
                    treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                    ts = new ToolStripMenuItem();
                    ts.Click += new System.EventHandler(editMetaSource);
                    ts.Text = "Edit the Repository Source with the Server Manager...";
                    ts.Enabled = (((ReportSource)entity).MetaSourceGUID != null);
                    treeContextMenuStrip.Items.Add(ts);
                }
                else
                {
                    treeViewHelper.initTreeContextMenuStrip(new EventHandler(addToolStripMenuItem_Click));
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            object newEntity = null;
            KeyEventArgs key = null;
            if (selectedEntity is SourceFolder && Report != null)
            {
                MetaSource source = ((ToolStripMenuItem)sender).Tag as MetaSource;
                ReportSource newSource = Report.AddSource(source);
                newSource.IsNoSQL = ((ToolStripMenuItem)sender).Text.Contains("LINQ");
                if (source != null)
                {
                    newSource.LoadRepositoryMetaSources(_repository);
                }
                if (!newSource.IsNoSQL) newEntity = newSource.Connection;
                else
                {
                    newSource.Connection.ConnectionString = "";
                    newEntity = newSource;
                }
            }
            else if (selectedEntity is ModelFolder)
            {
                bool isSQLModel = (int)((ToolStripMenuItem)sender).Tag == 2;
                newEntity = _report.AddModel(isSQLModel); //1 Meta Model, 2 SQL Model
                if (isSQLModel) key = new KeyEventArgs(Keys.F7);
            }
            else if (selectedEntity is ViewFolder)
            {
                newEntity = _report.AddModelHTMLView();
            }
            else if (selectedEntity is ReportView)
            {
                newEntity = _report.AddChildView((ReportView)selectedEntity, (ReportViewTemplate)((ToolStripMenuItem)sender).Tag);
            }
            else if (selectedEntity is TasksFolder || selectedEntity is ReportTask)
            {
                ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
                newEntity = _report.AddTask(selectedEntity as ReportTask, (ReportTaskTemplate)menuItem.Tag);
            }
            else if (selectedEntity is OutputFolder)
            {
                ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
                if (menuItem != null) newEntity = _report.AddOutput((OutputDevice)menuItem.Tag);
            }
            else if (selectedEntity is ScheduleFolder)
            {
                ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
                if (menuItem != null) newEntity = _report.AddSchedule((ReportOutput)menuItem.Tag);
            }
            else
            {
                newEntity = treeViewHelper.addToolStripMenuItem_Click(sender, e);
            }

            if (newEntity != null)
            {
                IsModified = true;
                init(newEntity);

                if (key != null) toolStripHelper.HandleShortCut(key);
            }
        }

        private void sortColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                treeViewHelper.sortColumns_Click(sender, e, sender == sortColumnSQLOrderToolStripMenuItem);
                IsModified = true;
                mainTreeView.Sort();
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void resetDisplayOrderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                treeViewHelper.resetDisplayOrder_Click(sender, e);
                IsModified = true;
                mainTreeView.Sort();
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void convertModel(object sender, EventArgs e)
        {
            var model = selectedEntity as ReportModel;

            if (model.IsSQLModel)
            {
                var alias = model.Table.Alias;
                foreach (var col in model.Table.Columns) col.Name = model.Table.Alias + "." + col.Name;
                model.Source.MetaData.Tables.Add(model.Table);
                model.Table = null;
                MessageBox.Show(string.Format("A table named '{0}' has been created in the Data Source '{1}'.\r\nThe model is now a MetaData model.", alias, model.Source.Name), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                //Set column names
                foreach (var el in model.Elements) el.SQLColumnName = Regex.Replace(el.MetaColumn.Name.Replace(el.MetaColumn.MetaTable.AliasName + ".", ""), "[^A-Za-z]", "");
                foreach (var re in model.Restrictions) re.SQLColumnName = Regex.Replace(re.MetaColumn.Name.Replace(re.MetaColumn.MetaTable.AliasName + ".", ""), "[^A-Za-z]", "");
                foreach (var re in model.AggregateRestrictions) re.SQLColumnName = Regex.Replace(re.MetaColumn.Name.Replace(re.MetaColumn.MetaTable.AliasName + ".", ""), "[^A-Za-z]", "");

                model.BuildQuery(true);
                model.Table = MetaTable.Create();
                model.Table.DynamicColumns = true;
                model.Table.Sql = model.Sql;
                model.RefreshMetaTable(false);

                //Set new metacolumn GUID
                foreach (var el in model.Elements)
                {
                    var col = model.Table.Columns.FirstOrDefault(i => i.Name == el.SQLColumnName);
                    if (col != null) el.MetaColumnGUID = col.GUID;
                }
                foreach (var re in model.Restrictions)
                {
                    var col = model.Table.Columns.FirstOrDefault(i => i.Name == re.SQLColumnName);
                    if (col != null) re.MetaColumnGUID = col.GUID;
                }
                foreach (var re in model.AggregateRestrictions)
                {
                    var col = model.Table.Columns.FirstOrDefault(i => i.Name == re.SQLColumnName);
                    if (col != null) re.MetaColumnGUID = col.GUID;
                }

                MessageBox.Show("The model is now a SQL model having the original SQL generated.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            SetModified();
            init(model);
        }

        private void convertReportSourceAsRepositorySource(object sender, EventArgs e)
        {
            ReportSource source = selectedEntity as ReportSource;
            if (source != null)
            {
                if (MessageBox.Show("You are about to save the Report Source into a Repository Source file and convert the report to use this new Repository Source.\r\nDo you want to continue ?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel) return;
                string path = ToolsHelper.SaveConfigurationFile(_repository.SourcesFolder, "", source.Name);
                if (string.IsNullOrEmpty(path)) return;

                //Create and save a meta source
                MetaSource metaSource = MetaSource.Create(_repository);
                metaSource.IsNoSQL = source.IsNoSQL;
                metaSource.InitScript = source.InitScript;
                metaSource.Connections.Clear();
                metaSource.Connections.AddRange(source.Connections);
                metaSource.MetaData.Joins.AddRange(source.MetaData.Joins);
                metaSource.MetaData.Tables.Clear();
                metaSource.MetaData.Tables.AddRange(source.MetaData.Tables.Where(i => source.IsNoSQL));
                metaSource.MetaData.Enums.AddRange(source.MetaData.Enums);
                metaSource.ConnectionGUID = source.ConnectionGUID;
                metaSource.PreSQL = source.PreSQL;
                metaSource.PostSQL = source.PostSQL;
                metaSource.IgnorePrePostError = source.IgnorePrePostError;
                metaSource.SaveToFile(path);

                _repository.Sources.Add(metaSource);

                //convert the report source to the metasource
                source.MetaSourceGUID = metaSource.GUID;
                source.Connections.Clear();
                source.MetaData.Joins.Clear();
                source.MetaData.Tables.Clear();
                source.MetaData.Enums.Clear();
                source.Name += " (Repository)";
                source.LoadRepositoryMetaSources(_repository);
                IsModified = true;
                init(source);
            }
        }


        private void editMetaSource(object sender, EventArgs e)
        {
            ReportSource source = selectedEntity as ReportSource;
            string path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), Repository.SealServerManager);
#if DEBUG
            path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) + string.Format(@"\..\..\..\{0}\bin\Debug", Path.GetFileNameWithoutExtension(Repository.SealServerManager)), Repository.SealServerManager);
#endif
            MetaSource metaSource = _repository.Sources.FirstOrDefault(i => i.GUID == source.MetaSourceGUID);
            if (metaSource != null)
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(path, string.Format("/o {0}", Helper.QuoteDouble(metaSource.FilePath))) { UseShellExecute = true };
                p.Start();
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            object newEntity = null;
            if (selectedEntity is ReportModel)
            {
                newEntity = Helper.Clone(selectedEntity);
                _report.Models.Add((ReportModel)newEntity);
                _report.InitReferences();
                ((RootComponent)newEntity).GUID = Guid.NewGuid().ToString();
                ReportModel model = (ReportModel)newEntity;
                foreach (var item in model.Elements) item.GUID = Guid.NewGuid().ToString();
                foreach (var item in model.Restrictions)
                {
                    string oldGUID = item.GUID;
                    item.GUID = Guid.NewGuid().ToString();
                    model.Restriction = model.Restriction.Replace(oldGUID, item.GUID);
                }
                ((RootComponent)newEntity).Name = Helper.GetUniqueName(((RootComponent)selectedEntity).Name + " - Copy", (from i in _report.Models select i.Name).ToList());
            }
            else if (selectedEntity is ReportView)
            {
                ReportView parent = mainTreeView.SelectedNode.Parent.Tag as ReportView;
                List<ReportView> views = (parent == null ? _report.Views : parent.Views);
                newEntity = Helper.Clone(selectedEntity);
                views.Add((ReportView)newEntity);
                _report.InitReferences();
                ((RootComponent)newEntity).GUID = Guid.NewGuid().ToString();
                ((ReportView)newEntity).ReinitGUIDChildren();
                ((RootComponent)newEntity).Name = Helper.GetUniqueName(((RootComponent)selectedEntity).Name + " - Copy", (from i in views select i.Name).ToList());
                int idx = 1;
                foreach (var view in views.OrderBy(i => i.SortOrder)) view.SortOrder = idx++;
            }
            else if (selectedEntity is ReportTask)
            {
                var taskEntity = selectedEntity as ReportTask;
                newEntity = Helper.Clone(selectedEntity);
                var tasks = taskEntity.ParentTask != null ? taskEntity.ParentTask.Tasks : Report.Tasks;
                tasks.Add((ReportTask)newEntity);
                _report.InitReferences();
                ((RootComponent)newEntity).GUID = Guid.NewGuid().ToString();
                ((RootComponent)newEntity).Name = Helper.GetUniqueName(taskEntity.Name + " - Copy", (from i in tasks select i.Name).ToList());
                int idx = 1;
                foreach (var task in tasks.OrderBy(i => i.SortOrder)) task.SortOrder = idx++;
            }
            else if (selectedEntity is ReportOutput)
            {
                newEntity = Helper.Clone(selectedEntity);
                _report.Outputs.Add((ReportOutput)newEntity);
                _report.InitReferences();
                ((RootComponent)newEntity).GUID = Guid.NewGuid().ToString();
                ((RootComponent)newEntity).Name = Helper.GetUniqueName(((RootComponent)selectedEntity).Name + " - Copy", (from i in _report.Outputs select i.Name).ToList());
            }
            /* not useful unless we copy also the Task Definition....
            else if (selectedEntity is ReportSchedule)
            {
                newEntity = _report.AddSchedule(((ReportSchedule)selectedEntity).Output);
                Helper.CopyProperties(selectedEntity, newEntity);
                ((ReportSchedule)newEntity).Task = null; //Force a new task to be created in the Task Scheduler..
                ((RootComponent)newEntity).GUID = Guid.NewGuid().ToString();
                ((RootComponent)newEntity).Name = Helper.GetUniqueName(((RootComponent)selectedEntity).Name + " - Copy", (from i in _report.Schedules select i.Name).ToList());
            }*/
            else
            {
                newEntity = treeViewHelper.copyToolStripMenuItem_Click(sender, e);
            }

            if (newEntity != null)
            {
                IsModified = true;
                init(newEntity);
            }
        }

        private void smartCopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SmartCopyForm form = null;
            if (selectedEntity is ReportModel)
            {
                form = new SmartCopyForm("Smart copy of " + ((ReportModel)selectedEntity).Name, selectedEntity, _report);
                form.ShowDialog();
            }
            else if (selectedEntity is ReportView)
            {
                form = new SmartCopyForm("Smart copy of " + ((ReportView)selectedEntity).Name, selectedEntity, _report);
                form.ShowDialog();
            }
            else if (selectedEntity is ReportTask)
            {
                form = new SmartCopyForm("Smart copy of " + ((ReportTask)selectedEntity).Name, selectedEntity, _report);
                form.ShowDialog();
            }
            else if (selectedEntity is ReportOutput)
            {
                form = new SmartCopyForm("Smart copy of " + ((ReportOutput)selectedEntity).Name, selectedEntity, _report);
                form.ShowDialog();
            }
            else if (selectedEntity is ReportSchedule)
            {
                form = new SmartCopyForm("Smart copy of " + ((ReportSchedule)selectedEntity).Name, selectedEntity, _report);
                form.ShowDialog();
            }
            else if (selectedEntity is TasksFolder)
            {
                form = new SmartCopyForm("Smart copy of Tasks Script", selectedEntity, _report);
                form.ShowDialog();
            }

            if (form != null && form.IsReportModified)
            {
                CannotRenderAnymore();
                SetModified();
                init();
                var lastEntity = selectedEntity;
                mainTreeView.Sort();
                if (lastEntity != null) selectNode(lastEntity);
            }
        }


        private void removeRootToolStripMenuItem_Click(object sender, EventArgs e)
        {
            object newEntity;
            var nodes = mainTreeView.SelectedNode.Parent.Nodes;
            var index = nodes.IndexOf(mainTreeView.SelectedNode) + 1;

            if (nodes.Count > index) newEntity = nodes[index].Tag;
            else if (nodes.Count == index && nodes.Count > 1) newEntity = nodes[index - 2].Tag;
            else newEntity = mainTreeView.SelectedNode.Parent.Tag;

            if (selectedEntity is ReportModel)
            {
                _report.RemoveModel((ReportModel)selectedEntity);
            }
            else if (selectedEntity is ReportView)
            {
                var view = (ReportView)selectedEntity;

                if (mainTreeView.SelectedNode.Parent != null && mainTreeView.SelectedNode.Parent.Tag is ReportView)
                {
                    _report.RemoveView((ReportView)mainTreeView.SelectedNode.Parent.Tag, view);
                }
                else
                {
                    _report.RemoveView(null, view);
                }

            }
            else if (selectedEntity is ReportTask)
            {
                _report.RemoveTask((ReportTask)selectedEntity);
            }
            else if (selectedEntity is ReportOutput)
            {
                _report.RemoveOutput((ReportOutput)selectedEntity);
            }
            else if (selectedEntity is ReportSchedule)
            {
                _report.RemoveSchedule((ReportSchedule)selectedEntity);
            }
            else if (selectedEntity is ReportSource)
            {
                _report.RemoveSource((ReportSource)selectedEntity);
            }
            else
            {
                newEntity = treeViewHelper.removeRootToolStripMenuItem_Click(sender, e);
            }


            if (newEntity != null)
            {
                IsModified = true;
                init(newEntity);
            }
        }
        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (treeViewHelper.removeToolStripMenuItem_Click(sender, e))
                {
                    IsModified = true;
                    init();
                }
            }
            catch
            {
                IsModified = true;
                init();
                throw;
            }
        }


        private void addFromToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewHelper.addFromToolStripMenuItem_Click(sender, e))
            {
                IsModified = true;
                init();
            }
        }

        private void executeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem && ((ToolStripMenuItem)sender).Tag is bool)
            {
                sender = ((bool)((ToolStripMenuItem)sender).Tag) ? renderViewOutputToolStripMenuItem : executeViewOutputToolStripMenuItem;
            }

            bool render = (sender == renderToolStripButton || sender == renderToolStripMenuItem || sender == renderViewOutputToolStripButton || sender == renderViewOutputToolStripMenuItem);
            string viewGUID = null, outputGUID = null, taskGUID = null;
            if (sender == renderViewOutputToolStripMenuItem || sender == executeViewOutputToolStripMenuItem || sender == renderViewOutputToolStripButton || sender == executeViewOutputToolStripButton)
            {
                if (selectedEntity is ReportTask)
                {
                    taskGUID = ((ReportTask)selectedEntity).GUID;
                }
                else if (selectedEntity is ReportOutput)
                {
                    outputGUID = ((ReportOutput)selectedEntity).GUID;
                }
                else if (selectedEntity is ReportView)
                {
                    //Get the parent view
                    TreeNode node = mainTreeView.SelectedNode;
                    while (!(node.Parent.Tag is ViewFolder)) node = node.Parent;
                    viewGUID = (node.Tag as ReportView).GUID;
                }
            }
            ExecuteReport(render, viewGUID, outputGUID, taskGUID);
        }

        public void ExecuteReport(bool render, string viewGUID, string outputGUID, string taskGUID)
        {
            //commit panels
            if (selectedEntity is ReportModel) modelPanel.Commit();

            //check report integrity...
            if (_report.Models.Count == 0)
            {
                if (MessageBox.Show("This report has no Model and cannot be executed. Do you want to create a Model now ?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    IsModified = true;
                    init(_report.AddModel(false));
                }
                return;
            }
            if (_report.Views.Count == 0)
            {
                if (MessageBox.Show("This report has no View and cannot be executed. Do you want to create a View now ?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    IsModified = true;
                    init(_report.AddModelHTMLView());
                }
                return;
            }

            if (_reportViewer == null || !_reportViewer.Visible)
            {
                _reportViewer = new ReportViewerForm(false, Properties.Settings.Default.ShowScriptErrors);
                _reportViewer.ReportDesignerForm = this;
            }
            _reportViewer.ViewReport(_report.Clone(), render, viewGUID, outputGUID, _report.FilePath, taskGUID);
            _canRender = true;
            FileHelper.PurgeTempApplicationDirectory();
        }

        private void mainPropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (treeViewHelper.mainPropertyGrid_PropertyValueChanged(s, e)) init();
            IsModified = true;
            enableControls();
        }

        private void dynamicColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewHelper.dynamicColumnsToolStripMenuItem_Click(sender, e);
            IsModified = true;
            init();
        }

        private bool _isDoubleClick = false;

        private void mainTreeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            if (_isDoubleClick && e.Action == TreeViewAction.Collapse && (selectedEntity is ReportTask || selectedEntity is ReportModel || selectedEntity is ReportView))
                e.Cancel = true;
        }

        private void mainTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (_isDoubleClick && e.Action == TreeViewAction.Expand && (selectedEntity is ReportTask || selectedEntity is ReportModel || selectedEntity is ReportView))
                e.Cancel = true;
        }

        private void mainTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            _isDoubleClick = e.Clicks > 1;
        }

        private void mainTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (selectedEntity is ReportTask)
            {
                if (!string.IsNullOrEmpty(((ReportTask)selectedEntity).SQL)) toolStripHelper.HandleShortCut(new KeyEventArgs(Keys.F8));
                else toolStripHelper.HandleShortCut(new KeyEventArgs(Keys.F7));
                mainTreeView.SelectedNode.Expand();
            }
            else if (selectedEntity is ReportModel)
            {
                if (((ReportModel)selectedEntity).IsSQLModel) toolStripHelper.HandleShortCut(new KeyEventArgs(Keys.F7));
                else if (((ReportModel)selectedEntity).Elements.Count > 0) toolStripHelper.HandleShortCut(new KeyEventArgs(Keys.F8));
                mainTreeView.SelectedNode.Expand();
            }
            else if (selectedEntity is ReportView)
            {
                ReportView view = (ReportView)selectedEntity;
                if (view.UseCustomTemplate) toolStripHelper.HandleShortCut(new KeyEventArgs(Keys.F8));
                else mainTreeView.SelectedNode.Expand();
            }
            else if (selectedEntity is ReportSchedule)
            {
                toolStripHelper.HandleShortCut(new KeyEventArgs(Keys.F8));
            }
        }

        private void mainTimer_Tick(object sender, EventArgs e)
        {
            executeToolStripMenuItem.Enabled = (_report != null && (_reportViewer == null || (_reportViewer != null && _reportViewer.CanExecute)));
            executeToolStripButton.Enabled = executeToolStripMenuItem.Enabled;
            renderToolStripMenuItem.Enabled = (_canRender && _report != null && _reportViewer != null && _reportViewer.Visible && _reportViewer.CanRender);
            renderToolStripButton.Enabled = renderToolStripMenuItem.Enabled;

            executeViewOutputToolStripMenuItem.Enabled = executeToolStripMenuItem.Enabled;
            renderViewOutputToolStripMenuItem.Enabled = renderToolStripMenuItem.Enabled;
            executeViewOutputToolStripButton.Enabled = executeToolStripMenuItem.Enabled;
            renderViewOutputToolStripButton.Enabled = renderToolStripMenuItem.Enabled;
        }

        private void ReportDesigner_KeyDown(object sender, KeyEventArgs e)
        {
            toolStripHelper.HandleShortCut(e);
        }

        private void nextView_Click(object sender, EventArgs e)
        {

            if (_report != null)
            {
                if (mainTreeView.SelectedNode == null) selectNode(_report.Views[0]);
                ReportView currentView = mainTreeView.SelectedNode.Tag as ReportView;

                var templateToSearch = "Model";
                if (sender == nextWidgetViewMenuItem) templateToSearch = "Widget";

                if (currentView == null) currentView = _report.Views[0];

                ReportView nextView = null;
                var allViews = _report.AllViews;
                for (int i = allViews.IndexOf(currentView) + 1; i < allViews.Count; i++)
                {
                    if (allViews[i].TemplateName == templateToSearch)
                    {
                        nextView = allViews[i];
                        break;
                    }
                }
                if (nextView == null)
                {
                    for (int i = 0; i <= allViews.IndexOf(currentView); i++)
                    {
                        if (allViews[i].TemplateName == templateToSearch)
                        {
                            nextView = allViews[i];
                            break;
                        }
                    }
                }

                if (nextView != null)
                {
                    selectNode(nextView);
                }
                else
                {
                    MessageBox.Show($"This report has no {templateToSearch} View", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        #endregion

        #region Drag and drop
        private void mainTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            treeViewHelper.mainTreeView_ItemDrag(this, sender, e);
        }

        private void mainTreeView_DragEnter(object sender, DragEventArgs e)
        {
            treeViewHelper.mainTreeView_DragEnter(sender, e);
        }

        TreeNode _lastDragOverNode = null;

        private void mainTreeView_DragDrop(object sender, DragEventArgs e)
        {
            _lastDragOverNode = null;
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                TreeNode targetNode = ((TreeView)sender).GetNodeAt(((TreeView)sender).PointToClient(new Point(e.X, e.Y)));
                TreeNode sourceNode = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
                if (sourceNode != null && targetNode != null && sourceNode.Parent != null && sourceNode.Tag is ReportView && targetNode.Tag is ReportView)
                {
                    ReportView sourceView = sourceNode.Tag as ReportView;
                    ReportView targetView = targetNode.Tag as ReportView;
                    if (targetView.ReportViewTemplateChildren.Exists(i => i.Name == sourceView.TemplateName) && !sourceView.IsAncestorOf(targetView))
                    {
                        //move the parent
                        ReportView parent = sourceNode.Parent.Tag as ReportView;
                        parent.Views.Remove(sourceView);

                        //check if the target view is not a child of the source
                        var view = targetView;
                        while (view.ParentView != null)
                        {
                            if (view.ParentView == sourceView)
                            {
                                sourceView.Views.Remove(view);
                                parent.Views.Add(view);
                                view.ParentView = parent;
                                break;
                            }
                            view = view.ParentView;
                        }

                        sourceView.ParentView = targetView;
                        targetView.Views.Add(sourceView);
                        targetView.InitReferences();
                        SetModified();
                        init(sourceView);
                        e.Effect = DragDropEffects.Move;
                        mainTreeView.SelectedNode = sourceNode;
                    }
                    else if (sourceNode.Parent == targetNode.Parent)
                    {
                        //move the position
                        List<ReportView> views = (targetNode.Parent.Tag is ReportView) ? ((ReportView)targetNode.Parent.Tag).Views : _report.Views;
                        int index = 0;
                        foreach (var view in views.OrderBy(i => i.SortOrder))
                        {
                            if (view == targetView)
                            {
                                sourceView.SortOrder = index++;
                                targetView.SortOrder = index;
                                if (index == views.Count)
                                {
                                    sourceView.SortOrder = index;
                                    targetView.SortOrder = index - 1;
                                }
                            }
                            else if (view != sourceView)
                            {
                                view.SortOrder = index;
                            }
                            index++;
                        }
                        SetModified();
                        mainTreeView.Sort();
                        e.Effect = DragDropEffects.Move;
                        mainTreeView.SelectedNode = sourceNode;
                    }
                }
                else if (sourceNode != null && targetNode != null && sourceNode != targetNode && sourceNode.Tag is ReportTask)
                {
                    ReportTask sourceTask = sourceNode.Tag as ReportTask;
                    ReportTask targetTask = targetNode.Tag as ReportTask;
                    var tasks = targetTask == null ? Report.Tasks : targetTask.Tasks;

                    //move the parent
                    ReportTask parent = sourceNode.Parent.Tag as ReportTask;
                    if (parent != null) parent.Tasks.Remove(sourceTask);
                    else Report.Tasks.Remove(sourceTask);

                    //Set new parent
                    sourceTask.ParentTask = targetTask;
                    if (targetTask == null && sourceTask.ConnectionGUID == ReportTask.ParentTaskConnectionGUID) sourceTask.ConnectionGUID = ReportSource.DefaultReportConnectionGUID;

                    tasks.Add(sourceTask);
                    //Set first or last
                    sourceTask.SortOrder = sourceTask.SortOrder == tasks.Min(i => i.SortOrder) ? tasks.Count + 1 : -1;

                    //move the position
                    int index = 0;
                    foreach (var task in tasks.OrderBy(i => i.SortOrder))
                    {
                        task.SortOrder = index++;
                    }
                    SetModified();
                    mainTreeView.Sort();
                    init(_tasksTN);
                    e.Effect = DragDropEffects.Move;
                    selectNode(sourceTask);
                }
                else treeViewHelper.mainTreeView_DragDrop(sender, e);
            }
        }

        private void mainTreeView_DragOver(object sender, DragEventArgs e)
        {
            treeViewHelper.mainTreeView_DragOver(sender, e);
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                TreeNode targetNode = ((TreeView)sender).GetNodeAt(((TreeView)sender).PointToClient(new Point(e.X, e.Y)));
                _lastDragOverNode = null;
                if (targetNode != null && e.Effect == DragDropEffects.Move)
                {
                    _lastDragOverNode = targetNode;
                    mainTreeView.SelectedNode = targetNode;
                }
            }
        }
        #endregion

    }

}
