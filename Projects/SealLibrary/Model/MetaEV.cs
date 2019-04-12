//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.ComponentModel;
using System.Xml.Serialization;

namespace Seal.Model
{
    public class MetaEV
    {
        string _identifier;
        [Category("Definition"), DisplayName("\tIdentifier"), Description("The database value of the enumerated value")]
        public string Id
        {
            get { return _identifier; }
            set { _identifier = value; }
        }

        string _value;
        [Category("Definition"), DisplayName("\tValue"), Description("The optional display value")]
        public string Val
        {
            get { return _value; }
            set { _value = value; }
        }

        string _restrictionValue;
        [Category("Definition"), DisplayName("Restriction Value"), Description("The optional display value for the restriction list")]
        public string ValR
        {
            get { return _restrictionValue; }
            set { _restrictionValue = value; }
        }

        string _css;
        [Category("Definition"), DisplayName("CSS Style"), Description("The optional CSS Style applied to the cell displayed")]
        public string Css
        {
            get { return _css; }
            set { _css = value; }
        }

        string _class;
        [Category("Definition"), DisplayName("CSS Class"), Description("The optional CSS Class applied to the cell displayed")]
        public string Class
        {
            get { return _class; }
            set { _class = value; }
        }

        [Category("Display"), DisplayName("\tValue"), Description("The final value displayed in the report")]
        public string DisplayValue
        {
            get { return !string.IsNullOrEmpty(_value) ? _value : _identifier; }
        }

        [Category("Display"), DisplayName("Restriction"), Description("The final value displayed for the restriction")]
        public string DisplayRestriction
        {
            get { return !string.IsNullOrEmpty(_restrictionValue) ? _restrictionValue : DisplayValue; }
        }

        //For execution
        [XmlIgnore]
        public string HtmlId;
    }
}
