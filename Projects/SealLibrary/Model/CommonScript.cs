//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using DynamicTypeDescriptor;
using Seal.Forms;
using System.ComponentModel;
using System.Drawing.Design;

namespace Seal.Model
{
    public class CommonScript : RootEditor
    {
        public const string RazorTemplate = @"@using System.Text
@functions {
    //Before execution, this script will be added at the end of all scripts executed...
    public string MyConvertString(string input) {
        return input.Replace(""__"",""_"");
    }
}
";

        #region Editor
        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Name").SetIsBrowsable(true);
                GetProperty("Script").SetIsBrowsable(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        string _name = "name";
        [Category("Definition"), DisplayName("Name"), Description("The script name."), Id(1, 1)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        string _script = RazorTemplate;
        [Category("Definition"), DisplayName("Script"), Description("A Razor script that will be included in all executed script."), Id(2, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string Script
        {
            get { return _script; }
            set { _script = value; }
        }

    }
}
