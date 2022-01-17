//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
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
    /// <remarks> 
    ///
    public class Impersonator : IDisposable
    {
        protected const int LOGON32_PROVIDER_DEFAULT = 0;
        protected const int LOGON32_LOGON_INTERACTIVE = 2;

        public WindowsIdentity Identity = null;
        private System.IntPtr m_accessToken;


        [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain,
        string lpszPassword, int dwLogonType, int dwLogonProvider, ref System.IntPtr phToken);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private extern static bool CloseHandle(System.IntPtr handle);


        // AccessToken ==> this.Identity.AccessToken
        //public Microsoft.Win32.SafeHandles.SafeAccessTokenHandle AT
        //{
        //    get
        //    {
        //        var at = new Microsoft.Win32.SafeHandles.SafeAccessTokenHandle(this.m_accessToken);
        //        return at;
        //    }
        //}


        public Impersonator()
        {
            this.Identity = WindowsIdentity.GetCurrent();
        }


        public Impersonator(string username, string domain, string password)
        {
            Login(username, domain, password);
        }


        public void Login(string username, string domain, string password)
        {
            if (this.Identity != null)
            {
                this.Identity.Dispose();
                this.Identity = null;
            }


            try
            {
                this.m_accessToken = new System.IntPtr(0);
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


        void System.IDisposable.Dispose()
        {
            Logout();
        } // End Sub Dispose 

        public static WindowsIdentity CheckWindowsLogin(string userName, string domain, string password)
        {
            var imp = new Impersonator(userName, domain, password);
            return imp.Identity;
        }
    }
}