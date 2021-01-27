//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Web;

namespace Seal.Model
{
    /// <summary>
    /// ResultTable are generated after a model execution. It stores list of arrays of ResultCell.
    /// </summary>
    public class ResultTable
    {
        /// <summary>
        /// Start row of the body of the table
        /// </summary>
        public int BodyStartRow = 0;

        /// <summary>
        /// End row of the body of the table
        /// </summary>
        public int BodyEndRow = 0;

        /// <summary>
        /// Strat column of the body
        /// </summary>
        public int BodyStartColumn = 0;

        /// <summary>
        /// List of the ResultTotalCell of the table
        /// </summary>
        public List<ResultTotalCell> TotalCells = new List<ResultTotalCell>();

        /// <summary>
        /// List of a arrays of ResultCell
        /// </summary>
        public List<ResultCell[]> Lines = new List<ResultCell[]>();

        /// <summary>
        /// True if the table has been inverted
        /// </summary>
        public bool InvertDone = false;

        /// <summary>
        /// Custom Tag the can be used at execution time to store any object
        /// </summary>
        public object Tag;

        /// <summary>
        /// Custom Tag the can be used at execution time to store any object
        /// </summary>
        public object Tag2;

        /// <summary>
        /// Custom Tag the can be used at execution time to store any object
        /// </summary>
        public object Tag3;

        private string _lastSearch = "";
        private List<ResultCell[]> _filteredLines = null;

        /// <summary>
        /// Function to return partial table data to the report result
        /// </summary>
        public string GetLoadTableData(ReportView view, string parameter)
        {
            var model = view.ModelView.Model;
            var parameters = parameter.Replace("&lt;", "<").Replace("&gt;", ">").Split('§');

            int echo = 1, len = 50, start = 0;
            string sort = "", search = "";
            try
            {
                if (parameters.Length != 5) throw new Exception("Invalid parameter size");
                echo = int.Parse(parameters[0]);
                sort = parameters[1];
                search = parameters[2];
                len = int.Parse(parameters[3]);
                start = int.Parse(parameters[4]);
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntry("Seal Get Table Data", EventLogEntryType.Error, string.Format("GetLoadTableData-> Error in parameter:{0}\r\n{1}", parameter, ex.Message));
                echo = 1; len = 50; start = 0;
                sort = ""; search = "";
            }
            var sb = new StringBuilder();
            sb.Append("\"aaData\": [");

            //Check filter first
            if (search != _lastSearch || _filteredLines == null)
            {
                start = 0;
                _filteredLines = new List<ResultCell[]>();
                var search2 = search.ToLower();
                for (int row = BodyStartRow; row < BodyEndRow; row++)
                {
                    ResultCell[] line = Lines[row];
                    bool filtered = false;
                    if (string.IsNullOrEmpty(search)) filtered = true;
                    else
                    {
                        for (int col = 0; col < line.Length && !filtered; col++)
                        {
                            ResultCell cell = line[col];
                            var cellValue = cell.HTMLValue;
                            if (!string.IsNullOrEmpty(search) && HttpUtility.HtmlDecode(cellValue).ToLower().Contains(search2))
                            {
                                filtered = true;
                                break;
                            }
                        }
                    }
                    if (filtered) _filteredLines.Add(line);
                }
            }
            _lastSearch = search;

            //handle sort
            if (!string.IsNullOrEmpty(sort) && sort.Contains(",") && _filteredLines.Count > 0)
            {
                var sortIndex = int.Parse(sort.Split(',')[0]);
                var refLine = Lines[BodyStartRow];

                //Handle hidden columns
                for (int col = 0; col < refLine.Length; col++)
                {
                    if (col <= sortIndex && (view.IsColumnHidden(col) || IsColumnHidden(col))) {
                        sortIndex++;
                    }
                }


                if (sortIndex < refLine.Length)
                {
                    //clear other sort
                    foreach (var element in model.Elements) element.FinalSortOrder = "";
                    //set sort to the column, find the related element...
                    foreach (var line in _filteredLines)
                    {
                        ResultCell cell = line[sortIndex];
                        if (cell.Element != null)
                        {
                            var ascdesc = (sort.ToLower().Contains("asc") ? ReportElement.kAscendantSortKeyword : ReportElement.kDescendantSortKeyword);
                            cell.Element.FinalSortOrder = sortIndex + " " + ascdesc;
                            break;
                        }
                    }
                }
                if (ResultCell.ShouldSort(_filteredLines)) _filteredLines.Sort(ResultCell.CompareCellsForTableLoad);
            }

            //build the json result
            if (len == -1)
            {
                start = 0;
                len = _filteredLines.Count;
            }

            var rowBodyClass = view.GetValue("data_table_body_class");
            var rowBodyStyle = view.GetValue("data_table_body_css");
            var rowSubClass = view.GetValue("data_table_subtotal_class");
            var rowSubStyle = view.GetValue("data_table_subtotal_css");

            for (int row = start; row < _filteredLines.Count && row < start + len; row++)
            {
                ResultCell[] line = _filteredLines[row];
                if (row != start) sb.Append(",");
                sb.Append("[");
                for (int col = 0; col < line.Length; col++)
                {
                    if (view.IsColumnHidden(col) || IsColumnHidden(col)) { continue; }
                    ResultCell cell = line[col];
                    var cellValue = cell.HTMLValue;
                    var fullValue = HttpUtility.JavaScriptStringEncode(string.Format("{0}§{1}§{2}§{3}§{4}§{5}", cell.IsSubTotal ? rowSubStyle : rowBodyStyle, cell.IsSubTotal ? rowSubClass : rowBodyClass, model.GetNavigation(view, cell, true), cell.CellCssStyle, cell.CellCssClass, cellValue));
                    sb.AppendFormat("\"{0}\",", fullValue);
                }
                sb.Length = sb.Length - 1;
                sb.Append("]");
            }
            sb.Append("]}");

            var sbFinal = new StringBuilder();
            sbFinal.Append(@"{" + "\"sEcho\": " + echo.ToString() + ",");
            sbFinal.Append("\"recordsTotal\": " + _filteredLines.Count.ToString() + ",");
            sbFinal.Append("\"recordsFiltered\": " + _filteredLines.Count.ToString() + ",");
            sbFinal.Append("\"iTotalRecords\": " + (BodyEndRow - BodyStartRow).ToString() + ",");
            sbFinal.Append("\"iTotalDisplayRecords\": " + _filteredLines.Count.ToString() + ",");
            sbFinal.Append(sb);

            return sbFinal.ToString();
        }

