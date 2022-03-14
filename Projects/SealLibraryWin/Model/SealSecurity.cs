//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Data;
using System.ComponentModel;
using System.IO;
using Seal.Helpers;
using System.Xml;
#if WINDOWS
using System.Drawing.Design;
using Seal.Forms;
using DynamicTypeDescriptor;
#endif


namespace Seal.Model
{
    /// <summary>
    /// SealSecurity defines all the repository security
    /// </summary>
    public class SealSecurity : RootEditor
    {
        /// <summary>
        /// Current file path
        /// </summary>
        [XmlIgnore]
        public string FilePath;

        /// <summary>
        /// Current repository
        /// </summary>
        [XmlIgnore]
        public Repository Repository;

#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Groups").SetIsBrowsable(true);
                GetProperty("Logins").SetIsBrowsable(true);
                GetProperty("ProviderName").SetIsBrowsable(true);
                GetProperty("UseCustomScript").SetIsBrowsable(true);
                GetProperty("Script").SetIsBrowsable(true);
                GetProperty("CurrentParameters").SetIsBrowsable(true);
                GetProperty("Error").SetIsBrowsable(true);
                GetProperty("TestUserName").SetIsBrowsable(true);
                GetProperty("TestPassword").SetIsBrowsable(true);
                GetProperty("TestCurrentWindowsUser").SetIsBrowsable(true);
                GetProperty("HelperSimulateLogin").SetIsBrowsable(true);


                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

#endif
        string _providerName;
        /// <summary>
        /// The security provider used for the authentication
        /// </summary>
#if WINDOWS
        [DisplayName("Security Provider"), Description("The security provider used for the authentication. Security providers are defined in the repository Security\\Providers folder."), Category("Security Provider Definition"), Id(2, 1)]
        [TypeConverter(typeof(SecurityProviderConverter))]
#endif
        public string ProviderName
        {
            get { return _providerName; }
            set
            {
                _providerName = value;
                UpdateEditorAttributes();
            }
        }

