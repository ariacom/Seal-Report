namespace Seal.Controls
{
    partial class ModelPanel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModelPanel));
            modelSplitContainer = new System.Windows.Forms.SplitContainer();
            modelSourceSplitContainer = new System.Windows.Forms.SplitContainer();
            buttonResetFilter = new System.Windows.Forms.Button();
            treeViewFilter = new System.Windows.Forms.TextBox();
            elementTreeView = new System.Windows.Forms.TreeView();
            mainImageList = new System.Windows.Forms.ImageList(components);
            elementsSplitContainer = new System.Windows.Forms.SplitContainer();
            selectedElementsGroupBox = new System.Windows.Forms.GroupBox();
            elementsContainer = new System.Windows.Forms.SplitContainer();
            selectedRestrictionsGroupBox = new System.Windows.Forms.GroupBox();
            restrictionsContainer = new System.Windows.Forms.SplitContainer();
            restrictionsSplitContainer = new System.Windows.Forms.SplitContainer();
            restrictionsPanel = new RestrictionsPanel();
            aggregateRestrictionsPanel = new RestrictionsPanel();
            ((System.ComponentModel.ISupportInitialize)modelSplitContainer).BeginInit();
            modelSplitContainer.Panel1.SuspendLayout();
            modelSplitContainer.Panel2.SuspendLayout();
            modelSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)modelSourceSplitContainer).BeginInit();
            modelSourceSplitContainer.Panel2.SuspendLayout();
            modelSourceSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)elementsSplitContainer).BeginInit();
            elementsSplitContainer.Panel1.SuspendLayout();
            elementsSplitContainer.Panel2.SuspendLayout();
            elementsSplitContainer.SuspendLayout();
            selectedElementsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)elementsContainer).BeginInit();
            elementsContainer.SuspendLayout();
            selectedRestrictionsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)restrictionsContainer).BeginInit();
            restrictionsContainer.Panel1.SuspendLayout();
            restrictionsContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)restrictionsSplitContainer).BeginInit();
            restrictionsSplitContainer.Panel1.SuspendLayout();
            restrictionsSplitContainer.Panel2.SuspendLayout();
            restrictionsSplitContainer.SuspendLayout();
            SuspendLayout();
            // 
            // modelSplitContainer
            // 
            modelSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            modelSplitContainer.Location = new System.Drawing.Point(0, 0);
            modelSplitContainer.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            modelSplitContainer.Name = "modelSplitContainer";
            // 
            // modelSplitContainer.Panel1
            // 
            modelSplitContainer.Panel1.Controls.Add(modelSourceSplitContainer);
            // 
            // modelSplitContainer.Panel2
            // 
            modelSplitContainer.Panel2.Controls.Add(elementsSplitContainer);
            modelSplitContainer.Panel2.SizeChanged += panel_Resize;
            modelSplitContainer.Panel2.Resize += panel_Resize;
            modelSplitContainer.Size = new System.Drawing.Size(727, 573);
            modelSplitContainer.SplitterDistance = 207;
            modelSplitContainer.SplitterWidth = 5;
            modelSplitContainer.TabIndex = 0;
            // 
            // modelSourceSplitContainer
            // 
            modelSourceSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            modelSourceSplitContainer.Location = new System.Drawing.Point(0, 0);
            modelSourceSplitContainer.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            modelSourceSplitContainer.Name = "modelSourceSplitContainer";
            modelSourceSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // modelSourceSplitContainer.Panel2
            // 
            modelSourceSplitContainer.Panel2.Controls.Add(buttonResetFilter);
            modelSourceSplitContainer.Panel2.Controls.Add(treeViewFilter);
            modelSourceSplitContainer.Panel2.Controls.Add(elementTreeView);
            modelSourceSplitContainer.Size = new System.Drawing.Size(207, 573);
            modelSourceSplitContainer.SplitterDistance = 138;
            modelSourceSplitContainer.SplitterWidth = 5;
            modelSourceSplitContainer.TabIndex = 1;
            // 
            // buttonResetFilter
            // 
            buttonResetFilter.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            buttonResetFilter.FlatAppearance.BorderSize = 0;
            buttonResetFilter.Location = new System.Drawing.Point(160, 2);
            buttonResetFilter.Name = "buttonResetFilter";
            buttonResetFilter.Size = new System.Drawing.Size(48, 24);
            buttonResetFilter.TabIndex = 4;
            buttonResetFilter.Text = "Reset";
            // 
            // treeViewFilter
            // 
            treeViewFilter.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            treeViewFilter.BorderStyle = System.Windows.Forms.BorderStyle.None;
            treeViewFilter.Location = new System.Drawing.Point(4, 5);
            treeViewFilter.Name = "treeViewFilter";
            treeViewFilter.Size = new System.Drawing.Size(152, 16);
            treeViewFilter.TabIndex = 0;
            // 
            // elementTreeView
            // 
            elementTreeView.AllowDrop = true;
            elementTreeView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            elementTreeView.HideSelection = false;
            elementTreeView.ImageIndex = 0;
            elementTreeView.ImageList = mainImageList;
            elementTreeView.Location = new System.Drawing.Point(0, 26);
            elementTreeView.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            elementTreeView.Name = "elementTreeView";
            elementTreeView.SelectedImageIndex = 0;
            elementTreeView.Size = new System.Drawing.Size(208, 400);
            elementTreeView.TabIndex = 1;
            elementTreeView.ItemDrag += elementTreeView_ItemDrag;
            elementTreeView.BeforeSelect += ElementTreeView_BeforeSelect;
            elementTreeView.NodeMouseClick += elementTreeView_NodeMouseClick;
            elementTreeView.DragDrop += elementTreeView_DragDrop;
            elementTreeView.DragEnter += elementTreeView_DragEnter;
            elementTreeView.DragOver += elementTreeView_DragOver;
            elementTreeView.DoubleClick += elementTreeView_DoubleClick;
            // 
            // mainImageList
            // 
            mainImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            mainImageList.ImageStream = (System.Windows.Forms.ImageListStreamer)resources.GetObject("mainImageList.ImageStream");
            mainImageList.TransparentColor = System.Drawing.Color.Transparent;
            mainImageList.Images.SetKeyName(0, "database.png");
            mainImageList.Images.SetKeyName(1, "connection.png");
            mainImageList.Images.SetKeyName(2, "folder.png");
            mainImageList.Images.SetKeyName(3, "label.png");
            mainImageList.Images.SetKeyName(4, "table.png");
            mainImageList.Images.SetKeyName(5, "join.png");
            mainImageList.Images.SetKeyName(6, "enum.png");
            mainImageList.Images.SetKeyName(7, "element.png");
            // 
            // elementsSplitContainer
            // 
            elementsSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            elementsSplitContainer.Location = new System.Drawing.Point(0, 0);
            elementsSplitContainer.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            elementsSplitContainer.Name = "elementsSplitContainer";
            elementsSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // elementsSplitContainer.Panel1
            // 
            elementsSplitContainer.Panel1.Controls.Add(selectedElementsGroupBox);
            // 
            // elementsSplitContainer.Panel2
            // 
            elementsSplitContainer.Panel2.Controls.Add(selectedRestrictionsGroupBox);
            elementsSplitContainer.Size = new System.Drawing.Size(515, 573);
            elementsSplitContainer.SplitterDistance = 285;
            elementsSplitContainer.SplitterWidth = 5;
            elementsSplitContainer.TabIndex = 2;
            // 
            // selectedElementsGroupBox
            // 
            selectedElementsGroupBox.Controls.Add(elementsContainer);
            selectedElementsGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            selectedElementsGroupBox.Location = new System.Drawing.Point(0, 0);
            selectedElementsGroupBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            selectedElementsGroupBox.Name = "selectedElementsGroupBox";
            selectedElementsGroupBox.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            selectedElementsGroupBox.Size = new System.Drawing.Size(515, 285);
            selectedElementsGroupBox.TabIndex = 1;
            selectedElementsGroupBox.TabStop = false;
            selectedElementsGroupBox.Text = "Elements";
            selectedElementsGroupBox.Resize += panel_Resize;
            // 
            // elementsContainer
            // 
            elementsContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            elementsContainer.Location = new System.Drawing.Point(4, 19);
            elementsContainer.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            elementsContainer.Name = "elementsContainer";
            // 
            // elementsContainer.Panel1
            // 
            elementsContainer.Panel1.Resize += panel_Resize;
            elementsContainer.Size = new System.Drawing.Size(507, 263);
            elementsContainer.SplitterDistance = 332;
            elementsContainer.SplitterWidth = 5;
            elementsContainer.TabIndex = 2;
            // 
            // selectedRestrictionsGroupBox
            // 
            selectedRestrictionsGroupBox.Controls.Add(restrictionsContainer);
            selectedRestrictionsGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            selectedRestrictionsGroupBox.Location = new System.Drawing.Point(0, 0);
            selectedRestrictionsGroupBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            selectedRestrictionsGroupBox.Name = "selectedRestrictionsGroupBox";
            selectedRestrictionsGroupBox.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            selectedRestrictionsGroupBox.Size = new System.Drawing.Size(515, 283);
            selectedRestrictionsGroupBox.TabIndex = 2;
            selectedRestrictionsGroupBox.TabStop = false;
            selectedRestrictionsGroupBox.Text = "Restrictions and Aggregate Restrictions";
            // 
            // restrictionsContainer
            // 
            restrictionsContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            restrictionsContainer.Location = new System.Drawing.Point(4, 19);
            restrictionsContainer.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            restrictionsContainer.Name = "restrictionsContainer";
            // 
            // restrictionsContainer.Panel1
            // 
            restrictionsContainer.Panel1.Controls.Add(restrictionsSplitContainer);
            restrictionsContainer.Size = new System.Drawing.Size(507, 261);
            restrictionsContainer.SplitterDistance = 329;
            restrictionsContainer.SplitterWidth = 5;
            restrictionsContainer.TabIndex = 3;
            // 
            // restrictionsSplitContainer
            // 
            restrictionsSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            restrictionsSplitContainer.Location = new System.Drawing.Point(0, 0);
            restrictionsSplitContainer.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            restrictionsSplitContainer.Name = "restrictionsSplitContainer";
            restrictionsSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // restrictionsSplitContainer.Panel1
            // 
            restrictionsSplitContainer.Panel1.Controls.Add(restrictionsPanel);
            // 
            // restrictionsSplitContainer.Panel2
            // 
            restrictionsSplitContainer.Panel2.Controls.Add(aggregateRestrictionsPanel);
            restrictionsSplitContainer.Size = new System.Drawing.Size(329, 261);
            restrictionsSplitContainer.SplitterDistance = 189;
            restrictionsSplitContainer.SplitterWidth = 5;
            restrictionsSplitContainer.TabIndex = 2;
            // 
            // restrictionsPanel
            // 
            restrictionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            restrictionsPanel.Location = new System.Drawing.Point(0, 0);
            restrictionsPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            restrictionsPanel.Name = "restrictionsPanel";
            restrictionsPanel.Size = new System.Drawing.Size(329, 189);
            restrictionsPanel.TabIndex = 2;
            // 
            // aggregateRestrictionsPanel
            // 
            aggregateRestrictionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            aggregateRestrictionsPanel.Location = new System.Drawing.Point(0, 0);
            aggregateRestrictionsPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            aggregateRestrictionsPanel.Name = "aggregateRestrictionsPanel";
            aggregateRestrictionsPanel.Size = new System.Drawing.Size(329, 67);
            aggregateRestrictionsPanel.TabIndex = 3;
            // 
            // ModelPanel
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(modelSplitContainer);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "ModelPanel";
            Size = new System.Drawing.Size(727, 573);
            ClientSizeChanged += panel_Resize;
            modelSplitContainer.Panel1.ResumeLayout(false);
            modelSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)modelSplitContainer).EndInit();
            modelSplitContainer.ResumeLayout(false);
            modelSourceSplitContainer.Panel2.ResumeLayout(false);
            modelSourceSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)modelSourceSplitContainer).EndInit();
            modelSourceSplitContainer.ResumeLayout(false);
            elementsSplitContainer.Panel1.ResumeLayout(false);
            elementsSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)elementsSplitContainer).EndInit();
            elementsSplitContainer.ResumeLayout(false);
            selectedElementsGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)elementsContainer).EndInit();
            elementsContainer.ResumeLayout(false);
            selectedRestrictionsGroupBox.ResumeLayout(false);
            restrictionsContainer.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)restrictionsContainer).EndInit();
            restrictionsContainer.ResumeLayout(false);
            restrictionsSplitContainer.Panel1.ResumeLayout(false);
            restrictionsSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)restrictionsSplitContainer).EndInit();
            restrictionsSplitContainer.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer modelSplitContainer;
        private System.Windows.Forms.SplitContainer elementsSplitContainer;
        private System.Windows.Forms.GroupBox selectedElementsGroupBox;
        private System.Windows.Forms.GroupBox selectedRestrictionsGroupBox;
        private System.Windows.Forms.SplitContainer elementsContainer;
        private System.Windows.Forms.SplitContainer restrictionsContainer;
        private System.Windows.Forms.SplitContainer restrictionsSplitContainer;
        private RestrictionsPanel restrictionsPanel;
        private RestrictionsPanel aggregateRestrictionsPanel;
        private System.Windows.Forms.SplitContainer modelSourceSplitContainer;
        private System.Windows.Forms.TreeView elementTreeView;
        private System.Windows.Forms.ImageList mainImageList;
        private System.Windows.Forms.TextBox treeViewFilter
            ;
        private System.Windows.Forms.Button buttonResetFilter;
    }
}
