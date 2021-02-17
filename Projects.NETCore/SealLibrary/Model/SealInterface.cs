//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Seal.Model
{
    /// <summary>
    /// Virtual class for SealInterface implementation
    /// </summary>
    public class SealInterface
    {
        static bool _loaded = false;
        public static SealInterface Create(Repository repository)
        {
            SealInterface result = null;
            //Check if an implementation is available in a .dll
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SealInterface.dll");
            if (File.Exists(path))
            {
                if (!_loaded)
                {
                    try
                    {
                        Assembly.LoadFrom(path);
                        _loaded = true;
                    }
                    catch (Exception Exception)
                    {
                        Debug.WriteLine(Exception.Message);
                    }
                }

                try
                {
                    Assembly currentAssembly = AppDomain.CurrentDomain.Load("SealInterface");
                    Type t = currentAssembly.GetType("Seal.Model.SealLicenseInterface", true);
                    object[] args = new object[] { };
                    result = (SealInterface)t.InvokeMember(null, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance, null, null, args);
                    result._repository = repository;
                }
                catch { }
            }


            if (result == null) result = new SealInterface();

            return result;
        }

        public virtual void Init()
        {
        }

        public virtual string Text()
        {
            return "";
        }

#if !NETCOREAPP
        public virtual bool ProcessAction(string action, WebBrowser webBrowser, NavigationContext navigation)
        {
            return false;
        }
#endif
        protected Repository _repository = null;
    }
}