        /// <summary>
        /// The script executed to login and find the security group used to published reports
        /// </summary>
#if WINDOWS
        [Category("Security Provider Definition"), DisplayName("Provider Security Script"), Description("The script executed to login and find the security group used to published reports."), Id(3, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string ProviderScript
        {
            get { return Provider.Script; }
        }

        bool _useCustomScript = false;
        /// <summary>
        /// If true, a custom script can be used for the authentication process
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Use custom Security Script"), Description("If true, a custom script can be used for the authentication process."), Id(2, 2)]
        [DefaultValue(false)]
#endif
        public bool UseCustomScript
        {
            get { return _useCustomScript; }
            set
            {
                _useCustomScript = value;
                UpdateEditorAttributes();
            }
        }

        /// <summary>
        /// The script executed to login and find the security group used to published reports. If the script is empty, the publication is done using the first security group defined.
        /// </summary>
#if WINDOWS
        [Category("Security Provider Configuration"), DisplayName("Custom Security Script"), Description("The script executed to login and find the security group used to published reports. If the script is empty, the publication is done using the first security group defined."), Id(3, 2)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string Script { get; set; }

        /// <summary>
        /// Current SecurityProvider
        /// </summary>
        public SecurityProvider Provider
        {
            get
            {
                var result = Providers.FirstOrDefault(i => i.Name == ProviderName);
                if (result == null) result = Providers.FirstOrDefault(i => i.Name == "No Security");
                if (result == null) result = Providers.FirstOrDefault();
                if (result == null) throw new Exception("Invalid configuration: No security providers available. Check your repository configuration...");
                _providerName = result.Name;
                return result;
            }
        }


        List<SecurityProvider> _providers = null;
        /// <summary>
        /// List of SecurityProviders available in the repository
        /// </summary>
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

        /// <summary>
        /// List of SecurityParameter used by the provider
        /// </summary>
        public List<SecurityParameter> Parameters { get; set; } = new List<SecurityParameter>();
        public bool ShouldSerializeParameters() { return Parameters.Count > 0; }

        /// <summary>
        /// Parameter values used in the script
        /// </summary>
#if WINDOWS
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        [Category("Security Provider Configuration"), DisplayName("Parameters"), Description("Parameter values used in the script."), Id(4, 2)]
#endif
        [XmlIgnore]
        public List<SecurityParameter> CurrentParameters
        {
            get
            {
                //Copy existing parameter values
                for (int i = 0; i < Parameters.Count; i++)
                {
                    var param = Parameters[i];
                    var paramProvider = Provider.Parameters.FirstOrDefault(j => j.Name == param.Name);
                    if (paramProvider != null) paramProvider.Value = param.Value;
                }
                //Then use the parameters of the provider
                Parameters.AddRange(Provider.Parameters.Where(i => !Parameters.Exists(j => j.Name == i.Name)));
                return Provider.Parameters;
            }
            set { Parameters = value; }
        }

        /// <summary>
        /// The groups defines how are published folders and reports in the Web Report Server. At least one group must exist.
        /// </summary>
#if WINDOWS
        [Category("Groups and logins"), DisplayName("Security Groups"), Description("The groups defines how are published folders and reports in the Web Report Server. At least one group must exist."), Id(1, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<SecurityGroup> Groups { get; set; } = new List<SecurityGroup>();
        public bool ShouldSerializeGroups() { return Groups.Count > 0; }

        /// <summary>
        /// The groups defines how are published folders and reports in the Web Report Server. At least one group must exist.
        /// </summary>
#if WINDOWS
        [Category("Groups and logins"), DisplayName("Security Logins"), Description("The logins defined in the security. Depending on the Security Provider defined, logins may be used during the authentication process. This is the case for the 'Basic Autentication' Security provider"), Id(2, 1)]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
#endif
        public List<SecurityLogin> Logins { get; set; } = new List<SecurityLogin>();
        public bool ShouldSerializeLogins() { return Logins.Count > 0; }

        /// <summary>
        /// Returns a SecurityFolder from a given name
        /// </summary>
        public SecurityFolder FindSecurityFolder(List<SecurityGroup> groups, string folder)
        {
            SecurityFolder result = null;
            foreach (var group in groups)
            {
                string folderRoot = folder;
                SecurityFolder current = null;
                while (current == null && !string.IsNullOrEmpty(folderRoot))
                {
                    current = group.Folders.FirstOrDefault(i => i.Path == folderRoot);
                    folderRoot = Path.GetDirectoryName(folderRoot);
                }
                if (current != null && current.Path != folder && !current.UseSubFolders)
                {
                    //cannot use this parent
                    current = null;
                }

                if (current != null)
                {
                    if (result != null)
                    {
                        //Merge the groupFolder find in this group with the current result
                        //Highest right is applied..
                        result.FolderRight = (FolderRight)Math.Max((int)result.FolderRight, (int)current.FolderRight);
                        result.ExpandSubFolders = result.ExpandSubFolders || current.ExpandSubFolders;
                        result.ManageFolder = result.ManageFolder && current.ManageFolder;
                    }
                    else
                    {
                        result = (SecurityFolder)Helper.Clone(current);
                    }

                    //set IsDefined flag
                    if (current.Path == folder) result.IsDefined = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Init security defaults and integrities
        /// </summary>
        public void InitSecurity()
        {
            //init at least a security group
            if (Groups.Count == 0)
            {
                SecurityGroup group = new SecurityGroup() { Name = "Default Group" };
                Groups.Add(group);
                group.Folders.Add(new SecurityFolder());
            }
            //Remove deleted groups in logins
            foreach (var login in Logins)
            {
                login.GroupNames.RemoveAll(i => !Groups.Exists(j => j.Name == i));
            }
        }

        /// <summary>
        /// Last modification date time
        /// </summary>
        [XmlIgnore]
        public DateTime LastModification;

        /// <summary>
        /// Load a SealSecurity from a file
        /// </summary>
        static public SealSecurity LoadFromFile(string path, bool ignoreException)
        {
            SealSecurity result = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SealSecurity));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (SealSecurity)serializer.Deserialize(xr);
                }
                result.FilePath = path;
                result.LastModification = File.GetLastWriteTime(path);
                result.InitSecurity();
            }
            catch (Exception ex)
            {
                if (!ignoreException) throw new Exception(string.Format("Unable to read the security file '{0}'.\r\n{1}", path, ex.Message));
                result = new SealSecurity();
                result.InitSecurity();
            }
            return result;
        }


        /// <summary>
        /// Save to the current file
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(FilePath);
        }

        /// <summary>
        /// Save to a destination file path
        /// </summary>
        /// <param name="path"></param>
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
            Helper.Serialize(path, this);
            FilePath = path;
            LastModification = File.GetLastWriteTime(path);
        }

        /// <summary>
        /// Last error message
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(7, 11)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
#endif
		[XmlIgnore]
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

        /// <summary>
        /// Editor Helper: User name to test a login
        /// </summary>
#if WINDOWS
        [Category("Test a login"), DisplayName("User name for test"), Description("User name to test a login."), Id(8, 10)]
#endif
		[XmlIgnore]
        public string TestUserName { get; set; }

        /// <summary>
        /// Editor Helper: Password to test a login
        /// </summary>
#if WINDOWS
        [Category("Test a login"), PasswordPropertyText(true), DisplayName("Password for test"), Description("Password to test a login."), Id(9, 10)]
#endif
		[XmlIgnore]
        public string TestPassword { get; set; }

        /// <summary>
        /// Editor Helper: If true, the current user will be use as IPrincipal to test the Integrated Windows authentication
        /// </summary>
#if WINDOWS
        [Category("Test a login"), DisplayName("Use current Windows User to test authentication"), Description("If true, the current user will be use as IPrincipal to test the Integrated Windows authentication."), Id(10, 10)]
#endif
		[XmlIgnore]
        public bool TestCurrentWindowsUser { get; set; } = true;

        /// <summary>
        /// Editor Helper: Test a login using the test user name and password or the current windows user
        /// </summary>
#if WINDOWS
        [Category("Test a login"), DisplayName("Test a login"), Description("Test a login using the test user name and password or the current windows user."), Id(11, 10)]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
#endif
        public string HelperSimulateLogin
        {
            get { return "<Click to test a login>"; }
        }

        /// <summary>
        /// True if a parameter with a given name has a value
        /// </summary>
        public bool HasValue(string name)
        {
            return !string.IsNullOrEmpty(GetValue(name));
        }

        /// <summary>
        /// Parameter value of a given name
        /// </summary>
        public string GetValue(string name)
        {
            SecurityParameter parameter = CurrentParameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? "" : parameter.Value;
        }

        /// <summary>
        /// Parameter boolean value of a given name
        /// </summary>
        public bool GetBoolValue(string name)
        {
            SecurityParameter parameter = CurrentParameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? false : parameter.BoolValue;
        }

        /// <summary>
        /// Parameter numeric value of a given name
        /// </summary>
        public int GetNumericValue(string name)
        {
            SecurityParameter parameter = CurrentParameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? 0 : parameter.NumericValue;
        }

        /// <summary>
        /// List of logged users
        /// </summary>
        static public List<SecurityUser> LoggedUsers = new List<SecurityUser>();

        /// <summary>
        /// Add a logged user
        /// </summary>
        static public void AddLoggedUsed(SecurityUser user)
        {
            lock (LoggedUsers)
            {
                if (!LoggedUsers.Contains(user)) LoggedUsers.Add(user);
            }
        }

        /// <summary>
        /// Remove a logged user
        /// </summary>
        static public void RemoveLoggedUsed(SecurityUser user)
        {
            lock (LoggedUsers)
            {
                if (LoggedUsers.Contains(user)) LoggedUsers.Remove(user);
            }
        }
    }
}
