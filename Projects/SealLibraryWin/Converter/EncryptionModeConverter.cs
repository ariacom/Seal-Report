//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System.ComponentModel;
using System.Linq;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// TypeConverter for <see cref="SealServerConfiguration.EncryptionMode"/>: the obsolete RSA key container modes are proposed only if one of them is the current mode
    /// (their values stay readable to re-encrypt the secrets with the Certificate mode)
    /// </summary>
    public class EncryptionModeConverter : NamedEnumConverter
    {
        public EncryptionModeConverter() : base(typeof(EncryptionMode)) { }

        /// <summary>
        /// True if the mode is an obsolete RSA key container mode
        /// </summary>
        public static bool IsObsolete(EncryptionMode mode)
        {
            return mode == EncryptionMode.MachineRSAContainer || mode == EncryptionMode.UserRSAContainer;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            EncryptionMode? current = null;
            try
            {
                if (context?.PropertyDescriptor != null && context.Instance != null) current = (EncryptionMode)context.PropertyDescriptor.GetValue(context.Instance);
            }
            catch { }
            var values = System.Enum.GetValues(typeof(EncryptionMode)).Cast<EncryptionMode>().Where(i => !IsObsolete(i) || i == current).ToArray();
            return new StandardValuesCollection(values);
        }
    }
}
