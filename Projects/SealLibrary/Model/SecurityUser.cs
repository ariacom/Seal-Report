using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace Seal.Model
{
    public class SecurityUser
    {
        public string Name = "";
        public List<SecurityGroup> SecurityGroups = new List<SecurityGroup>();
        public SealSecurity Security;
        public string Error = "";
        public string Warning = "";
        public List<SWIFolder> Folders = new List<SWIFolder>();

        //Parameters to authenticate
        public string WebUserName = "";
        public string WebPassword = "";
        public IPrincipal WebPrincipal = null;
        public WindowsIdentity Identity = null;

        public bool IsAuthenticated
        {
            get { return SecurityGroups.Count > 0; }
        }

        public SecurityUser(SealSecurity security)
        {
            Security = security;
        }

        public bool HasPersonalFolder
        {
            get { return SecurityGroups.Exists(i => i.PersonalFolder); }
        }

        bool _tryAgain = true; //Try again to get rid of Load Assemblies exceptions...
        public void Authenticate()
        {
            Error = "";
            Warning = "";
            string script = (Security.UseCustomScript && !string.IsNullOrEmpty(Security.Script) ? Security.Script : Security.ProviderScript);
            try
            {
                Helper.ParseRazor(script, this);
            }
            catch (Exception ex)
            {
                if (_tryAgain)
                {
                    ex = null;
                    _tryAgain = false;
                    try
                    {
                        Helper.ParseRazor(script, this);
                    }
                    catch (Exception ex2)
                    {
                        ex = ex2;
                    }
                }

                if (ex != null)
                {
                    SecurityGroups.Clear();
                    Error = ex.Message;
                }
            }
        }


        public void SetDefaultCulture(string culture)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                Security.Repository.SetCultureInfo(culture);
            }
        }

        public void SetDefaultTheme(string theme)
        {
            if (!string.IsNullOrEmpty(theme))
            {
                var defaultTheme = Security.Repository.Themes.FirstOrDefault(i => i.Name == theme);
                if (defaultTheme != null)
                {
                    foreach (var t in Security.Repository.Themes) t.IsDefault = false;
                    defaultTheme.IsDefault = true;
                }
            }
        }

        public void SetDefaultLogoName(string logoName)
        {
            if (!string.IsNullOrEmpty(logoName))
            {
                Security.Repository.Configuration.LogoName = logoName;
            }
        }

        public void SetGroupConfiguration(SecurityGroup group)
        {
            //set defaults from the group
            SetDefaultCulture(group.Culture);
            SetDefaultTheme(group.Theme);
            SetDefaultLogoName(group.LogoName);
        }

        public SecurityFolder FindSecurityFolder(string folder)
        {
            if (Security == null) return null;
            return Security.FindSecurityFolder(SecurityGroups, folder);
        }


        private List<string> _noEditionCategories = null;
        public List<string> NoEditionCategories
        {
            get
            {
                if (_noEditionCategories == null) InitSecurityColumnRights();
                return _noEditionCategories;
            }
        }

        private List<string> _noEditionTags = null;
        public List<string> NoEditionTags
        {
            get
            {
                if (_noEditionTags == null) InitSecurityColumnRights();
                return _noEditionTags;
            }
        }

        public bool CanEditColumn(MetaColumn column)
        {
            return (!NoEditionCategories.Contains(column.Category) && !NoEditionTags.Contains(column.Tag));        
        }

        private void InitSecurityColumnRights()
        {
            _noEditionCategories = new List<string>();
            _noEditionTags = new List<string>();

            foreach (var sgroup in SecurityGroups)
            {
                _noEditionCategories.AddRange((from i in sgroup.Columns where i.Rights == ColumnRight.None select i.Category));
                _noEditionTags.AddRange((from i in sgroup.Columns where i.Rights == ColumnRight.None select i.Tag));
            }
        }


        public void AddDefaultSecurityGroup()
        {
            if (Security.Groups.Count > 0)
            {
                SecurityGroups.Add(Security.Groups[0]);
                SetGroupConfiguration(Security.Groups[0]);
            }
            else
            {
                Warning += "No security group defined in the security...\r\n";
            }
        }

        public void AddSecurityGroup(string name)
        {
            var newGroup = Security.Groups.FirstOrDefault(i => i.Name == name);
            if (newGroup != null)
            {
                SecurityGroups.Add(newGroup);
                SetGroupConfiguration(newGroup);
            }
            else
            {
                Warning += "Unable to add the security group: " + name + "\r\n";
            }
        }

        public void AddWindowsGroupToSecurityGroup(bool skipDomainName, string ADcontextType)
        {
            if (_windowsGroups == null)
            {
                _windowsGroups = !string.IsNullOrEmpty(ADcontextType) ? GetWindowsGroupsUsingAD(ADcontextType) : GetWindowsGroupsUsingWindowsIdentity();
            }

            List<string> groups = new List<string>();
            foreach (var group in _windowsGroups) groups.Add(skipDomainName ? Path.GetFileName(group.ToLower()) : group.ToLower());

            SecurityGroups.AddRange(Security.Groups.Where(i => groups.Contains(i.Name.ToLower())));

            if (SecurityGroups.Count == 0 && _windowsGroups.Count > 0)
            {
                Warning += "Create a security group having one of the following names:\r\n";
                foreach (var group in _windowsGroups)
                {
                    Warning += group + "\r\n";
                }
            }
            else if (SecurityGroups.Count > 0)
            {
                SetGroupConfiguration(SecurityGroups[0]);
            }
        }

        List<string> _windowsGroups = null;
        bool _tryAgainWindowsIdentity = true; //Try again to get rid of Load Assemblies exceptions...
        List<string> GetWindowsGroupsUsingWindowsIdentity()
        {
            var result = new List<string>();
            try
            {
                var identity = Identity;
                if (WebPrincipal != null) identity = WebPrincipal.Identity as WindowsIdentity;
                if (identity != null)
                {
                    foreach (IdentityReference group in identity.Groups)
                    {
                        result.Add(group.Translate(typeof(NTAccount)).ToString().ToLower());
                    }
                }
            }
            catch (Exception ex)
            {
                if (_tryAgainWindowsIdentity)
                {
                    _tryAgainWindowsIdentity = false;
                    result = GetWindowsGroupsUsingWindowsIdentity();
                }
                else
                {
                    Warning += "Error getting groups using WindowsIdentity, try perhaps with the AD option...\r\n" + ex.Message + "\r\n";
                }
            }
            return result;
        }

        bool _tryAgainAD = true; //Try again to get rid of Load Assemblies exceptions...
        List<string> GetWindowsGroupsUsingAD(string contextType)
        {
            var result = new List<string>();
            try
            {
                // set up domain context
                PrincipalContext context = new PrincipalContext((ContextType)Enum.Parse(typeof(ContextType), contextType));
                string name = WebUserName;
                if (WebPrincipal != null) name = WebPrincipal.Identity.Name;
                if (Identity != null) name = Identity.Name;

                var user = new UserPrincipal(context);
                user.SamAccountName = name;
                var searcher = new PrincipalSearcher(user);
                user = searcher.FindOne() as UserPrincipal;
                if (user == null) user = UserPrincipal.FindByIdentity(context, name);

                if (user != null)
                {
                    // find the roles....
                    var roles = user.GetGroups();
                    // enumerate over them
                    foreach (Principal p in roles)
                    {
                        result.Add(p.Name);
                    }
                }
                else throw new Exception("Unable to find user:" + name);
            }
            catch (Exception ex)
            {
                if (_tryAgainAD)
                {
                    _tryAgainAD = false;
                    result = GetWindowsGroupsUsingAD(contextType);
                }
                else
                {
                    Warning += "Error getting groups using AD...\r\n" + ex.Message + "\r\n";
                }
            }
            return result;
        }

        public string AuthenticationSummary
        {
            get
            {
                string message = IsAuthenticated ? "SUCCESS: User is authenticated\r\n" : "ERROR: Authentication failed\r\n";
                if (!string.IsNullOrEmpty(WebUserName)) message += string.Format("User login name: {0}\r\n", WebUserName);
                if (!string.IsNullOrEmpty(Name)) message += string.Format("User display name: {0}\r\n", Name);
                foreach (var group in SecurityGroups)
                {
                    message += string.Format("Security group: {0}\r\n", group.Name);
                }
                if (!string.IsNullOrEmpty(Error)) message += "\r\nError:\r\n" + Error;
                if (!string.IsNullOrEmpty(Warning)) message += "\r\nWarning:\r\n" + Warning;

                return message;
            }
        }

        public string SecurityGroupsDisplay
        {
            get
            {
                string result = "";
                foreach (var group in SecurityGroups)
                {
                    if (!string.IsNullOrEmpty(result)) result += ";";
                    result += group.Name;
                }
                return result;
            }
        }

    }
}
