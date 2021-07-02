#region Using Directives

using ScintillaNET;
using System;
using System.Drawing;
using System.Windows.Forms;

#endregion Using Directives

namespace ScintillaNET_FindReplaceDialog
{
    public partial class GoToDialog : Form
    {
        #region Fields

        private Scintilla _scintilla;
        private int _currentLineNumber;
        private int _maximumLineNumber;
        private int _gotoLineNumber;

        #endregion Fields

        #region Methods

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtGotoLine.Text, out _gotoLineNumber))
            {
                //	Line #s are 0 based but the users don't think that way
                _gotoLineNumber--;
                if (_gotoLineNumber < 0 || _gotoLineNumber >= _maximumLineNumber)
                    err.SetError(txtGotoLine, "Go to line # must be greater than 0 and less than " + (_maximumLineNumber + 1).ToString());
                else
                {
                    _scintilla.Lines[_gotoLineNumber].Goto();
                    //Line(GotoLineNumber);
                    this.Hide();
                    //DialogResult = DialogResult.OK;
                }
            }
            else
            {
                err.SetError(txtGotoLine, "Go to line # must be a numeric value");
            }
        }

        // This was taken from FindReplaceDialog. Obviously some refactoring is called for
        // since we have common code. However I'm holding off on this because I'm coming
        // up with some other ideas for the FindReplaceDialog. Right now every scintilla
        // gets its own FindReplaceDialog, but they really need to be sharable across
        // multiple scintillas much like how DropMarkers work.

        private void MoveFormAwayFromSelection()
        {
            if (!Visible)
                return;

            int pos = Scintilla.CurrentPosition;
            int x = Scintilla.PointXFromPosition(pos);
            int y = Scintilla.PointYFromPosition(pos);

            Point cursorPoint = Scintilla.PointToScreen(new Point(x, y));

            Rectangle r = new Rectangle(Location, Size);
            if (r.Contains(cursorPoint))
            {
                Point newLocation;
                if (cursorPoint.Y < (Screen.PrimaryScreen.Bounds.Height / 2))
                {
                    //TODO - replace lineheight with ScintillaNET command, when added
                    int SCI_TEXTHEIGHT = 2279;
                    int lineHeight = Scintilla.DirectMessage(SCI_TEXTHEIGHT, IntPtr.Zero, IntPtr.Zero).ToInt32();
                    // Top half of the screen
                    newLocation = Scintilla.PointToClient(
                        new Point(Location.X, cursorPoint.Y + lineHeight * 2)
                        );
                }
                else
                {
                    //TODO - replace lineheight with ScintillaNET command, when added
                    int SCI_TEXTHEIGHT = 2279;
                    int lineHeight = Scintilla.DirectMessage(SCI_TEXTHEIGHT, IntPtr.Zero, IntPtr.Zero).ToInt32();
                    // Bottom half of the screen
                    newLocation = Scintilla.PointToClient(
                        new Point(Location.X, cursorPoint.Y - Height - (lineHeight * 2))
                        );
                }
                newLocation = Scintilla.PointToScreen(newLocation);
                Location = newLocation;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            MoveFormAwayFromSelection();
        }

        #endregion Methods

        #region Properties

        public int CurrentLineNumber
        {
            get { return _currentLineNumber; }
            set
            {
                _currentLineNumber = value;
            }
        }

        public int GotoLineNumber
        {
            get { return _gotoLineNumber; }
            set
            {
                _gotoLineNumber = value;
            }
        }

        public int MaximumLineNumber
        {
            get { return _maximumLineNumber; }
            set
            {
                _maximumLineNumber = value;
            }
        }

        public Scintilla Scintilla
        {
            get { return _scintilla; }
            set { _scintilla = value; }
        }

        #endregion Properties

        #region Constructors

        public GoToDialog()
        {
            InitializeComponent();
        }

        #endregion Constructors

        private void GoToDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void GoToDialog_Activated(object sender, EventArgs e)
        {
            string displayLine = (_currentLineNumber + 1).ToString();

            txtCurrentLine.Text = displayLine;
            txtMaxLine.Text = _maximumLineNumber.ToString();
            txtGotoLine.Text = displayLine;

            txtGotoLine.Select();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Hide();
        }
    }
}