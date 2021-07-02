namespace ScintillaNET_FindReplaceDialog
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Text.RegularExpressions;
	using ScintillaNET;
	using System.Drawing;
	using System.Windows.Forms;

	[TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
	public class FindReplace : Component
	{
		#region Fields

		private SearchFlags _flags;
		private IncrementalSearcher _incrementalSearcher;
		private Indicator _indicator;
		private int _lastReplaceAllOffset = 0;
		private CharacterRange _lastReplaceAllRangeToSearch;
		private string _lastReplaceAllReplaceString = "";
		private int _lastReplaceCount = 0;
		private Marker _marker;
		private Scintilla _scintilla;
		private FindReplaceDialog _window;

		#endregion Fields

		#region Constructors

		/// <summary>
		/// Creates an instance of the FindReplace class.
		/// </summary>
		/// <param name="scintilla">The Scintilla class to which the FindReplace class is attached.</param>
		public FindReplace(Scintilla scintilla)
		{
			_window = CreateWindowInstance();

			if (scintilla != null)
				Scintilla = scintilla;
		}

		/// <summary>
		/// Creates an instance of the FindReplace class.
		/// </summary>
		public FindReplace() : this(null)
		{
		}

		#endregion Constructors

		public event FindAllResultsEventHandler FindAllResults;

		public event ReplaceAllResultsEventHandler ReplaceAllResults;

		public delegate void FindAllResultsEventHandler(object sender, FindResultsEventArgs FindAllResults);

		public delegate void ReplaceAllResultsEventHandler(object sender, ReplaceResultsEventArgs FindAllResults);

		#region Properties

		public Scintilla Scintilla
		{
			get
			{
				return _scintilla;
			}
			set
			{
				_scintilla = value;
				_marker = _scintilla.Markers[10];
				_marker.Symbol = MarkerSymbol.Circle;
				_marker.SetForeColor(Color.Black);
				_marker.SetBackColor(Color.Blue);
				_indicator = _scintilla.Indicators[16];
				_indicator.ForeColor = Color.Red;
				//_indicator.ForeColor = Color.LawnGreen; //Smart highlight
				_indicator.Alpha = 100;
				_indicator.Style = IndicatorStyle.RoundBox;
				_indicator.Under = true;

				_window.Scintilla = _scintilla;
				_window.FindReplace = this;
				_window.KeyPressed += _window_KeyPressed;

				_incrementalSearcher = CreateIncrementalSearcherInstance();
				_incrementalSearcher.Scintilla = _scintilla;
				_incrementalSearcher.FindReplace = this;
				_incrementalSearcher.Visible = false;
				_scintilla.Controls.Add(_incrementalSearcher);
			}
		}

		/// <summary>
		/// Triggered when a key is pressed on the Find and Replace Dialog.
		/// </summary>
		public event KeyPressedHandler KeyPressed;

		/// <summary>
		/// Handler for the key press on a Find and Replace Dialog.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The key info of the key(s) pressed.</param>
		public delegate void KeyPressedHandler(object sender, KeyEventArgs e);

		private void _window_KeyPressed(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (KeyPressed != null)
				KeyPressed(this, e);
		}

		//[Editor(typeof(ScintillaNET.Design.FlagEnumUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
		//public SearchFlags Flags
		//{
		//	get
		//	{
		//		return _flags;
		//	}
		//	set
		//	{
		//		_flags = value;
		//	}
		//}
		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IncrementalSearcher IncrementalSearcher
		{
			get
			{
				return _incrementalSearcher;
			}
			set
			{
				_incrementalSearcher = value;
			}
		}

		public Indicator Indicator
		{
			get
			{
				return _indicator;
			}
			set
			{
				_indicator = value;
			}
		}

		public Marker Marker
		{
			get
			{
				return _marker;
			}
			set
			{
				_marker = value;
			}
		}

		[Browsable(false),
		DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FindReplaceDialog Window
		{
			get
			{
				return _window;
			}
			set
			{
				_window = value;
			}
		}

		[Browsable(false)]
		public bool _lastReplaceHighlight
		{
			get; set;
		}

		[Browsable(false)]
		public int _lastReplaceLastLine
		{
			get; set;
		}

		[Browsable(false)]
		public bool _lastReplaceMark
		{
			get; set;
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Clears highlights from the entire document
		/// </summary>
		public void ClearAllHighlights()
		{
			int currentIndicator = _scintilla.IndicatorCurrent;

			_scintilla.IndicatorCurrent = Indicator.Index;
			_scintilla.IndicatorClearRange(0, _scintilla.TextLength);

			_scintilla.IndicatorCurrent = currentIndicator;
		}

		/// <summary>
		/// Highlight ranges in the document.
		/// </summary>
		/// <param name="Ranges">List of ranges to which highlighting should be applied.</param>
		public void HighlightAll(List<CharacterRange> Ranges)
		{
			_scintilla.IndicatorCurrent = Indicator.Index;

			foreach (var r in Ranges)
			{
				_scintilla.IndicatorFillRange(r.cpMin, r.cpMax - r.cpMin);
			}
		}

		public CharacterRange Find(int startPos, int endPos, Regex findExpression)
		{
			return Find(new CharacterRange(startPos, endPos), findExpression, false);
		}

		public CharacterRange Find(int startPos, int endPos, Regex findExpression, bool searchUp)
		{
			return Find(new CharacterRange(startPos, endPos), findExpression, searchUp);
		}

		public CharacterRange Find(int startPos, int endPos, string searchString, SearchFlags flags)
		{
			if (string.IsNullOrEmpty(searchString))
				return new CharacterRange();

			_scintilla.TargetStart = startPos;
			_scintilla.TargetEnd = endPos;
			_scintilla.SearchFlags = flags;
			int pos = _scintilla.SearchInTarget(searchString);
			if (pos == -1)
				return new CharacterRange();
			return new CharacterRange(_scintilla.TargetStart, _scintilla.TargetEnd);
		}

		public CharacterRange Find(CharacterRange r, Regex findExpression)
		{
			return Find(r, findExpression, false);
		}

		public CharacterRange Find(CharacterRange r, Regex findExpression, bool searchUp)
		{
			// Single line and Multi Line in RegExp doesn't really effect
			// whether or not a match will include newline characters. This
			// means we can't do a line by line search. We have to search
			// the entire range because it could potentially match the
			// entire range.

			string text = _scintilla.GetTextRange(r.cpMin, r.cpMax - r.cpMin + 1);

			Match m = findExpression.Match(text);

			if (!m.Success)
				return new CharacterRange();

			if (searchUp)
			{
				// Since we can't search backwards with RegExp we
				// have to search the entire string and return the
				// last match. Not the most efficient way of doing
				// things but it works.
				CharacterRange range = new CharacterRange();
				while (m.Success)
				{
					//TODO - check that removing the byte count does not upset anything
					//int start = r.cpMin + _scintilla.Encoding.GetByteCount(text.Substring(0, m.Index));
					//int end = _scintilla.Encoding.GetByteCount(text.Substring(m.Index, m.Length));
					int start = r.cpMin + text.Substring(0, m.Index).Length;
					int end = text.Substring(m.Index, m.Length).Length;

					range = new CharacterRange(start, start + end);
					m = m.NextMatch();
				}

				return range;
			}
			else
			{
				//TODO - check that removing the byte count does not upset anything
				//int start = r.cpMin + _scintilla.Encoding.GetByteCount(text.Substring(0, m.Index));
				//int end = _scintilla.Encoding.GetByteCount(text.Substring(m.Index, m.Length));
				int start = r.cpMin + text.Substring(0, m.Index).Length;
				int end = text.Substring(m.Index, m.Length).Length;

				return new CharacterRange(start, start + end);
			}
		}

		public CharacterRange Find(CharacterRange rangeToSearch, string searchString)
		{
			return Find(rangeToSearch.cpMin, rangeToSearch.cpMax, searchString, _flags);
		}

		public CharacterRange Find(CharacterRange rangeToSearch, string searchString, bool searchUp)
		{
			if (searchUp)
				return Find(rangeToSearch.cpMax, rangeToSearch.cpMin, searchString, _flags);
			else
				return Find(rangeToSearch.cpMin, rangeToSearch.cpMax, searchString, _flags);
		}

		public CharacterRange Find(CharacterRange rangeToSearch, string searchString, SearchFlags searchflags)
		{
			return Find(rangeToSearch.cpMin, rangeToSearch.cpMax, searchString, searchflags);
		}

		public CharacterRange Find(CharacterRange rangeToSearch, string searchString, SearchFlags searchflags, bool searchUp)
		{
			if (searchUp)
				return Find(rangeToSearch.cpMax, rangeToSearch.cpMin, searchString, searchflags);
			else
				return Find(rangeToSearch.cpMin, rangeToSearch.cpMax, searchString, searchflags);
		}

		public CharacterRange Find(Regex findExpression)
		{
			return Find(new CharacterRange(0, _scintilla.TextLength), findExpression, false);
		}

		public CharacterRange Find(Regex findExpression, bool searchUp)
		{
			return Find(new CharacterRange(0, _scintilla.TextLength), findExpression, searchUp);
		}

		public CharacterRange Find(string searchString)
		{
			return Find(0, _scintilla.TextLength, searchString, _flags);
		}

		public CharacterRange Find(string searchString, bool searchUp)
		{
			if (searchUp)
				return Find(_scintilla.TextLength, 0, searchString, _flags);
			else
				return Find(0, _scintilla.TextLength, searchString, _flags);
		}

		public CharacterRange Find(string searchString, SearchFlags searchflags)
		{
			return Find(0, _scintilla.TextLength, searchString, searchflags);
		}

		public CharacterRange Find(string searchString, SearchFlags searchflags, bool searchUp)
		{
			if (searchUp)
				return Find(_scintilla.TextLength, 0, searchString, searchflags);
			else
				return Find(0, _scintilla.TextLength, searchString, searchflags);
		}

		public List<CharacterRange> FindAll(int startPos, int endPos, Regex findExpression, bool Mark, bool Highlight)
		{
			return FindAll(new CharacterRange(startPos, endPos), findExpression, Mark, Highlight);
		}

		public List<CharacterRange> FindAll(int startPos, int endPos, string searchString, SearchFlags flags, bool Mark, bool Highlight)
		{
			List<CharacterRange> Results = new List<CharacterRange>();

			_scintilla.IndicatorCurrent = Indicator.Index;

			int findCount = 0;
			int lastLine = -1;
			while (true)
			{
				CharacterRange r = Find(startPos, endPos, searchString, flags);
				if (r.cpMin == r.cpMax)
				{
					break;
				}
				else
				{
					Results.Add(r);
					findCount++;
					if (Mark)
					{
						//	We can of course have multiple instances of a find on a single
						//	line. We don't want to mark this line more than once.
						Line line = new Line(_scintilla, _scintilla.LineFromPosition(r.cpMin));
						if (line.Position > lastLine)
							line.MarkerAdd(_marker.Index);
						lastLine = line.Position;
					}
					if (Highlight)
					{
						_scintilla.IndicatorFillRange(r.cpMin, r.cpMax - r.cpMin);
					}
					startPos = r.cpMax;
				}
			}
			//return findCount;

			if (FindAllResults != null)
				FindAllResults(this, new FindResultsEventArgs(this, Results));

			return Results;
		}

		public List<CharacterRange> FindAll(CharacterRange rangeToSearch, Regex findExpression, bool Mark, bool Highlight)
		{
			List<CharacterRange> Results = new List<CharacterRange>();

			_scintilla.IndicatorCurrent = Indicator.Index;

			int findCount = 0;
			int lastLine = -1;

			while (true)
			{
				CharacterRange r = Find(rangeToSearch, findExpression);
				if (r.cpMin == r.cpMax)
				{
					break;
				}
				else
				{
					Results.Add(r);
					findCount++;
					if (Mark)
					{
						//	We can of course have multiple instances of a find on a single
						//	line. We don't want to mark this line more than once.
						Line line = new Line(_scintilla, _scintilla.LineFromPosition(r.cpMin));
						if (line.Position > lastLine)
							line.MarkerAdd(_marker.Index);
						lastLine = line.Position;
					}
					if (Highlight)
					{
						_scintilla.IndicatorFillRange(r.cpMin, r.cpMax - r.cpMin);
					}
					rangeToSearch = new CharacterRange(r.cpMax, rangeToSearch.cpMax);
				}
			}
			//return findCount;
			if (FindAllResults != null)
				FindAllResults(this, new FindResultsEventArgs(this, Results));

			return Results;
		}

		public List<CharacterRange> FindAll(CharacterRange rangeToSearch, string searchString, SearchFlags flags, bool Mark, bool Highlight)
		{
			return FindAll(rangeToSearch.cpMin, rangeToSearch.cpMax, searchString, _flags, Mark, Highlight);
		}

		public List<CharacterRange> FindAll(Regex findExpression, bool Mark, bool Highlight)
		{
			return FindAll(0, _scintilla.TextLength, findExpression, Mark, Highlight);
		}

		public List<CharacterRange> FindAll(string searchString, bool Mark, bool Highlight)
		{
			return FindAll(searchString, _flags, Mark, Highlight);
		}

		public List<CharacterRange> FindAll(string searchString)
		{
			return FindAll(searchString, _flags, false, false);
		}

		public List<CharacterRange> FindAll(string searchString, SearchFlags flags, bool Mark, bool Highlight)
		{
			return FindAll(0, _scintilla.TextLength, searchString, flags, Mark, Highlight);
		}

		public CharacterRange FindNext(Regex findExpression)
		{
			return FindNext(findExpression, false);
		}

		public CharacterRange FindNext(Regex findExpression, bool wrap)
		{
			CharacterRange r = Find(_scintilla.CurrentPosition, _scintilla.TextLength, findExpression);
			if (r.cpMin != r.cpMax)
				return r;
			else if (wrap)
				return Find(0, _scintilla.CurrentPosition, findExpression);
			else
				return new CharacterRange();
		}

		public CharacterRange FindNext(Regex findExpression, bool wrap, CharacterRange searchRange)
		{
			int caret = _scintilla.CurrentPosition;
			if (!(caret >= searchRange.cpMin && caret <= searchRange.cpMax))
				return Find(searchRange.cpMin, searchRange.cpMax, findExpression, false);

			CharacterRange r = Find(caret, searchRange.cpMax, findExpression);
			if (r.cpMin != r.cpMax)
				return r;
			else if (wrap)
				return Find(searchRange.cpMin, caret, findExpression);
			else
				return new CharacterRange();
		}

		public CharacterRange FindNext(string searchString)
		{
			return FindNext(searchString, true, _flags);
		}

		public CharacterRange FindNext(string searchString, bool wrap)
		{
			return FindNext(searchString, wrap, _flags);
		}

		public CharacterRange FindNext(string searchString, bool wrap, SearchFlags flags)
		{
			CharacterRange r = Find(_scintilla.CurrentPosition, _scintilla.TextLength, searchString, flags);
			if (r.cpMin != r.cpMax)
				return r;
			else if (wrap)
				return Find(0, _scintilla.CurrentPosition, searchString, flags);
			else
				return new CharacterRange();
		}

		public CharacterRange FindNext(string searchString, bool wrap, SearchFlags flags, CharacterRange searchRange)
		{
			int caret = _scintilla.CurrentPosition;
			if (!(caret >= searchRange.cpMin && caret <= searchRange.cpMax))
				return Find(searchRange.cpMin, searchRange.cpMax, searchString, flags);

			CharacterRange r = Find(caret, searchRange.cpMax, searchString, flags);
			if (r.cpMin != r.cpMax)
				return r;
			else if (wrap)
				return Find(searchRange.cpMin, caret, searchString, flags);
			else
				return new CharacterRange();
		}

		public CharacterRange FindNext(string searchString, SearchFlags flags)
		{
			return FindNext(searchString, true, flags);
		}

		public CharacterRange FindPrevious(Regex findExpression)
		{
			return FindPrevious(findExpression, false);
		}

		public CharacterRange FindPrevious(Regex findExpression, bool wrap)
		{
			CharacterRange r = Find(0, _scintilla.AnchorPosition, findExpression, true);
			if (r.cpMin != r.cpMax)
				return r;
			else if (wrap)
				return Find(_scintilla.CurrentPosition, _scintilla.TextLength, findExpression, true);
			else
				return new CharacterRange();
		}

		public CharacterRange FindPrevious(Regex findExpression, bool wrap, CharacterRange searchRange)
		{
			int caret = _scintilla.CurrentPosition;
			if (!(caret >= searchRange.cpMin && caret <= searchRange.cpMax))
				return Find(searchRange.cpMin, searchRange.cpMax, findExpression, true);

			int anchor = _scintilla.AnchorPosition;
			if (!(anchor >= searchRange.cpMin && anchor <= searchRange.cpMax))
				anchor = caret;

			CharacterRange r = Find(searchRange.cpMin, anchor, findExpression, true);
			if (r.cpMin != r.cpMax)
				return r;
			else if (wrap)
				return Find(anchor, searchRange.cpMax, findExpression, true);
			else
				return new CharacterRange();
		}

		public CharacterRange FindPrevious(string searchString)
		{
			return FindPrevious(searchString, true, _flags);
		}

		public CharacterRange FindPrevious(string searchString, bool wrap)
		{
			return FindPrevious(searchString, wrap, _flags);
		}

		public CharacterRange FindPrevious(string searchString, bool wrap, SearchFlags flags)
		{
			CharacterRange r = Find(_scintilla.AnchorPosition, 0, searchString, flags);
			if (r.cpMin != r.cpMax)
				return r;
			else if (wrap)
				return Find(_scintilla.TextLength, _scintilla.CurrentPosition, searchString, flags);
			else
				return new CharacterRange();
		}

		public CharacterRange FindPrevious(string searchString, bool wrap, SearchFlags flags, CharacterRange searchRange)
		{
			int caret = _scintilla.CurrentPosition;
			if (!(caret >= searchRange.cpMin && caret <= searchRange.cpMax))
				return Find(searchRange.cpMax, searchRange.cpMin, searchString, flags);

			int anchor = _scintilla.AnchorPosition;
			if (!(anchor >= searchRange.cpMin && anchor <= searchRange.cpMax))
				anchor = caret;

			CharacterRange r = Find(anchor, searchRange.cpMin, searchString, flags);
			if (r.cpMin != r.cpMax)
				return r;
			else if (wrap)
				return Find(searchRange.cpMax, anchor, searchString, flags);
			else
				return new CharacterRange();
		}

		public CharacterRange FindPrevious(string searchString, SearchFlags flags)
		{
			return FindPrevious(searchString, true, flags);
		}

		public int ReplaceAll(int startPos, int endPos, Regex findExpression, string replaceString, bool Mark, bool Highlight)
		{
			return ReplaceAll(new CharacterRange(startPos, endPos), findExpression, replaceString, Mark, Highlight);
		}

		public int ReplaceAll(int startPos, int endPos, string searchString, string replaceString, SearchFlags flags, bool Mark, bool Highlight)
		{
			List<CharacterRange> Results = new List<CharacterRange>();

			_scintilla.IndicatorCurrent = Indicator.Index;

			int findCount = 0;
			int lastLine = -1;

			_scintilla.BeginUndoAction();

			int diff = replaceString.Length - searchString.Length;
			while (true)
			{
				CharacterRange r = Find(startPos, endPos, searchString, flags);
				if (r.cpMin == r.cpMax)
				{
					break;
				}
				else
				{
					_scintilla.SelectionStart = r.cpMin;
					_scintilla.SelectionEnd = r.cpMax;
					_scintilla.ReplaceSelection(replaceString);
					r.cpMax = startPos = r.cpMin + replaceString.Length;
					endPos += diff;

					Results.Add(r);
					findCount++;

					if (Mark)
					{
						//	We can of course have multiple instances of a find on a single
						//	line. We don't want to mark this line more than once.
						Line line = new Line(_scintilla, _scintilla.LineFromPosition(r.cpMin));
						if (line.Position > lastLine)
							line.MarkerAdd(_marker.Index);
						lastLine = line.Position;
					}
					if (Highlight)
					{
						_scintilla.IndicatorFillRange(r.cpMin, r.cpMax - r.cpMin);
					}
				}
			}

			_scintilla.EndUndoAction();

			if (ReplaceAllResults != null)
				ReplaceAllResults(this, new ReplaceResultsEventArgs(this, Results));

			return findCount;
		}

		public int ReplaceAll(CharacterRange rangeToSearch, Regex findExpression, string replaceString, bool Mark, bool Highlight)
		{
			_scintilla.IndicatorCurrent = Indicator.Index;
			_scintilla.BeginUndoAction();

			//	I tried using an anonymous delegate for this but it didn't work too well.
			//	It's too bad because it was a lot cleaner than using member variables as
			//	psuedo globals.
			_lastReplaceAllReplaceString = replaceString;
			_lastReplaceAllRangeToSearch = rangeToSearch;
			_lastReplaceAllOffset = 0;
			_lastReplaceCount = 0;
			_lastReplaceMark = Mark;
			_lastReplaceHighlight = Highlight;

			string text = _scintilla.GetTextRange(rangeToSearch.cpMin, rangeToSearch.cpMax - rangeToSearch.cpMin + 1);
			findExpression.Replace(text, new MatchEvaluator(ReplaceAllEvaluator));

			_scintilla.EndUndoAction();

			//	No use having these values hanging around wasting memory :)
			_lastReplaceAllReplaceString = null;
			_lastReplaceAllRangeToSearch = new CharacterRange();

			return _lastReplaceCount;
		}

		public int ReplaceAll(CharacterRange rangeToSearch, string searchString, string replaceString, SearchFlags flags, bool Mark, bool Highlight)
		{
			return ReplaceAll(rangeToSearch.cpMin, rangeToSearch.cpMax, searchString, replaceString, _flags, Mark, Highlight);
		}

		public int ReplaceAll(Regex findExpression, string replaceString, bool Mark, bool Highlight)
		{
			return ReplaceAll(0, _scintilla.TextLength, findExpression, replaceString, Mark, Highlight);
		}

		public int ReplaceAll(string searchString, string replaceString, SearchFlags flags, bool Mark, bool Highlight)
		{
			return ReplaceAll(0, _scintilla.TextLength, searchString, replaceString, flags, Mark, Highlight);
		}

		public CharacterRange ReplaceNext(string searchString, string replaceString)
		{
			return ReplaceNext(searchString, replaceString, true, _flags);
		}

		public CharacterRange ReplaceNext(string searchString, string replaceString, bool wrap)
		{
			return ReplaceNext(searchString, replaceString, wrap, _flags);
		}

		public CharacterRange ReplaceNext(string searchString, string replaceString, bool wrap, SearchFlags flags)
		{
			CharacterRange r = FindNext(searchString, wrap, flags);

			if (r.cpMin != r.cpMax)
			{
				_scintilla.SelectionStart = r.cpMin;
				_scintilla.SelectionEnd = r.cpMax;
				_scintilla.ReplaceSelection(replaceString);
				r.cpMax = r.cpMin + replaceString.Length;
			}

			return r;
		}

		public CharacterRange ReplaceNext(string searchString, string replaceString, SearchFlags flags)
		{
			return ReplaceNext(searchString, replaceString, true, flags);
		}

		public CharacterRange ReplacePrevious(string searchString, string replaceString)
		{
			return ReplacePrevious(searchString, replaceString, true, _flags);
		}

		public CharacterRange ReplacePrevious(string searchString, string replaceString, bool wrap)
		{
			return ReplacePrevious(searchString, replaceString, wrap, _flags);
		}

		public CharacterRange ReplacePrevious(string searchString, string replaceString, bool wrap, SearchFlags flags)
		{
			CharacterRange r = FindPrevious(searchString, wrap, flags);

			if (r.cpMin != r.cpMax)
			{
				_scintilla.SelectionStart = r.cpMin;
				_scintilla.SelectionEnd = r.cpMax;
				_scintilla.ReplaceSelection(replaceString);
				r.cpMax = r.cpMin + replaceString.Length;
			}

			return r;
		}

		public CharacterRange ReplacePrevious(string searchString, string replaceString, SearchFlags flags)
		{
			return ReplacePrevious(searchString, replaceString, true, flags);
		}

		//private void ResetFlags()
		//{
		//	_flags = SearchFlags.Empty;
		//}
		//private void ResetIndicator()
		//{
		//	_indicator. Reset();
		//}
		//private void ResetMarker()
		//{
		//	_marker.Reset();
		//	_marker.Number = 10;
		//}
		//internal bool ShouldSerialize()
		//{
		//	return ShouldSerializeFlags() ||
		//		ShouldSerializeIndicator() ||
		//		ShouldSerializeMarker();
		//}
		//private bool ShouldSerializeFlags()
		//{
		//	return _flags != SearchFlags.Empty;
		//}
		//private bool ShouldSerializeIndicator()
		//{
		//	return _indicator.Index != 16 || _indicator.Color != Color.Purple || _indicator.DrawMode != IndicatorDrawMode.Overlay;
		//}
		//private bool ShouldSerializeMarker()
		//{
		//	return _marker.Number != 10 || _marker.ForeColor != Color.White || _marker.BackColor != Color.Black || _marker.Symbol != MarkerSymbol.Arrows;
		//}
		public void ShowFind()
		{
			if (!_window.Visible)
				_window.Show(_scintilla.FindForm());

			_window.tabAll.SelectedTab = _window.tabAll.TabPages["tpgFind"];

			if (_scintilla.LineFromPosition(_scintilla.Selections[0].Start) != _scintilla.LineFromPosition(_scintilla.Selections[0].End)) //selRange.IsMultiLine)
			{
				_window.chkSearchSelectionF.Checked = true;
			}
			else if (_scintilla.Selections[0].End > _scintilla.Selections[0].Start)
			{
				_window.txtFindF.Text = _scintilla.SelectedText;
			}

			_window.txtFindF.Select();
			_window.txtFindF.SelectAll();
		}

		public void ShowIncrementalSearch()
		{
			_incrementalSearcher.Show();
		}

		public void ShowReplace()
		{
			if (!_window.Visible)
				_window.Show(_scintilla.FindForm());

			_window.tabAll.SelectedTab = _window.tabAll.TabPages["tpgReplace"];

			if (_scintilla.LineFromPosition(_scintilla.Selections[0].Start) != _scintilla.LineFromPosition(_scintilla.Selections[0].End)) //selRange.IsMultiLine)
			{
				_window.chkSearchSelectionR.Checked = true;
			}
			else if (_scintilla.Selections[0].End > _scintilla.Selections[0].Start)
			{
				//_window.cboFindR.Text = selRange.Text;
				_window.txtFindR.Text = _scintilla.SelectedText;
			}

			_window.txtFindR.Select();
			_window.txtFindR.SelectAll();
		}

		public string Transform(string data)
		{
			string result = data;
			char nullChar = (char)0;
			char cr = (char)13;
			char lf = (char)10;
			char tab = (char)9;

			result = result.Replace("\\r\\n", Environment.NewLine);
			result = result.Replace("\\r", cr.ToString());
			result = result.Replace("\\n", lf.ToString());
			result = result.Replace("\\t", tab.ToString());
			result = result.Replace("\\0", nullChar.ToString());

			return result;
		}

		/// <summary>
		/// Creates and returns a new <see cref="IncrementalSearcher" /> object.
		/// </summary>
		/// <returns>A new <see cref="IncrementalSearcher" /> object.</returns>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual IncrementalSearcher CreateIncrementalSearcherInstance()
		{
			return new IncrementalSearcher();
		}

		/// <summary>
		/// Creates and returns a new <see cref="FindReplaceDialog" /> object.
		/// </summary>
		/// <returns>A new <see cref="FindReplaceDialog" /> object.</returns>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual FindReplaceDialog CreateWindowInstance()
		{
			return new FindReplaceDialog();
		}

		private string ReplaceAllEvaluator(Match m)
		{
			//	So this method is called for every match

			//	We make a replacement in the range based upon
			//	the match range.
			string replacement = m.Result(_lastReplaceAllReplaceString);
			int start = _lastReplaceAllRangeToSearch.cpMin + m.Index + _lastReplaceAllOffset;
			int end = start + m.Length;

			CharacterRange r = new CharacterRange(start, end);
			_lastReplaceCount++;
			_scintilla.SelectionStart = r.cpMin;
			_scintilla.SelectionEnd = r.cpMax;
			_scintilla.ReplaceSelection(replacement);

			if (_lastReplaceMark)
			{
				//	We can of course have multiple instances of a find on a single
				//	line. We don't want to mark this line more than once.
				// TODO - Is determining the current line any more efficient that just setting the duplicate marker? LineFromPosition appears to have more code that MarkerAdd!
				Line line = new Line(_scintilla, _scintilla.LineFromPosition(r.cpMin));
				if (line.Position > _lastReplaceLastLine)
					line.MarkerAdd(_marker.Index);
				_lastReplaceLastLine = line.Position;
			}
			if (_lastReplaceHighlight)
			{
				_scintilla.IndicatorFillRange(r.cpMin, r.cpMax - r.cpMin);
			}

			//	But because we've modified the document, the RegEx
			//	match ranges are going to be different from the
			//	document ranges. We need to compensate
			_lastReplaceAllOffset += replacement.Length - m.Value.Length;

			return replacement;
		}

		#endregion Methods
	}

	public class FindResultsEventArgs : EventArgs
	{
		public FindResultsEventArgs(FindReplace FindReplace, List<CharacterRange> FindAllResults)
		{
			this.FindReplace = FindReplace;
			this.FindAllResults = FindAllResults;
		}

		public FindReplace FindReplace { get; set; }
		public List<CharacterRange> FindAllResults { get; set; }
	}

	public class ReplaceResultsEventArgs : EventArgs
	{
		public ReplaceResultsEventArgs(FindReplace FindReplace, List<CharacterRange> ReplaceAllResults)
		{
			this.FindReplace = FindReplace;
			this.ReplaceAllResults = ReplaceAllResults;
		}

		public FindReplace FindReplace { get; set; }
		public List<CharacterRange> ReplaceAllResults { get; set; }
	}
}