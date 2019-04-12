//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Windows.Forms.Design;

namespace DynamicTypeDescriptor
{
    public enum CustomSortOrder
    {
        // no custom sorting
        None,
        // sort asscending using the property name or category name
        AscendingByName,
        // sort asscending using property id or categor id
        AscendingById,
        // sort descending using the property name or category name
        DescendingByName,
        // sort descending using property id or categor id
        DescendingById,
    }
    [Flags]
    public enum PropertyFlags
    {
        [StandardValue("None", "None of the flags should be applied to this property.")]
        None = 0,
        [StandardValue("Display name", "Display name should be retrieved from resource if possible for this property.")]
        LocalizeDisplayName = 1,
        [StandardValue("Category name", "Category name should be retrieved from resource if possible for this property.")]
        LocalizeCategoryName = 2,
        [StandardValue("Description", "Description string should be retrieved from resource if possible for this property.")]
        LocalizeDescription = 4,
        [StandardValue("Enumeration", "Enumerations' display strings should be retrieved from resource if possible  for this property if it is an enumeration type.")]
        LocalizeEnumerations = 8,
        [StandardValue("Exclusive", "Values can only be selected from a list and user are not allowed to type in the value for this property.")]
        ExclusiveStandardValues = 16,

        [StandardValue("Use resource for all string", "Use resource for all string for this property.")]
        LocalizeAllString = PropertyFlags.LocalizeDisplayName | PropertyFlags.LocalizeDescription |
              PropertyFlags.LocalizeCategoryName | PropertyFlags.LocalizeEnumerations,

        [StandardValue("Expandable", "Make property expandlabe if property type is IEnemerable")]
        ExpandIEnumerable = 32,

        [StandardValue("Supports standard values", "Property supports standard values.")]
        SupportStandardValues = 64,

        [StandardValue("All flags", "All of the flags should be applied to this property.")]
        All = PropertyFlags.LocalizeAllString | PropertyFlags.ExclusiveStandardValues | PropertyFlags.ExpandIEnumerable | PropertyFlags.SupportStandardValues,

        Default = PropertyFlags.LocalizeAllString | PropertyFlags.SupportStandardValues,
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class PropertyStateFlagsAttribute : Attribute
    {
        public PropertyStateFlagsAttribute()
          : base()
        {

        }
        public PropertyStateFlagsAttribute(PropertyFlags flags)
          : base()
        {
            m_Flags = flags;
        }

        private PropertyFlags m_Flags = PropertyFlags.All & ~PropertyFlags.ExclusiveStandardValues;

        public PropertyFlags Flags
        {
            get
            {
                return m_Flags;
            }
            set
            {
                m_Flags = value;
            }
        }



    }

    public interface IResourceAttribute
    {

        string BaseName
        {
            get;
            set;
        }

        string KeyPrefix
        {
            get;
            set;
        }

        string AssemblyFullName
        {
            get;
            set;
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ClassResourceAttribute : Attribute, IResourceAttribute
    {
        public ClassResourceAttribute()
          : base()
        {

        }
        public ClassResourceAttribute(string baseString)
          : base()
        {
            m_BaseName = baseString;
        }
        public ClassResourceAttribute(string baseString, string keyPrefix)
          : base()
        {
            m_BaseName = baseString;
            m_KeyPrefix = keyPrefix;
        }
        private string m_BaseName = String.Empty;

        public string BaseName
        {
            get
            {
                return m_BaseName;
            }
            set
            {
                m_BaseName = value;
            }
        }
        private string m_KeyPrefix = String.Empty;

        public string KeyPrefix
        {
            get
            {
                return m_KeyPrefix;
            }
            set
            {
                m_KeyPrefix = value;
            }
        }

        private string m_AssemblyFullName = String.Empty;
        public string AssemblyFullName
        {
            get
            {
                return m_AssemblyFullName;
            }
            set
            {
                m_AssemblyFullName = value;
            }
        }

        // Use the hash code of the string objects and xor them together.
        public override int GetHashCode()
        {
            return (BaseName.GetHashCode() ^ KeyPrefix.GetHashCode()) ^ AssemblyFullName.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            if (!(obj is ClassResourceAttribute))
            {
                return false;
            }
            ClassResourceAttribute other = obj as ClassResourceAttribute;

            if (String.Compare(this.BaseName, other.BaseName, true) == 0 &&
                String.Compare(this.AssemblyFullName, other.AssemblyFullName, true) == 0)
            {
                return true;
            }

            return false;
        }

        public override bool Match(object obj)
        {
            // Obviously a match.
            if (obj == this)
                return true;

            // Obviously we're not null, so no.
            if (obj == null)
                return false;

            if (obj is ClassResourceAttribute)
                // Combine the hash codes and see if they're unchanged.
                return (((ClassResourceAttribute)obj).GetHashCode() & GetHashCode())
                  == GetHashCode();
            else
                return false;
        }

    }

    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = true)]
    public class EnumResourceAttribute : Attribute, IResourceAttribute
    {
        public EnumResourceAttribute()
          : base()
        {

        }
        public EnumResourceAttribute(string baseString)
          : base()
        {
            m_BaseName = baseString;
        }
        public EnumResourceAttribute(string baseString, string keyPrefix)
          : base()
        {
            m_BaseName = baseString;
            m_KeyPrefix = keyPrefix;
        }
        private string m_BaseName = String.Empty;

        public string BaseName
        {
            get
            {
                return m_BaseName;
            }
            set
            {
                m_BaseName = value;
            }
        }
        private string m_KeyPrefix = String.Empty;

        public string KeyPrefix
        {
            get
            {
                return m_KeyPrefix;
            }
            set
            {
                m_KeyPrefix = value;
            }
        }

        private string m_AssemblyFullName = String.Empty;
        public string AssemblyFullName
        {
            get
            {
                return m_AssemblyFullName;
            }
            set
            {
                m_AssemblyFullName = value;
            }
        }


        // Use the hash code of the string objects and xor them together.
        public override int GetHashCode()
        {

            return (BaseName.GetHashCode() ^ KeyPrefix.GetHashCode()) ^ AssemblyFullName.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            if (!(obj is ClassResourceAttribute))
            {
                return false;
            }
            ClassResourceAttribute other = obj as ClassResourceAttribute;

            if (String.Compare(this.BaseName, other.BaseName, true) == 0 &&
                String.Compare(this.AssemblyFullName, other.AssemblyFullName, true) == 0)
            {
                return true;
            }

            return false;
        }

