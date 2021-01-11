using System.Diagnostics;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Seal.Helpers;
using Seal.Model;
using SealWebServer.Controllers;
using System.Configuration;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SealWebServer
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static bool DebugMode = false;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, "Starting Web Report Server");
            Audit.LogEventAudit(AuditType.EventServer, "Starting Web Report Server");
            Audit.LogEventAudit(AuditType.EventLoggedUsers, "0");

            DebugMode = (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["DebugMode"]) && ConfigurationManager.AppSettings["DebugMode"].ToLower() == "true");

            var runScheduler = ConfigurationManager.AppSettings["RunScheduler"];
            if (runScheduler != null && runScheduler.ToLower() == "true" && Repository.Instance.Configuration.UseSealScheduler)
            {
                WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, "Starting Scheduler from the Web Report Server");
                //Run scheduler
                Task.Run(() => StartScheduler());
            }
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


        protected void Application_End()
        {
            SealReportScheduler.Instance.Shutdown();
            WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, "Ending Web Report Server");
            Audit.LogEventAudit(AuditType.EventServer, "Ending Web Report Server");
            Audit.LogEventAudit(AuditType.EventLoggedUsers, "0");
        }

        protected void Session_Start()
        {
        }

        protected void Session_End()
        {
            SecurityUser user = null;
            if (Session[HomeController.SessionUser] != null)
            {
                user = (SecurityUser)Session[HomeController.SessionUser];
                WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, "Ending Web Session '{0}' for user '{1}'", Session.SessionID, user.Name);
                user.Logout();
            }
            else
            {
                WebHelper.WriteLogEntryWeb(EventLogEntryType.Information, "Ending Web Session '{0}'", Session.SessionID);
            }
        }
    }
}