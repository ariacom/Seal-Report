//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Data.OleDb;
using System.Data;
using System.ComponentModel;
using Seal.Converter;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.IO;
using Seal.Helpers;
using System.Text.RegularExpressions;
using Seal.Forms;
using DynamicTypeDescriptor;
using System.Windows.Forms.Design;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;

namespace Seal.Model
{
    public abstract class OutputDevice : RootComponent
    {
        public virtual string FullName { get; set; }

        public abstract string Process(Report report);
        public abstract void Validate();

        [XmlIgnore]
        public string FilePath;

        public abstract void SaveToFile();
        public abstract void SaveToFile(string path);
    }
}
