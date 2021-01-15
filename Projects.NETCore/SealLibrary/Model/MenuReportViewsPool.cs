//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
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

        static void getMenuReportViews(Dictionary<string, DateTime> reports, string folder, Repository repository)
        {
            foreach (string reportPath in Directory.GetFiles(folder, "*." + Repository.SealReportFileExtension))
            {
                try
                {
                    //Report did not change
                    if (reports.ContainsKey(reportPath) && reports[reportPath] == File.GetLastWriteTime(reportPath)) continue;

                    var reportStr = File.ReadAllText(reportPath);
                    if (!reportStr.Contains("<ShowInMenu>true</ShowInMenu>")) continue;

                    Report report = Report.LoadFromFile(reportPath, repository, false);
                    if (string.IsNullOrEmpty(report.LoadErrors))
                    {
                        //clean previous dashbaoards
                        _menuReportViews.RemoveAll(i => i.Report.FilePath == report.FilePath);

                        //then add again
                        foreach (ReportView view in report.Views.Where(i =>i.ShowInMenu))
                        {
                            _menuReportViews.Add(view);
                        }
                    }
                    else Debug.WriteLine(report.LoadErrors);

                    if (reports.ContainsKey(reportPath)) reports[reportPath] = report.LastModification;
                    else reports.Add(reportPath, report.LastModification);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            foreach (string subFolder in Directory.GetDirectories(folder))
            {
                getMenuReportViews(reports, subFolder, repository);
            }
        }
    }
}

