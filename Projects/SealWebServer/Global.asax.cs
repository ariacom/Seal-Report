//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Seal.Helpers;
using Seal.Model;
using SealWebServer.Controllers;

namespace SealWebServer
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        public const string LiveSessionsCount = "LiveSessionsCount";

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            Application[LiveSessionsCount] = 0;

            Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Starting Web Report Server");
        }

        protected void Application_End()
        {
            Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Ending Web Report Server");
        }

        protected void Session_Start()
        {
            Application[LiveSessionsCount] = ((int)Application[LiveSessionsCount]) + 1;
        }

        protected void Session_End()
        {
            Application[LiveSessionsCount] = ((int)Application[LiveSessionsCount]) - 1;
            var securityUserName = "";
            if (Session[HomeController.SessionUser] != null)
            {
                var user = (SecurityUser)Session[HomeController.SessionUser];
                securityUserName = user.Name;
                user.Logout();
            }
            Helper.WriteLogEntryWeb(EventLogEntryType.Information, "Ending Web Session for user '{0}'", securityUserName);
        }
    }
}