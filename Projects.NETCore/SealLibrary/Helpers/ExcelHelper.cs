//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//

using Microsoft.VisualBasic.FileIO;
using OfficeOpenXml;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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


        static public bool IsRowEmpty(ExcelWorksheet worksheet, int row, int startCol, int colCount)
        {
            bool rowEmpty = true;
            for (int i = startCol; i <= startCol + colCount; i++)
            {
                if (worksheet.Cells[row, i].Value != null)
                {
                    rowEmpty = false;
                    break;
                }
            }
            return rowEmpty;
        }

        /// <summary>
        /// Load a DataTable from an Excel file. A start and end row, and/or colum can be specified. If hasHeader is false, column names are automatic. 
        /// </summary>
        static public DataTable LoadDataTableFromExcel(string excelPath, string tabName = "", int startRow = 1, int startCol = 1, int endRow = 0, int endCol = 0, bool hasHeader = true)
        {
            ExcelPackage package;
            try
            {
                package = new ExcelPackage(new FileInfo(excelPath));
            }
            catch
            {
                string newPath = FileHelper.GetTempUniqueFileName(excelPath);
                FileHelper.PurgeTempApplicationDirectory();
                File.Copy(excelPath, newPath, true);
                package = new ExcelPackage(new FileInfo(newPath));
            }
            var workbook = package.Workbook;
            ExcelWorksheet worksheet = null;
            if (workbook.Worksheets.Count == 0) throw new Exception("No sheet in the workbook.");
            if (!string.IsNullOrEmpty(tabName))
            {
                foreach (ExcelWorksheet ws in workbook.Worksheets)
                {
                    if (ws.Name.ToLower() == tabName.ToLower())
                    {
                        worksheet = ws;
                        break;
                    }
                }
                if (worksheet == null) throw new Exception("Unable to find tab name specified.");
            }
            else worksheet = workbook.Worksheets.First();

            DataTable result = new DataTable();
            int colTitle = startCol;
            int colCount = 0, index = startCol;
            while ((endCol == 0 && worksheet.Cells[startRow, index + 1].Value != null) || colCount < endCol)
            {
                colCount++;
                index++;
            }

            while ((endCol == 0 && (worksheet.Cells[startRow, colTitle].Value != null)) || colTitle <= endCol)
            {
                int rowTitle = startRow;
                string colName = worksheet.Cells[startRow, colTitle].Address;
                if (hasHeader)
                {
                    colName = worksheet.Cells[startRow, colTitle].Text;
                    if (string.IsNullOrEmpty(colName)) colName = worksheet.Cells[startRow, colTitle].Value.ToString();
                    rowTitle++;
                }
                else colName = colName.Replace("'", "").Replace("!", "").Replace(worksheet.Name, "");
                //get the type
                Type t = typeof(string);
                if (worksheet.Cells[rowTitle, colTitle] != null && worksheet.Cells[rowTitle, colTitle].Value != null)
                {
                    t = worksheet.Cells[rowTitle, colTitle].Value.GetType();
                    //check that the type is consistent
                    if (t != typeof(string))
                    {
                        rowTitle++;
                        while (!IsRowEmpty(worksheet, rowTitle, startCol, colCount))
                        {
                            if (worksheet.Cells[rowTitle, colTitle].Value != null && t != worksheet.Cells[rowTitle, colTitle].Value.GetType())
                            {
                                t = typeof(string);
                                break;
                            }
                            rowTitle++;
                        }
                    }
                }
                result.Columns.Add(colName, t);
                colTitle++;
            }

            //copy values
            int rowValue = startRow;
            if (hasHeader) rowValue++;
            while ((endRow == 0 && !IsRowEmpty(worksheet, rowValue, startCol, result.Columns.Count)) || rowValue < endRow)
            {
                DataRow dr = result.Rows.Add();
                for (int colValue = startCol; colValue < startCol + result.Columns.Count; colValue++)
                {
                    object val = null;
                    if (worksheet.Cells[rowValue, colValue].Value != null)
                    {
                        string valText = worksheet.Cells[rowValue, colValue].Text;
                        if (!string.IsNullOrEmpty(worksheet.Cells[rowValue, colValue].Text)) valText = worksheet.Cells[rowValue, colValue].Value.ToString();

                        if (string.IsNullOrEmpty(valText)) val = worksheet.Cells[rowValue, colValue].Value;
                        else val = valText;
                    }
                    if (val == null) val = DBNull.Value;
                    dr[colValue - startCol] = val;
                }
                rowValue++;
            }

            return result;
        }


        static public DataTable LoadDataTableFromCSV(string csvPath, char? separator, Encoding encoding)
        {
            DataTable result = null;
            bool isHeader = true;
            Regex regexp = null;

            string[] lines = null;
            try
            {
                lines = File.ReadAllLines(csvPath, encoding);
            }
            catch
            {
                //Try by copying the file...
                string newPath = FileHelper.GetTempUniqueFileName(csvPath);
                File.Copy(csvPath, newPath);
                lines = File.ReadAllLines(newPath, encoding);
                FileHelper.PurgeTempApplicationDirectory();
            }


            foreach (string line in lines)
            {
                var line2 = line.Trim();
                if (string.IsNullOrWhiteSpace(line2)) continue;

                if (regexp == null)
                {
                    if (separator == null)
                    {
                        //use the first line to determine the separator between , and ;
                        separator = ',';
                        if (line2.Split(';').Length > line2.Split(',').Length) separator = ';';
                    }
                    var sep2 = (separator.Value == '|' || separator.Value == ':' ? Path.DirectorySeparatorChar.ToString() : "") + separator.Value;
                    string exp = "(?<=^|" + sep2 + ")(\"(?:[^\"]|\"\")*\"|[^" + sep2 + "]*)";
                    regexp = new Regex(exp);
                }

                MatchCollection collection = regexp.Matches(line2);
                if (isHeader)
                {
                    result = new DataTable();
                    for (int i = 0; i < collection.Count; i++)
                    {
                        result.Columns.Add(new DataColumn(ExcelHelper.FromCsv(collection[i].Value), typeof(string)));
                    }
                    isHeader = false;
                }
                else
                {
                    var row = result.Rows.Add();
                    for (int i = 0; i < collection.Count && i < result.Columns.Count; i++)
                    {
                        row[i] = ExcelHelper.FromCsv(collection[i].Value);
                        if (row[i].ToString().Contains("\0")) row[i] = "";
                    }
                }
            }

            return result;
        }


        static public DataTable LoadDataTableFromCSVVBParser(string csvPath, char? separator, Encoding encoding)
        {
            DataTable result = null;
            bool isHeader = true;
            TextFieldParser csvParser;
            try
            {
                csvParser = new TextFieldParser(csvPath, encoding);
            }
            catch
            {
                //Try by copying the file...
                string newPath = FileHelper.GetTempUniqueFileName(csvPath);
                File.Copy(csvPath, newPath);
                csvParser = new TextFieldParser(newPath, encoding);
                FileHelper.PurgeTempApplicationDirectory();
            }
            if (separator == null) separator = ',';
            //csvParser.CommentTokens = new string[] { "#" };
            csvParser.SetDelimiters(new string[] { separator.ToString() });
            csvParser.HasFieldsEnclosedInQuotes = true;

            while (!csvParser.EndOfData)
            {
                string[] fields = csvParser.ReadFields();
                if (isHeader)
                {
                    result = new DataTable();
                    for (int i = 0; i < fields.Length; i++)
                    {
                        result.Columns.Add(new DataColumn(fields[i], typeof(string)));
                    }
                    isHeader = false;
                }
                else
                {
                    var row = result.Rows.Add();
                    for (int i = 0; i < fields.Length && i < result.Columns.Count; i++)
                    {
                        row[i] = fields[i];
                        if (row[i].ToString().Contains("\0")) row[i] = "";
                    }
                }
            }
            csvParser.Close();

            return result;
        }

    }
}

