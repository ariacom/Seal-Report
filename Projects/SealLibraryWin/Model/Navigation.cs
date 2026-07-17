//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
namespace Seal.Model
{
    /// <summary>
    /// A navigation for a given ReportExecution and NavigationLink
    /// </summary>
    public class Navigation
    {
        /// <summary>
        /// The report execution of the navigation
        /// </summary>
        public ReportExecution Execution;

        /// <summary>
        /// The link used to trigger the navigation
        /// </summary>
        public NavigationLink Link;
    }
}
