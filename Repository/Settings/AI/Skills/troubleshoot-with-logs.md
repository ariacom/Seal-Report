# Troubleshoot with logs

Load this skill to diagnose server and report issues — errors, failed
authentications, slow or failing report executions, and scheduler problems —
using the server log files and the system check reports. Read-only.

## Tools

- **`log_read`** — read server log files for a date. Types:
  - `events` — server start/stop, authentication, errors
  - `executions` — report execution traces
  - `schedules` — scheduler runs
  Date defaults to today (pass `YYYY-MM-DD` for another day). Use `keyword` to
  filter lines (e.g. a user name, report name, error code, `Error`,
  `FailureAudit`, `authenticated`). `max_lines` defaults to 200.
- **`report_execute_get_data`** — run a system check report for structured data.
- **`report_list`** — locate the relevant system report.

## Useful system reports for diagnosis
| Path | Purpose |
|---|---|
| `System\210 Audit - Last Errors` | Most recent server errors |
| `System\220 Audit - Login Failures` | Failed authentication attempts |
| `System\310 Reports - Check Executions` | Execution status and error check |
| `System\350 Reports - Executions` | Report execution history |
| `System\320 Reports - Server Schedule Definitions` | All configured schedules |
| `System\400 Sources - Check Sources` | Datasource connectivity check |

## Workflow

1. Identify the symptom (error, failed login, slow/failed execution, missed
   schedule) and the date.
2. Read the relevant log type with `log_read`, using `keyword` to narrow down.
   **When diagnosing an issue, check both `events` and `executions` for the same
   date.** For scheduler problems, also read `schedules`.
3. Cross-check with the matching system report via `report_execute_get_data`
   (prefer the structured report over raw logs when it covers the question).
4. Present findings clearly: highlight errors, failed logins, unusual patterns,
   and concrete, actionable recommendations.

## Rules
- Read-only — never modify report files or user/group configuration.
- Prefer the system check reports over raw logs when a report covers the question.
- When you mention a report the user could run, include an
  `[EXECUTE_REPORT:System\…|Display Name]` tag on its own line, reproducing the
  path exactly. Emit it verbatim in every language.
