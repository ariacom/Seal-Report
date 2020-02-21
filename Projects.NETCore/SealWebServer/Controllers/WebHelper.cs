using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SealWebServer.Controllers
{
    public class WebHelper
    {
        public static void WriteWebException(Exception ex, string detail)
        {
            var currentEx = ex;
            var message = new StringBuilder("Unexpected error:\r\n");
            try
            {
                while (currentEx != null)
                {
                    message.AppendFormat("\r\n{0}\r\n({1})\r\n", currentEx.Message, currentEx.StackTrace);
                    currentEx = currentEx.InnerException;
                }
                message.Append(detail);
            }
            catch { }
            Helper.WriteLogEntry("Seal Web Server", EventLogEntryType.Error, message.ToString());
        }

        public static void WriteLogEntryWeb(EventLogEntryType type, string message, params object[] args)
        {
            Helper.WriteLogEntry("Seal Web Server", type, message, args);
        }

        public static void WriteLogEntryWeb(EventLogEntryType type, string message, string detail, params object[] args)
        {
            Helper.WriteLogEntry("Seal Web Server", type, message + detail, args);
        }

        public static void WriteLogEntryWebDebug(string message, string detail)
        {
            Helper.WriteLogEntry("Seal Web Server", EventLogEntryType.Information, message + detail);
        }

    }
}
