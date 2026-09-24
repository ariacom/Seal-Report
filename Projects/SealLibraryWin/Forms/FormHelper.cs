using RazorEngine.Templating;
using ScintillaNET;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Seal.Forms
{
    public class FormHelper
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        /// <summary>
        /// Re-enable a top-level window that may have been disabled by an active modal dialog.
        /// Form.ShowDialog() disables every other window in the application; this lets a window (e.g. the
        /// Report Viewer) stay clickable while a modal script editor is open and runs the report.
        /// </summary>
        public static void EnsureWindowEnabled(Form form)
        {
            if (form != null && form.IsHandleCreated) EnableWindow(form.Handle, true);
        }

        /// <summary>
        /// Windows Forms Objects and Helpers
        /// </summary>
        internal class NamespaceDoc
        {
        }

        static void setIndicatorAppearance(Scintilla textBox, int NUM)
        {
            textBox.Indicators[NUM].Style = IndicatorStyle.StraightBox;
            textBox.Indicators[NUM].Under = true;
            textBox.Indicators[NUM].ForeColor = Color.Red;
            textBox.Indicators[NUM].OutlineAlpha = 120;
            textBox.Indicators[NUM].Alpha = 120;
        }

        static void setIndicatorAppearance2(Scintilla textBox, int NUM2)
        {
            textBox.Indicators[NUM2].Style = IndicatorStyle.StraightBox;
            textBox.Indicators[NUM2].Under = true;
            textBox.Indicators[NUM2].ForeColor = Color.Orange;
            textBox.Indicators[NUM2].OutlineAlpha = 120;
            textBox.Indicators[NUM2].Alpha = 120;
        }

        static void setRazorError(Scintilla textBox, Dictionary<int, string> compilationErrors, Line line, int column, string error)
        {
            line.Goto();
            textBox.CurrentPosition += column - 1;
            int end = textBox.CurrentPosition;
            while (++end < textBox.Text.Length)
            {
                if (" [](){}.,;\"\':-+*&".IndexOf(textBox.Text[end]) >= 0) break;
            }
            textBox.SelectionStart = textBox.CurrentPosition;
            textBox.SelectionEnd = textBox.CurrentPosition;
            textBox.Focus();

            textBox.IndicatorFillRange(textBox.CurrentPosition, end - textBox.CurrentPosition);
            for (int i = textBox.CurrentPosition; i < end; i++)
            {
                //Split error text if too long...
                var errText = "";
                int lineCount = 0;
                for (int j = 0; j < error.Length; j++)
                {
                    lineCount++;
                    errText += error[j];
                    if (lineCount > 120 && error[j] == ' ')
                    {
                        lineCount = 0;
                        errText += "\r\n";
                    }
                }

                if (!compilationErrors.ContainsKey(i)) compilationErrors.Add(i, errText);
            }
        }

        public static void CheckRazorSyntax(Scintilla textBox, object objectForCheckSyntax, Dictionary<int, string> compilationErrors, string finalScript = "")
        {
            string error = "";
            const int NUM = 18;
            const int NUM2 = 19;

            // Remove all uses of our indicator
            textBox.IndicatorCurrent = NUM;
            textBox.IndicatorClearRange(0, textBox.TextLength);
            textBox.IndicatorCurrent = NUM2;
            textBox.IndicatorClearRange(0, textBox.TextLength);

            compilationErrors.Clear();
            try
            {
                var script = RazorHelper.GetFullScript(!string.IsNullOrEmpty(finalScript) ? finalScript : textBox.Text);
                if (objectForCheckSyntax is MetaEnum) script = Helper.ClearAllLINQKeywords(script);

                RazorHelper.Compile(script, objectForCheckSyntax.GetType(), Guid.NewGuid().ToString());
            }
            catch (TemplateCompilationException ex)
            {
                //Note: Razor parsing errors are also reported as TemplateCompilationException by RazorEngineCore
                setIndicatorAppearance(textBox, NUM);
                setIndicatorAppearance2(textBox, NUM2);
                Line firstErrorLine = null;
                var sourceLines = ex.CompilationData.SourceCode.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                foreach (var err in ex.CompilerErrors.OrderBy(i => i.Line))
                {
                    //err.Line is 1-based (Roslyn line + 1, see RazorCoreEngine), sourceLines is 0-based
                    if (err.Line > 0 && err.Line <= sourceLines.Length)
                    {
                        //Text of the faulty line in the generated code: Razor keeps the indentation of the script, so it matches the editor line
                        var pattern = sourceLines[err.Line - 1].Trim();
                        var lines = new List<Line>();
                        //1) Exact line given by the #line pragmas of the generated code (valid when the editor shows the compiled script,
                        //which is the common case: the usings appended by GetFullScript do not shift the lines). The text is checked to be safe
                        //(e.g. the editor shows only a function of the whole script, or the header line count of the engine has changed).
                        if (err.TemplateLine > 0 && err.TemplateLine <= textBox.Lines.Count)
                        {
                            var line = textBox.Lines[err.TemplateLine - 1];
                            if (line.Text.Trim() == pattern) lines.Add(line);
                        }
                        //2) Fallback: all the editor lines having the same text (may highlight several lines, e.g. for a '}')
                        if (lines.Count == 0) lines.AddRange(textBox.Lines.Where(i => i.Text.Trim() == pattern));

                        foreach (var line in lines)
                        {
                            textBox.IndicatorCurrent = (err.IsWarning ? NUM2 : NUM);
                            setRazorError(textBox, compilationErrors, line, err.Column, (err.IsWarning ? "Warning: " : "Error: ") + err.ErrorText);

                            if (!err.IsWarning && firstErrorLine == null) firstErrorLine = line;
                        }
                    }
                }
                if (firstErrorLine != null) firstErrorLine.Goto();

                error = string.Format("Compilation error:\r\n{0}", Helper.GetExceptionMessage(ex));
                if (ex.InnerException != null) error += "\r\n" + ex.InnerException.Message;
                if (error.ToLower().Contains("are you missing an assembly reference")) error += string.Format("\r\nNote that you can add assemblies to load by copying your .dll files in the Assemblies Repository folder:'{0}'", Repository.Instance.AssembliesFolder);
            }
            catch (Exception ex)
            {
                error = string.Format("Compilation error:\r\n{0}", ex.Message);
                if (ex.InnerException != null) error += "\r\n" + ex.InnerException.Message;
            }


            if (!string.IsNullOrEmpty(error)) throw new Exception(error);
        }


        public static void RestoreForm(Form form, Size size, Point location, string stateStr)
        {
            if (size.Width > 0 && size.Height > 0)
            {
                form.StartPosition = FormStartPosition.Manual; // Prevents overriding by Windows' default behavior
                form.Location = location;
                form.Size = size;
                // Restore the window state
                FormWindowState state;
                if (Enum.TryParse(stateStr, out state))
                {
                    form.WindowState = state;
                }

                if (!IsFormVisibleOnScreen(form))
                {
                    //Set default if not visible
                    form.Location = new Point(150, 150);
                    form.Size = new Size(1200, 800);
                }
            }

        }


        public static bool IsFormVisibleOnScreen(Form form)
        {
            // Get the bounds of the form
            Rectangle formRectangle = new Rectangle(form.Location, form.Size);

            // Check if the form is visible on any screen
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(formRectangle))
                {
                    return true; // Form is visible (even partially) on the screen
                }
            }
            return false; // Form is not visible on any screen
        }
    }
}
