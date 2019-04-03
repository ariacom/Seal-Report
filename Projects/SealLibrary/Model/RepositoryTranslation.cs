//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System.Collections.Generic;
using System.IO;
using Seal.Helpers;
using System.Text.RegularExpressions;
using System.Linq;
using System;

namespace Seal.Model
{
    public class RepositoryTranslation
    {
        public string Context = "";
        public string Instance = "";
        public string Reference = "";
        public Dictionary<string, string> Translations = new Dictionary<string, string>();
        public int Usage;

        static public void InitFromCSV(List<RepositoryTranslation> translations, string path, bool hasInstance)
        {
            try
            {
                initFromCSV(translations, path, hasInstance);
            }
            catch
            {
                if (File.Exists(path))
                {
                    try
                    {
                        //probably locked with Excel, copy in a temp file to try
                        string newPath = FileHelper.GetTempUniqueFileName(path);
                        File.Copy(path, newPath, true);
                        initFromCSV(translations, newPath, hasInstance);
                    }
                    catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            }
        }

        static private void initFromCSV(List<RepositoryTranslation> translations, string filePath, bool hasInstance)
        {
            if (File.Exists(filePath))
            {
                bool isHeader = true;
                Regex regexp = null;
                List<string> languages = new List<string>();

                foreach (string line in File.ReadAllLines(filePath, System.Text.Encoding.UTF8))
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
                            var context = ExcelHelper.FromCsv(collection[0].Value);
                            var reference = ExcelHelper.FromCsv(collection[startCol - 1].Value);

                            RepositoryTranslation translation = null;
                            if (hasInstance)
                            {
                                var instance = ExcelHelper.FromCsv(collection[1].Value);
                                translation = translations.FirstOrDefault(i => i.Context == context && i.Reference == reference && i.Instance == instance);
                                if (translation == null)
                                {
                                    translation = new RepositoryTranslation() { Context = context, Reference = reference, Instance = instance };
                                    translations.Add(translation);
                                }
                            }
                            else
                            {
                                translation = translations.FirstOrDefault(i => i.Context == context && i.Reference == reference);
                                if (translation == null)
                                {
                                    translation = new RepositoryTranslation() { Context = context, Reference = reference };
                                    translations.Add(translation);
                                }
                            }

                            for (int i = 0; i < languages.Count && i + startCol < collection.Count; i++)
                            {
                                if (string.IsNullOrEmpty(languages[i]) || translation.Translations.ContainsKey(languages[i])) continue;
                                translation.Translations.Add(languages[i], ExcelHelper.FromCsv(collection[i + startCol].Value));
                            }
                        }
                    }
                }
            }
        }
    }
}
