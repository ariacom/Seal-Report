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
    /// TypeConverter for <see cref="SealServerConfiguration.NotificationDeviceGUID"/>: displays the names of the email devices of
    /// the repository in the property-grid dropdown while storing the GUID of the device; an empty value is shown as
    /// <see cref="DefaultName"/> (the device having 'Used for notification' set).
    /// </summary>
    public class NotificationDeviceConverter : StringConverter
    {
        /// <summary>Displayed value of an empty GUID</summary>
        public const string DefaultName = "<Device having 'Used for notification'>";

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var names = Repository.Instance.Devices.OfType<OutputEmailDevice>().Select(i => i.Name).OrderBy(i => i, StringComparer.OrdinalIgnoreCase).ToList();
            names.Insert(0, DefaultName);
            return new StandardValuesCollection(names);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) => destType == typeof(string);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            var guid = value as string;
            if (string.IsNullOrEmpty(guid)) return DefaultName;
            var device = Repository.Instance.Devices.OfType<OutputEmailDevice>().FirstOrDefault(i => i.GUID == guid);
            return device != null ? device.Name : guid;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType) => srcType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var name = value as string;
            if (string.IsNullOrEmpty(name) || name == DefaultName) return "";
            var device = Repository.Instance.Devices.OfType<OutputEmailDevice>().FirstOrDefault(i => i.Name == name);
            return device != null ? device.GUID : name;
        }
    }
}
