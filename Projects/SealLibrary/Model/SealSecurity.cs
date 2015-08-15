//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Data.OleDb;
using System.Data;
using System.ComponentModel;
using Seal.Converter;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.IO;
using Seal.Helpers;
using System.Text.RegularExpressions;
using Seal.Forms;
using DynamicTypeDescriptor;
using System.Windows.Forms.Design;
using System.Net.Mail;

namespace Seal.Model
{
    public class SealSecurity : RootEditor
    {
        [XmlIgnore]
        public string FilePath;

        [XmlIgnore]
        public Repository Repository;

        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("CurrentParameters").SetIsBrowsable(true);
                GetProperty("ProviderName").SetIsBrowsable(true);
                GetProperty("Groups").SetIsBrowsable(true);
                //GetProperty("ProviderScript").SetIsBrowsable(true);
                GetProperty("UseCustomScript").SetIsBrowsable(true);
                GetProperty("PromptUserPassword").SetIsBrowsable(true);
                GetProperty("Script").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);
                GetProperty("TestUserName").SetIsBrowsable(true);
                GetProperty("TestPassword").SetIsBrowsable(true);
                GetProperty("TestCurrentWindowsUser").SetIsBrowsable(true);
                GetProperty("HelperSimulateLogin").SetIsBrowsable(true);

                GetProperty("ProviderScript").SetIsReadOnly(true);
                GetProperty("PromptUserPassword").SetIsReadOnly(true);
                GetProperty("Script").SetIsReadOnly(!UseCustomScript);
                GetProperty("TestUserName").SetIsReadOnly(!Provider.PromptUserPassword);
                GetProperty("TestPassword").SetIsReadOnly(!Provider.PromptUserPassword);

