namespace ScintillaNET_FindReplaceDialog
{
    partial class GoToDialog
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
            this.lblCurrentLine = new System.Windows.Forms.Label();
            this.txtCurrentLine = new System.Windows.Forms.TextBox();
            this.err = new System.Windows.Forms.ErrorProvider(this.components);
            this.txtMaxLine = new System.Windows.Forms.TextBox();
            this.lblMaxLine = new System.Windows.Forms.Label();
            this.txtGotoLine = new System.Windows.Forms.TextBox();
            this.lblGotoLine = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.err)).BeginInit();
            this.SuspendLayout();
            // 
            // lblCurrentLine
            // 
            this.lblCurrentLine.AutoSize = true;
            this.lblCurrentLine.Location = new System.Drawing.Point(9, 13);
            this.lblCurrentLine.Name = "lblCurrentLine";
            this.lblCurrentLine.Size = new System.Drawing.Size(102, 13);
            this.lblCurrentLine.TabIndex = 0;
            this.lblCurrentLine.Text = "&Current line number";
            // 
            // txtCurrentLine
            // 
            this.txtCurrentLine.Location = new System.Drawing.Point(132, 8);
            this.txtCurrentLine.Name = "txtCurrentLine";
            this.txtCurrentLine.ReadOnly = true;
            this.txtCurrentLine.Size = new System.Drawing.Size(63, 21);
            this.txtCurrentLine.TabIndex = 1;
            // 
            // err
            // 
            this.err.ContainerControl = this;
            // 
            // txtMaxLine
            // 
            this.txtMaxLine.Location = new System.Drawing.Point(132, 33);
            this.txtMaxLine.Name = "txtMaxLine";
            this.txtMaxLine.ReadOnly = true;
            this.txtMaxLine.Size = new System.Drawing.Size(63, 21);
            this.txtMaxLine.TabIndex = 3;
            // 
            // lblMaxLine
            // 
            this.lblMaxLine.AutoSize = true;
            this.lblMaxLine.Location = new System.Drawing.Point(9, 37);
            this.lblMaxLine.Name = "lblMaxLine";
            this.lblMaxLine.Size = new System.Drawing.Size(117, 13);
            this.lblMaxLine.TabIndex = 2;
            this.lblMaxLine.Text = "&Maxmimum line number";
            // 
            // txtGotoLine
            // 
            this.txtGotoLine.Location = new System.Drawing.Point(132, 58);
            this.txtGotoLine.Name = "txtGotoLine";
            this.txtGotoLine.Size = new System.Drawing.Size(63, 21);
            this.txtGotoLine.TabIndex = 5;
            // 
            // lblGotoLine
            // 
            this.lblGotoLine.AutoSize = true;
            this.lblGotoLine.Location = new System.Drawing.Point(9, 61);
            this.lblGotoLine.Name = "lblGotoLine";
            this.lblGotoLine.Size = new System.Drawing.Size(91, 13);
            this.lblGotoLine.TabIndex = 4;
            this.lblGotoLine.Text = "&Go to line number";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(39, 85);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 6;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(120, 85);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // GoToDialog
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(210, 113);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtGotoLine);
            this.Controls.Add(this.lblGotoLine);
            this.Controls.Add(this.txtMaxLine);
            this.Controls.Add(this.lblMaxLine);
            this.Controls.Add(this.txtCurrentLine);
            this.Controls.Add(this.lblCurrentLine);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GoToDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Go To Line";
            this.Activated += new System.EventHandler(this.GoToDialog_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GoToDialog_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.err)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblCurrentLine;
        private System.Windows.Forms.TextBox txtCurrentLine;
        private System.Windows.Forms.ErrorProvider err;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TextBox txtGotoLine;
        private System.Windows.Forms.Label lblGotoLine;
        private System.Windows.Forms.TextBox txtMaxLine;
        private System.Windows.Forms.Label lblMaxLine;
    }
}