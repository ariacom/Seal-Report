//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Helpers;
using Seal.Model;

namespace Test
{
    /// <summary>
    /// Compile-only guards for the e-mail device script templates that are compiled at runtime
    /// (they are never compiled by the build, so an SDK breaking change would otherwise only
    /// surface in production when an e-mail is actually sent).
    /// </summary>
    [TestClass]
    public class TestEmailTemplateCompilation
    {
        /// <summary>
        /// Compiles the shipped Microsoft Graph e-mail template against the resolved Microsoft.Graph
        /// SDK. Guards against breaking changes when the Graph NuGet package is bumped (e.g. v5 -> v6):
        /// a renamed/moved type or namespace in the template surfaces here as a compilation exception.
        /// Compile-only: the template is not executed, so no Azure credentials or network are needed.
        /// </summary>
        [TestMethod]
        public void CompileMSGraphEmailTemplate()
        {
            //Resolves the in-repo repository (Debug) and forces the load of the Razor assemblies.
            Repository.Create();

            //GetFullScript mirrors what CompileExecute adds in production (the Seal default usings).
            var script = RazorHelper.GetFullScript(OutputEmailDevice.MSGraphScriptTemplate);

            //Throws TemplateCompilationException if the template no longer compiles against the SDK.
            RazorHelper.Compile(script, typeof(OutputEmailDevice.EmailDefinition), "TST_MSGraphEmailTemplate");
        }
    }
}
