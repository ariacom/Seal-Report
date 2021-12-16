//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Windows.Forms;
using Seal.Model;
using System.Data.OleDb;
using System.Data;
using System.Drawing;
using Seal.Helpers;
using System.Collections;
using System.Data.Common;
using System.Data.Odbc;
using MongoDB.Driver;

namespace Seal.Forms
{
    public interface ITreeSort
    {
        int GetSort();
    }

    public class SourceFolder : ITreeSort { 
        public int GetSort() { return 1; }
        public static SourceFolder Instance = new SourceFolder();
    }
    public class TasksFolder : ITreeSort { 
        public int GetSort() { return 2; }
        public static TasksFolder Instance = new TasksFolder();
    }
    public class ModelFolder : ITreeSort { 
        public int GetSort() { return 3; }
        public static ModelFolder Instance = new ModelFolder();
    }
    public class ViewFolder : ITreeSort { 
        public int GetSort() { return 4; }
        public static ViewFolder Instance = new ViewFolder();
    }
    public class OutputFolder : ITreeSort { 
        public int GetSort() { return 5; }
        public static OutputFolder Instance = new OutputFolder();
    }
    public class ScheduleFolder : ITreeSort { 
        public int GetSort() { return 6; }
        public static ScheduleFolder Instance = new ScheduleFolder();
    }

    public class ConnectionFolder : ITreeSort { 
        public int GetSort() { return 0; }
        public static ConnectionFolder Instance = new ConnectionFolder();
    }
    public class TableFolder : ITreeSort { 
        public int GetSort() { return 1; }
        public static TableFolder Instance = new TableFolder();
    }
    public class TableLinkFolder {
        public static TableLinkFolder Instance = new TableLinkFolder();
    };

    public class JoinFolder : ITreeSort { 
        public int GetSort() { return 2; }
        public static JoinFolder Instance = new JoinFolder();
    }
    public class EnumFolder : ITreeSort { 
        public int GetSort() { return 3; }
        public static EnumFolder Instance = new EnumFolder();
    }


    // Create a node sorter that implements the IComparer interface. 
    public class NodeSorter : IComparer
    {
        // Compare the length of the strings, or the strings 
        // themselves, if they are the same length. 
        public int Compare(object x, object y)
        {
            TreeNode tx = x as TreeNode;
            TreeNode ty = y as TreeNode;

            //Category folder bottom
            if (tx.Tag is CategoryFolder && ty.Tag is CategoryFolder) return (string.Compare(((CategoryFolder)tx.Tag).Path, ((CategoryFolder)ty.Tag).Path));
            if (tx.Tag is CategoryFolder) return 1;
            if (ty.Tag is CategoryFolder) return -1;
            if (tx.Tag is TableLinkFolder) return 1;
            if (ty.Tag is TableLinkFolder) return -1;

            if (tx.Tag is ITreeSort && ty.Tag is ITreeSort)
            {
                ITreeSort viewx = tx.Tag as ITreeSort;
                ITreeSort viewy = ty.Tag as ITreeSort;
                if (viewx.GetSort() == viewy.GetSort()) return string.Compare(tx.Text, ty.Text);
                if (viewx.GetSort() > viewy.GetSort()) return 1;
                return -1;
            }
            return string.Compare(tx.Text, ty.Text);
        }
    }


    public class TreeViewEditorHelper
    {
        public IEntityHandler entityHandler;
        public ContextMenuStrip treeContextMenuStrip;
        public TreeView mainTreeView;
        public ToolStripMenuItem addToolStripMenuItem;
        public ToolStripMenuItem removeToolStripMenuItem;
        public ToolStripMenuItem addFromToolStripMenuItem;
        public ToolStripMenuItem removeRootToolStripMenuItem;
        public ToolStripMenuItem copyToolStripMenuItem;
        public ToolStripMenuItem sortColumnAlphaOrderToolStripMenuItem;
        public ToolStripMenuItem sortColumnSQLOrderToolStripMenuItem;
        public bool ForReport = false;
        public Report Report = null;

        public void AfterSelect(object sender, TreeViewEventArgs e)
        {
            //Expand by default
            if (mainTreeView.SelectedNode != null && !mainTreeView.SelectedNode.IsExpanded && mainTreeView.SelectedNode.Tag is MetaSource)
            {
                MetaSource metaSource = mainTreeView.SelectedNode.Tag as MetaSource;
                if (metaSource != null) mainTreeView.SelectedNode.Expand();
            }
        }

        public MetaSource GetSource(TreeNode node)
        {
            MetaSource result;
            TreeNode currentNode = node;
            while (currentNode != null && !(currentNode.Tag is MetaSource))
            {
                currentNode = currentNode.Parent;
            }
            result = (MetaSource)(currentNode != null ? currentNode.Tag : null);
            return result;
        }


