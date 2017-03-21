//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RazorEngine;
using RazorEngine.Templating;
using System.Web.Razor;
using Seal.Model;
using DynamicTypeDescriptor;
using Seal.Helpers;
using System.Diagnostics;
using Seal.Forms;

namespace Seal.Controls
{
    public partial class RestrictionsPanel : UserControl
    {
        ModelPanel ModelPanel;
        public bool IsAggregate = false;
        bool collapseRestrictionCategoriesDone = false;

        string Restriction
        {
            get
            {
                return IsAggregate ? ModelPanel.Model.AggregateRestriction : ModelPanel.Model.Restriction;
            }
        }

        List<ReportRestriction> Restrictions
        {
            get
            {
                return IsAggregate ? ModelPanel.Model.AggregateRestrictions : ModelPanel.Model.Restrictions;
            }
        }


        public RestrictionsPanel()
        {
            InitializeComponent();
            restrictionsTextBox.ConfigurationManager.Language = "mssql";
        }

        public void Init(ModelPanel modelPanel)
        {
            ModelPanel = modelPanel;
            ModelToRestrictionText();

        }

        public void ClearSelection()
        {
            restrictionsTextBox.Selection.Length = 0;
        }

        public void Commit()
        {
            if (ModelPanel != null) RestrictionTextToModel();
        }

        public void RestrictionTextToModel()
        {
            List<ReportRestriction> restrictions = new List<ReportRestriction>();
            int startPos = 0, endPos = 0;
            string text = restrictionsTextBox.Text;
            while (startPos < text.Length)
            {
                ReportRestriction restriction = getRestriction(ref startPos, ref endPos, text);
                if (restriction != null)
                {
                    restrictions.Add(restriction);
                    //replace by restriction GUID
                    text = text.Remove(startPos + 1, endPos - startPos - 1);
                    text = text.Insert(startPos + 1, restriction.GUID);
                    startPos = startPos + 1 + restriction.GUID.Length + 1;
                }
            }
            //Restrictions not used are then removed
            if (IsAggregate)
            {
                ModelPanel.Model.AggregateRestriction = text;
                ModelPanel.Model.AggregateRestrictions = restrictions;
            }
            else
            {
                ModelPanel.Model.Restriction = text;
                ModelPanel.Model.Restrictions = restrictions;
            }
        }

        public void ModelToRestrictionText()
        {
            bool isModified = ModelPanel.MainForm.IsModified;
            
            int startPos = 0, endPos = 0;
            string text = Restriction;
            if (!string.IsNullOrEmpty(text))
            {
                while (startPos < text.Length)
                {
                    ReportRestriction restriction = getRestriction(ref startPos, ref endPos, text);
                    if (restriction != null)
                    {
                        //replace by restriction text
                        text = text.Remove(startPos + 1, endPos - startPos - 1);
                        text = text.Insert(startPos + 1, restriction.DisplayRestrictionForEditor);
                        startPos = startPos + 1 + restriction.DisplayRestrictionForEditor.Length + 1;
                    }
                }
            }
            restrictionsTextBox.Text = text;
            ModelPanel.MainForm.IsModified = isModified;
            restrictionsTextBox.Caret.Position = 0;
            restrictionsTextBox.Scrolling.ScrollToCaret();
        }

        public void UpdateRestrictionText()
        {
            if (ModelPanel.RestrictionGrid.SelectedObject != null)
            {
                ReportRestriction restriction = (ReportRestriction)ModelPanel.RestrictionGrid.SelectedObject;
                restrictionsTextBox.Selection.Text = ReportRestriction.kStartRestrictionChar + restriction.DisplayRestrictionForEditor + ReportRestriction.kStopRestrictionChar;
                restrictionsTextBox.Caret.Position -= 1;
                highlightRestriction(false);
            }
        }


