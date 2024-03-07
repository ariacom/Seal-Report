//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
namespace Seal.Renderer
{
    public class ExcelRenderer : RootRenderer
    {

        /// <summary>
        /// Type name of the renderer
        /// </summary>
        public override string GetRenderType()
        {
            return "Excel";
        }

        /// <summary>
        /// File name extension of the generated result
        /// </summary>
        public override string GetFileExtension()
        {
            return "xlsx";
        }

        /// <summary>
        /// Current result objects used by the Renderer
        /// </summary>
        public ExcelResult Result
        {
            get
            {
                if (View.Report.ExcelResult  == null) { 
                    View.Report.ExcelResult = new ExcelResult(View.Report); 
                }
                return View.Report.ExcelResult;
            }

        }
    }
}
