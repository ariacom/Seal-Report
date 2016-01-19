//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;

namespace Seal.Model
{
    public class ResultSerie
    {
        public string SplitterValues;
        public ResultCell[] SplitterCells;
        public ReportElement Element = null;
        public List<ResultSerieValue> Values = new List<ResultSerieValue>();

        public string SerieDisplayName
        {
            get
            {
                bool hasMultipleSerieDef = Element.Model.Elements.Exists(i => i != Element && i.SerieDefinition == Element.SerieDefinition);
                if (!hasMultipleSerieDef) return string.IsNullOrEmpty(SplitterValues) ? Element.DisplayNameElTranslated : SplitterValues;
                return Element.DisplayNameElTranslated + (string.IsNullOrEmpty(SplitterValues) ? "" : ": " + SplitterValues);
            }
        }

        public string NVD3SerieValues;
        public string NVD3MultiChartType
        {
            get
            {
                if (Element.Nvd3Serie == NVD3SerieDefinition.Line) return "line";
                else if (Element.Nvd3Serie == NVD3SerieDefinition.StackedAreaChart) return "area";
                else if (Element.Nvd3Serie == NVD3SerieDefinition.MultiBarChart) return "bar";
                else if (Element.Nvd3Serie == NVD3SerieDefinition.ScatterChart) return "scatter";
                return "";
            }
        }

        public static int CompareSeries(ResultSerie a, ResultSerie b)
        {
            if (a.SplitterCells == null || b.SplitterCells == null) return 0;
            return ResultCell.CompareCells(a.SplitterCells, b.SplitterCells);

       /*     a.


            if (a.Length == 0 || a.Length != b.Length) return 0;
            ReportModel model = a[0].Element.Model;

            foreach (ReportElement element in model.Elements.OrderBy(i => i.FinalSortOrder))
            {
                ResultCell aCell = a.FirstOrDefault(i => i.Element == element);
                ResultCell bCell = b.FirstOrDefault(i => i.Element == element);
                if (aCell != null && bCell != null)
                {
                    int result = CompareCell(aCell, bCell);
                    if (result != 0) return (element.SortOrder.Contains(SortOrderConverter.kAscendantSortKeyword) ? 1 : -1) * result;
                }
            }*/
            //return 0;
        }

    }

    public class ResultSerieValue
    {
        public ResultCell[] XDimensionValues;
        public ResultTotalCell Yvalue;
    }
}
