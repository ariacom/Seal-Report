//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Converter;
using System.Collections.Generic;

namespace Seal.Model
{
    public class ResultSerieComparer : IComparer<ResultSerie>
    {
        int IComparer<ResultSerie>.Compare(ResultSerie x, ResultSerie y)
        {
            ResultSerie sx = x as ResultSerie;
            ResultSerie sy = y as ResultSerie;

            //Priority to element sort order
            if (sx.Element != sy.Element) return string.Compare(sx.Element.FinalSortOrder, sy.Element.FinalSortOrder);
            else
            {
                //Then by splitter values descending or ascending
                var result = string.Compare(sx.SplitterValues, sy.SplitterValues);
                if (sx.SplitterCells.Length > 0 && sx.SplitterCells[0].Element != null && !sx.SplitterCells[0].Element.SortOrder.Contains(SortOrderConverter.kAscendantSortKeyword))
                {
                    return -1 * result;
                }
                return result;
            }
        }
    }

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
                bool hasMultipleSerieDef = Element.Model.Elements.Exists(i => i != Element && i.PivotPosition == PivotPosition.Data && ((i.ChartJSSerie != ChartJSSerieDefinition.None && Element.ChartJSSerie != ChartJSSerieDefinition.None) || (i.Nvd3Serie != NVD3SerieDefinition.None && Element.Nvd3Serie != NVD3SerieDefinition.None) || (i.PlotlySerie != PlotlySerieDefinition.None || Element.PlotlySerie != PlotlySerieDefinition.None)));
                if (!hasMultipleSerieDef) return string.IsNullOrEmpty(SplitterValues) ? Element.DisplayNameElTranslated : SplitterValues;
                return Element.DisplayNameElTranslated + (string.IsNullOrEmpty(SplitterValues) ? "" : ": " + SplitterValues);
            }
        }


        //True is the splitted element is sort ASC, false for DESC
        public bool SortAscending
        {
            get
            {
                bool result = true;
                if (SplitterCells.Length > 0 && SplitterCells[0].Element != null && !SplitterCells[0].Element.SortOrder.Contains(SortOrderConverter.kAscendantSortKeyword))
                {
                    result = false;
                }
                return result;
            }
        }

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

        public string ChartXYSerieValues;
        public string ChartYXSerieValues;
        public string ChartXSerieValues;
        public string ChartXDateTimeSerieValues;
        public string ChartYSerieValues;
        public string ChartYDateSerieValues;

        public static int CompareSeries(ResultSerie a, ResultSerie b)
        {
            if (a.SplitterCells == null || b.SplitterCells == null) return 0;
            return ResultCell.CompareCells(a.SplitterCells, b.SplitterCells);
        }

    }

    public class ResultSerieValue
    {
        public ResultCell[] XDimensionValues;
        public ResultTotalCell Yvalue;
    }
}
