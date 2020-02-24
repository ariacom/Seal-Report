//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace Seal.Model
{
    /// <summary>
    /// Base class for the Excel Converter
    /// </summary>
    public class SealExcelConverter : RootEditor
    {

        /// <summary>
        /// Creates a basic SealExcelConverter
        /// </summary>
        public static SealExcelConverter Create(string assemblyDirectory)
        {
            SealExcelConverter result = null;
            //Check if an implementation is available in a .dll
            string applicationPath = string.IsNullOrEmpty(assemblyDirectory) ? Helper.GetApplicationDirectory() : assemblyDirectory;
            if (File.Exists(Path.Combine(applicationPath, "SealConverter.dll")))
            {
                try
                {
                    Assembly currentAssembly = Assembly.LoadFrom(Path.Combine(applicationPath, "SealConverter.dll"));
                    //Load related DLLs
                    Assembly.LoadFrom(Path.Combine(applicationPath, "DocumentFormat.OpenXml.dll"));
                    Assembly.LoadFrom(Path.Combine(applicationPath, "EPPlus.dll"));
                    Type t = currentAssembly.GetType("Seal.Converter.ExcelConverter", true);
                    Object[] args = new Object[] { };
                    result = (SealExcelConverter)t.InvokeMember(null, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance, null, null, args);
                    result.ApplicationPath = applicationPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            if (result == null) result = new SealExcelConverter();
            
            return result;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public override string ToString() {
            //PlaceHolder1
            return "Not implemented in the open source version. A commercial component is available at https://ariacom.com"; 
        }

        /// <summary>
        /// Current application path
        /// </summary>
        public string ApplicationPath = Helper.GetApplicationDirectory();

        /// <summary>
        /// Convert to Excel and save the result to a destination path
        /// </summary>
        public virtual string ConvertToExcel(string destination)
        {
            //PlaceHolder2
            throw new Exception("The Excel Converter is not implemented in the open source version...\r\nA commercial component is available at https://ariacom.com\r\n");
        }

        public virtual void SetConfigurations(List<string> configurations, ReportView view)
        {
        }

        public virtual List<string> GetConfigurations()
        {
            return new List<string>();
        }



        public virtual string GetLicenseText()
        {
            return "";
        }

        public virtual void InitFromReferenceView(ReportView referenceView)
        {
        }
        public virtual Report GetReport()
        {
            return null;
        }
    }
}

