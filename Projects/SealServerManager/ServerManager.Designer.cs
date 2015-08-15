namespace Seal
{
    partial class ServerManager
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerManager));
            this.mainMenuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataSourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.noSQLdataSourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.emailOutputDeviceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openSourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openDeviceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainTreeView = new System.Windows.Forms.TreeView();
            this.treeContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFromToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeRootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sortColumnAlphaOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sortColumnSQLOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainImageList = new System.Windows.Forms.ImageList(this.components);
            this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.mainPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.mainToolStrip = new System.Windows.Forms.ToolStrip();
            this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.openFolderToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.openTasksToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.openEventsToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.mainMenuStrip.SuspendLayout();
            this.treeContextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).BeginInit();
            this.mainSplitContainer.Panel1.SuspendLayout();
            this.mainSplitContainer.Panel2.SuspendLayout();
            this.mainSplitContainer.SuspendLayout();
            this.mainToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenuStrip
            // 
            this.mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.configurationToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.mainMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.mainMenuStrip.Name = "mainMenuStrip";
            this.mainMenuStrip.Size = new System.Drawing.Size(1220, 24);
            this.mainMenuStrip.TabIndex = 0;
            this.mainMenuStrip.Text = "mainMenuStrip";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openSourceToolStripMenuItem,
            this.openDeviceToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.closeToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dataSourceToolStripMenuItem,
            this.noSQLdataSourceToolStripMenuItem,
            this.toolStripSeparator2,
            this.emailOutputDeviceToolStripMenuItem});
            this.newToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("newToolStripMenuItem.Image")));
            this.newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.newToolStripMenuItem.Text = "New";
            // 
            // dataSourceToolStripMenuItem
            // 
            this.dataSourceToolStripMenuItem.Image = global::Seal.Properties.Resources.database;
            this.dataSourceToolStripMenuItem.Name = "dataSourceToolStripMenuItem";
            this.dataSourceToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.dataSourceToolStripMenuItem.Text = "Data Source";
            this.dataSourceToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // noSQLdataSourceToolStripMenuItem
            // 
            this.noSQLdataSourceToolStripMenuItem.Image = global::Seal.Properties.Resources.nosql;
            this.noSQLdataSourceToolStripMenuItem.Name = "noSQLdataSourceToolStripMenuItem";
            this.noSQLdataSourceToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.noSQLdataSourceToolStripMenuItem.Text = "No SQL Data Source";
            this.noSQLdataSourceToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(179, 6);
            // 
            // emailOutputDeviceToolStripMenuItem
            // 
            this.emailOutputDeviceToolStripMenuItem.Image = global::Seal.Properties.Resources.device;
            this.emailOutputDeviceToolStripMenuItem.Name = "emailOutputDeviceToolStripMenuItem";
            this.emailOutputDeviceToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.emailOutputDeviceToolStripMenuItem.Text = "Email Output Device";
            this.emailOutputDeviceToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openSourceToolStripMenuItem
            // 
            this.openSourceToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openSourceToolStripMenuItem.Image")));
            this.openSourceToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.openSourceToolStripMenuItem.Name = "openSourceToolStripMenuItem";
            this.openSourceToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.openSourceToolStripMenuItem.Text = "Open Data Source";
            // 
            // openDeviceToolStripMenuItem
            // 
            this.openDeviceToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openDeviceToolStripMenuItem.Image")));
            this.openDeviceToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.openDeviceToolStripMenuItem.Name = "openDeviceToolStripMenuItem";
            this.openDeviceToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.openDeviceToolStripMenuItem.Text = "Open Output Device";
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripMenuItem.Image")));
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.ShortcutKeyDisplayString = "";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(179, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("exitToolStripMenuItem.Image")));
            this.exitToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // configurationToolStripMenuItem
            // 
            this.configurationToolStripMenuItem.Name = "configurationToolStripMenuItem";
            this.configurationToolStripMenuItem.Size = new System.Drawing.Size(93, 20);
            this.configurationToolStripMenuItem.Text = "Configuration";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // mainTreeView
            // 
            this.mainTreeView.ContextMenuStrip = this.treeContextMenuStrip;
            this.mainTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTreeView.FullRowSelect = true;
            this.mainTreeView.HideSelection = false;
            this.mainTreeView.ImageIndex = 0;
            this.mainTreeView.ImageList = this.mainImageList;
            this.mainTreeView.Location = new System.Drawing.Point(0, 0);
            this.mainTreeView.Name = "mainTreeView";
            this.mainTreeView.SelectedImageIndex = 0;
            this.mainTreeView.Size = new System.Drawing.Size(245, 625);
            this.mainTreeView.TabIndex = 1;
            this.mainTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.mainTreeView_AfterSelect);
            this.mainTreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.mainTreeView_NodeMouseClick);
            this.mainTreeView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.mainTreeView_MouseUp);
            // 
            // treeContextMenuStrip
            // 
            this.treeContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem,
            this.removeToolStripMenuItem,
            this.addFromToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.removeRootToolStripMenuItem,
            this.sortColumnAlphaOrderToolStripMenuItem,
            this.sortColumnSQLOrderToolStripMenuItem});
            this.treeContextMenuStrip.Name = "treeContextMenuStrip";
            this.treeContextMenuStrip.Size = new System.Drawing.Size(233, 158);
            this.treeContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.treeContextMenuStrip_Opening);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.addToolStripMenuItem.Text = "Add";
            this.addToolStripMenuItem.Click += new System.EventHandler(this.addToolStripMenuItem_Click);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.removeToolStripMenuItem.Text = "Remove...";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
            // 
            // addFromToolStripMenuItem
            // 
            this.addFromToolStripMenuItem.Name = "addFromToolStripMenuItem";
            this.addFromToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.addFromToolStripMenuItem.Text = "Add from Catalog...";
            this.addFromToolStripMenuItem.Click += new System.EventHandler(this.addFromToolStripMenuItem_Click);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.copyToolStripMenuItem.Text = "Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // removeRootToolStripMenuItem
            // 
            this.removeRootToolStripMenuItem.Name = "removeRootToolStripMenuItem";
            this.removeRootToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.removeRootToolStripMenuItem.Text = "Remove Root";
            this.removeRootToolStripMenuItem.Click += new System.EventHandler(this.removeRootToolStripMenuItem_Click);
            // 
            // sortColumnAlphaOrderToolStripMenuItem
            // 
            this.sortColumnAlphaOrderToolStripMenuItem.Name = "sortColumnAlphaOrderToolStripMenuItem";
            this.sortColumnAlphaOrderToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.sortColumnAlphaOrderToolStripMenuItem.Text = "Sort Columns by Name ";
            this.sortColumnAlphaOrderToolStripMenuItem.Click += new System.EventHandler(this.sortColumnsToolStripMenuItem_Click);
            // 
            // sortColumnSQLOrderToolStripMenuItem
            // 
            this.sortColumnSQLOrderToolStripMenuItem.Name = "sortColumnSQLOrderToolStripMenuItem";
            this.sortColumnSQLOrderToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.sortColumnSQLOrderToolStripMenuItem.Text = "Sort Columns by SQL position";
            this.sortColumnSQLOrderToolStripMenuItem.Click += new System.EventHandler(this.sortColumnsToolStripMenuItem_Click);
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
            this.mainImageList.Images.SetKeyName(8, "device.png");
            this.mainImageList.Images.SetKeyName(9, "nosql.png");
            // 
            // mainSplitContainer
            // 
            this.mainSplitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainSplitContainer.Location = new System.Drawing.Point(0, 52);
            this.mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            this.mainSplitContainer.Panel1.Controls.Add(this.mainTreeView);
            // 
            // mainSplitContainer.Panel2
            // 
            this.mainSplitContainer.Panel2.Controls.Add(this.mainPropertyGrid);
            this.mainSplitContainer.Size = new System.Drawing.Size(1220, 625);
            this.mainSplitContainer.SplitterDistance = 245;
            this.mainSplitContainer.TabIndex = 3;
            // 
            // mainPropertyGrid
            // 
            this.mainPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.mainPropertyGrid.Name = "mainPropertyGrid";
            this.mainPropertyGrid.Size = new System.Drawing.Size(971, 625);
            this.mainPropertyGrid.TabIndex = 0;
            this.mainPropertyGrid.ToolbarVisible = false;
            this.mainPropertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.mainPropertyGrid_PropertyValueChanged);
            // 
            // mainToolStrip
            // 
            this.mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToolStripButton,
            this.toolStripSeparator8,
            this.openFolderToolStripButton,
            this.openTasksToolStripButton,
            this.openEventsToolStripButton});
            this.mainToolStrip.Location = new System.Drawing.Point(0, 24);
            this.mainToolStrip.Name = "mainToolStrip";
            this.mainToolStrip.Size = new System.Drawing.Size(1220, 25);
            this.mainToolStrip.TabIndex = 32;
            // 
            // saveToolStripButton
            // 
            this.saveToolStripButton.Image = global::Seal.Properties.Resources.save;
            this.saveToolStripButton.ImageTransparentColor = System.Drawing.Color.Black;
            this.saveToolStripButton.Name = "saveToolStripButton";
            this.saveToolStripButton.Size = new System.Drawing.Size(51, 22);
            this.saveToolStripButton.Text = "Save";
            this.saveToolStripButton.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(6, 25);
            // 
            // openFolderToolStripButton
            // 
            this.openFolderToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("openFolderToolStripButton.Image")));
            this.openFolderToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openFolderToolStripButton.Name = "openFolderToolStripButton";
            this.openFolderToolStripButton.Size = new System.Drawing.Size(92, 22);
            this.openFolderToolStripButton.Text = "Open Folder";
            this.openFolderToolStripButton.ToolTipText = "Open Repository Folder in Windows Explorer";
            this.openFolderToolStripButton.Click += new System.EventHandler(this.openFolderToolStripButton_Click);
            // 
            // openTasksToolStripButton
            // 
            this.openTasksToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("openTasksToolStripButton.Image")));
            this.openTasksToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            this.openTasksToolStripButton.Name = "openTasksToolStripButton";
            this.openTasksToolStripButton.Size = new System.Drawing.Size(109, 22);
            this.openTasksToolStripButton.Text = " Task Scheduler";
            this.openTasksToolStripButton.ToolTipText = "Run the Task Scheduler Microsoft Management Console";
            this.openTasksToolStripButton.Click += new System.EventHandler(this.openTasksToolStripButton_Click);
            // 
            // openEventsToolStripButton
            // 
            this.openEventsToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("openEventsToolStripButton.Image")));
            this.openEventsToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            this.openEventsToolStripButton.Name = "openEventsToolStripButton";
            this.openEventsToolStripButton.Size = new System.Drawing.Size(94, 22);
            this.openEventsToolStripButton.Text = "Event Viewer";
            this.openEventsToolStripButton.ToolTipText = "Run the Event Viewer Microsoft Management Console";
            this.openEventsToolStripButton.Click += new System.EventHandler(this.openTasksToolStripButton_Click);
            // 
            // ServerManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1220, 677);
            this.Controls.Add(this.mainToolStrip);
            this.Controls.Add(this.mainSplitContainer);
            this.Controls.Add(this.mainMenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.mainMenuStrip;
            this.Name = "ServerManager";
            this.Text = "Seal Server Manager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ServerManager_FormClosing);
            this.Load += new System.EventHandler(this.ServerManager_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ServerManager_KeyDown);
            this.mainMenuStrip.ResumeLayout(false);
            this.mainMenuStrip.PerformLayout();
            this.treeContextMenuStrip.ResumeLayout(false);
            this.mainSplitContainer.Panel1.ResumeLayout(false);
            this.mainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).EndInit();
            this.mainSplitContainer.ResumeLayout(false);
            this.mainToolStrip.ResumeLayout(false);
            this.mainToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip mainMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.TreeView mainTreeView;
        private System.Windows.Forms.SplitContainer mainSplitContainer;
        private System.Windows.Forms.ContextMenuStrip treeContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openSourceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openDeviceToolStripMenuItem;
        private System.Windows.Forms.PropertyGrid mainPropertyGrid;
        private System.Windows.Forms.ImageList mainImageList;
        private System.Windows.Forms.ToolStrip mainToolStrip;
        private System.Windows.Forms.ToolStripButton saveToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem addFromToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton openFolderToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dataSourceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem emailOutputDeviceToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton openEventsToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem configurationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeRootToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton openTasksToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem sortColumnAlphaOrderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sortColumnSQLOrderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noSQLdataSourceToolStripMenuItem;
    }
}

