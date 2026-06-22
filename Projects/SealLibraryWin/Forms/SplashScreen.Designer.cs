namespace Seal.Forms
{
    partial class SplashScreen
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SplashScreen));
            pictureBox1 = new System.Windows.Forms.PictureBox();
            pictureBox2 = new System.Windows.Forms.PictureBox();
            _label = new System.Windows.Forms.Label();
            _versionLabel = new System.Windows.Forms.Label();
            _copyrightLabel = new System.Windows.Forms.Label();
            _progressBar = new System.Windows.Forms.ProgressBar();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            SuspendLayout();
            //
            // pictureBox1
            //
            pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new System.Drawing.Point(2, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(119, 50);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            //
            // pictureBox2
            //
            pictureBox2.Image = (System.Drawing.Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new System.Drawing.Point(-10, 60);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new System.Drawing.Size(392, 101);
            pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBox2.TabIndex = 2;
            pictureBox2.TabStop = false;
            //
            // _label
            //
            _label.AutoSize = true;
            _label.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            _label.ForeColor = System.Drawing.SystemColors.ControlText;
            _label.Location = new System.Drawing.Point(127, 15);
            _label.Name = "_label";
            _label.Size = new System.Drawing.Size(93, 21);
            _label.TabIndex = 3;
            _label.Text = "Initializing";
            //
            // _versionLabel
            //
            _versionLabel.AutoSize = true;
            _versionLabel.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            _versionLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            _versionLabel.Location = new System.Drawing.Point(129, 39);
            _versionLabel.Name = "_versionLabel";
            _versionLabel.Size = new System.Drawing.Size(45, 13);
            _versionLabel.TabIndex = 4;
            _versionLabel.Text = "Version";
            //
            // _progressBar
            //
            _progressBar.Location = new System.Drawing.Point(20, 168);
            _progressBar.MarqueeAnimationSpeed = 30;
            _progressBar.Name = "_progressBar";
            _progressBar.Size = new System.Drawing.Size(340, 6);
            _progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            _progressBar.TabIndex = 5;
            //
            // _copyrightLabel
            //
            _copyrightLabel.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            _copyrightLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            _copyrightLabel.Location = new System.Drawing.Point(0, 178);
            _copyrightLabel.Name = "_copyrightLabel";
            _copyrightLabel.Size = new System.Drawing.Size(380, 16);
            _copyrightLabel.TabIndex = 6;
            _copyrightLabel.Text = "Copyright";
            _copyrightLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // SplashScreen
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(380, 196);
            Controls.Add(_progressBar);
            Controls.Add(_copyrightLabel);
            Controls.Add(_versionLabel);
            Controls.Add(_label);
            Controls.Add(pictureBox2);
            Controls.Add(pictureBox1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Name = "SplashScreen";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Seal Report";
            TopMost = true;
            Shown += SplashScreen_Shown;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Label _label;
        private System.Windows.Forms.Label _versionLabel;
        private System.Windows.Forms.Label _copyrightLabel;
        private System.Windows.Forms.ProgressBar _progressBar;
    }
}
