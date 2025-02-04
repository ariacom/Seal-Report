//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using Seal.Model;
using System;
using System.Diagnostics;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Seal.Forms
{
    public partial class AboutBoxForm : Form
    {
        bool _startup = false;

        // Constants for window styles
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        // Import the Windows API functions
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public AboutBoxForm(bool startup = false)
        {
            _startup = startup;

            InitializeComponent();
            this.Text = String.Format("About {0}", AssemblyTitle.Replace(" Library", ""));
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = String.Format("Version {0}", AssemblyVersion);
            linkLabel.Text = "Get the last version and free support at https://sealreport.org";
            linkLicense.Text = "Licensing information at https://sealreport.com";
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
            var defaultText = @"
You are using Seal Report under the MIT Community License:
This license is for non-profit usage or small businesses.

If you are using Seal Report in a production environment,
please ensure that you are eligible to use this free license !

Seal Report follows a dual-licensing model to ensure its maintenance, quality, and support.
";

            if (!_startup)
            {
                defaultText = @"
A genuine seal named 'Chocolat' from Dun Laoghaire, Dublin.

Visit our Web site, take a dive and join the Seal community...

";
            }
            else
            {
                okButton.Visible = false;
                int style = GetWindowLong(this.Handle, GWL_STYLE);
                SetWindowLong(this.Handle, GWL_STYLE, style & ~WS_SYSMENU); 

                var timer = new Timer();
                timer.Interval = 7500;
                timer.Start();
                timer.Tick += Timer_Tick;
            }

            try
            {
                string text = Repository.Instance.LicenseText;
                if (string.IsNullOrWhiteSpace(text))
                {
                    this.textBoxDescription.Text = defaultText;
                    if (!_startup)
                    {
                        SoundPlayer simpleSound = new SoundPlayer(Properties.Resources.seal_barking);
                        simpleSound.Play();
                    }
                }
                else
                {
                    linkLabel.Text = "https://sealreport.org";
                    this.textBoxDescription.Text = text;
                }
            }
            catch { }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            okButton.Visible = true;
            int style = GetWindowLong(this.Handle, GWL_STYLE);
            SetWindowLong(this.Handle, GWL_STYLE, style | WS_SYSMENU); // Enable the system menu (which includes the close button)

        }
    }
}
