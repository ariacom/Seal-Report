using Seal.Helpers;
using Seal.Model;
using System;
using System.Diagnostics;

namespace SealScheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                SealReportScheduler.Instance.Run();
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Error, ex.Message);
            }
        }
    }
}
