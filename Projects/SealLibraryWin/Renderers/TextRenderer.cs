//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
namespace Seal.Renderer
{
    /// <summary>
    /// Renderer generating a plain text result of the report view
    /// </summary>
    public class TextRenderer : RootRenderer
    {

        /// <summary>
        /// Type name of the renderer
        /// </summary>
        public override string GetRenderType()
        {
            return "Text";
        }

        /// <summary>
        /// File name extension of the generated result
        /// </summary>
        public override string GetFileExtension()
        {
            return "txt";
        }

    }
}
