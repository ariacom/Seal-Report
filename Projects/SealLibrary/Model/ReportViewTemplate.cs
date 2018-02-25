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
using System.Drawing;
using RazorEngine;
using RazorEngine.Templating;

namespace Seal.Model
{
    public class ReportViewTemplate
    {
        public const string ModelHTMLName = "Model HTML";
        public const string ModelDetailHTMLName = "Model Detail HTML";
        public const string ModelCSVExcelName = "Model CSV Excel";
        public const string DataTableName = "Data Table";
        public const string PageTableName = "Page Table";
        public const string ChartNVD3Name = "Chart NVD3";
        public const string ChartJSName = "Chart JS";
        public const string ChartPlotlyName = "Chart Plotly";
        public const string ModelContainerName = "Model Container";
        public static readonly string[] DefaultCSS = { "display:none;", "display:inline; float:left;", "position:absolute; top:50px; left:500px;", "height: 600px; width: 400px;", "text-align:center;", "display:inline-block;", "page-break-after: always;" };
        public static readonly string[] DefaultFontSize = { "30pt", "24pt", "16pt", "14pt", "12pt", "10pt", "8pt", "6pt" };
        public static readonly string[] DefaultFontFamily = { "Verdana, Arial, Helvetica, sans-serif", "Times,Times New Roman, serif", "Courier New, Courier, monospace", "Comic Sans MS, cursive" };

        string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; }
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


        bool _supportTheme = false;
        public bool UseThemeValues
        {
            get { return _supportTheme; }
            set { _supportTheme = value; }
        }


        string _externalViewerExtension = "";
        public string ExternalViewerExtension
        {
            get { return _externalViewerExtension; }
            set { _externalViewerExtension = value; }
        }

        bool _skipFileAttachments = false;
        public bool SkipFileAttachments
        {
            get { return _skipFileAttachments; }
            set { _skipFileAttachments = value; }
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

        public string Configuration;

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

        public void Init(string path)
        {
            FilePath = path;
            LastModification = File.GetLastWriteTime(path);
            ConfigurationPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".config.cshtml");
            if (File.Exists(ConfigurationPath))
            {
                LastConfigModification = File.GetLastWriteTime(ConfigurationPath);
                Configuration = File.ReadAllText(ConfigurationPath);
            }
            //load partial templates related
            PartialTemplatesPath.Clear();
            foreach (var partialPath in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".*.partial.cshtml"))
            {
                PartialTemplatesPath.Add(partialPath);
            }

            IsParsed = false;
        }

        public static List<ReportViewTemplate> LoadTemplates(string templateFolder)
        {
            List<ReportViewTemplate> viewTemplates = new List<ReportViewTemplate>();
            //Templates
            foreach (var path in Directory.GetFiles(templateFolder, "*.cshtml"))
            {
                if (path.EndsWith(".config.cshtml") || path.EndsWith(".partial.cshtml")) continue;
                ReportViewTemplate template = new ReportViewTemplate();
                template.Init(path);
                viewTemplates.Add(template);
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
