//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicTypeDescriptor;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Seal.Model
{
    public class RootComponent : RootEditor
    {
        protected string _GUID;
        virtual public string GUID
        {
            get { return _GUID; }
            set { _GUID = value; }
        }


        protected string _name;
        virtual public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }

}
