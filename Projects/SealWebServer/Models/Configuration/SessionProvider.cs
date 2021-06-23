using System.Diagnostics.CodeAnalysis;

namespace SealWebServer.Models.Configuration
{
    [ExcludeFromCodeCoverage]
    public class SessionProvider
    {
        public SessionSqlServerConfiguration SqlServer { get; set; }
    }
}