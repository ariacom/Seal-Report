using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using Seal.Model;
using SealWebServer.Controllers;
using SealWebServer.Models.Configuration;

namespace SealWebServer
{
    public partial class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //Set repository path
            Repository.RepositoryConfigurationPath = Configuration.GetValue<string>($"{Repository.SealConfigurationSectionKeyword}:{Repository.SealConfigurationRepositoryPathKeyword}");
            DebugMode = Configuration.GetValue<Boolean>($"{Repository.SealConfigurationSectionKeyword}:DebugMode", false);
            RunScheduler = Configuration.GetValue<Boolean>($"{Repository.SealConfigurationSectionKeyword}:RunScheduler", false);
            SessionTimeout = Configuration.GetValue<int>($"{Repository.SealConfigurationSectionKeyword}:SessionTimeout", 60);
            PathBaseProxy = Configuration.GetValue<string>($"{Repository.SealConfigurationSectionKeyword}:PathBaseProxy", null);
            
            WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, "Starting Web Report Server");
            Audit.LogEventAudit(AuditType.EventServer, "Starting Web Report Server");
            Audit.LogEventAudit(AuditType.EventLoggedUsers, "0");

            if (RunScheduler && Repository.Instance.Configuration.UseSealScheduler)
            {
                WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, "Starting Scheduler from the Web Report Server");
                //Run scheduler
                Task.Run(() => StartScheduler());
            }
        }


        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public static int SessionTimeout = 60;
        public static bool DebugMode = false;
        public static bool RunScheduler = false;
        public static string PathBaseProxy = null;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureSessionServices(services, Configuration.GetSection(Repository.SealConfigurationSectionKeyword).Get<SessionConfiguration>());
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Cookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("Cookies")
                .AddOpenIdConnect("oidc", options =>
                {
                    options.SignInScheme = "Cookies";
                    options.Authority = Configuration["Authentication:Id4EndPoint"];//授权服务中心
                    //令牌保存标识
                    options.SaveTokens = true;
                    //options.CallbackPath = PathString.FromUriComponent("/Home/Main");
                    options.RequireHttpsMetadata = false;
                    options.ClientId = Configuration["Authentication:ClientId"];//授权服务分配的ClientId
                    options.ClientSecret = Configuration["Authentication:AccessKeySecret"];
                    options.ResponseType = "code";
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                });

            services.Configure<Authentication>(Configuration.GetSection("Authentication"));
            services
                .AddControllersWithViews()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver(); //Force PascalCase
                });
            services.AddCors(option =>
                option.AddPolicy("cors", policy => policy
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                ));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {
            if (!string.IsNullOrEmpty(PathBaseProxy))
            {
                app.Use((context, next) =>
                {
                    context.Request.PathBase = new PathString(PathBaseProxy);
                    return next();
                });
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.All
                });
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();
            app.UseCors("cors");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{action=Main}",
                    new { controller = "Home", action = "Main" });

                endpoints.MapControllerRoute(
                    name: "mvc",
                    pattern: "{controller=Home}/{action=Main}");
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
            if (RunScheduler && Repository.Instance.Configuration.UseSealScheduler)
            {
                SealReportScheduler.Instance.Shutdown();
            }
            WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, "Ending Web Report Server");
            Audit.LogEventAudit(AuditType.EventServer, "Ending Web Report Server");
            Audit.LogEventAudit(AuditType.EventLoggedUsers, "0");

        }

        private void StartScheduler()
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
