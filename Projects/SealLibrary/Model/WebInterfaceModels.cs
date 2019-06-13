//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
namespace Seal.Model
{
    public class SWIUserProfile
    {
        public string name;
        public string group;
        public string culture;
        public string folder;
        public string dashboard;
        public string lastview;
        public ViewType viewtype;
        public SWIDashboardFolder[] dashboardFolders;
        public bool manageDashboards = false;
    }

    public class SWIFolder
    {
        private static string PersonalPrefix = ":";

        public string path;
        public string name;
        public string fullname;
        public int right = 0;
        public bool sql = false; //can edit sql model
        public bool expand = true;
        public bool files = false; //true = files only = no reports
        public int manage = 0; //0 do not manage, 1 manage sub-folders only, 2 manage all :create, delete and rename
        public SWIFolder[] folders = null;

        public void SetManageFlag(bool useSubFolders, bool manageFolder, bool isDefined)
        {
            if (!useSubFolders) manage = 0;
            //the folder is defined in the security = no rename or delete
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

    public class SWIDashboardFolder
    {
        public static string PersonalPath = ":";

        public string name;
        public string path;
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