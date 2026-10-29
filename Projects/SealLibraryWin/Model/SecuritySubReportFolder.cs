//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System.ComponentModel;
#if WINDOWS
using DynamicTypeDescriptor;
using Seal.Forms;
#endif


namespace Seal.Model
{
    /// <summary>
    /// A SecuritySubReportFolder defines the right to execute the sub-reports of a folder of the repository 'SubReports' folder.
    /// A folder is protected once a group of the security declares it: only the users of a group having the right Execute on it can then execute its sub-reports.
    /// A folder declared by no group can be executed by all the users (from the navigation links of the reports they execute).
    /// </summary>
    public class SecuritySubReportFolder : RootEditor
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
                GetProperty("Execute").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion
#endif

        /// <summary>
        /// The folder path containing the sub-reports. The path is relative to the repository 'SubReports' folder and should be unique in the security group.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("\tPath"), Description("The folder path containing the sub-reports. The path is relative to the repository 'SubReports' folder and should be unique in the security group."), Id(1, 1)]
        [TypeConverter(typeof(RepositoryFolderConverter))]
        [DefaultValue("\\")]
#endif
        public string Path { get; set; } = System.IO.Path.DirectorySeparatorChar.ToString();

        /// <summary>
        /// If true, sub-folders have the same definition
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Include sub-folders"), Description("If true, sub-folders have the same definition."), Id(2, 1)]
        [DefaultValue(true)]
#endif
        public bool UseSubFolders { get; set; } = true;

        /// <summary>
        /// If true, the users of the group can execute the sub-reports of the folder. If false, the folder is protected without giving the right (e.g. to exclude a sub-folder of a folder having the right).
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Execute"), Description("If true, the users of the group can execute the sub-reports of the folder. If false, the folder is protected without giving the right (e.g. to exclude a sub-folder of a folder having the right)."), Id(3, 1)]
        [DefaultValue(true)]
#endif
        public bool Execute { get; set; } = true;
    }
}

