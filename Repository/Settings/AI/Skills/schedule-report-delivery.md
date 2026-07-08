# Schedule report delivery (outputs & schedules)

Load this skill when the user wants to configure how an **existing** report is
delivered and when it runs — save to a folder, email it, upload it to FTP/SFTP
or SharePoint, or run it on a recurring or one-time schedule. This skill only configures outputs
and schedules; it never creates or edits report content.

## Trigger phrases
| User says | Action |
|---|---|
| "save to a folder", "export to C:\…", "write to disk", "output as PDF/Excel" | `report_configure_output` with `device_type=folder` |
| "email this report to…", "send by email", "email results to…" | `report_configure_output` with `device_type=email` |
| "upload to FTP", "send to SFTP", "push to file server" | `report_configure_output` with `device_type=ftp` |
| "upload to SharePoint", "put in the document library", "send to the SharePoint site" | `report_configure_output` with `device_type=sharepoint` |
| "run every day", "schedule daily at 8am", "every Monday", "automate this report" | `report_configure_output` (if no output yet) then `report_configure_schedule` |
| "run once on [date]", "execute tomorrow at 6am" | `report_configure_output` + `report_configure_schedule` with `type=once` |
| "delete the output", "remove the output" | `report_configure_output` with `action=delete` |
| "delete the schedule", "unschedule", "stop the scheduled run" | `report_configure_schedule` with `action=delete` |

## Steps — always in this order

1. **`report_get_detail`** — inspect existing outputs and schedules before adding
   or modifying one.
2. **`report_configure_output`** — configure delivery. A report can have multiple
   outputs (e.g. PDF to a folder AND emailed to a recipient). Each call returns an
   `outputGUID`; store it to link a schedule or to update/delete that specific
   output later. Omit `output_guid` to create a new output; pass it to update or
   to target `action=delete`.
   - Call **`device_list`** first when several devices may exist or the user must
     choose where to deliver, then pass the chosen `device_name`.

   **Folder output** (`device_type=folder`):
   - `folder_path` — `Personal` / `Personal\subfolder` (→ `%SEALPERSONALREPOSITORY%`);
     `Reports\subfolder` (→ `%SEALREPORTSREPOSITORY%`); an absolute path
     (e.g. `C:\Reports\Daily`); or a path with `%SEALREPOSITORY%`.
   - `file_name` — use `{0:yyyyMMdd}` for the execution date. **Never include a
     file extension** — Seal appends it from `output_format`.
   - `output_format` — `Excel` (default), `pdf`, `csv`, `html`, `Text`, `Json`.

   **Email output** (`device_type=email`):
   - `email_to` (required, semicolon-separated), `email_cc`, `email_bcc`.
   - `email_subject` (defaults to report name), `email_body`.
   - `email_html_body=true` sends the report HTML as the body (no attachment).
   - `email_skip_attachments=true` sends body only, no file attached.
   - `output_format` — format of the attached file (default `Excel`).

   **FTP output** (`device_type=ftp`):
   - `folder_path` — remote directory (e.g. `/reports/daily`, default `/`).
   - `file_name` — same template rules, **no extension**.
   - `output_format` — format of the uploaded file (default `Excel`).

   **SharePoint output** (`device_type=sharepoint`):
   - `folder_path` — folder in the document library (e.g. `/Monthly Reports`,
     default `/` for the library root).
   - `file_name` — same template rules, **no extension**.
   - `output_format` — format of the uploaded file (default `Excel`).

3. **`report_configure_schedule`** — set when the output runs.
   - `output_guid` — **required when the report has multiple outputs** so the
     schedule binds to the correct one.
   - `type` — `daily` | `weekly` | `monthly` | `once`.
   - `start_datetime` — ISO 8601, e.g. `2025-06-01T07:00:00`.
   - **daily** → `days_interval` (default 1 = every day).
   - **weekly** → `weekdays` int array, 0=Sunday … 6=Saturday (e.g. `[1]` = Monday).
   - **monthly** → `months` (1–12, omit for every month) and `days` (1–31, 32=last day).
   - **Requires Administrator rights (Windows).** Registering a schedule writes a
     Windows Task Scheduler task, so the Seal server/process must run **as
     Administrator**. If the tool returns `Access is denied` / `E_ACCESSDENIED`,
     the schedule was **not** created — do **not** retry. Tell the user the output
     was saved but the schedule could not be registered because the Seal server is
     not running in Administrator mode, and ask them to restart it elevated, then
     re-request the schedule.

## Rules
- Configure outputs/schedules on **existing reports** only — never create or edit
  report content.
- **Always `report_configure_output` before `report_configure_schedule`** — a
  schedule requires an output to exist.
- **CRITICAL — never use `file_manage` for output or schedule operations.**
  "delete the output" → `report_configure_output action=delete`;
  "delete the schedule" → `report_configure_schedule action=delete`.
- **Never include a file extension in `file_name`.**
- When the user asks only to schedule a folder output without a format/folder,
  default to `Excel` and ask for the folder path if not given.
- **After every successful `report_configure_output`, emit an
  `[EXECUTE_REPORT:Reports\…\report.srex|Report Name : Output Name|outputGUID]`
  tag** on its own line so the user can test the output immediately. Reproduce the
  path exactly as returned by `report_list`. Emit the tag verbatim in every
  language — never translate or reword it.
