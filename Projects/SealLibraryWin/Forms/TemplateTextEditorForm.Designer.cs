namespace Seal.Forms
{
    partial class TemplateTextEditorForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TemplateTextEditorForm));
            mainToolStrip = new System.Windows.Forms.ToolStrip();
            okToolStripButton = new System.Windows.Forms.ToolStripButton();
            cancelToolStripButton = new System.Windows.Forms.ToolStripButton();
            mainPanel = new System.Windows.Forms.Panel();
            mainStatusStrip = new System.Windows.Forms.StatusStrip();
            toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            mainTimer = new System.Windows.Forms.Timer(components);
            mainToolStrip.SuspendLayout();
            mainPanel.SuspendLayout();
            mainStatusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // mainToolStrip
            // 
            mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { okToolStripButton, cancelToolStripButton });
            mainToolStrip.Location = new System.Drawing.Point(0, 0);
            mainToolStrip.Name = "mainToolStrip";
            mainToolStrip.Size = new System.Drawing.Size(1050, 25);
            mainToolStrip.TabIndex = 2;
            mainToolStrip.Text = "toolStrip1";
            // 
            // okToolStripButton
            // 
            okToolStripButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            okToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            okToolStripButton.Image = (System.Drawing.Image)resources.GetObject("okToolStripButton.Image");
            okToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            okToolStripButton.Name = "okToolStripButton";
            okToolStripButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            okToolStripButton.Size = new System.Drawing.Size(47, 22);
            okToolStripButton.Text = "OK";
            okToolStripButton.Click += okToolStripButton_Click;
            // 
            // cancelToolStripButton
            // 
            cancelToolStripButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            cancelToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            cancelToolStripButton.Image = (System.Drawing.Image)resources.GetObject("cancelToolStripButton.Image");
            cancelToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            cancelToolStripButton.Name = "cancelToolStripButton";
            cancelToolStripButton.Size = new System.Drawing.Size(47, 22);
            cancelToolStripButton.Text = "Cancel";
            cancelToolStripButton.Click += cancelToolStripButton_Click;
            // 
            // mainPanel
            // 
            mainPanel.Controls.Add(mainStatusStrip);
            mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            mainPanel.Location = new System.Drawing.Point(0, 25);
            mainPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new System.Drawing.Size(1050, 667);
            mainPanel.TabIndex = 3;
            // 
            // mainStatusStrip
            // 
            mainStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripStatusLabel });
            mainStatusStrip.Location = new System.Drawing.Point(0, 645);
            mainStatusStrip.Name = "mainStatusStrip";
            mainStatusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            mainStatusStrip.Size = new System.Drawing.Size(1050, 22);
            mainStatusStrip.TabIndex = 1;
            mainStatusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            toolStripStatusLabel.Image = Properties.Resources.checkedGreen;
            toolStripStatusLabel.ImageTransparentColor = System.Drawing.Color.White;
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new System.Drawing.Size(16, 17);
            // 
            // mainTimer
            // 
            mainTimer.Enabled = true;
            mainTimer.Interval = 500;
            mainTimer.Tick += mainTimer_Tick;
            // 
            // TemplateTextEditorForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1050, 692);
            Controls.Add(mainPanel);
            Controls.Add(mainToolStrip);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "TemplateTextEditorForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Template Text Editor";
            mainToolStrip.ResumeLayout(false);
            mainToolStrip.PerformLayout();
            mainPanel.ResumeLayout(false);
            mainPanel.PerformLayout();
            mainStatusStrip.ResumeLayout(false);
            mainStatusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        public System.Windows.Forms.ToolStrip mainToolStrip;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.StatusStrip mainStatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        public System.Windows.Forms.ToolStripButton okToolStripButton;
        public System.Windows.Forms.ToolStripButton cancelToolStripButton;
        private System.Windows.Forms.Timer mainTimer;
    }
}