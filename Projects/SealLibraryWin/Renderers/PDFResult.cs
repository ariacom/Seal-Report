//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Svg.Skia;
using Seal.Model;
using PuppeteerSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Dom;
using IHtmlElement = AngleSharp.Dom.IElement;

namespace Seal.Renderer
{
    public class PDFResult
    {
        //Quest PDF Objects
        public QuestPDF.Infrastructure.IDocument Document;
        public IContainer Container;

        //Puppeeter Objects
        public IBrowser Browser;
        public IPage Page;

        public Report Report { get; }

        public PDFResult(Report report)
        {
            Report = report;
            report.PDFResult = this;
        }
    }


    // =========================================================================
    // HtmlToQuestPdf — renders limited HTML into a QuestPDF IContainer.
    // Supported tags: <h4>, <p>, <ul>, <ol>, <li>, <b>, <em>, <br>
    // Block tags map to column items; inline tags produce styled spans.
    // Uses AngleSharp (already a project dependency) — no extra NuGet needed.
    //
    // Usage in a Razor template:
    //   column.Item().PaddingTop(10).Element(c =>
    //       HtmlToQuestPdf.Render(c, Report.ExecutionView.GetValue("report_summary")));
    // =========================================================================

    public static class HtmlToQuestPdf
    {
        public static void Render(IContainer container, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return;

            var parser   = new HtmlParser();
            var document = parser.ParseDocument(html);
            var nodes    = document.Body?.ChildNodes ?? document.ChildNodes;

            container.Column(col =>
            {
                col.Spacing(4);
                RenderBlockNodes(col, nodes);
            });
        }

        // ---------------------------------------------------------------------
        // Block-level rendering
        // ---------------------------------------------------------------------

        private static void RenderBlockNodes(ColumnDescriptor col, INodeList nodes)
        {
            foreach (var node in nodes)
            {
                if (node.NodeType == NodeType.Text)
                {
                    var raw = node.TextContent.Trim();
                    if (!string.IsNullOrEmpty(raw))
                        col.Item().Text(raw);
                    continue;
                }

                if (node.NodeType != NodeType.Element)
                    continue;

                var el  = (IHtmlElement)node;
                var tag = el.TagName.ToLower();

                switch (tag)
                {
                    case "h4":
                        col.Item().Text(t =>
                        {
                            t.DefaultTextStyle(s => s.Bold().FontSize(13));
                            AppendInlineSpans(t, el.ChildNodes);
                        });
                        break;

                    case "p":
                        col.Item().Text(t => AppendInlineSpans(t, el.ChildNodes));
                        break;

                    case "ul":
                        RenderList(col, el, ordered: false);
                        break;

                    case "ol":
                        RenderList(col, el, ordered: true);
                        break;

                    case "br":
                        col.Item().Height(4);
                        break;

                    // Inline tags at block level — wrap in a text item
                    case "b":
                    case "strong":
                        col.Item().Text(t => AppendInlineSpans(t, el.ChildNodes, bold: true));
                        break;

                    case "em":
                    case "i":
                        col.Item().Text(t => AppendInlineSpans(t, el.ChildNodes, italic: true));
                        break;

                    default:
                        if (el.HasChildNodes)
                            RenderBlockNodes(col, el.ChildNodes);
                        break;
                }
            }
        }

        // ---------------------------------------------------------------------
        // List rendering
        // ---------------------------------------------------------------------

        private static void RenderList(ColumnDescriptor col, IHtmlElement listEl, bool ordered)
        {
            int counter = 1;

            foreach (var child in listEl.ChildNodes)
            {
                if (child.NodeType != NodeType.Element) continue;
                var li = (IHtmlElement)child;
                if (li.TagName.ToLower() != "li") continue;

                string bullet = ordered ? $"{counter}." : "•";

                col.Item().Row(row =>
                {
                    row.ConstantItem(18).Text(bullet);
                    row.RelativeItem().Text(t => AppendInlineSpans(t, li.ChildNodes));
                });

                counter++;
            }
        }

        // ---------------------------------------------------------------------
        // Inline span rendering — threads bold/italic state through recursion
        // so nested tags compose: <b>bold <em>bold+italic</em></b>
        // ---------------------------------------------------------------------

        private static void AppendInlineSpans(
            TextDescriptor text,
            INodeList      nodes,
            bool           bold   = false,
            bool           italic = false)
        {
            foreach (var node in nodes)
            {
                if (node.NodeType == NodeType.Text)
                {
                    var content = node.TextContent;  // AngleSharp decodes entities
                    if (!string.IsNullOrEmpty(content))
                    {
                        var span = text.Span(content);
                        if (bold)   span.Bold();
                        if (italic) span.Italic();
                    }
                    continue;
                }

                if (node.NodeType != NodeType.Element)
                    continue;

                var el  = (IHtmlElement)node;
                var tag = el.TagName.ToLower();

                switch (tag)
                {
                    case "b":
                    case "strong":
                        AppendInlineSpans(text, el.ChildNodes, bold: true,  italic: italic);
                        break;

                    case "em":
                    case "i":
                        AppendInlineSpans(text, el.ChildNodes, bold: bold,  italic: true);
                        break;

                    case "br":
                        text.Line("");
                        break;

                    default:
                        if (el.HasChildNodes)
                            AppendInlineSpans(text, el.ChildNodes, bold, italic);
                        break;
                }
            }
        }
    }


    public static class CellExtensions
    {
        public static IContainer AlignCell(this IContainer container, ResultCell cell)
        {
            if (cell.Element != null)
            {
                var cellClass = cell.FinalCssClass ?? "";
                var cellStyle = cell.FinalCssStyle ?? "";
                if (cellClass.Contains("text-left") || cellStyle.Contains("text-align:left"))
                {
                    return container.AlignLeft();
                }
                else if (cellClass.Contains("text-center") || cellStyle.Contains("text-align:center"))
                {
                    return container.AlignCenter();
                }
                else if (cellClass.Contains("text-right") || cellStyle.Contains("text-align:right"))
                {
                    return container.AlignRight();
                }
                else if (!cell.Element.IsEnum &&  (cell.Element.IsNumeric || cell.Element.IsDateTime))
                {
                    return container.AlignRight();
                }
            }
            return container.AlignLeft();
        }
    }
}

