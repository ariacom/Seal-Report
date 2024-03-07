//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using Seal.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using RazorEngine.Templating;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Linq;
using Seal.Helpers;
#if WINDOWS
using System.Drawing.Design;
using Seal.Forms;
using DynamicTypeDescriptor;
#endif


namespace Seal.Renderer
{
    public class RootRenderer : RootEditor
    {
#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);

                //Then enable
                GetProperty("UseCustomTemplate").SetIsBrowsable(true);
                GetProperty("CustomTemplate").SetIsBrowsable(true);
                GetProperty("CustomTemplate").SetDisplayName("Custom template for " + GetRenderDisplayType());

                GetProperty("TemplateConfiguration").SetIsBrowsable(Parameters.Count > 0);
                GetProperty("TemplateConfiguration").SetDisplayName(GetRenderDisplayType() + " Parameters");

                TypeDescriptor.Refresh(this);
            }
        }

        public override void InitEditor()
        {
            base.InitEditor();
        }

        #endregion
#endif

        public override string ToString()
        {
            return "";
        }

        /// <summary>
        /// Default serialization
        /// </summary>
        public string Serialize()
        {
            XmlSerializer serializer = new XmlSerializer(this.GetType());
            StringWriter sw = new StringWriter();
            serializer.Serialize(sw, this);
            sw.Close();
            return sw.ToString();
        }

        /// <summary>
        /// Report view of the renderer
        /// </summary>
        [XmlIgnore]
        public ReportView View = null;

        /// <summary>
        /// Type name of the renderer
        /// </summary>
        public virtual string GetRenderType()
        {
            return "";
        }

        /// <summary>
        /// Type display name of the renderer
        /// </summary>
        public virtual string GetRenderDisplayType()
        {
            return GetRenderType();
        }

        /// <summary>
        /// File name extension of the generated result
        /// </summary>
        public virtual string GetFileExtension()
        {
            return "";
        }

        /// <summary>
        /// Report of the renderer
        /// </summary>
        /// <returns></returns>
        public Report Report
        {
            get { 
                return View?.Report; 
            }            
        }

        bool _useCustomTemplate = false;
        /// <summary>
        /// If true, the template text can be modified
        /// </summary>
#if WINDOWS
        [DisplayName("Use custom template text"), Description("If true, the template text can be modified."), Category("Custom template texts"), Id(2, 3)]
        [DefaultValue(false)]
#endif
        public bool UseCustomTemplate
        {
            get { return _useCustomTemplate; }
            set
            {
                _useCustomTemplate = value;
                UpdateEditorAttributes();
            }
        }

        [XmlIgnore]
        public DateTime LastTemplateModification = DateTime.Now;

        string _customTemplate;
        /// <summary>
        /// The custom template text used instead of the template defined by the template name
        /// </summary>
#if WINDOWS
        [DisplayName("Custom template"), Description("The custom template text used instead of the template defined by the template name."), Category("Custom template texts"), Id(3, 3)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string CustomTemplate
        {
            get { return _customTemplate; }
            set
            {
                LastTemplateModification = DateTime.Now;
                _customTemplate = value;
            }
        }
        public bool ShouldSerializeCustomTemplate() { return !string.IsNullOrEmpty(CustomTemplate); }

        /// <summary>
        /// The renderer parameters
        /// </summary>
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        public bool ShouldSerializeParameters() { return Parameters.Count > 0; }

#if WINDOWS
        /// <summary>
        /// The configuration values for edition.
        /// </summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("Renderer Parameters"), Description("The renderer parameter values."), Category("Parameters"), Id(3, 4)]
        [XmlIgnore]
        public ParametersEditor TemplateConfiguration
        {
            get
            {
                var editor = new ParametersEditor();
                editor.Init(Parameters);
                return editor;
            }
        }
#endif



        //Temporary variables to help for report serialization...
        private List<Parameter> _tempParameters;

        /// <summary>
        /// Operations performed before the serialization
        /// </summary>
        public void BeforeSerialization()
        {
            _tempParameters = Parameters.ToList();
            //Remove parameters identical to config
            Parameters.RemoveAll(i => i.Value == null || i.Value == i.ConfigValue);
        }
        public void AfterSerialization()
        {
            Parameters = _tempParameters;
        }

        bool _initDone = false;
        /// <summary>
        /// Init all parameters with an option to reset their values
        /// </summary>
        public void InitParameters(bool resetValues)
        {
            if (View == null || Template == null) return;
            if (!resetValues && _initDone) return;

            Template.InitParameters(Parameters, resetValues);
            View.Error = Template.Error;
            View.Information = "";
            if (resetValues) View.Information += "Values have been reset";
            if (!string.IsNullOrEmpty(View.Information)) View.Information = Helper.FormatMessage(View.Information);
            _initDone = true;
        }


        ReportViewTemplate _template = null;
        /// <summary>
        /// Current ReportViewTemplate
        /// </summary>
        [XmlIgnore]
        public ReportViewTemplate Template
        {
            get
            {
                if (_template == null)
                {
                    _template = RepositoryServer.GetRendererTemplate(View.TemplateName, GetRenderType());
                    if (_template == null)
                    {
                        _template = new ReportViewTemplate() { Name = View.TemplateName };
                        View.Error = string.Format("Unable to find template named '{0}'. Check your repository Views folder.", View.TemplateName);
                    }
                    else
                    {
                        View.Error = _template.Error;
                    }
                }
                return _template;
            }
        }


        /// <summary>
        /// Current template text of the view
        /// </summary>
        [XmlIgnore]
        public string ViewTemplateText
        {
            get
            {
                if (UseCustomTemplate)
                {
                    if (string.IsNullOrWhiteSpace(CustomTemplate)) return Template.Text;
                    return CustomTemplate;
                }
                return Template.Text;
            }
        }

        /// <summary>
        /// Returns the parameter value
        /// </summary>
        public string GetValue(string name)
        {
            Parameter parameter = Parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? "" : (string.IsNullOrEmpty(parameter.Value) ? parameter.ConfigValue : parameter.Value);
        }

        /// <summary>
        /// Returns a parameter boolean value
        /// </summary>
        public bool GetBoolValue(string name)
        {
            Parameter parameter = Parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? false : parameter.BoolValue;
        }

        /// <summary>
        /// Returns a parameter boolean value with a default if it does not exist
        /// </summary>
        public bool GetBoolValue(string name, bool defaultValue)
        {
            Parameter parameter = Parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? defaultValue : parameter.BoolValue;
        }

        /// <summary>
        /// Returns a parameter integer value
        /// </summary>
        public int GetNumericValue(string name)
        {
            Parameter parameter = Parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? 0 : parameter.NumericValue;
        }

        /// <summary>
        /// Returns a parameter double value
        /// </summary>
        public double GetDoubleValue(string name)
        {
            Parameter parameter = Parameters.FirstOrDefault(i => i.Name == name);
            return parameter == null ? 0 : parameter.DoubleValue;
        }
    }
}
