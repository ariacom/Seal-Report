# Seal Report Library

**[Seal Report & Task](https://sealreport.org)** is a complete open source framework for producing reports from any database or NoSQL source, and for performing complex tasks (ETL, batch). Entirely written in C# for Microsoft .NET, and free for everyone under the MIT License.

This package provides the objects to **create, load and execute Seal reports** from your own .NET application.

> A full Seal Report repository (file structure with sources, connections, templates and settings) must be available on the machine running the program. Install [Seal Report](https://sealreport.org) or copy a repository folder to get started.

## Basic usage

Execute an existing report and generate its HTML result:

```csharp
using Seal.Model;

// Load the repository (path from appsettings.json or set Repository.RepositoryConfigurationPath)
Repository repository = Repository.Create();

// Load and execute a report
Report report = Report.LoadFromFile(@"C:\Seal\Reports\Sales.srex", repository);
report.ExecutionContext = ReportExecutionContext.TaskScheduler;
var execution = new ReportExecution() { Report = report };
execution.Execute();
while (report.IsExecuting) System.Threading.Thread.Sleep(100);
if (report.HasErrors) throw new Exception(report.ExecutionErrors);

// Generate the self-contained HTML result file
string resultPath = execution.GenerateHTMLResult();
```

Reports can also be created from scratch, modified, rendered in other formats (Excel, PDF, CSV, JSON, XML, Text) or scheduled — see the [documentation](https://sealreport.org) for more samples.

## Main features of Seal Report

* **Dynamic SQL sources**: use your own SQL, or let the Seal engine build the SQL dynamically.
* **LINQ queries**: join and query any data sources (SQL, Excel, XML, OLAP Cube, HTTP JSON, etc.).
* **Native pivot tables and HTML5 charts** (ChartJS, ECharts, Plotly, ScottPlot, Gauge).
* **AI Agents**: chat with role-based AI agents to design reports or analyze data, using your own provider (OpenAI, Azure OpenAI, Anthropic or Ollama).
* **Fully responsive HTML rendering** with the Razor engine, plus Excel and PDF results.
* **Web Report Server** (Windows and Linux) and **Report Scheduler**.
* **Report tasks & ETL** for batch operations, and native support of MongoDB.

## Links

* Web site & quick start guides: [sealreport.org](https://sealreport.org)
* Sources: [github.com/ariacom/Seal-Report](https://github.com/ariacom/Seal-Report)
* Free support & community: [GitHub Discussions](https://github.com/ariacom/Seal-Report/discussions)
* Professional support: [Seal Report Services](https://sealreport.com)

## License

Seal Report is free and open source under the [MIT License](https://github.com/ariacom/Seal-Report/blob/master/LICENSE). Third-party components are distributed under their own licenses, see [THIRD-PARTY-NOTICES.md](https://github.com/ariacom/Seal-Report/blob/master/THIRD-PARTY-NOTICES.md) (in particular QuestPDF and EPPlus if your own code calls their APIs directly).
