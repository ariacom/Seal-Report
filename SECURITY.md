# Security Policy

## Supported Versions

Security fixes are provided for the latest released version of Seal Report.

| Version | Supported          |
| ------- | ------------------ |
| 10.x    | :white_check_mark: |
| < 10.0  | :x:                |

## Reporting a Vulnerability

Please **do not report security vulnerabilities through public GitHub issues**.

Instead, use GitHub's private vulnerability reporting:
[Report a vulnerability](https://github.com/ariacom/Seal-Report/security/advisories/new)

Alternatively, you can contact us by email at **contact@ariacom.com**.

Please include as much of the following as you can:

- The type of issue (e.g. SQL injection, cross-site scripting, script/template injection, authentication bypass)
- The affected component (Web Report Server, Web Report Designer, Report Designer, Task Scheduler, report execution engine)
- Step-by-step instructions or a proof of concept to reproduce the issue
- The version of Seal Report and the environment used
- The impact of the issue, including how an attacker might exploit it

You should receive an acknowledgment within a few business days. We will keep you
informed of the progress towards a fix and may ask for additional information.

## Scope Notes

- Report scripts and templates (Razor) are executed with full trust by design.
  The ability of an **authorized report designer** to execute code through
  Razor scripts is a documented feature, not a vulnerability. Reports involving
  Razor script execution are only in scope when they allow a user **without**
  design rights to inject or execute scripts.
- Vulnerabilities in third-party dependencies should be reported to the
  respective projects; however, feel free to notify us so we can update the
  dependency.

## Disclosure

We ask that you give us a reasonable time to release a fix before any public
disclosure. We are happy to credit reporters in the release notes unless you
prefer to remain anonymous.
