namespace Seal.Forms
{
    partial class SecurityEditorForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SecurityEditorForm));
            this.mainToolStrip = new System.Windows.Forms.ToolStrip();
            this.okToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.cancelToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.summaryToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.mainPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.panel2 = new System.Windows.Forms.Panel();
            this.infoTextBox = new System.Windows.Forms.TextBox();
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
            this.summaryToolStripButton});
            this.mainToolStrip.Location = new System.Drawing.Point(0, 0);
            this.mainToolStrip.Name = "mainToolStrip";
            this.mainToolStrip.Size = new System.Drawing.Size(764, 25);
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
            // summaryToolStripButton
            // 
            this.summaryToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.summaryToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("summaryToolStripButton.Image")));
            this.summaryToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.summaryToolStripButton.Name = "summaryToolStripButton";
            this.summaryToolStripButton.Size = new System.Drawing.Size(173, 22);
            this.summaryToolStripButton.Text = "View security groups summary";
            this.summaryToolStripButton.ToolTipText = "Display a summary per group and per folder";
            this.summaryToolStripButton.Click += new System.EventHandler(this.summaryToolStripButton_Click);
            // 
            // mainPanel
            // 
            this.mainPanel.Controls.Add(this.mainPropertyGrid);
            this.mainPanel.Controls.Add(this.panel2);
            this.mainPanel.Controls.Add(this.mainStatusStrip);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 25);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(764, 557);
            this.mainPanel.TabIndex = 3;
            // 
            // mainPropertyGrid
            // 
            this.mainPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.mainPropertyGrid.Name = "mainPropertyGrid";
            this.mainPropertyGrid.Size = new System.Drawing.Size(764, 377);
            this.mainPropertyGrid.TabIndex = 5;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.infoTextBox);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 377);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(764, 158);
            this.panel2.TabIndex = 4;
            // 
            // infoTextBox
            // 
            this.infoTextBox.CausesValidation = false;
            this.infoTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.infoTextBox.Location = new System.Drawing.Point(0, 0);
            this.infoTextBox.Multiline = true;
            this.infoTextBox.Name = "infoTextBox";
            this.infoTextBox.ReadOnly = true;
            this.infoTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.infoTextBox.Size = new System.Drawing.Size(764, 158);
            this.infoTextBox.TabIndex = 3;
            this.infoTextBox.TabStop = false;
            // 
            // mainStatusStrip
            // 
            this.mainStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.mainStatusStrip.Location = new System.Drawing.Point(0, 535);
            this.mainStatusStrip.Name = "mainStatusStrip";
            this.mainStatusStrip.Size = new System.Drawing.Size(764, 22);
            this.mainStatusStrip.TabIndex = 1;
            this.mainStatusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Image = global::Seal.Properties.Resources.checkedGreen;
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(16, 17);
            // 
            // SecurityEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(764, 582);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.mainToolStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SecurityEditorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configuration Editor";
            this.Load += new System.EventHandler(this.ConfigurationEditorForm_Load);
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
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TextBox infoTextBox;
        private System.Windows.Forms.PropertyGrid mainPropertyGrid;
        private System.Windows.Forms.ToolStripButton summaryToolStripButton;
    }
}