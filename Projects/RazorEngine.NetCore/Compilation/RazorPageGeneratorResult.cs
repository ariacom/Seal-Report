using System;
using System.Collections.Generic;
using System.Text;

namespace RazorEngine.Compilation
{
    //https://github.com/aspnet/Razor/blob/27e66c37505fce36c4bb368bbe06d7d301a13617/src/RazorPageGenerator/RazorPageGeneratorResult.cs
    public class RazorPageGeneratorResult
    {
        public string FilePath { get; set; }

        public string GeneratedCode { get; set; }
    }
}
