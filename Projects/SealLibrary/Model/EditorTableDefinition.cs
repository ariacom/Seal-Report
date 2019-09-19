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
        public bool IsReadOnly = false;
        public string ChildViewName = "";
    }

    public class EditorTableDefinition
    {
        public string PkName = "";
        public string PkDisplayName = "";
        public string TableDisplayName = "";

        public string SPInsert = "";
        public string SPUpdate = "";
        public string SPDelete = "";

        public bool CanInsert = true;
        public bool CanUpdate = true;
        public bool CanDelete = true;
        public bool ReadOnlyByDefault = true;

        public List<EditorColumnDefinition> Cols = new List<EditorColumnDefinition>();
        public Dictionary<string, Tuple<string, string>> Navs = new Dictionary<string, Tuple<string, string>>();
    }
}
