//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using ClosedXML.Excel;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Linq;
using AngleSharp.Html.Parser;
using AngleSharp.Dom;

namespace Seal.Renderer
{
    /// <summary>
    /// Current Excel objects (workbook, worksheet and current position) shared by the Excel renderer templates during the report execution. The instance is available from the report with Report.ExcelResult.
    /// </summary>
    public class ExcelResult
    {
        /// <summary>
        /// Current report
        /// </summary>
        public Report Report { get; }

        /// <summary>
        /// Creates the Excel result and assigns it to the report
        /// </summary>
        public ExcelResult(Report report)
        {
            Report = report;
            report.ExcelResult = this;
        }

        /// <summary>
        /// Current ClosedXML workbook generating the result file
        /// </summary>
        public XLWorkbook Workbook;

        /// <summary>
        /// Current worksheet being generated
        /// </summary>
        public IXLWorksheet Worksheet;

        /// <summary>
        /// Current row index in the worksheet, starting at 1
        /// </summary>
        public int CurrentRow = 1;

        /// <summary>
        /// Current column index in the worksheet, starting at 1
        /// </summary>
        public int CurrentCol = 1;

        /// <summary>
        /// Name of the style applied to a standard cell value
        /// </summary>
        public const string CellValueStyle = "CellValueStyle";

        /// <summary>
        /// Name of the style applied to a total cell value
        /// </summary>
        public const string CellValueTotalStyle = "CellValueTotalStyle";

        /// <summary>
        /// Name of the style applied to a title cell
        /// </summary>
        public const string CellTitleStyle = "CellTitleStyle";

        /// <summary>
        /// Cell at the current row and column
        /// </summary>
        public IXLCell CurrentCell
        {
            get { return Worksheet.Cell(CurrentRow, CurrentCol); }
        }

        /// <summary>
        /// Apply one of the standard Seal styles (value, title or total) to a style object.
        /// ClosedXML has no named styles, so the attributes are set directly on the cell or the range.
        /// </summary>
        public void ApplyStyle(IXLStyle style, string styleName)
        {
            if (style == null) return;

            if (styleName == CellTitleStyle)
            {
                style.Font.Bold = true;
                style.Font.Italic = false;
            }
            else if (styleName == CellValueTotalStyle)
            {
                style.Font.Bold = true;
                style.Font.Italic = true;
            }
            else
            {
                style.Font.Bold = false;
                style.Font.Italic = false;
            }
        }

        /// <summary>
        /// Apply one of the standard Seal styles to a cell
        /// </summary>
        public void ApplyStyle(IXLCell cell, string styleName)
        {
            if (cell != null) ApplyStyle(cell.Style, styleName);
        }

        /// <summary>
        /// Apply one of the standard Seal styles to a range
        /// </summary>
        public void ApplyStyle(IXLRange range, string styleName)
        {
            if (range != null) ApplyStyle(range.Style, styleName);
        }

        /// <summary>
        /// Set a ResultCell value at the current row and column
        /// </summary>
        public void SetValue(ResultCell cell, bool elementFormat, bool useStyle)
        {
            SetValue(Worksheet.Cell(CurrentRow, CurrentCol), cell, elementFormat, useStyle);
        }

        /// <summary>
        /// Set a ResultCell value at a given row and column
        /// </summary>
        public void SetValue(int row, int col, ResultCell cell, bool elementFormat, bool useStyle)
        {
            SetValue(Worksheet.Cell(row, col), cell, elementFormat, useStyle);
        }

        /// <summary>
        /// Set a ResultCell value in the first cell of a range (e.g. a merged range)
        /// </summary>
        public void SetValue(IXLRange range, ResultCell cell, bool elementFormat, bool useStyle)
        {
            if (range != null) SetValue(range.FirstCell(), cell, elementFormat, useStyle);
        }

