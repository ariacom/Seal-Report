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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModelPanel));
            this.modelSplitContainer = new System.Windows.Forms.SplitContainer();
            this.modelSourceSplitContainer = new System.Windows.Forms.SplitContainer();
            this.elementTreeView = new System.Windows.Forms.TreeView();
            this.elementsSplitContainer = new System.Windows.Forms.SplitContainer();
            this.selectedElementsGroupBox = new System.Windows.Forms.GroupBox();
            this.elementsContainer = new System.Windows.Forms.SplitContainer();
            this.selectedRestrictionsGroupBox = new System.Windows.Forms.GroupBox();
            this.restrictionsContainer = new System.Windows.Forms.SplitContainer();
            this.restrictionsSplitContainer = new System.Windows.Forms.SplitContainer();
            this.restrictionsPanel = new Seal.Controls.RestrictionsPanel();
            this.aggregateRestrictionsPanel = new Seal.Controls.RestrictionsPanel();
            this.mainImageList = new System.Windows.Forms.ImageList(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.modelSplitContainer)).BeginInit();
            this.modelSplitContainer.Panel1.SuspendLayout();
            this.modelSplitContainer.Panel2.SuspendLayout();
            this.modelSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.modelSourceSplitContainer)).BeginInit();
            this.modelSourceSplitContainer.Panel2.SuspendLayout();
            this.modelSourceSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.elementsSplitContainer)).BeginInit();
            this.elementsSplitContainer.Panel1.SuspendLayout();
            this.elementsSplitContainer.Panel2.SuspendLayout();
            this.elementsSplitContainer.SuspendLayout();
            this.selectedElementsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.elementsContainer)).BeginInit();
            this.elementsContainer.SuspendLayout();
            this.selectedRestrictionsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.restrictionsContainer)).BeginInit();
            this.restrictionsContainer.Panel1.SuspendLayout();
            this.restrictionsContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.restrictionsSplitContainer)).BeginInit();
            this.restrictionsSplitContainer.Panel1.SuspendLayout();
            this.restrictionsSplitContainer.Panel2.SuspendLayout();
            this.restrictionsSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // modelSplitContainer
            // 
            this.modelSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modelSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.modelSplitContainer.Name = "modelSplitContainer";
            // 
            // modelSplitContainer.Panel1
            // 
            this.modelSplitContainer.Panel1.Controls.Add(this.modelSourceSplitContainer);
            // 
            // modelSplitContainer.Panel2
            // 
            this.modelSplitContainer.Panel2.Controls.Add(this.elementsSplitContainer);
            this.modelSplitContainer.Panel2.SizeChanged += new System.EventHandler(this.panel_Resize);
            this.modelSplitContainer.Panel2.Resize += new System.EventHandler(this.panel_Resize);
            this.modelSplitContainer.Size = new System.Drawing.Size(623, 497);
            this.modelSplitContainer.SplitterDistance = 178;
            this.modelSplitContainer.TabIndex = 0;
            // 
            // modelSourceSplitContainer
            // 
            this.modelSourceSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modelSourceSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.modelSourceSplitContainer.Name = "modelSourceSplitContainer";
            this.modelSourceSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // modelSourceSplitContainer.Panel2
            // 
            this.modelSourceSplitContainer.Panel2.Controls.Add(this.elementTreeView);
            this.modelSourceSplitContainer.Size = new System.Drawing.Size(178, 497);
            this.modelSourceSplitContainer.SplitterDistance = 120;
            this.modelSourceSplitContainer.TabIndex = 1;
            // 
            // elementTreeView
            // 
            this.elementTreeView.AllowDrop = true;
            this.elementTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementTreeView.ImageIndex = 0;
            this.elementTreeView.ImageList = this.mainImageList;
            this.elementTreeView.Location = new System.Drawing.Point(0, 0);
            this.elementTreeView.Margin = new System.Windows.Forms.Padding(3, 30, 3, 3);
            this.elementTreeView.Name = "elementTreeView";
            this.elementTreeView.SelectedImageIndex = 0;
            this.elementTreeView.Size = new System.Drawing.Size(178, 400);
            this.elementTreeView.TabIndex = 1;
            this.elementTreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.elementTreeView_ItemDrag);
            this.elementTreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.elementTreeView_NodeMouseClick);
            this.elementTreeView.DragDrop += new System.Windows.Forms.DragEventHandler(this.elementTreeView_DragDrop);
            this.elementTreeView.DragEnter += new System.Windows.Forms.DragEventHandler(this.elementTreeView_DragEnter);
            this.elementTreeView.DragOver += new System.Windows.Forms.DragEventHandler(this.elementTreeView_DragOver);
            this.elementTreeView.DoubleClick += new System.EventHandler(this.elementTreeView_DoubleClick);
            // 
            // elementsSplitContainer
            // 
            this.elementsSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementsSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.elementsSplitContainer.Name = "elementsSplitContainer";
            this.elementsSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // elementsSplitContainer.Panel1
            // 
            this.elementsSplitContainer.Panel1.Controls.Add(this.selectedElementsGroupBox);
            // 
            // elementsSplitContainer.Panel2
            // 
            this.elementsSplitContainer.Panel2.Controls.Add(this.selectedRestrictionsGroupBox);
            this.elementsSplitContainer.Size = new System.Drawing.Size(441, 497);
            this.elementsSplitContainer.SplitterDistance = 248;
            this.elementsSplitContainer.TabIndex = 2;
            // 
            // selectedElementsGroupBox
            // 
            this.selectedElementsGroupBox.Controls.Add(this.elementsContainer);
            this.selectedElementsGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectedElementsGroupBox.Location = new System.Drawing.Point(0, 0);
            this.selectedElementsGroupBox.Name = "selectedElementsGroupBox";
            this.selectedElementsGroupBox.Size = new System.Drawing.Size(441, 248);
            this.selectedElementsGroupBox.TabIndex = 1;
            this.selectedElementsGroupBox.TabStop = false;
            this.selectedElementsGroupBox.Text = "Elements";
            this.selectedElementsGroupBox.Resize += new System.EventHandler(this.panel_Resize);
            // 
            // elementsContainer
            // 
            this.elementsContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementsContainer.Location = new System.Drawing.Point(3, 16);
            this.elementsContainer.Name = "elementsContainer";
            // 
            // elementsContainer.Panel1
            // 
            this.elementsContainer.Panel1.Resize += new System.EventHandler(this.panel_Resize);
            this.elementsContainer.Size = new System.Drawing.Size(435, 229);
            this.elementsContainer.SplitterDistance = 285;
            this.elementsContainer.TabIndex = 2;
            // 
            // selectedRestrictionsGroupBox
            // 
            this.selectedRestrictionsGroupBox.Controls.Add(this.restrictionsContainer);
            this.selectedRestrictionsGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectedRestrictionsGroupBox.Location = new System.Drawing.Point(0, 0);
            this.selectedRestrictionsGroupBox.Name = "selectedRestrictionsGroupBox";
            this.selectedRestrictionsGroupBox.Size = new System.Drawing.Size(441, 245);
            this.selectedRestrictionsGroupBox.TabIndex = 2;
            this.selectedRestrictionsGroupBox.TabStop = false;
            this.selectedRestrictionsGroupBox.Text = "Restrictions and Aggregate Restrictions";
            // 
            // restrictionsContainer
            // 
            this.restrictionsContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.restrictionsContainer.Location = new System.Drawing.Point(3, 16);
            this.restrictionsContainer.Name = "restrictionsContainer";
            // 
            // restrictionsContainer.Panel1
            // 
            this.restrictionsContainer.Panel1.Controls.Add(this.restrictionsSplitContainer);
            this.restrictionsContainer.Size = new System.Drawing.Size(435, 226);
            this.restrictionsContainer.SplitterDistance = 283;
            this.restrictionsContainer.TabIndex = 3;
            // 
            // restrictionsSplitContainer
            // 
            this.restrictionsSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.restrictionsSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.restrictionsSplitContainer.Name = "restrictionsSplitContainer";
            this.restrictionsSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // restrictionsSplitContainer.Panel1
            // 
            this.restrictionsSplitContainer.Panel1.Controls.Add(this.restrictionsPanel);
            // 
            // restrictionsSplitContainer.Panel2
            // 
            this.restrictionsSplitContainer.Panel2.Controls.Add(this.aggregateRestrictionsPanel);
            this.restrictionsSplitContainer.Size = new System.Drawing.Size(283, 226);
            this.restrictionsSplitContainer.SplitterDistance = 164;
            this.restrictionsSplitContainer.TabIndex = 2;
            // 
            // restrictionsPanel
            // 
            this.restrictionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.restrictionsPanel.Location = new System.Drawing.Point(0, 0);
            this.restrictionsPanel.Name = "restrictionsPanel";
            this.restrictionsPanel.Size = new System.Drawing.Size(283, 164);
            this.restrictionsPanel.TabIndex = 2;
            // 
            // aggregateRestrictionsPanel
            // 
            this.aggregateRestrictionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.aggregateRestrictionsPanel.Location = new System.Drawing.Point(0, 0);
            this.aggregateRestrictionsPanel.Name = "aggregateRestrictionsPanel";
            this.aggregateRestrictionsPanel.Size = new System.Drawing.Size(283, 58);
            this.aggregateRestrictionsPanel.TabIndex = 3;
            // 
            // mainImageList
            // 
            this.mainImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("mainImageList.ImageStream")));
            this.mainImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.mainImageList.Images.SetKeyName(0, "database.png");
            this.mainImageList.Images.SetKeyName(1, "connection.png");
            this.mainImageList.Images.SetKeyName(2, "folder.png");
            this.mainImageList.Images.SetKeyName(3, "label.png");
            this.mainImageList.Images.SetKeyName(4, "table.png");
            this.mainImageList.Images.SetKeyName(5, "join.png");
            this.mainImageList.Images.SetKeyName(6, "enum.png");
            this.mainImageList.Images.SetKeyName(7, "element.png");
            // 
            // ModelPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.modelSplitContainer);
            this.Name = "ModelPanel";
            this.Size = new System.Drawing.Size(623, 497);
            this.ClientSizeChanged += new System.EventHandler(this.panel_Resize);
            this.modelSplitContainer.Panel1.ResumeLayout(false);
            this.modelSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.modelSplitContainer)).EndInit();
            this.modelSplitContainer.ResumeLayout(false);
            this.modelSourceSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.modelSourceSplitContainer)).EndInit();
            this.modelSourceSplitContainer.ResumeLayout(false);
            this.elementsSplitContainer.Panel1.ResumeLayout(false);
            this.elementsSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.elementsSplitContainer)).EndInit();
            this.elementsSplitContainer.ResumeLayout(false);
            this.selectedElementsGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.elementsContainer)).EndInit();
            this.elementsContainer.ResumeLayout(false);
            this.selectedRestrictionsGroupBox.ResumeLayout(false);
            this.restrictionsContainer.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.restrictionsContainer)).EndInit();
            this.restrictionsContainer.ResumeLayout(false);
            this.restrictionsSplitContainer.Panel1.ResumeLayout(false);
            this.restrictionsSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.restrictionsSplitContainer)).EndInit();
            this.restrictionsSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

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
    }
}
