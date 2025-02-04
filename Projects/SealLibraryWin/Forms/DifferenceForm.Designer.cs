using System.Drawing;
using System.Windows.Forms;

namespace DiffPlex.WindowsForms
{
    partial class DifferenceForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            MainLayoutPanel = new TableLayoutPanel();
            flowLayoutPanel2 = new FlowLayoutPanel();
            ignoreCase = new CheckBox();
            ignoreWhiteSpace = new CheckBox();
            flowLayoutPanel1 = new FlowLayoutPanel();
            SwitchButton = new Button();
            reload = new Button();
            close = new Button();
            MainLayoutPanel.SuspendLayout();
            flowLayoutPanel2.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // MainLayoutPanel
            // 
            MainLayoutPanel.ColumnCount = 2;
            MainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            MainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            MainLayoutPanel.Controls.Add(flowLayoutPanel2, 0, 1);
            MainLayoutPanel.Controls.Add(flowLayoutPanel1, 1, 0);
            MainLayoutPanel.Controls.Add(close, 1, 1);
            MainLayoutPanel.Dock = DockStyle.Fill;
            MainLayoutPanel.Location = new Point(0, 0);
            MainLayoutPanel.Margin = new Padding(0);
            MainLayoutPanel.Name = "MainLayoutPanel";
            MainLayoutPanel.RowCount = 2;
            MainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            MainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            MainLayoutPanel.Size = new Size(1215, 598);
            MainLayoutPanel.TabIndex = 3;
            // 
            // flowLayoutPanel2
            // 
            flowLayoutPanel2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            flowLayoutPanel2.Controls.Add(ignoreCase);
            flowLayoutPanel2.Controls.Add(ignoreWhiteSpace);
            flowLayoutPanel2.Location = new Point(6, 569);
            flowLayoutPanel2.Margin = new Padding(6, 5, 6, 5);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            flowLayoutPanel2.Size = new Size(1108, 24);
            flowLayoutPanel2.TabIndex = 8;
            // 
            // ignoreCase
            // 
            ignoreCase.AutoSize = true;
            ignoreCase.Location = new Point(3, 3);
            ignoreCase.Name = "ignoreCase";
            ignoreCase.Size = new Size(86, 19);
            ignoreCase.TabIndex = 8;
            ignoreCase.Text = "Ignore case";
            ignoreCase.UseVisualStyleBackColor = true;
            ignoreCase.CheckedChanged += ignoreCase_CheckedChanged;
            // 
            // ignoreWhiteSpace
            // 
            ignoreWhiteSpace.AutoSize = true;
            ignoreWhiteSpace.Location = new Point(95, 3);
            ignoreWhiteSpace.Name = "ignoreWhiteSpace";
            ignoreWhiteSpace.Size = new Size(125, 19);
            ignoreWhiteSpace.TabIndex = 9;
            ignoreWhiteSpace.Text = "Ignore white space";
            ignoreWhiteSpace.UseVisualStyleBackColor = true;
            ignoreWhiteSpace.CheckedChanged += ignoreWhiteSpace_CheckedChanged;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            flowLayoutPanel1.Controls.Add(SwitchButton);
            flowLayoutPanel1.Controls.Add(reload);
            flowLayoutPanel1.Location = new Point(1126, 5);
            flowLayoutPanel1.Margin = new Padding(6, 5, 6, 5);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(83, 344);
            flowLayoutPanel1.TabIndex = 6;
            // 
            // SwitchButton
            // 
            SwitchButton.Anchor = AnchorStyles.Top;
            SwitchButton.Location = new Point(2, 2);
            SwitchButton.Margin = new Padding(2);
            SwitchButton.Name = "SwitchButton";
            SwitchButton.Size = new Size(79, 22);
            SwitchButton.TabIndex = 0;
            SwitchButton.Text = "Mode...";
            SwitchButton.UseVisualStyleBackColor = true;
            SwitchButton.Click += FutherActionsButton_Click;
            // 
            // reload
            // 
            reload.Location = new Point(2, 28);
            reload.Margin = new Padding(2);
            reload.Name = "reload";
            reload.Size = new Size(79, 22);
            reload.TabIndex = 2;
            reload.Text = "Reload";
            reload.UseVisualStyleBackColor = true;
            reload.Click += reload_Click;
            // 
            // close
            // 
            close.Location = new Point(1122, 566);
            close.Margin = new Padding(2);
            close.Name = "close";
            close.Size = new Size(91, 22);
            close.TabIndex = 4;
            close.Text = "Close";
            close.UseVisualStyleBackColor = true;
            close.Click += close_Click;
            // 
            // DifferenceForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1215, 598);
            Controls.Add(MainLayoutPanel);
            Margin = new Padding(2);
            Name = "DifferenceForm";
            Text = "Differences";
            Load += MainForm_Load;
            MainLayoutPanel.ResumeLayout(false);
            flowLayoutPanel2.ResumeLayout(false);
            flowLayoutPanel2.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel MainLayoutPanel;
        private Button close;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button SwitchButton;
        private Button reload;
        private FlowLayoutPanel flowLayoutPanel2;
        private CheckBox ignoreCase;
        private CheckBox ignoreWhiteSpace;
    }
}

