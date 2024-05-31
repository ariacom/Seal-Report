//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
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
using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Style;

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
        ToolStripMenuItem _refreshEnum = new ToolStripMenuItem() { Text = "Refresh Enumerated lists...", ToolTipText = "Refresh all the dynamic enmerated list values from the database", AutoToolTip = true };
        ToolStripMenuItem _exportSourceTranslations = new ToolStripMenuItem() { Text = "Export Data Source translations in CSV...", ToolTipText = "Export all translations found in the Data Source into a CSV file.", AutoToolTip = true };
        ToolStripMenuItem _exportReportsTranslations = new ToolStripMenuItem() { Text = "Export Folders and Reports translations in CSV...", ToolTipText = "Export all report and folders translations found in the repository into a CSV file.", AutoToolTip = true };
        ToolStripMenuItem _importSourceObjectsExcel = new ToolStripMenuItem() { Text = "Import Objects from an Excel file...", ToolTipText = "Import Data Source Objects from an Excel file.", AutoToolTip = true };
        ToolStripMenuItem _exportSourceObjectsExcel = new ToolStripMenuItem() { Text = "Export Objects to an Excel file...", ToolTipText = "Export Data Source  Objects to an Excel file.", AutoToolTip = true };
        ToolStripMenuItem _importSourceObjects = new ToolStripMenuItem() { Text = "Import Objects from another Data Source file...", ToolTipText = "Import Data Source Objects from another Data Source file.", AutoToolTip = true };
        ToolStripMenuItem _synchronizeSchedules = new ToolStripMenuItem() { Text = "Synchronize Report Schedules...", ToolTipText = "Parse all reports in the repository and and synchronize their schedules with their definition in the Windows Task Scheduler", AutoToolTip = true };
        ToolStripMenuItem _synchronizeSchedulesCurrentUser = new ToolStripMenuItem() { Text = "Synchronize Report Schedules with the logged user...", ToolTipText = "Parse all reports in the repository and and synchronize their schedules with their definition in the Windows Task Scheduler using the current logged user", AutoToolTip = true };
        ToolStripMenuItem _executeDesigner = new ToolStripMenuItem() { Text = Repository.SealRootProductName + " Report Designer", ToolTipText = "run the Report Designer application", AutoToolTip = true, ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D))), ShowShortcutKeys = true };
        ToolStripMenuItem _executeManager = new ToolStripMenuItem() { Text = Repository.SealRootProductName + " Server Manager", ToolTipText = "run the Server Manager application", AutoToolTip = true, ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.M))), ShowShortcutKeys = true };
        ToolStripMenuItem _openReportFolder = new ToolStripMenuItem() { Text = "Open Repository Reports Folder", ToolTipText = "open the Reports repository folder in Windows Explorer", AutoToolTip = true };
        ToolStripMenuItem _openTempFolder = new ToolStripMenuItem() { Text = "Open Temporary Report Results Folder", ToolTipText = "open the folder where are generated report results in Windows Explorer", AutoToolTip = true };

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
            }

            if (!forDesigner)
            {
                toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());

                _exportSourceTranslations.Click += tools_Click;
                toolsMenuItem.DropDownItems.Add(_exportSourceTranslations);

                _exportReportsTranslations.Click += tools_Click;
                toolsMenuItem.DropDownItems.Add(_exportReportsTranslations);

                toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());

                _exportSourceObjectsExcel.Click += tools_Click;
                toolsMenuItem.DropDownItems.Add(_exportSourceObjectsExcel);

                _importSourceObjectsExcel.Click += tools_Click;
                toolsMenuItem.DropDownItems.Add(_importSourceObjectsExcel);

                _importSourceObjects.Click += tools_Click;
                toolsMenuItem.DropDownItems.Add(_importSourceObjects);
            }

            _executeManager.Click += tools_Click;
            _executeDesigner.Click += tools_Click;
            _openReportFolder.Click += tools_Click;
            _openTempFolder.Click += tools_Click;

            toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());
            toolsMenuItem.DropDownItems.Add(forDesigner ? _executeManager : _executeDesigner);
            toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());
            toolsMenuItem.DropDownItems.Add(_openReportFolder);
            toolsMenuItem.DropDownItems.Add(_openTempFolder);
        }

        public void EnableControls()
        {
            _exportSourceTranslations.Enabled = Source != null;
            _checkSource.Enabled = Report != null || Source != null;
            _refreshEnum.Enabled = _checkSource.Enabled;

            _importSourceObjectsExcel.Enabled = _exportSourceTranslations.Enabled;
            _exportSourceObjectsExcel.Enabled = _exportSourceTranslations.Enabled;
            _importSourceObjects.Enabled = _exportSourceTranslations.Enabled;
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
                else if (sender == _exportSourceTranslations)
                {
                    thread = new Thread(delegate (object param) { ExportSourceTranslations((ExecutionLogInterface)param); });
                }
                else if (sender == _exportReportsTranslations)
                {
                    thread = new Thread(delegate (object param) { ExportReportsTranslations((ExecutionLogInterface)param); });
                }
                else if (sender == _importSourceObjects)
                {
                    OpenFileDialog dlg = new OpenFileDialog();
                    dlg.Filter = string.Format(Repository.SealRootProductName + " Data Source files (*.{0})|*.{0}|All files (*.*)|*.*", Repository.SealConfigurationFileExtension);
                    dlg.Title = "Select a Data Source file";
                    dlg.CheckFileExists = true;
                    dlg.CheckPathExists = true;
                    dlg.InitialDirectory = Repository.Instance.SourcesFolder;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        if (dlg.FileName == Source.FilePath) throw new Exception("Error: This is the same Data Source file.");
                        var sourceFrom = MetaSource.LoadFromFile(dlg.FileName);

                        List<RootComponent> objects = new List<RootComponent>();
                        objects.AddRange(sourceFrom.Connections);
                        if (sourceFrom.IsSQL == Source.IsSQL)
                        {
                            objects.AddRange(sourceFrom.MetaData.Tables);
                            objects.AddRange(sourceFrom.MetaData.Joins);
                        }
                        objects.AddRange(sourceFrom.MetaData.Enums);

                        foreach (var obj in objects)
                        {
                            if (obj is MetaTable) obj.Name = "TABLE: " + obj.Name;
                            if (obj is MetaEnum) obj.Name = "ENUM: " + obj.Name;
                            if (obj is MetaJoin) obj.Name = "JOIN: " + obj.Name;
                            if (obj is MetaConnection) obj.Name = "CONNECTION: " + obj.Name;
                        }

                        MultipleSelectForm frm = new MultipleSelectForm("Please select objects to add or update", objects, "Name");
                        List<CheckBox> options = new List<CheckBox>();
                        var changeGUID = new CheckBox() { Text = "Generate new identifiers (GUID)", Checked = true, AutoSize = true };
                        options.Add(changeGUID);
                        var overwriteProperties = new CheckBox() { Text = "Overwrite existing table column properties", Checked = true, AutoSize = true, Enabled = false };
                        changeGUID.CheckStateChanged += new EventHandler(delegate (object sender2, EventArgs e2)
                        {
                            overwriteProperties.Enabled = !changeGUID.Checked;
                        });
                        options.Add(overwriteProperties);

                        int index = 10;
                        foreach (var checkbox in options)
                        {
                            checkbox.Location = new Point(index, 5);
                            frm.optionPanel.Controls.Add(checkbox);
                            index += checkbox.Width + 10;
                            frm.Width = Math.Max(index + 5, frm.Width);
                        }
                        frm.optionPanel.Visible = (options.Count > 0);
                        if (frm.ShowDialog() == DialogResult.OK)
                        {
                            foreach (var obj in objects) obj.Name = obj.Name.Substring(obj.Name.IndexOf(':') + 2);
                            EntityHandler.SetModified();

                            thread = new Thread(delegate (object param)
                            {
                                ImportSourceObjects((ExecutionLogInterface)param, frm.CheckedItems, changeGUID.Checked, overwriteProperties.Checked);
                            });
                        }
                    }
                }
                else if (sender == _importSourceObjectsExcel)
                {
                    OpenFileDialog dlg = new OpenFileDialog();
                    dlg.Filter = "Excel Source files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                    dlg.Title = "Select an Excel Source file";
                    dlg.CheckFileExists = true;
                    dlg.CheckPathExists = true;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        EntityHandler.SetModified();
                        thread = new Thread(delegate (object param)
                        {
                            ImportSourceObjectsExcel((ExecutionLogInterface)param, dlg.FileName);
                        });
                    }
                }
                else if (sender == _exportSourceObjectsExcel)
                {
                    var newSource = MetaSource.LoadFromFile(Source.FilePath);
                    newSource.InitReferences(Repository.Instance);
                    List<RootComponent> objects = new List<RootComponent>();
                    objects.AddRange(newSource.MetaData.Tables);
                    objects.AddRange(newSource.MetaData.Enums);

                    foreach (var obj in objects)
                    {
                        if (obj is MetaTable) obj.Name = "TABLES AND COLUMNS: " + obj.Name;
                        if (obj is MetaEnum) obj.Name = "ENUM VALUES: " + obj.Name;
                    }

                    MultipleSelectForm frm = new MultipleSelectForm("Please select objects to export", objects, "Name");
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        foreach (var obj in objects) obj.Name = obj.Name.Substring(obj.Name.IndexOf(':') + 2);

                        SaveFileDialog dlg = new SaveFileDialog();
                        dlg.Filter = "Excel Source files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                        dlg.FileName = Source.Name + ".xlsx";
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            thread = new Thread(delegate (object param)
                            {
                                ExportsSourceObjectsExcel((ExecutionLogInterface)param, frm.CheckedItems, dlg.FileName);
                            });
                        }
                    }
                }
                else if (sender == _synchronizeSchedules || sender == _synchronizeSchedulesCurrentUser)
                {
                    if (!Helper.CheckTaskSchedulerOS()) return;
                    thread = new Thread(delegate (object param)
                    {
                        SynchronizeSchedules((ExecutionLogInterface)param, sender == _synchronizeSchedulesCurrentUser);
                    });
                }
                else if (sender == _executeManager || sender == _executeDesigner)
                {
                    string exe = (sender == _executeManager ? Repository.SealServerManager : Repository.SealReportDesigner);
                    string path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), exe);
