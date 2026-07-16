# Third-Party Notices

Seal Report is licensed under the [MIT License](LICENSE). It includes third-party
packages that are distributed under their own licenses. Most are permissive
(MIT, Apache-2.0, BSD); the components below deserve special attention.

## Components with specific license terms

### QuestPDF (PDF generation)
[QuestPDF](https://www.questpdf.com/) is dual-licensed. Seal Report uses it under the
[QuestPDF Community License](https://www.questpdf.com/license/community.html), which is
free for open-source projects distributed under an OSI-approved license (Seal Report
qualifies via its MIT license).

**For users of Seal Report:** per the [QuestPDF License Guide](https://www.questpdf.com/license/guide.html),
organizations whose own code does not directly reference or call QuestPDF APIs — i.e. that
use QuestPDF only as a transitive dependency inside Seal Report — qualify for the free
Community License regardless of revenue. However, if **your own code calls QuestPDF APIs
directly** (for example in a custom Razor script or a product embedding Seal Report) and
your organization exceeds USD 1,000,000 in annual revenue, you may need your own QuestPDF
Professional or Enterprise license. Please review the QuestPDF license terms for your case.

### EPPlus 4.5.3.3 (Excel processing)
[EPPlus](https://github.com/JanKallman/EPPlus) is used at version **4.5.3.3**, the last
version published under the **LGPL-2.1** license. Seal Report deliberately stays on this
version: EPPlus 5 and later switched to the Polyform Noncommercial license, which requires
a paid license for commercial use. Do not upgrade this package.

### Oracle.ManagedDataAccess.Core (Oracle database driver)
Distributed by Oracle under the
[Oracle Free Use Terms and Conditions (FUTC)](https://www.oracle.com/downloads/licenses/oracle-free-license.html).
Free to use and redistribute, but not an open-source license.

## Other notable packages

| Package | License |
|---|---|
| MySqlConnector | MIT |
| Npgsql (PostgreSQL) | PostgreSQL License (MIT-like) |
| Microsoft.Data.SqlClient | MIT |
| System.Data.SQLite | Public Domain |
| MongoDB.Driver | Apache-2.0 |
| ScottPlot | MIT |
| PuppeteerSharp | MIT |
| SkiaSharp / HarfBuzzSharp | MIT |
| Newtonsoft.Json | MIT |
| MailKit / MimeKit | MIT |
| AngleSharp | MIT |
| HtmlSanitizer | MIT |
| SharpZipLib | MIT |
| FluentFTP | MIT |
| SSH.NET | MIT |
| DocumentFormat.OpenXml | MIT |
| RazorEngineCore | MIT |
| jose-jwt | MIT |
| DiffPlex | Apache-2.0 |
| SendGrid | MIT |
| Twilio | MIT |
| Azure SDKs / Microsoft.* packages | MIT |
| TaskScheduler | MIT |
| Bootstrap, jQuery and other web assets | MIT |
| Chart.js / ECharts / Plotly.js | MIT / Apache-2.0 / MIT |
| Font Awesome (Free) | CC BY 4.0 (icons), OFL (fonts), MIT (code) |

This list covers the main dependencies; each NuGet package and web asset retains its own
license file and copyright notice. If you redistribute Seal Report, keep this notice file
and the third-party license texts that ship with the packages.
