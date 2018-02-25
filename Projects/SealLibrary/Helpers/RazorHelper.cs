using System;
using System.Data.OleDb;
using System.Web;
using RazorEngine;
using RazorEngine.Templating;
using System.Data;
using System.DirectoryServices.Protocols;
using System.Xml.Linq;
using System.ServiceModel.Syndication;
using System.Windows.Forms;

namespace Seal.Helpers
{
    public class RazorHelper
    {

        static HtmlString dummy = null;
        static DataTable dummy2 = null;
        static OleDbConnection dummy3 = null;
        static LdapConnection dummy4 = null;
        static SyndicationFeed dummy5 = null;
        static XDocument dummy6 = null;
        static Control dummy7 = null;

        static bool _loadDone = false;
        static public void LoadRazorAssemblies()
        {
            if (!_loadDone)
            {
                //Force the load of the assemblies
                if (dummy == null) dummy = new HtmlString("");
                if (dummy2 == null) dummy2 = new DataTable();
                if (dummy3 == null) dummy3 = new OleDbConnection();
                if (dummy4 == null) dummy4 = new LdapConnection("");
                if (dummy5 == null) dummy5 = new SyndicationFeed();
                if (dummy6 == null) dummy6 = new XDocument();
                if (dummy7 == null) dummy7 = new Control(); 
                _loadDone = true;
            }
        }


        static public string CompileExecute(string script, object model, string key = null)
        {
            if (model != null && script != null && script.StartsWith("@"))
            {
                LoadRazorAssemblies();
                if (string.IsNullOrEmpty(key))
                {
                    key = (model != null ? model.GetType().ToString() : "_") + "_" + script;
                }
                if (Engine.Razor.IsTemplateCached(key, model.GetType()))
                {
                    return Engine.Razor.Run(key, model.GetType(), model).Trim();
                }
                return Engine.Razor.RunCompile(script, key, model.GetType(), model).Trim();
            }
            return script;
        }

        static public void Compile(string script, Type modelType, string key)
        {
            if (!string.IsNullOrEmpty(script) && !Engine.Razor.IsTemplateCached(key, modelType))
            {
                LoadRazorAssemblies();
                Engine.Razor.Compile(script, key, modelType);
            }
        }
    }
}