        private ReportRestriction getRestriction(ref int startPos, ref int endPos, string text)
        {
            ReportRestriction result = null;
            if (startPos > text.Length) startPos = text.Length;
            int initialPos = startPos;

            endPos = startPos;
            bool startOk = false, endOk = false;
            while (--startPos >= 0)
            {
                if (text[startPos] == ReportRestriction.kStopRestrictionChar) break;
                if (text[startPos] == ReportRestriction.kStartRestrictionChar)
                {
                    if (startPos == 0 || (startPos > 0 && (text[startPos - 1]) != ReportRestriction.kStartRestrictionChar))
                    {
                        startOk = true;
                        break;
                    }
                }
            }

            if (startOk)
            {
                bool inQuote = false;
                while (endPos < text.Length)
                {
                    if (text[endPos] == '\'') inQuote = true;
                    if (text[endPos] == ReportRestriction.kStartRestrictionChar && !inQuote) break;
                    if (text[endPos] == ReportRestriction.kStopRestrictionChar)
                    {
                        if (endPos == text.Length - 1 || (endPos < text.Length - 1 && (text[endPos + 1]) != ReportRestriction.kStopRestrictionChar))
                        {
                            endOk = true;
                            break;
                        }
                    }
                    endPos++;
                }
            }

            if (startOk && endOk)
            {
                string fullText = text.Substring(startPos + 1, endPos - startPos - 1);
                result = Restrictions.FirstOrDefault(i => i.DisplayRestrictionForEditor == fullText);
                //Try with the GUID
                if (result == null) result = Restrictions.FirstOrDefault(i => i.GUID == fullText);
            }

            if (result == null) startPos = initialPos + 1;

            return result;
        }

        int getUtfCharLen(byte c)
        {
            if (c >= 194 && c <= 223) return 2;
            if (c >= 224 && c <= 239) return 3;
            if (c >= 240 && c <= 244) return 4;
            return 1;
        }

        int convertToRealPosition(int scintillaPos, byte[] rawText)
        {
            int pos = 0;
            for (int i = 0; i < scintillaPos; i++)
            {
                pos++;
                if (rawText[i] > 127) 
                {
                    int len = getUtfCharLen(rawText[i]) - 1;
                    i += len;
                }
            }
            return pos;
        }

        int convertToScintillaPosition(int realPos, string text)
        {
            return System.Text.Encoding.UTF8.GetBytes(text.Substring(0, realPos)).Length;
        }

        void highlightRestriction(bool isDragging)
        {
            //!! behaviour scintillina -> we have to convert the positions for UTF char...  :-(, waiting for a Scintillina expert !
            int startPos = convertToRealPosition(restrictionsTextBox.Caret.Position, restrictionsTextBox.RawText);
           //Debug.WriteLine("calc={0} caret={1} selstart={2} indentpos={3} linestart={4}", startPos, restrictionsTextBox.Caret.Position, restrictionsTextBox.Selection.Start, restrictionsTextBox.Lines.Current.IndentPosition, restrictionsTextBox.Lines.Current.StartPosition);
            int endPos = 0;
            var restriction = getRestriction(ref startPos, ref endPos, restrictionsTextBox.Text);
            if (!isDragging)
            {
                if (restriction != null)
                {
                    ModelPanel.SetMetaColumn(restriction);
                    restriction.InitEditor();
                }

                bool collapseCategories = (ModelPanel.RestrictionGrid.SelectedObject == null);
                ModelPanel.RestrictionGrid.SelectedObject = restriction;
                //Collapse Advanced categories
                if (collapseCategories && !collapseRestrictionCategoriesDone)
                {
                    collapseRestrictionCategoriesDone = true;
                    ModelPanel.CollapseCategories(ModelPanel.RestrictionGrid);
                }
            }
            if (restriction != null)
            {
                restrictionsTextBox.Selection.Start = convertToScintillaPosition(startPos, restrictionsTextBox.Text);
                restrictionsTextBox.Selection.End = convertToScintillaPosition(endPos, restrictionsTextBox.Text) + 1;
                restrictionsTextBox.Focus();

                MenuItem item = new MenuItem("Smart copy...");
                item.Click += new EventHandler(delegate(object sender2, EventArgs e2)
                {
                    SmartCopyForm form = new SmartCopyForm("Smart copy of " + restriction.DisplayNameEl, restriction, restriction.Model.Report);
                     form.ShowDialog();
                     if (form.IsReportModified)
                     {
                         ModelPanel.MainForm.IsModified = true;
                         ModelPanel.MainForm.CannotRenderAnymore();
                         ModelToRestrictionText();
                     }
                });

                restrictionsTextBox.ContextMenu = new System.Windows.Forms.ContextMenu();
                restrictionsTextBox.ContextMenu.MenuItems.Add(item);


            }
            else restrictionsTextBox.ContextMenu = null;
        }



        private void scintilla_MouseUp(object sender, MouseEventArgs e)
        {
            highlightRestriction(false);
        }

        private void scintilla_KeyUp(object sender, KeyEventArgs e)
        {
            ModelPanel.MainForm.IsModified = true;
            RestrictionTextToModel();
            highlightRestriction(false);
        }