        public void addSource(TreeNodeCollection nodes, MetaSource source, int noSQLIndex)
        {
            int index = source.IsNoSQL ? noSQLIndex : 0;
            TreeNode mainTN = new TreeNode() { Tag = source, Text = source.Name, ImageIndex = index, SelectedImageIndex = index };
            nodes.Add(mainTN);

            TreeNode sourceConnectionTN = new TreeNode("Connections") { Tag = ConnectionFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
            mainTN.Nodes.Add(sourceConnectionTN);
            foreach (var item in source.Connections.OrderByDescending(i => i.IsEditable).ThenBy(i => i.Name))
            {
                TreeNode tn = new TreeNode(item.Name + (!item.IsEditable ? " (Repository)" : "")) { Tag = item, ImageIndex = 1, SelectedImageIndex = 1 };
                sourceConnectionTN.Nodes.Add(tn);
            }
            if (!ForReport) sourceConnectionTN.ExpandAll();

            TreeNode sourceTableTN = new TreeNode("Tables") { Tag = TableFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
            mainTN.Nodes.Add(sourceTableTN);
            foreach (var table in source.MetaData.Tables.OrderByDescending(i => i.IsEditable).ThenBy(i => i.AliasName))
            {
                TreeNode tableTN = new TreeNode(table.AliasName + (!table.IsEditable ? " (Repository)" : "")) { Tag = table, ImageIndex = 4, SelectedImageIndex = 4 };
                sourceTableTN.Nodes.Add(tableTN);

                //Add elements
                foreach (var column in table.Columns.OrderBy(i => i.DisplayOrder))
                {
                    TreeNode columnTN = new TreeNode(column.DisplayName) { Tag = column, ImageIndex = 7, SelectedImageIndex = 7 };
                    tableTN.Nodes.Add(columnTN);
                }
            }
            sourceTableTN.Expand();

            //Columns by category
            TreeNode categoryTN = new TreeNode("Columns by categories") { Tag = CategoryFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
            sourceTableTN.Nodes.Add(categoryTN);
            TreeViewHelper.InitCategoryTreeNode(categoryTN.Nodes, source.MetaData.Tables);
            categoryTN.Expand();

            if (source.IsNoSQL)
            {
                //Table links
                TreeNode tableLinksTN = new TreeNode("Table Links") { Tag = TableLinkFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
                sourceTableTN.Nodes.Add(tableLinksTN);
                TreeViewHelper.InitTablesLinksTreeNode(tableLinksTN.Nodes, source.MetaData.TableLinks);
            }

            //Joins
            TreeNode sourceJoinTN = new TreeNode("Joins") { Tag = JoinFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
            mainTN.Nodes.Add(sourceJoinTN);
            foreach (var item in source.MetaData.Joins.OrderByDescending(i => i.IsEditable).ThenBy(i => i.Name))
            {
                TreeNode tn = new TreeNode(item.Name + (!item.IsEditable ? " (Repository)" : "")) { Tag = item, ImageIndex = 5, SelectedImageIndex = 5 };
                sourceJoinTN.Nodes.Add(tn);
            }
            if (!ForReport) sourceJoinTN.ExpandAll();

            TreeNode sourceEnumTN = new TreeNode("Enumerated Lists") { Tag = EnumFolder.Instance, ImageIndex = 2, SelectedImageIndex = 2 };
            mainTN.Nodes.Add(sourceEnumTN);
            foreach (var item in source.MetaData.Enums.OrderByDescending(i => i.IsEditable).ThenBy(i => i.Name))
            {
                TreeNode tn = new TreeNode(item.Name + (!item.IsEditable ? " (Repository)" : "")) { Tag = item, ImageIndex = 6, SelectedImageIndex = 6 };
                sourceEnumTN.Nodes.Add(tn);
            }
            if (!ForReport) sourceEnumTN.ExpandAll();

            mainTreeView.TreeViewNodeSorter = new NodeSorter();
        }

        public void sortColumns_Click(object sender, EventArgs e, bool byPosition)
        {
            if (mainTreeView.SelectedNode == null) return;
            if (mainTreeView.SelectedNode.Tag is MetaTable)
            {
                MetaTable table = mainTreeView.SelectedNode.Tag as MetaTable;
                table.SortColumns(byPosition);
            }
            if (mainTreeView.SelectedNode.Tag is CategoryFolder)
            {
                CategoryFolder folder = mainTreeView.SelectedNode.Tag as CategoryFolder;
                List<MetaColumn> cols = new List<MetaColumn>();
                List<string> colNames = new List<string>();
                foreach (TreeNode child in mainTreeView.SelectedNode.Nodes)
                {
                    if (child.Tag is MetaColumn)
                    {
                        cols.Add(child.Tag as MetaColumn);
                    }
                }
                int position = 0;
                foreach (var col in cols.OrderBy(i => i.DisplayName))
                {
                    col.DisplayOrder = position++;
                }

                folder.SetInformation("Columns have been sorted by Name");
            }
        }

        public object addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mainTreeView.SelectedNode == null) return null;
            object entity = mainTreeView.SelectedNode.Tag;
            object newEntity = null;
            MetaSource source = GetSource(mainTreeView.SelectedNode);

            if (entity is ConnectionFolder && source != null)
            {
                newEntity = source.AddConnection();
            }
            else if (entity is TableFolder && source != null)
            {
                newEntity = source.AddTable(ForReport);
                var item = sender as ToolStripMenuItem;
                if (item != null && item.Tag is MetaTableTemplate)
                {
                    //NoSQL from template
                    ((MetaTableTemplate)item.Tag).ParseConfiguration((MetaTable)newEntity);
                    ((MetaTable)newEntity).TemplateName = ((MetaTableTemplate)item.Tag).Name;
                    ((MetaTable)newEntity).InitParameters();
                }
            }
            else if (entity is JoinFolder && source != null)
            {
                newEntity = source.AddJoin();
            }
            else if (entity is MetaTable && source != null)
            {
                newEntity = source.AddColumn((MetaTable)entity);
            }
            else if (entity is EnumFolder && source != null)
            {
                newEntity = source.AddEnum();
            }

            return newEntity;
        }

        public object copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mainTreeView.SelectedNode == null) return null;
            object entity = mainTreeView.SelectedNode.Tag;
            object newEntity = null;
            MetaSource source = GetSource(mainTreeView.SelectedNode);

            if (entity is MetaConnection && source != null)
            {
                newEntity = Helper.Clone(entity);
                source.Connections.Add((MetaConnection)newEntity);
                source.InitReferences(source.Repository);
                ((RootComponent)newEntity).GUID = Guid.NewGuid().ToString();
                ((RootComponent)newEntity).Name = Helper.GetUniqueName(((RootComponent)entity).Name + " - Copy", (from i in source.Connections select i.Name).ToList());
            }
            else if (entity is MetaTable && source != null)
            {
                MetaTable table = entity as MetaTable;
                newEntity = Helper.Clone(entity);
                source.MetaData.Tables.Add((MetaTable)newEntity);
                source.InitReferences(source.Repository);
                ((RootComponent)newEntity).GUID = Guid.NewGuid().ToString();
                //Set a table alias
                string oldName = !string.IsNullOrEmpty(table.Alias) ? table.Alias : table.Name;
                string newName = oldName + "Copy";
                ((MetaTable)newEntity).Alias = newName;
                if (!table.IsSQL) ((MetaTable)newEntity).Name = newName;
                //Change the table name in the columns 
                changeTableColumnNames((MetaTable)newEntity, oldName, newName);
                foreach (MetaColumn col in ((MetaTable)newEntity).Columns)
                {
                    col.GUID = Guid.NewGuid().ToString();
                    col.Category = col.Category + " - Copy";
                }
            }
            else if (entity is MetaColumn && source != null)
            {
                newEntity = Helper.Clone(entity);
                ((MetaColumn)entity).MetaTable.Columns.Add((MetaColumn)newEntity);
                source.InitReferences(source.Repository);
                ((RootComponent)newEntity).GUID = Guid.NewGuid().ToString();
                ((MetaColumn)newEntity).DisplayName = ((MetaColumn)entity).DisplayName + " - Copy";
            }
            else if (entity is MetaJoin && source != null)
            {
                newEntity = Helper.Clone(entity);
                source.MetaData.Joins.Add((MetaJoin)newEntity);
                source.InitReferences(source.Repository);
                ((RootComponent)newEntity).GUID = Guid.NewGuid().ToString();
                ((RootComponent)newEntity).Name = Helper.GetUniqueName(((RootComponent)entity).Name + " - Copy", (from i in source.MetaData.Joins select i.Name).ToList());
            }
            else if (entity is MetaEnum && source != null)
            {
                newEntity = Helper.Clone(entity);
                source.MetaData.Enums.Add((MetaEnum)newEntity);
                source.InitReferences(source.Repository);
                ((RootComponent)newEntity).GUID = Guid.NewGuid().ToString();
                ((RootComponent)newEntity).Name = Helper.GetUniqueName(((RootComponent)entity).Name + " - Copy", (from i in source.MetaData.Enums select i.Name).ToList());
            }
            return newEntity;
        }

        public object removeRootToolStripMenuItem_Click(object sender, EventArgs e)
        {
            object newEntity;
            if (mainTreeView.SelectedNode == null) return null;

            var nodes = mainTreeView.SelectedNode.Parent.Nodes;
            var index = nodes.IndexOf(mainTreeView.SelectedNode) + 1;
            if (nodes.Count > index) newEntity = nodes[index].Tag;
            else if (nodes.Count == index && nodes.Count > 1) newEntity = nodes[index - 2].Tag;
            else newEntity = mainTreeView.SelectedNode.Parent.Tag;

            object entity = mainTreeView.SelectedNode.Tag;
            MetaSource source = GetSource(mainTreeView.SelectedNode);

            if (entity is MetaConnection && source != null)
            {
                source.RemoveConnection((MetaConnection)entity);
            }
            else if (entity is MetaTable && source != null)
            {
                source.RemoveTable((MetaTable)entity);
            }
            else if (entity is MetaTableLink && source != null)
            {
                source.RemoveTableLink((MetaTableLink)entity);
            }
            else if (entity is MetaColumn && source != null)
            {
                ((MetaColumn)entity).MetaTable.Columns.Remove((MetaColumn)entity);
            }
            else if (entity is MetaJoin && source != null)
            {
                source.RemoveJoin((MetaJoin)entity);
            }
            else if (entity is MetaEnum && source != null)
            {
                source.RemoveEnum((MetaEnum)entity);
            }
            return newEntity;
        }

        public IList getRemoveSource(ref string displayName)
        {
            IList selectSource = null;

            object entity = mainTreeView.SelectedNode.Tag;
            MetaSource source = GetSource(mainTreeView.SelectedNode);
            ReportView parentView = null;

            if (entity is SourceFolder && Report != null)
            {
                selectSource = Report.Sources.OrderBy(i => i.Name).ToList();
                displayName = "Name";
            }
            else if (entity is ConnectionFolder && source != null)
            {
                selectSource = source.Connections.Where(i => i.IsEditable).OrderBy(i => i.Name).ToList();
                displayName = "Name";
            }
            else if (entity is TableFolder && source != null)
            {
                selectSource = source.MetaData.Tables.Where(i => i.IsEditable).OrderBy(i => i.AliasName).ToList();
                displayName = "DisplayName";
            }
            else if (entity is TableLinkFolder && source != null)
            {
                selectSource = source.MetaData.TableLinks.Where(i => i.IsEditable).OrderBy(i => i.DisplayName).ToList();
                displayName = "DisplayName";
            }
            else if (entity is JoinFolder && source != null)
            {
                selectSource = source.MetaData.Joins.Where(i => i.IsEditable).OrderBy(i => i.Name).ToList();
                displayName = "Name";
            }
            else if (entity is EnumFolder && source != null)
            {
                selectSource = source.MetaData.Enums.Where(i => i.IsEditable).OrderBy(i => i.Name).ToList();
                displayName = "Name";
            }
            else if (entity is MetaTable && source != null)
            {
                selectSource = ((MetaTable)entity).Columns.OrderBy(i => i.DisplayName2).ToList();
                displayName = "DisplayName2";
            }
            else if (entity is ModelFolder)
            {
                selectSource = Report.Models.OrderBy(i => i.Name).ToList();
                displayName = "Name";
            }
            else if (entity is ViewFolder)
            {
                selectSource = Report.Views.OrderBy(i => i.Name).ToList();
                displayName = "Name";
            }
            else if (entity is ReportView)
            {
                parentView = (ReportView)entity;
            }
            else if (entity is TasksFolder)
            {
                selectSource = Report.Tasks.OrderBy(i => i.SortOrder).ToList();
                displayName = "Name";
            }
            else if (entity is OutputFolder)
            {
                selectSource = Report.Outputs.OrderBy(i => i.Name).ToList();
                displayName = "Name";
            }
            else if (entity is ScheduleFolder)
            {
                selectSource = Report.Schedules.OrderBy(i => i.Name).ToList();
                displayName = "Name";
            }

            if (parentView != null)
            {
                selectSource = parentView.Views.OrderBy(i => i.SortOrder).ToList();
                displayName = "Name";
            }

            return selectSource;
        }


        public bool removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool isModified = false;
            string displayName = "";
            IList selectSource = getRemoveSource(ref displayName);

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                if (selectSource != null)
                {
                    MultipleSelectForm frm = new MultipleSelectForm("Please select objects to remove", selectSource, displayName);

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        object entity = mainTreeView.SelectedNode.Tag;
                        MetaSource source = GetSource(mainTreeView.SelectedNode);

                        Cursor.Current = Cursors.WaitCursor;
                        isModified = true;
                        List<ReportSource> sources = new List<ReportSource>();

                        foreach (var item in frm.CheckedItems)
                        {
                            if (item is ReportSource)
                            {
                                sources.Add((ReportSource)item);
                            }
                            else if (item is MetaConnection)
                            {
                                source.RemoveConnection((MetaConnection)item);
                            }
                            else if (item is MetaTable)
                            {
                                source.RemoveTable((MetaTable)item);
                            }
                            else if (item is MetaTableLink)
                            {
                                source.RemoveTableLink((MetaTableLink)item);
                            }
                            else if (item is MetaJoin)
                            {
                                source.RemoveJoin((MetaJoin)item);
                            }
                            else if (item is MetaColumn)
                            {
                                ((MetaTable)entity).Columns.Remove((MetaColumn)item);
                            }
                            else if (item is MetaEnum)
                            {
                                source.RemoveEnum((MetaEnum)item);
                            }
                            else if (item is ReportModel)
                            {
                                Report.RemoveModel((ReportModel)item);
                            }
                            else if (entity is ViewFolder && item is ReportView)
                            {
                                Report.RemoveView(null, (ReportView)item);
                            }
                            else if (item is ReportView)
                            {
                                Report.RemoveView(((ReportView)entity), (ReportView)item);
                            }
                            else if (item is ReportTask)
                            {
                                Report.RemoveTask((ReportTask)item);
                            }
                            else if (item is ReportOutput)
                            {
                                Report.RemoveOutput((ReportOutput)item);
                            }
                            else if (item is ReportSchedule)
                            {
                                Report.RemoveSchedule((ReportSchedule)item);
                            }
                        }

                        foreach (var reportSource in sources.OrderBy(i => !i.IsNoSQL))
                        {
                            Report.RemoveSource(reportSource);
                        }
                    }
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

            return isModified;
        }



