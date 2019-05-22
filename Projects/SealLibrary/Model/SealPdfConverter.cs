//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Seal.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Seal.Model
{
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

        public static SealPdfConverter Create(string assemblyDirectory)
        {
            SealPdfConverter result = null;
            //Check if an implementation is available in a .dll
            string applicationPath = string.IsNullOrEmpty(assemblyDirectory) ? Path.GetDirectoryName(Application.ExecutablePath) : assemblyDirectory;
            if (File.Exists(Path.Combine(applicationPath, "SealConverter.dll")))
            {
                try
                {
                    Assembly currentAssembly = AppDomain.CurrentDomain.Load("SealConverter");
                    Type t = currentAssembly.GetType("SealPdfConverter.PdfConverter", true);
                    Object[] args = new Object[] { };
                    result = (SealPdfConverter)t.InvokeMember(null, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance, null, null, args);
                    result.ApplicationPath = applicationPath;
                }
                catch (Exception ex) {
                    Debug.WriteLine(ex.Message);
                }
            }

            if (result == null) result = new SealPdfConverter();
            
            return result;
        }

        public override string ToString() 
        {
            //PlaceHolder1
            return "Not implemented in the open source version. A commercial component is available at www.ariacom.com"; 
        }

        public string ApplicationPath = Path.GetDirectoryName(Application.ExecutablePath);

        public virtual void ConvertHTMLToPDF(string source, string destination)
        {
            //PlaceHolder2
            throw new Exception("The PDF Converter is not implemented in the open source version...\r\nA commercial component is available at www.ariacom.com\r\n");
        }

        public virtual void SetConfigurations(List<string> configurations, ReportView view)
        {
        }

        public virtual List<string> GetConfigurations()
        {
            return new List<string>();
        }

        public virtual void ConfigureTemplateEditor(TemplateTextEditorForm frm, string propertyName, ref string template, ref string language) { }

        public IEntityHandler EntityHandler = null;

        public virtual string GetLicenseText()
        {
            return "";
        }
    }
}
