//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Converter;
using Seal.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;

namespace Seal.Model
{
    public class ParametersEditor : RootEditor
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                //     foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        public override string ToString()
        {
            return "";
        }
        List<Parameter> _parameters;
        List<Parameter> _strings;
        List<Parameter> _texts;
        List<Parameter> _enums;
        List<Parameter> _numerics;
        List<Parameter> _bools;

        void initList(List<Parameter> list, string prefix)
        {
            int index = 0;
            foreach (var parameter in list)
            {
                var propName = string.Format("{0}{1}", prefix, index++);
                var property = GetProperty(propName);
                if (property != null)
                {
                    property.PropertyId = _parameters.IndexOf(parameter); //Set order
                    property.SetDisplayName(parameter.DisplayName);
                    property.SetDescription(parameter.Description);
                    //property.SetCategory(parameter.Category.ToString());
                    property.DefaultValue = parameter.ConfigObject;
                    property.SetIsBrowsable(true);                    
                }
            }
        }


        public void Init(List<Parameter> parameters)
        {
            _parameters = parameters;
            _strings = parameters.Where(i => i.Type == ViewParameterType.String).OrderBy(i => i.DisplayName).ToList();
            _texts = parameters.Where(i => i.Type == ViewParameterType.Text).OrderBy(i => i.DisplayName).ToList();
            _enums = parameters.Where(i => i.Type == ViewParameterType.Enum).OrderBy(i => i.DisplayName).ToList();
            _numerics = parameters.Where(i => i.Type == ViewParameterType.Numeric).OrderBy(i => i.DisplayName).ToList();
            _bools = parameters.Where(i => i.Type == ViewParameterType.Boolean).OrderBy(i => i.DisplayName).ToList();

            Init();
            foreach (var property in Properties) property.SetIsBrowsable(false);

            initList(_strings, "s");
            initList(_texts, "t");
            initList(_enums, "e");
            initList(_numerics, "n");
            initList(_bools, "b");

            InitDefaultValues();

            TypeDescriptor.Refresh(this);
        }

