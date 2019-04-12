//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using Seal.Helpers;
using Seal.Model;

namespace Seal.Forms
{
    public partial class ExecutionForm : Form, ExecutionLogInterface
    {
        public ExecutionForm(Thread thread)
        {
            InitializeComponent();
            ShowIcon = true;
            Icon = Repository.ProductIcon;
            _thread = thread;
        }

        private void ExecutionForm_Load(object sender, EventArgs e)
        {
            this.KeyDown += TextBox_KeyDown;
            this.logTextBox.KeyDown += TextBox_KeyDown;
            if (_thread != null)
            {
                _thread.Start(this);
            }
        }

        public bool IsJobCancelled()
        {
            return CancelJob;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && (_thread == null || !_thread.IsAlive)) Close();
        }

        public void Log(string text, params object[] args)
        {
            lock (_textToAppend)
            {
                try
                {
                    string newtext = string.Format(text, args);
                    _textToAppend += string.Format("{0} {1}\r\n", DateTime.Now.ToLongTimeString(), newtext);
                }
                catch {
                    _textToAppend += text;
                }
            }
        }

        public void LogNoCR(string text, params object[] args)
        {
            lock (_textToAppend)
            {
                try
                {
                    string newtext = string.Format(text, args);
                    _textToAppend += string.Format("{0} {1}", DateTime.Now.ToLongTimeString(), newtext);
                }
                catch {
                    _textToAppend += text;
                }
            }
        }

        public void LogRaw(string text, params object[] args)
        {
            lock (_textToAppend)
            {
                try
                {
                    _textToAppend += string.Format(text, args);
                }
                catch {
                    _textToAppend += text;
                }
            }
        }

        public bool CancelJob = false;
        string _textToAppend = "";
        bool _inPause = false;
        Thread _thread;

        void EnableControls()
        {
            if (_thread != null)
            {
                closeToolStripButton.Enabled = !_thread.IsAlive;
                cancelToolStripButton.Enabled = _thread.IsAlive;
                pauseToolStripButton.Text = (!_inPause ? "Pause Log" : "Resume Log");
            }
        }

        private void mainTimer_Tick(object sender, EventArgs e)
        {
            lock (_textToAppend)
            {
                if (!string.IsNullOrEmpty(_textToAppend) && !_inPause)
                {
                    logTextBox.AppendText(_textToAppend);
                    _textToAppend = "";
                    logTextBox.SelectionLength = 0;
                    logTextBox.SelectionStart = logTextBox.TextLength - 1;
                    logTextBox.ScrollToCaret();
                }
                EnableControls();
            }
        }

        private void LogFromForm(string text, params object[] args)
        {
            Log(text, args);
            mainTimer_Tick(null, null);
        }

        private void pauseToolStripButton_Click(object sender, EventArgs e)
        {
            _inPause = !_inPause;
            EnableControls();
        }


        private void cancelJob()
        {
            LogFromForm("Cancelling job...");

            CancelJob = true;
            //Wait a while
            int cnt = 10;
            while (--cnt >= 0)
            {
                if (!_thread.IsAlive) break;
                Thread.Sleep(1000);
                mainTimer_Tick(null, null);
            }

            if (_thread.IsAlive)
            {
                LogFromForm("Unable to cancel Job. Terminating job...");
                _thread.Abort();
                Thread.Sleep(1000);
            }

            LogFromForm(!_thread.IsAlive ? "Job is terminated" : "Job is still alive !");
        }

        private void closeToolStripButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void notepadToolStripButton_Click(object sender, EventArgs e)
        {
            string path = FileHelper.GetTempUniqueFileName("log.txt");
            FileHelper.WriteFile(path, logTextBox.Text);
            Process.Start(path);
            FileHelper.PurgeTempApplicationDirectory();
        }

        private void ExecutionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_thread != null && _thread.IsAlive)
            {
                DialogResult dlgResult = MessageBox.Show("You are about to Close the Form but a job is being executing.\r\nAre you sure ?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (dlgResult != DialogResult.Yes)
                {
                    cancelJob();
                    e.Cancel = true;
                }
            }
        }

        private void cancelToolStripButton_Click(object sender, EventArgs e)
        {
            if (_thread != null && _thread.IsAlive)
            {
                DialogResult dlgResult = MessageBox.Show("You are about to Cancel the current job execution.\r\nAre you sure ?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (dlgResult == DialogResult.Yes)
                {
                    cancelJob();
                }
            }
        }

    }
}
