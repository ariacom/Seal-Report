//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;
using Seal.Helpers;

namespace Seal.Converter
{
    public class SourceConnectionConverter : StringConverter
    {
        const string DefaultRepositoryStr = "<Default repository connection>";
        const string DefaultReportStr = "<Current connection>";

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true; //true will limit to list. false will show the list, but allow free-form entry
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            string[] choices = new string[] { "" };
            MetaSource source = context.Instance as MetaSource;
            if (context.Instance is ReportModel) source = ((ReportModel)context.Instance).Source;
            else if (context.Instance is ReportTask) source = ((ReportTask)context.Instance).Source;

            if (source != null)
            {
                List<string> choices2 = (from s in source.Connections select s.Name + (!s.IsEditable ? " (Repository)" : "")).ToList();
                if (source.Report != null && (source is ReportSource || context.Instance is ReportView) && !string.IsNullOrEmpty(((ReportSource)source).MetaSourceGUID))
                {
                    string defaultRepConnectionStr = string.Format("{0} ({1})", DefaultRepositoryStr, ((ReportSource)source).RepositoryConnection.Name);
                    choices2.Insert(0, defaultRepConnectionStr);
                }
                if (source.Report != null && (context.Instance is ReportModel || context.Instance is ReportTask) && !string.IsNullOrEmpty(((ReportSource)source).MetaSourceGUID))
                {
                    choices2.Insert(0, DefaultReportStr);
                }
                choices = choices2.ToArray();
            }

            return new StandardValuesCollection(choices);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
        {
            return destType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            if (context != null && value != null)
            {
                MetaSource source = context.Instance as MetaSource;
                if (context.Instance is ReportModel) source = ((ReportModel)context.Instance).Source;
                else if (context.Instance is ReportTask) source = ((ReportTask)context.Instance).Source;

                if (source != null)
                {
                    if (source is ReportSource && value.ToString() == ReportSource.DefaultRepositoryConnectionGUID) return string.Format("{0} ({1})", DefaultRepositoryStr, ((ReportSource)source).RepositoryConnection.Name);
                    if (value.ToString() == ReportSource.DefaultReportConnectionGUID) return DefaultReportStr;
                    MetaConnection connection = source.Connections.FirstOrDefault(i => i.GUID == value.ToString());
                    if (connection != null) return connection.Name + (!connection.IsEditable ? " (Repository)" : "");
                }
            }
            return base.ConvertTo(context, culture, value, destType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType)
        {
            return srcType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            MetaSource source = context.Instance as MetaSource;
            if (context.Instance is ReportModel) source = ((ReportModel)context.Instance).Source;
            else if (context.Instance is ReportTask) source = ((ReportTask)context.Instance).Source;

            if (source != null)
            {
                if (source is ReportSource && ((ReportSource)source).RepositoryConnection != null && value.ToString() == string.Format("{0} ({1})", DefaultRepositoryStr, ((ReportSource)source).RepositoryConnection.Name)) return ReportSource.DefaultRepositoryConnectionGUID;
                if (value.ToString() == ReportSource.DefaultReportConnectionGUID) return DefaultReportStr;
                MetaConnection connection = source.Connections.FirstOrDefault(i => i.Name + (!i.IsEditable ? " (Repository)" : "") == value.ToString());
                if (connection != null) return connection.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }

    }

}
