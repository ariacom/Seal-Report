//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Seal.Model;
using System.Data;
using Seal.Helpers;
using System.Threading;
using System.IO;
using Microsoft.Win32.TaskScheduler;
using System.Diagnostics;

namespace Seal.Forms
{
    public class ToolsHelper
    {
        private Report _report = null;
        public Report Report
        {
            get { return _report; }
            set { _report = value; EnableControls(); }
        }
        private MetaSource _source = null;
        public MetaSource Source
        {
            get { return _source; }
            set { _source = value; EnableControls(); }
        }

        public IEntityHandler EntityHandler = null;

        ToolStripMenuItem _checkSource = new ToolStripMenuItem() { Text = "Check Data Sources...", ToolTipText = "Check all data source definitions with the objects in the database", AutoToolTip = true };
        ToolStripMenuItem _refreshEnum = new ToolStripMenuItem() { Text = "Refresh Enumerated Lists...", ToolTipText = "Refresh all the dynamic enmerated list values from the database", AutoToolTip = true };
        ToolStripMenuItem _checkExecution = new ToolStripMenuItem() { Text = "Check Report Executions...", ToolTipText = "Check all the reports in the repository", AutoToolTip = true };
        ToolStripMenuItem _exportSourceTranslations = new ToolStripMenuItem() { Text = "Export Data Source translations in CSV...", ToolTipText = "Export all translations found in the Data Source into a CSV file.", AutoToolTip = true };
        ToolStripMenuItem _exportReportsTranslations = new ToolStripMenuItem() { Text = "Export Folders, Reports and Dashboards translations in CSV...", ToolTipText = "Export all report and folders translations found in the repository into a CSV file.", AutoToolTip = true };
        ToolStripMenuItem _synchronizeSchedules = new ToolStripMenuItem() { Text = "Synchronize Report Schedules...", ToolTipText = "Parse all reports in the repository and and synchronize their schedules with their definition in the Windows Task Scheduler", AutoToolTip = true };
        ToolStripMenuItem _synchronizeSchedulesCurrentUser = new ToolStripMenuItem() { Text = "Synchronize Report Schedules with the logged user...", ToolTipText = "Parse all reports in the repository and and synchronize their schedules with their definition in the Windows Task Scheduler using the current logged user", AutoToolTip = true };
        ToolStripMenuItem _executeDesigner = new ToolStripMenuItem() { Text = Repository.SealRootProductName + " Report Designer", ToolTipText = "run the Report Designer application", AutoToolTip = true, ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D))), ShowShortcutKeys = true };
        ToolStripMenuItem _executeManager = new ToolStripMenuItem() { Text = Repository.SealRootProductName + " Server Manager", ToolTipText = "run the Server Manager application", AutoToolTip = true, ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.M))), ShowShortcutKeys = true };
        ToolStripMenuItem _openReportFolder = new ToolStripMenuItem() { Text = "Open Repository Reports Folder", ToolTipText = "open the Reports repository folder in Windows Explorer", AutoToolTip = true };
        ToolStripMenuItem _viewWidgetList = new ToolStripMenuItem() { Text = "List Widgets published in the repository", ToolTipText = "view all reports having Widgets published in the repository", AutoToolTip = true };

        public void InitHelpers(ToolStripMenuItem toolsMenuItem, bool forDesigner)
        {
            _checkSource.Click += tools_Click;
            toolsMenuItem.DropDownItems.Add(_checkSource);

            _refreshEnum.Click += tools_Click;
            toolsMenuItem.DropDownItems.Add(_refreshEnum);

            if (!forDesigner)
            {
                toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());

                if (Helper.IsMachineAdministrator())
                {
                    _synchronizeSchedules.Click += tools_Click;
                    toolsMenuItem.DropDownItems.Add(_synchronizeSchedules);
                }

                _synchronizeSchedulesCurrentUser.Click += tools_Click;
                toolsMenuItem.DropDownItems.Add(_synchronizeSchedulesCurrentUser);

                toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());

                _checkExecution.Click += tools_Click;
                toolsMenuItem.DropDownItems.Add(_checkExecution);
            }

            _viewWidgetList.Click += tools_Click;
            toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());
            toolsMenuItem.DropDownItems.Add(_viewWidgetList);

            if (!forDesigner)
            {
                toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());

                _exportSourceTranslations.Click += tools_Click;
                toolsMenuItem.DropDownItems.Add(_exportSourceTranslations);

                _exportReportsTranslations.Click += tools_Click;
                toolsMenuItem.DropDownItems.Add(_exportReportsTranslations);
            }

            _executeManager.Click += tools_Click;
            _executeDesigner.Click += tools_Click;
            _openReportFolder.Click += tools_Click;

            toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());
            toolsMenuItem.DropDownItems.Add(forDesigner ? _executeManager : _executeDesigner);
            toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());
            toolsMenuItem.DropDownItems.Add(_openReportFolder);
        }

        public void EnableControls()
        {
            _exportSourceTranslations.Enabled = Source != null;
            _checkSource.Enabled = Report != null || Source != null;
            _refreshEnum.Enabled = _checkSource.Enabled;
        }


        bool _setModified = false;
        void tools_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                _setModified = false;
                List<MetaSource> sources = new List<MetaSource>();

                if (Report != null) sources.AddRange(Report.Sources);
                else if (Source != null) sources.Add(Source);

                Thread thread = null;
                if (sender == _checkSource)
                {
                    thread = new Thread(delegate (object param) { CheckDataSources((ExecutionLogInterface)param, sources); });
                }
                else if (sender == _refreshEnum)
                {
                    thread = new Thread(delegate (object param) { RefreshEnums((ExecutionLogInterface)param, sources); });
                }
                else if (sender == _checkExecution)
                {
                    thread = new Thread(delegate (object param) { CheckExecutions((ExecutionLogInterface)param); });
                }
                else if (sender == _exportSourceTranslations)
                {
                    thread = new Thread(delegate (object param) { ExportSourceTranslations((ExecutionLogInterface)param); });
                }
                else if (sender == _exportReportsTranslations)
                {
                    thread = new Thread(delegate (object param) { ExportReportsTranslations((ExecutionLogInterface)param); });
                }
                else if (sender == _synchronizeSchedules || sender == _synchronizeSchedulesCurrentUser)
                {
                    if (!Helper.CheckTaskSchedulerOS()) return;
                    thread = new Thread(delegate (object param) { SynchronizeSchedules((ExecutionLogInterface)param, sender == _synchronizeSchedulesCurrentUser); });
                }
                else if (sender == _executeManager || sender == _executeDesigner)
                {
                    string exe = (sender == _executeManager ? Repository.SealServerManager : Repository.SealReportDesigner);
                    string path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), exe);
