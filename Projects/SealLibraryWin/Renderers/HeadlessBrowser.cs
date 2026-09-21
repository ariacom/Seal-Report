//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using PuppeteerSharp;
using Seal.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Seal.Renderer
{
    /// <summary>
    /// Headless browser showing the HTML result of a report: used by the renderers to print the result (HTML to PDF)
    /// or to take pictures of the JavaScript views (charts, gauges and maps for the PDF and Excel renderers)
    /// </summary>
    public class HeadlessBrowser : IAsyncDisposable
    {
        /// <summary>
        /// Headless browser: an unbranded Chromium build (BSD-3-Clause, no Google Chrome terms of service),
        /// downloaded at first use from the chromium-browser-snapshots bucket into Assemblies\Chromium.
        /// The revision is pinned to the nearest snapshot of the Chrome 153 stable branch point (September 2026).
        /// To upgrade: take chromium_main_branch_position of the current stable release from
        /// https://chromiumdash.appspot.com/fetch_releases?channel=Stable&amp;platform=Windows&amp;num=1
        /// and use the nearest revision that exists in https://storage.googleapis.com/chromium-browser-snapshots/Win_x64/
        /// </summary>
        public const string ChromiumRevisionWindows = "1681099";

        /// <summary>
        /// Chromium revision for Linux (nearest snapshot of the same branch point)
        /// </summary>
        public const string ChromiumRevisionLinux = "1681097";

        /// <summary>
        /// Width in pixels of the browser viewport showing the HTML result
        /// </summary>
        public static int ViewportWidth = 1600;

        /// <summary>
        /// Height in pixels of the browser viewport showing the HTML result
        /// </summary>
        public static int ViewportHeight = 1200;

        /// <summary>
        /// The browser
        /// </summary>
        public IBrowser Browser;

        /// <summary>
        /// The page showing the HTML result
        /// </summary>
        public IPage Page;

        /// <summary>
        /// Returns the path of the Chromium executable, the browser is downloaded at first use into the Assemblies folder of the repository
        /// </summary>
        public static async Task<string> GetChromiumPath(Repository repository)
        {
            //The download goes through the system default proxy; set fetcher.WebProxy for a specific one.
            var fetcher = new BrowserFetcher(new BrowserFetcherOptions() { Browser = SupportedBrowser.Chromium, Path = repository.AssembliesFolder });
            string chromiumRevision = fetcher.Platform == Platform.Linux ? ChromiumRevisionLinux : ChromiumRevisionWindows;
            string chromePath = fetcher.GetExecutablePath(chromiumRevision);
            if (!File.Exists(chromePath))
            {
                try
                {
                    await fetcher.DownloadAsync(chromiumRevision);
                    //The snapshot archive also contains a large test executable that is useless here (about 340 MB)
                    foreach (var testsName in new[] { "interactive_ui_tests.exe", "interactive_ui_tests" })
                    {
                        var testsPath = Path.Combine(Path.GetDirectoryName(chromePath), testsName);
                        if (File.Exists(testsPath)) File.Delete(testsPath);
                    }
                }
                catch (Exception ex)
                {
                    //No download possible (server without internet access): use any Chromium-based browser copied under the Assemblies folder
                    var expectedPath = chromePath;
                    chromePath = Directory.GetFiles(repository.AssembliesFolder, Path.GetFileName(expectedPath), SearchOption.AllDirectories).FirstOrDefault();
                    if (string.IsNullOrEmpty(chromePath))
                    {
                        var isLinux = fetcher.Platform == Platform.Linux;
                        var archiveUrl = $"https://storage.googleapis.com/chromium-browser-snapshots/{(isLinux ? "Linux_x64" : "Win_x64")}/{chromiumRevision}/{(isLinux ? "chrome-linux" : "chrome-win")}.zip";
                        var targetFolder = Path.GetDirectoryName(Path.GetDirectoryName(expectedPath));
                        throw new Exception($"Unable to download Chromium revision {chromiumRevision} into '{repository.AssembliesFolder}': {ex.Message}\r\nOn a server without internet access, install the browser manually: download {archiveUrl} and unzip it into '{targetFolder}' so that '{expectedPath}' exists.", ex);
                    }
                }
            }
            return chromePath;
        }

        /// <summary>
        /// Launch the browser and show an HTML result file, the page is returned when its resources are loaded and its scripts executed
        /// </summary>
        public static async Task<HeadlessBrowser> Open(Repository repository, string htmlFilePath)
        {
            var result = new HeadlessBrowser();
            try
            {
                var chromePath = await GetChromiumPath(repository);
                //Desktop size: with the default viewport of PuppeteerSharp (800x600), the Bootstrap columns are narrow and the pictures of the views may be truncated by their container
                result.Browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, ExecutablePath = chromePath, DefaultViewport = new ViewPortOptions() { Width = ViewportWidth, Height = ViewportHeight } });
                result.Page = await result.Browser.NewPageAsync();
                //The result is a local file (no HTTP Referer): the application is identified in the User-Agent, as required by the usage policy of
                //some resource providers (e.g. the tiles of the Map view)
                var userAgent = await result.Browser.GetUserAgentAsync();
                await result.Page.SetUserAgentAsync(string.Format("SealReport/{0} (+https://sealreport.org) {1}", Repository.ProductVersion, userAgent));
                await result.Page.GoToAsync("file:///" + htmlFilePath, null, new WaitUntilNavigation[] { WaitUntilNavigation.Networkidle0, WaitUntilNavigation.DOMContentLoaded });
                Thread.Sleep(200);
            }
            catch
            {
                await result.DisposeAsync();
                throw;
            }
            return result;
        }

        /// <summary>
        /// Returns the picture (PNG) of an element of the page
        /// </summary>
        public static async Task<byte[]> GetElementPicture(IPage page, string selector)
        {
            var element = await page.QuerySelectorAsync(selector);
            if (element == null) return null;
            //The element is scrolled into view for the picture: the fixed bars of the report (e.g. the top navigation bar) must not cover it
            await page.EvaluateExpressionAsync("document.querySelectorAll('.fixed-top,.sticky-top,.fixed-bottom,#back-to-top').forEach(function (e) { e.style.visibility = 'hidden'; })");
            return await element.ScreenshotDataAsync();
        }

        /// <summary>
        /// Close the page and the browser
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (Page != null) await Page.CloseAsync();
            if (Browser != null) await Browser.CloseAsync();
            Page = null;
            Browser = null;
        }
    }
}
