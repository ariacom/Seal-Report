//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Model;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace Test
{
    /// <summary>
    /// Maximum size of the emails of an Email device (OutputEmailDevice.MaxMessageSize): the result is compressed in a ZIP file,
    /// an HTML body becomes a ZIP attachment, and a result still too large is removed from the email with a message
    /// </summary>
    [TestClass]
    public class TestEmailMaxSize
    {
        static string _folder;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _folder = Path.Combine(Path.GetTempPath(), "SealTestEmailMaxSize_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            try { Directory.Delete(_folder, true); } catch { }
        }

        static OutputEmailDevice device(int maxMessageSize)
        {
            return new OutputEmailDevice()
            {
                GUID = Guid.NewGuid().ToString(),
                Name = "Test max size",
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectory = Path.Combine(_folder, "pickup"),
                SenderEmail = "sender@test.com",
                MaxMessageSize = maxMessageSize
            };
        }

        static string file(string name, byte[] content)
        {
            var path = Path.Combine(_folder, name);
            File.WriteAllBytes(path, content);
            return path;
        }

        //About 3 MB of text: compressed far below 1 MB
        static byte[] compressible() { return Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("Product;Category;Amount\r\nChai;Beverages;18.00\r\n", 60000))); }

        //2 MB of random bytes: a ZIP file cannot make it smaller
        static byte[] incompressible() { var bytes = new byte[2 * 1024 * 1024]; new Random(1).NextBytes(bytes); return bytes; }

        [TestMethod]
        public void NoLimitOrSmallEmailUnchanged()
        {
            var path = file("small.csv", compressible());
            bool html = false; string body = "body", attachPath = path, attachName = "small.csv";
            Assert.IsNull(device(0).LimitMessageSize(null, "Subject", ref html, ref body, ref attachPath, ref attachName), "0 = no limit");
            Assert.AreEqual(path, attachPath);
            Assert.IsNull(device(10).LimitMessageSize(null, "Subject", ref html, ref body, ref attachPath, ref attachName), "below the limit");
            Assert.AreEqual(path, attachPath);
            Assert.AreEqual("body", body);
        }

        [TestMethod]
        public void LargeAttachmentCompressed()
        {
            var path = file("large.csv", compressible());
            bool html = false; string body = "Please find the report", attachPath = path, attachName = "Products.csv";
            var message = device(1).LimitMessageSize(null, "Products", ref html, ref body, ref attachPath, ref attachName);
            StringAssert.Contains(message, "ZIP");
            StringAssert.EndsWith(attachPath, ".zip");
            Assert.AreEqual("Products.zip", attachName);
            Assert.AreEqual("Please find the report", body, "the body is kept");
            Assert.IsTrue(new FileInfo(attachPath).Length < 1024 * 1024);
        }

        [TestMethod]
        public void TooLargeAttachmentRemoved()
        {
            var path = file("random.bin", incompressible());
            bool html = true; string body = "<p>result</p>", attachPath = path, attachName = "random.bin";
            var message = device(1).LimitMessageSize(null, "Random", ref html, ref body, ref attachPath, ref attachName);
            StringAssert.Contains(message, "exceeds the maximum size of the emails (1 MB)");
            StringAssert.Contains(message, "'Random' (2 MB)");
            Assert.AreEqual("", attachPath);
            Assert.AreEqual(message, body);
            Assert.IsFalse(html);
        }

        [TestMethod]
        public void LargeHtmlBodySentAsZip()
        {
            bool html = true; string body = "<html><body><table>" + string.Concat(Enumerable.Repeat("<tr><td>Chai</td><td>18.00</td></tr>", 80000)) + "</table></body></html>", attachPath = "", attachName = "";
            var message = device(1).LimitMessageSize(null, "Alert: new rows", ref html, ref body, ref attachPath, ref attachName);
            StringAssert.Contains(message, "ZIP");
            Assert.IsFalse(html, "the body becomes a text telling that the result is attached");
            StringAssert.Contains(body, "too large to be displayed in the email");
            StringAssert.EndsWith(attachName, ".zip");
            Assert.IsTrue(File.Exists(attachPath));
        }

        [TestMethod]
        public void NoZipWhenTheDeviceRefusesThem()
        {
            //AllowZipAttachments false: a large result is not compressed but removed from the email, an HTML body too
            var d = device(1);
            d.AllowZipAttachments = false;
            var path = file("nozip.csv", compressible());
            bool html = false; string body = "Please find the report", attachPath = path, attachName = "Products.csv";
            var message = d.LimitMessageSize(null, "Products", ref html, ref body, ref attachPath, ref attachName);
            StringAssert.Contains(message, "exceeds the maximum size of the emails (1 MB)");
            Assert.AreEqual("", attachPath);
            Assert.AreEqual(message, body);

            html = true; body = "<html><body>" + string.Concat(Enumerable.Repeat("<p>Chai 18.00</p>", 100000)) + "</body></html>"; attachPath = ""; attachName = "";
            message = d.LimitMessageSize(null, "Alert", ref html, ref body, ref attachPath, ref attachName);
            StringAssert.Contains(message, "exceeds the maximum size");
            Assert.IsFalse(html);
            Assert.AreEqual("", attachPath);

            //Below the limit, the email is unchanged
            path = file("nozip-small.csv", Encoding.UTF8.GetBytes("Product;Amount\r\nChai;18.00\r\n"));
            html = false; body = "body"; attachPath = path; attachName = "small.csv";
            Assert.IsNull(d.LimitMessageSize(null, "Small", ref html, ref body, ref attachPath, ref attachName));
            Assert.AreEqual(path, attachPath);
        }

        [TestMethod]
        public void ZipResultOptionIgnoredWhenTheDeviceRefusesZip()
        {
            var report = Report.Create(Repository.Create());
            report.DisplayName = "Zip test";
            report.OutputToExecute = new ReportOutput() { ZipResult = true };
            var path = file("zipoption.csv", compressible());
            report.ResultFilePath = path;

            var d = device(0);
            d.AllowZipAttachments = false;
            d.HandleZipOptions(report);
            Assert.AreEqual(path, report.ResultFilePath, "the result is not compressed");
            StringAssert.Contains(report.ExecutionMessages, "is ignored");
            Assert.IsTrue(new OutputEmailDevice().AllowZipAttachments, "ZIP attachments allowed by default");
        }

        [TestMethod]
        public void SendEmailAppliesTheLimit()
        {
            var d = device(1);
            var path = file("send.bin", incompressible());
            d.SendEmail("sender@test.com", "to@test.com", "", "", "", "Too large", false, "body", path, "send.bin");
            var eml = Directory.GetFiles(d.PickupDirectoryPath, "*.eml").Single();
            Assert.IsTrue(new FileInfo(eml).Length < 100 * 1024, "the 2 MB attachment is not in the email");
            var content = File.ReadAllText(eml);
            Assert.IsFalse(content.Contains("send.bin"), "no attachment");
            File.Delete(eml);

            //The new signature with the report (null here) applies it too
            d.SendEmail("sender@test.com", "to@test.com", "", "", "", "Compressed", false, "body", file("send.csv", compressible()), "send.csv", null);
            eml = Directory.GetFiles(d.PickupDirectoryPath, "*.eml").Single();
            StringAssert.Contains(File.ReadAllText(eml), "send.zip");
        }
    }
}
