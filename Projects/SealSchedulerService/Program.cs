using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text;

namespace SealSchedulerService
{
    static class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            try
            {
                //Configuration
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                SealReportScheduler.SchedulerOuterProcess = true;
                SealReportScheduler.Instance.Run();
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Error, ex.Message);
            }
#else
            CreateHostBuilder(args).Build().Run();
#endif

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });
    }
}
