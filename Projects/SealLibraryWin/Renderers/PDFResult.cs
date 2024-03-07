//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Svg.Skia;
using Seal.Model;
using PuppeteerSharp;

namespace Seal.Renderer
{
    public class PDFResult
    {
        //Quest PDF Objects
        public IDocument Document;
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

    public static class SvgExtensions
    {
        public static void Svg(this IContainer container, SKSvg svg)
        {
            container
                .AlignCenter()
                .AlignMiddle()
                .ScaleToFit()
                .Width(svg.Picture.CullRect.Width)
                .Height(svg.Picture.CullRect.Height)
                .Canvas((canvas, space) => canvas.DrawPicture(svg.Picture));
        }
    }
}

