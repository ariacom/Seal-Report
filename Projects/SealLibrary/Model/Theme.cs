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
    public class Theme
    {
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

        bool _isDefault = false;
        public bool IsDefault
        {
            get { return _isDefault; }
            set { _isDefault = value; }
        }

        List<Parameter> _values = new List<Parameter>();
        public List<Parameter> Values
        {
            get { return _values; }
            set { _values = value; }
        }

        public bool IsParsed = false; //Flag for optimization, by default the theme is not parsed...until it is used
        public DateTime LastModification;
        public string Text = "";

        public static List<Theme> LoadThemes(string folder)
        {
            List<Theme> result = new List<Theme>();
            //Templates
            foreach (var path in Directory.GetFiles(folder, "*.cshtml"))
            {
                Theme theme = new Theme();
                theme.Init(path);
                result.Add(theme);
            }
            return result;
        }

        public void Init(string path)
        {
            Name = "";
            FilePath = path;
            Text = File.ReadAllText(path);
            IsDefault = Text.Contains("Theme.IsDefault = true;");
            LastModification = File.GetLastWriteTime(path);
            IsParsed = false;
        }

        string _error = "";
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }


        public void Clear()
        {
            _values.Clear();
        }

        public void Parse()
        {
            try
            {
                Clear();
                RazorHelper.CompileExecute(Text, this);
                IsParsed = true;
            }
            catch (TemplateCompilationException ex)
            {
                _error = Helper.GetExceptionMessage(ex);
            }
            catch (Exception ex)
            {
                _error = string.Format("Unexpected error got when parsing theme configuration.\r\n{0}", ex.Message);
                if (ex.InnerException != null) _error += "\r\n" + ex.InnerException.Message;
            }
        }

    }
}
