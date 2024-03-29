﻿//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using Microsoft.AspNetCore.Http;

namespace Seal.Model
{
    /// <summary>
    /// Main model used for the Web Report Server
    /// </summary>
    public class WebMainModel
    {
        /// <summary>
        /// Current Repository
        /// </summary>
        public Repository Repository;

        /// <summary>
        /// True is the Web Report Designer is used
        /// </summary>
        public bool HasEditor { get; set; }

        /// <summary>
        /// True is JavaScripts are minified
        /// </summary>
        public bool HasMinifiedScripts()
        {
#if MINIFIED
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Root path of the Web Report Server
        /// </summary>
        public string ServerPath = "";

        /// <summary>
        /// Base URL path of the Web Report Server
        /// </summary>
        public string BaseURL = "";

        /// <summary>
        /// Current HTTP request
        /// </summary>
        public HttpRequest Request;
    }
}
