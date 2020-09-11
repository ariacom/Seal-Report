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
        /// <summary>
        /// Attach CSS file
        /// </summary>
        public string AttachCSSFile(string fileName)
        {
            if (!Exporting) return string.Format("<link rel='stylesheet' href='{0}/Content/{1}'/>", BaseURL, fileName);
            string result = "<style type='text/css'>\r\n";
            result += Repository.Configuration.GetAttachedFileContent(Path.Combine(Path.Combine(ServerPath, "Content"), fileName));
            result += "\r\n</style>\r\n";
            return result;
        }

        /// <summary>
        /// Attach Script file
        /// </summary>
        public string AttachScriptFile(string fileName)
        {
            if (!Exporting) return string.Format("<script src='{0}/Scripts/{1}'></script>", BaseURL, fileName);
            string result = "<script type='text/javascript'>\r\n";
            result += Repository.Configuration.GetAttachedFileContent(Path.Combine(Path.Combine(ServerPath, "Scripts"), fileName));
            result += "\r\n</script>\r\n";
            return result;
        }

        /// <summary>
        /// Attach Image file
        /// </summary>
        public string AttachImageFile(string fileName)
        {
            if (!Exporting) return string.Format("{0}/Images/{1}", BaseURL, fileName);
            return Helper.HtmlMakeImageSrcData(Path.Combine(Path.Combine(ServerPath, "Images"), fileName));
        }
    }
}