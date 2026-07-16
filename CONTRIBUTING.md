# Contributing to Seal Report

Thank you for your interest in contributing! Seal Report is free and open source under the [MIT License](LICENSE), maintained by [Ariacom](https://ariacom.com) — and community contributions are welcome.

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md) — by participating, you are expected to uphold it.

## Ways to contribute

- **Give a star** ⭐ — one click, increases the product visibility.
- **Ask and answer questions** in the [GitHub Discussions](https://github.com/ariacom/Seal-Report/discussions).
- **Report bugs** — open an issue with the product version, steps to reproduce, and any error message or log extract (logs are in the repository `Logs` folder).
- **Improve the documentation** — the [sealreport.org](https://sealreport.org) site is part of this repository (`Projects/SealDocumentation`), so documentation fixes are normal pull requests.
- **Contribute code** — bug fixes, view templates, task templates, security providers, or new features.

> **Planning a significant change?** Please open a discussion or an issue first to validate the approach before investing time in a pull request.

## Development setup

**Prerequisites**
- Windows with **Visual Studio 2026** (the solution targets **.NET 10**)
- Optional, depending on what you work on: a database (SQL Server, MySQL, PostgreSQL, SQLite, MongoDB...), IIS for the Web Report Server

**Build**
1. Clone the repository.
2. Open `Projects/Seal.sln` in Visual Studio.
3. Build the solution (Debug).

**Run**
- **Debug builds resolve their repository from the in-repo `Repository/` folder** (sources, connections, views, security, AI configuration) — no external setup needed: `Repository.FindRepository()` walks up from the executable to the sibling `Repository/` folder in Debug. Release builds use the path configured in `appsettings.json` (by default `C:\ProgramData\Seal Report Repository`).
- Start `SealReportDesigner` to design reports, `SealServerManager` for the administration, or `SealWebServer` for the web server.

## Repository layout

| Path | Content |
|---|---|
| `Projects/SealLibrary` | Main library (`Seal.Model` classes: Report, MetaTable, ReportRestriction, ReportTask...) compiled for the .NET runtime (web server, scheduler worker) |
| `Projects/SealLibraryWin` | Same sources compiled for the Windows applications (Report Designer, Server Manager) — **this is where the shared `.cs` files live** |
| `Projects/SealReportDesigner` | Report Designer (Windows application) |
| `Projects/SealServerManager` | Server Manager (Windows application) |
| `Projects/SealWebServer` | Web Report Server (ASP.NET Core) |
| `Projects/SealTaskScheduler` / `SealSchedulerService` / `SealSchedulerWorker` | Schedulers (console / Windows Service / worker for non-Windows OS) |
| `Projects/SealReportRunner` | Console runner used to execute and validate reports (see Testing) |
| `Projects/SealDocumentation` | The sealreport.org documentation web site |
| `Projects/Tests` | Unit tests and basic samples |
| `Repository/` | The development repository: data sources, sample reports, view templates, security providers, translations, AI configuration |

## Coding guidelines and gotchas

- **Match the existing style** of the file you edit (naming, comment density, formatting).
- **Shared sources**: `SealLibrary` compiles the `.cs` files of `SealLibraryWin` through explicit `<Compile Include=... Link=...>` items. If you add a new shared `.cs` file under `Projects/SealLibraryWin`, you must also add its Compile-link in `Projects/SealLibrary/SealLibrary.csproj`, or the web/server builds fail.
- **TypeScript, not minified JS**: for the web client, edit the `swi-*.ts` files in `Projects/SealWebServer/wwwroot/js`. The generated/minified `.js` files are build artifacts — do not edit them by hand.
- **`seal.css` twin copies**: `Projects/SealWebServer/wwwroot/css/seal.css` is the canonical copy; it is synchronized to `Repository/Views/css/seal.css` by the web build. Edit the `wwwroot` copy.
- **XML documentation**: public members of `SealLibrary` are documented with `///` comments (they feed the published API reference) — please document new public APIs.

## Testing your changes

- **Unit tests**: run the `Projects/Tests` project.
- **Run a report** with the console runner — it executes the report AND fully renders the result view, so a broken view template fails with exit code 1:

  ```
  Projects\SealReportRunner\bin\Debug\net10.0\SealReportRunner.exe "<absolute path to .srex>"
  ```

  Useful flags: `--assert "<text>"` (fail unless the rendered HTML contains the text), `--out <path>`, `--quiet`.
- **Regression reports**: test reports live in `Repository/Reports/System/Tests` (TST010, TST020, ...). Sample reports in `Repository/Reports/Samples` are also good smoke tests (they run on the bundled Northwind SQLite database).

## Submitting a pull request

1. Fork the repository and create a branch from `master`.
2. Keep the change focused: one topic per pull request.
3. Make sure the solution builds and the relevant tests/reports pass.
4. Describe **what** the change does and **why**; link the related issue or discussion.
5. For documentation changes, verify `Projects/SealDocumentation` still builds.

By submitting a pull request you agree that your contribution is licensed under the [MIT License](LICENSE) of the project.

## Questions?

Use the [GitHub Discussions](https://github.com/ariacom/Seal-Report/discussions) — for professional support, training or feature sponsoring, see [Seal Report Services](https://sealreport.com).
