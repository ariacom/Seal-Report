using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Seal.Helpers;
using Seal.Model;

namespace SealSchedulerService
{
    public class Worker : BackgroundService
    {
        public Worker(IConfiguration configuration)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //Set repository path
            Repository.RepositoryConfigurationPath = configuration.GetValue<string>("SealConfiguration:RepositoryPath");

            Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Starting Scheduler from the Scheduler Service");
            //Run scheduler
            Task.Run(() => StartScheduler());
        }

        private void StartScheduler()
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
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (SealReportScheduler.Running)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            SealReportScheduler.Instance.Shutdown();
            await Task.CompletedTask;
        }
    }
}
