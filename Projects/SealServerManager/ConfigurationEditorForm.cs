//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Seal.Helpers;
using Seal.Model;

namespace Seal.Forms
{
    public partial class ConfigurationEditorForm : Form
    {
        SealServerConfiguration _configuration;

        public ConfigurationEditorForm(SealServerConfiguration configuration)
        {
            Visible = false;
            _configuration = configuration;

            InitializeComponent();
            toolStripStatusLabel.Image = null;

            configuration.InitEditor();
            mainPropertyGrid.ToolbarVisible = false;
            mainPropertyGrid.SelectedObject = configuration;
            mainPropertyGrid.PropertySort = PropertySort.Categorized;

            publish1ToolStripButton.Visible = configuration.ForPublication;
            publish2ToolStripButton.Visible = configuration.ForPublication;
            browseToolStripButton.Visible = configuration.ForPublication;
            iisToolStripButton.Visible = configuration.ForPublication;

            Text = Repository.SealRootProductName + (!configuration.ForPublication ? " Server Configuration Editor" : " Web Server Publisher");

            ShowIcon = true;
            Icon = Properties.Resources.serverManager; 
        }

        private void ConfigurationEditorForm_Load(object sender, EventArgs e)
        {

            if (_configuration.ForPublication)
            {
                okToolStripButton.Text = "Close";
                cancelToolStripButton.Visible = false;
                bool serverOk = false;
                try
                {
                    Microsoft.Web.Administration.ServerManager serverMgr = null;
                    serverMgr = new Microsoft.Web.Administration.ServerManager();
                    serverOk = true;
                }
                catch { }
                if (serverOk)
                {
                    infoTextBox.Text = string.Format(@"This simple helper allows to publish the Web Server application on your Internet Information Server version 7 or 8.
It creates an application under the 'Default Web Site' of IIS using the 'LocalSystem' Windows Account.

IIS must be installed with the following features: 
IIS version 7: Application Development/ASP.Net
IIS version 8: Application Development/ASP.Net 3.5, Application Development/ASP.Net 4.5
MVC 4 extension

The site can be configured with any user having the following rights:
Read access to the repository directory ({0}).
Rights to connect to the databases defined in the Data Sources.
Read/Write access to the temp sub-folder created in the published directory ({1}).

Note that publishing will stop the current Web Server instance.
", _configuration.Repository.RepositoryPath, Path.Combine(_configuration.WebPublicationDirectory, "temp"));
                }
                else
                {
                    infoTextBox.Text = @"No Internet Information Server detected on this machine.
Please install IIS with the following features: 
IIS version 7: Application Development/ASP.Net
IIS version 8: Application Development/ASP.Net 3.5, Application Development/ASP.Net 4.5
MVC 4 extension
";
                }
            }
            else
            {
                infoTextBox.Text = @"This editor allows to configure your Seal Server parameters.

New parameter values may require a restart of the Report Designer or the Web Server.";
            }
            Visible = true;
        }


        private void cancelToolStripButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void okToolStripButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        bool _publicationInError = false;
        private void publishToolStripButton_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(delegate(object param) { publish((ExecutionLogInterface)param, (sender == publish2ToolStripButton)); });
            if (thread != null)
            {
                ExecutionForm frm = new ExecutionForm(thread);
                frm.ShowDialog();
            }

            if (!_publicationInError && MessageBox.Show("The site has been successfully published. Do you want to try it now ?", "Publisher", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                browseToolStripButton_Click(null, null);
            }
        }

        void publish(ExecutionLogInterface log, bool filesOnly)
        {
            log.Log("Starting Web Site Publishing...");
            _publicationInError = false;
            try
            {
                string publicationDirectory = _configuration.WebPublicationDirectory;
                string sourceDirectory = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Web");
#if DEBUG
                sourceDirectory = Path.GetDirectoryName(Application.ExecutablePath) + @"\..\..\..\SealWebServer\";
#endif

                //Copy installation directory
                log.Log("Copying files from '{0}' to '{1}'", sourceDirectory, publicationDirectory);
                FileHelper.CopyDirectory(sourceDirectory, publicationDirectory, true);

                //Check web config...
                if (!File.Exists(Path.Combine(publicationDirectory, "web.config")) && File.Exists(Path.Combine(publicationDirectory, "web.release.config")))
                {
                    log.Log("Creating web.config file");
                    File.Copy(Path.Combine(publicationDirectory, "web.release.config"), Path.Combine(publicationDirectory, "web.config"));
                }

                if (!filesOnly && !log.IsJobCancelled())
                {
                    log.Log("Publishing Site on IIS...");

                    Microsoft.Web.Administration.ServerManager serverMgr = new Microsoft.Web.Administration.ServerManager();
                    Microsoft.Web.Administration.Site site = null;
                    if (serverMgr.Sites.Count == 0)
                    {
                        log.Log("Creating Default Web Site");
                        site = serverMgr.Sites.Add("Default Web Site", "C:\\inetpub\\wwwroot", 80);
                    }
                    else site = serverMgr.Sites[0];

                    Microsoft.Web.Administration.ApplicationPool pool = serverMgr.ApplicationPools.FirstOrDefault(i => i.Name == _configuration.WebApplicationPoolName);
                    if (pool == null)
                    {
                        log.Log("Creating Application Pool");
                        pool = serverMgr.ApplicationPools.Add(_configuration.WebApplicationPoolName);
                    }
                    pool.ManagedRuntimeVersion = "v4.0";
                    pool.Enable32BitAppOnWin64 = true;
                    pool.ProcessModel.IdentityType = Microsoft.Web.Administration.ProcessModelIdentityType.LocalSystem;

                    Microsoft.Web.Administration.Application application = site.Applications.FirstOrDefault(i => i.Path == _configuration.WebApplicationName);
                    if (application == null)
                    {
                        log.Log("Creating Application");
                        application = site.Applications.Add(_configuration.WebApplicationName, _configuration.WebPublicationDirectory);
                    }
                    application.ApplicationPoolName = _configuration.WebApplicationPoolName;
                    if (!log.IsJobCancelled())
                    {
                        serverMgr.CommitChanges();
                    }
                    log.Log("Web Site has been published successfully.");
                }
            }
            catch (Exception ex)
            {
                _publicationInError = true;
                log.Log("\r\n[UNEXPECTED ERROR RECEIVED]\r\n{0}\r\n", ex.Message);
                if (ex.InnerException != null) log.Log("{0}\r\n", ex.InnerException.Message);
            }
            log.Log("Web Site Publishing terminated.");
            if (log.IsJobCancelled()) log.Log("Publication has been cancelled.");
        }

        private void browseToolStripButton_Click(object sender, EventArgs e)
        {
            string url = string.Format("http://localhost{0}/Main", _configuration.WebApplicationName);
            Process.Start(url);
        }

        private void iisManagerToolStripButton_Click(object sender, EventArgs e)
        {
            Process.Start(System.Environment.SystemDirectory + @"\inetsrv\iis.msc", "/s");
        }

    }
}
