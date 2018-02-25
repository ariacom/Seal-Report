using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seal.Forms
{
    public class WebBrowserHelper
    {
        public static int GetEmbVersion()
        {
            int ieVer = GetBrowserVersion();

            if (ieVer > 9)
                return ieVer * 1000 + 1;

            if (ieVer > 7)
                return ieVer * 1111;

            return 7000;
        } // End Function GetEmbVersion

        public static bool fixDone = false;
        public static void FixBrowserVersion()
        {
            if (!fixDone)
            {
                string appName = System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location);
                FixBrowserVersion(appName);
                fixDone = true;
            }
        }

        public static void FixBrowserVersion(string appName)
        {
            FixBrowserVersion(appName, GetEmbVersion());
        } // End Sub FixBrowserVersion

        // FixBrowserVersion("<YourAppName>", 9000);
        public static void FixBrowserVersion(string appName, int ieVer)
        {
            FixBrowserVersion_Internal("HKEY_LOCAL_MACHINE", appName + ".exe", ieVer);
            FixBrowserVersion_Internal("HKEY_CURRENT_USER", appName + ".exe", ieVer);
            FixBrowserVersion_Internal("HKEY_LOCAL_MACHINE", appName + ".vshost.exe", ieVer);
            FixBrowserVersion_Internal("HKEY_CURRENT_USER", appName + ".vshost.exe", ieVer);
        } // End Sub FixBrowserVersion 

        private static void FixBrowserVersion_Internal(string root, string appName, int ieVer)
        {
            try
            {
                //For 64 bit Machine 
                if (Environment.Is64BitOperatingSystem)
                    Microsoft.Win32.Registry.SetValue(root + @"\Software\Wow6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", appName, ieVer);
                else  //For 32 bit Machine 
                    Microsoft.Win32.Registry.SetValue(root + @"\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", appName, ieVer);


            }
            catch (Exception)
            {
                // some config will hit access rights exceptions
                // this is why we try with both LOCAL_MACHINE and CURRENT_USER
            }
        } // End Sub FixBrowserVersion_Internal 

        public static int GetBrowserVersion()
        {
            // string strKeyPath = @"HKLM\SOFTWARE\Microsoft\Internet Explorer";
            string strKeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer";
            string[] ls = new string[] { "svcVersion", "svcUpdateVersion", "Version", "W2kVersion" };

            int maxVer = 0;
            for (int i = 0; i < ls.Length; ++i)
            {
                object objVal = Microsoft.Win32.Registry.GetValue(strKeyPath, ls[i], "0");
                string strVal = System.Convert.ToString(objVal);
                if (strVal != null)
                {
                    int iPos = strVal.IndexOf('.');
                    if (iPos > 0)
                        strVal = strVal.Substring(0, iPos);

                    int res = 0;
                    if (int.TryParse(strVal, out res))
                        maxVer = Math.Max(maxVer, res);
                } // End if (strVal != null)

            } // Next i

            return maxVer;
        } // End Function GetBrowserVersion 


    }
}
