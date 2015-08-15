//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using Seal.Model;
using System.Diagnostics;
using System.IO;
using Seal.Helpers;
using System.Threading;
using System.Security.Principal;
using System.Net.Mail;

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
                    if (context.PropertyDescriptor.Name == "HelperShowValues")
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
                }
                else if (_metaJoin != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperCheckJoin")
                    {
                        _metaJoin.CheckJoin();
                    }
                }
                else if (_reportView != null)
                {
                    if (context.PropertyDescriptor.Name == "HelperReloadConfiguration")
                    {
                        _reportView.InitParameters(false);
                    }
                    else if (context.PropertyDescriptor.Name == "HelperResetParameters")
                    {
                        _reportView.InitParameters(true);
                        setModified();
                    }
                    else if (context.PropertyDescriptor.Name == "HelperResetChartConfiguration")
                    {
                        _reportView.ResetChartConfiguration();
                        setModified();
                    }
                    else if (context.PropertyDescriptor.Name == "HelperResetNVD3ChartConfiguration")
                    {
                        var defaultValue = _reportView.Template.Parameters.FirstOrDefault(i => i.Name == Parameter.NVD3ConfigurationParameter);
                        if (_reportView.NVD3ConfigurationParameter != null && defaultValue != null)
                        {
                            _reportView.NVD3Configuration = defaultValue.TextValue;
                            _reportView.Information = Helper.FormatMessage("NVD3 Chart configuration has been reset");
                            setModified();
                        }
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
