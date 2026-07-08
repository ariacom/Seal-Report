//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
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
