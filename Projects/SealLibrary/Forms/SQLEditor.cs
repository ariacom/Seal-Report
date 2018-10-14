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

namespace Seal.Forms
{
    public class SQLEditor : UITypeEditor
    {
        const string razorTableTemplate = "@using Seal.Model\r\n@{\r\nMetaTable table = Model;\r\nstring result = \"select...\";\r\n}\r\n@Raw(result)";
        const string razorTableWhereTemplate = @"@using Seal.Model
@using Seal.Helpers
@{
    MetaTable table = Model;
    string restriction = Environment.UserName; //This gives the windows user of the process running the engine
    if (table.Source.Report != null && table.Source.Report.SecurityContext != null)
    {
        var securityContext = table.Source.Report.SecurityContext; //User is logged through a Web Report Servert and has a context
        restriction = securityContext.Name; //Name of the user set during the login
        restriction = securityContext.WebUserName; //Name got from the login window
        //securityContext.SecurityGroups; //List of security groups set for the user
        if (securityContext.BelongsToGroup(""Default Group"")) { //Test if the user belongs to a group
            //Special restriction here
        }
    }
    string result = string.Format(""aColumnName={0}"", Helper.QuoteSingle(restriction));
    }
@Raw(result)";
        const string razorSourceTemplate = "@using Seal.Model\r\n@{\r\nMetaSource source = Model;\r\nstring result = \"select...\";\r\n}\r\n@Raw(result)";
        const string razorModelTemplate = "@using Seal.Model\r\n@{\r\nReportModel model = Model;\r\nstring result = \"select...\";\r\n}\r\n@Raw(result)";
        const string razorTaskTemplate = "@using Seal.Model\r\n@{\r\nReportTask task= Model;\r\nstring result = \"select...\";\r\n}\r\n@Raw(result)";
        const string razorEnumTemplate = "@using Seal.Model\r\n@{\r\nMetaEnum enumList= Model;\r\nstring result = \"select...\";\r\n}\r\n@Raw(result)";

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (context.Instance is MetaEnum && context.PropertyDescriptor.IsReadOnly)
            {
                return UITypeEditorEditStyle.None;
            }

            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var svc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if (svc != null)
            {
                var frm = new SQLEditorForm();
                frm.Instance = context.Instance;
                frm.PropertyName = context.PropertyDescriptor.Name;
                string template = "";
                string valueToEdit = (value == null ? "" : value.ToString());
                bool forceValueToEdit = false;

                if (context.Instance is ReportModel)
                {
                    ReportModel model = context.Instance as ReportModel;
                    model.BuildSQL();
                    if (!string.IsNullOrEmpty(model.ExecutionError)) 
                    {
                        throw new Exception("Error building the SQL Statement...\r\nPlease fix these errors first.\r\n" + model.ExecutionError); 
                    } 

                    if (context.PropertyDescriptor.Name == "SqlEditor")
                    {
                        frm.clearToolStripButton.Visible = false;
                        frm.sqlTextBox.Text = model.Sql;
                        frm.sqlTextBox.ReadOnly = true;
                    }
                    else if (context.PropertyDescriptor.Name == "PreSQL" || context.PropertyDescriptor.Name == "PostSQL")
                    {
                        template = razorModelTemplate; 
                        frm.clearToolStripButton.Visible = false;
                    }
                    else
                    {
                        frm.checkSQLToolStripButton.Visible = false;
                        forceValueToEdit = true;
                        var value2 = value;
                        if (value2 == null) value2 = "";
                        if (context.PropertyDescriptor.Name == "SqlSelect" && string.IsNullOrEmpty(value2.ToString())) valueToEdit = model.execSelect;
                        if (context.PropertyDescriptor.Name == "SqlFrom" && string.IsNullOrEmpty(value2.ToString())) valueToEdit = "FROM " + model.execFromClause.ToString();
                        if (context.PropertyDescriptor.Name == "SqlGroupBy" && string.IsNullOrEmpty(value2.ToString())) valueToEdit = "GROUP BY " + model.execGroupByClause.ToString();
                        if (context.PropertyDescriptor.Name == "SqlOrderBy" && string.IsNullOrEmpty(value2.ToString())) valueToEdit = "ORDER BY " + model.execOrderByClause.ToString();
                    }
                }
                else if (context.Instance is MetaJoin)
                {
                    frm.clearToolStripButton.Visible = false;
                }
                else if (context.Instance is MetaEnum)
                {
                    template = razorEnumTemplate;
                    frm.clearToolStripButton.Visible = false;
                    MetaEnum anEnum = context.Instance as MetaEnum;
                    if (value == null || string.IsNullOrEmpty(value.ToString()))
                    {
                        forceValueToEdit = true;
                        valueToEdit = anEnum.DefaultSQL;
                    }
                }
                else if (context.Instance is ReportSource || context.Instance is MetaSource)
                {
                    template = razorSourceTemplate;
                    frm.clearToolStripButton.Visible = false;
                }
                else if (context.Instance is MetaTable)
                {
                    if (context.PropertyDescriptor.Name == "PreSQL" || context.PropertyDescriptor.Name == "PostSQL")
                    {
                        template = razorTableTemplate; 
                    }
                    else if (context.PropertyDescriptor.Name == "WhereSQL")
                    {
                        template = razorTableWhereTemplate;
                    }
                    frm.clearToolStripButton.Visible = false;
                }
                else if (context.Instance is ReportTask)
                {
                    template = razorTaskTemplate;
                    frm.clearToolStripButton.Visible = false;
                }

                if (value != null || forceValueToEdit) frm.sqlTextBox.Text = valueToEdit.ToString();

                if (context.PropertyDescriptor.IsReadOnly) frm.SetReadOnly();
                else if (!string.IsNullOrEmpty(template))
                {
                    frm.SetSamples(new List<string>() { template });
                    frm.errorTextBox.Text = "Note that Razor script can be used if the text starts with '@'.\r\n";
                }

                if (svc.ShowDialog(frm) == DialogResult.OK)
                {
                    value = frm.sqlTextBox.Text;
                }
            }
            return value;
        }
    }
}
