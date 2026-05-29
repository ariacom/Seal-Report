using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Seal.AI;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// TypeConverter for <see cref="AIServerConfiguration.DefaultAssistantGUID"/>: displays
    /// <see cref="AIAssistantConfiguration.Name"/> values in the property-grid dropdown
    /// while storing and returning the corresponding <see cref="AIAssistantConfiguration.GUID"/>.
    /// </summary>
    public class AIAssistantConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var names = Repository.Instance.AIConfiguration.AIAssistants
                .Select(a => a.Name)
                .OrderBy(n => n)
                .ToList();
            return new StandardValuesCollection(names);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) => destType == typeof(string);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            if (value != null)
            {
                var assistant = Repository.Instance.AIConfiguration.AIAssistants
                    .FirstOrDefault(a => a.GUID == value.ToString());
                if (assistant != null) return assistant.Name;
            }
            return base.ConvertTo(context, culture, value, destType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType) => srcType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value != null)
            {
                var assistant = Repository.Instance.AIConfiguration.AIAssistants
                    .FirstOrDefault(a => a.Name == value.ToString());
                if (assistant != null) return assistant.GUID;
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
