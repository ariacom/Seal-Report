//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
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
            Application.Run(new ServerManager());
        }

        private static void ExceptionHandler(object sender, ThreadExceptionEventArgs t)
        {
            DialogResult result = DialogResult.Cancel;
            try
            {
                result = MessageBox.Show(t.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Helper.WriteLogEntry("Server Manager", System.Diagnostics.EventLogEntryType.Error, t.Exception.Message + "\r\n" + t.Exception.StackTrace);
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
