//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Forms;
using Seal.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Seal.Model
{
    /// <summary>
    /// Base class for the Pdf Converter
    /// </summary>
    public class SealPdfConverter : RootEditor
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                TypeDescriptor.Refresh(this);
            }
        }

        public override void InitEditor()
        {
            base.InitEditor();
        }

        #endregion

        /// <summary>
        /// Optional source format
        /// </summary>
        public string SourceFormat = "";

        /// <summary>
        /// Dashboards to convert if the converter is for Dashboards export
        /// </summary>
        public List<Dashboard> Dashboards;

        public static SealPdfConverter Create()
        {
            SealPdfConverter result = null;
            //Check if an implementation is available in a .dll
            if (File.Exists(Repository.Instance.SealConverterPath))
            {
                try
                {
                    string assembliesFolder = Repository.Instance.AssembliesFolder;
                    //Load related DLLs
#if NETCOREAPP
                    Assembly.LoadFrom(Path.Combine(assembliesFolder, "WnvHtmlToPdf_NetCore.dll"));
                    Assembly.LoadFrom(Path.Combine(assembliesFolder, "WnvHtmlToPdfClient_NetCore.dll"));
#else
                    Assembly.LoadFrom(Path.Combine(assembliesFolder, "wnvhtmltopdf.dll"));
                    Assembly.LoadFrom(Path.Combine(assembliesFolder, "WnvHtmlToPdfClient.dll"));
#endif
                    Assembly currentAssembly = Assembly.LoadFrom(Repository.Instance.SealConverterPath);
                    Type t = currentAssembly.GetType("Seal.Converter.PdfConverter", true);
                    object[] args = new object[] { };
                    result = (SealPdfConverter)t.InvokeMember(null, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance, null, null, args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            if (result == null) result = new SealPdfConverter();

            return result;
        }

        public override string ToString()
        {
            //PlaceHolder1
            return "Not implemented in the open source version. A commercial component is available at https://ariacom.com";
        }

        public virtual void ConvertHTMLToPDF(string source, string destination)
        {
            //PlaceHolder2
            throw new Exception("The PDF Converter is not implemented in the open source version...\r\nA commercial component is available at https://ariacom.com\r\n");
        }

        public virtual void SetConfigurations(List<string> configurations, ReportView view)
        {
        }

        public virtual List<string> GetConfigurations()
        {
            return new List<string>();
        }

        public virtual void ConfigureTemplateEditor(TemplateTextEditorForm frm, string propertyName, ref string template, ref string language) { } //!NETCore

        public IEntityHandler EntityHandler = null; //!NETCore

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
