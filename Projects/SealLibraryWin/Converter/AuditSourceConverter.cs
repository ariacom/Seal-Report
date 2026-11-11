//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// TypeConverter for <see cref="SealServerConfiguration.AuditSourceGUID"/>: displays the names of the SQL data sources of
    /// the repository in the property-grid dropdown while storing the GUID of the source; an empty value is shown as
    /// <see cref="DefaultName"/> (the first source whose name starts with 'Audit').
    /// </summary>
    public class AuditSourceConverter : StringConverter
    {
        /// <summary>Displayed value of an empty GUID</summary>
        public const string DefaultName = "<First source named 'Audit...'>";

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var names = Repository.Instance.Sources.Where(i => !i.IsNoSQL).Select(i => i.Name).OrderBy(i => i, StringComparer.OrdinalIgnoreCase).ToList();
            names.Insert(0, DefaultName);
            return new StandardValuesCollection(names);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) => destType == typeof(string);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            var guid = value as string;
            if (string.IsNullOrEmpty(guid)) return DefaultName;
            var source = Repository.Instance.Sources.FirstOrDefault(i => i.GUID == guid);
            return source != null ? source.Name : guid;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType) => srcType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var name = value as string;
            if (string.IsNullOrEmpty(name) || name == DefaultName) return "";
            var source = Repository.Instance.Sources.FirstOrDefault(i => i.Name == name);
            return source != null ? source.GUID : name;
        }
    }
}
