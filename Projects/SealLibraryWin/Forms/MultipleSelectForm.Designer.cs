namespace Seal.Forms
{
    partial class MultipleSelectForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MultipleSelectForm));
            this.mainToolStrip = new System.Windows.Forms.ToolStrip();
            this.filterToolStripLabel = new System.Windows.Forms.ToolStripLabel();
            this.filterToolStripTextBox = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.selectAllToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.sortToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.okToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.cancelToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.optionPanel = new System.Windows.Forms.Panel();
            this.listPanel = new System.Windows.Forms.Panel();
            this.checkedListBox = new System.Windows.Forms.CheckedListBox();
            this.mainToolStrip.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.listPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainToolStrip
            // 
            this.mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.filterToolStripLabel,
            this.filterToolStripTextBox,
            this.toolStripSeparator1,
            this.selectAllToolStripButton,
            this.sortToolStripButton,
            this.okToolStripButton,
            this.cancelToolStripButton});
            this.mainToolStrip.Location = new System.Drawing.Point(0, 0);
            this.mainToolStrip.Name = "mainToolStrip";
            this.mainToolStrip.Size = new System.Drawing.Size(401, 25);
            this.mainToolStrip.TabIndex = 1;
            this.mainToolStrip.Text = "toolStrip1";
            // 
            // filterToolStripLabel
            // 
            this.filterToolStripLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.filterToolStripLabel.Image = ((System.Drawing.Image)(resources.GetObject("filterToolStripLabel.Image")));
            this.filterToolStripLabel.Name = "filterToolStripLabel";
            this.filterToolStripLabel.Size = new System.Drawing.Size(16, 22);
            this.filterToolStripLabel.Text = "Filter";
            this.filterToolStripLabel.ToolTipText = "Filter";
            // 
            // filterToolStripTextBox
            // 
            this.filterToolStripTextBox.Name = "filterToolStripTextBox";
            this.filterToolStripTextBox.Size = new System.Drawing.Size(140, 25);
            this.filterToolStripTextBox.TextChanged += new System.EventHandler(this.filterToolStripTextBox_TextChanged);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // selectAllToolStripButton
            // 
            this.selectAllToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.selectAllToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("selectAllToolStripButton.Image")));
            this.selectAllToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.selectAllToolStripButton.Name = "selectAllToolStripButton";
            this.selectAllToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.selectAllToolStripButton.Text = "Select";
            this.selectAllToolStripButton.Click += new System.EventHandler(this.selectAllToolStripButton_Click);
            // 
            // sortToolStripButton
            // 
            this.sortToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.sortToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("sortToolStripButton.Image")));
            this.sortToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.sortToolStripButton.Name = "sortToolStripButton";
            this.sortToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.sortToolStripButton.Text = "Sort";
            this.sortToolStripButton.Click += new System.EventHandler(this.sortToolStripButton_Click);
            // 
            // okToolStripButton
            // 
            this.okToolStripButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.okToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.okToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("okToolStripButton.Image")));
            this.okToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.okToolStripButton.Name = "okToolStripButton";
            this.okToolStripButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.okToolStripButton.Size = new System.Drawing.Size(47, 22);
            this.okToolStripButton.Text = "OK";
            this.okToolStripButton.Click += new System.EventHandler(this.okToolStripButton_Click);
            // 
            // cancelToolStripButton
            // 
            this.cancelToolStripButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.cancelToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.cancelToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("cancelToolStripButton.Image")));
            this.cancelToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cancelToolStripButton.Name = "cancelToolStripButton";
            this.cancelToolStripButton.Size = new System.Drawing.Size(47, 22);
            this.cancelToolStripButton.Text = "Cancel";
            this.cancelToolStripButton.Click += new System.EventHandler(this.cancelToolStripButton_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 526);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(401, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // optionPanel
            // 
            this.optionPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.optionPanel.Location = new System.Drawing.Point(0, 25);
            this.optionPanel.Name = "optionPanel";
            this.optionPanel.Size = new System.Drawing.Size(401, 26);
            this.optionPanel.TabIndex = 3;
            this.optionPanel.Visible = false;
            // 
            // listPanel
            // 
            this.listPanel.Controls.Add(this.checkedListBox);
            this.listPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listPanel.Location = new System.Drawing.Point(0, 51);
            this.listPanel.Name = "listPanel";
            this.listPanel.Size = new System.Drawing.Size(401, 475);
            this.listPanel.TabIndex = 4;
            // 
            // checkedListBox
            // 
            this.checkedListBox.CheckOnClick = true;
            this.checkedListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkedListBox.FormattingEnabled = true;
            this.checkedListBox.Location = new System.Drawing.Point(0, 0);
            this.checkedListBox.Name = "checkedListBox";
            this.checkedListBox.Size = new System.Drawing.Size(401, 475);
            this.checkedListBox.TabIndex = 1;
            this.checkedListBox.SelectedIndexChanged += new System.EventHandler(this.checkedListBox_SelectedIndexChanged);
            // 
            // MultipleSelectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(401, 548);
            this.Controls.Add(this.listPanel);
            this.Controls.Add(this.optionPanel);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.mainToolStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MultipleSelectForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Multiple Select Form";
            this.Load += new System.EventHandler(this.MultipleSelectForm_Load);
            this.Shown += new System.EventHandler(this.MultipleSelectForm_Shown);
            this.mainToolStrip.ResumeLayout(false);
            this.mainToolStrip.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.listPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.ToolStripTextBox filterToolStripTextBox;
        public System.Windows.Forms.ToolStripButton sortToolStripButton;
        private System.Windows.Forms.ToolStripLabel filterToolStripLabel;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        public System.Windows.Forms.ToolStripButton okToolStripButton;
        public System.Windows.Forms.ToolStripButton cancelToolStripButton;
        public System.Windows.Forms.ToolStripButton selectAllToolStripButton;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        public System.Windows.Forms.ToolStrip mainToolStrip;
        private System.Windows.Forms.Panel listPanel;
        public System.Windows.Forms.Panel optionPanel;
        public System.Windows.Forms.CheckedListBox checkedListBox;
    }
}