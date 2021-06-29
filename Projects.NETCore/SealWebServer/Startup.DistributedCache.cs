using System;
using Microsoft.Extensions.DependencyInjection;
using SealWebServer.Models.Configuration;

namespace SealWebServer
{
    public partial class Startup
    {
        private void ConfigureSessionServices(IServiceCollection services, SessionConfiguration sessionConfiguration)
        {
            if(!string.IsNullOrWhiteSpace(sessionConfiguration.SessionProvider?.SqlServer?.ConnectionString))
            {
                services.AddDistributedSqlServerCache(
                    x =>
                    {
                        x.SchemaName = sessionConfiguration.SessionProvider.SqlServer.SchemaName;
                        x.TableName = sessionConfiguration.SessionProvider.SqlServer.TableName;
                        x.ConnectionString = sessionConfiguration.SessionProvider.SqlServer.ConnectionString;
                    });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(sessionConfiguration.SessionTimeout);
                options.Cookie.HttpOnly = true;
                // Make the session cookie essential
                options.Cookie.IsEssential = true;
            });
        }   
    }
}