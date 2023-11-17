using Microsoft.Web.WebView2.Core;

namespace Seal.Forms
{
    partial class ReportViewerForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReportViewerForm));
            mainPanel = new System.Windows.Forms.Panel();
            webBrowser = new Microsoft.Web.WebView2.WinForms.WebView2();
            mainPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webBrowser).BeginInit();
            SuspendLayout();
            // 
            // mainPanel
            // 
            mainPanel.Controls.Add(webBrowser);
            mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            mainPanel.Location = new System.Drawing.Point(0, 0);
            mainPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new System.Drawing.Size(1176, 842);
            mainPanel.TabIndex = 1;
            // 
            // webBrowser
            // 
            webBrowser.AllowExternalDrop = true;
            webBrowser.CreationProperties = null;
            webBrowser.DefaultBackgroundColor = System.Drawing.Color.White;
            webBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            webBrowser.Location = new System.Drawing.Point(0, 0);
            webBrowser.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            webBrowser.MinimumSize = new System.Drawing.Size(23, 23);
            webBrowser.Name = "webBrowser";
            webBrowser.Size = new System.Drawing.Size(1176, 842);
            webBrowser.TabIndex = 0;
            webBrowser.ZoomFactor = 1D;
            webBrowser.NavigationStarting += WebBrowser_NavigationStarting;
            // 
            // ReportViewerForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1176, 842);
            Controls.Add(mainPanel);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "ReportViewerForm";
            Text = "Report Viewer";
            FormClosing += ReportViewerForm_FormClosing;
            ClientSizeChanged += ReportViewerForm_ClientSizeChanged;
            LocationChanged += ReportViewerForm_ClientSizeChanged;
            Load += ReportViewerForm_Load;
            mainPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webBrowser).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel mainPanel;
        private Microsoft.Web.WebView2.WinForms.WebView2 webBrowser;

    }
}