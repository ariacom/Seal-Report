//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
namespace Seal.Helpers
{
    //Got from http://platinumdogs.me/2008/10/30/net-c-impersonation-with-network-credentials/

    public enum LogonType
    {
        LOGON32_LOGON_INTERACTIVE = 2,
        LOGON32_LOGON_NETWORK = 3,
        LOGON32_LOGON_BATCH = 4,
        LOGON32_LOGON_SERVICE = 5,
        LOGON32_LOGON_UNLOCK = 7,
        LOGON32_LOGON_NETWORK_CLEARTEXT = 8, // Win2K or higher
        LOGON32_LOGON_NEW_CREDENTIALS = 9 // Win2K or higher
    };

    public enum LogonProvider
    {
        LOGON32_PROVIDER_DEFAULT = 0,
        LOGON32_PROVIDER_WINNT35 = 1,
        LOGON32_PROVIDER_WINNT40 = 2,
        LOGON32_PROVIDER_WINNT50 = 3
    };

    public enum ImpersonationLevel
    {
        SecurityAnonymous = 0,
        SecurityIdentification = 1,
        SecurityImpersonation = 2,
        SecurityDelegation = 3
    }

    class Win32NativeMethods
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int LogonUser(string lpszUserName,
             string lpszDomain,
             string lpszPassword,
             int dwLogonType,
             int dwLogonProvider,
             ref IntPtr phToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int DuplicateToken(IntPtr hToken,
              int impersonationLevel,
              ref IntPtr hNewToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);
    }

    /// <summary>
    /// Allows code to be executed under the security context of a specified user account.
    /// </summary>
    /// <remarks> 
    ///
    /// Implements IDispose, so can be used via a using-directive or method calls;
    ///  ...
    ///
    ///  var imp = new Impersonator( "myUsername", "myDomainname", "myPassword" );
    ///  imp.UndoImpersonation();
    ///
    ///  ...
    ///
    ///   var imp = new Impersonator();
    ///  imp.Impersonate("myUsername", "myDomainname", "myPassword");
    ///  imp.UndoImpersonation();
    ///
    ///  ...
    ///
    ///  using ( new Impersonator( "myUsername", "myDomainname", "myPassword" ) )
    ///  {
    ///   ...
    ///   1
    ///   ...
    ///  }
    ///
    ///  ...
    /// </remarks>
    public class Impersonator : IDisposable
    {
        private WindowsImpersonationContext _wic;

        /// <summary>
        /// Begins impersonation with the given credentials, Logon type and Logon provider.
        /// </summary>
        ///
        public Impersonator(string userName, string domainName, string password, LogonType logonType, LogonProvider logonProvider)
        {
            Impersonate(userName, domainName, password, logonType, logonProvider);
        }

        /// <summary>
        /// Begins impersonation with the given credentials.
        /// </summary>
        ///
        public Impersonator(string userName, string domainName, string password)
        {
            Impersonate(userName, domainName, password, LogonType.LOGON32_LOGON_INTERACTIVE, LogonProvider.LOGON32_PROVIDER_DEFAULT);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Impersonator"/> class.
        /// </summary>
        public Impersonator()
        { }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            UndoImpersonation();
        }

        /// <summary>
        /// Impersonates the specified user account.
        /// </summary>
        ///
        public void Impersonate(string userName, string domainName, string password)
        {
            Impersonate(userName, domainName, password, LogonType.LOGON32_LOGON_INTERACTIVE, LogonProvider.LOGON32_PROVIDER_DEFAULT);
        }

        /// <summary>
        /// Impersonates the specified user account.
        /// </summary>
        ///
        public void Impersonate(string userName, string domainName, string password, LogonType logonType, LogonProvider logonProvider)
        {
            UndoImpersonation();

            IntPtr logonToken = IntPtr.Zero;
            IntPtr logonTokenDuplicate = IntPtr.Zero;
            try
            {
                // revert to the application pool identity, saving the identity of the current requestor
                _wic = WindowsIdentity.Impersonate(IntPtr.Zero);

                // do logon & impersonate
                if (Win32NativeMethods.LogonUser(userName,
                    domainName,
                    password,
                    (int)logonType,
                    (int)logonProvider,
                    ref logonToken) != 0)
                {
                    if (Win32NativeMethods.DuplicateToken(logonToken, (int)ImpersonationLevel.SecurityImpersonation, ref logonTokenDuplicate) != 0)
                    {
                        var wi = new WindowsIdentity(logonTokenDuplicate);
                        wi.Impersonate(); // discard the returned identity context (which is the context of the application pool)
                    }
                    else
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                else
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (logonToken != IntPtr.Zero)
                    Win32NativeMethods.CloseHandle(logonToken);

                if (logonTokenDuplicate != IntPtr.Zero)
                    Win32NativeMethods.CloseHandle(logonTokenDuplicate);
            }
        }

        /// <summary>
        /// Stops impersonation.
        /// </summary>
        private void UndoImpersonation()
        {
            // restore saved requestor identity
            if (_wic != null)
                _wic.Undo();
            _wic = null;
        }

        public static WindowsIdentity CheckWindowsLogin(string userName, string domainName, string password)
        {
            return CheckWindowsLogin(userName, domainName, password, LogonType.LOGON32_LOGON_INTERACTIVE, LogonProvider.LOGON32_PROVIDER_DEFAULT);
        }


        public static WindowsIdentity CheckWindowsLogin(string userName, string domainName, string password, LogonType logonType, LogonProvider logonProvider)
        {
            WindowsIdentity result = null;
            IntPtr logonToken = IntPtr.Zero;
            try
            {
                // do logon & impersonate
                if (Win32NativeMethods.LogonUser(userName,
                    domainName,
                    password,
                    (int)logonType,
                    (int)logonProvider,
                    ref logonToken) != 0)
                {
                    result = new WindowsIdentity(logonToken);
                }
                else
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (logonToken != IntPtr.Zero)
                    Win32NativeMethods.CloseHandle(logonToken);
            }
            return result;

        }
    }
}