        public void initTreeContextMenuStrip(EventHandler addHandler)
        {
            string entityName = null, copyEntityName = null;
            object entity = mainTreeView.SelectedNode.Tag;
            var source = GetSource(mainTreeView.SelectedNode);

            if (entity is ConnectionFolder) entityName = "Connection";
            else if (entity is TableFolder)
            {
                entityName = "Table";
            }
            else if (entity is JoinFolder)
            {
                entityName = "Join";
            }
            else if (entity is EnumFolder)
            {
                entityName = "Enum";
            }
            else if (entity is MetaConnection)
            {
                copyEntityName = ((MetaConnection)entity).Name;
            }
            else if (entity is MetaTable)
            {
                if (((MetaTable)entity).IsSubTable)
                {
                    return;
                }

                entityName = "Column";
                copyEntityName = ((MetaTable)entity).DisplayName;
            }
            else if (entity is MetaColumn)
            {
                copyEntityName = ((MetaColumn)entity).Name;
            }
            else if (entity is MetaJoin)
            {
                copyEntityName = ((MetaJoin)entity).Name;
            }
            else if (entity is MetaEnum)
            {
                copyEntityName = ((MetaEnum)entity).Name;
            }

            treeContextMenuStrip.Items.Clear();

            if (!string.IsNullOrEmpty(entityName))
            {
                if (entity is TableFolder && source != null && source.IsNoSQL)
                {
                    //Add from No SQL Templates
                    foreach (var template in RepositoryServer.TableTemplates)
                    {
                        ToolStripMenuItem ts = new ToolStripMenuItem();
                        ts.Click += addHandler;
                        ts.Tag = template;
                        ts.Text = "Add a " + template.Name + " Table";
                        treeContextMenuStrip.Items.Add(ts);
                    }
                    treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                    addFromToolStripMenuItem.Text = string.Format("Add {0}s from Mongo DB Catalog...", entityName);
                    treeContextMenuStrip.Items.Add(addFromToolStripMenuItem);
                }
                else
                {
                    addToolStripMenuItem.Text = string.Format("Add {0}", entityName);
                    treeContextMenuStrip.Items.Add(addToolStripMenuItem);
                }

                treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                removeToolStripMenuItem.Text = string.Format("Remove {0}s...", entityName);
                treeContextMenuStrip.Items.Add(removeToolStripMenuItem);

                string displayName = "";
                IList selectSource = getRemoveSource(ref displayName);
                removeToolStripMenuItem.Enabled = (selectSource.Count > 0);

                if (source != null && !source.IsNoSQL && (entity is TableFolder || entity is JoinFolder || entity is MetaTable))
                {
                    treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                    addFromToolStripMenuItem.Text = string.Format("Add {0}s from Catalog...", entityName);
                    treeContextMenuStrip.Items.Add(addFromToolStripMenuItem);
                }
            }

            //Disable menu for repository tables in report designer
            if (entity is MetaTable && !((MetaTable)entity).IsEditable) treeContextMenuStrip.Items.Clear();

            //Copy....
            if (!string.IsNullOrEmpty(copyEntityName))
            {
                if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                copyToolStripMenuItem.ShortcutKeys = (Keys.Control | Keys.C);
                copyToolStripMenuItem.Text = string.Format("Copy {0}", Helper.QuoteSingle(copyEntityName));
                treeContextMenuStrip.Items.Add(copyToolStripMenuItem);
            }

            //Remove
            if (entity is MetaTableLink)
            {
                copyEntityName = ((MetaTableLink)entity).DisplayName;
            }

            //Disable remove for repository elements in Report Designer
            if (entity is MetaTable && !((MetaTable)entity).IsEditable) copyEntityName = null;
            if (entity is MetaTableLink && !((MetaTableLink)entity).IsEditable) copyEntityName = null;
            if (entity is MetaJoin && !((MetaJoin)entity).IsEditable) copyEntityName = null;
            if (entity is MetaEnum && !((MetaEnum)entity).IsEditable) copyEntityName = null;
            if (!string.IsNullOrEmpty(copyEntityName))
            {
                if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                removeRootToolStripMenuItem.Text = string.Format("Remove {0}", Helper.QuoteSingle(copyEntityName));
                removeRootToolStripMenuItem.ShortcutKeys = Keys.Delete;
                treeContextMenuStrip.Items.Add(removeRootToolStripMenuItem);
            }

            //Disable menu for repository columns in report designer
            if (entity is MetaColumn && !((MetaColumn)entity).MetaTable.IsEditable) treeContextMenuStrip.Items.Clear();


            //Special menus
            //Table Links
            if (entity is TableLinkFolder)
            {
                addFromToolStripMenuItem.Text = "Add Table Links from current Data Sources...";
                treeContextMenuStrip.Items.Add(addFromToolStripMenuItem);
                treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                removeToolStripMenuItem.Text = "Remove Table Links...";
                treeContextMenuStrip.Items.Add(removeToolStripMenuItem);
            }
            if (entity is MetaTable && ((MetaTable)entity).IsEditable)
            {
                if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                treeContextMenuStrip.Items.Add(sortColumnAlphaOrderToolStripMenuItem);
                treeContextMenuStrip.Items.Add(sortColumnSQLOrderToolStripMenuItem);
            }
            if (entity is MetaColumn&& ((MetaColumn)entity).MetaTable.IsEditable && mainTreeView.SelectedNode.Parent.Tag == ((MetaColumn)entity).MetaTable)
            {
                addMoveUpDown(entity);
            }

            if (entity is CategoryFolder && mainTreeView.SelectedNode.Parent != null && mainTreeView.SelectedNode.Parent.Tag is CategoryFolder)
            {
                bool canSort = true;
                foreach (TreeNode child in mainTreeView.SelectedNode.Nodes)
                {
                    if (child.Tag is MetaColumn && !((MetaColumn)child.Tag).MetaTable.IsEditable)
                    {
                        canSort = false;
                        break;
                    }
                }
                if (canSort)
                {
                    if (treeContextMenuStrip.Items.Count > 0) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
                    treeContextMenuStrip.Items.Add(sortColumnAlphaOrderToolStripMenuItem);
                }
            }
        }

