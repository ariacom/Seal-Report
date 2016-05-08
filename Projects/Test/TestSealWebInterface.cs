using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Seal.Model;
using System.Text;

namespace Test
{
    class SWIUserResponse : SWIUser
    {
        public string error = "";
    }

    class SWIFolderResponse : SWIFolder
    {
        public string error = "";
    }

    class SWIFolderDetailResponse : SWIFolderDetail
    {
        public string error = "";
    }

    class SWIReportDetailResponse : SWIReportDetail
    {
        public string error = "";
    }

    class SWIURLResponse
    {
        public string url = "";
        public string error = "";
    }

    class SWICultureResponse
    {
        public string culture = "";
        public string error = "";
    }

    class SWITranslationResponse
    {
        public string text = "";
        public string error = "";
    }

    class SWILogoutResponse
    {
        public string error = "";
    }

    [TestClass]
    public class TestSealWebInterface
    {
        [TestMethod]
        public async Task SWIConnectionTest()
        {
            //Just test the basic methods of SWI. The Server must be running... 
            var serverURL = "http://localhost/Seal/";
            //serverURL = "http://localhost:17178/";
            var httpClient = new HttpClient();

            using (var response = await httpClient.PostAsJsonAsync(serverURL + "SWILogin", new { user = "", password = "" }))
            {
                var result = await response.Content.ReadAsAsync<SWIUserResponse>();
                Assert.IsTrue(string.IsNullOrEmpty(result.error));
                Assert.IsTrue(result.name == "Anonymous" && result.group == "Default Group");
            }

            using (var response = await httpClient.PostAsJsonAsync(serverURL + "SWIGetFolders", new { path = @"\" }))
            {
                var result = await response.Content.ReadAsAsync<SWIFolderResponse>();
                Assert.IsTrue(string.IsNullOrEmpty(result.error));
            }

            using (var response = await httpClient.PostAsJsonAsync(serverURL + "SWIGetFolderDetail", new { path = @"\Samples" }))
            {
                var result = await response.Content.ReadAsAsync<SWIFolderDetailResponse>();
                Assert.IsTrue(string.IsNullOrEmpty(result.error));
                Assert.IsTrue(result.files.Length > 10);
            }

            using (var response = await httpClient.PostAsJsonAsync(serverURL + "SWIGetReportDetail", new { path = @"\Samples\07-Outputs and schedules.srex" }))
            {
                var result = await response.Content.ReadAsAsync<SWIReportDetailResponse>();
                Assert.IsTrue(string.IsNullOrEmpty(result.error));
                Assert.IsTrue(result.views.Length == 2 && result.outputs.Length == 3);
            }

            using (var response = await httpClient.PostAsJsonAsync(serverURL + "SWIExecuteReport?r0_name=Quantity&r0_operator=Between&r0_value_1=34&r0_value_2=123", 
                new { path = @"\Search - Orders.srex" }))
            {
                var result = await response.Content.ReadAsAsync<SWIURLResponse>();
                Assert.IsTrue(string.IsNullOrEmpty(result.error));
                Assert.IsTrue(!string.IsNullOrEmpty(result.url));
                Process.Start(result.url);
             }

            using (var response = await httpClient.PostAsJsonAsync(serverURL + "SWISetCulture", new { culture = "it-IT" }))
            {
                var result = await response.Content.ReadAsAsync<SWICultureResponse>();
                Assert.IsTrue(string.IsNullOrEmpty(result.error));
                Assert.IsTrue(result.culture == "it-IT");
            }

            using (var response = await httpClient.PostAsJsonAsync(serverURL + "SWITranslate", new { context = "Report", reference = "report restrictions" }))
            {
                var result = await response.Content.ReadAsAsync<SWITranslationResponse>();
                Assert.IsTrue(string.IsNullOrEmpty(result.error));
                Assert.IsTrue(!string.IsNullOrEmpty(result.text));
            }

            using (var response = await httpClient.PostAsJsonAsync(serverURL + "SWIRepositoryTranslate", new { context = "Element", instance = "Customers.City", reference = "City" }))
            {
                var result = await response.Content.ReadAsAsync<SWITranslationResponse>();
                Assert.IsTrue(string.IsNullOrEmpty(result.error));
                Assert.IsTrue(!string.IsNullOrEmpty(result.text));
            }

            using (var response = await httpClient.PostAsJsonAsync(serverURL + "SWILogout", new { }))
            {
                var result = await response.Content.ReadAsAsync<SWILogoutResponse>();
                Assert.IsTrue(string.IsNullOrEmpty(result.error));
            }
        }
    }
}
