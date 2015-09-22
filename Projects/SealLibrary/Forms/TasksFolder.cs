//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Seal.Converter;
using Seal.Model;
using System.Drawing.Design;

namespace Seal.Forms
{
    public class TasksFolder : ReportComponent, ITreeSort 
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("CommunScript").SetIsBrowsable(true);
                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

        public int GetSort() { return 1; }

        [Category("Helpers"), DisplayName("Commun Razor Script"), Description("If set, the script is added to all task script executed. This may be useful to defined commun functions.")]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string CommunScript
        {
            get { return Report.CommunTaskScript; }
            set { Report.CommunTaskScript = value; }
        }
    }
}
