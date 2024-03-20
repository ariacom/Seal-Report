//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using MySqlX.XDevAPI.Common;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Linq;

namespace Seal.Renderer
{
    public class ExcelResult
    {
        public Report Report { get; }

        public ExcelResult(Report report)
        {
            Report = report;
            report.ExcelResult = this;
        }

        public ExcelPackage Package;
        public ExcelWorkbook Workbook;
        public ExcelWorksheet Worksheet;

        public int CurrentRow = 1;
        public int CurrentCol = 1;

        public const string CellValueStyle = "CellValueStyle";
        public const string CellValueTotalStyle = "CellValueTotalStyle";
        public const string CellTitleStyle = "CellTitleStyle";

        public void SetValue(ResultCell cell, bool elementFormat, bool useStyle)
        {
            SetValue(Worksheet.Cells[CurrentRow, CurrentCol], cell, elementFormat, useStyle);
        }

        public void SetValue(int row, int col, ResultCell cell, bool elementFormat, bool useStyle)
        {
            SetValue(Worksheet.Cells[row, col], cell, elementFormat, useStyle);
        }

        /// <summary>
        /// Set a ResultCell value in an Excel cell
        /// </summary>
        public void SetValue(ExcelRange cells, ResultCell cell, bool elementFormat, bool useStyle)
        {
            string format = null;
            var cultureInfo = Report.CultureInfo;
            if (cell.Element != null && !cell.Element.IsEnum && !cell.IsTitle && cell.Element.IsNumeric && elementFormat)
            {
                format = cell.Element.GetExcelFormat(cultureInfo);
                if (cell.DoubleValue != null) cells.Value = cell.DoubleValue.Value;
            }
            else if (cell.Element != null && !cell.Element.IsEnum && !cell.IsTitle && cell.Element.IsDateTime && elementFormat)
            {
                format = cell.Element.GetExcelFormat(cultureInfo);
                if (cell.DateTimeValue != null) cells.Value = cell.DateTimeValue.Value;
            }
            else
            {
                if (elementFormat) cells.Value = cell.DisplayValue;
                else if (cell.Value != null) cells.Value = cell.Value.ToString();
            }

            if (useStyle)
            {
                string style = CellValueStyle;
                if (cell.IsTitle) style = CellTitleStyle;
                else if (cell.IsTotal) style = CellValueTotalStyle;

                cells.StyleName = style;
            }

            //Apply format at the end to make it work
            if (!string.IsNullOrEmpty(format))
            {
                cells.Style.Numberformat.Format = format;
            }

        }

        /// <summary>
        /// Clean the current Workbook
        /// </summary>
        public void CleanWorkbook()
        {
            try
            {
                //Clear unused worksheet
                bool cleaning = true;
                while (cleaning)
                {
                    cleaning = false;
                    ExcelWorksheet toClean = null;
                    foreach (var sheet in Workbook.Worksheets)
                    {
                        if (sheet.Dimension == null)
                        {
                            toClean = sheet;
                            break;
                        }
                    }

                    if (toClean != null && Workbook.Worksheets.Count > 1)
                    {
                        cleaning = true;
                        Workbook.Worksheets.Delete(toClean);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Add a Worksheet to the current Workbook
        /// </summary>
        public void AddWorksheet(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "Sheet1";

            if (name.Length > 31) name = name.Substring(0, 30);
            name = Helper.GetUniqueNameCaseInsensitive(name, (from w in Workbook.Worksheets.ToList() select w.Name).ToList());
            Worksheet = Workbook.Worksheets.Add(name);
            CurrentRow = 1;
            CurrentCol = 1;
        }
    }
}
