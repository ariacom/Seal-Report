//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A SecurityFolder defines the security applied to a published folder
    /// </summary>
    public class SecurityFolder : RootEditor
    {

        /// <summary>
        /// The folder path containing the reports to publish. The path is relative to the repository 'Reports' folder and should be unique in the security group.
        /// </summary>
        public string Path { get; set; } = System.IO.Path.DirectorySeparatorChar.ToString();

        bool _useSubFolders = true;
        /// <summary>
        /// If true, sub-folders are also published with the same definition
        /// </summary>
        public bool UseSubFolders
        {
            get { return _useSubFolders; }
            set {
                _useSubFolders = value;
                
            }
        }

        /// <summary>
        /// If true, the user can Create, Rename or Delete sub-folders in this folder. This flag is only used if Sub-folders are shown.
        /// </summary>
        public bool ManageFolder { get; set; } = true;

        FolderRight _folderRight = FolderRight.Edit;
        /// <summary>
        /// The right applied on the reports and files of the folder
        /// </summary>
        public FolderRight FolderRight
        {
            get { return _folderRight; }
            set
            {
                _folderRight = value;
                
            }
        }

        /// <summary>
        /// If true, all the Sub-folders displayed in the Tree View are expanded by default
        /// </summary>
        public bool ExpandSubFolders { get; set; } = true;

        /// <summary>
        /// If true, only files can be viewed or managed in the folder (reports are not shown and can not be created)
        /// </summary>
        public bool FilesOnly { get; set; } = false;

        /// <summary>
        /// Helper: is true if defined in a group 
        /// </summary>
        [XmlIgnore]
        public bool IsDefined = false;
    }
}

