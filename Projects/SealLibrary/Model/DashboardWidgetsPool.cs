//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Seal.Model
{
    public class DashboardWidgetsPool
    {
        static Dictionary<string, DashboardWidget> _widgets;
        static Dictionary<string, DateTime> _reports;

        static bool _forceReload = false;
        public static void ForceReload()
        {
            _forceReload = true;
        }

        public static Dictionary<string, DashboardWidget> Widgets
        {
            get
            {
                if (_forceReload || _widgets == null)
                {
                    if (_widgets == null)
                    {
                        _widgets = new Dictionary<string, DashboardWidget>();
                        _reports = new Dictionary<string, DateTime>();
                    }

                    lock (_widgets)
                    {
                        var repository = Repository.Instance;
                        getWidgets(_widgets, _reports, repository.ReportsFolder, repository);
                        //Remove reports deleted
                        var keys = _widgets.Keys.ToList();
                        foreach (var key in keys)
                        {
                            if (!File.Exists(repository.ReportsFolder + _widgets[key].ReportPath)) _widgets.Remove(key);
                        }

                        _forceReload = false;
                    }
                }
                return _widgets;
            }
        }

        static void getWidgets(Dictionary<string, DashboardWidget> widgets, Dictionary<string, DateTime> reports, ReportView view, Repository repository)
        {
            if (!string.IsNullOrEmpty(view.WidgetDefinition.GUID) && _widgets.ContainsKey(view.WidgetDefinition.GUID)) _widgets.Remove(view.WidgetDefinition.GUID);

            if (view.WidgetDefinition.IsPublished)
            {
                view.WidgetDefinition.ReportPath = view.Report.FilePath.Replace(repository.ReportsFolder, "");
                view.WidgetDefinition.ReportName = view.Report.DisplayNameEx;
                view.WidgetDefinition.LastModification = view.Report.LastModification;
                if (string.IsNullOrEmpty(view.WidgetDefinition.Description)) view.WidgetDefinition.Description = "";
                widgets.Add(view.WidgetDefinition.GUID, view.WidgetDefinition);
            }

            foreach (ReportView child in view.Views)
            {
                getWidgets(widgets, reports, child, repository);
            }
        }

        static void getWidgets(Dictionary<string, DashboardWidget> widgets, Dictionary<string, DateTime> reports, string folder, Repository repository)
        {
            foreach (string reportPath in Directory.GetFiles(folder, "*." + Repository.SealReportFileExtension))
            {
                try
                {
                    //Report did not change
                    if (reports.ContainsKey(reportPath) && reports[reportPath] == File.GetLastWriteTime(reportPath)) continue;

                    var reportStr = File.ReadAllText(reportPath);
                    if (!reportStr.Contains("<WidgetDefinition>")) continue;

                    Report report = Report.LoadFromFile(reportPath, repository);
                    if (string.IsNullOrEmpty(report.LoadErrors))
                    {
                        foreach (ReportView view in report.Views) getWidgets(widgets, reports, view, repository);
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
                getWidgets(widgets, reports, subFolder, repository);
            }
        }
    }
}
