//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Model;
using System;

namespace Test
{
    /// <summary>
    /// Calculation of the next execution of the schedules of the Seal Report Scheduler
    /// </summary>
    [TestClass]
    public class TestSealSchedule
    {
        /// <summary>
        /// First date at 08:00 after now matching the condition
        /// </summary>
        static DateTime nextAt8(Func<DateTime, bool> condition)
        {
            var result = DateTime.Today.AddHours(8);
            while (result <= DateTime.Now || !condition(result)) result = result.AddDays(1);
            return result;
        }

        [TestMethod]
        public void WeeklyAllWeekdaysAreFound()
        {
            //Start 3 weeks ago, each weekday must be found, including the day before the weekday of the start
            var start = DateTime.Today.AddDays(-21).AddHours(8);
            for (int day = 0; day < 7; day++)
            {
                var schedule = new SealSchedule() { Type = TriggerType.Weekly, Start = start, Weekdays = new int[] { day } };
                schedule.CalculateNextExecution();
                Assert.AreEqual(nextAt8(d => (int)d.DayOfWeek == day), schedule.NextExecution, $"Weekday {day}");
            }
        }

        [TestMethod]
        public void WeeklyStartInFutureOnNotSelectedDay()
        {
            //The start day is not selected: the next execution is the selected day after the start, never the start day itself
            var start = DateTime.Today.AddDays(10).AddHours(8);
            var day = ((int)start.DayOfWeek + 6) % 7;
            var schedule = new SealSchedule() { Type = TriggerType.Weekly, Start = start, Weekdays = new int[] { day } };
            schedule.CalculateNextExecution();
            Assert.AreEqual(start.AddDays(6), schedule.NextExecution);
        }

        [TestMethod]
        public void WeeklyWithoutWeekday()
        {
            var schedule = new SealSchedule() { Type = TriggerType.Weekly, Start = DateTime.Today.AddDays(2), Weekdays = new int[] { } };
            schedule.CalculateNextExecution();
            Assert.AreEqual(DateTime.MaxValue, schedule.NextExecution);
        }

        [TestMethod]
        public void MonthlyLastDay()
        {
            var schedule = new SealSchedule() { Type = TriggerType.Monthly, Start = DateTime.Today.AddDays(-40).AddHours(8), Days = new int[] { 32 } };
            schedule.CalculateNextExecution();
            Assert.AreEqual(nextAt8(d => d.Day == DateTime.DaysInMonth(d.Year, d.Month)), schedule.NextExecution);
        }

        [TestMethod]
        public void MonthlyImpossibleDate()
        {
            //Day 30 of February does not exist: no execution and no exception
            var schedule = new SealSchedule() { Type = TriggerType.Monthly, Start = DateTime.Today.AddDays(-40).AddHours(8), Days = new int[] { 30 }, Months = new int[] { 2 } };
            schedule.CalculateNextExecution();
            Assert.AreEqual(DateTime.MaxValue, schedule.NextExecution);
        }

        [TestMethod]
        public void MonthlyFebruary29()
        {
            var schedule = new SealSchedule() { Type = TriggerType.Monthly, Start = DateTime.Today.AddDays(-40).AddHours(8), Days = new int[] { 29 }, Months = new int[] { 2 } };
            schedule.CalculateNextExecution();
            Assert.AreEqual(nextAt8(d => d.Month == 2 && d.Day == 29), schedule.NextExecution);
        }
    }
}
