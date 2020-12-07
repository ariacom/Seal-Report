using Seal.Helpers;
using System.Collections.Generic;
using System.IO;

namespace Seal.Model
{
    /// <summary>
    /// Main model used for the Web Report Server
    /// </summary>
    public class MainModel
    {
        /// <summary>
        /// Current Repository
        /// </summary>
        public Repository Repository;

        /// <summary>
        /// True is the Web Report Designer is used
        /// </summary>
        public bool HasEditor()
        {
#if EDITOR
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// True is JavaScripts are minified
        /// </summary>
        public bool HasMinifiedScripts()
        {
#if MINIFIED
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Root path of the Web Report Server
        /// </summary>
        public string ServerPath = "";

        /// <summary>
        /// Base URL path of the Web Report Server
        /// </summary>
        public string BaseURL = "";

        /// <summary>
        /// Title used for dashboard export
        /// </summary>
        public string Title= "";

        /// <summary>
        /// Format used for dashboard export
        /// </summary>
        public string Format = "";

        /// <summary>
        /// List of dashboard ids to export
        /// </summary>
        public string DashboardIds = "";

        /// <summary>
        /// Current user
        /// </summary>
        public SecurityUser User;

        /// <summary>
        /// Current report executions for dashboards
        /// </summary>
        public List<ReportExecution> DashboardExecutions;

        /// <summary>
        /// Is it for dashboards export
        /// </summary>
        public bool Exporting
        {
            get
            {
                return !string.IsNullOrEmpty(Format);
            }
        }
    }
}