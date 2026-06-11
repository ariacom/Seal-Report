using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Seal.AI;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// TypeConverter for <see cref="AIServerConfiguration.DefaultAgentGUID"/>: displays
    /// <see cref="AIAgentConfiguration.Name"/> values in the property-grid dropdown
    /// while storing and returning the corresponding <see cref="AIAgentConfiguration.GUID"/>.
    /// </summary>
    public class AIAgentConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var names = Repository.Instance.AIConfiguration.AIAgents
                .Select(a => a.Name)
                .OrderBy(n => n)
                .ToList();
            names.Insert(0, AIAgentConfiguration.FirstEnabledName);
            return new StandardValuesCollection(names);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) => destType == typeof(string);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            if (value != null)
            {
                if (string.IsNullOrEmpty(value.ToString()))
                    return AIAgentConfiguration.FirstEnabledName;
                var agent = Repository.Instance.AIConfiguration.AIAgents
                    .FirstOrDefault(a => a.GUID == value.ToString());
                if (agent != null) return agent.Name;
            }
            return base.ConvertTo(context, culture, value, destType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType) => srcType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value != null)
            {
                if (value.ToString() == AIAgentConfiguration.FirstEnabledName)
                    return "";
                var agent = Repository.Instance.AIConfiguration.AIAgents
                    .FirstOrDefault(a => a.Name == value.ToString());
                if (agent != null) return agent.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
