//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using ScottPlot;
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        /// Values used for the primary X axis of the chart
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
        public List<string> ChartXLabels = new List<string>();

        /// <summary>
        /// Navigation for the chart
        /// </summary>
        public List<string> ChartNavigations = new List<string>();

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

        /// <summary>
        /// Plot charts generated
        /// </summary>
        public List<Plot> Plots = new List<Plot>();

        /// <summary>
        /// Returns the current page name
        /// </summary>
        public string PageName
        {
            get
            {
                string result = "";
                for (int i = 0; i < PageTable.Lines[0].Length; i++)
                {
                    if (!PageTable.Lines[1][i].IsTotal)
                    {
                        if (!string.IsNullOrEmpty(result)) result += ",";
                        result += PageTable.Lines[1][i].DisplayValue;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Dictionary to store the identifiers generated during the page rendering.
        /// Used for storing JavaScript Charts Canvas Id
        /// </summary>
        public Dictionary<ResultPageIdentifierType, List<string>> Identifiers = new Dictionary<ResultPageIdentifierType, List<string>>();

        public void SetIdentifier(ResultPageIdentifierType type, string id)
        {
            if (!Identifiers.ContainsKey(type)) Identifiers.Add(type, new List<string>());
            if (!Identifiers[type].Contains(id)) Identifiers[type].Add(id);
        }

        public List<string> GetIdentifiers(ResultPageIdentifierType type)
        {
            if (Identifiers.ContainsKey(type)) return Identifiers[type];
            return new List<string>();
        }

        public void ClearIdentifiers(ResultPageIdentifierType type)
        {
            if (Identifiers.ContainsKey(type)) Identifiers[type].Clear();
        }
    }
}
