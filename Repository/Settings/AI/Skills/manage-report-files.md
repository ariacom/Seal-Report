# Manage report files

Load this skill when the user explicitly asks to **delete, rename, move, or copy
a report file** (or any repository file) — "delete the report", "rename it to…",
"move it to the Archive folder", "make a copy". These are file-level operations on
the `.srex` (or other) file, not changes to report content.

## Tools
- **`file_list`** — browse files in the folders the user can access (Reports and
  Personal roots). Returns relative paths in the exact format `file_manage`
  expects (e.g. `Reports\Sales\monthly.srex`, `Personal\user\notes.txt`) with
  size and last-modified date. Lists every file type. Filter by sub-folder
  (`folders`) or extension (`extension`).
- **`file_manage`** — perform the operation: `delete`, `rename` (new filename in
  the same folder), `move` (to a different folder or root), or `copy` (to a new
  path). Roots: `Reports`, `Personal`.

## Workflow
1. **Always call `file_list` first** to discover and confirm the exact source
   path before any `file_manage` operation. Never act on a guessed path.
2. Call `file_manage` with `action`, `path`, and (for rename/move/copy)
   `destination`.

## Rules
- Only operate on files within the `Reports` / `Personal` roots and folders the
  user has rights on.
- `file_manage` acts on the **file** only (the `.srex`). Use it only when the user
  explicitly says delete / rename / move / copy the report.
- **Never use `file_manage` to remove a report's output or schedule** — those are
  managed by `report_configure_output` / `report_configure_schedule` (the
  `schedule-report-delivery` skill). Deleting the file is not the same as deleting
  an output or schedule.
- Deleting a file is irreversible — always confirm the exact path with `file_list`
  first. When the request already names the delete ("delete report X", "delete all
  files in folder Y"), that request **is** the confirmation: perform the
  `file_manage` delete in the **same turn** — do not stop to ask the user to confirm
  again (you are running autonomously and will not receive an answer). Ask the user
  back only when the target is genuinely ambiguous (several files could match) or
  the request is vague about what to remove.
