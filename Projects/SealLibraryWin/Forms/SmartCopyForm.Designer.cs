namespace Seal.Forms
{
    partial class SmartCopyForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SmartCopyForm));
            this.listPanel = new System.Windows.Forms.Panel();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.addRadioButton = new System.Windows.Forms.RadioButton();
            this.updateRadioButton = new System.Windows.Forms.RadioButton();
            this.sourcesGroupBox = new System.Windows.Forms.GroupBox();
            this.source3CheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.source2CheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.propertiesCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.filterTextBox = new System.Windows.Forms.TextBox();
            this.filterLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.doItButton = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.destinationCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.reportsListBox = new System.Windows.Forms.ListBox();
            this.removeReportContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeReportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addReportButton = new System.Windows.Forms.Button();
            this.mainToolStrip = new System.Windows.Forms.ToolStrip();
            this.cancelToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.listPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.panel1.SuspendLayout();
            this.sourcesGroupBox.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.removeReportContextMenuStrip.SuspendLayout();
            this.mainToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // listPanel
            // 
            this.listPanel.Controls.Add(this.splitContainer);
            this.listPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listPanel.Location = new System.Drawing.Point(0, 0);
            this.listPanel.Name = "listPanel";
            this.listPanel.Size = new System.Drawing.Size(820, 611);
            this.listPanel.TabIndex = 4;
            // 
            // splitContainer
            // 
            this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer.Location = new System.Drawing.Point(3, 28);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.panel1);
            this.splitContainer.Panel1.Controls.Add(this.sourcesGroupBox);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.filterTextBox);
            this.splitContainer.Panel2.Controls.Add(this.filterLabel);
            this.splitContainer.Panel2.Controls.Add(this.label2);
            this.splitContainer.Panel2.Controls.Add(this.label1);
            this.splitContainer.Panel2.Controls.Add(this.doItButton);
            this.splitContainer.Panel2.Controls.Add(this.groupBox3);
            this.splitContainer.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer.Size = new System.Drawing.Size(814, 580);
            this.splitContainer.SplitterDistance = 289;
            this.splitContainer.TabIndex = 3;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.addRadioButton);
            this.panel1.Controls.Add(this.updateRadioButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(810, 30);
            this.panel1.TabIndex = 9;
            // 
            // addRadioButton
            // 
            this.addRadioButton.AutoSize = true;
            this.addRadioButton.Location = new System.Drawing.Point(459, 5);
            this.addRadioButton.Name = "addRadioButton";
            this.addRadioButton.Size = new System.Drawing.Size(255, 17);
            this.addRadioButton.TabIndex = 5;
            this.addRadioButton.Text = "Add model to the reports selected in Destinations";
            this.addRadioButton.UseVisualStyleBackColor = true;
            this.addRadioButton.CheckedChanged += new System.EventHandler(this.addRadioButton_CheckedChanged);
            // 
            // updateRadioButton
            // 
            this.updateRadioButton.AutoSize = true;
            this.updateRadioButton.Checked = true;
            this.updateRadioButton.Location = new System.Drawing.Point(7, 5);
            this.updateRadioButton.Name = "updateRadioButton";
            this.updateRadioButton.Size = new System.Drawing.Size(446, 17);
            this.updateRadioButton.TabIndex = 6;
            this.updateRadioButton.TabStop = true;
            this.updateRadioButton.Text = "Update selected properties, elements or restrictions to the models selected in De" +
    "stinations";
            this.updateRadioButton.UseVisualStyleBackColor = true;
            this.updateRadioButton.CheckedChanged += new System.EventHandler(this.addRadioButton_CheckedChanged);
            // 
            // sourcesGroupBox
            // 
            this.sourcesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sourcesGroupBox.Controls.Add(this.source3CheckedListBox);
            this.sourcesGroupBox.Controls.Add(this.source2CheckedListBox);
            this.sourcesGroupBox.Controls.Add(this.propertiesCheckedListBox);
            this.sourcesGroupBox.Location = new System.Drawing.Point(7, 28);
            this.sourcesGroupBox.Name = "sourcesGroupBox";
            this.sourcesGroupBox.Size = new System.Drawing.Size(800, 257);
            this.sourcesGroupBox.TabIndex = 7;
            this.sourcesGroupBox.TabStop = false;
            this.sourcesGroupBox.Text = "Sources";
            // 
            // source3CheckedListBox
            // 
            this.source3CheckedListBox.CheckOnClick = true;
            this.source3CheckedListBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.source3CheckedListBox.FormattingEnabled = true;
            this.source3CheckedListBox.Location = new System.Drawing.Point(534, 16);
            this.source3CheckedListBox.Name = "source3CheckedListBox";
            this.source3CheckedListBox.Size = new System.Drawing.Size(250, 238);
            this.source3CheckedListBox.TabIndex = 9;
            this.source3CheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.propertiesCheckedListBox_ItemCheck);
            // 
            // source2CheckedListBox
            // 
            this.source2CheckedListBox.CheckOnClick = true;
            this.source2CheckedListBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.source2CheckedListBox.FormattingEnabled = true;
            this.source2CheckedListBox.Location = new System.Drawing.Point(284, 16);
            this.source2CheckedListBox.Name = "source2CheckedListBox";
            this.source2CheckedListBox.Size = new System.Drawing.Size(250, 238);
            this.source2CheckedListBox.TabIndex = 8;
            this.source2CheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.propertiesCheckedListBox_ItemCheck);
            // 
            // propertiesCheckedListBox
            // 
            this.propertiesCheckedListBox.CheckOnClick = true;
            this.propertiesCheckedListBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.propertiesCheckedListBox.FormattingEnabled = true;
            this.propertiesCheckedListBox.Location = new System.Drawing.Point(3, 16);
            this.propertiesCheckedListBox.Name = "propertiesCheckedListBox";
            this.propertiesCheckedListBox.Size = new System.Drawing.Size(281, 238);
            this.propertiesCheckedListBox.TabIndex = 7;
            this.propertiesCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.propertiesCheckedListBox_ItemCheck);
            // 
            // filterTextBox
            // 
            this.filterTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.filterTextBox.Location = new System.Drawing.Point(643, 31);
            this.filterTextBox.Name = "filterTextBox";
            this.filterTextBox.Size = new System.Drawing.Size(121, 20);
            this.filterTextBox.TabIndex = 15;
            this.filterTextBox.TextChanged += new System.EventHandler(this.filterTextBox_TextChanged);
            // 
            // filterLabel
            // 
            this.filterLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.filterLabel.AutoSize = true;
            this.filterLabel.Location = new System.Drawing.Point(640, 15);
            this.filterLabel.Name = "filterLabel";
            this.filterLabel.Size = new System.Drawing.Size(88, 13);
            this.filterLabel.TabIndex = 14;
            this.filterLabel.Text = "Filter destinations";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(640, 229);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(144, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "and press the button below...";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(640, 213);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(162, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "Check the copy options selected";
            // 
            // doItButton
            // 
            this.doItButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.doItButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.doItButton.Location = new System.Drawing.Point(653, 253);
            this.doItButton.Name = "doItButton";
            this.doItButton.Size = new System.Drawing.Size(138, 23);
            this.doItButton.TabIndex = 11;
            this.doItButton.Text = "Copy and Close";
            this.doItButton.UseVisualStyleBackColor = true;
            this.doItButton.Click += new System.EventHandler(this.doItButton_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.destinationCheckedListBox);
            this.groupBox3.Location = new System.Drawing.Point(294, 0);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(340, 283);
            this.groupBox3.TabIndex = 10;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Destinations";
            // 
            // destinationCheckedListBox
            // 
            this.destinationCheckedListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.destinationCheckedListBox.CheckOnClick = true;
            this.destinationCheckedListBox.FormattingEnabled = true;
            this.destinationCheckedListBox.Location = new System.Drawing.Point(6, 20);
            this.destinationCheckedListBox.Name = "destinationCheckedListBox";
            this.destinationCheckedListBox.Size = new System.Drawing.Size(328, 259);
            this.destinationCheckedListBox.TabIndex = 4;
            this.destinationCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.propertiesCheckedListBox_ItemCheck);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.reportsListBox);
            this.groupBox1.Controls.Add(this.addReportButton);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(294, 283);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Current reports list";
            // 
            // reportsListBox
            // 
            this.reportsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.reportsListBox.ContextMenuStrip = this.removeReportContextMenuStrip;
            this.reportsListBox.FormattingEnabled = true;
            this.reportsListBox.Location = new System.Drawing.Point(4, 41);
            this.reportsListBox.Name = "reportsListBox";
            this.reportsListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.reportsListBox.Size = new System.Drawing.Size(284, 238);
            this.reportsListBox.TabIndex = 5;
            // 
            // removeReportContextMenuStrip
            // 
            this.removeReportContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeReportToolStripMenuItem});
            this.removeReportContextMenuStrip.Name = "contextMenuStrip1";
            this.removeReportContextMenuStrip.Size = new System.Drawing.Size(153, 26);
            this.removeReportContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.removeReportContextMenuStrip_Opening);
            // 
            // removeReportToolStripMenuItem
            // 
            this.removeReportToolStripMenuItem.Name = "removeReportToolStripMenuItem";
            this.removeReportToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.removeReportToolStripMenuItem.Text = "Remove report";
            this.removeReportToolStripMenuItem.Click += new System.EventHandler(this.removeReportToolStripMenuItem_Click);
            // 
            // addReportButton
            // 
            this.addReportButton.Location = new System.Drawing.Point(4, 15);
            this.addReportButton.Name = "addReportButton";
            this.addReportButton.Size = new System.Drawing.Size(95, 23);
            this.addReportButton.TabIndex = 4;
            this.addReportButton.Text = "Add reports...";
            this.addReportButton.UseVisualStyleBackColor = true;
            this.addReportButton.Click += new System.EventHandler(this.addReportButton_Click);
            // 
            // mainToolStrip
            // 
            this.mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cancelToolStripButton});
            this.mainToolStrip.Location = new System.Drawing.Point(0, 0);
            this.mainToolStrip.Name = "mainToolStrip";
            this.mainToolStrip.Size = new System.Drawing.Size(820, 25);
            this.mainToolStrip.TabIndex = 5;
            this.mainToolStrip.Text = "toolStrip1";
            // 
            // cancelToolStripButton
            // 
            this.cancelToolStripButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.cancelToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.cancelToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("cancelToolStripButton.Image")));
            this.cancelToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cancelToolStripButton.Name = "cancelToolStripButton";
            this.cancelToolStripButton.Size = new System.Drawing.Size(40, 22);
            this.cancelToolStripButton.Text = "Close";
            this.cancelToolStripButton.Click += new System.EventHandler(this.cancelToolStripButton_Click);
            // 
            // SmartCopyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(820, 611);
            this.Controls.Add(this.mainToolStrip);
            this.Controls.Add(this.listPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "SmartCopyForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Smart Copy Form";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SmartCopyForm_FormClosed);
            this.Load += new System.EventHandler(this.SmartCopyForm_Load);
            this.listPanel.ResumeLayout(false);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.sourcesGroupBox.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.removeReportContextMenuStrip.ResumeLayout(false);
            this.mainToolStrip.ResumeLayout(false);
            this.mainToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel listPanel;
        private System.Windows.Forms.SplitContainer splitContainer;
        public System.Windows.Forms.ToolStrip mainToolStrip;
        private System.Windows.Forms.ToolStripButton cancelToolStripButton;
        private System.Windows.Forms.Button addReportButton;
        private System.Windows.Forms.ListBox reportsListBox;
        private System.Windows.Forms.GroupBox sourcesGroupBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox3;
        public System.Windows.Forms.CheckedListBox source3CheckedListBox;
        public System.Windows.Forms.CheckedListBox source2CheckedListBox;
        public System.Windows.Forms.CheckedListBox propertiesCheckedListBox;
        public System.Windows.Forms.CheckedListBox destinationCheckedListBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton addRadioButton;
        private System.Windows.Forms.RadioButton updateRadioButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button doItButton;
        private System.Windows.Forms.ContextMenuStrip removeReportContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem removeReportToolStripMenuItem;
        private System.Windows.Forms.TextBox filterTextBox;
        private System.Windows.Forms.Label filterLabel;
    }
}