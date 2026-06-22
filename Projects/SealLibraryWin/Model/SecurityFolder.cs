//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
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
                GetProperty("DownloadUpload").SetIsBrowsable(true);
                GetProperty("Icon").SetIsBrowsable(true);


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
        /// Defines if the user can download reports/files or upload files and reports in this folder. DANGER ZONE: enabling Upload lets web users upload files and reports into the repository; an uploaded report may contain tasks that run code on the server.
        /// </summary>
#if WINDOWS
        [Category("Danger Zone"), DisplayName("Upload/Download"), Description("Defines if the user can download reports/files or upload files and reports in this folder.\r\n⚠ DANGER ZONE: setting 'Download and Upload' lets web users upload files and reports into this repository folder. An uploaded report may contain tasks that execute code on the server. Only allow Upload on folders and groups you fully trust."), Id(1, 9)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(DownloadUpload.None)]
#endif
        public DownloadUpload DownloadUpload { get; set; } = DownloadUpload.None;
        public bool ShouldSerializeDownloadUpload() { return DownloadUpload != DownloadUpload.None; }

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
        /// Optional icon used for this folder in the Web Report Server tree view. Enter a Font Awesome class (e.g. 'fa-solid fa-building'); legacy Glyphicon names are also accepted. If empty, the default folder icon is used.
        /// </summary>
#if WINDOWS
        [Category("Options"), DisplayName("Tree View icon"), Description("Optional icon used for this folder in the Web Report Server tree view. Enter a Font Awesome class (e.g. 'fa-solid fa-building'); legacy Glyphicon names are also accepted. If empty, the default folder icon is used."), Id(4, 2)]
        [Editor(typeof(FontAwesomeIconEditor), typeof(System.Drawing.Design.UITypeEditor))]
#endif
        public string Icon { get; set; }
        public bool ShouldSerializeIcon() { return !string.IsNullOrEmpty(Icon); }

        /// <summary>
        /// Helper: is true if defined in a group
        /// </summary>
        [XmlIgnore]
        public bool IsDefined = false;
    }
}

