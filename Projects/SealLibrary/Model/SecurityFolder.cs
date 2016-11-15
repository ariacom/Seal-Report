using DynamicTypeDescriptor;
using Seal.Converter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Windows.Forms.Design;
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


                GetProperty("ManageFolder").SetIsReadOnly(!_useSubFolders);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        string _path = "\\";
        [Category("Definition"), DisplayName("\tPath"), Description("The folder path containing the reports to publish. The path is relative to the repository 'Reports' folder and should be unique in the security group."), Id(1,1)]
        [TypeConverter(typeof(RepositoryFolderConverter))]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        bool _useSubFolders = true;
        [Category("Definition"), DisplayName("\tShow sub-folders"), Description("If true, sub-folders are also published with the same definition."), Id(2, 1)]
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
        public bool ManageFolder
        {
            get { return _manageFolder; }
            set { _manageFolder = value; }
        }

        FolderRight _folderRight = FolderRight.Edit;
        [Category("Definition"), DisplayName("\tRight"), Description("The right applied on the reports of the folder."), Id(4, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
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
        public bool ExpandSubFolders
        {
            get { return _expandSubFolders; }
            set { _expandSubFolders = value; }
        }

        //Helper, set to true if defined in a group
        [XmlIgnore]
        public bool IsDefined = false;
    }
}
