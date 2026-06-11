using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Seal.AI;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// TypeConverter for <see cref="AIAgentConfiguration.ProviderGUID"/>: displays
    /// <see cref="AIProviderConfiguration.Name"/> values in the property-grid dropdown
    /// while storing and returning the corresponding <see cref="AIProviderConfiguration.GUID"/>.
    /// </summary>
    public class AIProviderConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var names = Repository.Instance.AIConfiguration.AIProviders
                .Select(p => p.Name)
                .OrderBy(n => n)
                .ToList();
            // Offer the "<Default Provider>" sentinel only when editing an agent,
            // not in the server's own Default Provider field (which it would reference).
            if (context?.Instance is AIAgentConfiguration)
                names.Insert(0, AIProviderConfiguration.DefaultProviderName);
            else
                names.Insert(0, AIProviderConfiguration.FirstEnabledName);
            return new StandardValuesCollection(names);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) => destType == typeof(string);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            if (value != null)
            {
                if (string.IsNullOrEmpty(value.ToString()) && !(context?.Instance is AIAgentConfiguration))
                    return AIProviderConfiguration.FirstEnabledName;
                if (value.ToString() == AIProviderConfiguration.DefaultProviderGUID)
                    return AIProviderConfiguration.DefaultProviderName;
                var provider = Repository.Instance.AIConfiguration.AIProviders
                    .FirstOrDefault(p => p.GUID == value.ToString());
                if (provider != null) return provider.Name;
            }
            return base.ConvertTo(context, culture, value, destType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType) => srcType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value != null)
            {
                if (value.ToString() == AIProviderConfiguration.FirstEnabledName)
                    return "";
                if (value.ToString() == AIProviderConfiguration.DefaultProviderName)
                    return AIProviderConfiguration.DefaultProviderGUID;
                var provider = Repository.Instance.AIConfiguration.AIProviders
                    .FirstOrDefault(p => p.Name == value.ToString());
                if (provider != null) return provider.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
