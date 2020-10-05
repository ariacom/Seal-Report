using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Seal.NETCoreSync
{
    public class Sync
    {
        static public void ToNETCore(string rootDir)
        {
            ConvertDirectory(rootDir + @"Projects\SealLibrary", rootDir + @"Projects.NETCore\SealLibrary", "*.cs");
            ConvertDirectory(rootDir + @"Projects\SealLibrary\Model", rootDir + @"Projects.NETCore\SealLibrary\Model");
            ConvertDirectory(rootDir + @"Projects\SealLibrary\Helpers", rootDir + @"Projects.NETCore\SealLibrary\Helpers");
            ConvertDirectory(rootDir + @"Projects\SealWebServer\Controllers", rootDir + @"Projects.NETCore\SealWebServer\Controllers");
            ConvertDirectory(rootDir + @"Projects\SealWebServer\Models", rootDir + @"Projects.NETCore\SealWebServer\Models");

            ConvertDirectory(rootDir + @"Projects\SealWebServer\Views\Home", rootDir + @"Projects.NETCore\SealWebServer\Views\Home");
            ConvertDirectory(rootDir + @"Projects\SealWebServer\Content", rootDir + @"Projects.NETCore\SealWebServer\wwwroot\Content");
            ConvertDirectory(rootDir + @"Projects\SealWebServer\Scripts", rootDir + @"Projects.NETCore\SealWebServer\wwwroot\Scripts");
            ConvertDirectory(rootDir + @"Projects\SealWebServer\Images", rootDir + @"Projects.NETCore\SealWebServer\wwwroot\Images");
            ConvertDirectory(rootDir + @"Projects\SealWebServer\Fonts", rootDir + @"Projects.NETCore\SealWebServer\wwwroot\Fonts");
        }

        public static void ConvertDirectory(string source, string destination, string filter = "*.*")
        {
            foreach (string file in Directory.GetFiles(source, filter))
            {
                var destinationPath = Path.Combine(destination, Path.GetFileName(file));
                if (Path.GetExtension(file) == ".cs")
                {
                    var code = ConvertCS(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file, Encoding.UTF8));
                    File.WriteAllText(destinationPath, code, Encoding.UTF8);
                }
                else if (Path.GetExtension(file) == ".css")
                {
                    var code = ConvertCSS(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file, Encoding.UTF8));
                    File.WriteAllText(destinationPath, code, Encoding.UTF8);
                }
                else if (Path.GetExtension(file) == ".cshtml")
                {
                    var code = ConvertCSHTML(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file, Encoding.UTF8));
                    File.WriteAllText(destinationPath, code, Encoding.UTF8);
                }
                else
                {
                    File.Copy(file, destinationPath, true);
                }
            }
        }


        static string ConvertCS(string fileName, string code)
        {
            string result = code;
            result = result.Replace("using System.Windows.Forms;\r\n", "");
            result = result.Replace("using System.Windows.Forms.Design;\r\n", "");
            result = result.Replace("using DynamicTypeDescriptor;\r\n", "");
            result = result.Replace("using Seal.Forms;\r\n", "");
            result = result.Replace("using System.Web.UI.WebControls;\r\n", "");
            result = result.Replace("using Ionic.Zip;\r\n", "");
            result = result.Replace("UpdateEditorAttributes();", "");
            result = result.Replace(", ITreeSort", "");

            result = result.Replace("Properties.Settings.Default.RepositoryPath", "\"\"");

            if (fileName == "RootEditor")
            {
                result = result.Replace("protected DynamicCustomTypeDescriptor _dctd", "protected object _dctd");
            }
            else if (fileName == "SecurityUser" || fileName == "WebHelper" || fileName.StartsWith("HomeController"))
            {
                result = result.Replace("using System.Web;", "using System.Web;\r\nusing Microsoft.AspNetCore.Http;");
                result = result.Replace("using System.Web.Mvc;", "using Microsoft.AspNetCore.Mvc;");
                result = result.Replace("HttpRequestBase", "HttpRequest");
                result = result.Replace("HttpResponseBase", "HttpResponse");
                result = result.Replace("Request.QueryString", "Request.Query");

                result = result.Replace(", JsonRequestBehavior.AllowGet", "");

                result = result.Replace("Request.Form[ReportExecution.HtmlId_navigation_parameters] != null", "Request.Form.ContainsKey(ReportExecution.HtmlId_navigation_parameters)");
            }

            var lines = Regex.Split(result, "\r\n|\r|\n");
            result = "";

            bool skipping = false;
            foreach (var line in lines)
            {
                var line2 = line.Trim();
                if (line2.EndsWith("!NETCore")) continue;

                if (!skipping && line2.StartsWith("#region Editor"))
                {
                    skipping = true;
                }
                if (skipping && line2.StartsWith("#endregion"))
                {
                    skipping = false;
                    continue;
                }

                if (!skipping)
                {
                    if (line2.StartsWith("[Category") ||
                        line2.StartsWith("[Editor") ||
                        line2.StartsWith("[Default") ||
                        line2.StartsWith("[ClassResource") ||
                        line2.StartsWith("[OutputCache") ||
                        line2.StartsWith("[TypeConverter") ||
                        line2.StartsWith("[DisplayName")) continue;
                    
                    if (line2.StartsWith("[XmlIgnore")) result += "        [XmlIgnore]\r\n";
                    else result += line + "\r\n";
                }
            }

            return result;
        }

        static string ConvertCSS(string fileName, string code)
        {
            string result = code;
            if (fileName == "sealweb")
            {
                result += "\r\n.not-dotnet-core { display: none !important; }\r\n";
            }
            return result;
        }

        static string ConvertCSHTML(string fileName, string code)
        {
            string result = code;
            if (fileName == "Main")
            {
                result = result.Replace("@Html.Partial", "@await Html.PartialAsync");
            }
            return result;
        }

    }
}
