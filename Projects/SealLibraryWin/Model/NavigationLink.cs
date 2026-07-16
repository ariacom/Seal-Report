//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//

using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;

namespace Seal.Model
{
    /// <summary>
    /// A link to handle a navigation from an element
    /// </summary>
    public class NavigationLink
    {
        /// <summary>
        /// Prefix used for a hyperlink navigation
        /// </summary>
        public const string HyperLinkPrefix = "HL:";
        /// <summary>
        /// Prefix used for a file download navigation
        /// </summary>
        public const string FileDownloadPrefix = "FD:";
        /// <summary>
        /// Prefix used for a report script navigation
        /// </summary>
        public const string ReportScriptPrefix = "RS:";
        /// <summary>
        /// Prefix used for a report execution navigation
        /// </summary>
        public const string ReportExecutionPrefix = "RE:";

        /// <summary>
        /// Type of the navigation link
        /// </summary>
        public NavigationType Type;
        /// <summary>
        /// Link href
        /// </summary>
        public string Href = "";
        /// <summary>
        /// Link href prefixed by the navigation type
        /// </summary>
        public string FullHref {
            get
            {
                if (Type == NavigationType.Hyperlink) return HyperLinkPrefix + Href;
                else if (Type == NavigationType.FileDownload) return FileDownloadPrefix + Href;
                else if (Type == NavigationType.ReportScript) return ReportScriptPrefix + Href;
                else if (Type == NavigationType.ReportExecution) return ReportExecutionPrefix + Href;
                return Href;
            }
        }
        /// <summary>
        /// Text of the link displayed to the user
        /// </summary>
        public string Text = "";
        /// <summary>
        /// Result cell from which the navigation is done
        /// </summary>
        public ResultCell Cell;
        /// <summary>
        /// Current report
        /// </summary>
        public Report Report;

        /// <summary>
        /// User defined tag that may be used in the Navigation Script to identify the link
        /// </summary>
        public string Tag = "";

        /// <summary>
        /// The result expected after the navigation script is executed
        /// For File Download: the file path 
        /// </summary>
        public string ScriptResult;

        /// <summary>
        /// Optional parameters that can be used in the navigation script (e.g. form values for a custom script)
        /// </summary>
        public NameValueCollection Parameters;

        /// <summary>
        /// Current Request that can be used in the Navigation Script (e.g. getting a file upload) 
        /// </summary>
        public HttpRequest Request;
    }
}
