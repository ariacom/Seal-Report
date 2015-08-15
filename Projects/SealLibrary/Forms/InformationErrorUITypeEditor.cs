//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Text;

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
