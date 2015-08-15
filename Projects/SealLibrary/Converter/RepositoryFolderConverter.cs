//
// Copyright (c) Seal Report, Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Seal.Model;
using System.Globalization;
using System.IO;

namespace Seal.Converter
{
    public class RepositoryFolderConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            bool limit = true;
            if (context.Instance is ReportOutput) limit = false;
            return limit; //true will limit to list. false will show the list, but allow free-form entry
        }

        void addFolders(string path, string prefix, List<string> choices)
        {
            foreach (var folder in Directory.GetDirectories(path))
            {
                choices.Add(prefix + Repository.Instance.ConvertToRepositoryPath(folder));
                addFolders(folder, prefix, choices);
            }
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> choices = new List<string>();
            string prefix = (context.Instance is ReportOutput ? Repository.SealRepositoryKeyword + "\\Reports" : "");
            choices.Add(prefix + "\\");
            addFolders(Repository.Instance.ReportsFolder, prefix, choices);

            return new StandardValuesCollection(choices);
        }   
    }

}
