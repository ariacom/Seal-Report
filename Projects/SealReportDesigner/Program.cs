//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Windows.Forms;
using System.Threading;
using Seal.Model;
using System.IO;
using Seal.Forms;
using Seal.Helpers;
using System.Reflection;

namespace Seal
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Add the event handler for handling UI thread exceptions to the event.
            Application.ThreadException += new ThreadExceptionEventHandler(ExceptionHandler);

            // Set the unhandled exception mode to force all Windows Forms errors to go through 
            // our handler.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool execute = (args.Length >= 1 && args[0].ToLower() == "/e");
            bool executeOutputOrView = (args.Length >= 1 && args[0].ToLower() == "/x");
            string reportToOpen = null;
            if (args.Length >= 2 && File.Exists(args[1])) reportToOpen = args[1];

            if ((execute || executeOutputOrView) && reportToOpen != null)
            {
                //Execute only
                var report = Report.LoadFromFile(reportToOpen, Repository.Create());
                string outputGUID = null, viewGUID = null;
                if (executeOutputOrView)
                {
                    string guid = (args.Length >= 3 ? args[2] : "");
                    if (!string.IsNullOrEmpty(guid))
                    {
                        if (report.Views.Exists(i => i.GUID == guid)) viewGUID = guid;
                        if (report.Outputs.Exists(i => i.GUID == guid)) outputGUID = guid;
                    }
                    else
                    {
                        //by default execute first output
                        if (report.Outputs.Count > 0) outputGUID = report.Outputs[0].GUID;
                    }
                }
                var reportViewer = new ReportViewerForm(true, Properties.Settings.Default.ShowScriptErrors);
                reportViewer.ViewReport(report, report.Repository, false, viewGUID, outputGUID, report.FilePath);

                Application.Run();
            }
            else
            {
                Application.Run(new ReportDesigner());
            }
        }

        private static void ExceptionHandler(object sender, ThreadExceptionEventArgs t)
        {
            DialogResult result = DialogResult.Cancel;
            try
            {
                result = MessageBox.Show(t.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Helper.WriteLogEntry("Report Designer", System.Diagnostics.EventLogEntryType.Error, t.Exception.Message + "\r\n" + t.Exception.StackTrace);
            }
            catch
            {
                try
                {
                    MessageBox.Show("Fatal Windows Forms Error", "Fatal Windows Forms Error", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Stop);
                }
                finally
                {
                    Application.Exit();
                }
            }

            // Exits the program when the user clicks Abort. 
            if (result == DialogResult.Abort) Application.Exit();
        }
    }
}
