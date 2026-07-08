//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
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
            this.Text = String.Format("About {0}", AssemblyTitle.Replace(" Library", ""));
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = String.Format("Version {0}", AssemblyVersion);
            linkLabel.Text = "Get the last version and free support at https://sealreport.org";
            linkLicense.Text = "Services and support at https://sealreport.com";
            ShowIcon = true;
            Icon = Repository.ProductIcon;
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {

                return "Seal Report";
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
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://sealreport.org") { UseShellExecute = true };
            p.Start();
        }

        private void linkLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://sealreport.com") { UseShellExecute = true };
            p.Start();

        }

        private void AboutBoxForm_Shown(object sender, EventArgs e)
        {
            try
            {
                //Show the license information if a license file is installed (e.g. rebranded products), otherwise the default text
                string text = Repository.Instance.LicenseText;
                if (string.IsNullOrWhiteSpace(text))
                {
                    this.textBoxDescription.Text = "\r\nA genuine seal named 'Chocolat' from Dun Laoghaire, Dublin.\r\n\r\nVisit our Web site, take a dive and join the Seal community...\r\n";
                    SoundPlayer simpleSound = new SoundPlayer(Properties.Resources.seal_barking);
                    simpleSound.Play();
                }
                else
                {
                    this.textBoxDescription.Text = text;
                }
            }
            catch { }
        }
    }
}
