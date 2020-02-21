//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Seal.Model;
using Seal.Helpers;
using Seal.Forms;

namespace Seal.Controls
{
    public partial class ModelPanel : UserControl
    {
        public ReportModel Model;
        public ReportDesigner MainForm;

        ElementPanel PagePanel;
        ElementPanel ColumnPanel;
        ElementPanel RowPanel;
        ElementPanel DataPanel;
        List<ElementPanel> PanelList;

        public PropertyGrid ModelGrid = new PropertyGrid();
        public PropertyGrid ElementGrid = new PropertyGrid();
        public PropertyGrid RestrictionGrid = new PropertyGrid();

        bool collapseElementCategoriesDone = false;

        public Button SelectedButton = null;

        public ModelPanel()
        {
            RowPanel = new ElementPanel(this, PivotPosition.Row);
            DataPanel = new ElementPanel(this, PivotPosition.Data);
            PagePanel = new ElementPanel(this, PivotPosition.Page);
            ColumnPanel = new ElementPanel(this, PivotPosition.Column);
            PanelList = new List<ElementPanel>() { PagePanel, ColumnPanel, RowPanel, DataPanel };

            InitializeComponent();

            aggregateRestrictionsPanel.IsAggregate = true;

            ModelGrid.Dock = DockStyle.Fill;
            ModelGrid.PropertyValueChanged += Grid_PropertyValueChanged;
            ModelGrid.ToolbarVisible = false;
            ModelGrid.HelpVisible = true;
            ModelGrid.PropertySort = PropertySort.Categorized;
            ModelGrid.LineColor = SystemColors.ControlLight;
            modelSourceSplitContainer.Panel1.Controls.Add(ModelGrid);
            PropertyGridHelper.AddResetMenu(ModelGrid);

            ElementGrid.Dock = DockStyle.Fill;
            ElementGrid.PropertyValueChanged += Grid_PropertyValueChanged;
            ElementGrid.ToolbarVisible = false;
            ElementGrid.PropertySort = PropertySort.Categorized;
            ElementGrid.LineColor = SystemColors.ControlLight;
            elementsContainer.Panel2.Controls.Add(ElementGrid);
            foreach (var panel in PanelList) elementsContainer.Panel1.Controls.Add(panel);
            PropertyGridHelper.AddResetMenu(ElementGrid);

            RestrictionGrid.Dock = DockStyle.Fill;
            RestrictionGrid.PropertyValueChanged += Grid_PropertyValueChanged;
            RestrictionGrid.SelectedObjectsChanged += new EventHandler(RestrictionGrid_SelectedObjectsChanged);
            RestrictionGrid.ToolbarVisible = false;
            RestrictionGrid.PropertySort = PropertySort.Categorized;
            RestrictionGrid.LineColor = SystemColors.ControlLight;
            restrictionsContainer.Panel2.Controls.Add(RestrictionGrid);
            PropertyGridHelper.AddResetMenu(RestrictionGrid);
        }

        public void Init(ReportDesigner mainForm)
        {
            MainForm = mainForm;
            Model.InitReferences();

            Model.InitEditor();
            ModelGrid.SelectedObject = Model;

            ElementGrid.SelectedObject = null;
            SelectedButton = null;

            initTreeView();
            elementTreeView.MouseUp += elementTreeView_MouseUp;

            ElementsToPanels();

            restrictionsPanel.Init(this);
            aggregateRestrictionsPanel.Init(this);
            initNoSQL();
            RestrictionGrid.SelectedObject = null;

            resizeControls();

            //adjust description height
            PropertyGridHelper.ResizeDescriptionArea(ModelGrid, 3);
        }

        void initNoSQL()
        {
            aggregateRestrictionsPanel.Visible = !Model.Source.IsNoSQL;
            selectedRestrictionsGroupBox.Text = Model.Source.IsNoSQL ? "Restrictions" : "Restrictions and Aggregate Restrictions";
        }

