//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
namespace Seal.Helpers
{
    /// <summary>
    /// Allows code to be executed under the security context of a specified user account.
    /// </summary>
    public class Impersonator : IDisposable
    {
        /// <summary>
        /// Default logon provider for the LogonUser Windows API
        /// </summary>
        protected const int LOGON32_PROVIDER_DEFAULT = 0;
        /// <summary>
        /// Interactive logon type for the LogonUser Windows API
        /// </summary>
        protected const int LOGON32_LOGON_INTERACTIVE = 2;

        /// <summary>
        /// Windows identity of the impersonated user
        /// </summary>
        public WindowsIdentity Identity = null;
        private IntPtr m_accessToken;


        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain,
        string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        [DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private extern static bool CloseHandle(IntPtr handle);

        /// <summary>
        /// Constructor using the identity of the current user
        /// </summary>
        public Impersonator()
        {
            this.Identity = WindowsIdentity.GetCurrent();
        }


        /// <summary>
        /// Constructor performing a Windows logon with the given credentials
        /// </summary>
        public Impersonator(string username, string domain, string password)
        {
            Login(username, domain, password);
        }


        /// <summary>
        /// Perform a Windows logon with the given credentials and set the Identity. An exception is thrown if the logon fails.
        /// </summary>
        public void Login(string username, string domain, string password)
        {
            if (this.Identity != null)
            {
                this.Identity.Dispose();
                this.Identity = null;
            }


            try
            {
                this.m_accessToken = new IntPtr(0);
                Logout();

                this.m_accessToken = System.IntPtr.Zero;
                bool logonSuccessfull = LogonUser(
                   username,
                   domain,
                   password,
                   LOGON32_LOGON_INTERACTIVE,
                   LOGON32_PROVIDER_DEFAULT,
                   ref this.m_accessToken);

                if (!logonSuccessfull)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error);
                }
                Identity = new WindowsIdentity(this.m_accessToken);
            }
            catch
            {
                throw;
            }

        } // End Sub Login 


        /// <summary>
        /// Close the logon token and dispose the current identity
        /// </summary>
        public void Logout()
        {
            if (this.m_accessToken != System.IntPtr.Zero)
                CloseHandle(m_accessToken);

            this.m_accessToken = System.IntPtr.Zero;

            if (this.Identity != null)
            {
                this.Identity.Dispose();
                this.Identity = null;
            }

        } // End Sub Logout 


        void IDisposable.Dispose()
        {
            Logout();
        } // End Sub Dispose 

        /// <summary>
        /// Check Windows credentials by performing a logon and return the resulting identity
        /// </summary>
        public static WindowsIdentity CheckWindowsLogin(string userName, string domain, string password)
        {
            var imp = new Impersonator(userName, domain, password);
            return imp.Identity;
        }
    }
}