                TypeDescriptor.Refresh(this);
            }
        }

        #endregion



        string _providerName;
        [DisplayName("Security Provider"), Description("The security provider used for the authentication. Security providers are defined in the repository Security\\Providers folder."), Category("Security Provider Definition"), Id(1, 1)]
        [TypeConverter(typeof(SecurityProviderConverter))]
        public string ProviderName
        {
            get { return _providerName; }
            set
            {
                _providerName = value;
                UpdateEditorAttributes();
            }
        }


        [Category("Security Provider Definition"), DisplayName("Prompt User Name and Password"), Description("If true, a login screen is displayed to request a user name and password from the user. The user and password provided must then be used in the script below."), Id(2, 1)]
        [XmlIgnore]
        public bool PromptUserPassword
        {
            get { return Provider.PromptUserPassword; }
        }

        [Category("Security Provider Definition"), DisplayName("Provider Security Script"), Description("The script executed to login and find the security group used to published reports."), Id(3, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        [XmlIgnore]
        public string ProviderScript
        {
            get { return Provider.Script; }
        }

        bool _useCustomScript = false;
        [Category("Security Provider Configuration"), DisplayName("Use custom Security Script"), Description("If true, a custom script can be used for the authentication process."), Id(1, 2)]
        public bool UseCustomScript
        {
            get { return _useCustomScript; }
            set
            {
                _useCustomScript = value;
                UpdateEditorAttributes();
            }
        }

        string _script = "";
        [Category("Security Provider Configuration"), DisplayName("Custom Security Script"), Description("The script executed to login and find the security group used to published reports. If the script is empty, the publication is done using the first security group defined."), Id(2, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string Script
        {
            get { return _script; }
            set { _script = value; }
        }


        public SecurityProvider Provider
        {
            get
            {
                var result = Providers.FirstOrDefault(i => i.Name == ProviderName);
                if (result == null) result = Providers.FirstOrDefault(i => i.Name == "No Security");
                if (result == null) result = Providers.FirstOrDefault();
                _providerName = result.Name;
                return result;
            }
        }


        List<SecurityProvider> _providers = null;
        [XmlIgnore]
        public List<SecurityProvider> Providers
        {
            get
            {
                if (_providers == null)
                {
                    _providers = SecurityProvider.LoadProviders(Repository.SecurityProvidersFolder);
                }
                return _providers;
            }
        }

        List<SecurityParameter> _parameters = new List<SecurityParameter>();
        public List<SecurityParameter> Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }


        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        [Category("Security Provider Configuration"), DisplayName("Parameters"), Description("Parameter values used in the script."), Id(3, 2)]
        [XmlIgnore]
        public List<SecurityParameter> CurrentParameters
        {
            get
            {
                //Copy existing parameter values
                for (int i = 0; i < _parameters.Count; i++)
                {
                    var param = _parameters[i];
                    var paramProvider = Provider.Parameters.FirstOrDefault(j => j.Name == param.Name);
                    if (paramProvider != null) paramProvider.Value = param.Value;
                }
                //Then use the paramters of the provider
                _parameters.RemoveAll(i => Provider.Parameters.Exists(j => j.Name == i.Name));
                _parameters.AddRange(Provider.Parameters.Where(i => !_parameters.Exists(j => j.Name == i.Name)));
                return Provider.Parameters;
            }
            set { _parameters = value; }
        }


        List<SecurityGroup> _groups = new List<SecurityGroup>();
        [Category("Groups"), DisplayName("Security Groups"), Description("The groups defines how are published folders and reports in the Web Report Server. At least one group must exist."), Id(1, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<SecurityGroup> Groups
        {
            get { return _groups; }
            set { _groups = value; }
        }

        public SecurityFolder FindSecurityFolder(List<SecurityGroup> groups, string folder)
        {
            SecurityFolder result = null;
            foreach (var group in groups)
            {
                string folderRoot = Repository.ConvertToRepositoryPath(folder);
                while (result == null && !string.IsNullOrEmpty(folderRoot))
                {
                    result = group.Folders.FirstOrDefault(i => i.Path == folderRoot);
                    folderRoot = Path.GetDirectoryName(folderRoot);
                }
                if (result != null && result.Path != Repository.ConvertToRepositoryPath(folder) && !result.UseSubFolders)
                {
                    //cannot use this parent
                    result = null;
                }

                if (result != null) break;
            }
            return result;
        }

        public bool IsParentSecurityFolder(List<SecurityGroup> groups, string folder)
        {
            foreach (var group in groups)
            {
                string folderRoot = Repository.ConvertToRepositoryPath(folder);
                if (group.Folders.Exists(i => i.Path.StartsWith(folderRoot))) return true;
            }
            return false;
        }

        string getSecuritySummary(SecurityGroup group, string folder)
        {
            StringBuilder result = new StringBuilder();
            List<SecurityGroup> groups = new List<SecurityGroup>();
            groups.Add(group);
            SecurityFolder securityFolder = FindSecurityFolder(groups, folder);
            if (securityFolder != null)
            {
                string pattern = string.IsNullOrEmpty(securityFolder.SearchPattern) ? "" : string.Format(", Search Pattern:{0}", securityFolder.SearchPattern);
                result.AppendLine(string.Format("    Folder:{0} (Type:{1}{2})\r\n", Repository.ConvertToRepositoryPath(folder), Helper.GetEnumDescription(securityFolder.PublicationType.GetType(), securityFolder.PublicationType), pattern));
            }
            foreach (string subFolder in Directory.GetDirectories(folder))
            {
                result.Append(getSecuritySummary(group, subFolder));
            }
            return result.ToString();
        }

        public string GetSecuritySummary()
        {
            StringBuilder result = new StringBuilder();

            initSecurity();
            foreach (var group in _groups)
            {
                result.AppendLine(string.Format("Security Group: {0}\r\n", group.Name));
                result.AppendLine(getSecuritySummary(group, Repository.ReportsFolder));
            }
            return result.ToString();
        }

        void initSecurity()
        {
            //init at least a security group
            if (_groups.Count == 0)
            {
                SecurityGroup group = new SecurityGroup() { Name = "Default Group" };
                _groups.Add(group);
                group.Folders.Add(new SecurityFolder());
            }
        }

        [XmlIgnore]
        public DateTime LastModification;
        static public SealSecurity LoadFromFile(string path, bool ignoreException)
        {
            SealSecurity result = null;
            try
            {
                StreamReader sr = new StreamReader(path);
                XmlSerializer serializer = new XmlSerializer(typeof(SealSecurity));
                result = (SealSecurity)serializer.Deserialize(sr);
                sr.Close();
                result.FilePath = path;
                result.LastModification = File.GetLastWriteTime(path);
                result.initSecurity();
            }
            catch (Exception ex)
            {
                if (!ignoreException) throw new Exception(string.Format("Unable to read the security file '{0}'.\r\n{1}", path, ex.Message));
                result = new SealSecurity();
                result.initSecurity();
            }
            return result;
        }


        public void SaveToFile()
        {
            SaveToFile(FilePath);
        }

        public void SaveToFile(string path)
        {
            //Check last modification
            if (LastModification != DateTime.MinValue && File.Exists(path))
            {
                DateTime lastDateTime = File.GetLastWriteTime(path);
                if (LastModification != lastDateTime)
                {
                    throw new Exception("Unable to save the Security file. The file has been modified by another user.");
                }
            }
            XmlSerializer serializer = new XmlSerializer(typeof(SealSecurity));
            StreamWriter sw = new StreamWriter(path);
            serializer.Serialize(sw, (SealSecurity)this);
            sw.Close();
            FilePath = path;
            LastModification = File.GetLastWriteTime(path);
        }

        [XmlIgnore, Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(7, 11)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
        public string Error
        {
            get
            {
                string result = "";
                foreach (var provider in Providers.Where(i => !string.IsNullOrEmpty(i.Error)))
                {
                    result += string.Format("{0}: {1}\r\n", provider.Name, provider.Error);
                }
                return result;
            }
        }

        [XmlIgnore, Category("Test a login"), DisplayName("User name for test"), Description("User name to test a login."), Id(8, 10)]
        public string TestUserName { get; set; }

        [XmlIgnore, Category("Test a login"), PasswordPropertyText(true), DisplayName("Password for test"), Description("Password to test a login."), Id(9, 10)]
        public string TestPassword { get; set; }

        bool _testCurrentWindowsUser = true;
        [XmlIgnore, Category("Test a login"), DisplayName("Use current Windows User to test authentication"), Description("If true, the current user will be use as IPrincipal to test the Integrated Windows authentication."), Id(10, 10)]
        public bool TestCurrentWindowsUser
        {
            get { return _testCurrentWindowsUser; }
            set { _testCurrentWindowsUser = value; }
        }

        [Category("Test a login"), DisplayName("Test a login"), Description("Test a login using the test user name and password or the current windows user."), Id(11, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
        public string HelperSimulateLogin
        {
            get { return "<Click to test a login>"; }
        }

        public bool HasValue(string name)
        {
            return !string.IsNullOrEmpty(GetValue(name));
        }

        public string GetValue(string name)
        {
            SecurityParameter parameter = CurrentParameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? "" : parameter.Value;
        }

        public bool GetBoolValue(string name)
        {
            SecurityParameter parameter = CurrentParameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? false : parameter.BoolValue;
        }

        public int GetNumericValue(string name)
        {
            SecurityParameter parameter = CurrentParameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? 0 : parameter.NumericValue;
        }


    }
}
