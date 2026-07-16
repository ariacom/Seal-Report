//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
namespace Seal.Renderer
{
    /// <summary>
    /// Renderer generating a Json result of the report view, the result is built in the <see cref="JsonResult"/> of the report
    /// </summary>
    public class JsonRenderer : RootRenderer
    {

        /// <summary>
        /// Type name of the renderer
        /// </summary>
        public override string GetRenderType()
        {
            return "Json";
        }

        /// <summary>
        /// File name extension of the generated result
        /// </summary>
        public override string GetFileExtension()
        {
            return "json";
        }
    }
}