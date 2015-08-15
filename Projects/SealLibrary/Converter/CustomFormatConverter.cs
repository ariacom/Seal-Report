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
using Seal.Helpers;

namespace Seal.Converter
{
    public class CustomFormatConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false; //true will limit to list. false will show the list, but allow free-form entry
        }

        void addNumericChoices(List<string> choices)
        {
            choices.Add("0");
            choices.Add("N");
            choices.Add("N0");
            choices.Add("P");
            choices.Add("P2");
            choices.Add("00000");
            choices.Add("#,##0.00");
            choices.Add("00");
            choices.Add("0,0");
            choices.Add("0,0.0");
            choices.Add("C");
            choices.Add("C2");
            choices.Add("D");
            choices.Add("D2");
            choices.Add("E");
            choices.Add("E2");
            choices.Add("G");
            choices.Add("G8");
            choices.Add("F");
            choices.Add("F2");
            choices.Add("H");
            choices.Add("H8");
        }

        void addDateTimeChoices(List<string> choices)
        {
            choices.Add("0");
            choices.Add("d");
            choices.Add("D");
            choices.Add("t");
            choices.Add("T");
            choices.Add("yyyy");
            choices.Add("MM/yyyy");
            choices.Add("MM/dd/yy H:mm:ss zzz");
            choices.Add("dd/MM/yyyy HH:mm:ss zzz");
        }

        void addStringChoices(List<string> choices)
        {
            choices.Add("0");
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            MetaColumn column = context.Instance as MetaColumn;
            ReportElement element = context.Instance as ReportElement;
            ReportOutput output = context.Instance as ReportOutput;
            List<string> choices = new List<string>();
            choices.Add("");
            if (element != null)
            {
                if (element.IsNumeric) addNumericChoices(choices);
                else if (element.IsDateTime) addDateTimeChoices(choices);
                else addStringChoices(choices);

                if (!string.IsNullOrEmpty(element.FormatEl) && !choices.Contains(element.FormatEl)) choices.Add(element.FormatEl);
            }
            else if (column != null)
            {
                if (column.Type == ColumnType.Numeric) addNumericChoices(choices);
                else if (column.Type == ColumnType.DateTime) addDateTimeChoices(choices);
                else addStringChoices(choices);

                if (!string.IsNullOrEmpty(column.Format) && !choices.Contains(column.Format)) choices.Add(column.Format);
            }
            else if (output != null)
            {
                choices.Add(output.Report.ExecutionName);
                choices.Add(output.Report.ExecutionName + "_{0:yyyy_MM_dd}");
                choices.Add(output.Report.ExecutionName + "_{0:yyyy_MM_dd HH_mm_ss}");
                choices.Add(output.Name);
                choices.Add(output.Name + "_{0:yyyy_MM_dd}");
                choices.Add(output.Name + "_{0:yyyy_MM_dd HH_mm_ss}");

                if (!string.IsNullOrEmpty(output.FileName) && !choices.Contains(output.FileName)) choices.Add(output.FileName);
            }
            return new StandardValuesCollection(choices.ToArray());
        }
    }

}
