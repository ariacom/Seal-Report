//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Collections.Generic;
using System.Linq;
using Seal.Model;
using System.Windows.Forms;
using Seal.Forms;
using System;
using System.Runtime.InteropServices;

namespace Seal.Helpers
{
    public class TreeViewHelper
    {
        static TreeNode getCategoryTreeNode(TreeNodeCollection nodes, string category)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is CategoryFolder && ((CategoryFolder)node.Tag).Name == category) return node;
            }
            return null;
        }


        public static void InitCategoryTreeNode(TreeNodeCollection categoryNodes, List<MetaTable> tables)
        {
            var repository = Repository.Instance;
            categoryNodes.Clear();
            if (tables.Count > 0)
            {
                List<MetaColumn> columns = new List<MetaColumn>();
                foreach (MetaTable table in tables)
                {
                    foreach (MetaColumn column in table.Columns)
                    {
                        columns.Add(column);
                    }
                }

                foreach (var column in columns.OrderBy(i => i.DisplayOrder))
                {
                    string[] categories = column.Category.Split('/');
                    TreeNodeCollection nodes = categoryNodes;
                    TreeNode categoryNode = null;
                    string path = "";
                    foreach (var category in categories)
                    {
                        Helper.AddValue(ref path, "/", category);
                        categoryNode = getCategoryTreeNode(nodes, category);
                        if (categoryNode == null)
                        {
                            var cat = category;
                            if (!Repository.IsServerManager) cat = repository.TranslateCategory(path, category);
                            categoryNode = new TreeNode(cat) { Tag = new CategoryFolder() { Name = category, Path = path }, ImageIndex = 2, SelectedImageIndex = 2 };
                            nodes.Add(categoryNode);
                        }
                        nodes = categoryNode.Nodes;
                    }

                    var col = column.DisplayName;
                    if (!Repository.IsServerManager) col = repository.TranslateColumn(column);
                    var tn = new TreeNode(col) { Tag = column, ImageIndex = 7, SelectedImageIndex = 7 };
                    categoryNode.Nodes.Add(tn);
                }
            }
        }


        public static void InitTablesLinksTreeNode(TreeNodeCollection tableLinksNodes, List<MetaTableLink> tableLinks)
        {
            tableLinksNodes.Clear();
            if (tableLinks.Count > 0)
            {
                foreach (var link  in tableLinks)
                {
                    var tn = new TreeNode(link.DisplayName) { Tag = link, ImageIndex = 4, SelectedImageIndex = 4 };
                    tableLinksNodes.Add(tn);
                }
            }
        }

        public static void SelectNode(TreeView treeView, TreeNodeCollection nodes, object entity)
        {
            if (treeView.SelectedNode != null && treeView.SelectedNode.Tag == entity) return;
            
            foreach (TreeNode node in nodes)
            {
                if (node.Tag == entity)
                {
                    treeView.SelectedNode = node;
                    treeView.SelectedNode.Expand();
                    treeView.SelectedNode.EnsureVisible();
                    treeView.Focus();
                    break;
                }
                SelectNode(treeView, node.Nodes, entity);
            }
        }

        public static TreeNode SelectCategoryNode(TreeView treeView, TreeNodeCollection nodes, string path)
        {
            TreeNode result = null;
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is CategoryFolder && ((CategoryFolder)node.Tag).Path == path)
                {
                    result = node;
                    treeView.SelectedNode = node;
                    treeView.SelectedNode.Expand();
                    break;
                }
                if (result == null) SelectCategoryNode(treeView, node.Nodes, path);
            }
            return result;
        }


        public static void SelectNode(TreeView treeView, TreeNodeCollection nodes, string fullPath)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.FullPath == fullPath)
                {
                    treeView.SelectedNode = node;
                    treeView.SelectedNode.Expand();
                    break;
                }
                SelectNode(treeView, node.Nodes, fullPath);
            }
        }


        public static TreeNode GetRootCategoryNode(TreeNode node)
        {
            TreeNode result = node;
            while (result != null && !(result.Tag is TableFolder)) result = result.Parent;
            //get the child...
            foreach (TreeNode child in result.Nodes)
            {
                if (child.Tag is CategoryFolder)
                {
                    result = child;
                    break;
                }
            }
            return result;
        }


        public static void NodesFromEntity(TreeNodeCollection nodes, object entity, List<TreeNode> result)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag == entity)
                {
                    result.Add(node);
                }
                NodesFromEntity(node.Nodes, entity, result);
            }
        }

        public static List<TreeNode> CloneNodes(TreeNodeCollection nodes)
        {
            var list = new List<TreeNode>();
            foreach (TreeNode node in nodes)
            {
                list.Add((TreeNode)node.Clone());
            }
            return list;
        }
        public static void ApplyTreeViewFilter(TreeView tree, string filter, List<TreeNode> originalNodes)
        {
            object entityToSelect = tree.SelectedNode?.Tag;

            tree.BeginUpdate();
            tree.Nodes.Clear();

            foreach (var node in originalNodes)
            {
                var filtered = FilterNode(node, filter);
                if (filtered != null)
                    tree.Nodes.Add(filtered);
            }

            tree.ExpandAll();
            tree.EndUpdate();

            if (entityToSelect != null) {
                SelectNode(tree, tree.Nodes, entityToSelect);
            }
            else if (tree.Nodes.Count > 0)
            {
                TreeNode firstNode = tree.Nodes[0];
                tree.SelectedNode = firstNode;
                firstNode.EnsureVisible();
            }
        }

        public static TreeNode FilterNode(TreeNode node, string filter)
        {
            // Clone node (without children yet)
            TreeNode newNode = (TreeNode)node.Clone();
            newNode.Nodes.Clear();

            foreach (TreeNode child in node.Nodes)
            {
                var filteredChild = FilterNode(child, filter);
                if (filteredChild != null)
                    newNode.Nodes.Add(filteredChild);
            }

            // Match if node text matches OR any child matched
            if (node.Text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || newNode.Nodes.Count > 0)
            {
                return newNode;
            }

            return null;
        }

        public const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);
    }
}