        public override bool Match(object obj)
        {
            // Obviously a match.
            if (obj == this)
                return true;

            // Obviously we're not null, so no.
            if (obj == null)
                return false;

            if (obj is ClassResourceAttribute)
                // Combine the hash codes and see if they're unchanged.
                return (((ClassResourceAttribute)obj).GetHashCode() & GetHashCode())
                  == GetHashCode();
            else
                return false;
        }

    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PropertyResourceAttribute : Attribute, IResourceAttribute
    {
        public PropertyResourceAttribute()
          : base()
        {

        }
        public PropertyResourceAttribute(string baseString)
          : base()
        {
            m_BaseName = baseString;
        }
        public PropertyResourceAttribute(string baseString, string keyPrefix)
          : base()
        {
            m_BaseName = baseString;
            m_KeyPrefix = keyPrefix;
        }
        private string m_BaseName = String.Empty;

        public string BaseName
        {
            get
            {
                return m_BaseName;
            }
            set
            {
                m_BaseName = value;
            }
        }
        private string m_KeyPrefix = String.Empty;

        public string KeyPrefix
        {
            get
            {
                return m_KeyPrefix;
            }
            set
            {
                m_KeyPrefix = value;
            }
        }

        private string m_AssemblyFullName = String.Empty;
        public string AssemblyFullName
        {
            get
            {
                return m_AssemblyFullName;
            }
            set
            {
                m_AssemblyFullName = value;
            }
        }

        // Use the hash code of the string objects and xor them together.
        public override int GetHashCode()
        {

            return (BaseName.GetHashCode() ^ KeyPrefix.GetHashCode()) ^ AssemblyFullName.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            if (!(obj is ClassResourceAttribute))
            {
                return false;
            }
            ClassResourceAttribute other = obj as ClassResourceAttribute;

            if (String.Compare(this.BaseName, other.BaseName, true) == 0 &&
                String.Compare(this.AssemblyFullName, other.AssemblyFullName, true) == 0)
            {
                return true;
            }

            return false;
        }

        public override bool Match(object obj)
        {
            // Obviously a match.
            if (obj == this)
                return true;

            // Obviously we're not null, so no.
            if (obj == null)
                return false;

            if (obj is ClassResourceAttribute)
                // Combine the hash codes and see if they're unchanged.
                return (((ClassResourceAttribute)obj).GetHashCode() & GetHashCode())
                  == GetHashCode();
            else
                return false;
        }
    }


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class IdAttribute : Attribute
    {
        public IdAttribute()
          : base()
        {
        }

        public IdAttribute(int propertyId, int categoryId)
          : base()
        {
            PropertyId = propertyId;
            CategoryId = categoryId;
        }
        private int m_PropertyId = 0;

        public int PropertyId
        {
            get
            {
                return m_PropertyId;
            }
            set
            {
                m_PropertyId = value;
            }
        }
        private int m_CategoryId = 0;

        public int CategoryId
        {
            get
            {
                return m_CategoryId;
            }
            set
            {
                m_CategoryId = value;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class StandardValueAttribute : Attribute
    {
        //public StandardValueAttribute()
        //{

        //}

        public StandardValueAttribute(object value)
        {
            m_Value = value;
        }
        public StandardValueAttribute(object value, string displayName)
        {
            m_DisplayName = displayName;
            m_Value = value;
        }
        public StandardValueAttribute(string displayName, string description)
        {
            m_DisplayName = displayName;
            m_Description = description;
        }
        private string m_DisplayName = String.Empty;
        public string DisplayName
        {
            get
            {
                if (String.IsNullOrEmpty(m_DisplayName))
                {
                    if (Value != null)
                    {
                        return Value.ToString();
                    }
                }
                return m_DisplayName;
            }
            set
            {
                m_DisplayName = value;
            }
        }

        private bool m_Visible = true;
        public bool Visible
        {
            get
            {
                return m_Visible;
            }
            set
            {
                m_Visible = value;
            }
        }

        private bool m_Enabled = true;
        public bool Enabled
        {
            get
            {
                return m_Enabled;
            }
            set
            {
                m_Enabled = value;
            }
        }

        private string m_Description = String.Empty;
        public string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                m_Description = value;
            }
        }

        internal object m_Value = null;

        public object Value
        {
            get
            {
                return m_Value;
            }
        }
        public override string ToString()
        {
            return DisplayName;
        }
        internal static StandardValueAttribute[] GetEnumItems(Type enumType)
        {
            if (enumType == null)
            {
                throw new ArgumentNullException("'enumInstance' is null.");
            }

            if (!enumType.IsEnum)
            {
                throw new ArgumentException("'enumInstance' is not Enum type.");
            }

            ArrayList arrAttr = new ArrayList();
            FieldInfo[] fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo fi in fields)
            {
                StandardValueAttribute[] attr = fi.GetCustomAttributes(typeof(StandardValueAttribute), false) as StandardValueAttribute[];

                if (attr != null && attr.Length > 0)
                {
                    attr[0].m_Value = fi.GetValue(null);
                    arrAttr.Add(attr[0]);
                }
                else
                {
                    StandardValueAttribute atr = new StandardValueAttribute(fi.GetValue(null));
                    arrAttr.Add(atr);
                }
            }
            StandardValueAttribute[] retAttr = arrAttr.ToArray(typeof(StandardValueAttribute)) as StandardValueAttribute[];
            return retAttr;
        }



    }

    public class StandardValueEditor : UITypeEditor
    {
        private StandardValueEditorUI m_ui = new StandardValueEditorUI();

        public StandardValueEditor()
        {

        }

        public override bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context)
        {
            return false;
        }

        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        public override bool IsDropDownResizable
        {
            get
            {
                return true;
            }
        }

        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                IWindowsFormsEditorService editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
                if (editorService == null)
                    return value;

                m_ui.SetData(context, editorService, value);

                editorService.DropDownControl(m_ui);

                value = m_ui.GetValue();

            }

