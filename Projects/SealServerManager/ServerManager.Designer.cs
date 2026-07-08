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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerManager));
            mainMenuStrip = new System.Windows.Forms.MenuStrip();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            dataSourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            noSQLdataSourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            emailOutputDeviceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            fileServerDeviceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sharePointDeviceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openSourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openDeviceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            mainTreeView = new System.Windows.Forms.TreeView();
            treeContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(components);
            addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            addFromToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            removeRootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sortColumnAlphaOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sortColumnSQLOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            resetDisplayOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            mainImageList = new System.Windows.Forms.ImageList(components);
            mainSplitContainer = new System.Windows.Forms.SplitContainer();
            treeViewFilter = new System.Windows.Forms.TextBox();
            buttonResetFilter = new System.Windows.Forms.Button();
            mainPropertyGrid = new System.Windows.Forms.PropertyGrid();
            mainToolStrip = new System.Windows.Forms.ToolStrip();
            saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            openFolderToolStripButton = new System.Windows.Forms.ToolStripButton();
            openTasksToolStripButton = new System.Windows.Forms.ToolStripButton();
            openEventsToolStripButton = new System.Windows.Forms.ToolStripButton();
            mainMenuStrip.SuspendLayout();
            treeContextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            mainToolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // mainMenuStrip
            // 
            mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileToolStripMenuItem, configurationToolStripMenuItem, toolsToolStripMenuItem, helpToolStripMenuItem });
            mainMenuStrip.Location = new System.Drawing.Point(0, 0);
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            mainMenuStrip.Size = new System.Drawing.Size(1423, 24);
            mainMenuStrip.TabIndex = 0;
            mainMenuStrip.Text = "mainMenuStrip";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { newToolStripMenuItem, openSourceToolStripMenuItem, openDeviceToolStripMenuItem, reloadToolStripMenuItem, saveToolStripMenuItem, saveAsToolStripMenuItem, closeToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            fileToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { dataSourceToolStripMenuItem, noSQLdataSourceToolStripMenuItem, toolStripSeparator2, emailOutputDeviceToolStripMenuItem, fileServerDeviceToolStripMenuItem, sharePointDeviceToolStripMenuItem });
            newToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("newToolStripMenuItem.Image");
            newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            newToolStripMenuItem.Text = "New";
            // 
            // dataSourceToolStripMenuItem
            // 
            dataSourceToolStripMenuItem.Image = Properties.Resources.database;
            dataSourceToolStripMenuItem.Name = "dataSourceToolStripMenuItem";
            dataSourceToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            dataSourceToolStripMenuItem.Text = "SQL Data Source";
            dataSourceToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // noSQLdataSourceToolStripMenuItem
            // 
            noSQLdataSourceToolStripMenuItem.Image = Properties.Resources.nosql;
            noSQLdataSourceToolStripMenuItem.Name = "noSQLdataSourceToolStripMenuItem";
            noSQLdataSourceToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            noSQLdataSourceToolStripMenuItem.Text = "LINQ Data Source";
            noSQLdataSourceToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(203, 6);
            // 
            // emailOutputDeviceToolStripMenuItem
            // 
            emailOutputDeviceToolStripMenuItem.Image = Properties.Resources.device;
            emailOutputDeviceToolStripMenuItem.Name = "emailOutputDeviceToolStripMenuItem";
            emailOutputDeviceToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            emailOutputDeviceToolStripMenuItem.Text = "Email Output Device";
            emailOutputDeviceToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // fileServerDeviceToolStripMenuItem
            // 
            fileServerDeviceToolStripMenuItem.Image = Properties.Resources.fileserver;
            fileServerDeviceToolStripMenuItem.Name = "fileServerDeviceToolStripMenuItem";
            fileServerDeviceToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            fileServerDeviceToolStripMenuItem.Text = "File Server Output Device";
            fileServerDeviceToolStripMenuItem.Click += newToolStripMenuItem_Click;
            //
            // sharePointDeviceToolStripMenuItem
            //
            sharePointDeviceToolStripMenuItem.Image = Properties.Resources.fileserver;
            sharePointDeviceToolStripMenuItem.Name = "sharePointDeviceToolStripMenuItem";
            sharePointDeviceToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            sharePointDeviceToolStripMenuItem.Text = "SharePoint Output Device";
            sharePointDeviceToolStripMenuItem.Click += newToolStripMenuItem_Click;
            //
            // openSourceToolStripMenuItem
            // 
            openSourceToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("openSourceToolStripMenuItem.Image");
            openSourceToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            openSourceToolStripMenuItem.Name = "openSourceToolStripMenuItem";
            openSourceToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            openSourceToolStripMenuItem.Text = "Open Data Source";
            // 
            // openDeviceToolStripMenuItem
            // 
            openDeviceToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("openDeviceToolStripMenuItem.Image");
            openDeviceToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            openDeviceToolStripMenuItem.Name = "openDeviceToolStripMenuItem";
            openDeviceToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            openDeviceToolStripMenuItem.Text = "Open Output Device";
            // 
            // reloadToolStripMenuItem
            // 
            reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            reloadToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R;
            reloadToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            reloadToolStripMenuItem.Text = "Reload";
            reloadToolStripMenuItem.Click += reloadToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("saveToolStripMenuItem.Image");
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S;
            saveToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.ShortcutKeyDisplayString = "";
            saveAsToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            saveAsToolStripMenuItem.Text = "Save As...";
            saveAsToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            closeToolStripMenuItem.Text = "Close";
            closeToolStripMenuItem.Click += closeToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(179, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("exitToolStripMenuItem.Image");
            exitToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // configurationToolStripMenuItem
            // 
            configurationToolStripMenuItem.Name = "configurationToolStripMenuItem";
            configurationToolStripMenuItem.Size = new System.Drawing.Size(93, 20);
            configurationToolStripMenuItem.Text = "Configuration";
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            aboutToolStripMenuItem.Text = "About...";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // mainTreeView
            // 
            mainTreeView.AllowDrop = true;
            mainTreeView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mainTreeView.ContextMenuStrip = treeContextMenuStrip;
            mainTreeView.FullRowSelect = true;
            mainTreeView.HideSelection = false;
            mainTreeView.ImageIndex = 0;
            mainTreeView.ImageList = mainImageList;
            mainTreeView.Location = new System.Drawing.Point(4, 26);
            mainTreeView.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            mainTreeView.Name = "mainTreeView";
            mainTreeView.SelectedImageIndex = 0;
            mainTreeView.Size = new System.Drawing.Size(280, 692);
            mainTreeView.TabIndex = 1;
            mainTreeView.ItemDrag += mainTreeView_ItemDrag;
            mainTreeView.AfterSelect += mainTreeView_AfterSelect;
            mainTreeView.NodeMouseClick += mainTreeView_NodeMouseClick;
            mainTreeView.DragDrop += mainTreeView_DragDrop;
            mainTreeView.DragEnter += mainTreeView_DragEnter;
            mainTreeView.DragOver += mainTreeView_DragOver;
            mainTreeView.MouseUp += mainTreeView_MouseUp;
            // 
            // treeContextMenuStrip
            // 
            treeContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { addToolStripMenuItem, removeToolStripMenuItem, addFromToolStripMenuItem, copyToolStripMenuItem, removeRootToolStripMenuItem, sortColumnAlphaOrderToolStripMenuItem, sortColumnSQLOrderToolStripMenuItem, resetDisplayOrderToolStripMenuItem });
            treeContextMenuStrip.Name = "treeContextMenuStrip";
            treeContextMenuStrip.Size = new System.Drawing.Size(233, 180);
            treeContextMenuStrip.Opening += treeContextMenuStrip_Opening;
            // 
            // addToolStripMenuItem
            // 
            addToolStripMenuItem.Name = "addToolStripMenuItem";
            addToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            addToolStripMenuItem.Text = "Add";
            addToolStripMenuItem.Click += addToolStripMenuItem_Click;
            // 
            // removeToolStripMenuItem
            // 
            removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            removeToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            removeToolStripMenuItem.Text = "Remove...";
            removeToolStripMenuItem.Click += removeToolStripMenuItem_Click;
            // 
            // addFromToolStripMenuItem
            // 
            addFromToolStripMenuItem.Name = "addFromToolStripMenuItem";
            addFromToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            addFromToolStripMenuItem.Text = "Add from Catalog...";
            addFromToolStripMenuItem.Click += addFromToolStripMenuItem_Click;
            // 
            // copyToolStripMenuItem
            // 
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            copyToolStripMenuItem.Text = "Copy";
            copyToolStripMenuItem.Click += copyToolStripMenuItem_Click;
            // 
            // removeRootToolStripMenuItem
            // 
            removeRootToolStripMenuItem.Name = "removeRootToolStripMenuItem";
            removeRootToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            removeRootToolStripMenuItem.Text = "Remove Root";
            removeRootToolStripMenuItem.Click += removeRootToolStripMenuItem_Click;
            // 
            // sortColumnAlphaOrderToolStripMenuItem
            // 
            sortColumnAlphaOrderToolStripMenuItem.Name = "sortColumnAlphaOrderToolStripMenuItem";
            sortColumnAlphaOrderToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            sortColumnAlphaOrderToolStripMenuItem.Text = "Sort Columns by Name ";
            sortColumnAlphaOrderToolStripMenuItem.Click += sortColumnsToolStripMenuItem_Click;
            // 
            // sortColumnSQLOrderToolStripMenuItem
            // 
            sortColumnSQLOrderToolStripMenuItem.Name = "sortColumnSQLOrderToolStripMenuItem";
            sortColumnSQLOrderToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            sortColumnSQLOrderToolStripMenuItem.Text = "Sort Columns by SQL position";
            sortColumnSQLOrderToolStripMenuItem.Click += sortColumnsToolStripMenuItem_Click;
            // 
            // resetDisplayOrderToolStripMenuItem
            // 
            resetDisplayOrderToolStripMenuItem.Name = "resetDisplayOrderToolStripMenuItem";
            resetDisplayOrderToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            resetDisplayOrderToolStripMenuItem.Text = "Reset Display Order";
            resetDisplayOrderToolStripMenuItem.Click += resetDisplayOrderToolStripMenuItem_Click;
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
            mainImageList.Images.SetKeyName(8, "device.png");
            mainImageList.Images.SetKeyName(9, "nosql.png");
            mainImageList.Images.SetKeyName(10, "fileserver.png");
            mainImageList.Images.SetKeyName(11, "link.png");
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mainSplitContainer.Location = new System.Drawing.Point(0, 60);
            mainSplitContainer.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            mainSplitContainer.Panel1.Controls.Add(treeViewFilter);
            mainSplitContainer.Panel1.Controls.Add(buttonResetFilter);
            mainSplitContainer.Panel1.Controls.Add(mainTreeView);
            // 
            // mainSplitContainer.Panel2
            // 
            mainSplitContainer.Panel2.Controls.Add(mainPropertyGrid);
            mainSplitContainer.Size = new System.Drawing.Size(1423, 721);
            mainSplitContainer.SplitterDistance = 285;
            mainSplitContainer.SplitterWidth = 5;
            mainSplitContainer.TabIndex = 3;
            // 
            // treeViewFilter
            // 
            treeViewFilter.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            treeViewFilter.BorderStyle = System.Windows.Forms.BorderStyle.None;
            treeViewFilter.Location = new System.Drawing.Point(4, 5);
            treeViewFilter.Name = "treeViewFilter";
            treeViewFilter.Size = new System.Drawing.Size(228, 16);
            treeViewFilter.TabIndex = 4;
            // 
            // buttonResetFilter
            // 
            buttonResetFilter.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            buttonResetFilter.FlatAppearance.BorderSize = 0;
            buttonResetFilter.Location = new System.Drawing.Point(238, 2);
            buttonResetFilter.Name = "buttonResetFilter";
            buttonResetFilter.Size = new System.Drawing.Size(48, 24);
            buttonResetFilter.TabIndex = 5;
            buttonResetFilter.Text = "Reset";
            // 
            // mainPropertyGrid
            // 
            mainPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            mainPropertyGrid.Location = new System.Drawing.Point(0, 0);
            mainPropertyGrid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            mainPropertyGrid.Name = "mainPropertyGrid";
            mainPropertyGrid.Size = new System.Drawing.Size(1133, 721);
            mainPropertyGrid.TabIndex = 0;
            mainPropertyGrid.ToolbarVisible = false;
            mainPropertyGrid.PropertyValueChanged += mainPropertyGrid_PropertyValueChanged;
            // 
            // mainToolStrip
            // 
            mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { saveToolStripButton, toolStripSeparator8, openFolderToolStripButton, openTasksToolStripButton, openEventsToolStripButton });
            mainToolStrip.Location = new System.Drawing.Point(0, 24);
            mainToolStrip.Name = "mainToolStrip";
            mainToolStrip.Size = new System.Drawing.Size(1423, 25);
            mainToolStrip.TabIndex = 32;
            // 
            // saveToolStripButton
            // 
            saveToolStripButton.Image = Properties.Resources.save;
            saveToolStripButton.ImageTransparentColor = System.Drawing.Color.Black;
            saveToolStripButton.Name = "saveToolStripButton";
            saveToolStripButton.Size = new System.Drawing.Size(51, 22);
            saveToolStripButton.Text = "Save";
            saveToolStripButton.Click += saveToolStripMenuItem_Click;
            // 
            // toolStripSeparator8
            // 
            toolStripSeparator8.Name = "toolStripSeparator8";
            toolStripSeparator8.Size = new System.Drawing.Size(6, 25);
            // 
            // openFolderToolStripButton
            // 
            openFolderToolStripButton.Image = (System.Drawing.Image)resources.GetObject("openFolderToolStripButton.Image");
            openFolderToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            openFolderToolStripButton.Name = "openFolderToolStripButton";
            openFolderToolStripButton.Size = new System.Drawing.Size(92, 22);
            openFolderToolStripButton.Text = "Open Folder";
            openFolderToolStripButton.ToolTipText = "Open Repository Folder in Windows Explorer";
            openFolderToolStripButton.Click += openFolderToolStripButton_Click;
            // 
            // openTasksToolStripButton
            // 
            openTasksToolStripButton.Image = (System.Drawing.Image)resources.GetObject("openTasksToolStripButton.Image");
            openTasksToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            openTasksToolStripButton.Name = "openTasksToolStripButton";
            openTasksToolStripButton.Size = new System.Drawing.Size(108, 22);
            openTasksToolStripButton.Text = " Task Scheduler";
            openTasksToolStripButton.ToolTipText = "Run the Task Scheduler Microsoft Management Console";
            openTasksToolStripButton.Click += openTasksToolStripButton_Click;
            // 
            // openEventsToolStripButton
            // 
            openEventsToolStripButton.Image = (System.Drawing.Image)resources.GetObject("openEventsToolStripButton.Image");
            openEventsToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            openEventsToolStripButton.Name = "openEventsToolStripButton";
            openEventsToolStripButton.Size = new System.Drawing.Size(94, 22);
            openEventsToolStripButton.Text = "Event Viewer";
            openEventsToolStripButton.ToolTipText = "Run the Event Viewer Microsoft Management Console";
            openEventsToolStripButton.Click += openTasksToolStripButton_Click;
            // 
            // ServerManager
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1423, 781);
            Controls.Add(mainToolStrip);
            Controls.Add(mainSplitContainer);
            Controls.Add(mainMenuStrip);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = mainMenuStrip;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "ServerManager";
            Text = "Seal Server Manager";
            FormClosing += ServerManager_FormClosing;
            Load += ServerManager_Load;
            KeyDown += ServerManager_KeyDown;
            mainMenuStrip.ResumeLayout(false);
            mainMenuStrip.PerformLayout();
            treeContextMenuStrip.ResumeLayout(false);
            mainSplitContainer.Panel1.ResumeLayout(false);
            mainSplitContainer.Panel1.PerformLayout();
            mainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
            mainSplitContainer.ResumeLayout(false);
            mainToolStrip.ResumeLayout(false);
            mainToolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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
        private System.Windows.Forms.ToolStripMenuItem fileServerDeviceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sharePointDeviceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetDisplayOrderToolStripMenuItem;
        private System.Windows.Forms.TextBox treeViewFilter;
        private System.Windows.Forms.Button buttonResetFilter;
    }
}

