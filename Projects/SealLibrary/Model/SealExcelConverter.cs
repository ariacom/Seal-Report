using Seal.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Seal.Model
{
    public class SealExcelConverter : RootEditor
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

        public static SealExcelConverter Create(string assemblyDirectory)
        {
            SealExcelConverter result = null;
            //Check if an implementation is available in a .dll
            string applicationPath = string.IsNullOrEmpty(assemblyDirectory) ? Path.GetDirectoryName(Application.ExecutablePath) : assemblyDirectory;
            if (File.Exists(Path.Combine(applicationPath, "SealConverter.dll")))
            {
                try
                {
                    Assembly currentAssembly = AppDomain.CurrentDomain.Load("SealConverter");
                    Type t = currentAssembly.GetType("SealExcelConverter.ExcelConverter", true);
                    Object[] args = new Object[] { };
                    result = (SealExcelConverter)t.InvokeMember(null, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance, null, null, args);
                    result.ApplicationPath = applicationPath;
                }
                catch { }
            }

            if (result == null) result = new SealExcelConverter();
            
            return result;
        }

        public override string ToString() {
            //PlaceHolder1
            return "Not implemented in the open source version. A commercial component is available at www.ariacom.com"; 
        }

        public string ApplicationPath = Path.GetDirectoryName(Application.ExecutablePath);

        public virtual string ConvertToExcel(string destination)
        {
            //PlaceHolder2
            throw new Exception("Oh Punaise !\r\nThe Excel Converter is not implemented in the open source version...\r\nHowever a commercial component is available at www.ariacom.com\r\n");
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
