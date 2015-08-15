using DynamicTypeDescriptor;
using Seal.Converter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Windows.Forms.Design;

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
                GetProperty("SearchPattern").SetIsBrowsable(true);
                GetProperty("UseSubFolders").SetIsBrowsable(true);
                GetProperty("PublicationType").SetIsBrowsable(true);
                GetProperty("ExpandSubFolders").SetIsBrowsable(true);
                GetProperty("DescriptionFile").SetIsBrowsable(true);

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
        [Category("Definition"), DisplayName("\tUse Sub-folders"), Description("If true, Sub-folders are also published with the same definition."), Id(2, 1)]
        public bool UseSubFolders
        {
            get { return _useSubFolders; }
            set { _useSubFolders = value; }
        }

        PublicationType _publicationType = PublicationType.ExecuteOutput;
        [Category("Definition"), DisplayName("\tPublication Type"), Description("The publication type: execute reports only or execute reports and outputs."), Id(3, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public PublicationType PublicationType
        {
            get { return _publicationType; }
            set { 
                _publicationType = value;
                UpdateEditorAttributes();
            }
        }

        string _descriptionFile = "";
        [Category("Definition"), DisplayName("Folder Description File Name"), Description("If not empty and a file with this name exists in the folder, the Web Server displays the file content in the detail frame. If the file has a .cshtml extension, it will be displayed as a View located in the Views\\Home folder of the Web Server."), Id(4, 1)]
        public string DescriptionFile
        {
            get { return _descriptionFile; }
            set { _descriptionFile = value; }
        }

        string _searchPattern = "";
        [Category("Options"), DisplayName("Search Pattern"), Description("Optional search pattern for the report names in the folder. Wildchar * means Zero or more characters. Wildchar ? means Exactly zero or one character."), Id(1, 2)]
        public string SearchPattern
        {
            get { return _searchPattern; }
            set { _searchPattern = value; }
        }

        bool _expandSubFolders = true;
        [Category("Options"), DisplayName("Expand Tree View Sub-folders"), Description("If true, all the Sub-folders displayed in the Tree View are expanded by default."), Id(2, 2)]
        public bool ExpandSubFolders
        {
            get { return _expandSubFolders; }
            set { _expandSubFolders = value; }
        }


        public string UsedSearchPattern
        {
            get { return string.IsNullOrEmpty(_searchPattern) ? "*.*" : _searchPattern; }
        }
    }
}
