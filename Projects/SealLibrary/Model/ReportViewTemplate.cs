//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.IO;
using Seal.Helpers;
using RazorEngine.Templating;

namespace Seal.Model
{
    public class ReportViewTemplate
    {
        public const string ReportName = "Report";
        public const string ModelName = "Model";
        public const string ModelDetailName = "Model Detail";
        public const string DataTableName = "Data Table";
        public const string PageTableName = "Page Table";
        public const string ChartNVD3Name = "Chart NVD3";
        public const string ChartJSName = "Chart JS";
        public const string ChartPlotlyName = "Chart Plotly";
        public const string ModelContainerName = "Model Container";

        string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        string _description = "";
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }

        string _configurationPath;
        public string ConfigurationPath
        {
            get { return _configurationPath; }
            set { _configurationPath = value; }
        }

        List<Parameter> _parameters = new List<Parameter>();
        public List<Parameter> Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        List<string> _parentNames = new List<string>();
        public List<string> ParentNames
        {
            get { return _parentNames; }
            set { _parentNames = value; }
        }

        bool _forReportModel = false;
        public bool ForReportModel
        {
            get { return _forReportModel; }
            set { _forReportModel = value; }
        }

        public string Text
        {
            get
            {
                string result = "";
                try
                {
                    StreamReader sr = new StreamReader(FilePath);
                    result = sr.ReadToEnd();
                    sr.Close();
                }
                catch (Exception ex)
                {
                    _error = ex.Message;
                }
                return result;
            }
        }

        public string Configuration = "";

        string _error = "";
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        List<string> _partialTemplates = new List<string>();
        public List<string> PartialTemplatesPath
        {
            get { return _partialTemplates; }
            set { _partialTemplates = value; }
        }

        public string GetPartialTemplatePath(string name)
        {
            return Path.Combine(Path.GetDirectoryName(FilePath), name + ".partial.cshtml");
        }

        public string GetPartialTemplateText(string name)
        {
            return File.ReadAllText(GetPartialTemplatePath(name));
        }

        public bool Init(string path)
        {
            FilePath = path;
            LastModification = File.GetLastWriteTime(path);
            ConfigurationPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".config.cshtml");
            if (!File.Exists(ConfigurationPath)) return false;

            LastConfigModification = File.GetLastWriteTime(ConfigurationPath);
            Configuration = File.ReadAllText(ConfigurationPath);
            //load partial templates related
            PartialTemplatesPath.Clear();
            foreach (var partialPath in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".*.partial.cshtml"))
            {
                PartialTemplatesPath.Add(partialPath);
            }

            IsParsed = false;

            return true;
        }

        public static List<ReportViewTemplate> LoadTemplates(string templateFolder)
        {
            List<ReportViewTemplate> viewTemplates = new List<ReportViewTemplate>();
            //Templates
            foreach (var path in Directory.GetFiles(templateFolder, "*.cshtml"))
            {
                if (path.EndsWith(".config.cshtml") || path.EndsWith(".partial.cshtml")) continue;
                ReportViewTemplate template = new ReportViewTemplate();
                if (template.Init(path)) viewTemplates.Add(template);
            }
            return viewTemplates;
        }

        public void ClearConfiguration()
        {
            _parameters.Clear();
            _parentNames.Clear();
            _forReportModel = false;
        }

        public bool IsParsed = false; //Flag for optimization, by default the template is not parsed...until it is used
        public DateTime LastModification;
        public DateTime LastConfigModification;

        public bool IsModified
        {
            get
            {
                return LastModification != File.GetLastWriteTime(FilePath) || LastConfigModification != File.GetLastWriteTime(ConfigurationPath);
            }
        }

        public void ParseConfiguration()
        {
            //Parse the configuration file to init the view template
            try
            {
                string key = key = string.Format("TPLCFG:{0}_{1}", ConfigurationPath, File.GetLastWriteTime(ConfigurationPath).ToString("s"));
                _error = "";
                ClearConfiguration();
                RazorHelper.CompileExecute(Configuration, this);
                IsParsed = true;
            }
            catch (TemplateCompilationException ex)
            {
                _error = Helper.GetExceptionMessage(ex);
            }
            catch (Exception ex)
            {
                _error = string.Format("Unexpected error got when parsing template configuration.\r\n{0}", ex.Message);
            }
        }
    }
}
