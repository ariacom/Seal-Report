//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using Seal.Helpers;

namespace Seal.Model
{
    public class ResultPage
    {
        public Report Report;

        private string _pageId = null;
        public string PageId {
            get
            {
                if (string.IsNullOrEmpty(_pageId)) _pageId = Guid.NewGuid().ToString();
                return _pageId;
            }
        }
        public ResultCell[] Pages;
        public List<ResultCell[]> Rows = new List<ResultCell[]>();
        public List<ResultCell[]> Columns = new List<ResultCell[]>();
        public Dictionary<ResultCell[], List<ResultData>> Datas = new Dictionary<ResultCell[], List<ResultData>>();

        //Series
        public List<ResultSerie> Series = new List<ResultSerie>();
        public List<ResultCell[]> PrimaryXDimensions = new List<ResultCell[]>();
        public List<ResultCell[]> SecondaryXDimensions = new List<ResultCell[]>();
        public Dictionary<object, object> PrimaryXValues = new Dictionary<object, object>();
        public Dictionary<object, object> SecondaryXValues = new Dictionary<object, object>();

        public ResultTable PageTable;
        public ResultTable DataTable;

        //For MS Chart
        public Chart Chart = null;
        public string ChartPath = null;
        public string ChartFileName = null;

        //For NVD3 Chart
        public string NVD3ChartType = "";
        public string NVD3XLabels = "";
        public string NVD3XAxisFormat = "";
        public string NVD3PrimaryYAxisFormat = "";
        public bool NVD3PrimaryYIsDateTime = false;
        public string NVD3SecondaryYAxisFormat = "";
        public bool NVD3SecondaryYIsDateTime = false;
        public bool NVD3IsNumericAxis;
        public bool NVD3IsDateTimeAxis;
    }
}
