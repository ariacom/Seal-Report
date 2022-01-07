using Microsoft.Extensions.Configuration;
using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SealTaskScheduler
{
    internal class Worker
    {
        private readonly IConfiguration _configuration;

        public Worker(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public void DoWork(string[] args)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                //Set repository path
                var section = _configuration.GetSection(Repository.SealConfigurationSectionKeyword);
                if (section != null) Repository.RepositoryConfigurationPath = _configuration.GetSection(Repository.SealConfigurationSectionKeyword)[Repository.SealConfigurationRepositoryPathKeyword];

                ReportExecution.ExecuteReportSchedule(args[0].ToString());
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntryScheduler(EventLogEntryType.Error, ex.Message);
            }
        }
    }
}