        /// <summary>
        /// Set a ResultCell value in an Excel cell
        /// </summary>
        public void SetValue(IXLCell xlCell, ResultCell cell, bool elementFormat, bool useStyle)
        {
            string format = null;
            var cultureInfo = Report.CultureInfo;
            if (cell.Element != null && !cell.Element.IsEnum && !cell.IsTitle && cell.Element.IsNumeric && elementFormat)
            {
                format = cell.Element.GetExcelFormat(cultureInfo);
                if (cell.DoubleValue != null) xlCell.Value = cell.DoubleValue.Value;
            }
            else if (cell.Element != null && !cell.Element.IsEnum && !cell.IsTitle && cell.Element.IsDateTime && elementFormat)
            {
                format = cell.Element.GetExcelFormat(cultureInfo);
                if (cell.DateTimeValue != null) xlCell.Value = cell.DateTimeValue.Value;
            }
            else
            {
                if (elementFormat) xlCell.Value = cell.DisplayValue ?? "";
                else if (cell.Value != null) xlCell.Value = cell.Value.ToString();
            }

            if (useStyle)
            {
                string style = CellValueStyle;
                if (cell.IsTitle) style = CellTitleStyle;
                else if (cell.IsTotal) style = CellValueTotalStyle;

                ApplyStyle(xlCell, style);
            }

            //Apply format at the end to make it work
            if (!string.IsNullOrEmpty(format))
            {
                xlCell.Style.NumberFormat.Format = format;
            }
        }

