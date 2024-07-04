//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
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
    /// Template for Report Tasks.
    /// </summary>
    public class ReportTaskTemplate
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
        public static List<ReportTaskTemplate> LoadTemplates(string templatesFolder)
        {
            List<ReportTaskTemplate> result = new List<ReportTaskTemplate>();
            foreach (var path in Directory.GetFiles(templatesFolder, "*.cshtml"))
            {
                ReportTaskTemplate template = new ReportTaskTemplate() { Name = Path.GetFileNameWithoutExtension(path) };
                template.FilePath = path;
                template.LastModification = File.GetLastWriteTime(path);
                result.Add(template);
            }
            return result;
        }


        public void ParseConfiguration(ReportTask task)
        {
            //Parse the file to init the template
            try
            {
                task.Parameters.Clear();
                RazorHelper.CompileExecute(Configuration, task);
            }
            catch (TemplateCompilationException ex)
            {
                Helper.WriteLogException("ReportTaskTemplate.ParseConfiguration", ex);
                task.Error = Helper.GetExceptionMessage(ex);
            }
            catch (Exception ex)
            {
                Helper.WriteLogException("ReportTaskTemplate.ParseConfiguration", ex);
                task.Error = string.Format("Unexpected error got when parsing task template.\r\n{0}", ex.Message);
            }
        }

        ReportTask _task;

        void InitDefaultScripts()
        {
            if (_task == null)
            {
                _task = new ReportTask();
                ParseConfiguration(_task);
            }
        }

        public string Description
        {
            get
            {
                InitDefaultScripts();
                return _task.TemplateDescription ?? "";
            }
        }

        public ExecutionStep DefaultExecutionStep
        {
            get
            {
                InitDefaultScripts();
                return _task.Step;
            }
        }

        public string DefaultScript
        {
            get
            {
                InitDefaultScripts();
                return _task.Script ?? "";
            }
        }

        public string DefaultBodyScript
        {
            get
            {
                InitDefaultScripts();
                return _task.BodyScript ?? "";
            }
        }

        public List<Parameter> DefaultParameters
        {
            get
            {
                InitDefaultScripts();
                return _task.Parameters;
            }
        }

        public string Error
        {
            get
            {
                return _task.Error;
            }
        }

    }
}