            return value;
        }
    }

    public class PropertyValuePaintEditor : UITypeEditor
    {
        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        {
            // let the property browser know we'd like
            // to do custom painting.
            if (context != null)
            {
                if (context.PropertyDescriptor != null)
                {
                    if (context.PropertyDescriptor is CustomPropertyDescriptor)
                    {
                        CustomPropertyDescriptor cpd = context.PropertyDescriptor as CustomPropertyDescriptor;
                        return (cpd.ValueImage != null);
                    }
                }
            }
            return base.GetPaintValueSupported(context);
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.None;
        }
        public override void PaintValue(PaintValueEventArgs pe)
        {
            if (pe.Context != null)
            {
                if (pe.Context.PropertyDescriptor != null)
                {
                    if (pe.Context.PropertyDescriptor is CustomPropertyDescriptor)
                    {
                        CustomPropertyDescriptor cpd = pe.Context.PropertyDescriptor as CustomPropertyDescriptor;

                        if (cpd.ValueImage != null)
                        {
                            pe.Graphics.DrawImage(cpd.ValueImage, pe.Bounds);
                            return;
                        }
                    }
                }
            }
            base.PaintValue(pe);
        }

    }

    internal class StandardValuesConverter : TypeConverter
    {
        static int _COUNT = 0;
        public StandardValuesConverter()
          : base()
        {

        }
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            if (context != null && context.PropertyDescriptor is CustomPropertyDescriptor)
            {
                CustomPropertyDescriptor cpd = context.PropertyDescriptor as CustomPropertyDescriptor;
                IEnumerable enu = cpd.GetValue(context.Instance) as IEnumerable;
                if (enu != null && cpd.PropertyFlags != PropertyFlags.None && (cpd.PropertyFlags & PropertyFlags.ExpandIEnumerable) > 0)
                {
                    return true;
                }
            }
            return base.GetPropertiesSupported(context);

        }
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            if (value == null)
            {
                return base.GetProperties(context, value, attributes);
            }

            ICollection<StandardValueAttribute> col = null;
            Type propType = Type.Missing.GetType();
            if (context != null && context.PropertyDescriptor is CustomPropertyDescriptor)
            {
                CustomPropertyDescriptor cpd = context.PropertyDescriptor as CustomPropertyDescriptor;
                UpdateEnumDisplayText(cpd);
                col = cpd.StatandardValues;
                propType = cpd.PropertyType;
            }
            List<CustomPropertyDescriptor> pdl = new List<CustomPropertyDescriptor>();
            int nIndex = -1;
            if (pdl.Count == 0)
            {
                IEnumerable en = value as IEnumerable;
                if (en != null)
                {
                    IEnumerator enu = en.GetEnumerator();
                    enu.Reset();
                    while (enu.MoveNext())
                    {
                        nIndex++;
                        string sPropName = enu.Current.ToString();

                        IComponent comp = enu.Current as IComponent;
                        if (comp != null && comp.Site != null && !String.IsNullOrEmpty(comp.Site.Name))
                        {
                            sPropName = comp.Site.Name;
                        }
                        else if (propType.IsArray)
                        {
                            sPropName = "[" + nIndex.ToString() + "]";
                        }
                        pdl.Add(new CustomPropertyDescriptor(null, sPropName, enu.Current.GetType(), enu.Current));
                    }
                }
            }
            if (pdl.Count > 0)
            {
                return new PropertyDescriptorCollection(pdl.ToArray());
            }
            return base.GetProperties(context, value, attributes);
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return true;
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return true;
        }
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            if (context != null && context.PropertyDescriptor is CustomPropertyDescriptor)
            {
                CustomPropertyDescriptor cpd = context.PropertyDescriptor as CustomPropertyDescriptor;

                if (cpd.PropertyFlags != PropertyFlags.None && (cpd.PropertyFlags & PropertyFlags.SupportStandardValues) > 0)
                {
                    return true;
                }
            }
            return base.GetStandardValuesSupported(context);
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            if (context != null && context.PropertyDescriptor is CustomPropertyDescriptor)
            {
                CustomPropertyDescriptor cpd = context.PropertyDescriptor as CustomPropertyDescriptor;
                if (cpd.PropertyType == typeof(bool) || cpd.PropertyType.IsEnum)
                {
                    return true;
                }
                if (cpd.PropertyFlags != PropertyFlags.None && ((cpd.PropertyFlags & PropertyFlags.ExclusiveStandardValues) > 0))
                {
                    return true;
                }
                return false;
            }

            return base.GetStandardValuesExclusive(context);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            //WriteContext("ConvertFrom", context, value, Type.Missing.GetType( ));

            ICollection<StandardValueAttribute> col = null;
            Type propType = Type.Missing.GetType();
            if (context != null && context.PropertyDescriptor is CustomPropertyDescriptor)
            {
                CustomPropertyDescriptor cpd = context.PropertyDescriptor as CustomPropertyDescriptor;
                UpdateEnumDisplayText(cpd);
                col = cpd.StatandardValues;
                propType = cpd.PropertyType;
            }
            if (value == null)
            {
                return null;
            }
            else if (value is string)
            {
                if (propType.IsEnum)
                {
                    string sInpuValue = value as string;
                    string[] arrDispName = sInpuValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    StringBuilder sb = new StringBuilder(1000);
                    foreach (string sDispName in arrDispName)
                    {
                        string sTrimValue = sDispName.Trim();
                        foreach (StandardValueAttribute sva in col)
                        {
                            if (String.Compare(sva.Value.ToString(), sTrimValue, true) == 0 ||
                                String.Compare(sva.DisplayName, sTrimValue, true) == 0)
                            {
                                if (sb.Length > 0)
                                {
                                    sb.Append(",");
                                }
                                sb.Append(sva.Value.ToString());
                            }
                        }

                    }  // end of foreach..loop
                    return Enum.Parse(propType, sb.ToString(), true);
                }
                foreach (StandardValueAttribute sva in col)
                {
                    if (String.Compare(value.ToString(), sva.DisplayName, true, culture) == 0 ||
                        String.Compare(value.ToString(), sva.Value.ToString(), true, culture) == 0)
                    {
                        return sva.Value;

                    }
                }
                TypeConverter tc = TypeDescriptor.GetConverter(propType);
                if (tc != null)
                {
                    object convertedValue = null;
                    try
                    {
                        convertedValue = tc.ConvertFrom(context, culture, value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    if (tc.IsValid(convertedValue))
                    {
                        return convertedValue;
                    }
                }
            }
            else if (value.GetType() == propType)
            {
                return value;
            }
            else if (value is StandardValueAttribute)
            {
                return (value as StandardValueAttribute).Value;
            }

            return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            //WriteContext("ConvertTo", context, value, destinationType);

            ICollection<StandardValueAttribute> col = null;
            Type propType = Type.Missing.GetType();
            if (context != null && context.PropertyDescriptor is CustomPropertyDescriptor)
            {
                CustomPropertyDescriptor cpd = context.PropertyDescriptor as CustomPropertyDescriptor;
                UpdateEnumDisplayText(cpd);
                col = cpd.StatandardValues;
                propType = cpd.PropertyType;
            }
            if (value == null)
            {
                return null;
            }
            else if (value is string)
            {
                if (destinationType == typeof(string))
                {
                    return value;
                }
                else if (destinationType == propType)
                {
                    return ConvertFrom(context, culture, value);
                }
                else if (destinationType == typeof(StandardValueAttribute))
                {
                    foreach (StandardValueAttribute sva in col)
                    {
                        if (String.Compare(value.ToString(), sva.DisplayName, true, culture) == 0 ||
                            String.Compare(value.ToString(), sva.Value.ToString(), true, culture) == 0)
                        {
                            return sva;
                        }
                    }
                }
            }
            else if (value.GetType() == propType)
            {
                if (destinationType == typeof(string))
                {
                    if (propType.IsEnum)
                    {
                        string sDelimitedValues = Enum.Format(propType, value, "G");
                        string[] arrValue = sDelimitedValues.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        StringBuilder sb = new StringBuilder(1000);
                        foreach (string sDispName in arrValue)
                        {
                            string sTrimValue = sDispName.Trim();
                            foreach (StandardValueAttribute sva in col)
                            {
                                if (String.Compare(sva.Value.ToString(), sTrimValue, true) == 0 ||
                                    String.Compare(sva.DisplayName, sTrimValue, true) == 0)
                                {
                                    if (sb.Length > 0)
                                    {
                                        sb.Append(", ");
                                    }
                                    sb.Append(sva.DisplayName);
                                }
                            }

                        }  // end of foreach..loop
                        return sb.ToString();
                    }
                    foreach (StandardValueAttribute sva in col)
                    {
                        if (sva.Value.Equals(value))
                        {
                            return sva.DisplayName;
                        }
                    }
                    TypeConverter tc = TypeDescriptor.GetConverter(propType);
                    if (tc != null)
                    {
                        object convertedValue = null;
                        try
                        {
                            convertedValue = tc.ConvertTo(context, culture, value, destinationType);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        if (tc.IsValid(convertedValue))
                        {
                            return convertedValue;
                        }
                    }
                }
                else if (destinationType == typeof(StandardValueAttribute))
                {
                    foreach (StandardValueAttribute sva in col)
                    {
                        if (sva.Value.Equals(value))
                        {
                            return sva;
                        }
                    }

                }
                else if (destinationType == propType)
                {
                    return value;
                }
            }
            else if (value is StandardValueAttribute)
            {
                if (destinationType == typeof(string))
                {
                    return (value as StandardValueAttribute).DisplayName;
                }
                else if (destinationType == typeof(StandardValueAttribute))
                {
                    return value;
                }
                else if (destinationType == propType)
                {
                    return (value as StandardValueAttribute).Value;
                }
            }
            return base.ConvertFrom(context, culture, value);

        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            ICollection<StandardValueAttribute> col = null;
            if (context != null && context.PropertyDescriptor is CustomPropertyDescriptor)
            {
                CustomPropertyDescriptor cpd = context.PropertyDescriptor as CustomPropertyDescriptor;
                UpdateEnumDisplayText(cpd);
                col = cpd.StatandardValues;
            }

            List<StandardValueAttribute> list = new List<StandardValueAttribute>();
            foreach (StandardValueAttribute sva in col)
            {
                if (sva.Visible)
                {
                    list.Add(sva);
                }
            }
            if (list.Count > 0)
            {
                StandardValuesCollection svc = new StandardValuesCollection(list);
                return svc;
            }

            return base.GetStandardValues(context);
        }
        private void UpdateEnumDisplayText(CustomPropertyDescriptor cpd)
        {
            if (!(cpd.PropertyType.IsEnum || cpd.PropertyType == typeof(bool)))
            {
                return;
            }
            if ((cpd.PropertyFlags & PropertyFlags.LocalizeEnumerations) <= 0)
            {
                return;
            }
            string prefix = String.Empty;
            ResourceManager rm = null;
            StandardValueAttribute sva = null;

            sva = cpd.StatandardValues.FirstOrDefault() as StandardValueAttribute;

            // first try property itself
            if (cpd.ResourceManager != null)
            {
                string keyName = cpd.KeyPrefix + cpd.Name + "_" + sva.Value.ToString() + "_Name";
                string valueName = cpd.ResourceManager.GetString(keyName);
                if (!String.IsNullOrEmpty(valueName))
                {
                    rm = cpd.ResourceManager;
                    prefix = cpd.KeyPrefix + cpd.Name;
                }
            }

            // now try class level
            if (rm == null && cpd.ResourceManager != null)
            {
                string keyName = cpd.KeyPrefix + cpd.PropertyType.Name + "_" + sva.Value.ToString() + "_Name";
                string valueName = cpd.ResourceManager.GetString(keyName);
                if (!String.IsNullOrEmpty(valueName))
                {
                    rm = cpd.ResourceManager;
                    prefix = cpd.KeyPrefix + cpd.PropertyType.Name;
                }
            }

            // try the enum itself if still null
            if (rm == null && cpd.PropertyType.IsEnum)
            {
                EnumResourceAttribute attr = (EnumResourceAttribute)cpd.AllAttributes.FirstOrDefault(a => a is EnumResourceAttribute);
                if (attr != null)
                {
                    try
                    {
                        if (String.IsNullOrEmpty(attr.AssemblyFullName) == false)
                        {
                            rm = new ResourceManager(attr.BaseName, Assembly.ReflectionOnlyLoad(attr.AssemblyFullName));
                        }
                        else
                        {
                            rm = new ResourceManager(attr.BaseName, cpd.PropertyType.Assembly);
                        }
                        prefix = attr.KeyPrefix + cpd.PropertyType.Name;
                    }
                    catch
                    {
                        return;
                    }
                }
            }

            if (rm != null)
            {
                foreach (StandardValueAttribute sv in cpd.StatandardValues)
                {
                    string keyName = prefix + "_" + sv.Value.ToString() + "_Name";  // display name
                    string keyDesc = prefix + "_" + sv.Value.ToString() + "_Desc"; // description
                    string dispName = String.Empty;
                    string description = String.Empty;

                    try
                    {
                        dispName = rm.GetString(keyName);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    if (String.IsNullOrEmpty(dispName) == false)
                    {
                        sv.DisplayName = dispName;
                    }

                    try
                    {
                        description = rm.GetString(keyDesc);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    if (String.IsNullOrEmpty(description) == false)
                    {
                        sv.Description = description;
                    }
                }
            }
        }

        private void WriteContext(string prefix, ITypeDescriptorContext ctx, object value, Type destinationType)
        {
            _COUNT++;
            StringBuilder sb = new StringBuilder(1024);

            if (ctx != null)
            {
                if (ctx.Instance != null)
                {
                    sb.Append("ctx.Instance is " + ctx.Instance.ToString() + ". ");
                }

                if (ctx.PropertyDescriptor != null)
                {
                    sb.Append("ctx.PropertyDescriptor is " + ctx.PropertyDescriptor.ToString() + ". ");
                }
            }
            else
            {
                sb.Append("ctx is null. ");
            }

            if (value == null)
            {
                sb.AppendLine("Value is null. ");
            }
            else
            {
                sb.AppendLine("Value is " + value.ToString() + ", " + value.GetType().ToString() + ". ");
            }
            sb.AppendLine(destinationType.ToString());
            Console.WriteLine(_COUNT.ToString() + " " + prefix + ": " + sb.ToString());
        }

    }

    internal class PropertyDescriptorList : List<CustomPropertyDescriptor>
    {
        public PropertyDescriptorList()
        {

        }
    }

    internal class AttributeList : List<Attribute>
    {
        public AttributeList()
        {

        }
        public AttributeList(AttributeCollection ac)
        {
            foreach (Attribute attr in ac)
            {
                this.Add(attr);
            }
        }
        public AttributeList(Attribute[] aa)
        {
            foreach (Attribute attr in aa)
            {
                this.Add(attr);
            }
        }
    }

    internal class PropertySorter : IComparer<CustomPropertyDescriptor>
    {
        #region IComparer<PropertyDescriptor> Members

        public int Compare(CustomPropertyDescriptor x, CustomPropertyDescriptor y)
        {

            switch (m_SortOrder)
            {
                case CustomSortOrder.AscendingById:
                    if (x.PropertyId > y.PropertyId)
                    {
                        return 1;
                    }
                    else if (x.PropertyId < y.PropertyId)
                    {
                        return -1;
                    }
                    return 0;
                case CustomSortOrder.AscendingByName:
                    return (String.Compare(x.DisplayName, y.DisplayName, true));
                case CustomSortOrder.DescendingById:
                    if (x.PropertyId > y.PropertyId)
                    {
                        return -1;
                    }
                    else if (x.PropertyId < y.PropertyId)
                    {
                        return 1;
                    }
                    return 0;
                case CustomSortOrder.DescendingByName:
                    return (String.Compare(y.DisplayName, x.DisplayName, true));
            }
            return 0;
        }

        #endregion

        private CustomSortOrder m_SortOrder = CustomSortOrder.AscendingByName;

        public CustomSortOrder SortOrder
        {
            get
            {
                return m_SortOrder;
            }
            set
            {
                m_SortOrder = value;
            }
        }
    }

    internal class CategorySorter : IComparer<CustomPropertyDescriptor>
    {
        #region IComparer<PropertyDescriptor> Members

        public int Compare(CustomPropertyDescriptor x, CustomPropertyDescriptor y)
        {
            x.TabAppendCount = 0;
            y.TabAppendCount = 0;
            switch (m_SortOrder)
            {
                case CustomSortOrder.AscendingById:
                    if (x.CategoryId > y.CategoryId)
                    {
                        return 1;
                    }
                    else if (x.CategoryId < y.CategoryId)
                    {
                        return -1;
                    }
                    return 0;
                case CustomSortOrder.AscendingByName:
                    return (String.Compare(x.Category, y.Category, true));
                case CustomSortOrder.DescendingById:
                    if (x.CategoryId > y.CategoryId)
                    {
                        return -1;
                    }
                    else if (x.CategoryId < y.CategoryId)
                    {
                        return 1;
                    }
                    return 0;
                case CustomSortOrder.DescendingByName:
                    return (String.Compare(y.Category, x.Category, true));
            }
            return 0;
        }

        #endregion


        private CustomSortOrder m_SortOrder = CustomSortOrder.AscendingByName;

        public CustomSortOrder SortOrder
        {
            get
            {
                return m_SortOrder;
            }
            set
            {
                m_SortOrder = value;
            }
        }
    }

    public class DynamicCustomTypeDescriptor : CustomTypeDescriptor
    {
        private PropertyDescriptorList m_pdl = new PropertyDescriptorList();
        private object m_instance = null;
        private Hashtable m_hashRM = new Hashtable();
        public DynamicCustomTypeDescriptor(ICustomTypeDescriptor ctd, object instance)
          : base(ctd)
        {
            m_instance = instance;
            GetProperties();
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {

            List<CustomPropertyDescriptor> pdl = m_pdl.FindAll(pd => pd.Attributes.Contains(attributes));

            PreProcess(pdl);
            PropertyDescriptorCollection pdcReturn = new PropertyDescriptorCollection(pdl.ToArray());

            return pdcReturn;
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            if (m_pdl.Count == 0)
            {
                PropertyDescriptorCollection pdc = base.GetProperties();  // this gives us a readonly collection, no good    
                foreach (PropertyDescriptor pd in pdc)
                {
                    if (!(pd is CustomPropertyDescriptor))
                    {
                        CustomPropertyDescriptor cpd = new CustomPropertyDescriptor(base.GetPropertyOwner(pd), pd);
                        m_pdl.Add(cpd);
                    }
                }
            }

            List<CustomPropertyDescriptor> pdl = m_pdl.FindAll(pd => pd != null);

            PreProcess(pdl);
            PropertyDescriptorCollection pdcReturn = new PropertyDescriptorCollection(m_pdl.ToArray());

            return pdcReturn;
        }

        private void PreProcess(List<CustomPropertyDescriptor> pdl)
        {
            if (m_PropertySortOrder != CustomSortOrder.None && pdl.Count > 0)
            {
                PropertySorter propSorter = new PropertySorter();
                propSorter.SortOrder = m_PropertySortOrder;
                pdl.Sort(propSorter);
            }
            UpdateCategoryTabAppendCount();
            UpdateResourceManager();
        }
        private CustomSortOrder m_PropertySortOrder = CustomSortOrder.AscendingById;

        public CustomSortOrder PropertySortOrder
        {
            get
            {
                return m_PropertySortOrder;
            }
            set
            {
                m_PropertySortOrder = value;
            }
        }
        private CustomSortOrder m_CategorySortOrder = CustomSortOrder.AscendingById;

        public CustomSortOrder CategorySortOrder
        {
            get
            {
                return m_CategorySortOrder;
            }
            set
            {
                m_CategorySortOrder = value;
            }
        }
        private void UpdateResourceManager()
        {
            foreach (CustomPropertyDescriptor cpd in m_pdl)
            {
                IResourceAttribute attr = (PropertyResourceAttribute)cpd.AllAttributes.FirstOrDefault(a => a is PropertyResourceAttribute);
                if (attr == null)
                {
                    AttributeCollection ac = GetAttributes();
                    AttributeList al = new AttributeList(ac);
                    attr = (ClassResourceAttribute)al.FirstOrDefault(a => a is ClassResourceAttribute);
                }
                if (attr == null)
                {
                    cpd.ResourceManager = null;
                    continue;
                }
                cpd.KeyPrefix = attr.KeyPrefix;
                ResourceManager rm = m_hashRM[attr] as ResourceManager;
                if (rm != null)
                {
                    cpd.ResourceManager = rm;
                    continue;
                }
                try
                {
                    if (String.IsNullOrEmpty(attr.AssemblyFullName) == false)
                    {
                        rm = new ResourceManager(attr.BaseName, Assembly.ReflectionOnlyLoad(attr.AssemblyFullName));
                    }
                    else
                    {
                        rm = new ResourceManager(attr.BaseName, base.GetPropertyOwner(cpd).GetType().Assembly);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }
                m_hashRM.Add(attr, rm);
                cpd.ResourceManager = rm;
            }
        }
        private void UpdateCategoryTabAppendCount()
        {
            // get a copy of the list as we do not want to sort around the actual list
            List<CustomPropertyDescriptor> pdl = m_pdl.FindAll(pd => pd != null);
            if (pdl.Count == 0)
            {
                return;
            }
            CategorySorter propSorter = new CategorySorter();

            int nTabCount = 0;

            switch (m_CategorySortOrder)
            {
                case CustomSortOrder.AscendingById:
                    propSorter.SortOrder = CustomSortOrder.DescendingById;
                    pdl.Sort(propSorter);
                    nTabCount = 0;
                    int sortIndex = pdl[0].CategoryId;
                    foreach (CustomPropertyDescriptor cpd in pdl)
                    {
                        if (cpd.CategoryId == sortIndex)
                        {
                            cpd.TabAppendCount = nTabCount;
                        }
                        else
                        {
                            sortIndex = cpd.CategoryId;
                            nTabCount++;
                            cpd.TabAppendCount = nTabCount;
                        }
                    }
                    break;
                case CustomSortOrder.None:
                case CustomSortOrder.AscendingByName:  // by default, property grid sorts the category ascendingly
                    foreach (CustomPropertyDescriptor cpd in m_pdl)
                    {
                        cpd.TabAppendCount = 0;
                    }
                    break;
                case CustomSortOrder.DescendingById:
                    propSorter.SortOrder = CustomSortOrder.AscendingById;
                    pdl.Sort(propSorter);
                    nTabCount = 0;
                    int nCategorySortIndex = pdl[0].CategoryId;
                    foreach (CustomPropertyDescriptor cpd in pdl)
                    {
                        if (nCategorySortIndex == cpd.CategoryId)
                        {
                            cpd.TabAppendCount = nTabCount;
                        }
                        else
                        {
                            nCategorySortIndex = cpd.CategoryId;
                            nTabCount++;
                            cpd.TabAppendCount = nTabCount;
                        }
                    }
                    break;
                case CustomSortOrder.DescendingByName:
                    propSorter.SortOrder = CustomSortOrder.AscendingByName;
                    pdl.Sort(propSorter);
                    nTabCount = 0;
                    pdl[0].TabAppendCount = 0;
                    string sCat = pdl[0].Category;
                    foreach (CustomPropertyDescriptor cpd in pdl)
                    {
                        cpd.TabAppendCount = 0;
                        if (String.Compare(sCat, cpd.Category) == 0)
                        {
                            cpd.TabAppendCount = nTabCount;
                        }
                        else
                        {
                            sCat = cpd.Category;
                            nTabCount++;
                            cpd.TabAppendCount = nTabCount;
                        }
                    }
                    break;
            }
        }

        private ISite m_site = null;
        public ISite GetSite()
        {
            if (m_site == null)
            {
                SimpleSite site = new SimpleSite();
                IPropertyValueUIService service = new PropertyValueUIService();
                service.AddPropertyValueUIHandler(new PropertyValueUIHandler(this.GenericPropertyValueUIHandler));
                site.AddService(service);
                m_site = site;
            }
            return m_site;
        }

        private void GenericPropertyValueUIHandler(ITypeDescriptorContext context, PropertyDescriptor propDesc, ArrayList itemList)
        {
            CustomPropertyDescriptor cpd = propDesc as CustomPropertyDescriptor;
            if (cpd != null)
            {
                itemList.AddRange(cpd.StateItems as ICollection);
            }
        }

        public CustomPropertyDescriptor GetProperty(string propertyName)
        {
            CustomPropertyDescriptor cpd = m_pdl.FirstOrDefault(a => String.Compare(a.Name, propertyName, true) == 0);
            return cpd;
        }
        public CustomPropertyDescriptor CreateProperty(string name, Type type, object value, int index, params Attribute[] attributes)
        {
            CustomPropertyDescriptor cpd = new CustomPropertyDescriptor(m_instance, name, type, value, attributes);
            if (index == -1)
            {
                m_pdl.Add(cpd);
            }
            else
            {
                m_pdl.Insert(index, cpd);
            }
            TypeDescriptor.Refresh(m_instance);
            return cpd;
        }
        public bool RemoveProperty(string propertyName)
        {
            CustomPropertyDescriptor cpd = m_pdl.FirstOrDefault(a => String.Compare(a.Name, propertyName, true) == 0);
            bool bReturn = m_pdl.Remove(cpd);
            TypeDescriptor.Refresh(m_instance);
            return bReturn;
        }
        public void ResetProperties()
        {
            m_pdl.Clear();
            GetProperties();
        }
    }

    internal class CustomTypeDescriptionProvider : TypeDescriptionProvider
    {
        private TypeDescriptionProvider m_parent = null;
        private ICustomTypeDescriptor m_ctd = null;

        public CustomTypeDescriptionProvider()
          : base()
        {

        }

        public CustomTypeDescriptionProvider(TypeDescriptionProvider parent)
          : base(parent)
        {
            m_parent = parent;
        }
        public CustomTypeDescriptionProvider(TypeDescriptionProvider parent, ICustomTypeDescriptor ctd)
          : base(parent)
        {
            m_parent = parent;
            m_ctd = ctd;
        }
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            return m_ctd;
        }


    }

    public static class ProviderInstaller
    {
        public static DynamicCustomTypeDescriptor Install(object instance)
        {
            TypeDescriptionProvider parentProvider = TypeDescriptor.GetProvider(instance);
            ICustomTypeDescriptor parentCtd = parentProvider.GetTypeDescriptor(instance);
            DynamicCustomTypeDescriptor ourCtd = new DynamicCustomTypeDescriptor(parentCtd, instance);
            CustomTypeDescriptionProvider ourProvider = new CustomTypeDescriptionProvider(parentProvider, ourCtd);
            TypeDescriptor.AddProvider(ourProvider, instance);
            return ourCtd;
        }
    }

    public class CustomPropertyDescriptor : PropertyDescriptor
    {
        private static Hashtable m_hashRM = new Hashtable();

        internal object m_owner = null;
        private Type m_PropType = Type.Missing.GetType();
        private AttributeList m_Attributes = new AttributeList();
        private PropertyDescriptor m_pd = null;
        private Collection<PropertyValueUIItem> m_colUIItem = new Collection<PropertyValueUIItem>();

        internal CustomPropertyDescriptor(object owner, string sName, Type type, object value, params Attribute[] attributes)
          : base(sName, attributes)
        {
            this.m_owner = owner;
            this.m_value = value;
            m_PropType = type;
            m_Attributes.AddRange(attributes);

            UpdateMemberData();
        }
        internal CustomPropertyDescriptor(object owner, PropertyDescriptor pd)
          : base(pd)
        {
            m_pd = pd;
            m_owner = owner;
            m_Attributes = new AttributeList(pd.Attributes);
            //EP: update only if browsable...
            if (m_Attributes.Exists(i => i is BrowsableAttribute && ((BrowsableAttribute)i).Browsable)) UpdateMemberData();
        }
        public override TypeConverter Converter
        {
            get
            {
                TypeConverterAttribute tca = (TypeConverterAttribute)m_Attributes.FirstOrDefault(a => a is TypeConverterAttribute);

                if (tca != null)
                {
                    return base.Converter;
                }

                if (StatandardValues.Count > 0)
                {
                    return new StandardValuesConverter();
                }
                IEnumerable en = this.GetValue(this.m_owner) as IEnumerable;
                if (en != null && (this.PropertyFlags & PropertyFlags.ExpandIEnumerable) > 0)
                {
                    return new StandardValuesConverter();
                }
                return base.Converter;

            }
        }
        private void UpdateMemberData()
        {

            if (m_pd != null)
            {
                m_value = m_pd.GetValue(m_owner);
            }

            if (PropertyType.IsEnum)
            {
                StandardValueAttribute[] sva = StandardValueAttribute.GetEnumItems(PropertyType);
                this.m_StatandardValues.AddRange(sva);
            }
            else if (PropertyType == typeof(bool))
            {
                this.m_StatandardValues.Add(new StandardValueAttribute(true));
                this.m_StatandardValues.Add(new StandardValueAttribute(false));
            }
        }

        public override Type ComponentType
        {
            get
            {
                return m_owner.GetType();
            }
        }
        public override Type PropertyType
        {
            get
            {
                if (m_pd != null)
                {
                    return this.m_pd.PropertyType;
                }
                return m_PropType;
            }
        }

        protected override Attribute[] AttributeArray
        {
            get
            {
                return m_Attributes.ToArray();
            }
            set
            {
                m_Attributes.Clear();
                m_Attributes.AddRange(value);
            }
        }

        public override AttributeCollection Attributes
        {
            get
            {
                AttributeCollection ac = new AttributeCollection(m_Attributes.ToArray());
                return ac;
            }
        }
        protected override void FillAttributes(IList attributeList)
        {
            foreach (Attribute attr in m_Attributes)
            {
                attributeList.Add(attr);
            }
        }
        public IList<Attribute> AllAttributes
        {
            get
            {
                return m_Attributes;
            }
        }
        /// <summary>
        /// Must override abstract properties.
        /// </summary>
        /// 

        public override bool IsLocalizable
        {
            get
            {
                LocalizableAttribute attr = (LocalizableAttribute)m_Attributes.FirstOrDefault(a => a is LocalizableAttribute);
                if (attr != null)
                {
                    return attr.IsLocalizable;
                }
                return base.IsLocalizable;
            }
        }
        public void SetIsLocalizable(bool isLocalizable)
        {
            LocalizableAttribute attr = (LocalizableAttribute)m_Attributes.FirstOrDefault(a => a is LocalizableAttribute);
            if (attr != null)
            {
                m_Attributes.RemoveAll(a => a is LocalizableAttribute);
            }
            attr = new LocalizableAttribute(isLocalizable);
            m_Attributes.Add(attr);
        }
        public override bool IsReadOnly
        {
            get
            {
                ReadOnlyAttribute attr = (ReadOnlyAttribute)m_Attributes.FirstOrDefault(a => a is ReadOnlyAttribute);
                if (attr != null)
                {
                    return attr.IsReadOnly;
                }
                return false;
            }
        }
        public void SetIsReadOnly(bool isReadOnly)
        {
            ReadOnlyAttribute attr = (ReadOnlyAttribute)m_Attributes.FirstOrDefault(a => a is ReadOnlyAttribute);
            if (attr != null)
            {
                m_Attributes.RemoveAll(a => a is ReadOnlyAttribute);
            }
            attr = new ReadOnlyAttribute(isReadOnly);
            m_Attributes.Add(attr);
        }
        public override bool IsBrowsable
        {
            get
            {
                BrowsableAttribute attr = (BrowsableAttribute)m_Attributes.FirstOrDefault(a => a is BrowsableAttribute);
                if (attr != null)
                {
                    return attr.Browsable;
                }
                return base.IsBrowsable;
            }
        }
        public void SetIsBrowsable(bool isBrowsable)
        {
            BrowsableAttribute attr = (BrowsableAttribute)m_Attributes.FirstOrDefault(a => a is BrowsableAttribute);
            if (attr != null)
            {
                m_Attributes.RemoveAll(a => a is BrowsableAttribute);
            }
            attr = new BrowsableAttribute(isBrowsable);
            m_Attributes.Add(attr);
        }

        private string m_KeyPrefix = String.Empty;

        internal string KeyPrefix
        {
            get
            {
                return m_KeyPrefix;
            }
            set
            {
                m_KeyPrefix = value;
            }
        }

        public override string DisplayName
        {
            get
            {

                if (this.ResourceManager != null && (this.PropertyFlags & PropertyFlags.LocalizeDisplayName) > 0)
                {
                    string sKey = KeyPrefix + base.Name + "_Name";

                    string sResult = this.ResourceManager.GetString(sKey, CultureInfo.CurrentUICulture);
                    if (!String.IsNullOrEmpty(sResult))
                    {
                        return sResult;
                    }
                }
                DisplayNameAttribute attr = (DisplayNameAttribute)m_Attributes.FirstOrDefault(a => a is DisplayNameAttribute);
                if (attr != null)
                {
                    return attr.DisplayName;
                }
                return base.DisplayName;
            }
        }
        public void SetDisplayName(string displayName)
        {
            DisplayNameAttribute attr = (DisplayNameAttribute)m_Attributes.FirstOrDefault(a => a is DisplayNameAttribute);
            if (attr != null)
            {
                m_Attributes.RemoveAll(a => a is DisplayNameAttribute);
            }
            attr = new DisplayNameAttribute(displayName);
            m_Attributes.Add(attr);
        }
        public override string Category
        {
            get
            {
                string sResult = String.Empty;
                if (this.ResourceManager != null && CategoryId != 0 && (this.PropertyFlags & PropertyFlags.LocalizeCategoryName) > 0)
                {
                    string sKey = KeyPrefix + "Cat" + CategoryId.ToString();
                    sResult = this.ResourceManager.GetString(sKey, CultureInfo.CurrentUICulture);
                    if (!String.IsNullOrEmpty(sResult))
                    {
                        return sResult.PadLeft(sResult.Length + m_TabAppendCount, '\t');
                    }

                }
                CategoryAttribute attr = (CategoryAttribute)m_Attributes.FirstOrDefault(a => a is CategoryAttribute);
                if (attr != null)
                {
                    sResult = attr.Category;
                }
                if (String.IsNullOrEmpty(sResult))
                {
                    sResult = base.Category;
                }
                return sResult.PadLeft(base.Category.Length + m_TabAppendCount, '\t');
            }
        }
        public void SetCategory(string category)
        {
            CategoryAttribute attr = (CategoryAttribute)m_Attributes.FirstOrDefault(a => a is CategoryAttribute);
            if (attr != null)
            {
                m_Attributes.RemoveAll(a => a is CategoryAttribute);
            }
            attr = new CategoryAttribute(category);
            m_Attributes.Add(attr);
        }
        public override string Description
        {
            get
            {
                if (this.ResourceManager != null && (this.PropertyFlags & PropertyFlags.LocalizeDescription) > 0)
                {
                    string sKey = KeyPrefix + base.Name + "_Desc";
                    string sResult = this.ResourceManager.GetString(sKey, CultureInfo.CurrentUICulture);
                    if (!String.IsNullOrEmpty(sResult))
                    {
                        return sResult;
                    }
                }
                DescriptionAttribute attr = (DescriptionAttribute)m_Attributes.FirstOrDefault(a => a is DescriptionAttribute);
                if (attr != null)
                {
                    return attr.Description;
                }
                return base.Description;
            }
        }
        public void SetDescription(string description)
        {
            DescriptionAttribute attr = (DescriptionAttribute)m_Attributes.FirstOrDefault(a => a is DescriptionAttribute);
            if (attr != null)
            {
                m_Attributes.RemoveAll(a => a is DescriptionAttribute);
            }
            attr = new DescriptionAttribute(description);
            m_Attributes.Add(attr);
        }

        public object DefaultValue
        {
            get
            {
                DefaultValueAttribute attr = (DefaultValueAttribute)m_Attributes.FirstOrDefault(a => a is DefaultValueAttribute);
                if (attr != null)
                {
                    return attr.Value;
                }
                return null;
            }
            set
            {
                DefaultValueAttribute attr = (DefaultValueAttribute)m_Attributes.FirstOrDefault(a => a is DefaultValueAttribute);
                if (attr == null)
                {
                    m_Attributes.RemoveAll(a => a is DefaultValueAttribute);
                }
                attr = new DefaultValueAttribute(value);
                m_Attributes.Add(attr); //Fix
            }
        }

        public int PropertyId
        {
            get
            {
                IdAttribute rsa = (IdAttribute)m_Attributes.FirstOrDefault(a => a is IdAttribute);
                if (rsa != null)
                {
                    return rsa.PropertyId;
                }
                return 0;
            }
            set
            {
                IdAttribute rsa = (IdAttribute)m_Attributes.FirstOrDefault(a => a is IdAttribute);
                if (rsa == null)
                {
                    rsa = new IdAttribute();
                    m_Attributes.Add(rsa);
                }
                rsa.PropertyId = value;
            }
        }
        public int CategoryId
        {
            get
            {
                IdAttribute rsa = (IdAttribute)m_Attributes.FirstOrDefault(a => a is IdAttribute);
                if (rsa != null)
                {
                    return rsa.CategoryId;
                }
                return 0;
            }
            set
            {
                IdAttribute rsa = (IdAttribute)m_Attributes.FirstOrDefault(a => a is IdAttribute);
                if (rsa == null)
                {
                    rsa = new IdAttribute();
                    m_Attributes.Add(rsa);
                }
                rsa.CategoryId = value;
            }
        }
        private int m_TabAppendCount = 0;

        internal int TabAppendCount
        {
            get
            {
                return m_TabAppendCount;
            }
            set
            {
                m_TabAppendCount = value;
            }
        }

        private ResourceManager m_ResourceManager = null;

        internal ResourceManager ResourceManager
        {
            get
            {
                return m_ResourceManager;
            }
            set
            {
                m_ResourceManager = null;// value;
            }
        }

        private object m_value = null;
        public override object GetValue(object component)
        {
            if (m_pd != null)
            {
                return m_pd.GetValue(component);
            }
            return m_value;
        }

        public override void SetValue(object component, object value)
        {
            if (value != null && value is StandardValueAttribute)
            {
                m_value = (value as StandardValueAttribute).Value;
            }
            else
            {
                m_value = value;
            }

            if (m_pd != null)
            {
                m_pd.SetValue(component, m_value);
                this.OnValueChanged(this, new EventArgs());

            }
            else
            {
                EventHandler eh = this.GetValueChangedHandler(m_owner);
                if (eh != null)
                {
                    eh.Invoke(this, new EventArgs());
                }
                this.OnValueChanged(this, new EventArgs());
            }
        }
        protected override void OnValueChanged(object component, EventArgs e)
        {
            MemberDescriptor md = component as MemberDescriptor;

            base.OnValueChanged(component, e);
        }

        /// <summary>
        /// Abstract base members
        /// </summary>			
        public override void ResetValue(object component)
        {
            DefaultValueAttribute dva = (DefaultValueAttribute)m_Attributes.FirstOrDefault(a => a is DefaultValueAttribute);
            if (dva == null)
            {
                return;
            }
            SetValue(component, dva.Value);
        }

        public override bool CanResetValue(object component)
        {
            //Specific for parameters
         /*   if (m_value != null && component is ParametersEditor)
            {
                var parameter = ((ParametersEditor)component).GetParameter(Name);
                if (parameter != null) return (m_value.ToString() != parameter.ConfigValue);
            }
            */
            DefaultValueAttribute dva = (DefaultValueAttribute)m_Attributes.FirstOrDefault(a => a is DefaultValueAttribute);
            if (dva == null) return false;
            if (dva.Value == null) return true;

            bool bOk = (dva.Value.Equals(m_value));
            return !bOk;

        }

        public override bool ShouldSerializeValue(object component)
        {
            return CanResetValue(m_owner);
        }

        public override PropertyDescriptorCollection GetChildProperties(object instance, Attribute[] filter)
        {
            PropertyDescriptorCollection pdc = null;
            TypeConverter tc = this.Converter;
            if (tc.GetPropertiesSupported(null) == false)
            {
                pdc = base.GetChildProperties(instance, filter);
            }
            else
            {

            }
            if (m_pd != null)
            {
                tc = m_pd.Converter;
            }
            else
            {
                //pdc = base.GetChildProperties(instance, filter);// this gives us a readonly collection, no good    
                tc = TypeDescriptor.GetConverter(instance, true);
            }
            if (pdc == null || pdc.Count == 0)
            {
                return pdc;
            }
            if (pdc[0] is CustomPropertyDescriptor)
            {
                return pdc;
            }
            // now wrap these properties with our CustomPropertyDescriptor
            PropertyDescriptorList pdl = new PropertyDescriptorList();

            foreach (PropertyDescriptor pd in pdc)
            {
                if (pd is CustomPropertyDescriptor)
                {
                    pdl.Add(pd as CustomPropertyDescriptor);
                }
                else
                {
                    pdl.Add(new CustomPropertyDescriptor(instance, pd));
                }
            }

            pdl.Sort(new PropertySorter());
            PropertyDescriptorCollection pdcReturn = new PropertyDescriptorCollection(pdl.ToArray());
            pdcReturn.Sort();
            return pdcReturn;

        }

        public ICollection<PropertyValueUIItem> StateItems
        {
            get
            {
                return m_colUIItem;
            }
        }

        private List<StandardValueAttribute> m_StatandardValues = new List<StandardValueAttribute>();
        public ICollection<StandardValueAttribute> StatandardValues
        {
            get
            {
                if (PropertyType.IsEnum || PropertyType == typeof(bool))
                {
                    return m_StatandardValues.AsReadOnly();
                }
                return m_StatandardValues;
            }
        }
        private Image m_ValueImage = null;

        public Image ValueImage
        {
            get
            {
                return m_ValueImage;
            }
            set
            {
                m_ValueImage = value;
            }
        }

        public PropertyFlags PropertyFlags
        {
            get
            {
                PropertyStateFlagsAttribute attr = (PropertyStateFlagsAttribute)m_Attributes.FirstOrDefault(a => a is PropertyStateFlagsAttribute);
                if (attr == null)
                {
                    attr = new PropertyStateFlagsAttribute();
                    m_Attributes.Add(attr);
                    attr.Flags = PropertyFlags.Default;
                }

                return attr.Flags;
            }
            set
            {
                PropertyStateFlagsAttribute attr = (PropertyStateFlagsAttribute)m_Attributes.FirstOrDefault(a => a is PropertyStateFlagsAttribute);
                if (attr == null)
                {
                    attr = new PropertyStateFlagsAttribute();
                    m_Attributes.Add(attr);
                    attr.Flags = PropertyFlags.Default;
                }
                attr.Flags = value;

            }
        }

    }
}


