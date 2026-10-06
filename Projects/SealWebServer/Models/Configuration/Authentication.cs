namespace SealWebServer.Models.Configuration
{
    public class Authentication
    {
        public string Id4EndPoint { get; set; }
        public string AccessKeySecret { get; set; }
        public string ClientId { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Whether to enable the configuration
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// If true, only this authentication is enabled
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// Returns True if only this authentication is proposed
        /// </summary>
        /// <returns></returns>
        public bool IsUnique()
        {
            return Enabled && Unique;
        }

        /// <summary>
        /// Kept for compatibility: the option is now 'Hide login user name and password' in the Security configuration (Server Manager). If true, the Login Password fields are hidden.
        /// </summary>
        public bool HideLoginPassword { get; set; } = false;
    }
}