        List<MetaJoin> GetJoins(DbConnection connection, MetaSource source)
        {
            List<MetaJoin> joins = new List<MetaJoin>();
            if (!(connection is OleDbConnection)) return joins;
            DataTable schemaTables = ((OleDbConnection)connection).GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, null);

            foreach (DataRow row in schemaTables.Rows)
            {
                string table1Name = source.GetTableName(row["PK_TABLE_NAME"].ToString());
                string table2Name = source.GetTableName(row["FK_TABLE_NAME"].ToString());
                MetaTable table1 = source.MetaData.Tables.FirstOrDefault(i => i.Name == source.GetTableName(table1Name));
                MetaTable table2 = source.MetaData.Tables.FirstOrDefault(i => i.Name == source.GetTableName(table2Name));

                if (table1 == null)
                {
                    string pkschema = "";
                    if (schemaTables.Columns.Contains("PK_TABLE_SCHEMA")) pkschema = row["PK_TABLE_SCHEMA"].ToString();
                    else if (schemaTables.Columns.Contains("PK_TABLE_SCHEM")) pkschema = row["PK_TABLE_SCHEM"].ToString();
                    if (!string.IsNullOrEmpty(pkschema)) table1 = source.MetaData.Tables.FirstOrDefault(i => i.Name == pkschema + "." + table1Name);
                }

                if (table2 == null)
                {
                    string fkschema = "";
                    if (schemaTables.Columns.Contains("FK_TABLE_SCHEMA")) fkschema = row["FK_TABLE_SCHEMA"].ToString();
                    else if (schemaTables.Columns.Contains("FK_TABLE_SCHEM")) fkschema = row["FK_TABLE_SCHEM"].ToString();
                    if (!string.IsNullOrEmpty(fkschema)) table2 = source.MetaData.Tables.FirstOrDefault(i => i.Name == fkschema + "." + table2Name);
                }

                if (table1 != null && table2 != null && table1.Name != table2.Name && !source.MetaData.Joins.Exists(i => i.LeftTableGUID == table1.GUID && i.RightTableGUID == table2.GUID))
                {
                    MetaJoin join = joins.FirstOrDefault(i => i.LeftTableGUID == table1.GUID && i.RightTableGUID == table2.GUID);
                    if (join == null)
                    {
                        join = MetaJoin.Create();
                        join.Name = table1.Name + " - " + table2.Name;
                        join.LeftTableGUID = table1.GUID;
                        join.RightTableGUID = table2.GUID;
                        join.Source = source;
                        join.IsBiDirectional = true;
                        joins.Add(join);
                    }

                    if (!string.IsNullOrEmpty(join.Clause)) join.Clause += " AND ";
                    join.Clause += string.Format("{0}.{1} = {2}.{3}\r\n", table1.Name, source.GetColumnName(row["PK_COLUMN_NAME"].ToString()), table2.Name, source.GetColumnName(row["FK_COLUMN_NAME"].ToString()));
                    join.JoinType = JoinType.Inner;
                }
            }
            return joins;
        }

