using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Seal.Forms
{
    /// <summary>
    /// Splash screen for the Report Designer and the Server Manager
    /// </summary>
    public partial class SplashScreen : Form
    {
        IEntityHandler _mainForm;
        public SplashScreen(IEntityHandler mainForm)
        {
            InitializeComponent();
            _mainForm = mainForm;
            _label.Text = "Initializing";
        }

        Timer _timer;

        int _cnt = 1;
        void timer_tick(object sender, EventArgs e)

        {
            if (_mainForm.IsInitialized())
            {
                _timer.Stop();
                Close();
            }
            else
            {
                if (_cnt++ % 5 == 0 && Visible)
                {
                    _label.Text += ".";
                    if (_label.Text == "Initializing.....") _label.Text = "Initializing";
                }
            }
        }

        private void SplashScreen_Shown(object sender, EventArgs e)
        {
            _timer = new Timer();
            _timer.Interval = 200;
            _timer.Start();
            _timer.Tick += timer_tick;
        }
    }
}
