//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// TypeConverter for <see cref="SealServerConfiguration.CertificateThumbprint"/>: proposes the certificates having an RSA key of the
    /// Windows stores 'Local Machine\Personal' and 'Current User\Personal' while storing their thumbprint; a thumbprint can also be typed.
    /// </summary>
    public class CertificateConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        static List<(string Display, string Thumbprint)> getCertificates()
        {
            var result = new List<(string, string)>();
            foreach (var location in new[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser })
            {
                try
                {
                    using var store = new X509Store(StoreName.My, location);
                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                    foreach (var certificate in store.Certificates)
                    {
                        using (certificate)
                        {
                            using var rsa = certificate.GetRSAPublicKey();
                            if (rsa == null) continue;
                            var display = $"{certificate.GetNameInfo(X509NameType.SimpleName, false)} ({certificate.Thumbprint}) - {(location == StoreLocation.LocalMachine ? "Local Machine" : "Current User")}{(certificate.HasPrivateKey ? "" : " - no private key")}";
                            if (!result.Exists(i => i.Item2 == certificate.Thumbprint)) result.Add((display, certificate.Thumbprint));
                        }
                    }
                }
                catch { }
            }
            return result;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var list = getCertificates().Select(i => i.Display).OrderBy(i => i, StringComparer.OrdinalIgnoreCase).ToList();
            list.Insert(0, "");
            return new StandardValuesCollection(list);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) => destType == typeof(string);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            var thumbprint = value as string;
            if (string.IsNullOrEmpty(thumbprint)) return "";
            var certificate = getCertificates().FirstOrDefault(i => i.Thumbprint.Equals(thumbprint, StringComparison.OrdinalIgnoreCase));
            return certificate.Display ?? thumbprint;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType) => srcType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var text = (value as string ?? "").Trim();
            if (string.IsNullOrEmpty(text)) return "";
            var certificate = getCertificates().FirstOrDefault(i => i.Display == text);
            if (certificate.Thumbprint != null) return certificate.Thumbprint;
            //Thumbprint typed or pasted (e.g. from certlm.msc, with spaces or invisible characters)
            return new string(text.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        }
    }
}
