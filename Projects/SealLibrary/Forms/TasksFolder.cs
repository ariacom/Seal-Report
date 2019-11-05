//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.ComponentModel;
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
                GetProperty("CommonScripts").SetIsBrowsable(true);
                //GetProperty("CommonScripts").SetDisplayName("Common Scripts: " + (Report.CommonScripts.Count == 0 ? "None" : Report.CommonScripts.Count.ToString() + " Items(s)"));
                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

        public int GetSort() { return 1; }

        /// <summary>
        /// List of scripts added to all scripts executed for the report (including tasks). This may be useful to defined common functions for the report.
        /// </summary>
        [Category("Scripts"), DisplayName("Common Scripts"), Description("List of scripts added to all scripts executed for the report (including tasks). This may be useful to defined common functions for the report.")]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<CommonScript> CommonScripts
        {
            get { return Report.CommonScripts; }
            set { Report.CommonScripts = value; }
        }
    }
}
