using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SealWebServer.Models.Configuration;

namespace SealWebServer
{
    public partial class Startup
    {
        private void ConfigureSessionServices(IServiceCollection services, SessionConfiguration sessionConfiguration)
        {
            //The Seal configuration section may be missing on a fresh install: use defaults instead of crashing.
            if (sessionConfiguration == null) sessionConfiguration = new SessionConfiguration();
            if (sessionConfiguration.SessionTimeout <= 0) sessionConfiguration.SessionTimeout = SessionTimeout;

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
                // Scope the session cookie to the application path: the framework default is "/", so two instances
                // published as sub-applications of the same web site (e.g. /LEG and /REP) would share and overwrite
                // the same '.AspNetCore.Session' cookie, which makes them unusable simultaneously in one browser.
                options.Cookie.Path = GetSessionCookiePath(sessionConfiguration);
                // Also make the cookie name specific to the path: a stale site-wide cookie (written by an older version or
                // by an instance published at the site root) is then simply ignored instead of resetting the session on each request.
                options.Cookie.Name = GetSessionCookieName(options.Cookie.Path);
            });
        }

        /// <summary>
        /// Name of the session cookie: the default '.AspNetCore.Session' at the root, else suffixed with the sanitized path (e.g. '.AspNetCore.Session.REP')
        /// </summary>
        static string GetSessionCookieName(string path)
        {
            const string defaultName = ".AspNetCore.Session";
            if (string.IsNullOrEmpty(path) || path == "/") return defaultName;
            var suffix = new string(path.Trim('/').Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '.').ToArray());
            return defaultName + "." + suffix;
        }

        /// <summary>
        /// Path of the session cookie: the 'SessionCookiePath' setting if set, else the reverse proxy path base,
        /// else the IIS application path published by the ASP.NET Core Module (ASPNETCORE_APPL_PATH), else "/".
        /// </summary>
        static string GetSessionCookiePath(SessionConfiguration sessionConfiguration)
        {
            var path = sessionConfiguration.SessionCookiePath;
            if (string.IsNullOrWhiteSpace(path)) path = PathBaseProxy;
            if (string.IsNullOrWhiteSpace(path)) path = System.Environment.GetEnvironmentVariable("ASPNETCORE_APPL_PATH");
            if (string.IsNullOrWhiteSpace(path)) return "/";
            path = path.Trim();
            if (!path.StartsWith("/")) path = "/" + path;
            return path.Length > 1 ? path.TrimEnd('/') : path;
        }
    }
}