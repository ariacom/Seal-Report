//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//using System.ComponentModel;
using System.Xml.Serialization;
#if WINDOWS
using System.ComponentModel;
using DynamicTypeDescriptor;
using Seal.Forms;
#endif


namespace Seal.Model
{
    /// <summary>
    /// A SecurityFolder defines the security applied to a published folder
    /// </summary>
    public class SecurityFolder : RootEditor
    {
#if WINDOWS
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
#endif

        /// <summary>
        /// The folder path containing the reports to publish. The path is relative to the repository 'Reports' folder and should be unique in the security group.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tPath"), Description("The folder path containing the reports to publish. The path is relative to the repository 'Reports' folder and should be unique in the security group."), Id(1, 1)]
        [TypeConverter(typeof(RepositoryFolderConverter))]
        [DefaultValue("\\")]
#endif
        public string Path { get; set; } = System.IO.Path.DirectorySeparatorChar.ToString();

        bool _useSubFolders = true;
        /// <summary>
        /// If true, sub-folders are also published with the same definition
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tShow sub-folders"), Description("If true, sub-folders are also published with the same definition."), Id(2, 1)]
        [DefaultValue(true)]
#endif
        public bool UseSubFolders
        {
            get { return _useSubFolders; }
            set
            {
                _useSubFolders = value;
                UpdateEditorAttributes();
            }
        }

        /// <summary>
        /// If true, the user can Create, Rename or Delete sub-folders in this folder. This flag is only used if Sub-folders are shown.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Manage sub-folder"), Description("If true, the user can Create, Rename or Delete sub-folders in this folder. This flag is only used if Sub-folders are shown."), Id(3, 1)]
        [DefaultValue(true)]
#endif
        public bool ManageFolder { get; set; } = true;

        FolderRight _folderRight = FolderRight.Edit;
        /// <summary>
        /// The right applied on the reports and files of the folder
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tRight"), Description("The right applied on the reports and files of the folder"), Id(4, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(FolderRight.Edit)]
#endif
        public FolderRight FolderRight
        {
            get { return _folderRight; }
            set
            {
                _folderRight = value;
                UpdateEditorAttributes();
            }
        }

        /// <summary>
        /// If true, all the Sub-folders displayed in the Tree View are expanded by default
        /// </summary>
#if WINDOWS
        [Category("Options"), DisplayName("Expand Tree View Sub-folders"), Description("If true, all the Sub-folders displayed in the Tree View are expanded by default."), Id(2, 2)]
        [DefaultValue(true)]
#endif
        public bool ExpandSubFolders { get; set; } = true;

        /// <summary>
        /// If true, only files can be viewed or managed in the folder (reports are not shown and can not be created)
        /// </summary>
#if WINDOWS
        [Category("Options"), DisplayName("Files only (no reports)"), Description("If true, only files can be viewed or managed in the folder (reports are not shown and can not be created)."), Id(3, 2)]
        [DefaultValue(false)]
#endif
        public bool FilesOnly { get; set; } = false;

        /// <summary>
        /// Helper: is true if defined in a group 
        /// </summary>
        [XmlIgnore]
        public bool IsDefined = false;
    }
}

