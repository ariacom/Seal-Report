using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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

            services.AddHttpContextAccessor();
            services.AddSession();
            services.AddOptions<SessionOptions>().Configure<IHttpContextAccessor>((options, contextAccessor) =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(sessionConfiguration.SessionTimeout);
                // Scope the session cookie to the application: the framework default is a cookie named '.AspNetCore.Session' with
                // path "/", so two instances published as sub-applications of the same web site (e.g. /LEG and /REP) would share
                // and overwrite the same cookie, which makes them unusable simultaneously in one browser.
                var fixedPath = NormalizeCookiePath(sessionConfiguration.SessionCookiePath) ?? NormalizeCookiePath(PathBaseProxy);
                options.Cookie = new SessionCookieBuilder(fixedPath, contextAccessor);
            });
        }

        /// <summary>
        /// Normalized cookie path ("/xxx" without trailing slash), null if empty or root
        /// </summary>
        static string NormalizeCookiePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            path = path.Trim();
            if (!path.StartsWith("/")) path = "/" + path;
            path = path.TrimEnd('/');
            return path.Length > 1 ? path : null;
        }

        /// <summary>
        /// Builder of the session cookie, scoped to the application path: the 'SessionCookiePath' setting (or the reverse proxy
        /// path base) if set, else the path base of the current request (the IIS application path, as typed by the browser).
        /// Browsers match cookie paths case-sensitively whereas IIS paths are not, so a path derived from the configuration only
        /// (e.g. "/REP") would never be sent back when the user browses "/rep/", and the session would be lost on every request.
        /// The cookie name is also suffixed with the lower-case application path (e.g. '.AspNetCore.Session.rep'): a stale site-wide
        /// cookie written by an older version or by an instance published at the site root is then simply ignored instead of
        /// resetting the session on each request.
        /// </summary>
        class SessionCookieBuilder : CookieBuilder
        {
            const string DefaultName = ".AspNetCore.Session";
            readonly string _fixedPath;
            readonly IHttpContextAccessor _contextAccessor;

            public SessionCookieBuilder(string fixedPath, IHttpContextAccessor contextAccessor)
            {
                _fixedPath = fixedPath;
                _contextAccessor = contextAccessor;
                HttpOnly = true;
                // Make the session cookie essential
                IsEssential = true;
                SameSite = SameSiteMode.Lax;
                // Flag the cookie 'Secure' when the request is served over HTTPS: 'Always' would break the installations
                // published over plain HTTP, where the browser would simply drop the cookie and the session would be lost.
                SecurePolicy = CookieSecurePolicy.SameAsRequest;
            }

            /// <summary>
            /// Name of the cookie for the current request
            /// </summary>
            public override string Name
            {
                get
                {
                    var applicationPath = _fixedPath ?? GetRequestPathBase(_contextAccessor.HttpContext);
                    if (string.IsNullOrEmpty(applicationPath) || applicationPath == "/") return DefaultName;
                    var suffix = new string(applicationPath.Trim('/').ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '.').ToArray());
                    return DefaultName + "." + suffix;
                }
                set { }
            }

            public override CookieOptions Build(HttpContext context, DateTimeOffset expiresFrom)
            {
                var result = base.Build(context, expiresFrom);
                result.Path = _fixedPath ?? GetRequestPathBase(context);
                return result;
            }

            /// <summary>
            /// Path base of the request with the casing sent by the browser (the PathBase may carry the configured casing with some hosting models), "/" if none
            /// </summary>
            static string GetRequestPathBase(HttpContext context)
            {
                var pathBase = context?.Request.PathBase ?? PathString.Empty;
                if (!pathBase.HasValue) return "/";
                var raw = context.Features.Get<IHttpRequestFeature>()?.RawTarget;
                if (!string.IsNullOrEmpty(raw) && raw.StartsWith("/") && raw.Length >= pathBase.Value.Length
                    && string.Compare(raw, 0, pathBase.Value, 0, pathBase.Value.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return raw.Substring(0, pathBase.Value.Length);
                }
                return pathBase.Value;
            }
        }
    }
}
