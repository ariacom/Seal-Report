//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using Seal.Helpers;
#if WINDOWS
using DynamicTypeDescriptor;
using Seal.Forms;
#endif


namespace Seal.Model
{
    /// <summary>
    /// A SecurityRepositoryFolder defines the security applied to a folder published anywhere under the
    /// repository root (outside the 'Reports' tree). This is a DANGER ZONE feature: it can expose sensitive
    /// repository folders (Views, Sources, Settings, Security...) to web users.
    /// </summary>
    public class SecurityRepositoryFolder : RootEditor
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
                GetProperty("FolderRight").SetIsBrowsable(true);
                GetProperty("UseSubFolders").SetIsBrowsable(true);
                GetProperty("ManageFolder").SetIsBrowsable(true);
                GetProperty("AllowUpload").SetIsBrowsable(true);
                GetProperty("Icon").SetIsBrowsable(true);

                //Managing sub-folders only makes sense when sub-folders are shown and the folder is writable
                GetProperty("ManageFolder").SetIsReadOnly(!_useSubFolders || _folderRight != RepositoryFolderRight.ReadWrite);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion
#endif

        /// <summary>
        /// The folder path to publish, relative to the repository root (e.g. 'Views\\MyTheme'). It must NOT point at
        /// or inside the 'Reports' folder, which is managed by the 'Report Folders' configuration.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tPath"), Description("The folder path to publish, relative to the repository root (e.g. 'Views\\MyTheme'). It must NOT point at or inside the 'Reports' folder, which is managed by the 'Report folders' configuration."), Id(1, 1)]
        [TypeConverter(typeof(RepositoryRootFolderConverter))]
        [DefaultValue("\\")]
#endif
        public string Path { get; set; } = System.IO.Path.DirectorySeparatorChar.ToString();

        RepositoryFolderRight _folderRight = RepositoryFolderRight.None;
        /// <summary>
        /// The right applied on the files of the folder. By default, repository folders have no right.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tRight"), Description("The right applied on the files of the folder. By default, repository folders have no right."), Id(2, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        [DefaultValue(RepositoryFolderRight.None)]
#endif
        public RepositoryFolderRight FolderRight
        {
            get { return _folderRight; }
            set
            {
                _folderRight = value;
                UpdateEditorAttributes();
            }
        }

        bool _useSubFolders = true;
        /// <summary>
        /// If true, sub-folders are also published with the same definition
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Include sub-folders"), Description("If true, sub-folders are also published with the same definition."), Id(3, 1)]
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
        /// If true, the user can Create, Rename or Delete sub-folders in this folder. This flag is only used if Sub-folders are shown and the Right is 'Read write'.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Manage sub-folders"), Description("If true, the user can Create, Rename or Delete sub-folders in this folder. This flag is only used if Sub-folders are shown and the Right is 'Read write'."), Id(4, 1)]
        [DefaultValue(false)]
#endif
        public bool ManageFolder { get; set; } = false;

        /// <summary>
        /// If true, the user can upload files into this folder. Files are always downloadable; reports are not shown in repository folders, so only upload is configurable here.
        /// </summary>
#if WINDOWS
        [Category("Danger Zone"), DisplayName("Allow upload"), Description("If true, the user can upload files into this folder.\r\n⚠ DANGER ZONE: uploading into a folder holding server-executed templates (e.g. Views or Sources) is a remote code execution vector. Only allow upload on folders and groups you fully trust."), Id(1, 9)]
        [DefaultValue(false)]
#endif
        public bool AllowUpload { get; set; } = false;
        /// <summary>
        /// Xml serialization helper: serialize AllowUpload only if not the default value
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeAllowUpload() { return AllowUpload; }

        /// <summary>
        /// Optional icon used for this folder in the Web Report Server tree view. Enter a Font Awesome class (e.g. 'fa-solid fa-database'); legacy Glyphicon names are also accepted. If empty, the default repository folder icon is used.
        /// </summary>
#if WINDOWS
        [Category("Options"), DisplayName("Tree view icon"), Description("Optional icon used for this folder in the Web Report Server tree view. Enter a Font Awesome class (e.g. 'fa-solid fa-database'); legacy Glyphicon names are also accepted. If empty, the default repository folder icon is used."), Id(1, 2)]
        [Editor(typeof(FontAwesomeIconEditor), typeof(System.Drawing.Design.UITypeEditor))]
#endif
        public string Icon { get; set; }
        /// <summary>
        /// Xml serialization helper: serialize Icon only if not empty
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeIcon() { return !string.IsNullOrEmpty(Icon); }

        /// <summary>
        /// Helper: is true if defined in a group
        /// </summary>
        [XmlIgnore]
        public bool IsDefined = false;

        /// <summary>
        /// Returns the path normalized to compare with a SWIFolder repository-relative path:
        /// OS separators, a leading separator and no trailing separator (the repository root is a single separator).
        /// </summary>
        public static string Normalize(string path)
        {
            path = FileHelper.ConvertOSFilePath(path ?? "");
            var sep = System.IO.Path.DirectorySeparatorChar;
            path = path.Trim();
            if (string.IsNullOrEmpty(path)) return sep.ToString();
            if (!path.StartsWith(sep.ToString())) path = sep + path;
            path = path.TrimEnd(sep);
            return string.IsNullOrEmpty(path) ? sep.ToString() : path;
        }

        /// <summary>
        /// True if the given (normalized) repository-relative path points at the 'Reports' folder or any of its sub-folders.
        /// Such folders are managed by the 'Report Folders' configuration and must be ignored here.
        /// </summary>
        public static bool IsUnderReports(string normalizedPath)
        {
            var sep = System.IO.Path.DirectorySeparatorChar;
            var reports = sep + "Reports";
            return string.Equals(normalizedPath, reports, System.StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(reports + sep, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
