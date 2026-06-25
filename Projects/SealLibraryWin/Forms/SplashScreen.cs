using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// Splash screen for the Report Designer and the Server Manager
    /// </summary>
    public partial class SplashScreen : Form
    {
        //Rounded corners
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        //Drop shadow around the borderless window
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        IEntityHandler _mainForm;
        public SplashScreen(IEntityHandler mainForm)
        {
            InitializeComponent();
            _mainForm = mainForm;
            _label.Text = "Initializing...";
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            _versionLabel.Text = string.Format("Version {0}.{1}.{2}", v.Major, v.Minor, v.Build);
            _copyrightLabel.Text = string.Format("© {0} Ariacom - Open Source Reporting Tool", DateTime.Now.Year);
            Opacity = 0;
        }

        //Apply soft rounded corners scaled to the current DPI. Done after the form has been
        //laid out/scaled (Load and any later resize) so it matches the real, DPI-scaled size.
        void applyRoundedRegion()
        {
            int radius = (int)Math.Round(16.0 * DeviceDpi / 96.0);
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, radius, radius));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            applyRoundedRegion();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (IsHandleCreated) applyRoundedRegion();
        }

        Timer _timer;
        Timer _fadeTimer;
        bool _closing = false;

        void timer_tick(object sender, EventArgs e)
        {
            if (_mainForm.IsInitialized() && !_closing)
            {
                _timer.Stop();
                //Trigger the fade-out, the form closes once fully transparent
                _closing = true;
                _fadeTimer.Start();
            }
        }

        void fade_tick(object sender, EventArgs e)
        {
            if (_closing)
            {
                if (Opacity <= 0.05) { _fadeTimer.Stop(); Close(); }
                else Opacity -= 0.10;
            }
            else
            {
                if (Opacity >= 0.95) { Opacity = 1; _fadeTimer.Stop(); }
                else Opacity += 0.10;
            }
        }

        private void SplashScreen_Shown(object sender, EventArgs e)
        {
            //Fade-in / fade-out animation
            _fadeTimer = new Timer { Interval = 25 };
            _fadeTimer.Tick += fade_tick;
            _fadeTimer.Start();

            //Poll the main form initialization status
            _timer = new Timer { Interval = 200 };
            _timer.Tick += timer_tick;
            _timer.Start();
        }
    }
}
