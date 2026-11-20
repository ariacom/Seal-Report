//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using ClosedXML.Excel;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ClosedXML.Excel.Drawings;
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
                    //A sheet having only pictures (e.g. gauges or a map) is used
                    if (sheet.LastCellUsed() == null && sheet.Pictures.Count == 0) sheet.Delete();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Headless browser showing the HTML result of the report, opened at the first picture got with GetHtmlElementPicture()
        /// </summary>
        public HeadlessBrowser HeadlessBrowser;

        /// <summary>
        /// Returns the picture (PNG) of an element of the HTML result of the report (e.g. a map or a gauge). The HTML result is only generated if a view requires it (refer to ReportExecution.checkHTMLPrintResult), null is returned otherwise.
        /// </summary>
        public async Task<byte[]> GetHtmlElementPicture(string selector)
        {
            if (string.IsNullOrEmpty(Report.HTMLResultFilePath) || !File.Exists(Report.HTMLResultFilePath)) return null;
            if (HeadlessBrowser == null) HeadlessBrowser = await HeadlessBrowser.Open(Report.Repository, Report.HTMLResultFilePath);
            return await HeadlessBrowser.GetElementPicture(HeadlessBrowser.Page, selector);
        }

        /// <summary>
        /// Close the headless browser if it has been opened
        /// </summary>
        public async Task CloseHeadlessBrowser()
        {
            if (HeadlessBrowser != null) await HeadlessBrowser.DisposeAsync();
            HeadlessBrowser = null;
        }

        /// <summary>
        /// Insert a picture at the current position of the current worksheet and move the current row below it. If a width in pixels is given, the picture is resized with its proportions kept.
        /// </summary>
        public void AddPicture(byte[] image, int width)
        {
            using (var stream = new MemoryStream(image))
            {
                var picture = Worksheet.AddPicture(stream, XLPictureFormat.Png).MoveTo(Worksheet.Cell(CurrentRow, CurrentCol));
                if (width > 0 && picture.OriginalWidth > 0) picture.WithSize(width, (int)Math.Round((double)picture.OriginalHeight * width / picture.OriginalWidth));
                //Leave room for the picture (a default row is about 20 pixels high)
                CurrentRow += (int)Math.Ceiling(picture.Height / 20.0) + 1;
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

        /// <summary>
        /// Returns the path of the PNG or JPG logo to show in the page header: the file name given (in the Views\img folder of the repository, or a full path),
        /// else the logo of the server configuration. Excel does not accept a SVG in a page header: a '.svg' is replaced by the '.png', '.jpg' or '.jpeg' file of the same name.
        /// Returns null if no logo is found (a message is logged).
        /// </summary>
        public string GetHeaderLogoPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) fileName = Report.Repository.Configuration.LogoName;
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            var path = Path.IsPathRooted(fileName) ? fileName : Path.Combine(Report.Repository.ViewImagesFolder, fileName);
            if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var ext in new string[] { ".png", ".jpg", ".jpeg" })
                {
                    var other = Path.ChangeExtension(path, ext);
                    if (File.Exists(other)) return other;
                }
                Report.LogMessage("Excel header logo: no PNG or JPG file found for '{0}' (a SVG cannot be used in a page header).", path);
                return null;
            }
            if (!File.Exists(path))
            {
                Report.LogMessage("Excel header logo: file not found '{0}'.", path);
                return null;
            }
            return path;
        }

        /// <summary>
        /// Top margin of the page (inches) leaving the place of a header logo of the given height (points)
        /// </summary>
        static public double GetHeaderLogoTopMargin(double logoHeight, double headerMargin, double defaultTopMargin)
        {
            return Math.Max(defaultTopMargin, Math.Round(headerMargin + logoHeight / 72 + 0.15, 2));
        }

        /// <summary>
        /// Adds a PNG or JPG logo in the page header (printing, page layout view) of every sheet of a saved Excel file having a header.
        /// ClosedXML has no API for a header picture: the file is completed with the image, a VML drawing per sheet (legacyDrawingHF) and the code '&amp;G' in the header section.
        /// The logo keeps its proportions, within maxHeight points of height and 150 points of width. Position is 'Left', 'Center' or 'Right'.
        /// </summary>
        public static void AddHeaderLogo(string filePath, string logoPath, double maxHeight, string position)
        {
            const double maxWidth = 150; //points
            var image = File.ReadAllBytes(logoPath);
            if (!getImageSize(image, out int pxWidth, out int pxHeight) || pxWidth == 0 || pxHeight == 0) throw new Exception("Unsupported logo format (PNG or JPG only): " + logoPath);
            if (maxHeight <= 0) maxHeight = 30;
            double ptWidth = pxWidth * 0.75, ptHeight = pxHeight * 0.75; //96 dpi
            double ratio = Math.Min(maxHeight / ptHeight, maxWidth / ptWidth);
            ptWidth = Math.Round(ptWidth * ratio, 1);
            ptHeight = Math.Round(ptHeight * ratio, 1);
            bool isPng = image[0] == 0x89;
            string extension = isPng ? "png" : "jpeg";
            char section = string.Equals(position, "Left", StringComparison.OrdinalIgnoreCase) ? 'L' : string.Equals(position, "Center", StringComparison.OrdinalIgnoreCase) ? 'C' : 'R';
            var ic = CultureInfo.InvariantCulture;

            XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace rels = "http://schemas.openxmlformats.org/package/2006/relationships";
            XNamespace ct = "http://schemas.openxmlformats.org/package/2006/content-types";
            XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            const string vmlType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/vmlDrawing";
            const string imageType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";
            const string relId = "rIdSealHeaderLogo";

            using var zipStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Update);

            XDocument load(string name)
            {
                var entry = zip.GetEntry(name);
                if (entry == null) return null;
                using var s = entry.Open();
                return XDocument.Load(s);
            }
            void save(string name, XDocument doc)
            {
                zip.GetEntry(name)?.Delete();
                using var s = zip.CreateEntry(name).Open();
                doc.Save(s, System.Xml.Linq.SaveOptions.DisableFormatting);
            }
            void saveText(string name, string text)
            {
                zip.GetEntry(name)?.Delete();
                using var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
                w.Write(text);
            }

            //Image
            string imageName = "xl/media/sealHeaderLogo." + extension;
            zip.GetEntry(imageName)?.Delete();
            using (var s = zip.CreateEntry(imageName).Open()) s.Write(image, 0, image.Length);

            //Content types
            var types = load("[Content_Types].xml");
            void addDefault(string ext, string contentType)
            {
                if (!types.Root.Elements(ct + "Default").Any(i => string.Equals((string)i.Attribute("Extension"), ext, StringComparison.OrdinalIgnoreCase)))
                {
                    types.Root.AddFirst(new XElement(ct + "Default", new XAttribute("Extension", ext), new XAttribute("ContentType", contentType)));
                }
            }
            addDefault(extension, isPng ? "image/png" : "image/jpeg");
            addDefault("vml", "application/vnd.openxmlformats-officedocument.vmlDrawing");
            save("[Content_Types].xml", types);

            var sheetNames = zip.Entries.Select(i => i.FullName).Where(i => i.StartsWith("xl/worksheets/sheet") && i.EndsWith(".xml")).ToList();
            int index = 0;
            foreach (var sheetName in sheetNames)
            {
                var sheet = load(sheetName);
                var headerFooter = sheet.Root.Element(main + "headerFooter");
                if (headerFooter == null) continue;
                index++;

                //Header: code of the picture in its section, header created if missing (no text) for the pages using it:
                //all pages, the first page if it has its own header and footer, the even pages if they have their own header and footer
                bool differentFirst = (bool?)headerFooter.Attribute("differentFirst") == true;
                bool differentOddEven = (bool?)headerFooter.Attribute("differentOddEven") == true;
                var headerFooterOrder = new string[] { "oddHeader", "oddFooter", "evenHeader", "evenFooter", "firstHeader", "firstFooter" };
                foreach (var headerName in new string[] { "oddHeader", "evenHeader", "firstHeader" })
                {
                    var header = headerFooter.Element(main + headerName);
                    if (header != null) header.Value = AddHeaderPictureCode(header.Value, section);
                    else if (headerName == "oddHeader" || (headerName == "firstHeader" && differentFirst) || (headerName == "evenHeader" && differentOddEven))
                    {
                        header = new XElement(main + headerName, AddHeaderPictureCode("", section));
                        var nextElement = headerFooter.Elements().FirstOrDefault(i => Array.IndexOf(headerFooterOrder, i.Name.LocalName) > Array.IndexOf(headerFooterOrder, headerName));
                        if (nextElement != null) nextElement.AddBeforeSelf(header); else headerFooter.Add(header);
                    }
                }

                //VML drawing of the header, with its relation to the image: one shape per header (LH, CH or RH for all pages,
                //suffix FIRST for the header of the first page, EVEN for the even pages), else Excel puts the picture at the top left
                string shapes = "";
                int shapeIndex = 1;
                foreach (var headerName in new string[] { "oddHeader", "firstHeader", "evenHeader" })
                {
                    if (headerFooter.Element(main + headerName) == null) continue;
                    if ((headerName == "firstHeader" && !differentFirst) || (headerName == "evenHeader" && !differentOddEven)) continue;
                    string shapeId = section + "H" + (headerName == "firstHeader" ? "FIRST" : headerName == "evenHeader" ? "EVEN" : "");
                    shapes += "<v:shape id=\"" + shapeId + "\" o:spid=\"_x0000_s" + (index * 1024 + shapeIndex++) + "\" type=\"#_x0000_t75\" style=\"position:absolute;margin-left:0;margin-top:0;width:"
                        + ptWidth.ToString(ic) + "pt;height:" + ptHeight.ToString(ic) + "pt;z-index:" + shapeIndex + "\">"
                        + "<v:imagedata o:relid=\"rId1\" o:title=\"logo\"/><o:lock v:ext=\"edit\" rotation=\"t\"/></v:shape>";
                }
                string vmlName = "sealHeaderLogo" + index + ".vml";
                string vml = "<xml xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\">"
                    + "<o:shapelayout v:ext=\"edit\"><o:idmap v:ext=\"edit\" data=\"" + index + "\"/></o:shapelayout>"
                    + "<v:shapetype id=\"_x0000_t75\" coordsize=\"21600,21600\" o:spt=\"75\" o:preferrelative=\"t\" path=\"m@4@5l@4@11@9@11@9@5xe\" filled=\"f\" stroked=\"f\">"
                    + "<v:stroke joinstyle=\"miter\"/><v:formulas>"
                    + "<v:f eqn=\"if lineDrawn pixelLineWidth 0\"/><v:f eqn=\"sum @0 1 0\"/><v:f eqn=\"sum 0 0 @1\"/><v:f eqn=\"prod @2 1 2\"/>"
                    + "<v:f eqn=\"prod @3 21600 pixelWidth\"/><v:f eqn=\"prod @3 21600 pixelHeight\"/><v:f eqn=\"sum @0 0 1\"/><v:f eqn=\"prod @6 1 2\"/>"
                    + "<v:f eqn=\"prod @7 21600 pixelWidth\"/><v:f eqn=\"sum @8 21600 0\"/><v:f eqn=\"prod @7 21600 pixelHeight\"/><v:f eqn=\"sum @10 21600 0\"/>"
                    + "</v:formulas><v:path o:extrusionok=\"f\" gradientshapeok=\"t\" o:connecttype=\"rect\"/><o:lock v:ext=\"edit\" aspectratio=\"t\"/></v:shapetype>"
                    + shapes + "</xml>";
                saveText("xl/drawings/" + vmlName, vml);
                saveText("xl/drawings/_rels/" + vmlName + ".rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"" + rels.NamespaceName + "\">"
                    + "<Relationship Id=\"rId1\" Type=\"" + imageType + "\" Target=\"../media/sealHeaderLogo." + extension + "\"/></Relationships>");

                //Relation of the sheet to the VML drawing
                string sheetRelsName = "xl/worksheets/_rels/" + Path.GetFileName(sheetName) + ".rels";
                var sheetRels = load(sheetRelsName) ?? new XDocument(new XElement(rels + "Relationships"));
                sheetRels.Root.Elements(rels + "Relationship").Where(i => (string)i.Attribute("Id") == relId).Remove();
                sheetRels.Root.Add(new XElement(rels + "Relationship", new XAttribute("Id", relId), new XAttribute("Type", vmlType), new XAttribute("Target", "../drawings/" + vmlName)));
                save(sheetRelsName, sheetRels);

                //legacyDrawingHF element at its place in the sequence of the worksheet
                sheet.Root.Elements(main + "legacyDrawingHF").Remove();
                var legacy = new XElement(main + "legacyDrawingHF", new XAttribute(r + "id", relId));
                var after = new string[] { "picture", "oleObjects", "controls", "webPublishItems", "tableParts", "extLst" };
                var next = sheet.Root.Elements().FirstOrDefault(i => i.Name.Namespace == main && after.Contains(i.Name.LocalName));
                if (next != null) next.AddBeforeSelf(legacy); else sheet.Root.Add(legacy);
                save(sheetName, sheet);
            }
        }

        /// <summary>
        /// Adds the picture code '&amp;G' at the start of a section (L, C or R) of an Excel header text
        /// </summary>
        public static string AddHeaderPictureCode(string header, char section)
        {
            header ??= "";
            if (header.Contains("&G")) return header;
            int i = 0;
            while (i < header.Length - 1)
            {
                if (header[i] == '&')
                {
                    if (char.ToUpperInvariant(header[i + 1]) == section) return header.Insert(i + 2, "&G");
                    if (header[i + 1] == '"')
                    {
                        //Font code &"Name,Style": skip to the closing quote
                        int end = header.IndexOf('"', i + 2);
                        i = end < 0 ? header.Length : end + 1;
                        continue;
                    }
                    i += 2;
                    continue;
                }
                i++;
            }
            return header + "&" + section + "&G";
        }

        /// Size of a PNG or JPG image in pixels
        static bool getImageSize(byte[] data, out int width, out int height)
        {
            width = height = 0;
            //PNG: IHDR chunk
            if (data.Length > 24 && data[0] == 0x89 && data[1] == 0x50)
            {
                width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
                height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
                return true;
            }
            //JPEG: SOFn marker
            if (data.Length > 4 && data[0] == 0xFF && data[1] == 0xD8)
            {
                int i = 2;
                while (i + 9 < data.Length)
                {
                    if (data[i] != 0xFF) { i++; continue; }
                    int marker = data[i + 1];
                    int length = (data[i + 2] << 8) | data[i + 3];
                    if (marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
                    {
                        height = (data[i + 5] << 8) | data[i + 6];
                        width = (data[i + 7] << 8) | data[i + 8];
                        return true;
                    }
                    i += 2 + length;
                }
            }
            return false;
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
