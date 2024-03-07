//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
#if WINDOWS
using DynamicTypeDescriptor;
using Seal.Forms;
using System.Drawing.Design;
#endif
using System.ComponentModel;

namespace Seal.Model
{
    /// <summary>
    /// Common script that are added at the end of all scripts executed
    /// </summary>
    public class CommonScript : RootEditor
    {

        /// <summary>
        /// Sample
        /// </summary>
        public const string RazorTemplate = @"//Before execution, this code will be added at the end of all scripts executed...
@functions {
    public void SetNegativeValuesInRed(Report report) {
        report.LogMessage(""SetNegativeValuesInRed"");
        foreach (var model in report.Models) 
        {
            foreach (var page in model.Pages) 
            {
                foreach (var line in page.DataTable.Lines) 
                {
                    foreach (var cell in line) 
                    {
                        if (cell.Element != null && cell.Element.IsNumeric && cell.DoubleValue < 0) {
                            cell.FinalCssStyle = ""font-weight:bold;color:red;"";
                        }
                    }
                }       
            }
        }
    }

    public string MyConvertString(string input) {
        return input.Replace(""__"",""_"");
    }
}
";

#if WINDOWS
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

#endif
        /// <summary>
        /// The script name
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Name"), Description("The script name."), Id(1, 1)]
#endif
        public string Name { get; set; } = "name";

        /// <summary>
        /// A Razor script that will be included in all executed scripts
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Script"), Description("A Razor script that will be included in all executed scripts."), Id(2, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string Script { get; set; } = RazorTemplate;
    }


}
