using DiffPlex.WindowsForms.Controls;
using ScintillaNET;
using Seal.Model;
using System;
using System.Windows.Forms;

namespace DiffPlex.WindowsForms
{
    public partial class DifferenceForm : Form
    {
        private DiffViewer _view = null;
        private Scintilla _textBox = null;
        string _reference;

        /// <summary>
        /// Initializes a new instance of the Form2 class.
        /// </summary>
        public DifferenceForm(Scintilla textBox, string reference)
        {
            _textBox = textBox;
            _reference = reference;
            InitializeComponent();
            ShowIcon = true;
            Icon = Repository.ProductIcon;
        }

        public void Init()
        {
            var previousView = _view;
            if (previousView != null) MainLayoutPanel.Controls.Remove(previousView);

            _view = new DiffViewer
            {
                Margin = new Padding(0),
                Dock = DockStyle.Fill,
                OldText = _reference,
                NewText = _textBox.Text,
                OldTextHeader= "Reference script",
                NewTextHeader= "Current script"
            };
            MainLayoutPanel.Controls.Add(_view, 0, 0);

            if (previousView != null)
            {
                if (previousView.IsInlineViewMode) _view.ShowInline();
                _view.CollapseUnchangedSections(previousView.LinesContext);
                _view.IgnoreUnchanged = previousView.IgnoreUnchanged;
            }
            _view.IgnoreCase = ignoreCase.Checked;
            _view.IgnoreWhiteSpace = ignoreWhiteSpace.Checked;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Init();
        }

        private void SwitchButton_Click(object sender, EventArgs e)
        {
            if (_view.IsInlineViewMode)
            {
                _view.ShowSideBySide();
                return;
            }

            _view.ShowInline();
        }

        private void FutherActionsButton_Click(object sender, EventArgs e)
        {
            _view.OpenViewModeContextMenu();
        }

        private void reload_Click(object sender, EventArgs e)
        {
            Init();
        }

        private void close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ignoreWhiteSpace_CheckedChanged(object sender, EventArgs e)
        {
            _view.IgnoreWhiteSpace = ignoreWhiteSpace.Checked;
        }

        private void ignoreCase_CheckedChanged(object sender, EventArgs e)
        {
            _view.IgnoreCase = ignoreCase.Checked;
        }
    }
}
