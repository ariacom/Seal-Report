//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
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
