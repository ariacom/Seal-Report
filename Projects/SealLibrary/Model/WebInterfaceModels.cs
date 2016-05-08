using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Seal.Model
{
    public class SWIUser
    {
        public string name;
        public string group;
        public string culture;
    }

    public class SWIFolder
    {
        public string path;
        public string name;
        public SWIFolder[] folders = null;
    }

    public class SWIFile
    {
        public string path;
        public string name;
        public bool isReport;
        public bool execOutput;
    }

    public class SWIFolderDetail
    {
        public SWIFile[] files = null;
    }

    public class SWIView
    {
        public string guid;
        public string name;
        public string displayName;
    }

    public class SWIOutput
    {
        public string guid;
        public string name;
        public string displayName;
    }

    public class SWIReportDetail
    {
        public SWIView[] views;
        public SWIOutput[] outputs;
    }
}