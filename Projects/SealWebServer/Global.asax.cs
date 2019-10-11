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

namespace SealWebServer
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        public static bool DebugMode = false;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Starting Web Report Server");

            DebugMode = (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["DebugMode"]) && ConfigurationManager.AppSettings["DebugMode"].ToLower() == "true");

            var preload = ConfigurationManager.AppSettings["PreLoad"];
            if (preload == null || preload.ToLower() == "true")
            {
                //Preload templates and dashboard widgets
                var preloadThread = new Thread(PreLoadThread);
                preloadThread.Start();
            }
        }

        private void PreLoadThread()
        {
            try
            {
                Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Starting Preload Templates");
                RepositoryServer.PreLoadTemplates();

                Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Starting Preload Widgets");
                var widgets = DashboardWidgetsPool.Widgets;

                Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Ending Preload");
            }
            catch (Exception ex)
            {
                Helper.WriteLogEntryWeb(EventLogEntryType.Error, ex.Message);
            }
        }

        protected void Application_End()
        {
            Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Ending Web Report Server");
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
                Helper.WriteLogEntryWeb(EventLogEntryType.Information, null, user, "Ending Web Session '{0}' for user '{1}'", Session.SessionID, user.Name);
                user.Logout();
            }
            else
            {
                Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Ending Web Session '{0}'", Session.SessionID);
            }
        }
    }
}