using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using Seal.Model;
using SealWebServer.Controllers;

namespace SealWebServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
            //Set repository path
            Repository.RepositoryConfigurationPath = Configuration.GetValue<string>("SealConfiguration:RepositoryPath");
            DebugMode = Configuration.GetValue<Boolean>("SealConfiguration:DebugMode", false);
            SessionTimeout = Configuration.GetValue<int>("SealConfiguration:SessionTimeout", 60);

            WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, "Starting Web Report Server");

            if (Repository.Instance.Configuration.UseWebScheduler)
            {
                //Run scheduler
                var schedulerThread = new Thread(RunScheduler);
                schedulerThread.Start();
            }
        }


        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public static int SessionTimeout = 60;
        public static bool DebugMode = false;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(SessionTimeout);
                options.Cookie.HttpOnly = true;
                // Make the session cookie essential
                options.Cookie.IsEssential = true;
            });

            services
                .AddControllersWithViews()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver(); //Force PascalCase
                });

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{action=Main}",
                    new { controller = "Home", action = "Main" });
            });

            applicationLifetime.ApplicationStopping.Register(OnShutdown);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                applicationLifetime.StopApplication();
                // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                eventArgs.Cancel = true;
            };
        }

        private void OnShutdown()
        {
            SealReportScheduler.Instance.Shutdown();
        }

        private void RunScheduler()
        {
            try
            {
                SealReportScheduler.Instance.Run();
            }
            catch (Exception ex)
            {
                WebHelper.WriteLogEntryWeb(EventLogEntryType.Error, ex.Message);
            }
        }
    }
}
