//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.ComponentModel;
using Seal.Converter;
using Seal.Model;
using System.Drawing.Design;
using System.Collections.Generic;

namespace Seal.Forms
{
    public class ViewFolder : ReportComponent, ITreeSort 
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("ViewGUID").SetIsBrowsable(true);
                GetProperty("DisplayName").SetIsBrowsable(true);
                GetProperty("InitScript").SetIsBrowsable(true);
                GetProperty("InputValues").SetIsBrowsable(true);
                GetProperty("WidgetCache").SetIsBrowsable(true);
                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

        public int GetSort() { return 2; }

        [DefaultValue(null)]
        [Category("Definition"), DisplayName("Current view"), Description("The current view used to execute the report.")]
        [TypeConverter(typeof(ReportViewConverter))]
        public string ViewGUID
        {
            get { return Report.ViewGUID; }
            set { Report.ViewGUID = value; }
        }

        [Category("Definition"), DisplayName("Display name"), Description("The report name displayed in the result. If empty, the report file name is used. The display name may contain a Razor script  if it starts with '@'.")]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string DisplayName
        {
            get { return Report.DisplayName; }
            set { Report.DisplayName = value; }
        }

        [Category("Definition"), DisplayName("Report Execution Init script"), Description("A Razor script executed when the report is initialized for the execution. The script can be used to modify the report definition (e.g. set default values in restrictions).")]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string InitScript
        {
            get { return Report.InitScript; }
            set { Report.InitScript = value; }
        }

        [Category("Definition"), DisplayName("Report Input Values"), Description("Definition of additional report input values (actually a restriction used as value only that may be prompted). Input values can then be used in the task scripts or any scripts used to generate the report.")]
        [Editor(typeof(EntityCollectionEditor), typeof(UITypeEditor))]
        public List<ReportRestriction> InputValues
        {
            get { return Report.InputValues; }
            set { Report.InputValues = value; }
        }

        [Category("Definition"), DisplayName("Widgets cache duration"), Description("For dashboards, the duration in seconds the report execution is kept by the Web Report Server to render the widgets defined in the report.")]
        [DefaultValue(60)]
        public int WidgetCache
        {
            get { return Report.WidgetCache; }
            set { Report.WidgetCache = value; }
        }

    }
}
