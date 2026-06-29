# Manage a data source (metadata model)

Load this skill when the user asks to **change the data source / metadata model
itself** — add a table, add a derived column, rename or reformat a column, build
an enumerated list, create or fix a join, change a column's SQL expression, or
remove/refresh metadata — as opposed to building or running a report.

> A **report** is one `.srex` file with a single owner. A **data source** is
> shared infrastructure: one edit can change or break **every report built on
> it**. Treat data-source edits with far more caution than report edits.

## Hard prerequisites

- These tools require **write (Edit) access to the data source's own folder**,
  granted by an administrator by publishing that folder (e.g. `Sources`) in the
  security group's **Repository Folders** with the *Read write* right. If a tool
  returns a "not allowed" error, stop and tell the user their group lacks Edit
  access on the source folder — do not try to work around it.
- You may only touch the data sources you can access (the same set returned by
  `datasource_list`).
- **Every write is backed up first.** Each writing tool copies the source file to
  a timestamped `.bak-…` next to it before saving, so any edit can be reverted.

## Always-on workflow

1. Call `datasource_list` to get the `MetaSourceGUID`.
2. Call `datasource_get_detail` to get the exact table names and the
   `MetaColumnGUID` of every column you will touch. **Never guess or fabricate a
   GUID** — copy it verbatim.
3. Make the change with the appropriate tool below.
4. After the change, briefly tell the user what changed and (for destructive
   changes) which reports were affected.

## Tools

### Read / safety
- `datasource_find_usage` — **the safety primitive.** Given a column GUID (or a
  table name), returns the list of reports that reference it. **You MUST call this
  before any destructive change** (see below) and show the user the affected
  reports.
- `datasource_suggest_joins` — inspects keys and column names and returns
  *proposed* joins. It does **not** apply anything; present the suggestions and
  let the user pick, then apply with `datasource_apply_join`.

### Additive — safe, never breaks an existing report
- `datasource_add_table` — add a database table (by name) or a custom SQL table
  (by SELECT) to the source, with its columns.
- `datasource_add_column` — add a derived / computed column (a SQL expression) to
  an existing table.
- `datasource_set_column` — set a column's display name, category, number/date
  format, or type. Cosmetic: reports bind columns by GUID, so this never breaks
  references.
- `datasource_manage_enum` — create an enumerated list (static, or dynamic from a
  column) and optionally attach it to a column.

### Destructive — can change or break existing reports
**Rules for every destructive tool (`datasource_set_column_sql`,
`datasource_apply_join`, `datasource_remove`, `datasource_refresh_schema`):**

1. First call `datasource_find_usage` for the affected column/table.
2. Show the user exactly what will change **and the list of affected reports**.
3. Only call the tool **after the user explicitly approves**, passing
   `confirm: true`. The tool refuses to run without `confirm: true`.

- `datasource_set_column_sql` — change a column's underlying SQL expression,
  type, or aggregate flag. **High impact**: changes results in every report using
  the column.
- `datasource_apply_join` — add or update a join between two tables. A wrong join
  skews results model-wide.
- `datasource_remove` — remove a table, column, join, or enum.
- `datasource_refresh_schema` — re-read the database schema into the source's
  dynamic tables (adds new columns, updates types, drops columns no longer in the
  database).

## Rules
- Friendly, human-readable **display names** ("Full Customer Name"), never raw
  identifiers, for anything the end user sees.
- Never change a column's SQL or type, never remove anything, and never refresh
  the schema without first running `datasource_find_usage` and getting the user's
  explicit go-ahead.
- Prefer the additive tools. Reach for a destructive tool only when the user
  clearly asks to change or remove existing metadata.
- Never fabricate GUIDs — always source them from `datasource_get_detail`.
