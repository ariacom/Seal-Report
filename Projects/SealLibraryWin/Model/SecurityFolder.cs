//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
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
                GetProperty("AllowReportDownload").SetIsBrowsable(true);
                GetProperty("AllowUpload").SetIsBrowsable(true);
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
        [Category("Definition"), DisplayName("Include sub-folders"), Description("If true, sub-folders are also published with the same definition."), Id(2, 1)]
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
        [Category("Definition"), DisplayName("Manage sub-folders"), Description("If true, the user can Create, Rename or Delete sub-folders in this folder. This flag is only used if Sub-folders are shown."), Id(3, 1)]
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
        /// If true, the user can download the report definition (the .srex file) of the reports in this folder. Files are always downloadable; this flag only controls report definitions, which expose the report SQL, connections and task scripts.
        /// </summary>
#if WINDOWS
        [Category("Options"), DisplayName("Allow report definition download"), Description("If true, the user can download the report definition (the .srex file) of the reports in this folder. Files are always downloadable; this flag only controls report definitions, which expose the report SQL, connections and task scripts."), Id(5, 2)]
        [DefaultValue(false)]
#endif
        public bool AllowReportDownload { get; set; } = false;
        /// <summary>
        /// Serialize AllowReportDownload only if not the default value
        /// </summary>
        public bool ShouldSerializeAllowReportDownload() { return AllowReportDownload; }

        /// <summary>
        /// If true, the user can upload files and reports into this folder. DANGER ZONE: an uploaded report may contain tasks that execute code on the server.
        /// </summary>
#if WINDOWS
        [Category("Danger Zone"), DisplayName("Allow upload"), Description("If true, the user can upload files and reports into this folder.\r\n⚠ DANGER ZONE: an uploaded report may contain tasks that execute code on the server. Only allow upload on folders and groups you fully trust."), Id(1, 9)]
        [DefaultValue(false)]
#endif
        public bool AllowUpload { get; set; } = false;
        /// <summary>
        /// Serialize AllowUpload only if not the default value
        /// </summary>
        public bool ShouldSerializeAllowUpload() { return AllowUpload; }

        /// <summary>
        /// If true, all the Sub-folders displayed in the Tree View are expanded by default
        /// </summary>
#if WINDOWS
        [Category("Options"), DisplayName("Expand tree view sub-folders"), Description("If true, all the sub-folders displayed in the tree view are expanded by default."), Id(2, 2)]
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
        [Category("Options"), DisplayName("Tree view icon"), Description("Optional icon used for this folder in the Web Report Server tree view. Enter a Font Awesome class (e.g. 'fa-solid fa-building'); legacy Glyphicon names are also accepted. If empty, the default folder icon is used."), Id(4, 2)]
        [Editor(typeof(FontAwesomeIconEditor), typeof(System.Drawing.Design.UITypeEditor))]
#endif
        public string Icon { get; set; }
        /// <summary>
        /// Serialize Icon only if not empty
        /// </summary>
        public bool ShouldSerializeIcon() { return !string.IsNullOrEmpty(Icon); }

        /// <summary>
        /// Helper: is true if defined in a group
        /// </summary>
        [XmlIgnore]
        public bool IsDefined = false;
    }
}

