//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.IO;
using Seal.Helpers;
using RazorEngine.Templating;

namespace Seal.Model
{
    /// <summary>
    /// A ReportViewTemplate defines how a view is parsed and rendered.
    /// </summary>
    public class ReportViewTemplate
    {
        public const string ReportName = "Report";
        public const string ModelName = "Model";
        public const string ModelDetailName = "Model Detail";
        public const string RestrictionsName = "Restrictions";
        public const string WidgetName = "Widget";
        public const string DataTableName = "Data Table";
        public const string DataTableEditorName = "Data Table Editor";
        public const string PageTableName = "Page Table";
        public const string ChartNVD3Name = "Chart NVD3";
        public const string ChartJSName = "Chart JS";
        public const string ChartPlotlyName = "Chart Plotly";
        public const string ContainerName = "Container";
        public const string ContainerGridFlexName = "Container Grid Flex";

        public const string D3Colors = "['#1f77b4','#ff7f0e','#2ca02c','#d62728','#9467bd','#8c564b','#e377c2','#7f7f7f','#bcbd22','#17becf']";
        public const string GoogleColors = "['#3366cc','#dc3912','#ff9900','#109618','#990099','#0099c6','#dd4477','#66aa00','#b82e2e','#316395','#994499','#22aa99','#aaaa11','#6633cc','#e67300','#8b0707','#651067','#329262','#5574a6','#3b3eac']";

        /// <summary>
        /// Name of the view template
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Current file path of the template
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Path of the configuration file for the template
        /// </summary>
        public string ConfigurationPath { get; set; }

        /// <summary>
        /// Parameters defined for the template 
        /// </summary>
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();

        /// <summary>
        /// Allowed parent template names
        /// </summary>
        public List<string> ParentNames { get; set; } = new List<string>();

        /// <summary>
        /// True if the template is for a report model view
        /// </summary>
        public bool ForReportModel { get; set; } = false;

        /// <summary>
        /// True if the template must have a Model View parent 
        /// </summary>
        public bool IsModelViewChild { get; set; } = false;

        /// <summary>
        /// True if the template is for a restrictions view
        /// </summary>
        public bool IsRestrictionsView
        {
            get
            {
                return Name == RestrictionsName;
            }
        }

        /// <summary>
        /// True if the template is for a widget view
        /// </summary>
        public bool IsWidgetView
        {
            get
            {
                return Name == WidgetName;
            }
        }

        /// <summary>
        /// Additional partial templates to add to the template: Name of the partial template 
        /// </summary>
        public string[] SharedPartialTemplates = null;

