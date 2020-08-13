namespace Seal.Forms
{
    partial class SQLEditorForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SQLEditorForm));
            this.mainToolStrip = new System.Windows.Forms.ToolStrip();
            this.okToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.cancelToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.checkSQLToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.clearToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.copyToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.sqlTextBox = new ScintillaNET.Scintilla();
            this.panel2 = new System.Windows.Forms.Panel();
            this.errorTextBox = new System.Windows.Forms.TextBox();
            this.mainStatusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.mainToolStrip.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.panel2.SuspendLayout();
            this.mainStatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainToolStrip
            // 
            this.mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.okToolStripButton,
            this.cancelToolStripButton,
            this.checkSQLToolStripButton,
            this.clearToolStripButton,
            this.copyToolStripButton});
            this.mainToolStrip.Location = new System.Drawing.Point(0, 0);
            this.mainToolStrip.Name = "mainToolStrip";
            this.mainToolStrip.Size = new System.Drawing.Size(748, 25);
            this.mainToolStrip.TabIndex = 2;
            this.mainToolStrip.Text = "toolStrip1";
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
            // checkSQLToolStripButton
            // 
            this.checkSQLToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.checkSQLToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("checkSQLToolStripButton.Image")));
            this.checkSQLToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.checkSQLToolStripButton.Name = "checkSQLToolStripButton";
            this.checkSQLToolStripButton.Size = new System.Drawing.Size(68, 22);
            this.checkSQLToolStripButton.Text = "Check SQL";
            this.checkSQLToolStripButton.Click += new System.EventHandler(this.checkSQLToolStripButton_Click);
            // 
            // clearToolStripButton
            // 
            this.clearToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.clearToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("clearToolStripButton.Image")));
            this.clearToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.clearToolStripButton.Name = "clearToolStripButton";
            this.clearToolStripButton.Size = new System.Drawing.Size(62, 22);
            this.clearToolStripButton.Text = "Clear SQL";
            this.clearToolStripButton.ToolTipText = "Clear SQL to get default clause";
            this.clearToolStripButton.Click += new System.EventHandler(this.clearToolStripButton_Click);
            // 
            // copyToolStripButton
            // 
            this.copyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.copyToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripButton.Image")));
            this.copyToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.copyToolStripButton.Name = "copyToolStripButton";
            this.copyToolStripButton.Size = new System.Drawing.Size(106, 22);
            this.copyToolStripButton.Text = "Copy to clipboard";
            this.copyToolStripButton.Click += new System.EventHandler(this.copyToolStripButton_Click);
            // 
            // mainPanel
            // 
            this.mainPanel.Controls.Add(this.sqlTextBox);
            this.mainPanel.Controls.Add(this.panel2);
            this.mainPanel.Controls.Add(this.mainStatusStrip);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 25);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(748, 506);
            this.mainPanel.TabIndex = 3;
            // 
            // sqlTextBox
            // 
            this.sqlTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sqlTextBox.Location = new System.Drawing.Point(0, 0);
            this.sqlTextBox.Name = "sqlTextBox";
            this.sqlTextBox.Size = new System.Drawing.Size(748, 409);
            this.sqlTextBox.TabIndex = 5;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.errorTextBox);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 409);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(748, 75);
            this.panel2.TabIndex = 4;
            // 
            // errorTextBox
            // 
            this.errorTextBox.CausesValidation = false;
            this.errorTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.errorTextBox.Location = new System.Drawing.Point(0, 0);
            this.errorTextBox.Multiline = true;
            this.errorTextBox.Name = "errorTextBox";
            this.errorTextBox.ReadOnly = true;
            this.errorTextBox.Size = new System.Drawing.Size(748, 75);
            this.errorTextBox.TabIndex = 3;
            this.errorTextBox.TabStop = false;
            // 
            // mainStatusStrip
            // 
            this.mainStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.mainStatusStrip.Location = new System.Drawing.Point(0, 484);
            this.mainStatusStrip.Name = "mainStatusStrip";
            this.mainStatusStrip.Size = new System.Drawing.Size(748, 22);
            this.mainStatusStrip.TabIndex = 1;
            this.mainStatusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Image = global::Seal.Properties.Resources.checkedGreen;
            this.toolStripStatusLabel.ImageTransparentColor = System.Drawing.Color.White;
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(16, 17);
            // 
            // SQLEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(748, 531);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.mainToolStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SQLEditorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SQL Editor";
            this.mainToolStrip.ResumeLayout(false);
            this.mainToolStrip.PerformLayout();
            this.mainPanel.ResumeLayout(false);
            this.mainPanel.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.mainStatusStrip.ResumeLayout(false);
            this.mainStatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.ToolStrip mainToolStrip;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.StatusStrip mainStatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        public System.Windows.Forms.ToolStripButton okToolStripButton;
        public System.Windows.Forms.ToolStripButton cancelToolStripButton;
        public System.Windows.Forms.ToolStripButton checkSQLToolStripButton;
        public System.Windows.Forms.ToolStripButton clearToolStripButton;
        public System.Windows.Forms.ToolStripButton copyToolStripButton;
        private System.Windows.Forms.Panel panel2;
        public ScintillaNET.Scintilla sqlTextBox;
        public System.Windows.Forms.TextBox errorTextBox;
    }
}