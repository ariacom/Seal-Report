using Seal.Helpers;
using System.IO;

namespace Seal.Model
{
    public class MainModel
    {
        public Repository Repository;

        public bool HasEditor()
        {
#if EDITOR
            return true;
#else
            return false;
#endif
        }

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
        /// Format used for dashboard export
        /// </summary>
        public string Format = "";

        /// <summary>
        /// List of dashboard ids to export
        /// </summary>
        public string DashboardIds = "";

        /// <summary>
        /// User defined object
        /// </summary>
        public object Tag;

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