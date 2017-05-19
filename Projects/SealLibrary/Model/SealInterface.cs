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
    public class SealInterface
    {
        public static SealInterface Create(Repository repository)
        {
            SealInterface result = null;
            //Check if an implementation is available in a .dll
            string applicationPath = string.IsNullOrEmpty(repository.ApplicationPath) ? Path.GetDirectoryName(Application.ExecutablePath) : repository.ApplicationPath;
            if (File.Exists(Path.Combine(applicationPath, "SealInterface.dll")))
            {
                try
                {
                    Assembly currentAssembly = AppDomain.CurrentDomain.Load("SealInterface");
                    Type t = currentAssembly.GetType("Seal.Model.SealLicenseInterface", true);
                    Object[] args = new Object[] { };
                    result = (SealInterface)t.InvokeMember(null, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance, null, null, args);
                    result._repository = repository;
                }
                catch { }
            }

            if (result == null) result = new SealInterface();
            
            return result;
        }

        public virtual void Init() {
        }

        public virtual string Text()
        {
            return "";
        }

        protected Repository _repository = null;
    }
}
