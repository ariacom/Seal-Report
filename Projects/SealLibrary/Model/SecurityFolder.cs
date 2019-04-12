//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using DynamicTypeDescriptor;
using Seal.Converter;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    public class SecurityFolder : RootEditor
    {
        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Path").SetIsBrowsable(true);
                GetProperty("UseSubFolders").SetIsBrowsable(true);
                GetProperty("FolderRight").SetIsBrowsable(true);
                GetProperty("ExpandSubFolders").SetIsBrowsable(true);
                GetProperty("ManageFolder").SetIsBrowsable(true);
                GetProperty("FilesOnly").SetIsBrowsable(true);


                GetProperty("ManageFolder").SetIsReadOnly(!_useSubFolders);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        string _path = "\\";
        [Category("Definition"), DisplayName("\tPath"), Description("The folder path containing the reports to publish. The path is relative to the repository 'Reports' folder and should be unique in the security group."), Id(1,1)]
        [TypeConverter(typeof(RepositoryFolderConverter))]
        [DefaultValue("\\")]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        bool _useSubFolders = true;
        [Category("Definition"), DisplayName("\tShow sub-folders"), Description("If true, sub-folders are also published with the same definition."), Id(2, 1)]
        [DefaultValue(true)]
        public bool UseSubFolders
        {
            get { return _useSubFolders; }
            set {
                _useSubFolders = value;
                UpdateEditorAttributes();
            }
        }

        bool _manageFolder = true;
        [Category("Definition"), DisplayName("Manage sub-folder"), Description("If true, the user can Create, Rename or Delete sub-folders in this folder. This flag is only used if Sub-folders are shown."), Id(3, 1)]
        [DefaultValue(true)]
        public bool ManageFolder
        {
            get { return _manageFolder; }
            set { _manageFolder = value; }
        }

        FolderRight _folderRight = FolderRight.Edit;
        [Category("Definition"), DisplayName("\tRight"), Description("The right applied on the reports and files of the folder"), Id(4, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(FolderRight.Edit)]
        public FolderRight FolderRight
        {
            get { return _folderRight; }
            set
            {
                _folderRight = value;
                UpdateEditorAttributes();
            }
        }

        bool _expandSubFolders = true;
        [Category("Options"), DisplayName("Expand Tree View Sub-folders"), Description("If true, all the Sub-folders displayed in the Tree View are expanded by default."), Id(2, 2)]
        [DefaultValue(true)]
        public bool ExpandSubFolders
        {
            get { return _expandSubFolders; }
            set { _expandSubFolders = value; }
        }


        bool _filesOnly = false;
        [Category("Options"), DisplayName("Files only (no reports)"), Description("If true, only files can be viewed or managed in the folder (reports are not shown and can not be created)."), Id(3, 2)]
        [DefaultValue(false)]
        public bool FilesOnly
        {
            get { return _filesOnly; }
            set { _filesOnly = value; }
        }

        //Helper, set to true if defined in a group
        [XmlIgnore]
        public bool IsDefined = false;
    }
}
