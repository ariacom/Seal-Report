//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//

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

        static public string FromCsv(string value)
        {
            string result = value;
            if (value.StartsWith("\"") && value.EndsWith("\"")) result = result.Substring(1, value.Length - 2);
            result = result.Replace("\"\"", "\"");
            return result;
        }

    }
}
