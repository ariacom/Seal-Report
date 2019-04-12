//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Converter;
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Web;

namespace Seal.Model
{
    public class ResultTable
    {
        public int BodyStartRow = 0;
        public int BodyEndRow = 0;
        public int BodyStartColumn = 0;
        public List<ResultTotalCell> TotalCells = new List<ResultTotalCell>();

        public List<ResultCell[]> Lines = new List<ResultCell[]>();

        public bool InvertDone = false;

        private string _lastSearch = "";
        private List<ResultCell[]> _filteredLines = null;

        public string GetLoadTableData(ReportView view, string parameter)
        {
            var model = view.Model;
            var parameters = parameter.Replace("&lt;", "<").Replace("&gt;",">").Split('§');

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
                Helper.WriteLogEntryWeb(EventLogEntryType.Error, string.Format("GetLoadTableData-> Error in parameter:{0}\r\n{1}", parameter, ex.Message));
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
                            var cellValue = !string.IsNullOrEmpty(cell.FinalValue) ? cell.FinalValue : cell.DisplayValue;
                            if (!string.IsNullOrEmpty(search) && cellValue.ToLower().Contains(search2))
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
                            var ascdesc = (sort.ToLower().Contains("asc") ? SortOrderConverter.kAscendantSortKeyword : SortOrderConverter.kDescendantSortKeyword);
                            cell.Element.FinalSortOrder = sortIndex + " " + ascdesc;
                            break;
                        }
                    }
                }
                _filteredLines.Sort(ResultCell.CompareCellsForTableLoad);
            }

            //build the json result
            if (len == -1)
            {
                start = 0;
                len = _filteredLines.Count;
            }

            var dataTableView = view.Report.FindViewFromTemplate(view.Views, ReportViewTemplate.DataTableName);
            var rowBodyClass = dataTableView.GetValue("data_table_body_class");
            var rowBodyStyle = dataTableView.GetValue("data_table_body_css");
            var rowSubClass = dataTableView.GetValue("data_table_subtotal_class");
            var rowSubStyle = dataTableView.GetValue("data_table_subtotal_css");

            for (int row = start; row < _filteredLines.Count && row < start + len; row++)
            {
                ResultCell[] line = _filteredLines[row];
                if (row != start) sb.Append(",");
                sb.Append("[");
                for (int col = 0; col < line.Length; col++)
                {
                    if (dataTableView.IsColumnHidden(col) || IsColumnHidden(col)) { continue; }
                    ResultCell cell = line[col];
                    var cellValue = !string.IsNullOrEmpty(cell.FinalValue) ? cell.FinalValue : cell.DisplayValue;
                    var fullValue = HttpUtility.JavaScriptStringEncode(string.Format("{0}§{1}§{2}§{3}§{4}§{5}", cell.IsSubTotal ? rowSubStyle : rowBodyStyle, cell.IsSubTotal ? rowSubClass : rowBodyClass, model.GetNavigation(cell, true), cell.CellCssStyle, cell.CellCssClass, cellValue));
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
        public void SetColumnHidden(int col)
        {
            if (_columnsHidden == null) _columnsHidden = new List<int>();
            if (!_columnsHidden.Contains(col)) _columnsHidden.Add(col);
        }

        public bool IsColumnHidden(int col)
        {
            if (_columnsHidden != null) return _columnsHidden.Contains(col);
            return false;
        }

        //Helpers
        public int RowCount
        {
            get { return Lines.Count; }
        }
        public int ColumnCount
        {
            get { return Lines.Count > 0 ? Lines[0].Length : 0; }
        }

        public ResultCell this[int row, int column]
        {
            get
            {
                return Lines[row][column];
            }
        }

        public bool IsSubTotalRow(int row)
        {
            return (row < RowCount && ColumnCount > 0 && Lines[row][0].IsSubTotal);
        }

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
    }

}