#if DEBUG
                    path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) + string.Format(@"\..\..\..\{0}\bin\Debug", Path.GetFileNameWithoutExtension(exe)), exe);
#endif
                    Process.Start(path);
                }
                else if (sender == _openReportFolder)
                {
                    Process.Start(Repository.Instance.ReportsFolder);
                }
                else if (sender == _viewWidgetList)
                {
                    thread = new Thread(delegate (object param) { ViewWidgetsList((ExecutionLogInterface)param); });
                }

                if (thread != null)
                {
                    ExecutionForm frm = new ExecutionForm(thread);
                    frm.ShowDialog();
                }

                if (_setModified && EntityHandler != null) EntityHandler.SetModified();
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }


        public void CheckDataSources(ExecutionLogInterface log, List<MetaSource> sources)
        {
            int errorCount = 0;
            StringBuilder errorSummary = new StringBuilder("");
            try
            {
                log.Log("Starting check Data Sources\r\n");

                foreach (MetaSource source in sources.OrderBy(i => i.Name))
                {
                    if (log.IsJobCancelled()) return;
                    log.Log("Checking data source '{0}'", source.Name);

                    log.Log("Checking Connections...");
                    int cnt = 0;
                    foreach (MetaConnection item in source.Connections.OrderBy(i => i.Name))
                    {
                        if (source.IsNoSQL && !item.ConnectionString.ToLower().Contains("provider=")) continue;

                        if (log.IsJobCancelled()) return;
                        log.LogNoCR("Checking connection '{0}':", item.Name);
                        item.CheckConnection();
                        cnt++;
                        if (!string.IsNullOrEmpty(item.Error))
                        {
                            errorCount++;
                            log.LogRaw("ERROR\r\n");
                            log.Log(item.Error);
                            errorSummary.AppendFormat("\r\n[{2}] Connection '{0}': {1}\r\n", item.Name, item.Error, source.Name);
                        }
                        else log.LogRaw("OK\r\n");
                    }
                    log.Log("Connections: {0} Connection(s) checked\r\n", cnt);

                    log.Log("Checking Tables...");
                    foreach (MetaTable item in source.MetaData.Tables.OrderBy(i => i.Name))
                    {
                        if (log.IsJobCancelled()) return;
                        log.LogNoCR("Checking table '{0}':", item.DisplayName);
                        item.CheckTable(null);
                        if (!string.IsNullOrEmpty(item.Error))
                        {
                            errorCount++;
                            log.LogRaw("ERROR\r\n");
                            log.Log(item.Error);
                            errorSummary.AppendFormat("\r\n[{2}] Table '{0}': {1}\r\n", item.DisplayName, item.Error, source.Name);
                        }
                        else log.LogRaw("OK\r\n");
                    }
                    log.Log("Tables: {0} Table(s) checked\r\n", source.MetaData.Tables.Count);

                    log.Log("Checking Joins...");
                    foreach (MetaJoin item in source.MetaData.Joins.OrderBy(i => i.Name))
                    {
                        if (log.IsJobCancelled()) return;
                        log.LogNoCR("Checking Join '{0}':", item.Name);
                        item.CheckJoin();
                        if (!string.IsNullOrEmpty(item.Error))
                        {
                            errorCount++;
                            log.LogRaw("ERROR\r\n");
                            log.Log(item.Error);
                            errorSummary.AppendFormat("\r\n[{2}] Join '{0}': {1}\r\n", item.Name, item.Error, source.Name);
                        }
                        else log.LogRaw("OK\r\n");
                    }
                    log.Log("Joins: {0} Join(s) checked\r\n", source.MetaData.Joins.Count);

                    log.Log("Checking Enums...");
                    foreach (MetaEnum item in source.MetaData.Enums.OrderBy(i => i.Name))
                    {
                        if (log.IsJobCancelled()) return;
                        log.LogNoCR("Checking Enum '{0}':", item.Name);
                        item.RefreshEnum(true);
                        if (!string.IsNullOrEmpty(item.Error))
                        {
                            errorCount++;
                            log.LogRaw("ERROR\r\n");
                            log.Log(item.Error);
                            errorSummary.AppendFormat("\r\n[{2}] Enum '{0}': {1}\r\n", item.Name, item.Error, source.Name);
                        }
                        else log.LogRaw("OK\r\n");
                    }
                    log.Log("Enums: {0} Enum(s) checked\r\n", source.MetaData.Enums.Count);
                }

                log.Log("{0} Data Source(s) checked\r\n", sources.Count);

            }
            catch (Exception ex)
            {
                log.Log("\r\n[UNEXPECTED ERROR RECEIVED]\r\n{0}\r\n", ex.Message);
            }
            log.Log("Check Data Sources terminated\r\n");

            if (errorCount > 0)
            {
                log.Log("SUMMARY: {0} Error(s) detected.\r\n{1}", errorCount, errorSummary);
            }
            else
            {
                log.Log("Youpi, pas d'erreur !");
            }
        }

        public void RefreshEnums(ExecutionLogInterface log, List<MetaSource> sources)
        {
            int errorCount = 0;
            StringBuilder errorSummary = new StringBuilder("");
            try
            {
                log.Log("Starting Refresh Enumerated Lists\r\n");

                foreach (MetaSource source in sources.OrderBy(i => i.Name))
                {
                    log.Log("Processing data source '{0}'", source.Name);
                    foreach (MetaEnum item in source.MetaData.Enums.OrderBy(i => i.Name).Where(i => i.IsDynamic && (i.IsEditable || i.IsDbRefresh)))
                    {
                        if (log.IsJobCancelled()) return;
                        log.LogNoCR("Refreshing Enum '{0}':", item.Name);
                        item.RefreshEnum(false);
                        if (!string.IsNullOrEmpty(item.Error))
                        {
                            errorCount++;
                            log.LogRaw("ERROR\r\n");
                            log.Log(item.Error);
                            errorSummary.AppendFormat("\r\n[{2}] Enum '{0}': {1}\r\n", item.Name, item.Error, source.Name);
                        }
                        else
                        {
                            log.LogRaw("OK\r\n");
                            _setModified = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Log("\r\n[UNEXPECTED ERROR RECEIVED]\r\n{0}\r\n", ex.Message);
            }
            log.Log("Refresh Enumerated Lists terminated\r\n");

            if (errorCount > 0) log.Log("SUMMARY: {0} Error(s) detected.\r\n{1}", errorCount, errorSummary);
            else log.Log("Youpi, pas d'erreur !");
        }


        void checkExecutions(ExecutionLogInterface log, string folder, Repository repository, ref int count, ref int errorCount, StringBuilder errorSummary)
        {
            log.Log("Checking folder '{0}'", folder);
            foreach (string reportPath in Directory.GetFiles(folder, "*." + Repository.SealReportFileExtension))
            {
                try
                {
                    if (log.IsJobCancelled()) return;
                    log.Log("Checking report '{0}'", reportPath);
                    count++;
                    Report report = Report.LoadFromFile(reportPath, repository);
                    if (!string.IsNullOrEmpty(report.LoadErrors)) throw new Exception(string.Format("Error loading the report: {0}", report.LoadErrors));
                    report.CheckingExecution = true;
                    if (report.Tasks.Count > 0) log.Log("Warning: Report Task executions are skipped.");
                    foreach (ReportView view in report.Views)
                    {
                        if (log.IsJobCancelled()) return;
                        log.Log("Running report with view '{0}'", view.Name);
                        try
                        {
                            report.CurrentViewGUID = view.GUID;
                            ReportExecution reportExecution = new ReportExecution() { Report = report };
                            reportExecution.Execute();

                            int cnt = 120;
                            while (--cnt > 0 && report.IsExecuting && !log.IsJobCancelled())
                            {
                                Thread.Sleep(1000);
                            }

                            if (report.IsExecuting)
                            {
                                if (cnt == 0) log.Log("Warning: Report is running for more than 2 minutes. Cancelling the execution...");
                                report.CancelExecution();
                            }

                            if (!string.IsNullOrEmpty(report.ExecutionErrors)) throw new Exception(report.ExecutionErrors);
                            if (!string.IsNullOrEmpty(report.ExecutionView.Error)) throw new Exception(report.ExecutionView.Error);

                            report.RenderOnly = true;
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            log.LogRaw("ERROR\r\n");
                            log.Log(ex.Message);
                            errorSummary.AppendFormat("\r\nReport '{0}' View '{1}': {2}\r\n", reportPath, view.Name, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    log.LogRaw("ERROR\r\n");
                    log.Log(ex.Message);
                    errorSummary.AppendFormat("\r\nReport '{0}': {1}\r\n", reportPath, ex.Message);
                }
            }
            log.LogRaw("\r\n");

            foreach (string subFolder in Directory.GetDirectories(folder))
            {
                if (log.IsJobCancelled()) return;
                checkExecutions(log, subFolder, repository, ref count, ref errorCount, errorSummary);
            }
        }

        public void CheckExecutions(ExecutionLogInterface log)
        {
            int count = 0, errorCount = 0;
            StringBuilder errorSummary = new StringBuilder("");

            Repository repository = Repository.Instance.CreateFast();
            try
            {
                log.Log("Starting Check Report Executions\r\n");
                checkExecutions(log, repository.ReportsFolder, repository, ref count, ref errorCount, errorSummary);
                log.Log("Checking personal folders\r\n");
                checkExecutions(log, repository.PersonalFolder, repository, ref count, ref errorCount, errorSummary);
            }
            catch (Exception ex)
            {
                log.Log("\r\n[UNEXPECTED ERROR RECEIVED]\r\n{0}\r\n", ex.Message);
            }
            log.Log("Check Report Executions terminated\r\n");

            log.Log("SUMMARY: {0} Report(s) checked, {1} Error(s) detected.\r\n{2}", count, errorCount, errorSummary);
            if (errorCount == 0) log.Log("Youpi, pas d'erreur !");

        }

        string initTranslationFile(StringBuilder translations, string separator, Repository repository)
        {
            translations.AppendFormat("Context{0}Instance{0}Reference", separator);
            //Add header
            string extraSeparators = "";
            if (repository.Translations.Count > 0)
            {
                foreach (var lang in repository.Translations[repository.Translations.Keys.First()].Translations)
                {
                    translations.AppendFormat("{0}{1}", separator, lang.Key);
                    extraSeparators += separator;
                }
            }
            else
            {
                translations.AppendFormat("{0}en{0}fr", separator);
                extraSeparators += separator + separator;
            }
            translations.AppendLine();
            return extraSeparators;
        }

        public void ExportSourceTranslations(ExecutionLogInterface log)
        {
            Repository repository = Repository.Instance.CreateFast();
            StringBuilder translations = new StringBuilder();
            try
            {
                log.Log("Starting the export of the Data Source translations for '{0}'\r\n", Source.Name);
                string separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                string extraSeparators = initTranslationFile(translations, separator, repository);

                log.Log("Adding elements names in context: Element\r\n");
                foreach (var table in Source.MetaData.Tables)
                {
                    foreach (var element in table.Columns)
                    {
                        translations.AppendFormat("Element{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(element.Category + '.' + element.DisplayName), Helper.QuoteDouble(element.DisplayName), extraSeparators);
                    }
                }

                log.Log("Adding enum values in context: Enum\r\n");
                foreach (var enumList in Source.MetaData.Enums.Where(i => i.Translate))
                {
                    foreach (var enumVal in enumList.Values)
                    {
                        translations.AppendFormat("Enum{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(enumList.Name), Helper.QuoteDouble(enumVal.DisplayValue), extraSeparators);
                        if (enumVal.DisplayValue != enumVal.DisplayRestriction) translations.AppendFormat("Enum{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(enumList.Name), Helper.QuoteDouble(enumVal.DisplayRestriction), extraSeparators);

                    }
                }

                log.Log("Adding Sub-Report names in context: SubReport\r\n");
                foreach (var table in Source.MetaData.Tables)
                {
                    foreach (var element in table.Columns)
                    {
                        foreach (var subReport in element.SubReports)
                        {
                            translations.AppendFormat("SubReport{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(element.Category + '.' + element.DisplayName), subReport.Name, extraSeparators);
                        }
                    }
                }

                string fileName = FileHelper.GetUniqueFileName(Path.Combine(repository.SettingsFolder, Helper.CleanFileName(string.Format("RepositoryTranslations_{0}_WORK.csv", Source.Name))));
                File.WriteAllText(fileName, translations.ToString(), Encoding.UTF8);

                log.Log("\r\nExport of the Data Source translations terminated.\r\n\r\nThe file has been saved to '{0}' and can be re-worked and merged with the repository translations file.\r\n\r\nNote that the effective repository translations file is 'RepositoryTranslations.csv' in the Repository Sub-Folder 'Settings'.", fileName);

                Process.Start(fileName);
            }
            catch (Exception ex)
            {
                log.Log("\r\n[UNEXPECTED ERROR RECEIVED]\r\n{0}\r\n", ex.Message);
            }
        }

        void exportReportNamesTranslations(string path, StringBuilder translations, string separator, string extraSeparators, int len)
        {
            foreach (var fileName in Directory.GetFiles(path))
            {
                translations.AppendFormat("FileName{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(fileName.Substring(len)), Helper.QuoteDouble(Path.GetFileNameWithoutExtension(fileName)), extraSeparators);
            }

            foreach (string subdir in Directory.GetDirectories(path))
            {
                exportReportNamesTranslations(subdir, translations, separator, extraSeparators, len);
            }
        }

        void exportFolderNamesTranslations(string path, StringBuilder translations, string separator, string extraSeparators, int len)
        {
            foreach (string subdir in Directory.GetDirectories(path))
            {
                translations.AppendFormat("FolderName{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(subdir.Substring(len)), Helper.QuoteDouble(Path.GetFileName(subdir)), extraSeparators);
                translations.AppendFormat("FolderPath{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(subdir.Substring(len)), Helper.QuoteDouble(subdir.Substring(len)), extraSeparators);
                exportFolderNamesTranslations(subdir, translations, separator, extraSeparators, len);
            }
        }

        void exportViewsTranslations(ExecutionLogInterface log, ReportView view, Repository repository, StringBuilder translations, string reportPath, string separator, string extraSeparators, int len)
        {
            translations.AppendFormat("ReportViewName{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(reportPath.Substring(len)), Helper.QuoteDouble(view.Name), extraSeparators);
            if (view.WidgetDefinition.IsPublished)
            {
                translations.AppendFormat("WidgetName{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(reportPath.Substring(len)), Helper.QuoteDouble(view.WidgetDefinition.Name), extraSeparators);
                if (!string.IsNullOrEmpty(view.WidgetDefinition.Description)) translations.AppendFormat("WidgetDescription{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(reportPath.Substring(len)), Helper.QuoteDouble(view.WidgetDefinition.Description), extraSeparators);
            }
            foreach (var child in view.Views)
            {
                exportViewsTranslations(log, child, repository, translations, reportPath, separator, extraSeparators, len);
            }
        }

        void exportReportsTranslations(ExecutionLogInterface log, string folder, Repository repository, StringBuilder translations, string separator, string extraSeparators, int len)
        {
            foreach (string reportPath in Directory.GetFiles(folder, "*." + Repository.SealReportFileExtension))
            {
                try
                {
                    if (log.IsJobCancelled()) return;
                    Report report = Report.LoadFromFile(reportPath, repository);
                    translations.AppendFormat("ReportDisplayName{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(reportPath.Substring(len)), Helper.QuoteDouble(report.ExecutionName), extraSeparators);
                    foreach (var view in report.Views)
                    {
                        exportViewsTranslations(log, view, repository, translations, reportPath, separator, extraSeparators, len);
                    }
                    foreach (var output in report.Outputs)
                    {
                        translations.AppendFormat("ReportOutputName{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(reportPath.Substring(len)), Helper.QuoteDouble(output.Name), extraSeparators);
                    }
                }
                catch (Exception ex)
                {
                    log.LogRaw("ERROR loading report {0}\r\n", reportPath);
                    log.Log(ex.Message);
                }
            }

            foreach (string subFolder in Directory.GetDirectories(folder))
            {
                if (log.IsJobCancelled()) return;
                exportReportsTranslations(log, subFolder, repository, translations, separator, extraSeparators, len);
            }
        }

        public void ExportReportsTranslations(ExecutionLogInterface log)
        {
            Repository repository = Repository.Instance.CreateFast();
            StringBuilder translations = new StringBuilder();
            try
            {
                log.Log("Starting the export of the Folders, Reports and Dasboards translations\r\n");
                string separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                string extraSeparators = initTranslationFile(translations, separator, repository);

                log.Log("Adding texts in context: FolderName, FolderPath\r\n");
                exportFolderNamesTranslations(repository.ReportsFolder, translations, separator, extraSeparators, repository.ReportsFolder.Length);

                log.Log("Adding file names in context: FileName\r\n");
                exportReportNamesTranslations(repository.ReportsFolder, translations, separator, extraSeparators, repository.ReportsFolder.Length);

                log.Log("Adding texts in context: ReportExecutionName, ReportViewName, ReportOutputName, WidgetName, WidgetDescription\r\n");
                exportReportsTranslations(log, repository.ReportsFolder, repository, translations, separator, extraSeparators, repository.ReportsFolder.Length);

                log.Log("Adding texts in context: DashboardFolder\r\n");
                foreach (var folder in Directory.GetDirectories(repository.DashboardPublicFolder))
                {
                    translations.AppendFormat("DashboardFolder{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(folder.Substring(repository.DashboardPublicFolder.Length)), Helper.QuoteDouble(Path.GetFileName(folder)), extraSeparators);
                }

                log.Log("Adding texts in context: DashboardName, DashboardItemName, DashboardItemGroupName\r\n");
                foreach (var folder in Directory.GetDirectories(repository.DashboardPublicFolder))
                {
                    foreach (var p in Directory.GetFiles(folder, "*." + Repository.SealDashboardExtension))
                    {
                        List<string> names = new List<string>();
                        List<string> groupNames = new List<string>();
                        var dashboard = Dashboard.LoadFromFile(p);
                        translations.AppendFormat("DashboardName{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(p.Substring(repository.DashboardPublicFolder.Length)), Helper.QuoteDouble(dashboard.Name), extraSeparators);

                        foreach(var item in dashboard.Items)
                        {
                            if (!string.IsNullOrEmpty(item.Name) && !names.Contains(item.GroupName)) names.Add(item.Name);
                            if (!string.IsNullOrEmpty(item.GroupName) && !groupNames.Contains(item.GroupName)) groupNames.Add(item.GroupName);
                        }

                        foreach (var name in names.OrderBy(i => i))
                        {
                            translations.AppendFormat("DashboardItemName{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(p.Substring(repository.DashboardPublicFolder.Length)), Helper.QuoteDouble(name), extraSeparators);
                        }

                        foreach (var groupName in groupNames.OrderBy(i => i))
                        {
                            translations.AppendFormat("DashboardItemGroupName{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(p.Substring(repository.DashboardPublicFolder.Length)), Helper.QuoteDouble(groupName), extraSeparators);
                        }

                    }
                }

                string fileName = FileHelper.GetUniqueFileName(Path.Combine(repository.SettingsFolder, "FoldersReportsTranslations_WORK.csv"));
                File.WriteAllText(fileName, translations.ToString(), Encoding.UTF8);

                log.Log("\r\nExport of the Folders and Reports translations terminated.\r\n\r\nThe file has been saved to '{0}' and can be re-worked and merged with the repository translations file.\r\n\r\nNote that the effective repository translations file is 'RepositoryTranslations.csv' in the Repository Sub-Folder 'Settings'.", fileName);

                Process.Start(fileName);
            }
            catch (Exception ex)
            {
                log.Log("\r\n[UNEXPECTED ERROR RECEIVED]\r\n{0}\r\n", ex.Message);
            }
        }
        void SynchronizeSchedules(ExecutionLogInterface log, string folder, Repository repository, ref int count, ref int errorCount, StringBuilder errorSummary, bool useCurrentUser)
        {
            log.Log("Checking folder '{0}'", folder);
            foreach (string reportPath in Directory.GetFiles(folder, "*." + Repository.SealReportFileExtension))
            {
                try
                {
                    if (log.IsJobCancelled()) return;
                    count++;
                    Report report = Report.LoadFromFile(reportPath, repository);
                    report.SchedulesWithCurrentUser = useCurrentUser;
                    if (report.Schedules.Count > 0)
                    {
                        log.Log("Synchronizing schedules for report '{0}'", reportPath);
                        foreach (ReportSchedule schedule in report.Schedules)
                        {
                            if (log.IsJobCancelled()) return;
                            log.Log("Checking schedule '{0}'", schedule.Name);
                            try
                            {
                                Task task = schedule.FindTask();
                                if (task != null) schedule.SynchronizeTask();
                                else
                                {
                                    log.Log("Creating task for '{0}'", schedule.Name);
                                    schedule.SynchronizeTask();
                                }
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                log.LogRaw("ERROR\r\n");
                                log.Log(ex.Message);
                                errorSummary.AppendFormat("\r\nReport '{0}' Schedule '{1}': {2}\r\n", reportPath, schedule.Name, ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    log.LogRaw("ERROR\r\n");
                    log.Log(ex.Message);
                    errorSummary.AppendFormat("\r\nReport '{0}': {1}\r\n", reportPath, ex.Message);
                }
            }

            foreach (string subFolder in Directory.GetDirectories(folder))
            {
                if (log.IsJobCancelled()) return;
                SynchronizeSchedules(log, subFolder, repository, ref count, ref errorCount, errorSummary, useCurrentUser);
            }

            log.LogRaw("\r\n");
        }

        public void SynchronizeSchedules(ExecutionLogInterface log, bool useCurrentUser)
        {
            int count = 0, errorCount = 0, taskDeleted = 0;
            StringBuilder errorSummary = new StringBuilder("");

            Repository repository = Repository.Instance.CreateFast();
            try
            {
                log.Log("Starting Report Schedules Synchronization\r\n");

                if (!Helper.IsMachineAdministrator() && !useCurrentUser) log.Log("WARNING: For this tool, we recommend to execute the 'Server Manager' application with the option 'Run as administrator'\r\n");

                SynchronizeSchedules(log, repository.ReportsFolder, repository, ref count, ref errorCount, errorSummary, useCurrentUser);
                log.Log("Checking personal folders\r\n");
                SynchronizeSchedules(log, repository.PersonalFolder, repository, ref count, ref errorCount, errorSummary, useCurrentUser);

                log.Log("Checking for Orphan tasks\r\n");

                TaskService taskService = new TaskService();
                TaskFolder taskFolder = taskService.RootFolder.SubFolders.FirstOrDefault(i => i.Name == repository.Configuration.TaskFolderName);
                if (taskFolder != null)
                {
                    foreach (Task task in taskFolder.GetTasks())
                    {
                        log.Log("Checking task '{0}'", task.Name);
                        try
                        {
                            string reportPath = ReportSchedule.GetTaskSourceDetail(task.Definition.RegistrationInfo.Source, 0);
                            string reportGUID = ReportSchedule.GetTaskSourceDetail(task.Definition.RegistrationInfo.Source, 1);
                            string scheduleGUID = ReportSchedule.GetTaskSourceDetail(task.Definition.RegistrationInfo.Source, 3);
                            Report report = ReportExecution.GetScheduledReport(taskFolder, reportPath, reportGUID, scheduleGUID, repository);
                            if (report != null)
                            {
                                ReportSchedule schedule = ReportExecution.GetReportSchedule(taskFolder, report, scheduleGUID);
                                if (schedule == null)
                                {
                                    taskDeleted++;
                                    log.Log("WARNING: Unable to find schedule '{0}' in report '{1}'. Task has been deleted.", scheduleGUID, report.FilePath);
                                }
                            }
                            else
                            {
                                taskDeleted++;
                                log.Log("WARNING: Unable to find report '{0}' for schedule '{1}'. Report tasks have been deleted.", reportGUID, scheduleGUID);
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            log.LogRaw("ERROR\r\n");
                            log.Log(ex.Message);
                            errorSummary.AppendFormat("\r\nTask '{0}': {1}\r\n", task.Name, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Log("\r\n[UNEXPECTED ERROR RECEIVED]\r\n{0}\r\n", ex.Message);
            }
            log.Log("Report Schedules Synchronization terminated\r\n");

            log.Log("SUMMARY: {0} Report(s) checked, {1} Task(s) deleted, {2} Error(s) detected.\r\n{3}", count, taskDeleted, errorCount, errorSummary);
            if (errorCount == 0) log.Log("Youpi, pas d'erreur !");

        }

        public static string SaveConfigurationFile(string fileFolder, string filePath, string entityName)
        {
            bool cont = true;
            string newPath = "";

            while (cont)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.InitialDirectory = fileFolder;
                dlg.FileName = (string.IsNullOrEmpty(filePath) ? entityName + "." + Repository.SealConfigurationFileExtension : Path.GetFileName(filePath));
                dlg.Filter = string.Format("Seal configuration files (*.{0})|*.{0}|All files (*.*)|*.*", Repository.SealConfigurationFileExtension);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (fileFolder.ToLower() != Path.GetDirectoryName(dlg.FileName).ToLower())
                    {
                        MessageBox.Show("The configuration file must remain in the same repository folder.\r\n", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        newPath = dlg.FileName;
                        cont = false;
                    }
                }
                else return null;
            }
            return newPath;
        }

        public void ViewWidgetsList(ExecutionLogInterface log)
        {
            Repository repository = Repository.Instance.CreateFast();
            StringBuilder translations = new StringBuilder();
            try
            {
                DashboardWidgetsPool.ForceReload();

                log.Log("Building the list of Published Widgets in the repository...\r\n");
                foreach (var path in (from w in DashboardWidgetsPool.Widgets.Values select w.ReportPath).Distinct().OrderBy(i => i))
                {
                    if (log.IsJobCancelled()) return;

                    Report report = Report.LoadFromFile(repository.ReportsFolder + path, repository);
                    StringBuilder summary = new StringBuilder();
                    foreach (var view in report.GetWidgetViews())
                    {
                        summary.AppendFormat("Widget '{0}' in View '{1}' of Type '{2}'\r\n", view.WidgetDefinition.Name, view.Name, view.TemplateName);
                    }
                    log.Log("Report: '{0}' ({1}):\r\n{2}", report.ExecutionName, path, summary);
                }

                if (DashboardWidgetsPool.Widgets.Count == 0) log.Log("No Widget published in this repository.\r\n");
            }
            catch (Exception ex)
            {
                log.Log("\r\n[UNEXPECTED ERROR RECEIVED]\r\n{0}\r\n", ex.Message);
            }
        }
    }
}
