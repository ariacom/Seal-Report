//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Converter;
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public string GetLoadTableData(ReportModel model, string parameter)
        {
            var parameters = parameter.Split('§');

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
                Helper.WriteLogEntryWeb(System.Diagnostics.EventLogEntryType.Error, string.Format("GetLoadTableData-> Error in parameter:{0}\r\n{1}", parameter, ex.Message));
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

            for (int row = start; row < _filteredLines.Count && row < start + len; row++)
            {
                ResultCell[] line = _filteredLines[row];
                if (row != start) sb.Append(",");

                sb.Append("[");
                for (int col = 0; col < line.Length; col++)
                {
                    if (col > 0) sb.Append(",");
                    ResultCell cell = line[col];
                    string className = cell.IsTitle && col == 0 ? "cell_title" : "cell_value";
                    className = cell.IsTotal ? "cell_value_total" : className;
                    var cellValue = !string.IsNullOrEmpty(cell.FinalValue) ? cell.FinalValue : cell.DisplayValue;
                    var fullValue = HttpUtility.JavaScriptStringEncode(string.Format("{0}§{1}§{2}§{3}", model.GetNavigation(cell, true), cell.CellCssStyle, className, cellValue));
                    sb.AppendFormat("\"{0}\"", fullValue);
                }
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
    }

}
