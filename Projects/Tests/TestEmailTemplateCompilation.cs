//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Helpers;
using Seal.Model;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;

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

        /// <summary>
        /// Compiles the shipped SendGrid e-mail template (compile-only, no API key nor network needed).
        /// </summary>
        [TestMethod]
        public void CompileSendGridEmailTemplate()
        {
            Repository.Create();
            var script = RazorHelper.GetFullScript(OutputEmailDevice.SendGridScriptTemplate);
            RazorHelper.Compile(script, typeof(OutputEmailDevice.EmailDefinition), "TST_SendGridEmailTemplate");
        }

        /// <summary>
        /// SMTP device with the delivery method 'SpecifiedPickupDirectory': the default SMTP template writes
        /// the e-mail as a .eml file in the Pickup directory (created if missing, %SEALREPOSITORY% replaced),
        /// and the Reply-To falls back to the explicit address, then to the device Reply addresses, then to the sender.
        /// </summary>
        [TestMethod]
        public void SmtpPickupDirectoryAndReplyTo()
        {
            var repository = Repository.Create();
            var folderName = "TST_Pickup_" + Guid.NewGuid().ToString("N");
            var folder = Path.Combine(repository.RepositoryPath, folderName);
            try
            {
                var device = OutputEmailDevice.Create();
                device.Type = EmailServerType.SMTP;
                device.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                device.PickupDirectory = Path.Combine(Repository.SealRepositoryKeyword, folderName);
                device.SenderEmail = "sender@example.com";
                Assert.AreEqual(folder, device.PickupDirectoryPath);
                device.Validate(); //no server needed in pickup mode

                //No device Reply addresses: Reply-To = sender
                device.SendEmail("", "to@example.com", "Test 1", false, "Body 1");
                StringAssert.Contains(ReadSingleEml(folder), "Reply-To: sender@example.com");

                //Device Reply addresses used by the notifications (no explicit Reply-To)
                device.ReplyTo = "support@example.com";
                device.SendEmail("", "to@example.com", "Test 2", false, "Body 2");
                StringAssert.Contains(ReadSingleEml(folder), "Reply-To: support@example.com");

                //Explicit Reply-To wins
                device.SendEmail("", "to@example.com", "other@example.com", "", "", "Test 3", false, "Body 3", "", "");
                StringAssert.Contains(ReadSingleEml(folder), "Reply-To: other@example.com");

                //Pickup mode without folder is refused
                device.PickupDirectory = "";
                Assert.ThrowsExactly<Exception>(() => device.Validate());
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        static string ReadSingleEml(string folder)
        {
            var files = Directory.GetFiles(folder, "*.eml");
            Assert.AreEqual(1, files.Length);
            var result = File.ReadAllText(files[0]);
            File.Delete(files[0]);
            return result;
        }
    }
}
