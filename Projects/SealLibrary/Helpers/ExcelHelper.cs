//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seal.Helpers
{
    public class ExcelHelper
    {
        static public string ToCsv(string value, string separator = "\t")
        {
            string val = value;
            if (val != null) val = val.Replace("\"", "\"\"");
            return string.Format("\"{0}\"{1}", val, separator);
        }
    }}
