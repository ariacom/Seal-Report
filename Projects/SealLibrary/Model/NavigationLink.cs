//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//

namespace Seal.Model
{
    public class NavigationLink
    {
        public const string HyperLinkPrefix = "HL:";
        public const string FileDownloadPrefix = "FD:";

        public NavigationType Type;
        public string Href = "";
        public string FullHref {
            get
            {
                if (Type == NavigationType.Hyperlink) return HyperLinkPrefix + Href;
                if (Type == NavigationType.FileDownload) return FileDownloadPrefix + Href;
                return Href;
            }
        }
        public string Text = "";
        public ResultCell Cell;

        /// <summary>
        /// User defined tag that may be used in the Navigation Script to identify the link
        /// </summary>
        public string Tag = "";

        /// <summary>
        /// The result expected after the navigation script is executed
        /// For File Download: the file path 
        /// </summary>
        public string ScriptResult;
    }
}