#if DEBUG
                    path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) + string.Format(@"\..\..\..\{0}\bin\Debug", Path.GetFileNameWithoutExtension(exe)), exe);
#endif
                    var p = new Process();
                    p.StartInfo = new ProcessStartInfo(path) { UseShellExecute = true };
                    p.Start();
                }
                else if (sender == _openReportFolder)
                {
                    var p = new Process();
                    p.StartInfo = new ProcessStartInfo(Repository.Instance.ReportsFolder) { UseShellExecute = true };
                    p.Start();
                }
                else if (sender == _openTempFolder)
                {
                    var p = new Process();
                    p.StartInfo = new ProcessStartInfo(FileHelper.TempApplicationDirectory) { UseShellExecute = true };
                    p.Start();
                }

                if (thread != null)
                {
                    ExecutionForm frm = new ExecutionForm(thread);
                    frm.ShowDialog();
                }

                if (_setModified && EntityHandler != null)
                {
                    EntityHandler.SetModified();

                    if (sender == _importSourceObjectsExcel || sender == _importSourceObjects)
                    {
                        Source.InitReferences(Source.Repository);
                        EntityHandler.InitEntity(Source);
                    }
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void ChangeGUID_CheckStateChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
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
                log.Log("Starting Refresh Enumerated lists\r\n");

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
            log.Log("Refresh Enumerated lists terminated\r\n");

            if (errorCount > 0) log.Log("SUMMARY: {0} Error(s) detected.\r\n{1}", errorCount, errorSummary);
            else log.Log("Youpi, pas d'erreur !");
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
            repository.ReloadRepositoryTranslations();
            StringBuilder translations = new StringBuilder();
            try
            {
                log.Log("Starting the export of the Data Source translations for '{0}'\r\n", Source.Name);
                string separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                string extraSeparators = initTranslationFile(translations, separator, repository);

                log.Log("Adding elements names in context: Connection\r\n");
                foreach (var connection in Source.Connections)
                {
                    var context = "Connection";
                    var instance = Source.Name + '.' + connection.Name;
                    var reference = connection.Name;
                    if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                    {
                        translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                    }
                }

                log.Log("Adding elements names in context: Element\r\n");
                foreach (var table in Source.MetaData.Tables)
                {
                    foreach (var element in table.Columns)
                    {
                        var context = "Element";
                        var instance = element.Category + '.' + element.DisplayName;
                        var reference = element.DisplayName;
                        if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                        {
                            translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                        }
                    }
                }

                log.Log("Adding elements names in context: Category\r\n");
                var categories = Source.MetaData.AllColumns.Select(i => i.Category).Distinct().ToList();
                var categoriesDone = new List<string>();
                foreach (var category in categories)
                {
                    var context = "Category";
                    var instance = category;
                    var reference = Path.GetFileName(category);
                    if (!categoriesDone.Contains(instance) && Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                    {
                        translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                    }
                    categoriesDone.Add(category);
                    //Add subfolders if any
                    instance = Path.GetDirectoryName(instance).Replace("\\","/");
                    while (!string.IsNullOrEmpty(instance))
                    {
                        reference = Path.GetFileName(instance);
                        if (!categoriesDone.Contains(instance) && Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                        {
                            translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                        }
                        categoriesDone.Add(instance);
                        instance = Path.GetDirectoryName(instance).Replace("\\", "/"); ;
                    }
                }

                log.Log("Adding enum messages in context: EnumMessage\r\n");
                foreach (var enumList in Source.MetaData.Enums.Where(i => i.Translate))
                {
                    if (!string.IsNullOrEmpty(enumList.Message))
                    {
                        var context = "EnumMessage";
                        var instance = enumList.Name;
                        var reference = enumList.Message;
                        if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                        {
                            translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                        }
                    }
                }

                log.Log("Adding enum values in context: Enum\r\n");
                foreach (var enumList in Source.MetaData.Enums.Where(i => i.Translate))
                {
                    foreach (var enumVal in (from v in enumList.Values select new { v.DisplayValue, v.DisplayRestriction }).Distinct())
                    {
                        var context = "Enum";
                        var instance = enumList.Name;
                        var reference = enumVal.DisplayValue;
                        if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                        {
                            translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                        }

                        if (enumVal.DisplayValue != enumVal.DisplayRestriction)
                        {
                            reference = enumVal.DisplayRestriction;
                            if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                            {
                                translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                            }
                        }

                    }
                }

                log.Log("Adding Sub-Report names in context: SubReport\r\n");
                foreach (var table in Source.MetaData.Tables)
                {
                    foreach (var element in table.Columns)
                    {
                        foreach (var subReport in element.SubReports)
                        {
                            var context = "SubReport";
                            var instance = element.Category + '.' + element.DisplayName;
                            var reference = subReport.Name;
                            if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                            {
                                translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                            }
                        }
                    }
                }

                string fileName = FileHelper.GetUniqueFileName(Path.Combine(repository.SettingsFolder, Helper.CleanFileName(string.Format("RepositoryTranslations_{0}_MISSINGS.csv", Source.Name))));
                File.WriteAllText(fileName, translations.ToString(), Encoding.UTF8);

                log.Log("\r\nExport of the missings Data Source translations terminated.\r\n\r\nThe file has been saved to '{0}' and can be re-worked and merged with the repository translations file.\r\n\r\nNote that the effective repository translations file is 'RepositoryTranslations.csv' (or 'RepositoryTranslations.xlsx')' in the Repository Sub-Folder 'Settings'.", fileName);

                var p = new Process();
                p.StartInfo = new ProcessStartInfo(fileName) { UseShellExecute = true };
                p.Start();
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
                var context = "FileName";
                var instance = fileName.Substring(len);
                var reference = Path.GetFileNameWithoutExtension(fileName);
                if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                {
                    translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                }
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
                var context = "FolderName";
                var instance = subdir.Substring(len);
                var reference = Path.GetFileName(subdir);
                if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                {
                    translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                }
                reference = subdir.Substring(len);
                if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                {
                    translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                }
                exportFolderNamesTranslations(subdir, translations, separator, extraSeparators, len);
            }
        }

        void exportViewsTranslations(ExecutionLogInterface log, ReportView view, Repository repository, StringBuilder translations, string reportPath, string separator, string extraSeparators, int len)
        {
            var context = "ReportViewName";
            var instance = reportPath.Substring(len);
            var reference = view.Name;
            if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
            {
                translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
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
                    var context = "ReportDisplayName";
                    var instance = reportPath.Substring(len);
                    var reference = report.ExecutionName;
                    if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                    {
                        translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                    }
                    foreach (var view in report.Views)
                    {
                        exportViewsTranslations(log, view, repository, translations, reportPath, separator, extraSeparators, len);
                    }
                    foreach (var output in report.Outputs)
                    {
                        context = "ReportOutputName";
                        reference = output.Name;
                        if (Repository.Instance.FindRepositoryTranslation(context, instance, reference) == null)
                        {
                            translations.AppendFormat("{4}{0}{1}{0}{2}{3}\r\n", separator, Helper.QuoteDouble(instance), Helper.QuoteDouble(reference), extraSeparators, context);
                        }
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

        public void ExportsSourceObjectsExcel(ExecutionLogInterface log, List<object> checkedItems, string path)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                var ep = new ExcelPackage();
                var items = checkedItems.Where(i => i is MetaTable);
                if (items.Count() > 0)
                {
                    log.Log("Processing tables.");
                    var ws = ep.Workbook.Worksheets.Add("Tables");
                    ws.Cells["A1"].Value = "Name";
                    ws.Cells["B1"].Value = "Column";
                    ws.Cells["C1"].Value = "Type";
                    ws.Cells["D1"].Value = "Category";
                    ws.Cells["E1"].Value = "Display Name";
                    ws.Cells["F1"].Value = "Display Order";
                    ws.Cells["G1"].Value = "Enum";
                    ws.Cells["H1"].Value = "CSS Class";
                    ws.Cells["I1"].Value = "CSS Style";
                    ws.Cells["J1"].Value = "Is Aggregate";
                    ws.Cells["K1"].Value = "Numeric Standard Format";
                    ws.Cells["L1"].Value = "DateTime Standard Format";
                    ws.Cells["M1"].Value = "Format";
                    ws.Cells["N1"].Value = "Tag";
                    ws.Cells["O1"].Value = "GUID";
                    int index = 2;
                    foreach (var item in items)
                    {
                        var table = item as MetaTable;
                        foreach (var col in table.Columns)
                        {
                            ws.Cells["A" + index].Value = table.Name;
                            ws.Cells["B" + index].Value = col.Name;
                            ws.Cells["C" + index].Value = col.Type;
                            ws.Cells["D" + index].Value = col.Category;
                            ws.Cells["E" + index].Value = col.DisplayName;
                            ws.Cells["F" + index].Value = col.DisplayOrder;
                            ws.Cells["G" + index].Value = col.Enum?.Name;
                            ws.Cells["H" + index].Value = col.CssClass;
                            ws.Cells["I" + index].Value = col.CssStyle;
                            ws.Cells["J" + index].Value = col.IsAggregate;
                            ws.Cells["K" + index].Value = col.Enum == null && col.Type == ColumnType.Numeric ? col.NumericStandardFormat : "";
                            ws.Cells["L" + index].Value = col.Enum == null && col.Type == ColumnType.DateTime ? col.DateTimeStandardFormat : "";
                            ws.Cells["M" + index].Value = col.Format;
                            ws.Cells["N" + index].Value = col.Tag;
                            ws.Cells["O" + index].Value = col.GUID;
                            index++;
                        }

                    }
                    ws.Cells["A1:O1"].AutoFilter = true;
                    ws.Cells["A1:O1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells["A1:O1"].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    ws.Cells["A1:O1"].Style.Font.Bold = true;
                }

                items = checkedItems.Where(i => i is MetaEnum);
                if (items.Count() > 0)
                {
                    log.Log("Processing enumerated lists.");
                    var ws = ep.Workbook.Worksheets.Add("Enums");
                    ws.Cells["A1"].Value = "Name";
                    ws.Cells["B1"].Value = "Id";
                    ws.Cells["C1"].Value = "Value";
                    ws.Cells["D1"].Value = "Restriction Value";
                    ws.Cells["E1"].Value = "CSS";
                    ws.Cells["F1"].Value = "Class";
                    ws.Cells["G1"].Value = "GUID";
                    int index = 2;
                    foreach (var item in items)
                    {
                        var en = item as MetaEnum;
                        foreach (var ev in en.Values)
                        {
                            ws.Cells["A" + index].Value = en.Name.Replace("ENUM VALUES: ", "");
                            ws.Cells["B" + index].Value = ev.Id;
                            ws.Cells["C" + index].Value = ev.Val;
                            ws.Cells["D" + index].Value = ev.ValR;
                            ws.Cells["E" + index].Value = ev.Css;
                            ws.Cells["F" + index].Value = ev.Class;
                            ws.Cells["G" + index].Value = en.GUID;
                            index++;
                        }
                    }
                    ws.Cells["A1:G1"].AutoFilter = true;
                    ws.Cells["A1:G1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells["A1:G1"].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    ws.Cells["A1:G1"].Style.Font.Bold = true;
                }
                foreach (ExcelWorksheet sheet in ep.Workbook.Worksheets.Where(i => i.Dimension != null))
                {
                    sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
                    sheet.View.FreezePanes(2, 1);
                }
                ep.SaveAs(new FileInfo(path));
                log.Log($"The file '{path}' has been saved.\r\nIt can be modified and re-imported to the Data Source.");
            }
            catch (Exception ex)
            {
                log.LogRaw("ERROR\r\n");
                log.Log(ex.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        public void ImportSourceObjectsExcel(ExecutionLogInterface log, string path)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                _setModified = true;

                var ep = new ExcelPackage(new FileInfo(path));
                foreach (var ws in ep.Workbook.Worksheets)
                {
                    if (ws.Name == "Tables")
                    {
                        log.Log("Processing tables...");
                        int index = 2;
                        while (true)
                        {
                            var tableName = ws.Cells["A" + index].Value?.ToString().Trim();
                            var columnName = ws.Cells["B" + index].Value?.ToString().Trim();
                            var columnGUID = ws.Cells["G" + index].Value?.ToString();

                            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(columnName)) break;
                            log.Log($"Processing {tableName} {columnName}");
                            var table = Source.MetaData.Tables.FirstOrDefault(i => i.Name == tableName);
                            if (table == null)
                            {
                                log.Log("Creating table: " + tableName);
                                table = Source.AddTable(false);
                                table.Name = tableName;
                            }

                            MetaColumn column = null;
                            if (!string.IsNullOrEmpty(columnGUID)) column = table.Columns.FirstOrDefault(i => i.GUID.ToLower() == columnGUID.ToLower());
                            if (column == null) column = table.Columns.FirstOrDefault(i => i.Name.ToLower() == columnName.ToLower());
                            if (column == null)
                            {
                                log.Log("Creating column: " + columnName);
                                column = Source.AddColumn(table);
                                column.Name = columnName;
                            }

                            //properties
                            Helper.SetPropertyValue(column, "Type", ws.Cells["C" + index].Value?.ToString());
                            column.Category = ws.Cells["D" + index].Value?.ToString();
                            column.DisplayName = ws.Cells["E" + index].Value?.ToString();
                            column.DisplayOrder = int.Parse(ws.Cells["F" + index].Value?.ToString());
                            //Enum
                            column.EnumGUID = "";
                            var en = Source.MetaData.Enums.FirstOrDefault(i => i.Name.ToLower() == ws.Cells["G" + index].Value?.ToString().ToLower());
                            if (en != null)
                            {
                                column.EnumGUID = en.GUID;
                            }

                            column.CssClass = ws.Cells["H" + index].Value?.ToString();
                            column.CssStyle = ws.Cells["I" + index].Value?.ToString();
                            column.IsAggregate = ws.Cells["J" + index].Value?.ToString().ToLower() == "true";
                            var format = ws.Cells["K" + index].Value?.ToString();
                            if (!string.IsNullOrEmpty(format))
                            {
                                Helper.SetPropertyValue(column, "NumericStandardFormat", format);
                            }
                            format = ws.Cells["L" + index].Value?.ToString();
                            if (!string.IsNullOrEmpty(format))
                            {
                                Helper.SetPropertyValue(column, "DateTimeStandardFormat", format);
                            }
                            column.Format = ws.Cells["M" + index].Value?.ToString();
                            column.Tag = ws.Cells["N" + index].Value?.ToString();

                            column.SetStandardFormat();
                            index++;
                        }

                    }
                    if (ws.Name == "Enums")
                    {
                        int index = 2;
                        log.Log("Processing enumerated lists...");
                        var cleared = new List<MetaEnum>();
                        while (true)
                        {
                            var enumName = ws.Cells["A" + index].Value?.ToString().Trim();
                            if (string.IsNullOrEmpty(enumName)) break;
                            var el = Source.MetaData.Enums.FirstOrDefault(i => i.Name == enumName);
                            if (el == null)
                            {
                                log.Log("Creating enum: " + enumName);
                                el = Source.AddEnum();
                                el.Name = enumName;
                            }
                            if (!cleared.Contains(el))
                            {
                                log.Log("Enum: " + enumName);
                                el.Values.Clear();
                                cleared.Add(el);
                            }
                            var item = new MetaEV();
                            item.Id = ws.Cells["B" + index].Value?.ToString();
                            item.Val = ws.Cells["C" + index].Value?.ToString();
                            log.Log($"Processing {item.Id} {item.Val}");
                            item.ValR = ws.Cells["D" + index].Value?.ToString();
                            item.Css = ws.Cells["E" + index].Value?.ToString();
                            item.Class = ws.Cells["F" + index].Value?.ToString();
                            index++;
                            el.Values.Add(item);
                        }
                    }
                }
                log.Log("Import has been done.");
            }
            catch (Exception ex)
            {
                log.LogRaw("ERROR\r\n");
                log.Log(ex.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        public void ImportSourceObjects(ExecutionLogInterface log, List<object> checkedItems, bool changeGUID, bool overwriteProperties)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                var guids = new Dictionary<string, string>();

                _setModified = true;

                //First enums
                foreach (var item in checkedItems.Where(i => i is MetaEnum))
                {
                    var en = item as MetaEnum;
                    log.Log("Processing Enum: " + en.Name);

                    if (changeGUID)
                    {
                        var oldGUID = en.GUID;
                        en.GUID = Helper.NewGUID();
                        guids.Add(oldGUID, en.GUID);
                    }
                    else
                    {
                        Source.MetaData.Enums.RemoveAll(i => i.GUID == en.GUID);
                    }
                    Source.MetaData.Enums.Add(en);
                }

                //Then tables
                foreach (var item in checkedItems.Where(i => i is MetaTable))
                {
                    var table = item as MetaTable;
                    log.Log("Processing Table: " + table.Name);
                    if (changeGUID)
                    {
                        var oldGUID = table.GUID;
                        table.GUID = Helper.NewGUID();
                        guids.Add(oldGUID, table.GUID);
                        foreach (var col in table.Columns)
                        {
                            oldGUID = col.GUID;
                            col.GUID = Helper.NewGUID();
                            guids.Add(oldGUID, col.GUID);
                            //Check enum
                            if (!string.IsNullOrEmpty(col.EnumGUID) && guids.ContainsKey(col.EnumGUID)) col.EnumGUID = guids[col.EnumGUID];

                            if (!Source.MetaData.Enums.Exists(i => i.GUID == col.EnumGUID)) col.EnumGUID = null;
                        }
                    }
                    else
                    {
                        var destTable = Source.MetaData.Tables.FirstOrDefault(i => i.GUID == table.GUID);
                        if (destTable != null)
                        {
                            //Handles columns
                            foreach (var col in destTable.Columns)
                            {
                                if (!table.Columns.Exists(i => i.GUID == col.GUID)) table.Columns.Add(col);
                                else
                                {
                                    if (!overwriteProperties)
                                    {
                                        //keep column from the source
                                        table.Columns.RemoveAll(i => i.GUID == col.GUID);
                                        table.Columns.Add(col);
                                    }
                                }
                            }
                            Source.MetaData.Tables.Remove(destTable);
                        }
                    }
                    Source.MetaData.Tables.Add(table);
                }
                //Then joins
                foreach (var item in checkedItems.Where(i => i is MetaJoin))
                {
                    var join = item as MetaJoin;
                    log.Log("Processing Join: " + join.Name);
                    if (changeGUID)
                    {
                        var oldGUID = join.GUID;
                        join.GUID = Helper.NewGUID();
                        guids.Add(oldGUID, join.GUID);
                        //Check tables
                        if (!string.IsNullOrEmpty(join.LeftTableGUID) && guids.ContainsKey(join.LeftTableGUID)) join.LeftTableGUID = guids[join.LeftTableGUID];
                        if (!string.IsNullOrEmpty(join.RightTableGUID) && guids.ContainsKey(join.RightTableGUID)) join.RightTableGUID = guids[join.RightTableGUID];
                    }
                    else
                    {
                        Source.MetaData.Joins.RemoveAll(i => i.GUID == join.GUID);
                    }
                    Source.MetaData.Joins.Add(join);
                }
                //Then connections
                foreach (var item in checkedItems.Where(i => i is MetaConnection))
                {
                    var connection = item as MetaConnection;
                    log.Log("Processing Connection: " + connection.Name);
                    if (changeGUID)
                    {
                        var oldGUID = connection.GUID;
                        connection.GUID = Helper.NewGUID();
                        guids.Add(oldGUID, connection.GUID);
                    }
                    else
                    {
                        Source.Connections.RemoveAll(i => i.GUID == connection.GUID);
                    }
                    Source.Connections.Add(connection);
                }
                log.Log("Import has been done.");
            }
            catch (Exception ex)
            {
                log.LogRaw("ERROR\r\n");
                log.Log(ex.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        public void ExportReportsTranslations(ExecutionLogInterface log)
        {
            Repository repository = Repository.Instance.CreateFast();
            repository.ReloadRepositoryTranslations();
            StringBuilder translations = new StringBuilder();
            try
            {
                log.Log("Starting the export of the Folders, Reports translations\r\n");
                string separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                string extraSeparators = initTranslationFile(translations, separator, repository);

                log.Log("Adding texts in context: FolderName, FolderPath\r\n");
                exportFolderNamesTranslations(repository.ReportsFolder, translations, separator, extraSeparators, repository.ReportsFolder.Length);

                log.Log("Adding file names in context: FileName\r\n");
                exportReportNamesTranslations(repository.ReportsFolder, translations, separator, extraSeparators, repository.ReportsFolder.Length);

                log.Log("Adding texts in context: ReportExecutionName, ReportViewName, ReportOutputName\r\n");
                exportReportsTranslations(log, repository.ReportsFolder, repository, translations, separator, extraSeparators, repository.ReportsFolder.Length);
                exportReportsTranslations(log, repository.SubReportsFolder, repository, translations, separator, extraSeparators, repository.SubReportsFolder.Length);

                string fileName = FileHelper.GetUniqueFileName(Path.Combine(repository.SettingsFolder, "FoldersReportsTranslations_MISSINGS.csv"));
                File.WriteAllText(fileName, translations.ToString(), Encoding.UTF8);

                log.Log("\r\nExport of the missings Folders and Reports translations terminated.\r\n\r\nThe file has been saved to '{0}' and can be re-worked and merged with the repository translations file.\r\n\r\nNote that the effective repository translations file is 'RepositoryTranslations.csv' (or 'RepositoryTranslations.xlsx') in the Repository Sub-Folder 'Settings'.", fileName);

                var p = new Process();
                p.StartInfo = new ProcessStartInfo(fileName) { UseShellExecute = true };
                p.Start();
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
                    Report report = Report.LoadFromFile(reportPath, repository, false);
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

                log.Log("Checking for Orphan schedules\r\n");

                if (repository.UseSealScheduler)
                {
                    SealReportScheduler.Instance.GetSchedules();
                }
                else
                {
                    TaskService taskService = new TaskService();
                    TaskFolder taskFolder = taskService.RootFolder.SubFolders.FirstOrDefault(i => i.Name == repository.Configuration.TaskFolderName);
                    if (taskFolder != null)
                    {
                        var tasks = taskFolder.GetTasks().ToList();
                        foreach (Task task in tasks)
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
    }
}
