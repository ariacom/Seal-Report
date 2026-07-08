//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Seal.Model
{
    /// <summary>
    /// Static object dedicated to manage the list of Menu reports in the repository
    /// </summary>
    public class MenuReportViewsPool
    {
        static Dictionary<string, DateTime> _reports;
        static List<ReportView> _menuReportViews;

        static bool _forceReload = false;

        static void reload()
        {
            if (_menuReportViews == null)
            {
                _reports = new Dictionary<string, DateTime>();
                _menuReportViews = new List<ReportView>();
            }

            lock (_menuReportViews)
            {
                var repository = Repository.Instance;
                if (repository.MustReload())
                {
                    repository = Repository.ReloadInstance();
                }

                getMenuReportViews(_reports, repository.PersonalFolder, repository);
                getMenuReportViews(_reports, repository.ReportsFolder, repository);
                //Remove reports deleted
                _menuReportViews.RemoveAll(i => !File.Exists(i.Report.FilePath));
                _forceReload = false;
            }
        }

        /// <summary>
        /// Force a browsing of all reports to reload the list
        /// </summary>
        public static void ForceReload()
        {
            _forceReload = true;
        }

        /// <summary>
        /// Dictionary containing the Menu reports in the repository 
        /// </summary>
        public static List<ReportView> MenuReportViews
        {
            get
            {
                if (_forceReload || _menuReportViews == null)
                {
                    reload();
                }
                return _menuReportViews;
            }
        }

        // ShowInMenu / MenuName have been removed from ReportView — this pool is no longer populated.
        static void getMenuReportViews(Dictionary<string, DateTime> reports, string folder, Repository repository)
        {
        }
    }
}
