using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Seal.Model;

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

    class SWIURLResponse
    {
        public string url = "";
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

            var httpClient = new HttpClient();
            var response = await httpClient.PostAsJsonAsync(serverURL + "SWILogin", new { user = "", password = "" });
            var user = await response.Content.ReadAsAsync<SWIUserResponse>();
            if (!string.IsNullOrEmpty(user.error)) throw new Exception(user.error);

            response = await httpClient.PostAsJsonAsync(serverURL + "SWIGetFolders", new { path = "\\" });
            var folder = await response.Content.ReadAsAsync<SWIFolderResponse>();
            if (!string.IsNullOrEmpty(folder.error)) throw new Exception(folder.error);


            response = await httpClient.PostAsJsonAsync(serverURL + "SWIExecuteReport", new { path = "\\Search - Orders.srex", format = "html", r0_name = "Quantity", r0_operator = "Between", r0_value_1 = "34", r0_value_2 = "1234" });
            var url = await response.Content.ReadAsAsync<SWIURLResponse>();
            if (!string.IsNullOrEmpty(url.error)) throw new Exception(url.error);

            Process.Start(url.url);

        }
    }
}
