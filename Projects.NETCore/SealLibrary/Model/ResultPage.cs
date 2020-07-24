//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Helpers;
using System;
using System.Collections.Generic;

namespace Seal.Model
{
    /// <summary>
    /// A ResultPage is a generated for each page value after a model execution. It stores the Pages, Rows, Columns, Datas and Series results.
    /// </summary>
    public class ResultPage
    {
        /// <summary>
        /// Current report
        /// </summary>
        public Report Report;

        /// <summary>
        /// Current model
        /// </summary>
        public ReportModel Model;

        private string _pageId = null;
        /// <summary>
        /// Unique identifier for execution
        /// </summary>
        public string PageId {
            get
            {
                if (string.IsNullOrEmpty(_pageId)) _pageId = Helper.NewGUID();
                return _pageId;
            }
            set
            {
                _pageId = value;
            }
        }

        /// <summary>
        /// Cells generated for the Pages elements
        /// </summary>
        public ResultCell[] Pages;

        /// <summary>
        /// Cells generated for the Rows elements
        /// </summary>
        public List<ResultCell[]> Rows = new List<ResultCell[]>();

        /// <summary>
        /// Cells generated for the Columns elements
        /// </summary>
        public List<ResultCell[]> Columns = new List<ResultCell[]>();

        /// <summary>
        /// Cells generated for the Datas elements
        /// </summary>
        public Dictionary<ResultCell[], List<ResultData>> Datas = new Dictionary<ResultCell[], List<ResultData>>();

        /// <summary>
        /// ResultSerie generated if a serie is defined
        /// </summary>
        public List<ResultSerie> Series = new List<ResultSerie>();

        /// <summary>
        /// Cells used for the primary X axis of the chart
        /// </summary>
        public List<ResultCell[]> PrimaryXDimensions = new List<ResultCell[]>();

        /// <summary>
        /// Cells used for the secondary X axis of the chart
        /// </summary>
        public List<ResultCell[]> SecondaryXDimensions = new List<ResultCell[]>();

        /// <summary>
        /// Valus used for the secondary X axis of the chart
        /// </summary>
        public Dictionary<object, object> PrimaryXValues = new Dictionary<object, object>();

        /// <summary>
        /// Values used for the secondary X axis of the chart
        /// </summary>
        public Dictionary<object, object> SecondaryXValues = new Dictionary<object, object>();

        /// <summary>
        /// Current ResultTable for the page table
        /// </summary>
        public ResultTable PageTable;

        /// <summary>
        /// Current ResultTable for the data table
        /// </summary>
        public ResultTable DataTable;

        /// <summary>
        /// True is the chart initialisation was done
        /// </summary>
        public bool ChartInitDone = false;

        /// <summary>
        /// X labels for the chart
        /// </summary>
        public string ChartXLabels = "";

        /// <summary>
        /// Navigation for the chart
        /// </summary>
        public string ChartNavigations = "";

        /// <summary>
        /// Max length to adjust X axis margins 
        /// </summary>
        public int AxisXLabelMaxLen = 0;

        /// <summary>
        /// Max length to adjust Y primary axis margins 
        /// </summary>
        public int AxisYPrimaryMaxLen = 0;

        /// <summary>
        /// Max length to adjust Y secondary axis margins 
        /// </summary>
        public int AxisYSecondaryMaxLen = 0;

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
    }
}