        /// <summary>
        /// Text of the template
        /// </summary>
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
                    Error = ex.Message;
                }
                return result;
            }
        }

        /// <summary>
        /// Current template configuration text
        /// </summary>
        public string Configuration = "";

        /// <summary>
        /// Current errors
        /// </summary>
        public string Error { get; set; } = "";

        /// <summary>
        /// List of partial templates path
        /// </summary>
        public List<string> PartialTemplatesPath { get; set; } = new List<string>();

        /// <summary>
        /// Returns a partial template path from a given name
        /// </summary>
        public string GetPartialTemplatePath(string name)
        {
            return Path.Combine(Path.GetDirectoryName(FilePath), name + ".partial.cshtml");
        }

        /// <summary>
        /// Returns a partial template text from a given name
        /// </summary>
        public string GetPartialTemplateText(string name)
        {
            return File.ReadAllText(GetPartialTemplatePath(name));
        }

        /// <summary>
        /// Initialize the template from a file
        /// </summary>
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

        /// <summary>
        /// Returns a list of ReportViewTemplate from a given folder
        /// </summary>
        public static List<ReportViewTemplate> LoadTemplates(string templateFolder)
        {
            List<ReportViewTemplate> viewTemplates = new List<ReportViewTemplate>();
            //Templates
            foreach (var path in Directory.GetFiles(templateFolder, "*.cshtml"))
            {
                if (path.EndsWith(".config.cshtml") || path.EndsWith(".partial.cshtml")) continue;
                if (path.EndsWith("ModelContainer.cshtml")) continue; //backward compatibility before 6.1
                ReportViewTemplate template = new ReportViewTemplate();
                if (template.Init(path)) viewTemplates.Add(template);
            }
            if (viewTemplates.Count == 0) throw new Exception(string.Format("Unable to find View templates in the repository at {0}.\r\nCheck your installation", templateFolder));
            return viewTemplates;
        }

        /// <summary>
        /// Clear the template configuration
        /// </summary>
        public void ClearConfiguration()
        {
            Parameters.Clear();
            ParentNames.Clear();
            ForReportModel = false;
        }

        /// <summary>
        /// Flag for optimization, by default the template is not parsed... until it is used
        /// </summary>
        public bool IsParsed = false;

        /// <summary>
        /// Last modification date time
        /// </summary>
        public DateTime LastModification;

        /// <summary>
        /// Last modfication of the configuration file
        /// </summary>
        public DateTime LastConfigModification;

        /// <summary>
        /// True if the template or its configuration is modified
        /// </summary>
        public bool IsModified
        {
            get
            {
                return LastModification != File.GetLastWriteTime(FilePath) || LastConfigModification != File.GetLastWriteTime(ConfigurationPath);
            }
        }

        /// <summary>
        /// Compilation key for the template
        /// </summary>
        public string CompilationKey
        {
            get
            {
                return string.Format("TPL:{0}_{1}", FilePath, File.GetLastWriteTime(FilePath).ToString("s"));
            }
        }

        /// <summary>
        /// Parse the current configuration and initialize the parameters
        /// </summary>
        public void ParseConfiguration()
        {
            //Parse the configuration file to init the view template
            try
            {
                Error = "";
                ClearConfiguration();
                RazorHelper.CompileExecute(Configuration, this);
                IsParsed = true;
            }
            catch (TemplateCompilationException ex)
            {
                Helper.WriteLogException($"ParseConfiguration for {Name}", ex);
                Error = Helper.GetExceptionMessage(ex);
            }
            catch (Exception ex)
            {
                Helper.WriteLogException($"ParseConfiguration for {Name}", ex);
                Error = string.Format("Unexpected error got when parsing template configuration.\r\n{0}", ex.Message);
            }
        }

        public static string[] GridFlexContainerParameterSample = new string[] {
            "display:flex;\r\nflex-flow:row wrap;|Flex row in a single line with wrapping.",
            "display:flex;\r\nflex-flow:column;width:50%;|Flex column in a single column of 50%",
            "display:flex;\r\nflex-flow:row nowrap;\r\njustify-content:flex-start;\r\nalign-items:stretch;\r\nalign-content:stretch;|Flex: Full options",
            "display:grid;\r\ngrid-template-columns:1fr 1fr 1fr;|Grid with 3 columns with the same size",
            "display:grid;\r\ngrid-template-columns:auto auto;|Grid with 2 columns with automatic size",
            "display:grid;\r\ngrid-template-columns:40% 30% 30%;|Grid with 3 columns with specific sizes",
            "display:grid;\r\ngrid-template-areas:'myArea myArea .';|Grid: Areas definition for a 3 columns grid (requires the configuartion of 'grid-area' in the items)",
            "display:grid;\r\ngrid-template-columns:1fr\r\njustify-content:center;\r\nalign-content:center;|Grid: Full options",
        };

        public static string[] GridFlexItemParameterSample = new string[] {
            "width:50%;\r\nwidth:50%;|Flex: If container is flex-flow:row, the view1 and view2 have the same size on the first row",
            "flex:2;\r\nflex:1;\r\nflex:1;|Flex: If container is flex-flow:row, the view1, view2 and view3 have different sizes on the first row",
            "order:0; flex-grow:0; flex-shrink:1; flex-basis:fit-content align-self:center;|Flex: Full options",
            "grid-column: 1 / 5;\r\ngrid-column: 1 / span 3;|Grid: Item1 starts on column 1 and ends before column 5. Item2 starts on column 1 and spans 3 columns",
            "grid-row: 1 / 4;\r\ngrid-row: 1 / span 2;|Grid: Item1 starts on row-line 1 and ends on row-line 4. Item2 starts on row 1 and spans 2 rows",
            "grid-area: 1 / 2 / 5 / 6;|Grid: Starts on row-line 1 and column-line 2, and ends on row-line 5 and column line 6",
            "grid-area: 2 / 1 / span 2 / span 3;|Grid: Starts on row-line 2 and column-line 1, and spans 2 rows and 3 columns",
            "grid-area: myArea;|Grid: Item1 gets the name 'myArea' from the grid-template-areas of the container",
            "grid-column:1/5; grid-row:1/4; justify-self:center; align-self:start;|Grid: Full options",
        };

    }
}

