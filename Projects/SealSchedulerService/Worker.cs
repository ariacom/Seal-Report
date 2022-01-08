using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace SealSchedulerService
{
    public class Worker : BackgroundService
    {
        public Worker(IConfiguration configuration)
        {
            //Encoding registration
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //Set repository path
            Repository.RepositoryConfigurationPath = configuration.GetValue<string>($"{Repository.SealConfigurationSectionKeyword}:{Repository.SealConfigurationRepositoryPathKeyword}");
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

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            //Run scheduler
            StartScheduler();
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            SealReportScheduler.Instance.Shutdown();
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void Dispose()
        {
        }
    }
}