        void addSchemaTables(DataTable schemaTables, List<MetaTable> tables, MetaSource source)
        {
            //Helper.DisplayDataTable(schemaTables);
            foreach (DataRow row in schemaTables.Rows)
            {
                //if (row["TABLE_TYPE"].ToString() == "SYSTEM TABLE" || row["TABLE_TYPE"].ToString() == "SYSTEM VIEW") continue;
                MetaTable table = MetaTable.Create();
                string schema = "";
                string catalog = "";
                if (schemaTables.Columns.Contains("TABLE_CATALOG")) catalog= row["TABLE_CATALOG"].ToString();
                if (schemaTables.Columns.Contains("TABLE_SCHEMA")) schema = row["TABLE_SCHEMA"].ToString();
                else if (schemaTables.Columns.Contains("TABLE_SCHEM")) schema = row["TABLE_SCHEM"].ToString();

                if (!string.IsNullOrEmpty(schema) || !string.IsNullOrEmpty(catalog))
                {
                    table.Name = catalog + "." + schema + "." + source.GetTableName(row["TABLE_NAME"].ToString());

                }
                else
                {
                    table.Name = source.GetTableName(row["TABLE_NAME"].ToString());
                }

                if (schemaTables.Columns.Contains("TABLE_TYPE")) table.Type = row["TABLE_TYPE"].ToString();
                table.Source = source;
                tables.Add(table);
            }
        }

        public bool addFromToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool isModified = false;
            object entity = mainTreeView.SelectedNode.Tag;
            MetaSource source = GetSource(mainTreeView.SelectedNode);
            if (source == null) return isModified;