        private void restrictionsTextBox_DragDrop(object sender, DragEventArgs e)
        {
            if (!Helper.CanDragAndDrop(e)) return;

            //This clean unused restrictions
            RestrictionTextToModel();

            MetaColumn column = null;
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                TreeNode elementNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
                column = (MetaColumn)elementNode.Tag;
            }
            if (e.Data.GetDataPresent(typeof(Button)))
            {
                Button button = (Button)e.Data.GetData(typeof(Button));
                column = ((ReportElement)button.Tag).MetaColumn;
            }
            AddRestriction(column, false);
        }

        private void restrictionsTextBox_DragEnter(object sender, DragEventArgs e)
        {
            if (Helper.CanDragAndDrop(e)) e.Effect = DragDropEffects.Move;
        }

        private void restrictionsTextBox_DragOver(object sender, DragEventArgs e)
        {
            if (Helper.CanDragAndDrop(e))
            {
                restrictionsTextBox.Selection.Start = getCaretIndexFromPoint(e.X, e.Y);
                restrictionsTextBox.Selection.Length = 0;
                restrictionsTextBox.Focus();
                highlightRestriction(true);
                e.Effect = DragDropEffects.Move;
            }
        }

        private int getCaretIndexFromPoint(int x, int y)
        {
            Point realPoint = restrictionsTextBox.PointToClient(new Point(x, y));
            int index = restrictionsTextBox.PositionFromPoint(realPoint.X, realPoint.Y);
            if (index == restrictionsTextBox.Text.Length - 1)
            {
                Point caretPoint = new Point();
                caretPoint.X = restrictionsTextBox.PointXFromPosition(index);
                caretPoint.Y = restrictionsTextBox.PointYFromPosition(index);
                if (realPoint.X > caretPoint.X) index += 1;
            }
            return index;
        }

        private void restrictionsTextBox_Paint(object sender, PaintEventArgs e)
        {
            Control c = sender as Control;
            using (Font f = new Font("Arial", 12))
            {
                string text = !IsAggregate ? "Drop Restrictions..." : "Drop Aggregate Restrictions...";
                if (restrictionsTextBox.TextLength == 0 && Height > 50) e.Graphics.DrawString(text, f, Brushes.Gray, new PointF(15, Height - 50));
            }
        }


        private void restrictionsTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!restrictionsTextBox.Focused) return; 
            ModelPanel.MainForm.IsModified = true;
            ModelPanel.MainForm.CannotRenderAnymore();
        }


        public void AddRestriction(MetaColumn column, bool forPrompt)
        {
            if (column != null)
            {
                //Add restriction to current place
                ReportRestriction restriction = ReportRestriction.CreateReportRestriction();
                restriction.Source = ModelPanel.Model.Source;
                restriction.Model = ModelPanel.Model;
                restriction.MetaColumnGUID = column.GUID;
                restriction.Name = column.Name;
                //Set PivotPos for aggregate restrictions
                restriction.PivotPosition = IsAggregate ? PivotPosition.Data : PivotPosition.Row;
                restriction.SetDefaults();
                if (restriction.IsText && !restriction.IsEnum) restriction.Operator = Operator.Contains;

                ModelPanel.MainForm.IsModified = true;
                //Check for duplicate
                int index = 1;
                string initialName = restriction.DisplayNameEl;
                while (Restrictions.FirstOrDefault(i => i.DisplayRestrictionForEditor == restriction.DisplayRestrictionForEditor) != null)
                {
                    index++;
                    restriction.DisplayName = initialName + " " + index.ToString();
                }
                Restrictions.Add(restriction);

                if (forPrompt)
                {
                    restriction.Prompt = PromptType.Prompt;
                    restrictionsTextBox.Selection.Length = 0;
                    if (restrictionsTextBox.TextLength > 0) restrictionsTextBox.Selection.Start = restrictionsTextBox.TextLength;
                }

                string insertedText = "";
                if (restrictionsTextBox.TextLength > 0 && restrictionsTextBox.Caret.Position >= restrictionsTextBox.TextLength)
                {
                    if (restrictionsTextBox.Text.Last() != '\n') insertedText = "\r\n";
                    insertedText += "AND ";
                }
                insertedText += ReportRestriction.kStartRestrictionChar + restriction.DisplayRestrictionForEditor + ReportRestriction.kStopRestrictionChar;
                restrictionsTextBox.Selection.Text = insertedText;
                restrictionsTextBox.Caret.Position -= 1;
                highlightRestriction(false);
            }
            Commit();
        }

    }
}
