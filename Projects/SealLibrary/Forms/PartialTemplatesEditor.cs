//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;

namespace Seal.Model
{
    public class PartialTemplatesEditor : RootEditor
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
        List<ReportViewPartialTemplate> _templates;
        List<ReportViewPartialTemplate> _strings;

        void initList(List<ReportViewPartialTemplate> list, string prefix)
        {
            int index = 0;
            foreach (var template in list.Where(i => i.UseCustom))
            {
                var propName = string.Format("{0}{1}", prefix, index++);
                var property = GetProperty(propName);
                if (property != null)
                {
                    property.PropertyId = _templates.IndexOf(template); //Set order
                    property.SetDisplayName(template.Name.Replace(Path.GetFileNameWithoutExtension(template.View.Template.FilePath)+".i", ""));
                    property.SetDescription(string.Format("Partial Template Text for '{0}'", template.Name));
                    property.DefaultValue = "";
                    property.SetIsBrowsable(true);                    
                }
            }
        }


        public void Init(List<ReportViewPartialTemplate> templates)
        {
            _templates = templates;
            _strings = templates;

            Init();
            foreach (var property in Properties) property.SetIsBrowsable(false);

            initList(_strings, "s");
            InitDefaultValues();

            TypeDescriptor.Refresh(this);
        }

        public ReportViewPartialTemplate GetPartialTemplate(string propertyName)
        {
            if (propertyName.Length > 1)
            {
                List<ReportViewPartialTemplate> list = null;
                int index = int.Parse(propertyName.Substring(1));
                if (propertyName[0] == 's') list = _strings;
                if (index < list.Count)
                {
                    return list[index];
                }
            }
            return null;

        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s0
        {
            get { return _strings[0].Text; }
            set { _strings[0].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s1
        {
            get { return _strings[1].Text; }
            set { _strings[1].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s2
        {
            get { return _strings[2].Text; }
            set { _strings[2].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s3
        {
            get { return _strings[3].Text; }
            set { _strings[3].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s4
        {
            get { return _strings[4].Text; }
            set { _strings[4].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s5
        {
            get { return _strings[5].Text; }
            set { _strings[5].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s6
        {
            get { return _strings[6].Text; }
            set { _strings[6].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s7
        {
            get { return _strings[7].Text; }
            set { _strings[7].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s8
        {
            get { return _strings[8].Text; }
            set { _strings[8].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s9
        {
            get { return _strings[9].Text; }
            set { _strings[9].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s10
        {
            get { return _strings[10].Text; }
            set { _strings[10].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s11
        {
            get { return _strings[11].Text; }
            set { _strings[11].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s12
        {
            get { return _strings[12].Text; }
            set { _strings[12].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s13
        {
            get { return _strings[13].Text; }
            set { _strings[13].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s14
        {
            get { return _strings[14].Text; }
            set { _strings[14].Text = value; }
        }
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string s15
        {
            get { return _strings[15].Text; }
            set { _strings[15].Text = value; }
        }
    }
}
