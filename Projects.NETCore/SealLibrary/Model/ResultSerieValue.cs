//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;

namespace Seal.Model
{
    /// <summary>
    /// Class to sort the result series when rendering in the view
    /// </summary>
    public class ResultSerieComparer : IComparer<ResultSerie>
    {
        int IComparer<ResultSerie>.Compare(ResultSerie x, ResultSerie y)
        {
            //Priority to element sort order
            if (x.Element != y.Element)
            {
                if (x.Element.FinalSort > y.Element.FinalSort) return 1;
                else if (x.Element.FinalSort < y.Element.FinalSort) return -1;
                return 0;
            }
            else
            {
                //Then by splitter values descending or ascending
                return ResultCell.CompareCells(x.SplitterCells, y.SplitterCells);
            }
        }
    }

    /// <summary>
    /// ResultSerie is a serie result got after a model execution
    /// </summary>
    public class ResultSerie
    {
        /// <summary>
        /// Splitter values as a string
        /// </summary>
        public string SplitterValues;

        /// <summary>
        /// Result cells used for splitting 
        /// </summary>
        public ResultCell[] SplitterCells;

        /// <summary>
        /// Current element
        /// </summary>
        public ReportElement Element = null;

        /// <summary>
        /// List of ResultSerieValue
        /// </summary>
        public List<ResultSerieValue> Values = new List<ResultSerieValue>();

        /// <summary>
        /// Display name
        /// </summary>
        public string SerieDisplayName
        {
            get
            {
                bool hasMultipleSerieDef = Element.Model.Elements.Exists(i => i != Element && i.PivotPosition == PivotPosition.Data && ((i.ChartJSSerie != ChartJSSerieDefinition.None && Element.ChartJSSerie != ChartJSSerieDefinition.None) || (i.Nvd3Serie != NVD3SerieDefinition.None && Element.Nvd3Serie != NVD3SerieDefinition.None) || (i.PlotlySerie != PlotlySerieDefinition.None || Element.PlotlySerie != PlotlySerieDefinition.None)));
                if (!hasMultipleSerieDef) return string.IsNullOrEmpty(SplitterValues) ? Element.DisplayNameElTranslated : SplitterValues;
                return Element.DisplayNameElTranslated + (string.IsNullOrEmpty(SplitterValues) ? "" : ": " + SplitterValues);
            }
        }


        /// <summary>
        /// True is the splitted element is sort ASC, false for DESC 
        /// </summary>
        public bool SortAscending
        {
            get
            {
                bool result = true;
                if (SplitterCells.Length > 0 && SplitterCells[0].Element != null && !SplitterCells[0].Element.SortOrder.Contains(ReportElement.kAscendantSortKeyword))
                {
                    result = false;
                }
                return result;
            }
        }

        /// <summary>
        /// For NVD3 serie, returns the chart type
        /// </summary>
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

        /// <summary>
        /// Compares 2 ResultSerie objects
        /// </summary>
        public static int CompareSeries(ResultSerie a, ResultSerie b)
        {
            if (a.SplitterCells == null || b.SplitterCells == null) return 0;
            return ResultCell.CompareCells(a.SplitterCells, b.SplitterCells);
        }

    }

    /// <summary>
    /// Defines a serie value (One to several X values, one Y value)
    /// </summary>
    public class ResultSerieValue
    {
        /// <summary>
        /// X Values
        /// </summary>
        public ResultCell[] XDimensionValues;

        /// <summary>
        /// Y Value
        /// </summary>
        public ResultTotalCell Yvalue;
    }
}

