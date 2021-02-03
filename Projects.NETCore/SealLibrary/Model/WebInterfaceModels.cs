//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.IO;

namespace Seal.Model
{
    /// <summary>
    /// Class used for the Seal Web Interface: Communication from the Browser to the Web Report Server
    /// </summary>
    public class SWIUserProfile
    {
        public string name;
        public string group;
        public string culture;
        public string folder;
        public bool showfolders;
        public bool editprofile;
        public string usertag;
        public StartupOptions onstartup;
        public string startupreport;
        public string startupreportname;
        public string report;
        public string reportname;
    }

    /// <summary>
    /// Class used for the Seal Web Interface: Communication from the Browser to the Web Report Server
    /// </summary>
    public class SWIFolder
    {
        private static string PersonalPrefix = ":";

        /// <summary>
        /// Relative folder path including name
        /// </summary>
        public string path;

        /// <summary>
        /// Folder name
        /// </summary>
        public string name;

        /// <summary>
        /// Full folder path including name
        /// </summary>
        public string fullname;

        /// <summary>
        /// Right applied on the folder files/reports:
        /// 0: No right
        /// 1: Execute reports / View files
        /// 2: Execute reports and outputs / View files
        /// 3: Edit schedules / View files
        /// 4: Edit reports / Manage files
        /// </summary>
        public int right = 0;

        /// <summary>
        /// True if Sql Model and Sql can be edited
        /// </summary>
        public bool sql = false;

        /// <summary>
        /// If true, the folder is expanded after the login
        /// </summary>
        public bool expand = true;

        /// <summary>
        /// If true, only files can be displayed in this folder
        /// </summary>
        public bool files = false;

        /// <summary>
        /// Sub-folders management:
        /// 0 do not manage sub-folders
        /// 1 manage sub-folders only as they are defined by the security (no rename or delete allowed)
        /// 2 manage all: create, delete and rename sub-folders
        /// </summary>
        public int manage = 0;

        /// <summary>
        /// List of folder children.
        /// </summary>
        public List<SWIFolder> folders = new List<SWIFolder>();

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

        public string FinalPath
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
            string result;
            if (path.StartsWith(PersonalPrefix))
            {

                result = path.Length > 1 ? PersonalPrefix + Path.GetDirectoryName(path.Substring(1)) : PersonalPrefix;
                if (result == PersonalPrefix + Path.DirectorySeparatorChar.ToString()) result = PersonalPrefix;
            }
            else
            {
                result = Path.GetDirectoryName(path);
            }
            return result;
        }

        public string Combine(string newName)
        {
            return path + (path.EndsWith(Path.DirectorySeparatorChar.ToString()) ? "" : Path.DirectorySeparatorChar.ToString()) + System.IO.Path.GetFileName(newName);
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


    /// <summary>
    /// Class used for the Seal Web Interface: Communication from the Browser to the Web Report Server
    /// </summary>
    public class SWIFile
    {
        /// <summary>
        /// File path
        /// </summary>
        public string path;

        /// <summary>
        /// File name
        /// </summary>
        public string name;

        /// <summary>
        /// Last modification
        /// </summary>
        public string last;

        /// <summary>
        /// True if the file is a report
        /// </summary>
        public bool isreport;

        /// <summary>
        /// Right applied on the file/report:
        /// 0: No right
        /// 1: Execute reports / View files
        /// 2: Execute reports and outputs / View files
        /// 3: Edit schedules / View files
        /// 4: Edit reports / Manage files
        /// </summary>
        public int right;
    }

    /// <summary>
    /// Class used for the Seal Web Interface: Communication from the Browser to the Web Report Server
    /// </summary>
    public class SWIFolderDetail
    {
        public SWIFolder folder = null;
        public List<SWIFile> files = new List<SWIFile>();
    }

    /// <summary>
    /// Class used for the Seal Web Interface: Communication from the Browser to the Web Report Server
    /// </summary>
    public class SWIView
    {
        public string guid;
        public string name;
        public string displayname;
    }

    /// <summary>
    /// Class used for the Seal Web Interface: Communication from the Browser to the Web Report Server
    /// </summary>
    public class SWIOutput
    {
        public string guid;
        public string name;
        public string displayname;
    }

    /// <summary>
    /// Class used for the Seal Web Interface: Communication from the Browser to the Web Report Server
    /// </summary>
    public class SWIReportDetail
    {
        public List<SWIView> views = new List<SWIView>();
        public List<SWIOutput> outputs = new List<SWIOutput>();
    }

    /// <summary>
    /// Class used for the Seal Web Interface: Communication from the Browser to the Web Report Server
    /// </summary>
    public class SWIItem
    {
        public string id;
        public string val;
    }

    /// <summary>
    /// Class used for the Seal Web Interface: Communication from the Browser to the Web Report Server
    /// </summary>
    public class SWIWebMenu
    {
        public List<SWIMenuItem> recentreports = new List<SWIMenuItem>();
        public List<SWIMenuItem> reports = new List<SWIMenuItem>();
    }

    /// <summary>
    /// Class used for the Seal Web Interface: Communication from the Browser to the Web Report Server
    /// </summary>
    public class SWIMenuItem
    {
        public string path;
        public string viewGUID;
        public string outputGUID;
        public string name;
        public List<SWIMenuItem> items = new List<SWIMenuItem>();
    }
}
