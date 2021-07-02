namespace ScintillaNET_FindReplaceDialog.FindAllResults
{
    partial class FindAllResultsPanel
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
            this.FindResultsScintilla = new ScintillaNET.Scintilla();
            this.SuspendLayout();
            // 
            // FindResultsScintilla
            // 
            this.FindResultsScintilla.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FindResultsScintilla.Location = new System.Drawing.Point(0, 0);
            this.FindResultsScintilla.Name = "FindResultsScintilla";
            this.FindResultsScintilla.ScrollWidth = 5001;
            this.FindResultsScintilla.Size = new System.Drawing.Size(556, 220);
            this.FindResultsScintilla.TabIndex = 4;
            this.FindResultsScintilla.Text = "FindResultsScintilla";
            this.FindResultsScintilla.UseTabs = false;
            this.FindResultsScintilla.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindResultsScintilla_KeyUp);
            this.FindResultsScintilla.MouseClick += new System.Windows.Forms.MouseEventHandler(this.FindResultsScintilla_MouseClick);
            this.FindResultsScintilla.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.FindResultsScintilla_MouseDoubleClick);
            // 
            // FindAllResultsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.FindResultsScintilla);
            this.Name = "FindAllResultsPanel";
            this.Size = new System.Drawing.Size(556, 220);
            this.ResumeLayout(false);

        }

        #endregion

        private ScintillaNET.Scintilla FindResultsScintilla;
    }
}
