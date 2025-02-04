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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReportDesigner));
            mainMenuStrip = new System.Windows.Forms.MenuStrip();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openRepositoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            executeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            renderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            executeViewOutputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            renderViewOutputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            MRUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            showScriptErrorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            schedulesWithCurrentUserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            treeContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(components);
            addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            addFromToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            removeRootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sortColumnAlphaOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sortColumnSQLOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            resetDisplayOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            mainToolStrip = new System.Windows.Forms.ToolStrip();
            newToolStripButton = new System.Windows.Forms.ToolStripButton();
            openToolStripButton = new System.Windows.Forms.ToolStripButton();
            saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            executeToolStripButton = new System.Windows.Forms.ToolStripButton();
            renderToolStripButton = new System.Windows.Forms.ToolStripButton();
            executeViewOutputToolStripButton = new System.Windows.Forms.ToolStripButton();
            renderViewOutputToolStripButton = new System.Windows.Forms.ToolStripButton();
            mainPanel = new System.Windows.Forms.Panel();
            mainSplitContainer = new System.Windows.Forms.SplitContainer();
            mainTreeView = new System.Windows.Forms.TreeView();
            mainImageList = new System.Windows.Forms.ImageList(components);
            mainPropertyGrid = new System.Windows.Forms.PropertyGrid();
            mainTimer = new System.Windows.Forms.Timer(components);
            mainMenuStrip.SuspendLayout();
            treeContextMenuStrip.SuspendLayout();
            mainToolStrip.SuspendLayout();
            mainPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            SuspendLayout();
            // 
            // mainMenuStrip
            // 
            mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileToolStripMenuItem, optionsToolStripMenuItem, toolsToolStripMenuItem, helpToolStripMenuItem });
            mainMenuStrip.Location = new System.Drawing.Point(0, 0);
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            mainMenuStrip.Size = new System.Drawing.Size(1423, 24);
            mainMenuStrip.TabIndex = 0;
            mainMenuStrip.Text = "mainMenuStrip";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { newToolStripMenuItem, openToolStripMenuItem, openRepositoryToolStripMenuItem, reloadToolStripMenuItem, saveToolStripMenuItem, saveAsToolStripMenuItem, closeToolStripMenuItem, toolStripSeparator2, executeToolStripMenuItem, renderToolStripMenuItem, executeViewOutputToolStripMenuItem, renderViewOutputToolStripMenuItem, toolStripSeparator3, MRUToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            fileToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Image = Properties.Resources._new;
            newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N;
            newToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            newToolStripMenuItem.Text = "New";
            newToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Image = Properties.Resources.open;
            openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O;
            openToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            openToolStripMenuItem.Text = "Open (Last)";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // openRepositoryToolStripMenuItem
            // 
            openRepositoryToolStripMenuItem.Name = "openRepositoryToolStripMenuItem";
            openRepositoryToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            openRepositoryToolStripMenuItem.Text = "Open (Reports)";
            openRepositoryToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // reloadToolStripMenuItem
            // 
            reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            reloadToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R;
            reloadToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            reloadToolStripMenuItem.Text = "Reload";
            reloadToolStripMenuItem.Click += reloadToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Image = Properties.Resources.save;
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S;
            saveToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.ShortcutKeyDisplayString = "";
            saveAsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            saveAsToolStripMenuItem.Text = "Save As...";
            saveAsToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            closeToolStripMenuItem.Text = "Close";
            closeToolStripMenuItem.Click += closeToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(177, 6);
            // 
            // executeToolStripMenuItem
            // 
            executeToolStripMenuItem.Image = Properties.Resources.execute;
            executeToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            executeToolStripMenuItem.Name = "executeToolStripMenuItem";
            executeToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            executeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            executeToolStripMenuItem.Text = "Execute";
            executeToolStripMenuItem.Click += executeToolStripMenuItem_Click;
            // 
            // renderToolStripMenuItem
            // 
            renderToolStripMenuItem.Image = Properties.Resources.render;
            renderToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            renderToolStripMenuItem.Name = "renderToolStripMenuItem";
            renderToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F6;
            renderToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            renderToolStripMenuItem.Text = "Render";
            renderToolStripMenuItem.Click += executeToolStripMenuItem_Click;
            // 
            // executeViewOutputToolStripMenuItem
            // 
            executeViewOutputToolStripMenuItem.Image = Properties.Resources.execute;
            executeViewOutputToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            executeViewOutputToolStripMenuItem.Name = "executeViewOutputToolStripMenuItem";
            executeViewOutputToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F10;
            executeViewOutputToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            executeViewOutputToolStripMenuItem.Text = "Execute View";
            executeViewOutputToolStripMenuItem.Click += executeToolStripMenuItem_Click;
            // 
            // renderViewOutputToolStripMenuItem
            // 
            renderViewOutputToolStripMenuItem.Image = Properties.Resources.render;
            renderViewOutputToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.White;
            renderViewOutputToolStripMenuItem.Name = "renderViewOutputToolStripMenuItem";
            renderViewOutputToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F11;
            renderViewOutputToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            renderViewOutputToolStripMenuItem.Text = "Render View";
            renderViewOutputToolStripMenuItem.Click += executeToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new System.Drawing.Size(177, 6);
            // 
            // MRUToolStripMenuItem
            // 
            MRUToolStripMenuItem.Name = "MRUToolStripMenuItem";
            MRUToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            MRUToolStripMenuItem.Text = "Recent Reports";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Image = Properties.Resources.exit;
            exitToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { showScriptErrorsToolStripMenuItem, schedulesWithCurrentUserToolStripMenuItem });
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            optionsToolStripMenuItem.Text = "Options";
            // 
            // showScriptErrorsToolStripMenuItem
            // 
            showScriptErrorsToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            showScriptErrorsToolStripMenuItem.Name = "showScriptErrorsToolStripMenuItem";
            showScriptErrorsToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            showScriptErrorsToolStripMenuItem.Text = "Show Edge DevTools";
            showScriptErrorsToolStripMenuItem.ToolTipText = "If checked, the Edge DevTools Window is shown when a report is executed.";
            showScriptErrorsToolStripMenuItem.Click += showScriptErrorsToolStripMenuItem_Click;
            // 
            // schedulesWithCurrentUserToolStripMenuItem
            // 
            schedulesWithCurrentUserToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            schedulesWithCurrentUserToolStripMenuItem.Name = "schedulesWithCurrentUserToolStripMenuItem";
            schedulesWithCurrentUserToolStripMenuItem.Size = new System.Drawing.Size(260, 22);
            schedulesWithCurrentUserToolStripMenuItem.Text = "Schedule reports using current user";
            schedulesWithCurrentUserToolStripMenuItem.ToolTipText = "If checked, the schedules are created with the current logged user.\r\nNo administrators rights are required in this case.";
            schedulesWithCurrentUserToolStripMenuItem.Click += schedulesWithCurrentUserToolStripMenuItem_Click;
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
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
            // treeContextMenuStrip
            // 
            treeContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { addToolStripMenuItem, removeToolStripMenuItem, addFromToolStripMenuItem, copyToolStripMenuItem, removeRootToolStripMenuItem, sortColumnAlphaOrderToolStripMenuItem, sortColumnSQLOrderToolStripMenuItem, resetDisplayOrderToolStripMenuItem });
            treeContextMenuStrip.Name = "treeContextMenuStrip";
            treeContextMenuStrip.Size = new System.Drawing.Size(233, 180);
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
            removeToolStripMenuItem.Text = "Remove";
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
            // mainToolStrip
            // 
            mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { newToolStripButton, openToolStripButton, saveToolStripButton, toolStripSeparator8, executeToolStripButton, renderToolStripButton, executeViewOutputToolStripButton, renderViewOutputToolStripButton });
            mainToolStrip.Location = new System.Drawing.Point(0, 24);
            mainToolStrip.Name = "mainToolStrip";
            mainToolStrip.Size = new System.Drawing.Size(1423, 25);
            mainToolStrip.TabIndex = 31;
            // 
            // newToolStripButton
            // 
            newToolStripButton.Image = Properties.Resources._new;
            newToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            newToolStripButton.Name = "newToolStripButton";
            newToolStripButton.Size = new System.Drawing.Size(51, 22);
            newToolStripButton.Text = "New";
            newToolStripButton.Click += newToolStripMenuItem_Click;
            // 
            // openToolStripButton
            // 
            openToolStripButton.Image = Properties.Resources.open;
            openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            openToolStripButton.Name = "openToolStripButton";
            openToolStripButton.Size = new System.Drawing.Size(56, 22);
            openToolStripButton.Text = "Open";
            openToolStripButton.Click += openToolStripMenuItem_Click;
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
            // executeToolStripButton
            // 
            executeToolStripButton.Image = Properties.Resources.execute;
            executeToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            executeToolStripButton.Name = "executeToolStripButton";
            executeToolStripButton.Size = new System.Drawing.Size(68, 22);
            executeToolStripButton.Text = "Execute";
            executeToolStripButton.ToolTipText = "F5 Execute the report";
            executeToolStripButton.Click += executeToolStripMenuItem_Click;
            // 
            // renderToolStripButton
            // 
            renderToolStripButton.Image = Properties.Resources.render;
            renderToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            renderToolStripButton.Name = "renderToolStripButton";
            renderToolStripButton.Size = new System.Drawing.Size(64, 22);
            renderToolStripButton.Text = "Render";
            renderToolStripButton.ToolTipText = "F6 Execute the report using the models of the previous execution";
            renderToolStripButton.Click += executeToolStripMenuItem_Click;
            // 
            // executeViewOutputToolStripButton
            // 
            executeViewOutputToolStripButton.Image = Properties.Resources.execute;
            executeViewOutputToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            executeViewOutputToolStripButton.Name = "executeViewOutputToolStripButton";
            executeViewOutputToolStripButton.Size = new System.Drawing.Size(96, 22);
            executeViewOutputToolStripButton.Text = "Execute View";
            executeViewOutputToolStripButton.ToolTipText = "Execute the report using the selected view";
            executeViewOutputToolStripButton.Click += executeToolStripMenuItem_Click;
            // 
            // renderViewOutputToolStripButton
            // 
            renderViewOutputToolStripButton.Image = Properties.Resources.render;
            renderViewOutputToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            renderViewOutputToolStripButton.Name = "renderViewOutputToolStripButton";
            renderViewOutputToolStripButton.Size = new System.Drawing.Size(92, 22);
            renderViewOutputToolStripButton.Text = "Render View";
            renderViewOutputToolStripButton.ToolTipText = "Execute the report using the models of the previous execution and the selected view";
            renderViewOutputToolStripButton.Click += executeToolStripMenuItem_Click;
            // 
            // mainPanel
            // 
            mainPanel.Controls.Add(mainSplitContainer);
            mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            mainPanel.Location = new System.Drawing.Point(0, 49);
            mainPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new System.Drawing.Size(1423, 732);
            mainPanel.TabIndex = 32;
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            mainSplitContainer.Location = new System.Drawing.Point(0, 0);
            mainSplitContainer.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            mainSplitContainer.Panel1.Controls.Add(mainTreeView);
            // 
            // mainSplitContainer.Panel2
            // 
            mainSplitContainer.Panel2.Controls.Add(mainPropertyGrid);
            mainSplitContainer.Size = new System.Drawing.Size(1423, 732);
            mainSplitContainer.SplitterDistance = 240;
            mainSplitContainer.SplitterWidth = 5;
            mainSplitContainer.TabIndex = 4;
            // 
            // mainTreeView
            // 
            mainTreeView.AllowDrop = true;
            mainTreeView.ContextMenuStrip = treeContextMenuStrip;
            mainTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            mainTreeView.HideSelection = false;
            mainTreeView.ImageIndex = 0;
            mainTreeView.ImageList = mainImageList;
            mainTreeView.LabelEdit = true;
            mainTreeView.Location = new System.Drawing.Point(0, 0);
            mainTreeView.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            mainTreeView.Name = "mainTreeView";
            mainTreeView.SelectedImageIndex = 0;
            mainTreeView.Size = new System.Drawing.Size(240, 732);
            mainTreeView.TabIndex = 1;
            mainTreeView.BeforeLabelEdit += mainTreeView_BeforeLabelEdit;
            mainTreeView.AfterLabelEdit += mainTreeView_AfterLabelEdit;
            mainTreeView.BeforeCollapse += mainTreeView_BeforeCollapse;
            mainTreeView.BeforeExpand += mainTreeView_BeforeExpand;
            mainTreeView.ItemDrag += mainTreeView_ItemDrag;
            mainTreeView.BeforeSelect += mainTreeView_BeforeSelect;
            mainTreeView.AfterSelect += mainTreeView_AfterSelect;
            mainTreeView.NodeMouseClick += mainTreeView_NodeMouseClick;
            mainTreeView.NodeMouseDoubleClick += mainTreeView_NodeMouseDoubleClick;
            mainTreeView.DragDrop += mainTreeView_DragDrop;
            mainTreeView.DragEnter += mainTreeView_DragEnter;
            mainTreeView.DragOver += mainTreeView_DragOver;
            mainTreeView.MouseDown += mainTreeView_MouseDown;
            mainTreeView.MouseUp += mainTreeView_MouseUp;
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
            mainImageList.Images.SetKeyName(8, "view.png");
            mainImageList.Images.SetKeyName(9, "device.png");
            mainImageList.Images.SetKeyName(10, "model.png");
            mainImageList.Images.SetKeyName(11, "schedule.png");
            mainImageList.Images.SetKeyName(12, "task.png");
            mainImageList.Images.SetKeyName(13, "nosql.png");
            mainImageList.Images.SetKeyName(14, "task2.png");
            mainImageList.Images.SetKeyName(15, "modelSQL.png");
            mainImageList.Images.SetKeyName(16, "configuration.png");
            mainImageList.Images.SetKeyName(17, "modelLINQ.png");
            mainImageList.Images.SetKeyName(18, "viewModel.png");
            mainImageList.Images.SetKeyName(19, "viewWidget.png");
            mainImageList.Images.SetKeyName(20, "view2.png");
            mainImageList.Images.SetKeyName(21, "viewModel2.png");
            mainImageList.Images.SetKeyName(22, "viewWidget2.png");
            // 
            // mainPropertyGrid
            // 
            mainPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            mainPropertyGrid.Location = new System.Drawing.Point(0, 0);
            mainPropertyGrid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            mainPropertyGrid.Name = "mainPropertyGrid";
            mainPropertyGrid.Size = new System.Drawing.Size(1178, 732);
            mainPropertyGrid.TabIndex = 1;
            mainPropertyGrid.ToolbarVisible = false;
            mainPropertyGrid.PropertyValueChanged += mainPropertyGrid_PropertyValueChanged;
            // 
            // mainTimer
            // 
            mainTimer.Enabled = true;
            mainTimer.Interval = 500;
            mainTimer.Tick += mainTimer_Tick;
            // 
            // ReportDesigner
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1423, 781);
            Controls.Add(mainPanel);
            Controls.Add(mainToolStrip);
            Controls.Add(mainMenuStrip);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = mainMenuStrip;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "ReportDesigner";
            Text = "Seal Report Designer";
            FormClosing += ReportDesigner_FormClosing;
            Load += ReportDesigner_Load;
            KeyDown += ReportDesigner_KeyDown;
            mainMenuStrip.ResumeLayout(false);
            mainMenuStrip.PerformLayout();
            treeContextMenuStrip.ResumeLayout(false);
            mainToolStrip.ResumeLayout(false);
            mainToolStrip.PerformLayout();
            mainPanel.ResumeLayout(false);
            mainSplitContainer.Panel1.ResumeLayout(false);
            mainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
            mainSplitContainer.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
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
        private System.Windows.Forms.ToolStripMenuItem resetDisplayOrderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openRepositoryToolStripMenuItem;
    }
}

