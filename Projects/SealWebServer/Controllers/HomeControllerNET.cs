using System.Collections.Generic;
using Seal.Model;
using Seal.Helpers;
using System.IO;
using System.Web.Mvc;
using System.Web;
using System.Text;
using System.Security.Principal;
using System;
using System.Diagnostics;

namespace SealWebServer.Controllers
{
    /// <summary>
    /// Main Controller of the Web Report Server
    /// </summary>
    public partial class HomeController
    {
        private string getCookie(string name)
        {
            string result = "";
            if (Request.Cookies[name] != null)
            {
                result = Request.Cookies[name].Value;
            }
            return result;
        }
        private void setCookie(string name, string value)
        {
            var cookie = new HttpCookie(name, value);
            cookie.Expires = DateTime.Now.AddYears(2);
            cookie.HttpOnly = true;

            Response.Cookies.Add(cookie);
        }

        private string RequestUrl
        {
            get
            {
                return Request.Url.OriginalString;
            }
        }

        private ActionResult getFileResult(string path, Report report) 
        {
            var contentType = MimeMapping.GetMimeMapping(path);
            var result = new FilePathResult(path, contentType);
            if (contentType != "text/html")
            {
                if (report != null) result.FileDownloadName = Helper.CleanFileName(report.DisplayNameEx + Path.GetExtension(path));
                else result.FileDownloadName = Path.GetFileName(path);
            }

            return result;
        }

        /// <summary>
        /// Return the full Web URL
        /// </summary>
        public static string GetWebUrl(HttpRequestBase request, HttpResponseBase response)
        {
            var appPath = request.ApplicationPath + (request.ApplicationPath.EndsWith("/") ? "" : "/");
            if (!request.RequestContext.HttpContext.Session.IsCookieless) return appPath;
            return response.ApplyAppPathModifier(appPath);
        }

        private string getContextDetail(HttpRequestBase request, SecurityUser user)
        {
            var result = new StringBuilder("\r\n");
            try
            {
                if (user != null) result.AppendFormat("User: '{0}'; Groups: '{1}'; Windows User: '{2}'\r\n", user.Name, user.SecurityGroupsDisplay, WindowsIdentity.GetCurrent().Name);
                if (request != null)
                {
                    result.AppendFormat("URL: '{0}'\r\n", request.Url.OriginalString);
                    if (request.RequestContext != null && request.RequestContext.HttpContext != null) result.AppendFormat("Session: '{0}'\r\n", request.RequestContext.HttpContext.Session.SessionID);
                    result.AppendFormat("IP: '{0}'\r\n", getIPAddress(request));
                    if (request.Form.Count > 0) foreach (string key in request.Form.Keys) result.AppendFormat("{0}={1}\r\n", key, key.ToLower() == "password" ? "********" : request.Form[key]);
                    if (request.QueryString.Count > 0) foreach (string key in request.QueryString.Keys) result.AppendFormat("{0}={1}\r\n", key, key.ToLower() == "password" ? "********" : request.QueryString[key]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return result.ToString();
        }

        string getIPAddress(HttpRequestBase request)
        {
            string ipAddress = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(ipAddress))
            {
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                {
                    return addresses[0];
                }
            }
            return request.ServerVariables["REMOTE_ADDR"];
        }

        void writeDebug(string message)
        {
            if (MvcApplication.DebugMode)
            {
                Debug.WriteLine(message);
                WebHelper.WriteLogEntryWebDebug(message, getContextDetail(Request, WebUser));
            }
        }

        private object getSessionValue(string key)
        {
            return Session[key];
        }

        private void setSessionValue(string key, object value)
        {
            Session[key] = value;
        }
    }
}
