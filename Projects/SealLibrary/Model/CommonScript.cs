//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using DynamicTypeDescriptor;
using Seal.Forms;
using System.ComponentModel;
using System.Drawing.Design;

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

        /// <summary>
        /// The script name
        /// </summary>
        [Category("Definition"), DisplayName("Name"), Description("The script name."), Id(1, 1)]
        public string Name { get; set; } = "name";

        /// <summary>
        /// A Razor script that will be included in all executed scripts
        /// </summary>
        [Category("Definition"), DisplayName("Script"), Description("A Razor script that will be included in all executed scripts."), Id(2, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string Script { get; set; } = RazorTemplate;

    }


}
