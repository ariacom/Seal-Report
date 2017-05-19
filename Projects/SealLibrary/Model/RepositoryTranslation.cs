//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
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

        static public List<RepositoryTranslation> InitFromCSV(string filePath, bool hasInstance)
        {
            try
            {
                return initFromCSV(filePath, hasInstance);
            }
            catch
            {
                if (File.Exists(filePath))
                {
                    try
                    {
                        //copy in a temp file to try
                        string newPath = FileHelper.GetTempUniqueFileName(filePath);
                        File.Copy(filePath, newPath, true);
                        return initFromCSV(newPath, hasInstance);
                    }
                    catch { }
                }
            }
            return new List<RepositoryTranslation>();
        }
        static private List<RepositoryTranslation> initFromCSV(string filePath, bool hasInstance)
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
                                languages.Add(ExcelHelper.FromCsv(collection[i].Value));
                            }
                            isHeader = false;
                        }
                        else
                        {
                            RepositoryTranslation translation = new RepositoryTranslation() { Context = ExcelHelper.FromCsv(collection[0].Value), Reference = ExcelHelper.FromCsv(collection[startCol - 1].Value) };
                            if (hasInstance) translation.Instance = ExcelHelper.FromCsv(collection[1].Value);
                            translations.Add(translation);
                            for (int i = 0; i < languages.Count && i + startCol < collection.Count; i++)
                            {
                                if (string.IsNullOrEmpty(languages[i])) continue;
                                translation.Translations.Add(languages[i], ExcelHelper.FromCsv(collection[i + startCol].Value));
                            }
                        }
                    }
                }
            }
            return translations;
        }
}
}
