//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;

namespace Seal.Model
{
    public class ResultTotalCell : ResultCell
    {
        const long kTickDivider = 10000000;

        public double? Sum = null, Min = null, Max = null;
        public long? DateSum = null;
        public DateTime? DateMin = null, DateMax = null;

        public List<ResultCell> Cells = new List<ResultCell>();

        private bool _done = false;
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
            if (!IsSerie && Element.TotalAggregateFunction == AggregateFunction.Count) Value = IsTotalTotal ? Sum : RealCount;

            if (RealCount > 0)
            {
                AggregateFunction aggregatFunction = IsSerie ? Element.AggregateFunction : Element.TotalAggregateFunction;
                if (IsSerie && aggregatFunction == AggregateFunction.Count) Value = Sum; //Count aggregat for a serie -> we make the sum
                else if (IsTotalTotal && Element.AggregateFunction == AggregateFunction.Count && Element.TotalAggregateFunction == AggregateFunction.Sum) Value = Sum; //Count aggregat for totals -> we use the sum
                else if (Sum != null && Element.IsNumeric && aggregatFunction == AggregateFunction.Sum) Value = Sum;
                else if (Min != null && Element.IsNumeric && aggregatFunction == AggregateFunction.Min) Value = Min;
                else if (Max != null && Element.IsNumeric && aggregatFunction == AggregateFunction.Max) Value = Max;
                else if (Sum != null && Element.IsNumeric && aggregatFunction == AggregateFunction.Avg) Value = Sum / RealCount;
                else if (DateMin != null && Element.IsDateTime && aggregatFunction == AggregateFunction.Min) Value = DateMin;
                else if (DateMax != null && Element.IsDateTime && aggregatFunction == AggregateFunction.Max) Value = DateMax;
                else if (DateSum != null && Element.IsDateTime && aggregatFunction == AggregateFunction.Avg) Value = new DateTime((long)(DateSum.Value * kTickDivider / RealCount));
            }

            _done = true;
        }

        public void ProcessContext()
        {
            //set the common row or col of the cells part of the total
            //may be used in the cell script for Series values
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
