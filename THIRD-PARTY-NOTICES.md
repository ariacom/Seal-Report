# Third-Party Notices

Seal Report is licensed under the [MIT License](LICENSE). It includes third-party
packages that are distributed under their own licenses. Most are permissive
(MIT, Apache-2.0, BSD); the components below deserve special attention.

## Components with specific license terms

### QuestPDF (PDF generation)
Version 2026.2.4 (released 2026-03-20).
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

### Oracle.ManagedDataAccess.Core (Oracle database driver)
Version 23.26.200 (released 2026-04-06).
Distributed by Oracle under the
[Oracle Free Use Terms and Conditions (FUTC)](https://www.oracle.com/downloads/licenses/oracle-free-license.html).
Free to use and redistribute, but not an open-source license.

## Other notable packages

| Package | Version | Release date | License |
|---|---|---|---|
| MySqlConnector | 2.6.1 | 2026-06-25 | MIT |
| Npgsql (PostgreSQL) | 10.0.2 | 2026-03-12 | PostgreSQL License (MIT-like) |
| Microsoft.Data.SqlClient | 7.0.1 | 2026-04-24 | MIT |
| System.Data.SQLite | 1.0.119 | 2024-09-29 | Public Domain |
| MongoDB.Driver | 3.9.0 | 2026-05-27 | Apache-2.0 |
| ScottPlot | 5.1.58 | 2026-03-29 | MIT |
| PuppeteerSharp | 24.40.0 | 2026-03-20 | MIT |
| SkiaSharp / HarfBuzzSharp | 3.119.2 / 8.3.1.3 | 2026-02-07 | MIT |
| Newtonsoft.Json | 13.0.4 | 2025-09-16 | MIT |
| MailKit / MimeKit | 4.16.0 | 2026-04-15 | MIT |
| AngleSharp | 1.5.0 | 2026-06-06 | MIT |
| HtmlSanitizer | 9.1.923-beta | 2026-04-27 | MIT |
| SharpZipLib | 1.4.2 | 2023-01-30 | MIT |
| FluentFTP | 54.1.1 | 2026-04-10 | MIT |
| SSH.NET | 2026.0.0 | 2026-08-09 | MIT |
| ClosedXML (Excel processing) | 0.105.1 | 2026-07-25 | MIT |
| DocumentFormat.OpenXml | 3.5.1 | 2026-03-18 | MIT |
| RazorEngineCore | 2026.1.1 | 2026-01-17 | MIT |
| jose-jwt | 5.3.0 | 2026-03-27 | MIT |
| DiffPlex | 1.9.0 | 2025-09-13 | Apache-2.0 |
| SendGrid | 9.29.3 | 2024-04-02 | MIT |
| Twilio | 7.14.7 | 2026-04-14 | MIT |
| Azure SDKs / Microsoft.* packages | various | various | MIT |
| TaskScheduler | 2.12.2 | 2025-07-08 | MIT |
| Bootstrap | 5.3.3 | 2024-02-20 | MIT |
| jQuery | 3.7.1 | 2023-08-28 | MIT |
| DataTables | 2.3.7 | 2026-01-30 | MIT |
| Chart.js | 4.5.1 | 2025-10-13 | MIT |
| ECharts | 5.6.0 | 2024-12-28 | Apache-2.0 |
| Plotly.js | 3.1.1 | 2025-09-29 | MIT |
| Font Awesome (Free) | 6.7.2 | 2024-12-16 | CC BY 4.0 (icons), OFL (fonts), MIT (code) |

Versions and release dates are those shipped with the current Seal Report release.
This list covers the main dependencies; each NuGet package and web asset retains its own
license file and copyright notice. If you redistribute Seal Report, keep this notice file
and the third-party license texts that ship with the packages.
