//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;

namespace Seal.Model
{
    public class ResultPage
    {
        public Report Report;
        public ReportModel Model;

        private string _pageId = null;
        public string PageId {
            get
            {
                if (string.IsNullOrEmpty(_pageId)) _pageId = Guid.NewGuid().ToString();
                return _pageId;
            }
            set
            {
                _pageId = value;
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

        //For Charts
        public bool ChartInitDone = false;
        public string ChartXLabels = "";
        public string ChartNavigations = "";

        //Max lengths to adjust margins
        public int AxisXLabelMaxLen = 0;
        public int AxisYPrimaryMaxLen = 0;
        public int AxisYSecondaryMaxLen = 0;
    }
}
