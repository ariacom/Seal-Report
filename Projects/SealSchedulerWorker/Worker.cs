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
            Repository.RepositoryConfigurationPath = configuration.GetValue<string>($"{Repository.SealConfigurationSectionKeyword}:{Repository.SealConfigurationRepositoryPathKeyword}");
        }

        private void StartScheduler()
        {
            try
            {
                if (Repository.Instance.Configuration.SchedulerMode != SchedulerMode.Service)
                {
                    throw new Exception("The current Server Configuration 'Scheduler Mode' is not set to 'Service or Worker'. This Scheduler will not run any report. Please check your configuration.");
                }
                SealReportScheduler.Instance.Run();
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Error, ex.Message);
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Thread thread = new Thread(StartScheduler);
            thread.Start();

            while (!stoppingToken.IsCancellationRequested)
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
