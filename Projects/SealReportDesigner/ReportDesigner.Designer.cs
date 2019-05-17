namespace Seal
{
    partial class ReportDesigner
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReportDesigner));
            this.mainMenuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.executeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.executeViewOutputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renderViewOutputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.MRUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showScriptErrorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.schedulesWithCurrentUserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.treeContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFromToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeRootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sortColumnAlphaOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sortColumnSQLOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainToolStrip = new System.Windows.Forms.ToolStrip();
            this.newToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.executeToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.renderToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.executeViewOutputToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.renderViewOutputToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.mainTreeView = new System.Windows.Forms.TreeView();
            this.mainImageList = new System.Windows.Forms.ImageList(this.components);
            this.mainPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.mainTimer = new System.Windows.Forms.Timer(this.components);
            this.mainMenuStrip.SuspendLayout();
            this.treeContextMenuStrip.SuspendLayout();
            this.mainToolStrip.SuspendLayout();
            this.mainPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).BeginInit();
            this.mainSplitContainer.Panel1.SuspendLayout();
            this.mainSplitContainer.Panel2.SuspendLayout();
            this.mainSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenuStrip
            // 
            this.mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem,
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
            this.openToolStripMenuItem,
            this.reloadToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.closeToolStripMenuItem,
            this.toolStripSeparator2,
            this.executeToolStripMenuItem,
            this.renderToolStripMenuItem,
            this.executeViewOutputToolStripMenuItem,
            this.renderViewOutputToolStripMenuItem,
            this.toolStripSeparator3,
            this.MRUToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Image = global::Seal.Properties.Resources._new;
            this.newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.newToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.newToolStripMenuItem.Text = "New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Image = global::Seal.Properties.Resources.open;
            this.openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // reloadToolStripMenuItem
            // 
            this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            this.reloadToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.reloadToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.reloadToolStripMenuItem.Text = "Reload";
            this.reloadToolStripMenuItem.Click += new System.EventHandler(this.reloadToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Image = global::Seal.Properties.Resources.save;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.ShortcutKeyDisplayString = "";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(164, 6);
            // 
            // executeToolStripMenuItem
            // 
            this.executeToolStripMenuItem.Image = global::Seal.Properties.Resources.execute;
            this.executeToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            this.executeToolStripMenuItem.Name = "executeToolStripMenuItem";
            this.executeToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.executeToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.executeToolStripMenuItem.Text = "Execute";
            this.executeToolStripMenuItem.Click += new System.EventHandler(this.executeToolStripMenuItem_Click);
            // 
            // renderToolStripMenuItem
            // 
            this.renderToolStripMenuItem.Image = global::Seal.Properties.Resources.render;
            this.renderToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            this.renderToolStripMenuItem.Name = "renderToolStripMenuItem";
            this.renderToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F6;
            this.renderToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.renderToolStripMenuItem.Text = "Render";
            this.renderToolStripMenuItem.Click += new System.EventHandler(this.executeToolStripMenuItem_Click);
            // 
            // executeViewOutputToolStripMenuItem
            // 
            this.executeViewOutputToolStripMenuItem.Image = global::Seal.Properties.Resources.execute;
            this.executeViewOutputToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            this.executeViewOutputToolStripMenuItem.Name = "executeViewOutputToolStripMenuItem";
            this.executeViewOutputToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F10;
            this.executeViewOutputToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.executeViewOutputToolStripMenuItem.Text = "Execute View";
            this.executeViewOutputToolStripMenuItem.Click += new System.EventHandler(this.executeToolStripMenuItem_Click);
            // 
            // renderViewOutputToolStripMenuItem
            // 
            this.renderViewOutputToolStripMenuItem.Image = global::Seal.Properties.Resources.render;
            this.renderViewOutputToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            this.renderViewOutputToolStripMenuItem.Name = "renderViewOutputToolStripMenuItem";
            this.renderViewOutputToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F11;
            this.renderViewOutputToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.renderViewOutputToolStripMenuItem.Text = "Render View";
            this.renderViewOutputToolStripMenuItem.Click += new System.EventHandler(this.executeToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(164, 6);
            // 
            // MRUToolStripMenuItem
            // 
            this.MRUToolStripMenuItem.Name = "MRUToolStripMenuItem";
            this.MRUToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.MRUToolStripMenuItem.Text = "Recent Reports";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(164, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Image = global::Seal.Properties.Resources.exit;
            this.exitToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showScriptErrorsToolStripMenuItem,
            this.schedulesWithCurrentUserToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // showScriptErrorsToolStripMenuItem
            // 
            this.showScriptErrorsToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.showScriptErrorsToolStripMenuItem.Name = "showScriptErrorsToolStripMenuItem";
            this.showScriptErrorsToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            this.showScriptErrorsToolStripMenuItem.Text = "Show JavaScript errors";
            this.showScriptErrorsToolStripMenuItem.ToolTipText = "If checked, JavaScript errors are displayed in the report viewer.";
            this.showScriptErrorsToolStripMenuItem.Click += new System.EventHandler(this.showScriptErrorsToolStripMenuItem_Click);
            // 
            // schedulesWithCurrentUserToolStripMenuItem
            // 
            this.schedulesWithCurrentUserToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.schedulesWithCurrentUserToolStripMenuItem.Name = "schedulesWithCurrentUserToolStripMenuItem";
            this.schedulesWithCurrentUserToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            this.schedulesWithCurrentUserToolStripMenuItem.Text = "Schedule reports using current user";
            this.schedulesWithCurrentUserToolStripMenuItem.ToolTipText = "If checked, the schedules are created with the current logged user.\r\nNo administr" +
    "ators rights are required in this case.";
            this.schedulesWithCurrentUserToolStripMenuItem.Click += new System.EventHandler(this.schedulesWithCurrentUserToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
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
            this.removeToolStripMenuItem.Text = "Remove";
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
            // mainToolStrip
            // 
            this.mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripButton,
            this.openToolStripButton,
            this.saveToolStripButton,
            this.toolStripSeparator8,
            this.executeToolStripButton,
            this.renderToolStripButton,
            this.executeViewOutputToolStripButton,
            this.renderViewOutputToolStripButton});
            this.mainToolStrip.Location = new System.Drawing.Point(0, 24);
            this.mainToolStrip.Name = "mainToolStrip";
            this.mainToolStrip.Size = new System.Drawing.Size(1220, 25);
            this.mainToolStrip.TabIndex = 31;
            // 
            // newToolStripButton
            // 
            this.newToolStripButton.Image = global::Seal.Properties.Resources._new;
            this.newToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.newToolStripButton.Name = "newToolStripButton";
            this.newToolStripButton.Size = new System.Drawing.Size(51, 22);
            this.newToolStripButton.Text = "New";
            this.newToolStripButton.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripButton
            // 
            this.openToolStripButton.Image = global::Seal.Properties.Resources.open;
            this.openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripButton.Name = "openToolStripButton";
            this.openToolStripButton.Size = new System.Drawing.Size(56, 22);
            this.openToolStripButton.Text = "Open";
            this.openToolStripButton.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
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
            // executeToolStripButton
            // 
            this.executeToolStripButton.Image = global::Seal.Properties.Resources.execute;
            this.executeToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            this.executeToolStripButton.Name = "executeToolStripButton";
            this.executeToolStripButton.Size = new System.Drawing.Size(67, 22);
            this.executeToolStripButton.Text = "Execute";
            this.executeToolStripButton.ToolTipText = "F5 Execute the report";
            this.executeToolStripButton.Click += new System.EventHandler(this.executeToolStripMenuItem_Click);
            // 
            // renderToolStripButton
            // 
            this.renderToolStripButton.Image = global::Seal.Properties.Resources.render;
            this.renderToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.renderToolStripButton.Name = "renderToolStripButton";
            this.renderToolStripButton.Size = new System.Drawing.Size(64, 22);
            this.renderToolStripButton.Text = "Render";
            this.renderToolStripButton.ToolTipText = "F6 Execute the report using the models of the previous execution";
            this.renderToolStripButton.Click += new System.EventHandler(this.executeToolStripMenuItem_Click);
            // 
            // executeViewOutputToolStripButton
            // 
            this.executeViewOutputToolStripButton.Image = global::Seal.Properties.Resources.execute;
            this.executeViewOutputToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            this.executeViewOutputToolStripButton.Name = "executeViewOutputToolStripButton";
            this.executeViewOutputToolStripButton.Size = new System.Drawing.Size(95, 22);
            this.executeViewOutputToolStripButton.Text = "Execute View";
            this.executeViewOutputToolStripButton.ToolTipText = "Execute the report using the selected view";
            this.executeViewOutputToolStripButton.Click += new System.EventHandler(this.executeToolStripMenuItem_Click);
            // 
            // renderViewOutputToolStripButton
            // 
            this.renderViewOutputToolStripButton.Image = global::Seal.Properties.Resources.render;
            this.renderViewOutputToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.renderViewOutputToolStripButton.Name = "renderViewOutputToolStripButton";
            this.renderViewOutputToolStripButton.Size = new System.Drawing.Size(92, 22);
            this.renderViewOutputToolStripButton.Text = "Render View";
            this.renderViewOutputToolStripButton.ToolTipText = "Execute the report using the models of the previous execution and the selected vi" +
    "ew";
            this.renderViewOutputToolStripButton.Click += new System.EventHandler(this.executeToolStripMenuItem_Click);
            // 
            // mainPanel
            // 
            this.mainPanel.Controls.Add(this.mainSplitContainer);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 49);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(1220, 628);
            this.mainPanel.TabIndex = 32;
            // 
            // mainSplitContainer
            // 
            this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            this.mainSplitContainer.Panel1.Controls.Add(this.mainTreeView);
            // 
            // mainSplitContainer.Panel2
            // 
            this.mainSplitContainer.Panel2.Controls.Add(this.mainPropertyGrid);
            this.mainSplitContainer.Size = new System.Drawing.Size(1220, 628);
            this.mainSplitContainer.SplitterDistance = 206;
            this.mainSplitContainer.TabIndex = 4;
            // 
            // mainTreeView
            // 
            this.mainTreeView.AllowDrop = true;
            this.mainTreeView.ContextMenuStrip = this.treeContextMenuStrip;
            this.mainTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTreeView.HideSelection = false;
            this.mainTreeView.ImageIndex = 0;
            this.mainTreeView.ImageList = this.mainImageList;
            this.mainTreeView.LabelEdit = true;
            this.mainTreeView.Location = new System.Drawing.Point(0, 0);
            this.mainTreeView.Name = "mainTreeView";
            this.mainTreeView.SelectedImageIndex = 0;
            this.mainTreeView.Size = new System.Drawing.Size(206, 628);
            this.mainTreeView.TabIndex = 1;
            this.mainTreeView.BeforeLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.mainTreeView_BeforeLabelEdit);
            this.mainTreeView.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.mainTreeView_AfterLabelEdit);
            this.mainTreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.mainTreeView_ItemDrag);
            this.mainTreeView.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.mainTreeView_BeforeSelect);
            this.mainTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.mainTreeView_AfterSelect);
            this.mainTreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.mainTreeView_NodeMouseClick);
            this.mainTreeView.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.mainTreeView_NodeMouseDoubleClick);
            this.mainTreeView.DragDrop += new System.Windows.Forms.DragEventHandler(this.mainTreeView_DragDrop);
            this.mainTreeView.DragEnter += new System.Windows.Forms.DragEventHandler(this.mainTreeView_DragEnter);
            this.mainTreeView.DragOver += new System.Windows.Forms.DragEventHandler(this.mainTreeView_DragOver);
            this.mainTreeView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.mainTreeView_MouseUp);
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
            this.mainImageList.Images.SetKeyName(8, "view.png");
            this.mainImageList.Images.SetKeyName(9, "device.png");
            this.mainImageList.Images.SetKeyName(10, "model.png");
            this.mainImageList.Images.SetKeyName(11, "schedule.png");
            this.mainImageList.Images.SetKeyName(12, "task.png");
            this.mainImageList.Images.SetKeyName(13, "nosql.png");
            this.mainImageList.Images.SetKeyName(14, "task2.png");
            this.mainImageList.Images.SetKeyName(15, "modelSQL.png");
            // 
            // mainPropertyGrid
            // 
            this.mainPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.mainPropertyGrid.Name = "mainPropertyGrid";
            this.mainPropertyGrid.Size = new System.Drawing.Size(1010, 628);
            this.mainPropertyGrid.TabIndex = 1;
            this.mainPropertyGrid.ToolbarVisible = false;
            this.mainPropertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.mainPropertyGrid_PropertyValueChanged);
            // 
            // mainTimer
            // 
            this.mainTimer.Enabled = true;
            this.mainTimer.Interval = 500;
            this.mainTimer.Tick += new System.EventHandler(this.mainTimer_Tick);
            // 
            // ReportDesigner
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1220, 677);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.mainToolStrip);
            this.Controls.Add(this.mainMenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.mainMenuStrip;
            this.Name = "ReportDesigner";
            this.Text = "Seal Report Designer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ReportDesigner_FormClosing);
            this.Load += new System.EventHandler(this.ReportDesigner_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ReportDesigner_KeyDown);
            this.mainMenuStrip.ResumeLayout(false);
            this.mainMenuStrip.PerformLayout();
            this.treeContextMenuStrip.ResumeLayout(false);
            this.mainToolStrip.ResumeLayout(false);
            this.mainToolStrip.PerformLayout();
            this.mainPanel.ResumeLayout(false);
            this.mainSplitContainer.Panel1.ResumeLayout(false);
            this.mainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).EndInit();
            this.mainSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip mainMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem executeToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip treeContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStrip mainToolStrip;
        private System.Windows.Forms.ToolStripButton newToolStripButton;
        private System.Windows.Forms.ToolStripButton openToolStripButton;
        private System.Windows.Forms.ToolStripButton renderToolStripButton;
        private System.Windows.Forms.ToolStripButton saveToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem MRUToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton executeToolStripButton;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.SplitContainer mainSplitContainer;
        private System.Windows.Forms.TreeView mainTreeView;
        private System.Windows.Forms.ToolStripMenuItem addFromToolStripMenuItem;
        private System.Windows.Forms.PropertyGrid mainPropertyGrid;
        private System.Windows.Forms.ImageList mainImageList;
        private System.Windows.Forms.Timer mainTimer;
        private System.Windows.Forms.ToolStripMenuItem renderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton executeViewOutputToolStripButton;
        private System.Windows.Forms.ToolStripButton renderViewOutputToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem executeViewOutputToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem renderViewOutputToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeRootToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showScriptErrorsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem schedulesWithCurrentUserToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sortColumnAlphaOrderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sortColumnSQLOrderToolStripMenuItem;
    }
}

