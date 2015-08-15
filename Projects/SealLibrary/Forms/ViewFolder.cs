//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
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
                TypeDescriptor.Refresh(this);
            }
        }

        #endregion

        public int GetSort() { return 2; }

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
    }
}
