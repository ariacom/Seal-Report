//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
namespace Seal.Renderer
{
    /// <summary>
    /// Renderer generating a PDF result by converting the HTML result of the report view with a headless browser
    /// </summary>
    public class HTML2PDFRenderer : RootRenderer
    {

        /// <summary>
        /// Type name of the renderer
        /// </summary>
        public override string GetRenderType()
        {
            return "HTML2PDF";
        }

        /// <summary>
        /// Type display name of the renderer
        /// </summary>
        public override string GetRenderDisplayType()
        {
            return "HTML to PDF";
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
}
