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
using System.Web.Mvc;

namespace SealWebServer.Controllers
{
    /// <summary>
    /// Helper to log messages from the HomeController
    /// </summary>
    public class WebHelper
    {
        const string WebServerLogEntry = "Seal Web Server";

        /// <summary>
        /// Log an Exception
        /// </summary>
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
            Helper.WriteLogEntry(WebServerLogEntry, EventLogEntryType.Error, message.ToString());
        }

        /// <summary>
        /// Log a message with optional parameters
        /// </summary>
        public static void WriteLogEntryWeb(EventLogEntryType type, string message, params object[] args)
        {
            Helper.WriteLogEntry(WebServerLogEntry, type, message, args);
        }

        /// <summary>
        /// Log a message with a detail
        /// </summary>
        public static void WriteLogEntryWebDetail(EventLogEntryType type, string message, string detail)
        {
            Helper.WriteLogEntry(WebServerLogEntry, type, message + detail);
        }

        /// <summary>
        /// Log a debug message with a detail
        /// </summary>
        public static void WriteLogEntryWebDebug(string message, string detail)
        {
            Helper.WriteLogEntry(WebServerLogEntry, EventLogEntryType.Information, message + detail);
        }

    }
}