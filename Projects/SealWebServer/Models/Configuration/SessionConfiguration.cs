using System.Diagnostics.CodeAnalysis;

namespace SealWebServer.Models.Configuration
{
    [ExcludeFromCodeCoverage]
    public class SessionConfiguration
    {
        public int SessionTimeout { get; set; }

        /// <summary>
        /// Optional path of the session cookie. If not set, the IIS application path is used (or "/" when not hosted in a sub-application).
        /// </summary>
        public string SessionCookiePath { get; set; }

        public SessionProvider SessionProvider { get; set; }
    }
}