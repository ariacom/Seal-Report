//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using Seal.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
#if WINDOWS
using System.Windows.Forms;
#endif


namespace Seal.Model
{
    /// <summary>
    /// Virtual class for SealInterface implementation
    /// </summary>
    public class SealInterface
    {
        static bool _loaded = false;
        public static SealInterface Create(Repository repository = null)
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
                    catch (Exception ex)
                    {
                        Helper.WriteLogException("SealInterface.Create1", ex);
                        Debug.WriteLine(ex.Message);
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
                catch (Exception ex)
                {
                    Helper.WriteLogException("SealInterface.Create2", ex);
                }
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

        protected Repository _repository = null;
    }
}
