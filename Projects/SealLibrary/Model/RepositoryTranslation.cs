//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Seal.Helpers;
using RazorEngine;
using System.Xml.Serialization;
using System.Globalization;
using System.Data;
using System.Text.RegularExpressions;

namespace Seal.Model
{
    public class RepositoryTranslation
    {
        public string Context = "";
        public string Instance = "";
        public string Reference = "";
        public Dictionary<string, string> Translations = new Dictionary<string, string>();
        public int Usage;

        static string convert(string value)
        {
            string result = value;
            if (value.StartsWith("\"") && value.EndsWith("\"")) result = result.Substring(1, value.Length - 2);
            result = result.Replace("\"\"", "\"");
            return result;
        }

        static public List<RepositoryTranslation> InitFromCSV(string filePath, bool hasInstance)
        {
            List<RepositoryTranslation> translations = new List<RepositoryTranslation>();
            if (File.Exists(filePath))
            {
                bool isHeader = true;
                Regex regexp = null;
                List<string> languages = new List<string>();

                foreach (string line in File.ReadAllLines(filePath, System.Text.Encoding.Default))
                {
                    if (regexp == null)
                    {
                        string exp = "(?<=^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)";
                        char separator = ',';
                        //use the first line to determine the separator
                        if (line.StartsWith("Context")) separator = line[7];
                        if (separator != ',') exp = exp.Replace(',', separator);
                        regexp = new Regex(exp);
                    }

                    MatchCollection collection = regexp.Matches(line);
                    int startCol = (hasInstance ? 3 : 2);
                    if (collection.Count > startCol)
                    {
                        if (isHeader)
                        {
                            for (int i = startCol; i < collection.Count; i++)
                            {
                                languages.Add(convert(collection[i].Value));
                            }
                            isHeader = false;
                        }
                        else
                        {
                            RepositoryTranslation translation = new RepositoryTranslation() { Context = convert(collection[0].Value), Reference = convert(collection[startCol-1].Value) };
                            if (hasInstance) translation.Instance = convert(collection[1].Value);
                            translations.Add(translation);
                            for (int i = 0; i < languages.Count && i + startCol < collection.Count; i++)
                            {
                                translation.Translations.Add(languages[i], convert(collection[i + startCol].Value));
                            }
                        }
                    }
                }
            }

            return translations;
        }
    }
}
