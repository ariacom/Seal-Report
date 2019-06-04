//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms;
using Seal.Model;
using System.Diagnostics;
using System.IO;
using Seal.Helpers;
using System.Security.Principal;
using System.Data.OleDb;
using System.Data;
using System.Text;

namespace Seal.Forms
{
    public class HelperEditor : UITypeEditor
    {
        public static IEntityHandler HandlerInterface = null;

        MetaConnection _metaConnection;
        MetaEnum _metaEnum;
        MetaTable _metaTable;
        MetaColumn _metaColumn;
        MetaJoin _metaJoin;
        ReportView _reportView;
        ReportOutput _reportOutput;
        ReportSchedule _reportSchedule;
        Parameter _parameter;
        SealSecurity _security;
        OutputEmailDevice _emailDevice;
        ReportModel _model;
        SealServerConfiguration _configuration;

        void setContext(ITypeDescriptorContext context)
        {
            _metaConnection = context.Instance as MetaConnection;
            _metaEnum = context.Instance as MetaEnum;
            _metaTable = context.Instance as MetaTable;
            _metaColumn = context.Instance as MetaColumn;
            _metaJoin = context.Instance as MetaJoin;
            _reportView = context.Instance as ReportView;
            _reportOutput = context.Instance as ReportOutput;
            _reportSchedule = context.Instance as ReportSchedule;
            _parameter = context.Instance as Parameter;
            _security = context.Instance as SealSecurity;
            _emailDevice = context.Instance as OutputEmailDevice;
            _model = context.Instance as ReportModel;
            _configuration = context.Instance as SealServerConfiguration;
        }

        void setModified()
        {
            if (HandlerInterface != null) HandlerInterface.SetModified();
        }

