using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Seal.Model;
using System.Text;
using System.Net;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Test
{
    class SWIUserProfileResponse : SWIUserProfile
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

    class SWIVersionsResponse
    {
        public string SWIVersion = "";
        public string SRVersion = "";
        public string error = "";
    }

    class SWIURLResponse
    {
        public string url = "";
        public string error = "";
    }

    class SWIEmptyResponse
    {
        public string error = "";
    }

    class SWITranslationResponse
    {
        public string text = "";
        public string error = "";
    }

    [TestClass]
    public class TestSealWebInterface
    {
        [TestMethod]
        public async Task SWIConnectionTest()
        {
            //Test the basic methods of SWI. The Server must be running... 
            var serverURL = "http://localhost:17178/Seal/";
            // serverURL = "http://demo.sealreport.org/";

            var httpClient = new HttpClient();

            var response = await httpClient.PostAsJsonAsync(serverURL + "SWIGetVersions", new { });
            var versions = await response.Content.ReadAsAsync<SWIVersionsResponse>();
            Assert.IsTrue(string.IsNullOrEmpty(versions.error));
            Assert.IsTrue(!string.IsNullOrEmpty(versions.SWIVersion) && !string.IsNullOrEmpty(versions.SRVersion));

            response = await httpClient.PostAsJsonAsync(serverURL + "SWILogin", new { user = "", password = "" });
            var profile = await response.Content.ReadAsAsync<SWIUserProfileResponse>();
            Assert.IsTrue(string.IsNullOrEmpty(profile.error));
            Assert.IsTrue(!string.IsNullOrEmpty(profile.group));

            response = await httpClient.PostAsJsonAsync(serverURL + "SWIGetFolders", new { path = @"\" });
            var folder = await response.Content.ReadAsAsync<SWIFolderResponse>();
            Assert.IsTrue(string.IsNullOrEmpty(folder.error));

            response = await httpClient.PostAsJsonAsync(serverURL + "SWIGetFolderDetail", new { path = @"\Samples" });
            var folderDetail = await response.Content.ReadAsAsync<SWIFolderDetailResponse>();
            Assert.IsTrue(string.IsNullOrEmpty(folderDetail.error));
            Assert.IsTrue(folderDetail.files.Length > 10);

            response = await httpClient.PostAsJsonAsync(serverURL + "SWIGetReportDetail", new { path = @"\Samples\07-Outputs and schedules.srex" });
            var reportDetail = await response.Content.ReadAsAsync<SWIReportDetailResponse>();
            Assert.IsTrue(string.IsNullOrEmpty(reportDetail.error));
            Assert.IsTrue(reportDetail.views.Length == 1 && reportDetail.outputs.Length == 3);

            var parameters = new List<KeyValuePair<string, string>>();
            parameters.Add(new KeyValuePair<string, string>("path", @"\Search - Orders.srex"));
            parameters.Add(new KeyValuePair<string, string>("r0_name", "Quantity"));
            parameters.Add(new KeyValuePair<string, string>("r0_operator", "Between"));
            parameters.Add(new KeyValuePair<string, string>("r0_value_1", "34"));
            parameters.Add(new KeyValuePair<string, string>("r0_value_2", "123"));

            var formUrlEncodedContent = new FormUrlEncodedContent(parameters);
            formUrlEncodedContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var request = new HttpRequestMessage(HttpMethod.Post, serverURL + "SWExecuteReportToResult");
            request.Content = formUrlEncodedContent;
            var response2 = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
            var reader = new StreamReader(response2.Content.ReadAsStreamAsync().Result);

            var fileName = Path.GetTempFileName() + ".htm";
            File.WriteAllText(fileName,reader.ReadToEnd());
            Process.Start(fileName);

            response = await httpClient.PostAsJsonAsync(serverURL + "SWISetUserProfile", new { culture = "it-IT" });
            var empty = await response.Content.ReadAsAsync<SWIEmptyResponse>();
            Assert.IsTrue(string.IsNullOrEmpty(empty.error));

            response = await httpClient.PostAsJsonAsync(serverURL + "SWIGetUserProfile", new { });
            profile = await response.Content.ReadAsAsync<SWIUserProfileResponse>();
            Assert.IsTrue(string.IsNullOrEmpty(profile.error));
            Assert.IsTrue(!string.IsNullOrEmpty(profile.group) && profile.culture == "Italian (Italy)");

            response = await httpClient.PostAsJsonAsync(serverURL + "SWITranslate", new { context = "Report", reference = "report restrictions" });
            var translation = await response.Content.ReadAsAsync<SWITranslationResponse>();
            Assert.IsTrue(string.IsNullOrEmpty(translation.error));
            Assert.IsTrue(!string.IsNullOrEmpty(translation.text));

            response = await httpClient.PostAsJsonAsync(serverURL + "SWITranslate", new { context = "Element", instance = "Customers.City", reference = "City" });
            translation = await response.Content.ReadAsAsync<SWITranslationResponse>();
            Assert.IsTrue(string.IsNullOrEmpty(translation.error));
            Assert.IsTrue(!string.IsNullOrEmpty(translation.text));

            response = await httpClient.PostAsJsonAsync(serverURL + "SWILogout", new { });
            empty = await response.Content.ReadAsAsync<SWIEmptyResponse>();
            Assert.IsTrue(string.IsNullOrEmpty(empty.error));
        }
    }
}
