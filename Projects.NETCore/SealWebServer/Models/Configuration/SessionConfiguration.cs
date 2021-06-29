using System.Diagnostics.CodeAnalysis;

namespace SealWebServer.Models.Configuration
{
    [ExcludeFromCodeCoverage]
    public class SessionConfiguration
    {
        public int SessionTimeout { get; set; }

        public SessionProvider SessionProvider { get; set; }
    }
}