        public Parameter GetParameter(string propertyName)
        {
            if (propertyName.Length > 1)
            {
                List<Parameter> list = null;
                int index = int.Parse(propertyName.Substring(1));
                if (propertyName[0] == 's') list = _strings;
                if (propertyName[0] == 't') list = _texts;
                if (propertyName[0] == 'e') list = _enums;
                if (propertyName[0] == 'b') list = _bools;
                if (propertyName[0] == 'n') list = _numerics;

                if (index < list.Count)
                {
                    return list[index];
                }
            }
            return null;

        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string t0
        {
            get { return _texts[0].Value; }
            set { _texts[0].Value = value; }
        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string t1
        {
            get { return _texts[1].Value; }
            set { _texts[1].Value = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string t2
        {
            get { return _texts[2].Value; }
            set { _texts[2].Value = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string t3
        {
            get { return _texts[3].Value; }
            set { _texts[3].Value = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string t4
        {
            get { return _texts[4].Value; }
            set { _texts[4].Value = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string t5
        {
            get { return _texts[5].Value; }
            set { _texts[5].Value = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string t6
        {
            get { return _texts[6].Value; }
            set { _texts[6].Value = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string t7
        {
            get { return _texts[7].Value; }
            set { _texts[7].Value = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string t8
        {
            get { return _texts[8].Value; }
            set { _texts[8].Value = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string t9
        {
            get { return _texts[9].Value; }
            set { _texts[9].Value = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string t10
        {
            get { return _texts[10].Value; }
            set { _texts[10].Value = value; }
        }
        public string t11
        {
            get { return _texts[11].Value; }
            set { _texts[11].Value = value; }
        }

        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e0
        {
            get { return _enums[0].Value; }
            set { _enums[0].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e1
        {
            get { return _enums[1].Value; }
            set { _enums[1].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e2
        {
            get { return _enums[2].Value; }
            set { _enums[2].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e3
        {
            get { return _enums[3].Value; }
            set { _enums[3].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e4
        {
            get { return _enums[4].Value; }
            set { _enums[4].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e5
        {
            get { return _enums[5].Value; }
            set { _enums[5].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e6
        {
            get { return _enums[6].Value; }
            set { _enums[6].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e7
        {
            get { return _enums[7].Value; }
            set { _enums[7].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e8
        {
            get { return _enums[8].Value; }
            set { _enums[8].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e9
        {
            get { return _enums[9].Value; }
            set { _enums[9].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e10
        {
            get { return _enums[10].Value; }
            set { _enums[10].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e11
        {
            get { return _enums[11].Value; }
            set { _enums[11].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e12
        {
            get { return _enums[12].Value; }
            set { _enums[12].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e13
        {
            get { return _enums[13].Value; }
            set { _enums[13].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e14
        {
            get { return _enums[14].Value; }
            set { _enums[14].Value = value; }
        }
        [TypeConverter(typeof(ViewParameterEnumConverter))]
        public string e15
        {
            get { return _enums[15].Value; }
            set { _enums[15].Value = value; }
        }


        public int n0
        {
            get { return _numerics[0].NumericValue; }
            set { _numerics[0].NumericValue = value; }
        }
        public int n1
        {
            get { return _numerics[1].NumericValue; }
            set { _numerics[1].NumericValue = value; }
        }
        public int n2
        {
            get { return _numerics[2].NumericValue; }
            set { _numerics[2].NumericValue = value; }
        }
        public int n3
        {
            get { return _numerics[3].NumericValue; }
            set { _numerics[3].NumericValue = value; }
        }
        public int n4
        {
            get { return _numerics[4].NumericValue; }
            set { _numerics[4].NumericValue = value; }
        }
        public int n5
        {
            get { return _numerics[5].NumericValue; }
            set { _numerics[5].NumericValue = value; }
        }
        public int n6
        {
            get { return _numerics[6].NumericValue; }
            set { _numerics[6].NumericValue = value; }
        }
        public int n7
        {
            get { return _numerics[7].NumericValue; }
            set { _numerics[7].NumericValue = value; }
        }
        public int n8
        {
            get { return _numerics[8].NumericValue; }
            set { _numerics[8].NumericValue = value; }
        }
        public int n9
        {
            get { return _numerics[9].NumericValue; }
            set { _numerics[9].NumericValue = value; }
        }
        public int n10
        {
            get { return _numerics[10].NumericValue; }
            set { _numerics[10].NumericValue = value; }
        }
        public int n11
        {
            get { return _numerics[11].NumericValue; }
            set { _numerics[11].NumericValue = value; }
        }
        public int n12
        {
            get { return _numerics[12].NumericValue; }
            set { _numerics[12].NumericValue = value; }
        }
        public int n13
        {
            get { return _numerics[13].NumericValue; }
            set { _numerics[13].NumericValue = value; }
        }
        public int n14
        {
            get { return _numerics[14].NumericValue; }
            set { _numerics[14].NumericValue = value; }
        }
        public int n15
        {
            get { return _numerics[15].NumericValue; }
            set { _numerics[15].NumericValue = value; }
        }

        public bool b0
        {
            get { return _bools[0].BoolValue; }
            set { _bools[0].BoolValue = value; }
        }
        public bool b1
        {
            get { return _bools[1].BoolValue; }
            set { _bools[1].BoolValue = value; }
        }
        public bool b2
        {
            get { return _bools[2].BoolValue; }
            set { _bools[2].BoolValue = value; }
        }
        public bool b3
        {
            get { return _bools[3].BoolValue; }
            set { _bools[3].BoolValue = value; }
        }
        public bool b4
        {
            get { return _bools[4].BoolValue; }
            set { _bools[4].BoolValue = value; }
        }
        public bool b5
        {
            get { return _bools[5].BoolValue; }
            set { _bools[5].BoolValue = value; }
        }
        public bool b6
        {
            get { return _bools[6].BoolValue; }
            set { _bools[6].BoolValue = value; }
        }
        public bool b7
        {
            get { return _bools[7].BoolValue; }
            set { _bools[7].BoolValue = value; }
        }
        public bool b8
        {
            get { return _bools[8].BoolValue; }
            set { _bools[8].BoolValue = value; }
        }
        public bool b9
        {
            get { return _bools[9].BoolValue; }
            set { _bools[9].BoolValue = value; }
        }
        public bool b10
        {
            get { return _bools[10].BoolValue; }
            set { _bools[10].BoolValue = value; }
        }
        public bool b11
        {
            get { return _bools[11].BoolValue; }
            set { _bools[11].BoolValue = value; }
        }
        public bool b12
        {
            get { return _bools[12].BoolValue; }
            set { _bools[12].BoolValue = value; }
        }
        public bool b13
        {
            get { return _bools[13].BoolValue; }
            set { _bools[13].BoolValue = value; }
        }
        public bool b14
        {
            get { return _bools[14].BoolValue; }
            set { _bools[14].BoolValue = value; }
        }
        public bool b15
        {
            get { return _bools[15].BoolValue; }
            set { _bools[15].BoolValue = value; }
        }

        public string s0
        {
            get { return _strings[0].Value; }
            set { _strings[0].Value = value; }
        }
        public string s1
        {
            get { return _strings[1].Value; }
            set { _strings[1].Value = value; }
        }
        public string s2
        {
            get { return _strings[2].Value; }
            set { _strings[2].Value = value; }
        }
        public string s3
        {
            get { return _strings[3].Value; }
            set { _strings[3].Value = value; }
        }
        public string s4
        {
            get { return _strings[4].Value; }
            set { _strings[4].Value = value; }
        }
        public string s5
        {
            get { return _strings[5].Value; }
            set { _strings[5].Value = value; }
        }
        public string s6
        {
            get { return _strings[6].Value; }
            set { _strings[6].Value = value; }
        }
        public string s7
        {
            get { return _strings[7].Value; }
            set { _strings[7].Value = value; }
        }
        public string s8
        {
            get { return _strings[8].Value; }
            set { _strings[8].Value = value; }
        }
        public string s9
        {
            get { return _strings[9].Value; }
            set { _strings[9].Value = value; }
        }
        public string s10
        {
            get { return _strings[10].Value; }
            set { _strings[10].Value = value; }
        }
        public string s11
        {
            get { return _strings[11].Value; }
            set { _strings[11].Value = value; }
        }
        public string s12
        {
            get { return _strings[12].Value; }
            set { _strings[12].Value = value; }
        }
        public string s13
        {
            get { return _strings[13].Value; }
            set { _strings[13].Value = value; }
        }
        public string s14
        {
            get { return _strings[14].Value; }
            set { _strings[14].Value = value; }
        }
        public string s15
        {
            get { return _strings[15].Value; }
            set { _strings[15].Value = value; }
        }
    }
}
