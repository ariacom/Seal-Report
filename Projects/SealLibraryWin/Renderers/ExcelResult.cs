//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Linq;
using AngleSharp.Html.Parser;
using AngleSharp.Dom;

namespace Seal.Renderer
{
    /// <summary>
    /// Current Excel objects (package, workbook, worksheet and current position) shared by the Excel renderer templates during the report execution. The instance is available from the report with Report.ExcelResult.
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
        /// Current EPPlus package generating the result file
        /// </summary>
        public ExcelPackage Package;

        /// <summary>
        /// Current workbook of the package
        /// </summary>
        public ExcelWorkbook Workbook;

        /// <summary>
        /// Current worksheet being generated
        /// </summary>
        public ExcelWorksheet Worksheet;

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
        /// Set a ResultCell value at the current row and column
        /// </summary>
        public void SetValue(ResultCell cell, bool elementFormat, bool useStyle)
        {
            SetValue(Worksheet.Cells[CurrentRow, CurrentCol], cell, elementFormat, useStyle);
        }

        /// <summary>
        /// Set a ResultCell value at a given row and column
        /// </summary>
        public void SetValue(int row, int col, ResultCell cell, bool elementFormat, bool useStyle)
        {
            SetValue(Worksheet.Cells[row, col], cell, elementFormat, useStyle);
        }

        /// <summary>
        /// Set a ResultCell value in an Excel cell
        /// </summary>
        public void SetValue(ExcelRange cells, ResultCell cell, bool elementFormat, bool useStyle)
        {
            string format = null;
            var cultureInfo = Report.CultureInfo;
            if (cell.Element != null && !cell.Element.IsEnum && !cell.IsTitle && cell.Element.IsNumeric && elementFormat)
            {
                format = cell.Element.GetExcelFormat(cultureInfo);
                if (cell.DoubleValue != null) cells.Value = cell.DoubleValue.Value;
            }
            else if (cell.Element != null && !cell.Element.IsEnum && !cell.IsTitle && cell.Element.IsDateTime && elementFormat)
            {
                format = cell.Element.GetExcelFormat(cultureInfo);
                if (cell.DateTimeValue != null) cells.Value = cell.DateTimeValue.Value;
            }
            else
            {
                if (elementFormat) cells.Value = cell.DisplayValue;
                else if (cell.Value != null) cells.Value = cell.Value.ToString();
            }

            if (useStyle)
            {
                string style = CellValueStyle;
                if (cell.IsTitle) style = CellTitleStyle;
                else if (cell.IsTotal) style = CellValueTotalStyle;

                cells.StyleName = style;
            }

            //Apply format at the end to make it work
            if (!string.IsNullOrEmpty(format))
            {
                cells.Style.Numberformat.Format = format;
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
                bool cleaning = true;
                while (cleaning)
                {
                    cleaning = false;
                    ExcelWorksheet toClean = null;
                    foreach (var sheet in Workbook.Worksheets)
                    {
                        if (sheet.Dimension == null)
                        {
                            toClean = sheet;
                            break;
                        }
                    }

                    if (toClean != null && Workbook.Worksheets.Count > 1)
                    {
                        cleaning = true;
                        Workbook.Worksheets.Delete(toClean);
                    }
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

            if (name.Length > 31) name = name.Substring(0, 30);
            name = Helper.GetUniqueNameCaseInsensitive(name, (from w in Workbook.Worksheets.ToList() select w.Name).ToList());
            Worksheet = Workbook.Worksheets.Add(name);
            CurrentRow = 1;
            CurrentCol = 1;
        }

        /// <summary>
        /// Converts an HTML string to plain text and writes it into an EPPlus cell.
        /// The cell's WrapText / alignment / merge must be set by the caller.
        /// </summary>
        public void SetHtmlValue(ExcelRange cells, string html)
        {
            cells.Value = HtmlToText.Convert(html);
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
