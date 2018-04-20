//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Seal.Model;
using Seal.Helpers;

namespace Seal.Controls
{
    public class ElementPanel : Panel
    {
        ModelPanel _modelPanel;
        public PivotPosition Position;

        public ElementPanel(ModelPanel modelPanel, PivotPosition position)
        {
            _modelPanel = modelPanel;
            Position = position;

            BackColor = Color.White;
            DragEnter += new DragEventHandler(onDragEnter);
            DragOver += new DragEventHandler(onDragOver);
            DragDrop += new DragEventHandler(onDragDrop);
            Paint += new PaintEventHandler(ElementPanel_Paint);
            AutoScroll = true;
            AllowDrop = true;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ElementPanel
            // 
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ElementPanel_Paint);
            this.ResumeLayout(false);

        }

        public void RedrawPanel()
        {
            for (int i = 0; i < Controls.Count; i++)
            {
                Control button = Controls[i];
                button.Location = new Point(2, button.Height * i - VerticalScroll.Value);
            }
            RedrawButtons();
        }

        public void RedrawButtons()
        {
            for (int i = 0; i < Controls.Count; i++)
            {
                Button button = Controls[i] as Button;
                if (button != null)
                {
                    ReportElement element = button.Tag as ReportElement;
                    button.Text = element.DisplayNameEl;
                    button.BackColor = System.Drawing.SystemColors.Control; 
                    button.UseVisualStyleBackColor = true;
                    if (button == _modelPanel.SelectedButton) button.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    else button.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    if ((element.PivotPosition == PivotPosition.Column || element.PivotPosition == PivotPosition.Row) && element.SerieDefinition == SerieDefinition.Axis) button.Image = ((System.Drawing.Image)(Properties.Resources.chartAxis));
                    else if (element.IsSerie && element.PivotPosition == PivotPosition.Data) button.Image = ((System.Drawing.Image)(Properties.Resources.chartSerie));
                    else if ((element.PivotPosition == PivotPosition.Column || element.PivotPosition == PivotPosition.Row) && (element.SerieDefinition == SerieDefinition.Splitter || element.SerieDefinition == SerieDefinition.SplitterBoth)) button.Image = ((System.Drawing.Image)(Properties.Resources.chartSplitter));
                    else button.Image = null;
                    button.ImageAlign = ContentAlignment.MiddleRight;
                }
            }
        }

        public void ResizeControls()
        {
            foreach (Control control in Controls)
            {
                control.Width = Width - 5;
            }
        }

        public void PanelToElements(List<ReportElement> elements)
        {
            foreach (Control button in Controls)
            {
                elements.Add((ReportElement)button.Tag);
            }
        }

        int getIndexFocus(DragEventArgs e)
        {
            int result = -1;
            Point point = PointToClient(new Point(e.X, e.Y));
            for (int i = 0; i < Controls.Count; i++)
            {
                if ((i == 0 && point.Y < Controls[i].Location.Y + Controls[i].Height)  ||
                    (point.Y > Controls[i].Location.Y && point.Y < Controls[i].Location.Y + Controls[i].Height)  ||
                    (i == Controls.Count - 1 && point.Y > Controls[i].Location.Y)
                    )
                {
                    result = i;
                    break;
                }
            }
            return result;
        }

        void onDragDrop(object sender, DragEventArgs e)
        {
            if (!Helper.CanDragAndDrop(e)) return;

            Button button = null;
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                TreeNode elementNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
                button = _modelPanel.AddElement(this, (MetaColumn)elementNode.Tag, true);
                _modelPanel.MainForm.IsModified = true;
                _modelPanel.MainForm.CannotRenderAnymore();
            }
            if (e.Data.GetDataPresent(typeof(Button)))
            {
                button = (Button)e.Data.GetData(typeof(Button));
            }
            if (button != null)
            {
                ElementPanel source = null;
                if (button.Parent != this)
                {
                    //Button comes from another panel
                    ReportElement element = (ReportElement)button.Tag;
                    source = (ElementPanel)button.Parent;
                    source.Controls.Remove(button);
                    element.PivotPosition = Position;
                    element.InitEditor();
                    source.RedrawPanel();
                    Controls.Add(button);
                    _modelPanel.MainForm.IsModified = true;
                    _modelPanel.MainForm.CannotRenderAnymore();
                    _modelPanel.PanelsToElements();
                }

                //Set new position
                int index = getIndexFocus(e);
                if (index != -1 && Controls[index] != button)
                {
                    Controls.SetChildIndex(button, index);
                    _modelPanel.MainForm.IsModified = true;
                    _modelPanel.MainForm.CannotRenderAnymore();
                    _modelPanel.PanelsToElements();
                }
                RedrawPanel();
                button.Focus();
            }
            _modelPanel.Model.CheckSortOrders();
        }


        void onDragOver(object sender, DragEventArgs e)
        {
            if (Helper.CanDragAndDrop(e))
            {
                //set focus
                int index = getIndexFocus(e);
                if (index != -1)
                {
                    Controls[index].Focus();
                }

                //Scroll up handling
                Point point = PointToClient(new Point(e.X, e.Y));
                if (point.Y < 10 && VerticalScroll.Value > 0)
                {
                    if (VerticalScroll.Value < 5) VerticalScroll.Value = 0;
                    else VerticalScroll.Value -= 2;
                }

                e.Effect = DragDropEffects.Move;
            }
        }

        void onDragEnter(object sender, DragEventArgs e)
        {
            if (Helper.CanDragAndDrop(e))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void ElementPanel_Paint(object sender, PaintEventArgs e)
        {
            Control c = sender as Control;
            using (Font f = new Font("Arial", 12))
            {
                if (Height > 25) e.Graphics.DrawString(string.Format("Drop {0} Elements...", Position), f, Brushes.Gray, new PointF(2, Height - 25));
            }
        }


    }
}
