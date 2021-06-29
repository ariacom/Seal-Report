//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using Seal.Helpers;
using RazorEngine.Templating;
using System.IO;
using System.Diagnostics;

namespace Seal.Model
{
    /// <summary>
    /// Template for NoSQL Meta Tables.
    /// </summary>
    public class MetaTableTemplate
    {
        public const string DefaultName = "Default";

        /// <summary>
        /// Name 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Current file path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Last modification date time
        /// </summary>
        public DateTime LastModification;


        string _configuration;
        /// <summary>
        /// Current Configuration
        /// </summary>
        public string Configuration
        {
            get
            {
                if (_configuration == null || LastModification != File.GetLastWriteTime(FilePath))
                {
                    try
                    {
                        StreamReader sr = new StreamReader(FilePath);
                        _configuration = sr.ReadToEnd();
                        sr.Close();
                        LastModification = File.GetLastWriteTime(FilePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }

                return _configuration;
            }
        }

        /// <summary>
        /// List of available Table Templates
        /// </summary>
        public static List<MetaTableTemplate> LoadTemplates(string templatesFolder)
        {
            List<MetaTableTemplate> result = new List<MetaTableTemplate>();
            foreach (var path in Directory.GetFiles(templatesFolder, "*.cshtml"))
            {
                MetaTableTemplate template = new MetaTableTemplate() { Name = Path.GetFileNameWithoutExtension(path) };
                template.FilePath = path;
                template.LastModification = File.GetLastWriteTime(path);
                result.Add(template);
            }
            return result;
        }


        public void ParseConfiguration(MetaTable table)
        {
            //Parse the file to init the template
            try
            {
                table.Parameters.Clear();
                RazorHelper.CompileExecute(Configuration, table);
            }
            catch (TemplateCompilationException ex)
            {
                table.Error = Helper.GetExceptionMessage(ex);
            }
            catch (Exception ex)
            {
                table.Error = string.Format("Unexpected error got when parsing table template.\r\n{0}", ex.Message);
            }
        }

        MetaTable _table;

        void InitDefaultScripts()
        {
            if (_table == null)
            {
                _table = new MetaTable();
                ParseConfiguration(_table);
            }
        }

        public string DefaultDefinitionScript
        {
            get
            {
                InitDefaultScripts();
                if (_table.DefinitionScript == null) _table.DefinitionScript = "";
                return _table.DefinitionScript;
            }
        }

        public string DefaultLoadScript
        {
            get
            {
                InitDefaultScripts();
                if (_table.LoadScript == null) _table.LoadScript = "";
                return _table.LoadScript;
            }
        }

        public List<Parameter> DefaultParameters
        {
            get
            {
                InitDefaultScripts();
                return _table.Parameters;
            }
        }

    }
}


