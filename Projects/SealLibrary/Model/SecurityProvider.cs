//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using RazorEngine.Templating;
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.IO;

namespace Seal.Model
{
    public class SecurityProvider
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

        string _script = "";
        public string Script
        {
            get { return _script; }
            set { _script = value; }
        }

        //Not used anymore...
        bool _promptUserPassword = false;
        public bool PromptUserPassword
        {
            get { return _promptUserPassword; }
            set { _promptUserPassword = value; }
        }

        List<SecurityParameter> _parameters = new List<SecurityParameter>();
        public List<SecurityParameter> Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        public string Configuration
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

        string _error = "";
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        public static List<SecurityProvider> LoadProviders(string providerFolder)
        {
            List<SecurityProvider> providers = new List<SecurityProvider>();
            foreach (var path in Directory.GetFiles(providerFolder, "*.cshtml"))
            {
                SecurityProvider provider = new SecurityProvider() { Name = Path.GetFileNameWithoutExtension(path) };
                provider.FilePath = path;
                providers.Add(provider);
                provider.ParseConfiguration();
            }
            return providers;
        }

        public void ClearConfiguration()
        {
            _parameters.Clear();
        }


        public string _lastConfiguration = ""; //Cache to avoid re-compilation -> public for Cloning used in Repository.CreateFast()
        public void ParseConfiguration()
        {
            //Parse the file to init the provider
            try
            {
                string configuration = Configuration;
                if (configuration.Replace("\r\n", "\n") != _lastConfiguration.Replace("\r\n", "\n"))
                {
                    ClearConfiguration();
                    RazorHelper.CompileExecute(configuration, this);
                    _lastConfiguration = configuration;
                }
            }
            catch (TemplateCompilationException ex)
            {
                _error = Helper.GetExceptionMessage(ex);
            }
            catch (Exception ex)
            {
                _error = string.Format("Unexpected error got when parsing security provider.\r\n{0}", ex.Message);
            }
        }


    }
}
