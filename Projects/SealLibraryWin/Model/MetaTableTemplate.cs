//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
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
        /// <summary>
        /// Name of the default table template
        /// </summary>
        public const string DefaultName = "Default";
        /// <summary>
        /// Name of the Mongo DB table template
        /// </summary>
        public const string MongoDBName = "Mongo DB";

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


        /// <summary>
        /// Parse the template configuration to initialize the table
        /// </summary>
        public void ParseConfiguration(MetaTable table)
        {
            //Parse the file to init the template
            try
            {
                table.Parameters.Clear();
                RazorHelper.CompileExecute(Configuration, table, GetType().Name+"_"+Name, LastModification);
            }
            catch (TemplateCompilationException ex)
            {
                Helper.WriteLogException("MetaTableTemplate.ParseConfiguration", ex);
                table.Error = Helper.GetExceptionMessage(ex);
            }
            catch (Exception ex)
            {
                Helper.WriteLogException("MetaTableTemplate.ParseConfiguration", ex);
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

        /// <summary>
        /// Default definition init script defined by the template
        /// </summary>
        public string DefaultDefinitionInitScript
        {
            get
            {
                InitDefaultScripts();
                if (_table.DefinitionInitScript == null) _table.DefinitionInitScript = "";
                return _table.DefinitionInitScript;
            }
        }

        /// <summary>
        /// Default definition script defined by the template
        /// </summary>
        public string DefaultDefinitionScript
        {
            get
            {
                InitDefaultScripts();
                if (_table.DefinitionScript == null) _table.DefinitionScript = "";
                return _table.DefinitionScript;
            }
        }

        /// <summary>
        /// Default load script defined by the template
        /// </summary>
        public string DefaultLoadScript
        {
            get
            {
                InitDefaultScripts();
                if (_table.LoadScript == null) _table.LoadScript = "";
                return _table.LoadScript;
            }
        }

        /// <summary>
        /// Default parameters defined by the template
        /// </summary>
        public List<Parameter> DefaultParameters
        {
            get
            {
                InitDefaultScripts();
                return _table.Parameters;
            }
        }

        /// <summary>
        /// Last error message got when parsing the template
        /// </summary>
        public string Error
        {
            get
            {
                return _table.Error;
            }
        }

    }
}

