using RazorEngine.Templating;
using ScintillaNET;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seal.Forms
{
    class FormHelper
    {
        public static void CheckRazorSyntax(Scintilla textBox, string header, object objectForCheckSyntax, Dictionary<int, string> compilationErrors)
        {
            string error = "";
            const int NUM = 18;

            // Remove all uses of our indicator
            textBox.IndicatorCurrent = NUM;
            textBox.IndicatorClearRange(0, textBox.TextLength);

            compilationErrors.Clear();
            try
            {
                string script = textBox.Text;
                string scriptHeader = header;
                if (scriptHeader == null) scriptHeader = RazorHelper.GetScriptHeader(objectForCheckSyntax);
                if (!string.IsNullOrEmpty(scriptHeader)) script += "\r\n" + scriptHeader;
                RazorHelper.Compile(script, objectForCheckSyntax.GetType(), Guid.NewGuid().ToString());
            }
            catch (TemplateCompilationException ex)
            {
                // Update indicator appearance
                textBox.Indicators[NUM].Style = IndicatorStyle.StraightBox;
                textBox.Indicators[NUM].Under = true;
                textBox.Indicators[NUM].ForeColor = Color.Red;
                textBox.Indicators[NUM].OutlineAlpha = 120;
                textBox.Indicators[NUM].Alpha = 120;

                foreach (var err in ex.CompilerErrors)
                {
                    var sourceLines = ex.CompilationData.SourceCode.Split('\n');
                    if (err.Line > 0 && err.Line < sourceLines.Length)
                    {
                        var pattern = sourceLines[err.Line - 1].Trim();
                        foreach (var line in textBox.Lines)
                        {
                            var line2 = line.Text.Trim();
                            if (line2 == pattern)
                            {
                                line.Goto();
                                textBox.CurrentPosition += err.Column - 1;
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
                                    for (int j = 0; j < err.ErrorText.Length; j++)
                                    {
                                        lineCount++;
                                        errText += err.ErrorText[j];
                                        if (lineCount > 120 && err.ErrorText[j] == ' ')
                                        {
                                            lineCount = 0;
                                            errText += "\r\n";
                                        }
                                    }

                                    if (!compilationErrors.ContainsKey(i)) compilationErrors.Add(i, errText);
                                }
                            }
                        }
                    }
                }

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
    }
}
