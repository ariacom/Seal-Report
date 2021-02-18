//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//

using System.Collections.Specialized;
using System.Web;

namespace Seal.Model
{
    /// <summary>
    /// A link to handle a navigation from an element
    /// </summary>
    public class NavigationLink
    {
        public const string HyperLinkPrefix = "HL:";
        public const string FileDownloadPrefix = "FD:";
        public const string ReportScriptPrefix = "RS:";
        public const string ReportExecutionPrefix = "RE:";

        public NavigationType Type;
        public string Href = "";
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
        public string Text = "";
        public ResultCell Cell;
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
#if !NETCOREAPP
        public HttpRequestBase Request;
#else
        public Microsoft.AspNetCore.Http.HttpRequest Request;
#endif
    }
}
