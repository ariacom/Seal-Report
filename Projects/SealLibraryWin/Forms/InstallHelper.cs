//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Windows.Forms;
using Seal.Model;
using Seal.Helpers;
using System.Threading;
using System.IO;
using System.Net;
using System.Reflection;

namespace Seal.Forms
{
    public class InstallHelper
    {
        public static void InstallConverter(ToolStripMenuItem menu, string destinationFolder)
        {
            if (!File.Exists(Path.Combine(destinationFolder,Repository.SealConverterDll))) {
                var mi = new ToolStripMenuItem() { Text = "Install the PDF and Excel Converter..." };
                menu.DropDownItems.Add(new ToolStripSeparator());
                menu.DropDownItems.Add(mi);
                mi.Click += new EventHandler(delegate (object sender, EventArgs e)
                {
                    if (MessageBox.Show("This will install the PDF and Excel converter component (trial version) from Ariacom.\r\n\r\nDo you want to continue ?", "PDF and Excel Converter installation", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        try
                        {
                            Cursor.Current = Cursors.WaitCursor;
                            Thread thread = new Thread(delegate (object param)
                            {
                                var log = (ExecutionLogInterface)param;
                                var version = Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 3);
                                try
                                {
                                    WebClient myWebClient = new WebClient();
                                    var zipPath = FileHelper.GetTempUniqueFileName("converter.zip");
                                    log.Log("Downloading version {0} from https://ariacom.com...", version);
                                    var url = "https://ariacom.com/SealConverterDownload?v=" + version.Replace(".", "%2E");
                                    myWebClient.DownloadFile(url, zipPath);
                                    if (log.IsJobCancelled()) return;
                                    log.Log("Extracting files to your Repository Assemblies directory '{0}'...", destinationFolder);
                                    FileHelper.ExtractZipFile(zipPath, "", destinationFolder);

                                    log.Log("Installation completed.\r\n");
                                    log.Log("Please restart your applications to try the converter:");
                                    log.Log("Run the Report Designer, execute a report and view the result in PDF and Excel.");

                                    File.Delete(zipPath);
                                }
                                catch (Exception ex)
                                {
                                    log.Log("Unexpected exception:\r\n" + ex.Message + "\r\n");
                                    log.Log("Please go to https://ariacom.com/SealConverterDownload");
                                }
                            });

                            ExecutionForm frm = new ExecutionForm(thread);
                            frm.Text = "PDF and Excel Converter installation";
                            frm.ShowDialog();
                        }
                        finally
                        {
                            Cursor.Current = Cursors.Default;
                        }
                    }
                });
            }
        }

        private static void @delegate(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}