            object selectSource = null;
            string name = "";
            List<CheckBox> options = new List<CheckBox>();
            CheckBox autoCreateColumns = new CheckBox() { Text = "Auto create table columns", Checked = true, AutoSize = true };
            CheckBox autoCreateJoins = new CheckBox() { Text = "Auto create joins", Checked = true, AutoSize = true };
            CheckBox useTableSchemaName = new CheckBox() { Text = "Use schema name", Checked = false, AutoSize = true };
            CheckBox useTableCatalogName = new CheckBox() { Text = "Use catalog name", Checked = false, AutoSize = true };
            CheckBox keepColumnNames = new CheckBox() { Text = "Keep column names", Checked = false, AutoSize = true };

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                if (entity is TableFolder)
                {
                    List<MetaTable> tables = new List<MetaTable>();
                    if (source.IsNoSQL)
                    {
                        if (source.Connection.ConnectionType != ConnectionType.MongoDB || string.IsNullOrEmpty(source.Connection.MongoDBConnectionString))
                        {
                            MessageBox.Show("Please configure a Mongo DB Connection first", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        MongoClient client = new MongoClient(source.Connection.FullConnectionString);
                        foreach (var dbName in client.ListDatabaseNames().ToList<string>().OrderBy(i => i))
                        {
                            var database = client.GetDatabase(dbName);
                            foreach (var collectionName in database.ListCollectionNames().ToList().OrderBy(i => i))
                            {
                                MetaTable table = MetaTable.Create();
                                table.Name = $"{dbName}.{collectionName}";
                                table.Source = source;
                                tables.Add(table);
                            }
                        }
                    }
                    else {
                        DbConnection connection = source.GetOpenConnection();
                        DataTable schemaTables = connection.GetSchema("Tables");
                        addSchemaTables(schemaTables, tables, source);
                        if (connection is OdbcConnection)
                        {
                            //Add views for odbc connections..
                            addSchemaTables(connection.GetSchema("Views"), tables, source);
                        }
                        options.Add(autoCreateJoins);
                    }
                    options.Add(autoCreateColumns);

                    selectSource = tables.OrderBy(i => i.AliasName).ToList();
                    name = "DisplayName";


                    if (tables.Count > 0)
                    {
                        if (source.IsSQL && tables[0].Name.Contains(".")) 
                        {
                            options.Add(useTableCatalogName);
                            options.Add(useTableSchemaName);
                        }
                        options.Add(keepColumnNames);
                    }
                }
                else if (entity is TableLinkFolder)
                {
                    List<MetaTable> links = new List<MetaTable>();
                    if (source.Report != null)
                    {
                        foreach (var newSource in source.Report.Sources.Where(i => i != source))
                        {
                            links.AddRange(newSource.MetaData.Tables.Where(i => !source.MetaData.Tables.Contains(i)));
                        }
                    }

                    foreach (var newSource in source.Repository.Sources.Where(i => i != source))
                    {
                        links.AddRange(newSource.MetaData.Tables.Where(i => !links.Contains(i) && !source.MetaData.Tables.Contains(i)));
                    }
                    selectSource = links.OrderBy(i => i.FullDisplayName).ToList();
                    name = "FullDisplayName";
                }
                else if (entity is MetaTable)
                {
                    options.Add(autoCreateJoins);
                    autoCreateJoins.Checked = false;

                    DbConnection connection = source.GetOpenConnection();
                    List<MetaColumn> columns = new List<MetaColumn>();
                    source.AddColumnsFromCatalog(columns, connection, ((MetaTable)entity));
                    selectSource = columns.OrderBy(i => i.Name).ToList();
                    name = "Name";
                }
                else if (entity is JoinFolder)
                {
                    DbConnection connection = source.GetOpenConnection();
                    List<MetaJoin> joins = GetJoins(connection, source);
                    if (joins.Count == 0)
                    {
                        MessageBox.Show(connection is OleDbConnection ? "All Joins have been defined for the existing tables" : "Joins cannot be read from the database for 'Microsoft OleDB provider for ODBC drivers'...\r\nIf possible, use another OleDB provider for your connection.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return isModified;
                    }
                    selectSource = joins.OrderBy(i => i.Name).ToList();
                    name = "Name";
                }

                if (source != null && selectSource != null)
                {
                    MultipleSelectForm frm = new MultipleSelectForm("Please select objects to add", selectSource, name);

                    int index = 10;
                    foreach (var checkbox in options)
                    {
                        checkbox.Location = new Point(index, 5);
                        frm.optionPanel.Controls.Add(checkbox);
                        index += checkbox.Width + 10;
                        frm.Width = Math.Max(index + 5, frm.Width);
                    }
                    frm.optionPanel.Visible = (options.Count > 0);

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        isModified = true;
                        foreach (var item in frm.CheckedItems)
                        {
                            if (entity is TableFolder && item is MetaTable)
                            {
                                MetaTable table = (MetaTable)item;
                                if (source.IsSQL && table.Name.Contains("."))
                                {
                                    string[] names = table.Name.Split('.');
                                    table.Name = names.Last();
                                    if (names.Length == 3) 
                                    {
                                        var finalName = "";
                                        if (useTableCatalogName.Checked) finalName += names[0] + ".";
                                        if (useTableSchemaName.Checked) finalName += names[1] + ".";
                                        table.Name = finalName + names[2];
                                    }
                                }
                                else if (source.IsNoSQL)
                                {
                                    //Mongo DB Table
                                    string[] names = table.Name.Split('.');
                                    table.Name = names.Last();
                                    table.TemplateName = "Mongo DB";
                                    table.DynamicColumns = true;
                                    _ = table.TableTemplate;
                                    var param = table.Parameters.FirstOrDefault(i => i.Name == MetaTable.ParameterNameMongoDatabase);
                                    if (param != null) param.Value = names.First();
                                    param = table.Parameters.FirstOrDefault(i => i.Name == MetaTable.ParameterNameMongoCollection);
                                    if (param != null) param.Value = names.Last();
                                }

                                if (source.MetaData.Tables.Exists(i => i.AliasName == table.AliasName))
                                {
                                    table.Alias = Helper.GetUniqueName(table.Name, (from i in source.MetaData.Tables select i.AliasName).ToList());
                                }

                                if (keepColumnNames.Checked)
                                {
                                    table.KeepColumnNames = true;
                                }

                                if (autoCreateColumns.Checked)
                                {
                                    if (source.IsSQL)
                                    {
                                        DbConnection connection = source.GetOpenConnection();
                                        source.AddColumnsFromCatalog(table.Columns, connection, table);
                                    }
                                    else
                                    {
                                        table.Refresh();
                                    }
                                }

                                source.MetaData.Tables.Add(table);
                            }
                            else if (entity is TableLinkFolder && item is MetaTable)
                            {
                                MetaTable table = (MetaTable)item;
                                var link = new MetaTableLink() { TableGUID = table.GUID, SourceGUID = table.Source.GUID, Source = table.Source };
                                source.MetaData.TableLinks.Add(link);

                                if (source.Report != null) source.Report.CheckLinkedTablesSources();
                            }
                            else if (item is MetaColumn)
                            {
                                MetaColumn column = (MetaColumn)item;
                                ((MetaTable)entity).Columns.Add(column);
                            }
                            else if (item is MetaJoin)
                            {
                                MetaJoin join = (MetaJoin)item;
                                join.Name = Helper.GetUniqueName(join.Name, (from i in source.MetaData.Joins select i.Name).ToList());
                                source.MetaData.Joins.Add(join);
                            }
                        }

                        if (autoCreateJoins.Checked && source.IsSQL)
                        {
                            DbConnection connection = source.GetOpenConnection();
                            foreach (var join in GetJoins(connection, source))
                            {
                                join.Name = Helper.GetUniqueName(join.Name, (from i in source.MetaData.Joins select i.Name).ToList());
                                source.MetaData.Joins.Add(join);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

            return isModified;
        }

        private void changeTableColumnNames(MetaTable table, string oldValue, string newValue)
        {
            if (!string.IsNullOrEmpty(newValue) && newValue != oldValue)
            {
                //Change the table name in the columns 
                foreach (var col in table.Columns)
                {
                    col.Name = col.Name.Replace(oldValue + ".", newValue + ".");
                    col.Name = col.Name.Replace(oldValue.ToLower() + ".", newValue + ".");
                }
            }
        }

        public static void CheckPropertyValue(object selectedEntity, PropertyValueChangedEventArgs e)
        {
            //bug 47, if double click in property editor, values are not translated by the converter...
            //no better way so far to disable double click...
            //-> regression gave bug 50...
            string propertyName = e.ChangedItem.PropertyDescriptor.Name;
            if (e.ChangedItem.Value == null) return;
            var newValue = e.ChangedItem.Value.ToString();
            if (string.IsNullOrEmpty(newValue)) return;

            if (selectedEntity is MetaSource && propertyName == "ConnectionGUID")
            {
                var entity = selectedEntity as MetaSource;
                if (e.OldValue != null && newValue != ReportSource.DefaultRepositoryConnectionGUID &&
                    newValue != ReportSource.DefaultReportConnectionGUID &&
                    !entity.Connections.Exists(i => i.GUID == newValue)) entity.ConnectionGUID = e.OldValue.ToString();
            }
            if (selectedEntity is ReportModel && propertyName == "ConnectionGUID")
            {
                var entity = selectedEntity as ReportModel;
                if (e.OldValue != null && newValue != ReportSource.DefaultRepositoryConnectionGUID &&
                    newValue != ReportSource.DefaultReportConnectionGUID &&
                    !entity.Source.Connections.Exists(i => i.GUID == newValue)) entity.ConnectionGUID = e.OldValue.ToString();
            }
            if (selectedEntity is ReportTask && propertyName == "ConnectionGUID")
            {
                var entity = selectedEntity as ReportTask;
                if (e.OldValue != null && newValue != ReportSource.DefaultRepositoryConnectionGUID &&
                    newValue != ReportSource.DefaultReportConnectionGUID &&
                    !entity.Source.Connections.Exists(i => i.GUID == newValue)) entity.ConnectionGUID = e.OldValue.ToString();
            }
            else if (selectedEntity is MetaColumn && propertyName == "EnumGUID")
            {
                var entity = selectedEntity as MetaColumn;
                if (e.OldValue != null && !entity.Source.MetaData.Enums.Exists(i => i.GUID == newValue)) entity.EnumGUID = e.OldValue.ToString();
            }
            else if (selectedEntity is ReportModel && propertyName == "SourceGUID")
            {
                var entity = selectedEntity as ReportModel;
                if (e.OldValue != null && !entity.Report.Sources.Exists(i => i.GUID == newValue)) entity.SourceGUID = e.OldValue.ToString();
            }
            else if (selectedEntity is ReportTask && propertyName == "SourceGUID")
            {
                var entity = selectedEntity as ReportTask;
                if (e.OldValue != null && !entity.Report.Sources.Exists(i => i.GUID == newValue)) entity.SourceGUID = e.OldValue.ToString();
            }
            else if (selectedEntity is ReportView && propertyName == "ModelGUID")
            {
                var entity = selectedEntity as ReportView;
                if (e.OldValue != null && !entity.Report.Models.Exists(i => i.GUID == newValue)) entity.ModelGUID = e.OldValue.ToString();
            }
            else if (selectedEntity is Report && propertyName == "ViewGUID")
            {
                var entity = selectedEntity as Report;
                if (e.OldValue != null && !entity.Views.Exists(i => i.GUID == newValue)) entity.ViewGUID = e.OldValue.ToString();
            }
            else if (selectedEntity is ReportOutput && propertyName == "ViewGUID")
            {
                var entity = selectedEntity as ReportOutput;
                if (e.OldValue != null && !entity.Report.Views.Exists(i => i.GUID == newValue)) entity.ViewGUID = e.OldValue.ToString();
            }
            else if (selectedEntity is MetaJoin && propertyName == "LeftTableGUID")
            {
                var entity = selectedEntity as MetaJoin;
                if (e.OldValue != null && !entity.Source.MetaData.AllTables.Exists(i => i.GUID == newValue)) entity.LeftTableGUID = e.OldValue.ToString();
            }
            else if (selectedEntity is MetaJoin && propertyName == "RightTableGUID")
            {
                var entity = selectedEntity as MetaJoin;
                if (e.OldValue != null && !entity.Source.MetaData.AllTables.Exists(i => i.GUID == newValue)) entity.RightTableGUID = e.OldValue.ToString();
            }
        }

        public bool mainPropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (mainTreeView.SelectedNode == null) return false;

            bool mustInit = false;
            object selectedEntity = mainTreeView.SelectedNode.Tag;
            CheckPropertyValue(selectedEntity, e);
            string propertyName = e.ChangedItem.PropertyDescriptor.Name;

            if (selectedEntity is ReportTask && propertyName == "Enabled")
            {
                ReportTask entity = (ReportTask)selectedEntity;
                mainTreeView.SelectedNode.ImageIndex = entity.Enabled ? 12 : 14;
                mainTreeView.SelectedNode.SelectedImageIndex = mainTreeView.SelectedNode.ImageIndex;
            }
            else if (selectedEntity is ReportView && (propertyName == "UseModelName" || propertyName == "ModelGUID"))
            {
                ReportView entity = (ReportView)selectedEntity;
                mainTreeView.SelectedNode.Text = entity.Name;
            }
            else
            {
                MetaSource source = GetSource(mainTreeView.SelectedNode);
                if (source == null) return mustInit;
                if (selectedEntity is ReportSource && propertyName == "ConnectionGUID")
                {
                    ((ReportSource)selectedEntity).RefreshEnumsOnDbConnection();
                }
                else if (selectedEntity is MetaConnection && propertyName == "Name")
                {
                    MetaConnection entity = (MetaConnection)selectedEntity;
                    entity.Name = Helper.GetUniqueName(entity.Name, (from i in source.Connections where i != entity select i.Name).ToList());
                    mainTreeView.SelectedNode.Text = entity.Name;
                }
                else if (selectedEntity is MetaTable)
                {
                    MetaTable table = (MetaTable)selectedEntity;
                    if (propertyName == "Name" && string.IsNullOrEmpty(table.Alias)) table.Name = Helper.GetUniqueName(table.Name, (from i in source.MetaData.Tables where i != table select i.AliasName).ToList());
                    if (propertyName == "Alias") table.Alias = Helper.GetUniqueName(table.Alias, (from i in source.MetaData.Tables where i != table select i.AliasName).ToList());
                    mainTreeView.SelectedNode.Text = table.AliasName;
                    if (table.DynamicColumns && ((table.IsSQL && (propertyName == "Name" || propertyName == "Sql")) || (!table.IsSQL && propertyName == "DefinitionScript")))
                    {
                        table.Refresh();
                        mustInit = true;
                    }

                    string newValue = null;
                    string oldValue = null;
                    if (e.OldValue != null)
                    {
                        oldValue = e.OldValue.ToString();
                        if (propertyName == "Name") newValue = table.Name;
                        else if (propertyName == "Alias") newValue = table.Alias;
                        if (!string.IsNullOrEmpty(newValue) && newValue != oldValue)
                        {
                            //Change the table name in the columns and the joins...
                            changeTableColumnNames(table, oldValue, newValue);
                            foreach (var join in source.MetaData.Joins.Where(i => i.LeftTableGUID == table.GUID || i.RightTableGUID == table.GUID))
                            {
                                join.Clause = join.Clause.Replace(oldValue + ".", newValue + ".");
                                join.Clause = join.Clause.Replace(oldValue.ToLower() + ".", newValue + ".");
                            }
                        }
                    }

                }
                else if (selectedEntity is MetaJoin && (propertyName == "Name" || propertyName == "LeftTableGUID" || propertyName == "RightTableGUID"))
                {
                    MetaJoin entity = (MetaJoin)selectedEntity;
                    entity.Name = Helper.GetUniqueName(entity.Name, (from i in source.MetaData.Joins where i != entity select i.Name).ToList());
                    mainTreeView.SelectedNode.Text = entity.Name;
                }
                else if (selectedEntity is MetaColumn)
                {
                    MetaColumn entity = (MetaColumn)selectedEntity;
                    if (propertyName == "DisplayName")
                    {
                        //MetaColumn can be edited in several nodes...
                        List<TreeNode> nodes = new List<TreeNode>();
                        TreeViewHelper.NodesFromEntity(mainTreeView.Nodes, selectedEntity, nodes);
                        foreach (var node in nodes) node.Text = entity.DisplayName;
                    }
                    else if (propertyName == "Category")
                    {
                        //Rebuild category nodes...
                        TreeNode rootNode = TreeViewHelper.GetRootCategoryNode(mainTreeView.SelectedNode);
                        TreeViewHelper.InitCategoryTreeNode(rootNode.Nodes, source.MetaData.Tables);
                        TreeViewHelper.SelectNode(mainTreeView, rootNode.Nodes, selectedEntity);
                    }
                    else if (propertyName == "DisplayOrder")
                    {
                    }
                }
                else if (selectedEntity is CategoryFolder)
                {
                    CategoryFolder entity = (CategoryFolder)selectedEntity;
                    if (propertyName == "Path")
                    {
                        int cnt = 0;
                        //Change all categories having this name
                        foreach (MetaTable table in source.MetaData.Tables.Where(i => i.IsEditable))
                        {
                            foreach (MetaColumn column in table.Columns)
                            {
                                if (column.Category.StartsWith(e.OldValue.ToString()))
                                {
                                    cnt++;
                                    column.Category = entity.Path + column.Category.Substring(e.OldValue.ToString().Length);
                                }
                            }
                        }
                        //Rebuild category nodes...
                        TreeNode rootNode = TreeViewHelper.GetRootCategoryNode(mainTreeView.SelectedNode);
                        TreeViewHelper.InitCategoryTreeNode(rootNode.Nodes, source.MetaData.Tables);
                        TreeNode newFolderNode = TreeViewHelper.SelectCategoryNode(mainTreeView, rootNode.Nodes, entity.Path);
                        if (newFolderNode == null) newFolderNode = rootNode;
                        CategoryFolder newFolder = (CategoryFolder)newFolderNode.Tag;
                        newFolder.Information = string.Format("{0} column categories have been updated.", cnt);
                        if (ForReport) newFolder.Information += " Note that columns defined in Repository Sources cannot be updated from the Report Designer.";
                        mainTreeView.SelectedNode = null;
                        mainTreeView.SelectedNode = newFolderNode;
                    }
                }
                else if (selectedEntity is MetaEnum && propertyName == "Name")
                {
                    MetaEnum entity = (MetaEnum)selectedEntity;
                    entity.Name = Helper.GetUniqueName(entity.Name, (from i in source.MetaData.Enums where i != entity select i.Name).ToList());
                    mainTreeView.SelectedNode.Text = entity.Name;
                }
            }

            TreeNode previous = mainTreeView.SelectedNode;
            try
            {
                mainTreeView.BeginUpdate();
                mainTreeView.Sort();
                mainTreeView.SelectedNode = previous;
            }
            finally
            {
                mainTreeView.EndUpdate();
            }
            return mustInit;
        }

        public void dynamicColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            object entity = mainTreeView.SelectedNode.Tag;
            MetaSource source = GetSource(mainTreeView.SelectedNode);
            if (source == null) return;

            MetaTable table = entity as MetaTable;
            if (table != null && table.DynamicColumns)
            {
                table.Refresh();
            }
        }

        public void addMoveUpDown(object tag)
        {
            if (mainTreeView.SelectedNode.Parent.Nodes.Count == 1) return;

            ToolStripMenuItem tsFirst = null, tsUp = null, tsDown = null, tsLast = null;
            if (mainTreeView.SelectedNode.Parent.Nodes[0] != mainTreeView.SelectedNode)
            {
                tsFirst = new ToolStripMenuItem() { Text = "Move first", Tag = tag, ShortcutKeys = (Keys.Control | Keys.PageUp) };
                tsFirst.Click += new System.EventHandler(this.moveUpDownToolStripMenuItem_Click);
                tsUp = new ToolStripMenuItem() { Text = "Move up", Tag = tag, ShortcutKeys = (Keys.Control | Keys.Up) };
                tsUp.Click += new System.EventHandler(this.moveUpDownToolStripMenuItem_Click);
            }
            if (mainTreeView.SelectedNode.Parent.Nodes[mainTreeView.SelectedNode.Parent.Nodes.Count - 1] != mainTreeView.SelectedNode)
            {
                tsDown = new ToolStripMenuItem() { Text = "Move down", Tag = tag, ShortcutKeys = (Keys.Control | Keys.Down) };
                tsDown.Click += new System.EventHandler(this.moveUpDownToolStripMenuItem_Click);
                tsLast = new ToolStripMenuItem() { Text = "Move last", Tag = tag, ShortcutKeys = (Keys.Control | Keys.PageDown) };
                tsLast.Click += new System.EventHandler(this.moveUpDownToolStripMenuItem_Click);
            }


            if (treeContextMenuStrip.Items.Count > 0 && (tsUp != null || tsDown != null)) treeContextMenuStrip.Items.Add(new ToolStripSeparator());
            if (tsFirst != null) treeContextMenuStrip.Items.Add(tsFirst);
            if (tsUp != null) treeContextMenuStrip.Items.Add(tsUp);
            if (tsDown != null) treeContextMenuStrip.Items.Add(tsDown);
            if (tsLast != null) treeContextMenuStrip.Items.Add(tsLast);
        }

        private void moveUpDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var increment = 0;
            var text = ((ToolStripMenuItem)sender).Text.ToLower();
            if (text.Contains("first")) increment = -10000;
            else if (text.Contains("last")) increment = 10000;
            else if (text.Contains("down")) increment = 15;
            else increment = -15;
            var selectedEntity = mainTreeView.SelectedNode != null ? mainTreeView.SelectedNode.Tag : null;

            if (selectedEntity is ReportView)
            {
                var viewToMove = selectedEntity as ReportView;
                //move the position
                List<ReportView> views = (mainTreeView.SelectedNode.Parent.Tag is ReportView) ? ((ReportView)mainTreeView.SelectedNode.Parent.Tag).Views : Report.Views;
                foreach (var view in views) view.SortOrder = 10 * view.SortOrder;
                viewToMove.SortOrder += increment;
                int index = 1;
                foreach (var view in views.OrderBy(i => i.SortOrder)) view.SortOrder = index++;
                entityHandler.SetModified();
                mainTreeView.Sort();
                TreeViewHelper.SelectNode(mainTreeView, mainTreeView.Nodes, viewToMove);
            }
            else if (selectedEntity is ReportTask)
            {
                var taskToMove = selectedEntity as ReportTask;
                //move the position
                List<ReportTask> tasks = Report.Tasks;
                foreach (var task in tasks) task.SortOrder = 10 * task.SortOrder;
                taskToMove.SortOrder += increment;
                int index = 1;
                foreach (var task in tasks.OrderBy(i => i.SortOrder)) task.SortOrder = index++;
                entityHandler.SetModified();
                mainTreeView.Sort();
                TreeViewHelper.SelectNode(mainTreeView, mainTreeView.Nodes, taskToMove);
            }
            else if (selectedEntity is MetaColumn)
            {
                var colToMove = selectedEntity as MetaColumn;
                if (!colToMove.MetaTable.IsEditable) return;

                //move the position
                List<MetaColumn> cols = colToMove.MetaTable.Columns.ToList();
                foreach (var col in cols) col.DisplayOrder = 10 * col.DisplayOrder;
                colToMove.DisplayOrder += increment;
                int index = 1;
                foreach (var col in cols.OrderBy(i => i.DisplayOrder)) col.DisplayOrder = index++;
                entityHandler.SetModified();
                mainTreeView.Sort();
                TreeViewHelper.SelectNode(mainTreeView, mainTreeView.Nodes, colToMove);
            }
        }

        #region Drag and drop

        public void mainTreeView_ItemDrag(Control source, object sender, ItemDragEventArgs e)
        {
            TreeNode node = e.Item as TreeNode;
            if (node != null && (node.Tag is ReportView || node.Tag is ReportTask || node.Tag is MetaColumn))
            {
                if (node.Tag is MetaColumn && !((MetaColumn)(node.Tag)).MetaTable.IsEditable) return;

                mainTreeView.SelectedNode = node;
                source.DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        public void mainTreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
                TreeNode targetNode = ((TreeView)sender).GetNodeAt(pt);
                if (targetNode != null && (targetNode.Tag is ReportView || targetNode.Tag is ReportTask || targetNode.Tag is MetaColumn))
                {
                    e.Effect = DragDropEffects.Move;
                }
            }
        }

        public void mainTreeView_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                TreeNode targetNode = ((TreeView)sender).GetNodeAt(((TreeView)sender).PointToClient(new Point(e.X, e.Y)));
                TreeNode sourceNode = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
                if (sourceNode != null && targetNode != null && sourceNode.Tag is MetaColumn && targetNode.Tag is MetaColumn)
                {
                    var source = sourceNode.Tag as MetaColumn;
                    var target = targetNode.Tag as MetaColumn;
                    //move the position
                    int index = 1;
                    foreach (var col in source.MetaTable.Columns.OrderBy(i => i.DisplayOrder))
                    {
                        if (col == target)
                        {
                            source.DisplayOrder = index++;
                            target.DisplayOrder = index;
                            if (index == source.MetaTable.Columns.Count)
                            {
                                source.DisplayOrder = index;
                                target.DisplayOrder = index - 1;
                            }
                        }
                        else if (col != source)
                        {
                            col.DisplayOrder = index;
                        }
                        index++;
                    }
                    entityHandler.SetModified();
                    try
                    {
                        mainTreeView.BeginUpdate();
                        mainTreeView.Sort();
                        e.Effect = DragDropEffects.Move;
                        mainTreeView.SelectedNode = sourceNode;
                    }
                    finally
                    {
                        mainTreeView.EndUpdate();
                    }
                }
            }
        }

