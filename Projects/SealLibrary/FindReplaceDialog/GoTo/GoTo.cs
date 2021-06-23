#region Using Directives

using ScintillaNET;
using System;
using System.Windows.Forms;

#endregion Using Directives

namespace ScintillaNET_FindReplaceDialog
{
	public class GoTo
	{
		private Scintilla _scintilla;
		private GoToDialog _window;

		#region Methods

		public void Line(int number)
		{
			_scintilla.Lines[number].Goto();
		}

		public void Position(int pos)
		{
			_scintilla.GotoPosition(pos);
		}

		public void ShowGoToDialog()
		{
			//GoToDialog gd = new GoToDialog();
			GoToDialog gd = _window;

			gd.CurrentLineNumber = _scintilla.CurrentLine;
			gd.MaximumLineNumber = _scintilla.Lines.Count;
			gd.Scintilla = _scintilla;

			if (!_window.Visible)
				_window.Show(_scintilla.FindForm());

			//_window.ShowDialog(_scintilla.FindForm());
			//_window.Show(_scintilla.FindForm());

			//if (gd.ShowDialog() == DialogResult.OK)
			//Line(gd.GotoLineNumber);

			//gd.ShowDialog();
			//gd.Show();

			_scintilla.Focus();
		}

		#endregion Methods

		#region Constructors

		protected virtual GoToDialog CreateWindowInstance()
		{
			return new GoToDialog();
		}

		public GoTo(Scintilla scintilla)
		{
			_scintilla = scintilla;
			_window = CreateWindowInstance();
			_window.Scintilla = scintilla;
		}

		#endregion Constructors
	}
}