using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seal.Model
{
    public class EditorColumnDefinition
    {
        public string Name = "";
        public bool IsHidden = false;
        public string FkChildViewName = "";
    }

    public class EditorTableDefinition
    {
        public string PkName = "";
        public string PkDisplayName = "";
        public string TableDisplayName = "";
        public string SPDelete = "";

        public List<EditorColumnDefinition> Cols = new List<EditorColumnDefinition>();
        public Dictionary<string, Tuple<string, string>> Navs = new Dictionary<string, Tuple<string, string>>();
    }
}
