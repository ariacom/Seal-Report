namespace ScintillaNET_FindReplaceDialog
{
    using ScintillaNET;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;

    public partial class FindReplaceDialog : Form
    {
        #region Fields

        private bool _autoPosition;
        private BindingSource _bindingSourceFind = new BindingSource();
        private BindingSource _bindingSourceReplace = new BindingSource();
        private List<string> _mruFind;
        private int _mruMaxCount = 10;
        private List<string> _mruReplace;
        private Scintilla _scintilla;
        private CharacterRange _searchRange;

        #endregion Fields

        public event KeyPressedHandler KeyPressed;

        public delegate void KeyPressedHandler(object sender, KeyEventArgs e);

        #region Constructors

        public FindReplaceDialog()
        {
            InitializeComponent();

            _autoPosition = true;
            _mruFind = new List<string>();
            _mruReplace = new List<string>();
            _bindingSourceFind.DataSource = _mruFind;
            _bindingSourceReplace.DataSource = _mruReplace;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets whether the dialog should automatically move away from the current
        /// selection to prevent obscuring it.
        /// </summary>
        /// <returns>true to automatically move away from the current selection; otherwise, false.</returns>
        public bool AutoPosition
        {
            get
            {
                return _autoPosition;
            }
            set
            {
                _autoPosition = value;
            }
        }

        public List<string> MruFind
        {
            get
            {
                return _mruFind;
            }
            set
            {
                _mruFind = value;
                _bindingSourceFind.DataSource = _mruFind;
            }
        }

        public int MruMaxCount
        {
            get { return _mruMaxCount; }
            set { _mruMaxCount = value; }
        }

        public List<string> MruReplace
        {
            get
            {
                return _mruReplace;
            }
            set
            {
                _mruReplace = value;
                _bindingSourceReplace.DataSource = _mruReplace;
            }
        }

        public Scintilla Scintilla
        {
            get
            {
                return _scintilla;
            }
            set
            {
                _scintilla = value;
            }
        }

        public FindReplace FindReplace { get; set; }

        #endregion Properties

        #region Form Event Handlers

        private void FindReplaceDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        #endregion Form Event Handlers

        #region Event Handlers

        private void btnClear_Click(object sender, EventArgs e)
        {
            _scintilla.MarkerDeleteAll(FindReplace.Marker.Index);
            FindReplace.ClearAllHighlights();
        }

        private void btnFindAll_Click(object sender, EventArgs e)
        {
            if (txtFindF.Text == string.Empty)
                return;

            AddFindMru();

            lblStatus.Text = string.Empty;

            btnClear_Click(null, null);
            int foundCount = 0;

            #region RegEx

            if (rdoRegexF.Checked)
            {
                Regex rr = null;
                try
                {
                    rr = new Regex(txtFindF.Text, GetRegexOptions());
                }
                catch (ArgumentException ex)
                {
                    lblStatus.Text = "Error in Regular Expression: " + ex.Message;
                    return;
                }

                if (chkSearchSelectionF.Checked)
                {
                    if (_searchRange.cpMin == _searchRange.cpMax)
                    {
                        _searchRange = new CharacterRange(_scintilla.Selections[0].Start, _scintilla.Selections[0].End);
                    }

                    foundCount = FindReplace.FindAll(_searchRange, rr, chkMarkLine.Checked, chkHighlightMatches.Checked).Count;
                }
                else
                {
                    _searchRange = new CharacterRange();
                    foundCount = FindReplace.FindAll(rr, chkMarkLine.Checked, chkHighlightMatches.Checked).Count;
                }
            }

            #endregion

            #region Non-RegEx

            if (!rdoRegexF.Checked)
            {
                if (chkSearchSelectionF.Checked)
                {
                    if (_searchRange.cpMin == _searchRange.cpMax)
                        _searchRange = new CharacterRange(_scintilla.Selections[0].Start, _scintilla.Selections[0].End);

                    string textToFind = rdoExtendedF.Checked ? FindReplace.Transform(txtFindF.Text) : txtFindF.Text;
                    foundCount = FindReplace.FindAll(_searchRange, textToFind, GetSearchFlags(), chkMarkLine.Checked, chkHighlightMatches.Checked).Count;
                }
                else
                {
                    _searchRange = new CharacterRange();
                    string textToFind = rdoExtendedF.Checked ? FindReplace.Transform(txtFindF.Text) : txtFindF.Text;
                    foundCount = FindReplace.FindAll(textToFind, GetSearchFlags(), chkMarkLine.Checked, chkHighlightMatches.Checked).Count;
                }
            }

            #endregion

            lblStatus.Text = "Total found: " + foundCount.ToString();
        }

        private void btnFindNext_Click(object sender, EventArgs e)
        {
            FindNext();
        }

        private void btnFindPrevious_Click(object sender, EventArgs e)
        {
            FindPrevious();
        }

        private void btnReplaceAll_Click(object sender, EventArgs e)
        {
            if (txtFindR.Text == string.Empty)
                return;

            AddReplaceMru();
            lblStatus.Text = string.Empty;
            int foundCount = 0;

            #region RegEx

            if (rdoRegexR.Checked)
            {
                Regex rr = null;
                try
                {
                    rr = new Regex(txtFindR.Text, GetRegexOptions());
                }
                catch (ArgumentException ex)
                {
                    lblStatus.Text = "Error in Regular Expression: " + ex.Message;
                    return;
                }

                if (chkSearchSelectionR.Checked)
                {
                    if (_searchRange.cpMin == _searchRange.cpMax)
                    {
                        _searchRange = new CharacterRange(_scintilla.Selections[0].Start, _scintilla.Selections[0].End);
                    }

                    foundCount = FindReplace.ReplaceAll(_searchRange, rr, txtReplace.Text, false, false);
                }
                else
                {
                    _searchRange = new CharacterRange();
                    foundCount = FindReplace.ReplaceAll(rr, txtReplace.Text, false, false);
                }
            }

            #endregion

            #region Non-RegEx

            if (!rdoRegexR.Checked)
            {
                if (chkSearchSelectionR.Checked)
                {
                    if (_searchRange.cpMin == _searchRange.cpMax)
                        _searchRange = new CharacterRange(_scintilla.Selections[0].Start, _scintilla.Selections[0].End);

                    string textToFind = rdoExtendedR.Checked ? FindReplace.Transform(txtFindR.Text) : txtFindR.Text;
                    string textToReplace = rdoExtendedR.Checked ? FindReplace.Transform(txtReplace.Text) : txtReplace.Text;
                    foundCount = FindReplace.ReplaceAll(_searchRange, textToFind, textToReplace, GetSearchFlags(), false, false);
                }
                else
                {
                    _searchRange = new CharacterRange();
                    string textToFind = rdoExtendedR.Checked ? FindReplace.Transform(txtFindR.Text) : txtFindR.Text;
                    string textToReplace = rdoExtendedR.Checked ? FindReplace.Transform(txtReplace.Text) : txtReplace.Text;
                    foundCount = FindReplace.ReplaceAll(textToFind, textToReplace, GetSearchFlags(), false, false);
                }
            }

            #endregion

            lblStatus.Text = "Total Replaced: " + foundCount.ToString();
        }

        private void btnReplaceNext_Click(object sender, EventArgs e)
        {
            ReplaceNext();
        }

        private void btnReplacePrevious_Click(object sender, EventArgs e)
        {
            if (txtFindR.Text == string.Empty)
                return;

            AddReplaceMru();
            lblStatus.Text = string.Empty;

            CharacterRange nextRange;
            try
            {
                nextRange = ReplaceNext(true);
            }
            catch (ArgumentException ex)
            {
                lblStatus.Text = "Error in Regular Expression: " + ex.Message;
                return;
            }

            if (nextRange.cpMin == nextRange.cpMax)
            {
                lblStatus.Text = "Match could not be found";
            }
            else
            {
                if (nextRange.cpMin > _scintilla.AnchorPosition)
                {
                    if (chkSearchSelectionR.Checked)
                        lblStatus.Text = "Search match wrapped to the beginning of the selection";
                    else
                        lblStatus.Text = "Search match wrapped to the beginning of the document";
                }

                _scintilla.SetSel(nextRange.cpMin, nextRange.cpMax);
                MoveFormAwayFromSelection();
            }
        }

        private void chkEcmaScript_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                chkExplicitCaptureF.Checked = false;
                chkExplicitCaptureR.Checked = false;
                chkExplicitCaptureF.Enabled = false;
                chkExplicitCaptureR.Enabled = false;
                chkIgnorePatternWhitespaceF.Checked = false;
                chkIgnorePatternWhitespaceR.Checked = false;
                chkIgnorePatternWhitespaceF.Enabled = false;
                chkIgnorePatternWhitespaceR.Enabled = false;
                chkRightToLeftF.Checked = false;
                chkRightToLeftR.Checked = false;
                chkRightToLeftF.Enabled = false;
                chkRightToLeftR.Enabled = false;
                chkSinglelineF.Checked = false;
                chkSinglelineR.Checked = false;
                chkSinglelineF.Enabled = false;
                chkSinglelineR.Enabled = false;
            }
            else
            {
                chkExplicitCaptureF.Enabled = true;
                chkIgnorePatternWhitespaceF.Enabled = true;
                chkRightToLeftF.Enabled = true;
                chkSinglelineF.Enabled = true;
                chkExplicitCaptureR.Enabled = true;
                chkIgnorePatternWhitespaceR.Enabled = true;
                chkRightToLeftR.Enabled = true;
                chkSinglelineR.Enabled = true;
            }
        }

        private void cmdRecentFindF_Click(object sender, EventArgs e)
        {
            mnuRecentFindF.Items.Clear();
            foreach (var item in MruFind)
            {
                ToolStripItem newItem = mnuRecentFindF.Items.Add(item);
                newItem.Tag = item;
            }
            mnuRecentFindF.Items.Add("-");
            mnuRecentFindF.Items.Add("Clear History");
            mnuRecentFindF.Show(cmdRecentFindF.PointToScreen(cmdRecentFindF.ClientRectangle.Location));
        }

        private void cmdRecentFindR_Click(object sender, EventArgs e)
        {
            mnuRecentFindR.Items.Clear();
            foreach (var item in MruFind)
            {
                ToolStripItem newItem = mnuRecentFindR.Items.Add(item);
                newItem.Tag = item;
            }
            mnuRecentFindR.Items.Add("-");
            mnuRecentFindR.Items.Add("Clear History");
            mnuRecentFindR.Show(cmdRecentFindR.PointToScreen(cmdRecentFindR.ClientRectangle.Location));
        }

        private void cmdRecentReplace_Click(object sender, EventArgs e)
        {
            mnuRecentReplace.Items.Clear();
            foreach (var item in MruReplace)
            {
                ToolStripItem newItem = mnuRecentReplace.Items.Add(item);
                newItem.Tag = item;
            }
            mnuRecentReplace.Items.Add("-");
            mnuRecentReplace.Items.Add("Clear History");
            mnuRecentReplace.Show(cmdRecentReplace.PointToScreen(cmdRecentReplace.ClientRectangle.Location));
        }

        private void cmdExtendedCharFindF_Click(object sender, EventArgs e)
        {
            if (rdoExtendedF.Checked)
            {
                mnuExtendedCharFindF.Show(cmdExtCharAndRegExFindF.PointToScreen(cmdExtCharAndRegExFindF.ClientRectangle.Location));
            }
            else if (rdoRegexF.Checked)
            {
                mnuRegExCharFindF.Show(cmdExtCharAndRegExFindF.PointToScreen(cmdExtCharAndRegExFindF.ClientRectangle.Location));
            }
        }

        private void cmdExtendedCharFindR_Click(object sender, EventArgs e)
        {
            if (rdoExtendedR.Checked)
            {
                mnuExtendedCharFindR.Show(cmdExtCharAndRegExFindR.PointToScreen(cmdExtCharAndRegExFindR.ClientRectangle.Location));
            }
            else if (rdoRegexR.Checked)
            {
                mnuRegExCharFindR.Show(cmdExtCharAndRegExFindR.PointToScreen(cmdExtCharAndRegExFindR.ClientRectangle.Location));
            }
        }

        private void cmdExtendedCharReplace_Click(object sender, EventArgs e)
        {
            if (rdoExtendedR.Checked)
            {
                mnuExtendedCharReplace.Show(cmdExtCharAndRegExReplace.PointToScreen(cmdExtCharAndRegExReplace.ClientRectangle.Location));
            }
            else if (rdoRegexR.Checked)
            {
                mnuRegExCharReplace.Show(cmdExtCharAndRegExReplace.PointToScreen(cmdExtCharAndRegExReplace.ClientRectangle.Location));
            }
        }

        private void rdoStandardF_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoStandardF.Checked)
                pnlStandardOptionsF.BringToFront();
            else
                pnlRegexpOptionsF.BringToFront();

            cmdExtCharAndRegExFindF.Enabled = !rdoStandardF.Checked;
        }

        private void rdoStandardR_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoStandardR.Checked)
                pnlStandardOptionsR.BringToFront();
            else
                pnlRegexpOptionsR.BringToFront();

            cmdExtCharAndRegExFindR.Enabled = !rdoStandardR.Checked;
            cmdExtCharAndRegExReplace.Enabled = !rdoStandardR.Checked;
        }

        private void tabAll_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabAll.SelectedTab == tpgFind)
            {
                txtFindF.Text = txtFindR.Text;

                rdoStandardF.Checked = rdoStandardR.Checked;
                rdoExtendedF.Checked = rdoExtendedR.Checked;
                rdoRegexF.Checked = rdoRegexR.Checked;

                chkWrapF.Checked = chkWrapR.Checked;
                chkSearchSelectionF.Checked = chkSearchSelectionR.Checked;

                chkMatchCaseF.Checked = chkMatchCaseR.Checked;
                chkWholeWordF.Checked = chkWholeWordR.Checked;
                chkWordStartF.Checked = chkWordStartR.Checked;

                chkCompiledF.Checked = chkCompiledR.Checked;
                chkCultureInvariantF.Checked = chkCultureInvariantR.Checked;
                chkEcmaScriptF.Checked = chkEcmaScriptR.Checked;
                chkExplicitCaptureF.Checked = chkExplicitCaptureR.Checked;
                chkIgnoreCaseF.Checked = chkIgnoreCaseR.Checked;
                chkIgnorePatternWhitespaceF.Checked = chkIgnorePatternWhitespaceR.Checked;
                chkMultilineF.Checked = chkMultilineR.Checked;
                chkRightToLeftF.Checked = chkRightToLeftR.Checked;
                chkSinglelineF.Checked = chkSinglelineR.Checked;

                AcceptButton = btnFindNextF;
            }
            else
            {
                txtFindR.Text = txtFindF.Text;

                rdoStandardR.Checked = rdoStandardF.Checked;
                rdoExtendedR.Checked = rdoExtendedF.Checked;
                rdoRegexR.Checked = rdoRegexF.Checked;

                chkWrapR.Checked = chkWrapF.Checked;
                chkSearchSelectionR.Checked = chkSearchSelectionF.Checked;

                chkMatchCaseR.Checked = chkMatchCaseF.Checked;
                chkWholeWordR.Checked = chkWholeWordF.Checked;
                chkWordStartR.Checked = chkWordStartF.Checked;

                chkCompiledR.Checked = chkCompiledF.Checked;
                chkCultureInvariantR.Checked = chkCultureInvariantF.Checked;
                chkEcmaScriptR.Checked = chkEcmaScriptF.Checked;
                chkExplicitCaptureR.Checked = chkExplicitCaptureF.Checked;
                chkIgnoreCaseR.Checked = chkIgnoreCaseF.Checked;
                chkIgnorePatternWhitespaceR.Checked = chkIgnorePatternWhitespaceF.Checked;
                chkMultilineR.Checked = chkMultilineF.Checked;
                chkRightToLeftR.Checked = chkRightToLeftF.Checked;
                chkSinglelineR.Checked = chkSinglelineF.Checked;

                AcceptButton = btnReplaceNext;
            }
        }

        #endregion Event Handlers

        #region Methods

        public void FindNext()
        {
            SyncSearchText();

            if (txtFindF.Text == string.Empty)
                return;

            AddFindMru();
            lblStatus.Text = string.Empty;

            CharacterRange foundRange;

            try
            {
                foundRange = FindNextF(false);
            }
            catch (ArgumentException ex)
            {
                lblStatus.Text = "Error in Regular Expression: " + ex.Message;
                return;
            }

            if (foundRange.cpMin == foundRange.cpMax)
            {
                lblStatus.Text = "Match could not be found";
            }
            else
            {
                if (foundRange.cpMin < Scintilla.AnchorPosition)
                {
                    if (chkSearchSelectionF.Checked)
                        lblStatus.Text = "Search match wrapped to the beginning of the selection";
                    else
                        lblStatus.Text = "Search match wrapped to the beginning of the document";
                }

                Scintilla.SetSel(foundRange.cpMin, foundRange.cpMax);
                MoveFormAwayFromSelection();
            }
        }

        public void FindPrevious()
        {
            SyncSearchText();

            if (txtFindF.Text == string.Empty)
                return;

            AddFindMru();
            lblStatus.Text = string.Empty;
            CharacterRange foundRange;
            try
            {
                foundRange = FindNextF(true);
            }
            catch (ArgumentException ex)
            {
                lblStatus.Text = "Error in Regular Expression: " + ex.Message;
                return;
            }

            if (foundRange.cpMin == foundRange.cpMax)
            {
                lblStatus.Text = "Match could not be found";
            }
            else
            {
                if (foundRange.cpMin > Scintilla.CurrentPosition)
                {
                    if (chkSearchSelectionF.Checked)
                        lblStatus.Text = "Search match wrapped to the _end of the selection";
                    else
                        lblStatus.Text = "Search match wrapped to the _end of the document";
                }

                Scintilla.SetSel(foundRange.cpMin, foundRange.cpMax);
                MoveFormAwayFromSelection();
            }
        }

        public RegexOptions GetRegexOptions()
        {
            RegexOptions ro = RegexOptions.None;

            if (tabAll.SelectedTab == tpgFind)
            {
                if (chkCompiledF.Checked)
                    ro |= RegexOptions.Compiled;

                if (chkCultureInvariantF.Checked)
                    ro |= RegexOptions.Compiled;

                if (chkEcmaScriptF.Checked)
                    ro |= RegexOptions.ECMAScript;

                if (chkExplicitCaptureF.Checked)
                    ro |= RegexOptions.ExplicitCapture;

                if (chkIgnoreCaseF.Checked)
                    ro |= RegexOptions.IgnoreCase;

                if (chkIgnorePatternWhitespaceF.Checked)
                    ro |= RegexOptions.IgnorePatternWhitespace;

                if (chkMultilineF.Checked)
                    ro |= RegexOptions.Multiline;

                if (chkRightToLeftF.Checked)
                    ro |= RegexOptions.RightToLeft;

                if (chkSinglelineF.Checked)
                    ro |= RegexOptions.Singleline;
            }
            else
            {
                if (chkCompiledR.Checked)
                    ro |= RegexOptions.Compiled;

                if (chkCultureInvariantR.Checked)
                    ro |= RegexOptions.Compiled;

                if (chkEcmaScriptR.Checked)
                    ro |= RegexOptions.ECMAScript;

                if (chkExplicitCaptureR.Checked)
                    ro |= RegexOptions.ExplicitCapture;

                if (chkIgnoreCaseR.Checked)
                    ro |= RegexOptions.IgnoreCase;

                if (chkIgnorePatternWhitespaceR.Checked)
                    ro |= RegexOptions.IgnorePatternWhitespace;

                if (chkMultilineR.Checked)
                    ro |= RegexOptions.Multiline;

                if (chkRightToLeftR.Checked)
                    ro |= RegexOptions.RightToLeft;

                if (chkSinglelineR.Checked)
                    ro |= RegexOptions.Singleline;
            }

            return ro;
        }

        public SearchFlags GetSearchFlags()
        {
            SearchFlags sf = SearchFlags.None;

            if (tabAll.SelectedTab == tpgFind)
            {
                if (chkMatchCaseF.Checked)
                    sf |= SearchFlags.MatchCase;

                if (chkWholeWordF.Checked)
                    sf |= SearchFlags.WholeWord;

                if (chkWordStartF.Checked)
                    sf |= SearchFlags.WordStart;
            }
            else
            {
                if (chkMatchCaseR.Checked)
                    sf |= SearchFlags.MatchCase;

                if (chkWholeWordR.Checked)
                    sf |= SearchFlags.WholeWord;

                if (chkWordStartR.Checked)
                    sf |= SearchFlags.WordStart;
            }

            return sf;
        }

        public virtual void MoveFormAwayFromSelection()
        {
            if (!Visible)
                return;

            if (!AutoPosition)
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
                    // int lineHeight = Scintilla.Lines[Scintilla.LineFromPosition(pos)].Height;
                    
                    // Top half of the screen
                    newLocation = Scintilla.PointToClient(
                        new Point(Location.X, cursorPoint.Y + lineHeight * 2));
                }
                else
                {
                    //TODO - replace lineheight with ScintillaNET command, when added
                    int SCI_TEXTHEIGHT = 2279;
                    int lineHeight = Scintilla.DirectMessage(SCI_TEXTHEIGHT, IntPtr.Zero, IntPtr.Zero).ToInt32();
                    // int lineHeight = Scintilla.Lines[Scintilla.LineFromPosition(pos)].Height;
                    
                    // Bottom half of the screen
                    newLocation = Scintilla.PointToClient(
                        new Point(Location.X, cursorPoint.Y - Height - (lineHeight * 2)));
                }
                newLocation = Scintilla.PointToScreen(newLocation);
                Location = newLocation;
            }
        }

        public void ReplaceNext()
        {
            if (txtFindR.Text == string.Empty)
                return;

            AddReplaceMru();
            lblStatus.Text = string.Empty;

            CharacterRange nextRange;
            try
            {
                nextRange = ReplaceNext(false);
            }
            catch (ArgumentException ex)
            {
                lblStatus.Text = "Error in Regular Expression: " + ex.Message;
                return;
            }

            if (nextRange.cpMin == nextRange.cpMax)
            {
                lblStatus.Text = "Match could not be found";
            }
            else
            {
                if (nextRange.cpMin < Scintilla.AnchorPosition)
                {
                    if (chkSearchSelectionR.Checked)
                        lblStatus.Text = "Search match wrapped to the beginning of the selection";
                    else
                        lblStatus.Text = "Search match wrapped to the beginning of the document";
                }

                Scintilla.SetSel(nextRange.cpMin, nextRange.cpMax);
                MoveFormAwayFromSelection();
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            if (Scintilla.Selections.Count > 0)
            {
                chkSearchSelectionF.Enabled = true;
                chkSearchSelectionR.Enabled = true;
            }
            else
            {
                chkSearchSelectionF.Enabled = false;
                chkSearchSelectionR.Enabled = false;
                chkSearchSelectionF.Checked = false;
                chkSearchSelectionR.Checked = false;
            }

            //	if they leave the dialog and come back any "Search Selection"
            //	range they might have had is invalidated
            _searchRange = new CharacterRange();

            lblStatus.Text = string.Empty;

            MoveFormAwayFromSelection();

            base.OnActivated(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            //	So what we're doing here is testing for any of the find/replace
            //	command shortcut bindings. If the key combination matches we send
            //	the KeyEventArgs back to Scintilla so it can be processed. That
            //	way things like Find Next, Show Replace are all available from
            //	the dialog using Scintilla's configured Shortcuts

            //List<KeyBinding> findNextBinding = Scintilla.Commands.GetKeyBindings(BindableCommand.FindNext);
            //List<KeyBinding> findPrevBinding = Scintilla.Commands.GetKeyBindings(BindableCommand.FindPrevious);
            //List<KeyBinding> showFindBinding = Scintilla.Commands.GetKeyBindings(BindableCommand.ShowFind);
            //List<KeyBinding> showReplaceBinding = Scintilla.Commands.GetKeyBindings(BindableCommand.ShowReplace);

            //KeyBinding kb = new KeyBinding(e.KeyCode, e.Modifiers);

            //if (findNextBinding.Contains(kb) || findPrevBinding.Contains(kb) || showFindBinding.Contains(kb) || showReplaceBinding.Contains(kb))
            //{
            //Scintilla. FireKeyDown(e);
            //}

            if (KeyPressed != null)
                KeyPressed(this, e);

            if (e.KeyCode == Keys.Escape)
                Hide();

            base.OnKeyDown(e);
        }

        private void SyncSearchText()
        {
            if (tabAll.SelectedTab == tpgFind)
                txtFindR.Text = txtFindF.Text;
            else
                txtFindF.Text = txtFindR.Text;
        }

        private void AddFindMru()
        {
            string find = txtFindF.Text;
            _mruFind.Remove(find);

            _mruFind.Insert(0, find);

            if (_mruFind.Count > _mruMaxCount)
                _mruFind.RemoveAt(_mruFind.Count - 1);

            _bindingSourceFind.ResetBindings(false);
        }

        private void AddReplaceMru()
        {
            string find = txtFindR.Text;
            _mruFind.Remove(find);

            _mruFind.Insert(0, find);

            if (_mruFind.Count > _mruMaxCount)
                _mruFind.RemoveAt(_mruFind.Count - 1);

            string replace = txtReplace.Text;
            if (replace != string.Empty)
            {
                _mruReplace.Remove(replace);

                _mruReplace.Insert(0, replace);

                if (_mruReplace.Count > _mruMaxCount)
                    _mruReplace.RemoveAt(_mruReplace.Count - 1);
            }

            _bindingSourceFind.ResetBindings(false);
            _bindingSourceReplace.ResetBindings(false);
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            //Insert the string value held in the menu items Tag field (\t, \n, etc.)
            txtFindF.SelectedText = e.ClickedItem.Tag.ToString();
        }

        private CharacterRange FindNextF(bool searchUp)
        {
            CharacterRange foundRange;

            if (rdoRegexF.Checked)
            {
                Regex rr = new Regex(txtFindF.Text, GetRegexOptions());

                if (chkSearchSelectionF.Checked)
                {
                    if (_searchRange.cpMin == _searchRange.cpMax)
                        _searchRange = new CharacterRange(_scintilla.Selections[0].Start, _scintilla.Selections[0].End);

                    if (searchUp)
                        foundRange = FindReplace.FindPrevious(rr, chkWrapF.Checked, _searchRange);
                    else
                        foundRange = FindReplace.FindNext(rr, chkWrapF.Checked, _searchRange);
                }
                else
                {
                    _searchRange = new CharacterRange();
                    if (searchUp)
                        foundRange = FindReplace.FindPrevious(rr, chkWrapF.Checked);
                    else
                        foundRange = FindReplace.FindNext(rr, chkWrapF.Checked);
                }
            }
            else
            {
                if (chkSearchSelectionF.Checked)
                {
                    if (_searchRange.cpMin == _searchRange.cpMax)
                        _searchRange = new CharacterRange(_scintilla.Selections[0].Start, _scintilla.Selections[0].End);

                    if (searchUp)
                    {
                        string textToFind = rdoExtendedF.Checked ? FindReplace.Transform(txtFindF.Text) : txtFindF.Text;
                        foundRange = FindReplace.FindPrevious(textToFind, chkWrapF.Checked, GetSearchFlags(), _searchRange);
                    }
                    else
                    {
                        string textToFind = rdoExtendedF.Checked ? FindReplace.Transform(txtFindF.Text) : txtFindF.Text;
                        foundRange = FindReplace.FindNext(textToFind, chkWrapF.Checked, GetSearchFlags(), _searchRange);
                    }
                }
                else
                {
                    _searchRange = new CharacterRange();
                    if (searchUp)
                    {
                        string textToFind = rdoExtendedF.Checked ? FindReplace.Transform(txtFindF.Text) : txtFindF.Text;
                        foundRange = FindReplace.FindPrevious(textToFind, chkWrapF.Checked, GetSearchFlags());
                    }
                    else
                    {
                        string textToFind = rdoExtendedF.Checked ? FindReplace.Transform(txtFindF.Text) : txtFindF.Text;
                        foundRange = FindReplace.FindNext(textToFind, chkWrapF.Checked, GetSearchFlags());
                    }
                }
            }
            return foundRange;
        }

        private CharacterRange FindNextR(bool searchUp, ref Regex rr)
        {
            CharacterRange foundRange;

            if (rdoRegexR.Checked)
            {
                if (rr == null)
                    rr = new Regex(txtFindR.Text, GetRegexOptions());

                if (chkSearchSelectionR.Checked)
                {
                    if (_searchRange.cpMin == _searchRange.cpMax)
                        _searchRange = new CharacterRange(_scintilla.Selections[0].Start, _scintilla.Selections[0].End);

                    if (searchUp)
                        foundRange = FindReplace.FindPrevious(rr, chkWrapR.Checked, _searchRange);
                    else
                        foundRange = FindReplace.FindNext(rr, chkWrapR.Checked, _searchRange);
                }
                else
                {
                    _searchRange = new CharacterRange();
                    if (searchUp)
                        foundRange = FindReplace.FindPrevious(rr, chkWrapR.Checked);
                    else
                        foundRange = FindReplace.FindNext(rr, chkWrapR.Checked);
                }
            }
            else
            {
                if (chkSearchSelectionF.Checked)
                {
                    if (_searchRange.cpMin == _searchRange.cpMax)
                        _searchRange = new CharacterRange(_scintilla.Selections[0].Start, _scintilla.Selections[0].End);

                    if (searchUp)
                    {
                        string textToFind = rdoExtendedR.Checked ? FindReplace.Transform(txtFindR.Text) : txtFindR.Text;
                        foundRange = FindReplace.FindPrevious(textToFind, chkWrapR.Checked, GetSearchFlags(), _searchRange);
                    }
                    else
                    {
                        string textToFind = rdoExtendedR.Checked ? FindReplace.Transform(txtFindR.Text) : txtFindR.Text;
                        foundRange = FindReplace.FindNext(textToFind, chkWrapR.Checked, GetSearchFlags(), _searchRange);
                    }
                }
                else
                {
                    _searchRange = new CharacterRange();
                    if (searchUp)
                    {
                        string textToFind = rdoExtendedR.Checked ? FindReplace.Transform(txtFindR.Text) : txtFindR.Text;
                        foundRange = FindReplace.FindPrevious(textToFind, chkWrapF.Checked, GetSearchFlags());
                    }
                    else
                    {
                        string textToFind = rdoExtendedR.Checked ? FindReplace.Transform(txtFindR.Text) : txtFindR.Text;
                        foundRange = FindReplace.FindNext(textToFind, chkWrapF.Checked, GetSearchFlags());
                    }
                }
            }
            return foundRange;
        }

        private void FindReplaceDialog_Activated(object sender, EventArgs e)
        {
            this.Opacity = 1.0;
        }

        private void FindReplaceDialog_Deactivate(object sender, EventArgs e)
        {
            this.Opacity = 0.6;
        }

        private void mnuRecentFindF_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            //Insert the string value held in the menu items Tag field (\t, \n, etc.)
            if (e.ClickedItem.Text == "Clear History")
            {
                MruFind.Clear();
            }
            else
            {
                txtFindF.Text = e.ClickedItem.Tag.ToString();
            }
        }

        private void mnuRecentFindR_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            //Insert the string value held in the menu items Tag field (\t, \n, etc.)
            if (e.ClickedItem.Text == "Clear History")
            {
                MruFind.Clear();
            }
            else
            {
                txtFindR.Text = e.ClickedItem.Tag.ToString();
            }
        }

        private void mnuRecentReplace_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            //Insert the string value held in the menu items Tag field (\t, \n, etc.)
            if (e.ClickedItem.Text == "Clear History")
            {
                MruReplace.Clear();
            }
            else
            {
                txtReplace.Text = e.ClickedItem.Tag.ToString();
            }
        }

        private void mnuExtendedCharFindR_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            //Insert the string value held in the menu items Tag field (\t, \n, etc.)
            txtFindR.SelectedText = e.ClickedItem.Tag.ToString();
        }

        private void mnuExtendedCharReplace_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            //Insert the string value held in the menu items Tag field (\t, \n, etc.)
            txtReplace.SelectedText = e.ClickedItem.Tag.ToString();
        }

        private CharacterRange ReplaceNext(bool searchUp)
        {
            Regex rr = null;
            CharacterRange selRange = new CharacterRange(_scintilla.Selections[0].Start, _scintilla.Selections[0].End);

            //	We only do the actual replacement if the current selection exactly
            //	matches the find.
            if (selRange.cpMax - selRange.cpMin > 0)
            {
                if (rdoRegexR.Checked)
                {
                    rr = new Regex(txtFindR.Text, GetRegexOptions());
                    string selRangeText = Scintilla.GetTextRange(selRange.cpMin, selRange.cpMax - selRange.cpMin + 1);

                    if (selRange.Equals(FindReplace.Find(selRange, rr, false)))
                    {
                        //	If searching up we do the replacement using the range object.
                        //	Otherwise we use the selection object. The reason being if
                        //	we use the range the caret is positioned before the replaced
                        //	text. Conversely if we use the selection object the caret will
                        //	be positioned after the replaced text. This is very important
                        //	becuase we don't want the new text to be potentially matched
                        //	in the next search.
                        if (searchUp)
                        {
                            _scintilla.SelectionStart = selRange.cpMin;
                            _scintilla.SelectionEnd = selRange.cpMax;
                            _scintilla.ReplaceSelection(rr.Replace(selRangeText, txtReplace.Text));
                            _scintilla.GotoPosition(selRange.cpMin);
                        }
                        else
                            Scintilla.ReplaceSelection(rr.Replace(selRangeText, txtReplace.Text));
                    }
                }
                else
                {
                    string textToFind = rdoExtendedR.Checked ? FindReplace.Transform(txtFindR.Text) : txtFindR.Text;
                    if (selRange.Equals(FindReplace.Find(selRange, textToFind, false)))
                    {
                        //	If searching up we do the replacement using the range object.
                        //	Otherwise we use the selection object. The reason being if
                        //	we use the range the caret is positioned before the replaced
                        //	text. Conversely if we use the selection object the caret will
                        //	be positioned after the replaced text. This is very important
                        //	becuase we don't want the new text to be potentially matched
                        //	in the next search.
                        if (searchUp)
                        {
                            string textToReplace = rdoExtendedR.Checked ? FindReplace.Transform(txtReplace.Text) : txtReplace.Text;
                            _scintilla.SelectionStart = selRange.cpMin;
                            _scintilla.SelectionEnd = selRange.cpMax;
                            _scintilla.ReplaceSelection(textToReplace);

                            _scintilla.GotoPosition(selRange.cpMin);
                        }
                        else
                        {
                            string textToReplace = rdoExtendedR.Checked ? FindReplace.Transform(txtReplace.Text) : txtReplace.Text;
                            Scintilla.ReplaceSelection(textToReplace);
                        }
                    }
                }
            }
            return FindNextR(searchUp, ref rr);
        }

        #endregion Methods
    }
}