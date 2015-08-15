namespace Seal.Forms
{
    partial class ExecutionForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExecutionForm));
            this.logToolStrip = new System.Windows.Forms.ToolStrip();
            this.closeToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.pauseToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.notepadToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.cancelToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.logTextBox = new System.Windows.Forms.TextBox();
            this.mainTimer = new System.Windows.Forms.Timer(this.components);
            this.logToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // logToolStrip
            // 
            this.logToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.closeToolStripButton,
            this.pauseToolStripButton,
            this.notepadToolStripButton,
            this.cancelToolStripButton});
            this.logToolStrip.Location = new System.Drawing.Point(0, 0);
            this.logToolStrip.Name = "logToolStrip";
            this.logToolStrip.Size = new System.Drawing.Size(884, 25);
            this.logToolStrip.TabIndex = 0;
            this.logToolStrip.Text = "toolStrip1";
            // 
            // closeToolStripButton
            // 
            this.closeToolStripButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.closeToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("closeToolStripButton.Image")));
            this.closeToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.closeToolStripButton.Name = "closeToolStripButton";
            this.closeToolStripButton.Size = new System.Drawing.Size(56, 22);
            this.closeToolStripButton.Text = "Close";
            this.closeToolStripButton.ToolTipText = "Close the form if the job is terminated";
            this.closeToolStripButton.Click += new System.EventHandler(this.closeToolStripButton_Click);
            // 
            // pauseToolStripButton
            // 
            this.pauseToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("pauseToolStripButton.Image")));
            this.pauseToolStripButton.ImageTransparentColor = System.Drawing.Color.White;
            this.pauseToolStripButton.Name = "pauseToolStripButton";
            this.pauseToolStripButton.Size = new System.Drawing.Size(81, 22);
            this.pauseToolStripButton.Text = "Pause Log";
            this.pauseToolStripButton.ToolTipText = "Pause or resume log display and  scrolling";
            this.pauseToolStripButton.Click += new System.EventHandler(this.pauseToolStripButton_Click);
            // 
            // notepadToolStripButton
            // 
            this.notepadToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("notepadToolStripButton.Image")));
            this.notepadToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.notepadToolStripButton.Name = "notepadToolStripButton";
            this.notepadToolStripButton.Size = new System.Drawing.Size(73, 22);
            this.notepadToolStripButton.Text = "Notepad";
            this.notepadToolStripButton.ToolTipText = "View log text in Notepad";
            this.notepadToolStripButton.Click += new System.EventHandler(this.notepadToolStripButton_Click);
            // 
            // cancelToolStripButton
            // 
            this.cancelToolStripButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.cancelToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("cancelToolStripButton.Image")));
            this.cancelToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cancelToolStripButton.Name = "cancelToolStripButton";
            this.cancelToolStripButton.Size = new System.Drawing.Size(117, 22);
            this.cancelToolStripButton.Text = "Cancel Execution";
            this.cancelToolStripButton.ToolTipText = "Cancel current job execution or Close the form if it is terminated";
            this.cancelToolStripButton.Click += new System.EventHandler(this.cancelToolStripButton_Click);
            // 
            // logTextBox
            // 
            this.logTextBox.AcceptsReturn = true;
            this.logTextBox.AcceptsTab = true;
            this.logTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logTextBox.Location = new System.Drawing.Point(0, 25);
            this.logTextBox.Multiline = true;
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.logTextBox.Size = new System.Drawing.Size(884, 537);
            this.logTextBox.TabIndex = 1;
            // 
            // mainTimer
            // 
            this.mainTimer.Enabled = true;
            this.mainTimer.Interval = 400;
            this.mainTimer.Tick += new System.EventHandler(this.mainTimer_Tick);
            // 
            // ExecutionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 562);
            this.Controls.Add(this.logTextBox);
            this.Controls.Add(this.logToolStrip);
            this.Name = "ExecutionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Execution Form";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ExecutionForm_FormClosing);
            this.Load += new System.EventHandler(this.ExecutionForm_Load);
            this.logToolStrip.ResumeLayout(false);
            this.logToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip logToolStrip;
        private System.Windows.Forms.ToolStripButton closeToolStripButton;
        public System.Windows.Forms.TextBox logTextBox;
        private System.Windows.Forms.Timer mainTimer;
        public System.Windows.Forms.ToolStripButton notepadToolStripButton;
        public System.Windows.Forms.ToolStripButton pauseToolStripButton;
        public System.Windows.Forms.ToolStripButton cancelToolStripButton;

    }
}