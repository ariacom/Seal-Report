using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Seal.Model
{
    public class ReportEditor
    {
        public static bool HasEditor()
        {
#if EDITOR
            return true;
#else
            return false;
#endif
        }

    }
}