        /// <summary>
        /// Clean the current Workbook
        /// </summary>
        public void CleanWorkbook()
        {
            try
            {
                //Clear unused worksheet
                foreach (var sheet in Workbook.Worksheets.ToList())
                {
                    if (Workbook.Worksheets.Count <= 1) break;
                    if (sheet.LastCellUsed() == null) sheet.Delete();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Add a Worksheet to the current Workbook
        /// </summary>
        public void AddWorksheet(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "Sheet1";

            //Excel does not accept these characters in a sheet name and ClosedXML rejects them
            foreach (var invalidChar in new char[] { ':', '\\', '/', '?', '*', '[', ']' }) name = name.Replace(invalidChar, ' ');
            name = name.Trim('\'').Trim();
            if (string.IsNullOrEmpty(name)) name = "Sheet1";

            //31 characters is the Excel limit for a sheet name: keep one character free for the unicity suffix added below
            if (name.Length > 31) name = name.Substring(0, 30);
            name = Helper.GetUniqueNameCaseInsensitive(name, (from w in Workbook.Worksheets.ToList() select w.Name).ToList());
            Worksheet = Workbook.Worksheets.Add(name);
            CurrentRow = 1;
            CurrentCol = 1;
        }

        /// <summary>
        /// Converts an HTML string to plain text and writes it into a cell.
        /// The cell WrapText / alignment / merge must be set by the caller.
        /// </summary>
        public void SetHtmlValue(IXLCell cell, string html)
        {
            cell.Value = HtmlToText.Convert(html);
        }

        /// <summary>
        /// Converts an HTML string to plain text and writes it into the first cell of a range (e.g. a merged range).
        /// The range WrapText / alignment / merge must be set by the caller.
        /// </summary>
        public void SetHtmlValue(IXLRange range, string html)
        {
            SetHtmlValue(range.FirstCell(), html);
        }

        /// <summary>
        /// Returns the ClosedXML paper size matching a name. The names of the EPPlus 'ePaperSize' enumeration
        /// stored in existing reports (e.g. 'A4', 'Letter') are mapped to their ClosedXML equivalent ('A4Paper', 'LetterPaper').
        /// </summary>
        static public XLPaperSize GetPaperSize(string name)
        {
            XLPaperSize result;
            if (!string.IsNullOrEmpty(name))
            {
                if (Enum.TryParse(name, true, out result)) return result;
                //Legacy EPPlus name: 'A4' -> 'A4Paper'
                if (Enum.TryParse(name + "Paper", true, out result)) return result;
            }
            return XLPaperSize.A4Paper;
        }

        /// <summary>
        /// Returns the ClosedXML page orientation matching a name
        /// </summary>
        static public XLPageOrientation GetPageOrientation(string name)
        {
            XLPageOrientation result;
            if (!string.IsNullOrEmpty(name) && Enum.TryParse(name, true, out result)) return result;
            return XLPageOrientation.Portrait;
        }
    }

    /// <summary>
    /// Converts limited HTML to plain text with the structure preserved. Usable from any renderer (Excel, Text, ...).
    /// </summary>
    /// <remarks>
    /// Supported tags: h4, p, ul, ol, li, b, em, br.
    /// Inline tags (b, em) are stripped and their text content is kept, block tags produce newlines and list items get bullet or number prefixes.
    /// </remarks>
    public static class HtmlToText
    {
        /// <summary>
        /// Converts an HTML string to structured plain text.
        /// </summary>
        public static string Convert(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var parser   = new HtmlParser();
            var document = parser.ParseDocument(html);

            var sb = new System.Text.StringBuilder();
            ConvertNodes(sb, document.Body?.ChildNodes ?? document.ChildNodes, indent: 0, orderedCounters: new System.Collections.Generic.Stack<int>());

            return sb.ToString().Trim();
        }

        // ---------------------------------------------------------------------

        private static void ConvertNodes(
            System.Text.StringBuilder sb,
            INodeList nodes,
            int       indent,
            System.Collections.Generic.Stack<int> orderedCounters)
        {
            foreach (var node in nodes)
            {
                if (node.NodeType == NodeType.Text)
                {
                    // Raw text outside a block tag — emit as-is (trimmed)
                    var raw = node.TextContent.Trim();
                    if (!string.IsNullOrEmpty(raw))
                        AppendLine(sb, raw, indent);
                    continue;
                }

                if (node.NodeType != NodeType.Element)
                    continue;

                var el  = (IElement)node;
                var tag = el.TagName.ToLower();

                switch (tag)
                {
                    case "h4":
                        AppendLine(sb, InnerText(el), indent);
                        break;

                    case "p":
                        AppendLine(sb, InnerText(el), indent);
                        break;

                    case "br":
                        sb.Append('\n');
                        break;

                    case "ul":
                        foreach (var child in el.ChildNodes)
                        {
                            if (child.NodeType != NodeType.Element) continue;
                            var li = (IElement)child;
                            if (li.TagName.ToLower() != "li") continue;
                            AppendLine(sb, "• " + InnerText(li), indent + 1);
                        }
                        break;

                    case "ol":
                        int counter = 1;
                        foreach (var child in el.ChildNodes)
                        {
                            if (child.NodeType != NodeType.Element) continue;
                            var li = (IElement)child;
                            if (li.TagName.ToLower() != "li") continue;
                            AppendLine(sb, $"{counter}. " + InnerText(li), indent + 1);
                            counter++;
                        }
                        break;

                    default:
                        // Unknown / wrapper tag — recurse into children
                        if (el.HasChildNodes)
                            ConvertNodes(sb, el.ChildNodes, indent, orderedCounters);
                        break;
                }
            }
        }

        /// <summary>
        /// Extracts the plain text of a node, collapsing all inline tags.
        /// Inline &lt;br&gt; is converted to a space to keep text readable.
        /// </summary>
        private static string InnerText(IElement el)
        {
            var sb = new System.Text.StringBuilder();
            ExtractText(sb, el.ChildNodes);
            return sb.ToString().Trim();
        }

        private static void ExtractText(System.Text.StringBuilder sb, INodeList nodes)
        {
            foreach (var node in nodes)
            {
                if (node.NodeType == NodeType.Text)
                {
                    sb.Append(node.TextContent);
                }
                else if (node.NodeType == NodeType.Element)
                {
                    var el  = (IElement)node;
                    var tag = el.TagName.ToLower();
                    if (tag == "br")
                        sb.Append(' ');
                    else if (el.HasChildNodes)
                        ExtractText(sb, el.ChildNodes);
                }
            }
        }

        /// <summary>
        /// Appends a line with optional indentation. Each indent level adds two spaces.
        /// Ensures the previous content ends with a newline before writing.
        /// </summary>
        private static void AppendLine(System.Text.StringBuilder sb, string text, int indent)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Ensure block separation: don't double-blank, but do add newline between blocks
            if (sb.Length > 0 && sb[sb.Length - 1] != '\n')
                sb.Append('\n');

            if (indent > 0)
                sb.Append(new string(' ', indent * 2));

            sb.Append(text);
            sb.Append('\n');
        }
    }
}
