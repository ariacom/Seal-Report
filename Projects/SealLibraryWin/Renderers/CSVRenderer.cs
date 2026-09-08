//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Seal.Helpers;

namespace Seal.Renderer
{
    /// <summary>
    /// Renderer generating a CSV result of the report view
    /// </summary>
    public class CSVRenderer : RootRenderer
    {

        /// <summary>
        /// Type name of the renderer
        /// </summary>
        public override string GetRenderType()
        {
            return "CSV";
        }

        /// <summary>
        /// File name extension of the generated result
        /// </summary>
        public override string GetFileExtension()
        {
            return "csv";
        }

        /// <summary>
        /// Separator used for the CSV values: the 'csv_separator' parameter of the view, then the server configuration, then the list separator of the repository culture
        /// </summary>
        public string GetSeparator()
        {
            string separator = GetValue("csv_separator");
            if (string.IsNullOrEmpty(separator) && View != null) separator = View.Report.Repository.Configuration.CsvSeparator;
            if (string.IsNullOrEmpty(separator) && View != null) separator = View.Report.Repository.CultureInfo.TextInfo.ListSeparator;
            return string.IsNullOrEmpty(separator) ? "," : separator;
        }

        /// <summary>
        /// Quoting mode of the CSV values from the 'csv_quoting' parameter (Always by default)
        /// </summary>
        public CsvQuoting GetQuoting()
        {
            return ExcelHelper.GetCsvQuoting(GetValue("csv_quoting"));
        }

        /// <summary>
        /// Line ending of the CSV lines from the 'csv_line_ending' parameter (CRLF by default)
        /// </summary>
        public string GetLineEnding()
        {
            return ExcelHelper.GetCsvLineEnding(GetValue("csv_line_ending"));
        }

    }
}
