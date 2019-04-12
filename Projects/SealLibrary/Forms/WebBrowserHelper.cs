//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Microsoft.Win32;
using System.Windows.Forms;

namespace Seal.Forms
{
    public class WebBrowserHelper
    {
        static bool fixDone = false;
        public static void FixBrowserVersion()
        {
            if (!fixDone)
            {
                int BrowserVer, RegVal;

                // get the installed IE version
                using (WebBrowser Wb = new WebBrowser())
                    BrowserVer = Wb.Version.Major;

                // set the appropriate IE version
                if (BrowserVer >= 11)
                    RegVal = 11001;
                else if (BrowserVer == 10)
                    RegVal = 10001;
                else if (BrowserVer == 9)
                    RegVal = 9999;
                else if (BrowserVer == 8)
                    RegVal = 8888;
                else
                    RegVal = 7000;

                // set the actual key
                using (RegistryKey Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", RegistryKeyPermissionCheck.ReadWriteSubTree))
                    if (Key.GetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe") == null)
                        Key.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe", RegVal, RegistryValueKind.DWord);

                fixDone = true;
            }
        }

    }
}
