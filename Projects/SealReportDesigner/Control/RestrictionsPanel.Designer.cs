namespace Seal.Controls
{
    partial class RestrictionsPanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.restrictionsTextBox = new ScintillaNET.Scintilla();
            this.SuspendLayout();
            // 
            // restrictionsTextBox
            // 
            this.restrictionsTextBox.AllowDrop = true;
            this.restrictionsTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.restrictionsTextBox.Lexer = ScintillaNET.Lexer.Sql;
            this.restrictionsTextBox.Location = new System.Drawing.Point(0, 0);
            this.restrictionsTextBox.Name = "restrictionsTextBox";
            this.restrictionsTextBox.Size = new System.Drawing.Size(451, 300);
            this.restrictionsTextBox.TabIndex = 3;
            this.restrictionsTextBox.Paint += new System.Windows.Forms.PaintEventHandler(this.restrictionsTextBox_Paint);
            this.restrictionsTextBox.TextChanged += new System.EventHandler(this.restrictionsTextBox_TextChanged);
            this.restrictionsTextBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.restrictionsTextBox_DragDrop);
            this.restrictionsTextBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.restrictionsTextBox_DragEnter);
            this.restrictionsTextBox.DragOver += new System.Windows.Forms.DragEventHandler(this.restrictionsTextBox_DragOver);
            this.restrictionsTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.scintilla_KeyUp);
            this.restrictionsTextBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.scintilla_MouseUp);
            // 
            // RestrictionsPanel
            // 
            this.Controls.Add(this.restrictionsTextBox);
            this.Name = "RestrictionsPanel";
            this.Size = new System.Drawing.Size(451, 300);
            this.ResumeLayout(false);

        }

        #endregion

        private ScintillaNET.Scintilla restrictionsTextBox;

    }
}
