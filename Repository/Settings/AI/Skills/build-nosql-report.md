# Build a NoSQL / MongoDB report

Load this skill when creating a report on a **NoSQL** data source (e.g. MongoDB).
`datasource_list` marks these sources `/ NoSQL` and `datasource_get_detail` shows
`Storage: NoSQL`. They are handled differently from SQL sources — there is no SQL
engine, so the SQL-dialect rules do not apply.

## Rules specific to NoSQL sources

- **Never write SQL for them.** `database_execute_query` rejects NoSQL sources.
- **Inspect** them with `database_get_columns` (the source's defined fields and
  types) and `database_get_sample_values` (distinct sample values). For MongoDB
  these read a preview of up to ~100 documents.
- `database_count_rows` does **not** return an exact count for MongoDB (only a
  preview is loaded). For an exact MongoDB count, create a report with a Count
  aggregate and run it.
- **Always create NoSQL reports with `report_create_from_xml`** (metadata model)
  — never `report_create_from_sql`. Aggregation, grouping and filtering work
  exactly as for SQL sources.
- When calling `report_check_model_type`, pass the source's `source_guid` so it
  detects a NoSQL source and confirms the metadata route.

## The critical naming rule

**Element and restriction `<Name>` must be the column's RAW field name — NOT the
`Table.Column` form.** A NoSQL model is resolved in memory: each
`<ReportElement>` / `<ReportRestriction>` is matched to a result-table column
whose name is the metadata column's exact field name (the value shown before the
`(GUID: …)` by `datasource_get_detail`, e.g. `year`, `account_id`,
`transactions.amount`). Use that bare field name verbatim as the `<Name>`.

- Correct: with table `transactions` and field `year` → `<Name>year</Name>`
- Wrong: `<Name>transactions.year</Name>` — a table-qualified name matches no
  column and the report fails with *"Column '…' does not belong to table …"*.

This applies only to NoSQL sources (SQL sources keep the `Table.Column` form).
`report_create_from_xml` also auto-corrects the name for NoSQL, but generate it
correctly.

## Workflow

1. `report_check_model_type` (pass `source_guid`) to confirm the metadata route.
2. `datasource_list` → `MetaSourceGUID`.
3. `datasource_get_detail` → the exact field name and `MetaColumnGUID` for each
   element and restriction.
4. `database_get_sample_values` for any enumerated restriction values.
5. **Get a real report's XML with `report_get_xml`** (use `report_list` to find an
   existing report, ideally one already on this source) and use it as the
   structural template — do **not** invent the file structure. A valid report XML
   has a `<Sources>` block with a `<ReportSource>` whose `<MetaSourceGUID>` is the
   data-source GUID, `<Models>` with a `<ReportModel>` (its `<SourceGUID>` matches
   the `<ReportSource>` GUID) holding the `<Elements>`, and the mandatory nested
   `<Views>` (Report → Model → Container → result sub-views).
6. Adapt that template: set the `<MetaSourceGUID>` to the Mongo DB source, replace
   the elements with yours (each `<MetaColumnGUID>` from `datasource_get_detail`,
   each `<Name>` the **raw field name**, correct `<PivotPosition>` and
   `<AggregateFunction>`), then call `report_create_from_xml`.
7. Propose execution with the `[EXECUTE_REPORT:...]` tag.

Everything else (the XML structure, view nesting, totals, sorting, restriction
operators, charts) is identical to a SQL-source metadata report — copy it from the
template you fetched with `report_get_xml`.
