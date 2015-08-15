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
using System.Drawing;
using RazorEngine;
using RazorEngine.Templating;

namespace Seal.Model
{
    public class ReportViewTemplate
    {
        public const string ModelHTMLName = "Model HTML";
        public const string ModelCSVExcelName = "Model CSV Excel";
        public static readonly string[] DefaultCSS = { "display:none;", "display:inline; float:left;", "position:absolute; top:50px; left:500px;", "height: 600px; width: 400px;", "text-align:center;", "display:inline-block;", "page-break-after: always;" };
        public static readonly string[] DefaultFontSize = { "30pt", "24pt", "16pt", "14pt", "12pt", "10pt", "8pt", "6pt" };
        public static readonly string[] DefaultFontFamily = { "Verdana, Arial, Helvetica, sans-serif", "Times,Times New Roman, serif", "Courier New, Courier, monospace", "Comic Sans MS, cursive" };

        string _name;
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

        List<Parameter> _css = new List<Parameter>();
        public List<Parameter> CSS
        {
            get { return _css; }
            set { _css = value; }
        }

        List<string> _parentNames = new List<string>();
        public List<string> ParentNames
        {
            get { return _parentNames; }
            set { _parentNames = value; }
        }

        bool _forModel = false;
        public bool ForModel
        {
            get { return _forModel; }
            set { _forModel = value; }
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

        string _chartConfigurationXML;
        public string ChartConfigurationXML
        {
            get { return _chartConfigurationXML; }
            set { _chartConfigurationXML = value; }
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

        public string Configuration
        {
            get
            {
                string result = "";
                try
                {
                    StreamReader sr = new StreamReader(ConfigurationPath);
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

        string _error = "";
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }


        public static List<ReportViewTemplate> LoadTemplates(string templateFolder)
        {
            List<ReportViewTemplate> viewTemplates = new List<ReportViewTemplate>();
            //Templates
            foreach (var templatePath in Directory.GetFiles(templateFolder, "*.cshtml"))
            {
                if (templatePath.EndsWith(".config.cshtml")) continue;
                ReportViewTemplate template = new ReportViewTemplate() { Name = Path.GetFileNameWithoutExtension(templatePath) };
                template.FilePath = templatePath;
                template.ConfigurationPath = Path.Combine(templateFolder, Path.GetFileNameWithoutExtension(templatePath) + ".config.cshtml");
                viewTemplates.Add(template);

                if (!File.Exists(template.ConfigurationPath))
                {
                    template.Error = string.Format("Unable to find configuration file '{0}' for the template '{1}'", template.ConfigurationPath, template.Name);
                }
                else
                {
                    template.ParseConfiguration(null);
                }
            }
            return viewTemplates;
        }

        public void ClearConfiguration()
        {
            _parameters.Clear();
            _parentNames.Clear();
            _forModel = false;
        }

        public string _lastConfiguration = ""; //Cache to avoid re-compilation -> public for Cloning used in Repository.CreateFast()
        public void ParseConfiguration(string configurationEx)
        {
            //Parse the configuration file to init the view template
            try
            {
                string configuration = Configuration;
                if (!string.IsNullOrEmpty(configurationEx)) configuration = configurationEx;

                if (configuration.Replace("\r\n", "\n") != _lastConfiguration.Replace("\r\n", "\n"))
                {
                    ClearConfiguration();
                    Razor.Parse(configuration, this);
                    _lastConfiguration = configuration;
                }
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
