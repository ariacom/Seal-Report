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
        const string razorTableWhereTemplate ="@using Seal.Model\r\n@using Seal.Helpers\r\n@{\r\n  MetaTable table = Model;\r\n  string restriction = Environment.UserName;\r\n  if (table.Source.Report != null && table.Source.Report.SecurityContext != null)\r\n  {\r\n    restriction = table.Source.Report.SecurityContext.Name;\r\n  }\r\n  string result = string.Format(\"aColumnName={0}\", Helper.QuoteSingle(restriction));\r\n}\r\n@Raw(result)";
        const string razorSourceTemplate = "@using Seal.Model\r\n@{\r\nMetaSource source = Model;\r\nstring result = \"select...\";\r\n}\r\n@Raw(result)";
        const string razorModelTemplate = "@using Seal.Model\r\n@{\r\nReportModel model = Model;\r\nstring result = \"select...\";\r\n}\r\n@Raw(result)";
        const string razorTaskTemplate = "@using Seal.Model\r\n@{\r\nReportTask task= Model;\r\nstring result = \"select...\";\r\n}\r\n@Raw(result)";

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
                        frm.sqlTextBox.IsReadOnly = true;
                    }
                    else if (context.PropertyDescriptor.Name == "PreSQL" || context.PropertyDescriptor.Name == "PostSQL")
                    {
                        template = razorModelTemplate; 
                        frm.clearToolStripButton.Visible = false;
                    }
                    else
                    {
                        frm.checkSQLToolStripButton.Visible = false;
                        if (value == null) value = "";
                        if (context.PropertyDescriptor.Name == "SqlSelect" && string.IsNullOrEmpty(value.ToString())) valueToEdit = model.execSelect;
                        if (context.PropertyDescriptor.Name == "SqlFrom" && string.IsNullOrEmpty(value.ToString())) valueToEdit = "FROM " + model.execFromClause.ToString();
                        if (context.PropertyDescriptor.Name == "SqlGroupBy" && string.IsNullOrEmpty(value.ToString())) valueToEdit = "GROUP BY " + model.execGroupByClause.ToString();
                        if (context.PropertyDescriptor.Name == "SqlOrderBy" && string.IsNullOrEmpty(value.ToString())) valueToEdit = "ORDER BY " + model.execOrderByClause.ToString();
                    }
                }
                else if (context.Instance is MetaJoin)
                {
                    frm.clearToolStripButton.Visible = false;
                }
                else if (context.Instance is MetaEnum)
                {
                    frm.clearToolStripButton.Visible = false;
                    MetaEnum anEnum = context.Instance as MetaEnum;
                    if (value == null || string.IsNullOrEmpty(value.ToString()))
                    {
                        value = "";
                        valueToEdit = anEnum.DefaultSQL;
                    }
                }
                else if (context.Instance is ReportSource)
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

                if (!string.IsNullOrEmpty(template))
                {
                    frm.errorTextBox.Text = "Note that Razor script can be used if the text starts with '@'. Here is a template:\r\n" + template;
                }
                
                if (value != null) frm.sqlTextBox.Text = valueToEdit.ToString();

                if (context.PropertyDescriptor.IsReadOnly) frm.SetReadOnly();

                if (svc.ShowDialog(frm) == DialogResult.OK)
                {
                    value = frm.sqlTextBox.Text;
                }
            }
            return value;
        }
    }
}