        List<int> _columnsHidden = null;
        /// <summary>
        /// Set a column as hidden
        /// </summary>
        public void SetColumnHidden(int col)
        {
            if (_columnsHidden == null) _columnsHidden = new List<int>();
            if (!_columnsHidden.Contains(col)) _columnsHidden.Add(col);
        }

        /// <summary>
        /// True if the column is hidden
        /// </summary>
        public bool IsColumnHidden(int col)
        {
            if (_columnsHidden != null) return _columnsHidden.Contains(col);
            return false;
        }

        //Helpers
        /// <summary>
        /// Row count of the table
        /// </summary>
        public int RowCount
        {
            get { return Lines.Count; }
        }

        /// <summary>
        /// Column count of the table
        /// </summary>
        public int ColumnCount
        {
            get { return Lines.Count > 0 ? Lines[0].Length : 0; }
        }

        /// <summary>
        /// Helper to access a ResultCell of the table
        /// </summary>
        public ResultCell this[int row, int column]
        {
            get
            {
                return Lines[row][column];
            }
        }

        /// <summary>
        /// True if the row is a sub-total
        /// </summary>
        public bool IsSubTotalRow(int row)
        {
            return (row < RowCount && ColumnCount > 0 && Lines[row][0].IsSubTotal);
        }

        /// <summary>
        /// Returns the ResultTable as a DataTable
        /// </summary>
        public DataTable GetDataTable()
        {
            var result = new DataTable();
            if (RowCount > 0)
            {
                for (int col = 0; col < ColumnCount; col++)
                {
                    result.Columns.Add(this[0, col].DisplayValue);
                }

                for (int row = 1; row < RowCount; row++)
                {
                    var values = new List<string>();
                    for (int col = 0; col < ColumnCount; col++)
                    {
                        values.Add(this[row, col].DisplayValue);
                    }
                    result.Rows.Add(values.ToArray());
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the column number from the element column name. -1 if not found.
        /// </summary>
        public int GetCol(string elementName)
        {
            int result = -1;
            for (int col = 0; col < ColumnCount && RowCount > 0; col++)
            {
                ResultCell cell = this[0, col];
                if (cell != null)
                {
                    if (cell.Element.MetaColumn.ColumnName == elementName)
                    {
                        result = col;
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// True if the column has cells with navigation links
        /// </summary>
        public bool HasNavigation(ReportView view, int sourceRow, int col)
        {
            if (sourceRow == BodyStartRow - 1)
            {
                for (int row = BodyStartRow; row < BodyEndRow && col < ColumnCount; row++)
                {
                    if (this[row, col].GetNavigationLinks(view).Count > 0) return true;
                }
            }
            return false;
        }

    }

}

