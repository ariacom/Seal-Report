using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SealSchedulerService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //Encoding registration
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#if RELEASE
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new SchedulerService()
            };
            ServiceBase.Run(ServicesToRun);
#else
            try
            {
                SealReportScheduler.Instance.Run();
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Error, ex.Message);
            }
#endif
        }
    }
}
