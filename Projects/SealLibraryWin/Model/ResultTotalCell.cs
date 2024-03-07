//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;

namespace Seal.Model
{
    /// <summary>
    /// A ResultTotalCell is a ResultCell dedicated for totals
    /// </summary>
    public class ResultTotalCell : ResultCell
    {
        const long kTickDivider = 10000000;

        /// <summary>
        /// Sum, Min, Max after claculations
        /// </summary>
        public double? Sum = null, Min = null, Max = null;

        /// <summary>
        /// Date sum after claculations
        /// </summary>
        public long? DateSum = null;

        /// <summary>
        /// Date Min and Max after calculations
        /// </summary>
        public DateTime? DateMin = null, DateMax = null;

        /// <summary>
        /// List of ResultCell
        /// </summary>
        public List<ResultCell> Cells = new List<ResultCell>();

        private bool _done = false;
        /// <summary>
        /// Perform the calculations
        /// </summary>
        public void Calculate()
        {
            if (_done) return;

            Sum = null; Min = null; Max = null;
            DateSum = null;
            DateMin = null; DateMax = null;
            int RealCount = 0;

            foreach (var cell in Cells)
            {
                if (cell.Value != null && cell.Value != DBNull.Value) RealCount++;

                if (Element.IsNumeric)
                {
                    if (cell.DoubleValue != null)
                    {
                        if (Sum == null) Sum = 0;
                        if (Min == null) Min = cell.DoubleValue;
                        if (Max == null) Max = cell.DoubleValue;

                        Sum += cell.DoubleValue;
                        if (cell.DoubleValue < Min) Min = cell.DoubleValue;
                        if (cell.DoubleValue > Max) Max = cell.DoubleValue;
                    }
                }

                if (Element.IsDateTime)
                {
                    if (cell.DateTimeValue != null)
                    {
                        //Uses ticks/kTickDivider = seconds for calculations
                        if (DateSum == null) DateSum = cell.DateTimeValue.Value.Ticks / kTickDivider;
                        else DateSum += cell.DateTimeValue.Value.Ticks / kTickDivider;

                        if (DateMin == null) DateMin = cell.DateTimeValue;
                        if (DateMax == null) DateMax = cell.DateTimeValue;


                        if (cell.DateTimeValue < DateMin) DateMin = cell.DateTimeValue;
                        if (cell.DateTimeValue > DateMax) DateMax = cell.DateTimeValue;
                    }
                }
            }

            //Total of totals for a count -> we make the sum
            if (!IsSerie && (Element.TotalAggregateFunction == AggregateFunction.Count || Element.TotalAggregateFunction == AggregateFunction.CountDistinct)) Value = IsTotalTotal ? Sum : RealCount;

            if (RealCount > 0)
            {
                AggregateFunction aggregateFunction = IsSerie ? Element.AggregateFunction : Element.TotalAggregateFunction;
                if (IsSerie && (aggregateFunction == AggregateFunction.Count || aggregateFunction == AggregateFunction.CountDistinct)) Value = Sum; //Count aggregat for a serie -> we make the sum
                else if (IsTotalTotal && Element.IsCount && Element.TotalAggregateFunction == AggregateFunction.Sum) Value = Sum; //Count aggregat for totals -> we use the sum
                else if (Sum != null && Element.IsNumeric && aggregateFunction == AggregateFunction.Sum) Value = Sum;
                else if (Min != null && Element.IsNumeric && aggregateFunction == AggregateFunction.Min) Value = Min;
                else if (Max != null && Element.IsNumeric && aggregateFunction == AggregateFunction.Max) Value = Max;
                else if (Sum != null && Element.IsNumeric && aggregateFunction == AggregateFunction.Avg) Value = Sum / RealCount;
                else if (DateMin != null && Element.IsDateTime && aggregateFunction == AggregateFunction.Min) Value = DateMin;
                else if (DateMax != null && Element.IsDateTime && aggregateFunction == AggregateFunction.Max) Value = DateMax;
                else if (DateSum != null && Element.IsDateTime && aggregateFunction == AggregateFunction.Avg) Value = new DateTime((long)(DateSum.Value * kTickDivider / RealCount));
            }

            _done = true;
        }

        /// <summary>
        /// Set the common row or col of the cells part of the total. May be used in the cell script for Series values
        /// </summary>
        public void ProcessContext()
        {
            int commonRow = -1, commonCol = -1;
            if (Cells.Count > 0)
            {
                commonRow = Cells[0].ContextRow;
                commonCol = Cells[0].ContextCol;
            }

            foreach (var cell in Cells)
            {
                if (cell.ContextRow != commonRow) commonRow = -1;
                if (cell.ContextCol != commonCol) commonCol = -1;
            }

            if (commonRow != -1) ContextRow = commonRow;
            if (commonCol != -1) ContextCol = commonCol;
        }
    }
}
