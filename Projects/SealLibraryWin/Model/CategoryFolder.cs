//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.ComponentModel;
#if WINDOWS
using DynamicTypeDescriptor;
using System.Drawing.Design;
using Seal.Forms;
#endif

namespace Seal.Model
{
    /// <summary>
    /// Helper to change the category folder of elements
    /// </summary>
    public class CategoryFolder : RootComponent
    {
        public static CategoryFolder Instance = new CategoryFolder();

#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Path").SetIsBrowsable(!string.IsNullOrEmpty(Name));
                GetProperty("Information").SetIsBrowsable(!string.IsNullOrEmpty(Name) || !string.IsNullOrEmpty(Information));

                //Read only
                GetProperty("Information").SetIsReadOnly(true);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

#endif

        /// <summary>
        /// The full path of the of the category. This can be modified to change globally all the category names of the columns. The category path can be specified using the '/' character (e.g. '/Master/Name1/Name2')
        /// </summary>
#if WINDOWS
        [DisplayName("Path"), Description("The full path of the of the category. This can be modified to change globally all the category names of the columns. The category path can be specified using the '/' character (e.g. '/Master/Name1/Name2')"), Category("Helpers"), Id(1, 1)]
#endif
        public string Path { get; set; }

#if WINDOWS
        [DisplayName("Information"), Description("Last information"), Category("Helpers"), Id(2, 1)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
#endif
        public string Information { get; set; }

        public void SetInformation(string information)
        {
            Information = information;
            UpdateEditorAttributes();
        }
    }

}
