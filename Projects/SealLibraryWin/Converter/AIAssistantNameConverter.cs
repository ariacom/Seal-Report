//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Seal.AI;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// TypeConverter for <see cref="Seal.Model.SecurityGroup.AssistantGUID"/>:
    /// displays sentinel labels and assistant names in the property-grid dropdown
    /// while storing and returning the corresponding GUID.
    /// Null or empty GUID means &lt;No Assistant&gt;.
    /// </summary>
    public class AIAssistantNameConverter : StringConverter
    {

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var choices = new List<string> { AIAssistantConfiguration.DefaultAssistant, AIAssistantConfiguration.NoAssistant };
            choices.AddRange(
                Repository.Instance.AIConfiguration.AIAssistants
                    .Where(a => a.IsEnabled)
                    .Select(a => a.Name)
                    .OrderBy(n => n)
            );
            return new StandardValuesCollection(choices);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) => destType == typeof(string);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            var guid = value?.ToString();
            if (string.IsNullOrEmpty(guid))
                return AIAssistantConfiguration.NoAssistant;
            if (guid == AIAssistantConfiguration.DefaultAssistantGUIDValue)
                return AIAssistantConfiguration.DefaultAssistant;
            var assistant = Repository.Instance.AIConfiguration.AIAssistants
                .FirstOrDefault(a => a.GUID == guid);
            if (assistant != null) return assistant.Name;
            return base.ConvertTo(context, culture, value, destType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType) => srcType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var name = value?.ToString();
            if (string.IsNullOrEmpty(name) || name == AIAssistantConfiguration.NoAssistant)
                return string.Empty;
            if (name == AIAssistantConfiguration.DefaultAssistant)
                return AIAssistantConfiguration.DefaultAssistantGUIDValue;
            var assistant = Repository.Instance.AIConfiguration.AIAssistants
                .FirstOrDefault(a => a.Name == name);
            if (assistant != null) return assistant.GUID;
            return base.ConvertFrom(context, culture, value);
        }
    }
}
