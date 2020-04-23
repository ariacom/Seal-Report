using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SealSchedulerService
{
    public partial class SchedulerService : ServiceBase
    {
        public SchedulerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Helper.WriteLogEntryScheduler(EventLogEntryType.Information, "Starting Scheduler from the Scheduler Service");
            //Run scheduler
            var schedulerThread = new Thread(StartScheduler);
            schedulerThread.Start();
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

        protected override void OnStop()
        {
            SealReportScheduler.Instance.Shutdown();
        }
    }
}
