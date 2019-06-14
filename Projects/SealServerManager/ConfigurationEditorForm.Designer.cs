namespace Seal.Forms
{
    partial class ConfigurationEditorForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigurationEditorForm));
            this.mainToolStrip = new System.Windows.Forms.ToolStrip();
            this.okToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.cancelToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.publish1ToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.publish2ToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.browseToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.iisToolStripButton = new System.Windows.Forms.ToolStripButton();
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
            this.publish1ToolStripButton,
            this.publish2ToolStripButton,
            this.browseToolStripButton,
            this.iisToolStripButton});
            this.mainToolStrip.Location = new System.Drawing.Point(0, 0);
            this.mainToolStrip.Name = "mainToolStrip";
            this.mainToolStrip.Size = new System.Drawing.Size(932, 25);
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
            // publish1ToolStripButton
            // 
            this.publish1ToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.publish1ToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("publish1ToolStripButton.Image")));
            this.publish1ToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.publish1ToolStripButton.Name = "publish1ToolStripButton";
            this.publish1ToolStripButton.Size = new System.Drawing.Size(118, 22);
            this.publish1ToolStripButton.Text = "Publish files and site";
            this.publish1ToolStripButton.ToolTipText = "Copy the required files and publish the Web site";
            this.publish1ToolStripButton.Click += new System.EventHandler(this.publishToolStripButton_Click);
            // 
            // publish2ToolStripButton
            // 
            this.publish2ToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.publish2ToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("publish2ToolStripButton.Image")));
            this.publish2ToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.publish2ToolStripButton.Name = "publish2ToolStripButton";
            this.publish2ToolStripButton.Size = new System.Drawing.Size(100, 22);
            this.publish2ToolStripButton.Text = "Publish only files";
            this.publish2ToolStripButton.ToolTipText = "Copy only the required files. It can be used if the site was previously published" +
    " (for release upgrade)";
            this.publish2ToolStripButton.Click += new System.EventHandler(this.publishToolStripButton_Click);
            // 
            // browseToolStripButton
            // 
            this.browseToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.browseToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("browseToolStripButton.Image")));
            this.browseToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.browseToolStripButton.Name = "browseToolStripButton";
            this.browseToolStripButton.Size = new System.Drawing.Size(98, 22);
            this.browseToolStripButton.Text = "Browse Web Site";
            this.browseToolStripButton.ToolTipText = "View the Seal Web Server site in a browser..";
            this.browseToolStripButton.Click += new System.EventHandler(this.browseToolStripButton_Click);
            // 
            // iisToolStripButton
            // 
            this.iisToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.iisToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("iisToolStripButton.Image")));
            this.iisToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.iisToolStripButton.Name = "iisToolStripButton";
            this.iisToolStripButton.Size = new System.Drawing.Size(97, 22);
            this.iisToolStripButton.Text = "Run IIS Manager";
            this.iisToolStripButton.ToolTipText = "Run the IIS Microsoft Management Console";
            this.iisToolStripButton.Click += new System.EventHandler(this.iisManagerToolStripButton_Click);
            // 
            // mainPanel
            // 
            this.mainPanel.Controls.Add(this.mainPropertyGrid);
            this.mainPanel.Controls.Add(this.panel2);
            this.mainPanel.Controls.Add(this.mainStatusStrip);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 25);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(932, 646);
            this.mainPanel.TabIndex = 3;
            // 
            // mainPropertyGrid
            // 
            this.mainPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.mainPropertyGrid.Name = "mainPropertyGrid";
            this.mainPropertyGrid.Size = new System.Drawing.Size(932, 434);
            this.mainPropertyGrid.TabIndex = 5;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.infoTextBox);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 434);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(932, 190);
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
            this.infoTextBox.Size = new System.Drawing.Size(932, 190);
            this.infoTextBox.TabIndex = 3;
            this.infoTextBox.TabStop = false;
            // 
            // mainStatusStrip
            // 
            this.mainStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.mainStatusStrip.Location = new System.Drawing.Point(0, 624);
            this.mainStatusStrip.Name = "mainStatusStrip";
            this.mainStatusStrip.Size = new System.Drawing.Size(932, 22);
            this.mainStatusStrip.TabIndex = 1;
            this.mainStatusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Image = global::Seal.Properties.Resources.checkedGreen;
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(16, 17);
            // 
            // ConfigurationEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(932, 671);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.mainToolStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ConfigurationEditorForm";
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
        private System.Windows.Forms.ToolStripButton publish1ToolStripButton;
        private System.Windows.Forms.ToolStripButton publish2ToolStripButton;
        private System.Windows.Forms.ToolStripButton browseToolStripButton;
        private System.Windows.Forms.ToolStripButton iisToolStripButton;
    }
}