        void initEntity(object entity)
        {
            if (HandlerInterface != null) HandlerInterface.InitEntity(entity);
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            setContext(context);
            if (_metaTable != null)
            {
                if (context.PropertyDescriptor.Name == "HelperRefreshColumns")
                {
                    if (!_metaTable.DynamicColumns || !_metaTable.IsEditable) return UITypeEditorEditStyle.None;
                }
            }
            if (_metaEnum != null)
            {
                if (context.PropertyDescriptor.Name == "HelperRefreshEnum")
                {
                    if (!_metaEnum.IsDynamic || !_metaEnum.IsEditable) return UITypeEditorEditStyle.None;
                }
            }

            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                setContext(context);
                if (_emailDevice != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperTestEmail")
                    {
                        _emailDevice.SendTestEmail();
                    }
                }
                else if (_metaConnection != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperCheckConnection")
                    {
                        _metaConnection.CheckConnection();
                    }
                    if (context.PropertyDescriptor.Name == "HelperCreateFromExcelAccess")
                    {
                        string accessDriver = "Microsoft Access Driver (*.mdb)";
                        string excelDriver = "Microsoft Excel Driver (*.xls)";
                        try
                        {
                            List<string> drivers = Helper.GetSystemDriverList();
                            string accessDriver2 = "Microsoft Access Driver (*.mdb, *.accdb)";
                            if (drivers.Contains(accessDriver2)) accessDriver = accessDriver2;
                            string excelDriver2 = "Microsoft Excel Driver (*.xls, *.xlsx, *.xlsm, *.xlsb)";
                            if (drivers.Contains(excelDriver2)) excelDriver = excelDriver2;
                        }
                        catch { }

                        OpenFileDialog dlg = new OpenFileDialog();
                        dlg.Title = "Open an Excel or an MS Access File";
                        dlg.CheckFileExists = true;
                        dlg.CheckPathExists = true;
                        dlg.InitialDirectory = _metaConnection.Source.Repository.RepositoryPath;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            string ext = Path.GetExtension(dlg.FileName);
                            string driver = "";
                            if (ext == ".xls" || ext == ".xlsx" || ext == ".xlsm" || ext == ".xlsb")
                            {
                                _metaConnection.DatabaseType = DatabaseType.MSExcel;
                                driver = excelDriver;
                            }
                            else if (ext == ".mdb" || ext == ".accdb")
                            {
                                _metaConnection.DatabaseType = DatabaseType.MSAccess;
                                driver = accessDriver;
                            }
                            else throw new Exception("Please select an Excel or MS Access file");

                            string path = dlg.FileName.Replace(_metaConnection.Source.Repository.RepositoryPath, Repository.SealRepositoryKeyword);
                            _metaConnection.ConnectionString = string.Format(@"Provider=MSDASQL.1;Extended Properties=""DBQ={0};Driver={{{1}}};""", path, driver);
                            setModified();
                            MessageBox.Show("The connection has been created successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                else if (_metaTable != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperRefreshColumns")
                    {
                        _metaTable.Refresh();
                        setModified();
                        initEntity(_metaTable);
                    }
                    if (context.PropertyDescriptor.Name == "HelperCheckTable")
                    {
                        _metaTable.CheckTable(null);
                    }
                }
                else if (_metaEnum != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperRefreshEnum")
                    {
                        _metaEnum.RefreshEnum();
                        setModified();
                    }
                }
                else if (_metaColumn != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperCheckColumn")
                    {
                        _metaColumn.MetaTable.CheckTable(_metaColumn);
                    }
                    else if (context.PropertyDescriptor.Name == "HelperCreateEnum")
                    {
                        MetaEnum result = _metaColumn.Source.CreateEnumFromColumn(_metaColumn);
                        _metaColumn.EnumGUID = result.GUID;
                        initEntity(result);
                        setModified();
                    }
                    else if (context.PropertyDescriptor.Name == "HelperShowValues")
                    {
                        try
                        {
                            Cursor.Current = Cursors.WaitCursor;
                            string result = _metaColumn.MetaTable.ShowValues(_metaColumn);
                            ExecutionForm frm = new ExecutionForm(null);
                            frm.Text = "Show values";
                            frm.cancelToolStripButton.Visible = false;
                            frm.pauseToolStripButton.Visible = false;
                            frm.logTextBox.Text = result;
                            frm.logTextBox.SelectionStart = 0;
                            frm.logTextBox.SelectionLength = 0;
                            frm.ShowDialog();
                        }
                        finally
                        {
                            Cursor.Current = Cursors.Default;
                        }
                    }
                    else if (context.PropertyDescriptor.Name == "HelperCreateDrillDates")
                    {
                        var year = _metaColumn.MetaTable.Source.AddColumn(_metaColumn.MetaTable);
                        year.DisplayName = _metaColumn.DisplayName + " Year";
                        year.Type = ColumnType.DateTime;
                        year.DateTimeStandardFormat = DateTimeStandardFormat.Custom;
                        year.Format = "yyyy";
                        var month = _metaColumn.MetaTable.Source.AddColumn(_metaColumn.MetaTable);
                        month.DisplayName = _metaColumn.DisplayName + " Month";
                        month.Type = ColumnType.DateTime;
                        month.DateTimeStandardFormat = DateTimeStandardFormat.Custom;
                        month.Format = "MM/yyyy";
                        if (_metaColumn.MetaTable.Source.Connection.DatabaseType == DatabaseType.Oracle)
                        {
                            year.Name = string.Format("trunc({0},'year')", _metaColumn.Name);
                            month.Name = string.Format("trunc({0},'month')", _metaColumn.Name);
                        }
                        else if (_metaColumn.MetaTable.Source.Connection.DatabaseType == DatabaseType.MSSQLServer)
                        {
                            year.Name = string.Format("DATETIME2FROMPARTS(year({0}),1,1,0,0,0,0,0)", _metaColumn.Name);
                            month.Name = string.Format("DATETIME2FROMPARTS(year({0}),month({0}),1,0,0,0,0,0)", _metaColumn.Name);
                        }
                        else if (_metaColumn.MetaTable.Source.Connection.DatabaseType == DatabaseType.MSAccess)
                        {
                            year.Name = string.Format("DateSerial(DatePart('yyyy',{0}), 1, 1)", _metaColumn.Name);
                            month.Name = string.Format("DateSerial(DatePart('yyyy',{0}), DatePart('m',{0}), 1)", _metaColumn.Name);
                        }
                        year.DrillChildren.Add(month.GUID);
                        month.DrillChildren.Add(_metaColumn.GUID);
                        initEntity(_metaColumn.MetaTable);
                        setModified();
                        MessageBox.Show("A 'Year' column and a 'Month' column have been added to the table with a drill hierarchy.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (context.PropertyDescriptor.Name == "HelperCreateSubReport")
                    {
                        Report report = Report.Create(Repository.Create());
                        //Only on detail view
                        report.Views.Clear();
                        report.AddView(ReportViewTemplate.ModelDetailName);
                        report.Views[0].InitParameters(true);
                        report.Views[0].Parameters.First(i => i.Name == "restriction_button").BoolValue = false; 

                        report.Sources.RemoveAll(i => i.MetaSourceGUID != _metaColumn.Source.GUID);

                        if (report.Sources.Count == 0) throw new Exception("Unable to create the detail report. Please save the Data Source first...");

                        //And one model
                        ReportModel model = report.Models[0];
                        model.SourceGUID = _metaColumn.Source.GUID;
                        //Add all the element of the table
                        foreach (var el in _metaColumn.MetaTable.Columns.OrderBy(i => i.DisplayOrder))
                        {
                            ReportElement element = ReportElement.Create();
                            element.MetaColumnGUID = el.GUID;
                            element.Name = el.Name;
                            element.PivotPosition = (el == _metaColumn ? PivotPosition.Page : PivotPosition.Row);
                            model.Elements.Add(element);
                        }

                        string entityName = _metaColumn.MetaTable.Name;
                        if (entityName.EndsWith("s")) entityName = entityName.Substring(0, entityName.Length - 1);
                        string path = Path.Combine(_metaColumn.MetaTable.Source.Repository.SubReportsFolder, Helper.CleanFileName(entityName + " Detail.") + Repository.SealReportFileExtension);
                        path = FileHelper.GetUniqueFileName(path);

                        var sr = new SubReport() { Path = path.Replace(_metaColumn.Source.Repository.RepositoryPath, Repository.SealRepositoryKeyword), Name = entityName + " Detail" };
                        //And the restriction, try to find out the table primary keys
                        try
                        {
                            DataTable schemaTables = ((OleDbConnection)_metaColumn.Source.GetOpenConnection()).GetOleDbSchemaTable(OleDbSchemaGuid.Primary_Keys, null);
                            Helper.DisplayDataTable(schemaTables);
                            foreach (DataRow row in schemaTables.Rows)
                            {
                                string schema = "";
                                if (schemaTables.Columns.Contains("TABLE_SCHEMA")) schema = row["TABLE_SCHEMA"].ToString();
                                else if (schemaTables.Columns.Contains("TABLE_SCHEM")) schema = row["TABLE_SCHEM"].ToString();
                                string fullName = (!string.IsNullOrEmpty(schema) ? _metaColumn.Source.GetTableName(schema) + "." : "") + _metaColumn.Source.GetTableName(row["TABLE_NAME"].ToString());
                                if (row["TABLE_NAME"].ToString() == _metaColumn.MetaTable.Name || fullName == _metaColumn.MetaTable.Name)
                                {
                                    var col = _metaColumn.MetaTable.Columns.FirstOrDefault(i => i.Name.ToLower() == row["COLUMN_NAME"].ToString().ToLower() || i.Name.ToLower().EndsWith("." + row["COLUMN_NAME"].ToString().ToLower()));
                                    if (col != null)
                                    {
                                        sr.Restrictions.Add(col.GUID);
                                    }
                                    else
                                    {
                                        //not all pk available....
                                        sr.Restrictions.Clear();
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }

                        string message = "";
                        if (sr.Restrictions.Count == 0)
                        {
                            //no PK found, we add the value itself...
                            sr.Restrictions.Add(_metaColumn.GUID);
                            message = "The Sub-Report restriction is based on the Column.";
                        }
                        else
                        {
                            message = "The Sub-Report restrictions are based on the table Primary Keys.";
                        }

                        foreach (var guid in sr.Restrictions)
                        {
                            ReportRestriction restriction = ReportRestriction.CreateReportRestriction();
                            restriction.MetaColumnGUID = guid;
                            restriction.PivotPosition = PivotPosition.Row;
                            restriction.Prompt = PromptType.Prompt;
                            restriction.Operator = Operator.Equal;
                            model.Restrictions.Add(restriction);
                            if (!string.IsNullOrEmpty(model.Restriction)) model.Restriction += "\r\nAND ";
                            model.Restriction += ReportRestriction.kStartRestrictionChar + restriction.GUID + ReportRestriction.kStopRestrictionChar;
                        }
                        model.InitReferences();

                        report.SaveToFile(path);
                        _metaColumn.SubReports.Add(sr);

                        if (MessageBox.Show(string.Format("A Sub-Report named '{0}' has been created in the dedicated Repository folder.\r\n{1}\r\nDo you want to edit it using a new Report Designer ?", Path.GetFileName(path), message), "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            Process.Start(path);
                        };

                        _metaColumn.UpdateEditor();
                        setModified();
                    }
                    else if (context.PropertyDescriptor.Name == "HelperAddSubReport")
                    {
                        OpenFileDialog dlg = new OpenFileDialog();
                        dlg.Filter = string.Format(Repository.SealRootProductName + " Reports files (*.{0})|*.{0}|All files (*.*)|*.*", Repository.SealReportFileExtension);
                        dlg.Title = "Select a Sub-Report having prompted restrictions";
                        dlg.CheckFileExists = true;
                        dlg.CheckPathExists = true;
                        dlg.InitialDirectory = _metaColumn.Source.Repository.SubReportsFolder;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            Report report = Report.LoadFromFile(dlg.FileName, _metaColumn.Source.Repository);
                            var sr = new SubReport() { Path = report.FilePath.Replace(_metaColumn.Source.Repository.RepositoryPath, Repository.SealRepositoryKeyword), Name = Path.GetFileNameWithoutExtension(dlg.FileName) };

                            bool tableOk = false;
                            foreach (var model in report.Models.Where(i => i.Source.MetaSourceGUID == _metaColumn.Source.GUID))
                            {
                                foreach (var restriction in model.Restrictions.Where(i => i.Prompt != PromptType.None))
                                {
                                    foreach (var table in _metaColumn.MetaTable.Source.MetaData.Tables)
                                    {
                                        var col = table.Columns.FirstOrDefault(i => i.GUID == restriction.MetaColumnGUID);
                                        if (col != null)
                                        {
                                            tableOk = true;
                                            sr.Restrictions.Add(col.GUID);
                                        }
                                    }
                                }
                            }

                            if (!tableOk) throw new Exception("Unable to add this Sub-Report:\r\nThe report does no contain any prompted restriction...");

                            _metaColumn.SubReports.Add(sr);
                            MessageBox.Show(string.Format("The Sub-Report named '{0}' has been added with {1} restriction(s).", Path.GetFileName(dlg.FileName), sr.Restrictions.Count), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            _metaColumn.UpdateEditor();
                            setModified();
                        }
                    }
                    else if (context.PropertyDescriptor.Name == "HelperOpenSubReportFolder")
                    {
                        Process.Start(_metaColumn.Source.Repository.SubReportsFolder);
                    }
                }
                else if (_metaJoin != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperCheckJoin")
                    {
                        _metaJoin.CheckJoin();
                    }
                }
                else if (_model != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperViewJoins")
                    {
                        try
                        {
                            _model.JoinPaths = new StringBuilder();
                            _model.BuildSQL();

                            var frm = new ExecutionForm(null);
                            frm.Text = "List of Joins";
                            frm.cancelToolStripButton.Visible = false;
                            frm.pauseToolStripButton.Visible = false;
                            frm.logTextBox.Text = _model.JoinPaths.ToString();
                            frm.logTextBox.SelectionStart = 0;
                            frm.logTextBox.SelectionLength = 0;
                            frm.ShowDialog();
                        }
                        finally
                        {
                            _model.JoinPaths = null;
                        }

                    }
                }
                else if (_reportView != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperReloadConfiguration")
                    {
                        _reportView.ReloadConfiguration();
                    }
                    else if (context.PropertyDescriptor.Name == "HelperResetParameters")
                    {
                        _reportView.InitParameters(true);
                        setModified();
                    }
                    else if (context.PropertyDescriptor.Name == "HelperResetPDFConfigurations")
                    {
                        _reportView.PdfConfigurations = new List<string>();
                        _reportView.PdfConverter = null;
                        _reportView.Information = Helper.FormatMessage("The PDF configuration values have been reset");
                        setModified();
                    }
                    else if (context.PropertyDescriptor.Name == "HelperResetExcelConfigurations")
                    {
                        _reportView.ExcelConfigurations = new List<string>();
                        _reportView.ExcelConverter = null;
                        _reportView.Information = Helper.FormatMessage("The Excel configuration values have been reset");
                        setModified();
                    }
                }
                else if (_configuration != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperResetPDFConfigurations")
                    {
                        _configuration.PdfConfigurations = new List<string>();
                        _configuration.PdfConverter = null;
                    }
                    else if (context.PropertyDescriptor.Name == "HelperResetExcelConfigurations")
                    {
                        _configuration.ExcelConfigurations = new List<string>();
                        _configuration.ExcelConverter = null;
                    }
                }
                else if (_reportSchedule != null)
                {
                    if (HandlerInterface != null && context.PropertyDescriptor.Name == "HelperEditProperties")
                    {
                        HandlerInterface.EditSchedule(_reportSchedule);
                    }
                    else if (context.PropertyDescriptor.Name == "HelperRunTaskScheduler")
                    {
                        Process.Start(Path.Combine(Environment.SystemDirectory, "taskschd.msc"), "/s");
                    }
                }
                else if (_parameter != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperResetParameterValue")
                    {
                        _parameter.Value = _parameter.ConfigValue;
                        setModified();
                    }
                }
                else if (_security != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperSimulateLogin")
                    {
                        SecurityUser user = new SecurityUser(_security);
                        user.WebUserName = _security.TestUserName;
                        user.WebPassword = _security.TestPassword;
                        if (_security.TestCurrentWindowsUser) user.WebPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                        user.Authenticate();
                        try
                        {
                            Cursor.Current = Cursors.WaitCursor;
                            ExecutionForm frm = new ExecutionForm(null);
                            frm.Text = "Test a login";
                            frm.cancelToolStripButton.Visible = false;
                            frm.pauseToolStripButton.Visible = false;
                            frm.logTextBox.Text = user.AuthenticationSummary;
                            frm.logTextBox.SelectionStart = 0;
                            frm.logTextBox.SelectionLength = 0;
                            frm.ShowDialog();
                        }
                        finally
                        {
                            Cursor.Current = Cursors.Default;
                        }

                    }
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

            return value;
        }
    }
}