        public void mainTreeView_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                TreeNode targetNode = ((TreeView)sender).GetNodeAt(((TreeView)sender).PointToClient(new Point(e.X, e.Y)));
                TreeNode sourceNode = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
                if (sourceNode != null && targetNode != null && sourceNode.Tag is ReportView && targetNode.Tag is ReportView)
                {
                    ReportView sourceView = sourceNode.Tag as ReportView;
                    ReportView targetView = targetNode.Tag as ReportView;
                    if (sourceNode.Parent == targetNode.Parent)
                    {
                        //move position
                        e.Effect = DragDropEffects.Move;
                    }
                    else if (targetView.ReportViewTemplateChildren.Exists(i => i.Name == sourceView.TemplateName))
                    {
                        //move parent, check that the source if not a parent of the target
                        if (!sourceView.IsAncestorOf(targetView)) e.Effect = DragDropEffects.Move;
                    }
                }
                else if (sourceNode != null && targetNode != null && sourceNode.Tag is ReportTask && targetNode.Tag is ReportTask)
                {
                    if (sourceNode.Parent == targetNode.Parent)
                    {
                        //move position
                        e.Effect = DragDropEffects.Move;
                    }
                }
                else if (sourceNode != null && targetNode != null && sourceNode.Tag is MetaColumn && targetNode.Tag is MetaColumn)
                {
                    if (sourceNode.Parent == targetNode.Parent)
                    {
                        //move position
                        e.Effect = DragDropEffects.Move;
                    }
                }
            }
        }
 
        #endregion
    }
}
