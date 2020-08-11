using System.Diagnostics.CodeAnalysis;

namespace SealWebServer.Models.Configuration
{
    [ExcludeFromCodeCoverage]
    public class SessionSqlServerConfiguration
    {
        public string ConnectionString { get; set; }

        public string SchemaName { get; set; }

        public string TableName { get; set; }
    }
}