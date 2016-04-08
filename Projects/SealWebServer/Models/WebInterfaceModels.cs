using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SealWebServer.Models
{
    public class SWIUser
    {
        public string name;
        public string group;
        public string culture;
    }

    public class SWIFolder
    {
        public string folderPath;
        public string displayName;
        public SWIFolder[] folders = null;
    }

    public class SWIFile
    {
        public string filePath;
        public string displayName;
        public bool isReport;
        public bool canExecuteOutput;
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

    public class SWIReport
    {
        public SWIView[] views;
        public SWIOutput[] outputs;
    }
}