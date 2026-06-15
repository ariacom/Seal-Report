# Build a report from SQL

Use this skill when the user asks to create a new report backed by a SQL query
(a "report from SQL", "a report on the X table", "show me a report of …").
Follow these steps in order — do not skip the exploration steps.

## 1. Pick the data source

- Call `datasource_list` first to see the available sources, their connections,
  and tables, with the GUIDs you must pass to the other tools.
- If the user named a source, use its GUID. Otherwise use the default source
  (omit `datasource_guid`) unless the request clearly points to another one.

## 2. Understand the schema before writing SQL

- Call `datasource_get_detail` (or `database_get_columns`) to confirm the exact
  table and column names and their types. Never guess column names.
- When the request involves filtering on a category/enum column, call
  `database_get_sample_values` to discover the real values to filter on.

## 3. Write and validate the SQL

- Write a single `SELECT` statement. Keep it portable to the source's database
  engine; use the column names exactly as returned in step 2.
- Validate it with `database_execute_query` before creating the report. If it
  errors or returns unexpected rows, fix the SQL and re-run until it is correct.
- Prefer explicit column lists over `SELECT *` so the report columns are stable.
- Never run INSERT, UPDATE, DELETE, or DDL via `database_execute_query`.

### SQL dialect (use `DatabaseType` from `datasource_get_detail`)
- **MSSQLServer** → `[square bracket]` quoting, `TOP N`, `GETDATE()`
- **MySQL** → `` `backtick` `` quoting, `LIMIT N`, `NOW()`
- **PostgreSQL** → `"double quote"` quoting, `LIMIT N`, `NOW()`
- **Oracle** → `"double quote"` quoting, `ROWNUM`, `SYSDATE`
- **SQLite** → no quoting needed, `LIMIT N`, `datetime('now')`

### Hard rules for the report SQL
- **Never put `ORDER BY` in the SQL source** — Seal Report sorts at the view
  level; an `ORDER BY` in the source SQL breaks subqueries and CTEs.
- **Never end the SQL with a semicolon** — the query is wrapped inside a
  `FROM (…) AS sub` at runtime; a trailing `;` causes a syntax error.
- **Computed column aliases must use CamelCase** — write `SUM(x) AS TotalSales`,
  never `SUM(x) AS [Total Sales]` or `SUM(x) AS "Total Sales"`. Square brackets
  and quoted identifiers are only for referencing existing table/column names
  that require quoting.

## 4. Create the report

- Call `report_create_from_sql` with the validated `sql`, a clear `display_name`,
  the chosen `source_guid`, and a destination `path`.
- If the user did not specify a folder, ask or use the current working folder.
- Report back the created report path and a one-line summary of what it contains.

## Notes

- This is for SQL-model reports. If the request is better served by the source's
  defined metadata columns (tabular/pivot on the model), that is a metadata
  report instead — say so rather than forcing SQL.
- Respect the user's access rights: only use sources and folders they can access.
