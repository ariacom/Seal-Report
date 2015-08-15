//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using Seal.Model;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Seal.Forms;

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
                            categoryNode = new TreeNode(category) { Tag = new CategoryFolder() { Name = category, Path = path }, ImageIndex = 2, SelectedImageIndex = 2 };
                            nodes.Add(categoryNode);
                        }
                        nodes = categoryNode.Nodes;
                    }

                    var tn = new TreeNode(column.DisplayName) { Tag = column, ImageIndex = 7, SelectedImageIndex = 7 };
                    categoryNode.Nodes.Add(tn);
                }
            }
        }

        public static void SelectNode(TreeView mainTreeView, TreeNodeCollection nodes, object entity)
        {
            if (mainTreeView.SelectedNode != null && mainTreeView.SelectedNode.Tag == entity) return;

            foreach (TreeNode node in nodes)
            {
                if (node.Tag == entity)
                {
                    mainTreeView.SelectedNode = node;
                    mainTreeView.SelectedNode.Expand();
                    break;
                }
                SelectNode(mainTreeView, node.Nodes, entity);
            }
        }

        public static TreeNode SelectCategoryNode(TreeView mainTreeView, TreeNodeCollection nodes, string path)
        {
            TreeNode result = null;
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is CategoryFolder && ((CategoryFolder)node.Tag).Path == path)
                {
                    result = node;
                    mainTreeView.SelectedNode = node;
                    mainTreeView.SelectedNode.Expand();
                    break;
                }
                if (result == null) SelectCategoryNode(mainTreeView, node.Nodes, path);
            }
            return result;
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
    }
}
