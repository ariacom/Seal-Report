//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Model;
using System;
using System.Diagnostics;
using System.Media;
using System.Reflection;
using System.Windows.Forms;

namespace Seal.Forms
{
    public partial class AboutBoxForm : Form
    {
        public AboutBoxForm()
        {
            InitializeComponent();
            this.Text = String.Format("About {0}", AssemblyTitle.Replace(" Library",""));
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = String.Format("Version {0}", AssemblyVersion);
            linkLabel.Text = "Get the last version and free support at http://wwww.sealreport.org";
            ShowIcon = true;
            Icon = Repository.ProductIcon;
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void linkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.sealreport.org");
        }

        private void AboutBoxForm_Shown(object sender, EventArgs e)
        {
            var defaultText = "A genuine seal named 'Chocolat' from Dun Laoghaire, Dublin.\r\n\r\nVisit our Web site, take a dive and join the Seal community...\r\n\r\n\r\n";
            defaultText += "Copyright(c) Seal Report, Ariacom (www.ariacom.com).\r\n\r\n";
            defaultText += "Seal Report is licensed under the Apache License, Version 2.0.\r\nhttp://www.apache.org/licenses/LICENSE-2.0.";
            try
            {
                string text = "";
                try
                {
                    var si = SealInterface.Create(Repository.Instance);
                    si.Init();
                    text = Repository.Instance.LicenseText + "\r\n\r\n" + si.Text();
                    text = text.Trim();
                }
                catch { }

                if (string.IsNullOrWhiteSpace(text))
                {
                    this.textBoxDescription.Text = defaultText;
                    SoundPlayer simpleSound = new SoundPlayer(Properties.Resources.seal_barking);
                    simpleSound.Play();
                }
                else
                {
                    linkLabel.Text = "http://www.sealreport.org";
                    this.textBoxDescription.Text = text;
                }
            }
            catch { }
        }
    }
}
