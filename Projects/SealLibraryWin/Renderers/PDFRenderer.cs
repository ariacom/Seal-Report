//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System;
using System.IO;
using System.Text;

namespace Seal.Renderer
{
    /// <summary>
    /// Renderer generating a PDF result of the report view with QuestPDF, the document is built in the <see cref="PDFResult"/> of the report
    /// </summary>
    public class PDFRenderer : RootRenderer
    {

        /// <summary>
        /// Type name of the renderer
        /// </summary>
        public override string GetRenderType()
        {
            return "PDF";
        }

        /// <summary>
        /// File name extension of the generated result
        /// </summary>
        public override string GetFileExtension()
        {
            return "pdf";
        }

        /// <summary>
        /// Current result objects used by the Renderer
        /// </summary>
        public PDFResult Result
        {
            get
            {
                if (View.Report.PDFResult == null) { 
                    View.Report.PDFResult = new PDFResult(View.Report); 
                }
                return View.Report.PDFResult;
            }

        }
    }

    /// <summary>
    /// Helpers for image rendering
    /// </summary>
    public static class SkiaSharpHelpers
    {
        /// <summary>
        /// Draws on the container with a SkiaSharp canvas, the result is inserted as a vectorial SVG image
        /// </summary>
        public static void SkiaSharpCanvas(this IContainer container, Action<SKCanvas, Size> drawOnCanvas)
        {
            container.Svg(size =>
            {
                using var stream = new MemoryStream();

                using (var canvas = SKSvgCanvas.Create(new SKRect(0, 0, size.Width, size.Height), stream))
                    drawOnCanvas(canvas, size);

                var svgData = stream.ToArray();
                return Encoding.UTF8.GetString(svgData);
            });
        }

        /// <summary>
        /// Draws on the container with a SkiaSharp canvas, the result is inserted as a rasterized PNG image
        /// </summary>
        public static void SkiaSharpRasterized(this IContainer container, Action<SKCanvas, Size> drawOnCanvas)
        {
            container.Image(payload =>
            {
                using var bitmap = new SKBitmap(payload.ImageSize.Width, payload.ImageSize.Height);

                using (var canvas = new SKCanvas(bitmap))
                {
                    var scalingFactor = payload.Dpi / (float)DocumentSettings.DefaultRasterDpi;
                    canvas.Scale(scalingFactor);
                    drawOnCanvas(canvas, payload.AvailableSpace);
                }

                return bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray();
            });
        }
    }
}