        void elementTreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Select the clicked node
                elementTreeView.SelectedNode = elementTreeView.GetNodeAt(e.X, e.Y);
                ContextMenuStrip menu = new ContextMenuStrip();
                if (elementTreeView.SelectedNode != null && elementTreeView.SelectedNode.Tag is MetaColumn)
                {
                    MetaColumn column = elementTreeView.SelectedNode.Tag as MetaColumn;
                    ToolStripMenuItem item = new ToolStripMenuItem("Select");
                    item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                    {
                        addElement(false);
                    });
                    menu.Items.Add(item);

                    item = new ToolStripMenuItem("Prompt at run-time");
                    item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                    {
                        if (column.IsAggregate == true) aggregateRestrictionsPanel.AddRestriction(column, true);
                        else restrictionsPanel.AddRestriction(column, true);
                    });
                    menu.Items.Add(item);

                    if (SelectedButton != null && SelectedButton.Tag is ReportElement)
                    {
                        item = new ToolStripMenuItem("Replace the selected element");
                        item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                        {
                            addElement(true);
                        });
                        menu.Items.Add(item);
                    }
                }
                else if (elementTreeView.SelectedNode != null && elementTreeView.SelectedNode.Nodes.Count > 0)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem("Select all");
                    item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                    {
                        foreach (TreeNode node in elementTreeView.SelectedNode.Nodes)
                        {
                            if (node.Tag is MetaColumn) 
                            {
                                AddElement(RowPanel, (MetaColumn)node.Tag, true);
                                MainForm.IsModified = true;
                                MainForm.CannotRenderAnymore();
                                PanelsToElements();
                                RowPanel.RedrawPanel();
                            }
                        }
                    });
                    menu.Items.Add(item);

                    item = new ToolStripMenuItem("Sort by Name");
                    item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                    {
                        elementTreeView.TreeViewNodeSorter = null;
                        elementTreeView.Sort();
                    });
                    menu.Items.Add(new ToolStripSeparator());
                    menu.Items.Add(item);

                    item = new ToolStripMenuItem("Sort by Position");
                    item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                    {
                        elementTreeView.TreeViewNodeSorter = new NodeSorter();
                        elementTreeView.Sort();
                    });
                    menu.Items.Add(item);
                }
                if (menu.Items.Count > 0) menu.Show(elementTreeView, e.Location);
            }
        }

        public void Commit()
        {
            restrictionsPanel.Commit();
            aggregateRestrictionsPanel.Commit();
        }


        public Button AddElement(ElementPanel panel, MetaColumn column, bool selectButton)
        {
            ReportElement element = ReportElement.Create();
            element.Source = Model.Source;
            element.Format = column.Format;
            element.Report = Model.Report;
            element.Model = Model;
            element.MetaColumnGUID = column.GUID;
            element.MetaColumn = column;
            element.Name = column.Name;
            element.PivotPosition = panel.Position;
            element.SetDefaults();
            Model.Elements.Add(element);
            return AddElement(panel, element, selectButton);
        }

        public Button AddElement(ElementPanel panel, ReportElement element, bool selectButton)
        {
            Button button = new Button() { Text = element.DisplayNameEl };
            button.Tag = element;

            button.MouseDown += new MouseEventHandler(btn_MouseDown);
            panel.Controls.Add(button);
            if (selectButton) btn_MouseDown(button, null);
            panel.ResizeControls();
            return button;
        }

        public void ElementsToPanels()
        {
            foreach (var panel in PanelList) panel.Controls.Clear();
            foreach (var element in Model.Elements)
            {
                if (element.PivotPosition == PivotPosition.Page) AddElement(PagePanel, element, false);
                if (element.PivotPosition == PivotPosition.Column) AddElement(ColumnPanel, element, false);
                if (element.PivotPosition == PivotPosition.Row) AddElement(RowPanel, element, false);
                if (element.PivotPosition == PivotPosition.Data) AddElement(DataPanel, element, false);
            }

            foreach (var panel in PanelList) panel.RedrawPanel();
        }

        public void PanelsToElements()
        {
            Model.Elements.Clear();
            foreach (var panel in PanelList) panel.PanelToElements(Model.Elements);
        }

        public void CollapseCategories(PropertyGrid grid)
        {
            GridItem root = grid.SelectedGridItem;
            if (root == null) return;
            while (root.Parent != null) root = root.Parent;
            foreach (GridItem item in root.GridItems)
            {
                string label = item.Label.Replace("\t", "").ToLower();
                if (label == "advanced") item.Expanded = false;
                if (label == "chart") item.Expanded = false;
                if (label == "extra restriction values...") item.Expanded = false;
            }
        }


        void btn_MouseDown(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;

            //set property grid
            ElementGrid.PropertyValueChanged -= Grid_PropertyValueChanged;
            ReportElement element = null;
            if (button.Tag != null) element = button.Tag as ReportElement;
            if (element != null)
            {
                element.InitEditor();
                bool collapseCategories = (ElementGrid.SelectedObject == null);

                ElementGrid.SelectedObject = button.Tag;

                //Collapse Advanced categories
                if (collapseCategories && !collapseElementCategoriesDone)
                {
                    CollapseCategories(ElementGrid);
                    collapseElementCategoriesDone = true;
                }
            }

            ElementGrid.PropertyValueChanged += Grid_PropertyValueChanged;

            if (e != null)
            {
                DragDropEffects dde1 = DoDragDrop(sender, DragDropEffects.Move);
            }

            if (button.Parent != null)
            { 
                SelectedButton = button;
                redrawButtons();
            }

            //select meta element in TreeView
            if (element != null) SetMetaColumn(elementTreeView.Nodes, element);

            if (e != null && e.Button == MouseButtons.Right)
            {
                ContextMenuStrip menu = new ContextMenuStrip();
                ToolStripMenuItem item = new ToolStripMenuItem("Remove");
                item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                {
                    removeElementFromPanel(button, false);
                });
                menu.Items.Add(item);

                item = new ToolStripMenuItem("Remove all elements");
                item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                {
                    removeElementFromPanel(button, true);
                });
                menu.Items.Add(item);

                item = new ToolStripMenuItem("Copy");
                item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                {
                    copyElementFromPanel(button);
                });
                menu.Items.Add(item);

                item = new ToolStripMenuItem("Prompt at run-time");
                item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                {
                    if (element.MetaColumn.IsAggregate == true) aggregateRestrictionsPanel.AddRestriction(element.MetaColumn, true);
                    else restrictionsPanel.AddRestriction(element.MetaColumn, true);
                });
                menu.Items.Add(item);

                menu.Items.Add(new ToolStripSeparator());
                item = new ToolStripMenuItem("Smart copy...");
                item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                {
                    SmartCopyForm form = new SmartCopyForm("Smart copy of " + element.DisplayNameEl, element, Model.Report);
                    form.ShowDialog();
                    if (form.IsReportModified)
                    {
                        MainForm.IsModified = true;
                        MainForm.CannotRenderAnymore();
                        ElementsToPanels();
                    }

                });
                menu.Items.Add(item);
                //Display context menu
                menu.Show(button, e.Location);
            }

        }

        void redrawButtons()
        {
            foreach (var panel in PanelList) panel.RedrawButtons();
        }

        public void SetMetaColumn(ReportElement element)
        {
            SetMetaColumn(elementTreeView.Nodes, element);
        }

        public bool SetMetaColumn(TreeNodeCollection nodes, ReportElement element)
        {
            //select meta element in TreeView
            foreach (TreeNode node in nodes)
            {
                if (element.MetaColumn == node.Tag)
                {
                    elementTreeView.SelectedNode = node;
                    elementTreeView.SelectedNode.EnsureVisible();
                    elementTreeView.Focus();
                    return true;
                }
                if (SetMetaColumn(node.Nodes, element)) return true;
            }
            return false;
        }

        void resizeControls()
        {
            int referenceWidth = elementsContainer.Panel1.Width - 6;
            int referenceHeight = elementsContainer.Panel1.Height - 6;

            PagePanel.Width = referenceWidth / 2 - 4;
            PagePanel.Height = referenceHeight / 3;
            PagePanel.Location = new Point(4, 5);
            PagePanel.ResizeControls();

            ColumnPanel.Width = referenceWidth / 2 - 4;
            ColumnPanel.Height = referenceHeight / 3;
            ColumnPanel.Location = new Point(referenceWidth / 2 + 4, 5);
            ColumnPanel.ResizeControls();

            RowPanel.Width = referenceWidth / 2 - 4;
            RowPanel.Height = 2 * referenceHeight / 3 - 8;
            RowPanel.Location = new Point(4, referenceHeight / 3 + 8);
            RowPanel.ResizeControls();

            DataPanel.Width = referenceWidth / 2 - 4;
            DataPanel.Height = 2 * referenceHeight / 3 - 8;
            DataPanel.Location = new Point(referenceWidth / 2 + 4, referenceHeight / 3 + 8);
            DataPanel.ResizeControls();

            selectedElementsGroupBox.Refresh();
        }

        void initTreeView()
        {
            var tableList = Model.Source.MetaData.Tables;
            if (Model.IsSQLModel)
            {
                tableList = new List<MetaTable>();
                tableList.Add(Model.Table);
            }

            TreeViewHelper.InitCategoryTreeNode(elementTreeView.Nodes, tableList);
            elementTreeView.TreeViewNodeSorter = new NodeSorter(); 
            elementTreeView.Sort();
            if (Model.IsSQLModel) elementTreeView.ExpandAll();
        }

        private void elementTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void panel_Resize(object sender, EventArgs e)
        {
            resizeControls();
        }

        private void elementTreeView_DoubleClick(object sender, EventArgs e)
        {
            addElement(false);
        }

        void addElement(bool replaceSelected)
        {
            if (elementTreeView.SelectedNode != null && elementTreeView.SelectedNode.Tag is MetaColumn)
            {
                //If an element is already selected, try to insert the new one just after...
                ElementPanel panel = RowPanel;
                if (SelectedButton != null && SelectedButton.Parent is ElementPanel) panel = SelectedButton.Parent as ElementPanel;
                int newIndex = -1;
                ReportElement element = null;
                if (SelectedButton != null && SelectedButton.Tag is ReportElement && panel.Controls.Contains(SelectedButton))
                {
                    element = SelectedButton.Tag as ReportElement;
                    if (element.PivotPosition == PivotPosition.Data) panel = DataPanel;
                    if (element.PivotPosition == PivotPosition.Column) panel = ColumnPanel;
                    if (element.PivotPosition == PivotPosition.Page) panel = PagePanel;

                    newIndex = panel.Controls.GetChildIndex(SelectedButton) + 1;

                    if (replaceSelected)
                    {
                        panel.Controls.Remove(SelectedButton);
                        Model.Elements.Remove(element);
                        newIndex--;
                    } 
                }

                //For aggregate force data panel
                if (!replaceSelected && ((MetaColumn)elementTreeView.SelectedNode.Tag).IsAggregate) panel = DataPanel;

                Button button = AddElement(panel, (MetaColumn)elementTreeView.SelectedNode.Tag, true);
                //move position to the next button
                if (newIndex != -1) panel.Controls.SetChildIndex(button, newIndex);

                if (replaceSelected && element != null)
                {
                    //copy interesting properties
                    ReportElement newElement = button.Tag as ReportElement;
                    newElement.AggregateFunction = element.AggregateFunction;
                    newElement.CalculationOption = element.CalculationOption;
                    newElement.Nvd3Serie = element.Nvd3Serie;
                    newElement.ChartJSSerie = element.ChartJSSerie;
                    newElement.PlotlySerie = element.PlotlySerie;
                    newElement.SerieDefinition = element.SerieDefinition;
                    newElement.SerieSortOrder = element.SerieSortOrder;
                    newElement.SerieSortType = element.SerieSortType;
                    newElement.TotalAggregateFunction = element.TotalAggregateFunction;
                    newElement.ShowTotal = element.ShowTotal;
                    newElement.SortOrder = element.SortOrder;
                    newElement.XAxisType = element.XAxisType;
                    newElement.YAxisType = element.YAxisType;
                }

                MainForm.IsModified = true;
                MainForm.CannotRenderAnymore();
                PanelsToElements();
                panel.RedrawPanel();
                button.Focus();
            }
        }

        private void elementTreeView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Button)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void elementTreeView_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Button)))
            {
                removeElementFromPanel((Button)e.Data.GetData(typeof(Button)), false);
            }
            else
            {
                RestrictionGrid.SelectedObject = null;
            }
            Commit();
        }

        private void elementTreeView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Button)) || e.Data.GetDataPresent(typeof(TreeNode)) || e.Data.GetDataPresent(typeof(String)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        public void ReinitSource()
        {
            initNoSQL();
            initTreeView();
            restrictionsPanel.ModelToRestrictionText();
            aggregateRestrictionsPanel.ModelToRestrictionText();
            ElementsToPanels();
        }

        void Grid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (s == ModelGrid)
            {
                TreeViewEditorHelper.CheckPropertyValue(Model, e);
                if (e.ChangedItem.PropertyDescriptor.Name == "SourceGUID")
                {
                    if (Model.SourceGUID == e.OldValue.ToString()) return;
                    DialogResult result = DialogResult.Yes;
                    if (Model.Elements.Count > 0 || Model.Restrictions.Count > 0 || Model.AggregateRestrictions.Count > 0)
                    {
                        result = MessageBox.Show("All the elements and restrictions defined will be lost. Do you want to continue ?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    }

                    if (result == DialogResult.Yes)
                    {
                        initNoSQL();
                        if (Model.IsSQLModel) Model.RefreshMetaTable(true);

                        initTreeView();
                        Model.Elements.Clear();
                        Model.Restrictions.Clear();
                        Model.InitEditor();
                        Model.Restriction = "";
                        Model.AggregateRestrictions.Clear();
                        Model.AggregateRestriction = "";                    
                        restrictionsPanel.ModelToRestrictionText();
                        aggregateRestrictionsPanel.ModelToRestrictionText();
                        ElementsToPanels();
                    }
                    else
                    {
                        Model.SourceGUID = e.OldValue.ToString();
                        return;
                    }
                }
            }
            else if (s == RestrictionGrid)
            {
                //Redraw restriction text
                if (((ReportRestriction)RestrictionGrid.SelectedObject).PivotPosition == PivotPosition.Data) aggregateRestrictionsPanel.UpdateRestrictionText();
                else restrictionsPanel.UpdateRestrictionText();

            }
            else if (s == ElementGrid)
            {
                //Redraw current buttons
                redrawButtons();
            }

            string propertyName = e.ChangedItem.PropertyDescriptor.Name;
            //Disable rendering, except for display properties...
            if (propertyName != "CellCss" && propertyName != "DisplayNameEl" && propertyName != "Format" && propertyName != "NumericStandardFormat" && propertyName != "DateTimeStandardFormat" && propertyName != "DateTimeStandardFormatRe" && propertyName != "NumericStandardFormatRe" && propertyName != "FormatRe" && propertyName != "OperatorLabel")
            {
                MainForm.CannotRenderAnymore();
            }

            MainForm.IsModified = true;

        }

        void RestrictionGrid_SelectedObjectsChanged(object sender, EventArgs e)
        {
            if (RestrictionGrid.SelectedObject != null)
            {
                //Avoid to have 2 selections on the 2 panels
                restrictionsPanel.ClearSelection();
                aggregateRestrictionsPanel.ClearSelection();
            }
        }

        private void elementTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right) elementTreeView.SelectedNode = e.Node;
        }

        void removeElementFromPanel(Button elementButton, bool all)
        {
            ElementPanel panel = (ElementPanel)elementButton.Parent;
            if (all) panel.Controls.Clear();
            else panel.Controls.Remove(elementButton);
            MainForm.CannotRenderAnymore();
            PanelsToElements();
            SelectedButton = null;
            ElementGrid.SelectedObject = null;
            panel.RedrawPanel();
            MainForm.IsModified = true;
        }

        void copyElementFromPanel(Button elementButton)
        {
            ReportElement element = elementButton.Tag as ReportElement;
            if (element != null)
            {
                ElementPanel panel = (ElementPanel)elementButton.Parent;
                ReportElement element2 = (ReportElement)Helper.Clone(element);
                Model.Elements.Add(element2);
                Model.InitReferences();
                element2.DisplayName = element2.DisplayNameEl + " - Copy";
                ElementsToPanels();
                Button button = (Button)panel.Controls[panel.Controls.Count - 1];
                MainForm.CannotRenderAnymore();
                btn_MouseDown(button, null);
                MainForm.IsModified = true;
            }
        }

    }
}
