//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Seal.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text.RegularExpressions;
using ZstdSharp.Unsafe;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DnsClient.Protocol;

namespace Seal.Model
{
    /// <summary>
    /// Function definition with its name and source code
    /// </summary>
    public class FunctionDefinition
    {
        public string Name { get; set; }
        public string SourceCode { get; set; }
    }

    /// <summary>
    /// Generic editor for c# functions
    /// </summary>
    public class FunctionsEditor : RootEditor
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

        public override string ToString()
        {
            return "";
        }
        public List<FunctionDefinition> Functions;
        public Object SourceObject;

        public void Init(string script, object obj)
        {
            string statement;
            Functions = ExtractStatementAndFunctions(script, out statement);
            SourceObject = obj;
            Init();
            foreach (var property in Properties) property.SetIsBrowsable(false);

            int index = 0;
            foreach (var function in Functions)
            {
                var propName = $"f{index++}";
                var property = GetProperty(propName);
                if (property != null)
                {
                    property.PropertyId = index; //Set order
                    property.SetDisplayName(function.Name);
                    property.SetDescription("");
                    property.DefaultValue = function.SourceCode;
                    property.SetIsBrowsable(true);
                }
            }
            TypeDescriptor.Refresh(this);
        }


        public static List<FunctionDefinition> ExtractStatementAndFunctions(string code, out string statement)
        {
            var functions = new List<FunctionDefinition>();

            // Parse the code into a syntax tree
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code.Replace("@functions", "class functions"));
            // Get the root of the syntax tree
            var root = (CompilationUnitSyntax)tree.GetRoot();
            var desc = root.DescendantNodes();
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var functionDefinition = new FunctionDefinition
                {
                    Name = method.Identifier.Text,
                };
                //Remove empty lines at start
                var source = new System.Text.StringBuilder();
                bool trimDone = false;
                foreach (var line in method.ToFullString().Replace("\r\n","\n").Split("\n"))
                {
                    if (trimDone || line.Trim() != "")
                    {
                        source.AppendLine(line);
                        trimDone = true;
                    }
                }
                functionDefinition.SourceCode = source.ToString().TrimEnd();
                functions.Add(functionDefinition);
            }

            statement = "";
            foreach (var method in root.DescendantNodes().OfType<GlobalStatementSyntax>())
            {
                statement += method.ToFullString();
                Debug.WriteLine($"{method.GetType()}: {method}");
            }

            return functions;
        }

        public string ReplaceFunction(string source, string functionName, string value)
        {
            string statement;
            var functions = ExtractStatementAndFunctions(source, out statement);

            var function = functions.FirstOrDefault(i => i.Name == functionName);
            if (function != null) function.SourceCode = value;

            //Rebuild the script
            statement += "\r\n@functions {\r\n\r\n";
            foreach (var f in functions.Where(i => !string.IsNullOrWhiteSpace(i.SourceCode)))
            {
                statement += f.SourceCode + "\r\n\r\n";
            }
            statement += "}\r\n";

            return statement;
        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string f0
        {
            get { return Functions.Count > 0 ? Functions[0].SourceCode : ""; }
            set
            {
                Functions[0].SourceCode = value;
            }
        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string f1
        {
            get { return Functions.Count > 1 ? Functions[1].SourceCode : ""; }
            set { Functions[1].SourceCode = value; }
        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string f2
        {
            get { return Functions.Count> 2 ? Functions[2].SourceCode : ""; }
            set { Functions[2].SourceCode = value; }
        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string f3
        {
            get { return Functions.Count > 3 ? Functions[3].SourceCode : ""; }
            set { Functions[3].SourceCode = value; }
        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string f4
        {
            get { return Functions.Count > 4 ? Functions[4].SourceCode : ""; }
            set { Functions[4].SourceCode = value; }
        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string f5
        {
            get { return Functions.Count > 5 ? Functions[5].SourceCode : ""; }
            set { Functions[5].SourceCode = value; }
        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string f6
        {
            get { return Functions.Count > 6 ? Functions[6].SourceCode : ""; }
            set { Functions[6].SourceCode = value; }
        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string f7
        {
            get { return Functions.Count > 7 ? Functions[7].SourceCode : ""; }
            set { Functions[7].SourceCode = value; }
        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string f8
        {
            get { return Functions.Count > 8 ? Functions[8].SourceCode : ""; }
            set { Functions[8].SourceCode = value; }
        }

        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string f9
        {
            get { return Functions.Count > 9 ? Functions[9].SourceCode : ""; }
            set { Functions[9].SourceCode = value; }
        }
    }
}
