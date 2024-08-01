//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Seal.Helpers;

namespace Seal.Model
{
    /// <summary>
    /// The RepositoryServer is used to maintain a static list of ReportViewTemplate for performances purpose
    /// </summary>
    public class RepositoryServer
    {
        private static List<ReportViewTemplate> _viewTemplates = null;
        private static object _viewLock = new object();
        private static List<MetaTableTemplate> _tableTemplates = null;
        private static List<ReportTaskTemplate> _taskTemplates = null;



        public static string ViewsFolder = "";
        public static string TableTemplatesFolder = "";
        public static string TaskTemplatesFolder = "";


        /// <summary>
        /// Current list of ReportViewTemplate
        /// </summary>
        public static List<ReportViewTemplate> ViewTemplates
        {
            get
            {
                //used from the Report Designer, load and parse all...
                if (_viewTemplates == null)
                {
                    _viewTemplates = ReportViewTemplate.LoadTemplates(ViewsFolder);
                }
                foreach (var template in _viewTemplates.Where(i => !i.IsParsed)) template.ParseConfiguration();
                return _viewTemplates;
            }
        }

        /// <summary>
        /// Returns a ReportViewTemplate from a given name
        /// </summary>
        public static ReportViewTemplate GetViewTemplate(string name)
        {
            lock (_viewLock)
            {
                if (_viewTemplates == null)
                {
                    _viewTemplates = ReportViewTemplate.LoadTemplates(ViewsFolder);
                }
            }

            var result = _viewTemplates.FirstOrDefault(i => i.Name == name && i.RendererType == "");
            if (result == null) throw new Exception(string.Format("Unable to find view template named '{0}'", name));
            //Check if the file has changed
            if (result.IsModified)
            {
                lock (_viewLock)
                {
                    result.Init(result.FilePath);
                }
            }

            //Check if configuration has been parsed
            if (!result.IsParsed)
            {
                lock (_viewLock)
                {
                    result.ParseConfiguration();
                }
            }
            return result;
        }


        /// <summary>
        /// Returns a ReportViewTemplate from a given name and a given renderer
        /// </summary>
        public static ReportViewTemplate GetRendererTemplate(string name, string rendererType)
        {
            lock (_viewLock)
            {
                if (_viewTemplates == null)
                {
                    _viewTemplates = ReportViewTemplate.LoadTemplates(ViewsFolder);
                }
            }

            var result = _viewTemplates.FirstOrDefault(i => i.Name == name && i.RendererType == rendererType);
            if (result == null)
            {
                //Get it from default
                result = _viewTemplates.FirstOrDefault(i => i.Name == ReportViewTemplate.DefaultName && i.RendererType == rendererType);
                if (result == null) result = _viewTemplates.FirstOrDefault(i => i.Name == ReportViewTemplate.DefaultName && i.RendererType == "");
            }

            if (result == null) throw new Exception(string.Format("Unable to find view template named '{0}' for renderer '{1}'", name, rendererType));

            //Check if the file has changed
            if (result.IsModified)
            {
                lock (_viewLock)
                {
                    result.Init(result.FilePath);
                }
            }

            //Check if configuration has been parsed
            if (!result.IsParsed)
            {
                lock (_viewLock)
                {
                    result.ParseConfiguration();
                }
            }
            return result;
        }

        /// <summary>
        /// Current list of MetaTableTemplate
        /// </summary>
        public static List<MetaTableTemplate> TableTemplates
        {
            get
            {
                if (_tableTemplates == null)
                {
                    _tableTemplates = MetaTableTemplate.LoadTemplates(TableTemplatesFolder);
                }
                return _tableTemplates;
            }
        }

        /// <summary>
        /// Current list of ReportTaskTemplate
        /// </summary>
        public static List<ReportTaskTemplate> TaskTemplates
        {
            get
            {
                if (_taskTemplates == null)
                {
                    _taskTemplates = ReportTaskTemplate.LoadTemplates(TaskTemplatesFolder);
                }
                return _taskTemplates;
            }
        }

    }
}
