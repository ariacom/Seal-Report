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
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;

namespace Seal.Model
{
    public class RepositoryServer
    {
        private static List<ReportViewTemplate> _viewTemplates = null;
        private static List<Theme> _themes = null;
        private static Object _viewLock = new object();
        private static Object _themeLock = new object();

        //View templates
        public static List<ReportViewTemplate> ViewTemplates
        {
            get
            {
                //used from the Report Designer, load and parse all...
                if (_viewTemplates == null)
                {
                    _viewTemplates = ReportViewTemplate.LoadTemplates(Repository.Instance.ViewsFolder);
                }
                foreach (var template in _viewTemplates.Where(i => !i.IsParsed)) template.ParseConfiguration();
                return _viewTemplates;
            }
        }

        public static ReportViewTemplate GetViewTemplate(string name)
        {
            lock (_viewLock)
            {
                if (_viewTemplates == null)
                {
                    _viewTemplates = ReportViewTemplate.LoadTemplates(Repository.Instance.ViewsFolder);
                }
            }

            var result = _viewTemplates.FirstOrDefault(i => i.Name == name);
            if (result == null)
            {
                lock (_viewLock)
                {
                    //Get the name for configuration text to avoid useless parsing and save time
                    foreach (var template in _viewTemplates.Where(i => !i.IsParsed))
                    {
                        if (template.Configuration.Contains(string.Format("\"{0}\";", name)))
                        {
                            template.ParseConfiguration();
                            break;
                        }
                    }
                }
            }
            result = _viewTemplates.FirstOrDefault(i => i.Name == name);
            if (result == null)
            {

                lock (_viewLock)
                {
                    //Name not found in configuration -> we parse all...
                    foreach (var template in _viewTemplates.Where(i => !i.IsParsed)) template.ParseConfiguration();
                }
                result = _viewTemplates.FirstOrDefault(i => i.Name == name);
            }

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

        //Themes
        public static List<Theme> Themes
        {
            get
            {
                //used from the Report Designer, load and parse all...
                if (_themes == null)
                {
                    _themes = Theme.LoadThemes(Repository.Instance.ThemesFolder);
                }
                foreach (var theme in _themes.Where(i => !i.IsParsed)) theme.Parse();
                return _themes;
            }
        }


        public static Theme GetTheme(string name)
        {
            lock (_themeLock)
            {
                if (_themes == null)
                {
                    _themes = Theme.LoadThemes(Repository.Instance.ThemesFolder);
                }
            }

            Theme result;
            if (string.IsNullOrEmpty(name)) result = _themes.FirstOrDefault(i => i.IsDefault);
            else
            {
                result = _themes.FirstOrDefault(i => i.Name == name);
                if (result == null)
                {
                    lock (_themeLock)
                    {
                        //Get the name for text to avoid useless parsing and save time
                        foreach (var theme in _themes.Where(i => !i.IsParsed))
                        {
                            if (theme.Text.Contains(string.Format("\"{0}\";", name)))
                            {
                                theme.Parse();
                                break;
                            }
                        }
                    }
                }
                result = _themes.FirstOrDefault(i => i.Name == name);
                if (result == null)
                {

                    lock (_themeLock)
                    {
                        //Name not found in configuration -> we parse all...
                        foreach (var theme in _themes.Where(i => !i.IsParsed)) theme.Parse();
                    }
                    result = _themes.FirstOrDefault(i => i.Name == name);
                }
            }
            if (result == null) throw new Exception(string.Format("Unable to find theme named '{0}'", name));


            //Check if the file has changed
            if (result.LastModification != File.GetLastWriteTime(result.FilePath))
            {
                lock (_themeLock)
                {
                    result.Init(result.FilePath);
                }
            }

            //Check if themes has been parsed
            if (!result.IsParsed)
            {
                lock (_themeLock)
                {
                    result.Parse();
                }
            }


            return result;
        }


    }
}
