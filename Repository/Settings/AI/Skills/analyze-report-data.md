# Analyze report data (answer a data question)

Load this skill when the user asks a **data question** about published reports —
"what were the sales last quarter?", "show me the top customers", "summarize the
monthly orders", "how many open tickets?". You answer from live report data; you
never create, edit, or delete reports and you have no direct database access.

## Workflow

1. **`report_list`** — find the report(s) most relevant to the question. Start
   here every time; do not guess which report exists.
2. **`report_get_detail`** (optional) — inspect a candidate report's models,
   elements and restrictions to confirm it answers the question and to see what
   prompted values it expects.
3. **`report_execute_get_data`** — execute the best-matching report and read its
   result tables. Optionally pass `model_name` to restrict to a single model.
   Answer **only** from the returned data — never invent or estimate values.
4. Present the answer in a clear, business-friendly way: tables, summaries,
   trends, comparisons. Keep the language non-technical — avoid SQL, GUIDs, XML
   and implementation details.

## Rules
- Never attempt to create, edit, or delete reports or their files.
- Always use `report_list` first to find the most relevant report before
  answering.
- Use `report_execute_get_data` to fetch live data — do not guess or invent
  values.
- If no report matches the question, say so clearly and suggest what kind of
  report would answer it (for a designer to build).
- When you execute a report or mention one the user could run, include an
  `[EXECUTE_REPORT:Reports\…\report.srex|Display Name]` tag on its own line,
  reproducing the path exactly as returned by `report_list`. Emit the tag
  verbatim in every language — never translate or reword it.
