//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.ComponentModel;
using System.Drawing.Design;

namespace Seal.Forms
{
    class InformationUITypeEditor : UITypeEditor
    {
        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override void PaintValue(PaintValueEventArgs e) 
        {
            if (e.Value is string && !string.IsNullOrEmpty((string) e.Value))
            {
                e.Graphics.DrawImage(global::Seal.Properties.Resources.information, 3, 0, 16, 16);
            }
        }
    }

    class ErrorUITypeEditor : UITypeEditor
    {
        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override void PaintValue(PaintValueEventArgs e)
        {
            if (e.Value is string && !string.IsNullOrEmpty((string)e.Value))
            {
                e.Graphics.DrawImage(global::Seal.Properties.Resources.error, 3, 0, 16, 16);
            }
        }
    }

}
