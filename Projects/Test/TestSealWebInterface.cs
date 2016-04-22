using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Test
{
    class SWIUserResponse
    {
        public string name = "";
        public string group = "";
        public string culture = "";
        public string error = "";
    }

    class SWIFolderResponse
    {
        public string folderPath = "";
        public string displayName = "";
        public SWIFolderResponse[] folders = null;
        public string error = "";
    }

    [TestClass]
    public class TestSealWebInterface
    {
        [TestMethod]
        public async Task SWIConnectionTest()
        {
            //Just test the basic methods of SWI. The Server must be running... 
            var serverURL = "http://localhost:17178/";

            var httpClient = new HttpClient();
            var response = await httpClient.PostAsJsonAsync(serverURL + "SWILogin", new { user_name = "", password = "" });
            var user = await response.Content.ReadAsAsync<SWIUserResponse>();

            response = await httpClient.PostAsJsonAsync(serverURL + "SWIGetFolders", new { folderPath = "\\" });
            var folder = await response.Content.ReadAsAsync<SWIFolderResponse>();

           /* HttpResponseMessage httpResponse = httpClient.PostAsync(serverURL + "InitExecuteReport", new { folderPath = "\\Search - Orders.srex", r0_name = "Quantity", r0_operator = "Between", r0_value_1 = "34", r0_value_2 = "1234" }).Result;   
            if (httpResponse.IsSuccessStatusCode)
            {
                var products = response.Content.ReadAsStringAsync().Result;
            }
            */
            response = await httpClient.PostAsJsonAsync(serverURL + "InitExecuteReport", new { path = "\\Search - Orders.srex", r0_name = "Quantity", r0_operator = "Between", r0_value_1 = "34", r0_value_2 = "1234" });
            var reportResult = await response.Content.ReadAsStringAsync();
            var reportFile = Path.GetTempFileName() + ".htm";
            File.WriteAllText(reportFile, reportResult);
            Process.Start(reportFile);

        }
    }
}
