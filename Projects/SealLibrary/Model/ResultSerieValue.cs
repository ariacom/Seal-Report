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
    }

    public class ResultSerieValue
    {
        public ResultCell[] XDimensionValues;
        public ResultTotalCell Yvalue;
    }
}
