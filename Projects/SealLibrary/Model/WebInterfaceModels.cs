using Microsoft.Win32.TaskScheduler;
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace Seal.Model
{
    public enum SWIReportFormat
    {
        HTML,
        HTMLPrint,
        CSV,
        Excel,
        PDF,
        PDFPrint
    }

    public enum SWIScheduleType
    {
        None,
        Email,
        Folder,
    }


    public class SWIUserProfile
    {
        public string name;
        public string group;
        public string culture;
        public string folder;
    }

    public class SWIFolder
    {
        private static string PersonalPrefix = ":";

        public string path;
        public string name;
        public string fullname;
        public int right = 0;
        public bool expand = true;
        public int manage = 0; //0 do not manage, 1 manage sub-folders only, 2 manage all :create, delete and rename
        public SWIFolder[] folders = null;

        public void SetManageFlag(bool useSubFolders, bool manageFolder, bool isDefined)
        {
            if (!useSubFolders) manage = 0;
            //the folder in defined in the security = no rename or delete
            else manage = !manageFolder ? 0 : (isDefined ? 1 : 2);
        }

        public bool IsPersonal
        {
            get { return path.StartsWith(PersonalPrefix); }
        }

        public string Path
        {
            get { return path.StartsWith(PersonalPrefix) ? path.Substring(1) : path; }
        }

        //Helpers
        public static string GetPersonalRoot()
        {
            return PersonalPrefix;
        }

        public static string GetParentPath(string path)
        {
            return path.StartsWith(PersonalPrefix) ? (PersonalPrefix + System.IO.Path.GetDirectoryName(path.Substring(1))) : System.IO.Path.GetDirectoryName(path);
        }

        public string Combine(string newName)
        {
            return path + (path.EndsWith("\\") ? "" : "\\") + System.IO.Path.GetFileName(newName);
        }

        //Physical path
        private string _fullPath;
        public void SetFullPath(string fullPath)
        {
            _fullPath = fullPath;
        }
        public string GetFullPath()
        {
            return _fullPath;
        }
    }

    public class SWIFile
    {
        public string path;
        public string name;
        public string last;
        public bool isReport;
        public int right;
    }

    public class SWIFolderDetail
    {
        public SWIFolder folder = null;
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

    public class SWIItem
    {
        public string id;
        public string val;
    }

}