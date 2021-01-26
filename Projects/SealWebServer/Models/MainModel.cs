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
    }
}