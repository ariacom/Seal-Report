//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Seal.Converter;
using DynamicTypeDescriptor;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Xml.Serialization;

namespace Seal.Model
{ 
   public class MetaEV
    {
        string _identifier;
        [Category("Definition"), DisplayName("Identifier"), Description("The database value of the enumerated value")]
        public string Id
        {
            get { return _identifier; }
            set { _identifier = value; }
        }

        string _value;
        [Category("Definition"), DisplayName("Value"), Description("The display value of the enumerated value")]
        public string Val
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
