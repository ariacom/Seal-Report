# Style a report view (edit charts & rendering in place)

Load this skill when the user asks to change **how an already-saved report
renders** — for example *"make the chart in TST100 horizontal and stacked"*,
*"hide the legend"*, *"set a chart title"*, *"switch the pie to a doughnut"*, or
*"sort the chart by value descending"*. Do **not** regenerate the report XML for
these requests — edit the view in place.

This works for any view type (Chart JS and the other chart engines, Page Table,
Gauge, KPI, Card, Container, Widget…), not only Chart JS.

## Workflow

1. Call `view_get_parameters` with only the report `path` to list every view in
   the report.
2. Call `view_get_parameters` again with `view_name` (e.g. `Chart JS`) to see
   that view's configurable parameters: type, current value, default, allowed
   values, and description. Always do this before configuring — it gives you the
   exact parameter names and valid values.
3. Call `report_configure_view` with the `path`, `view_name` and a `parameters`
   object of name/value pairs, e.g.
   `{ "chartjs_bar_horizontal": "True", "chartjs_bar_stacked": "True" }`.
   Values are validated against each parameter's type and enumeration; parameters
   left at their default are not stored.

`report_configure_view` edits the view **in place** without rewriting the report,
so models, elements, restrictions and the other views are preserved.

## Common Chart JS parameters

| User asks for | Parameter | Value |
|---|---|---|
| Horizontal / sideways bars | `chartjs_bar_horizontal` | `True` |
| Stacked / cumulative bars | `chartjs_bar_stacked` | `True` |
| Hide the legend | `chartjs_show_legend` | `False` |
| Legend on the right / bottom | `chartjs_legend_position` | `right` / `bottom` |
| Chart title | `chartjs_title` | the title text |
| Doughnut instead of pie | `chartjs_doughnut` | `True` |

## Rules

- **Always call `view_get_parameters` first** to discover valid parameter names
  and allowed values for the target view.
- `chartjs_bar_horizontal` and `chartjs_bar_stacked` apply **only to `Bar`
  series**.
- **Stacking requires more than one series.** The model needs a Column-position
  element with `<SerieDefinition>Splitter</SerieDefinition>`; with a single Data
  series there is nothing to stack. If the report has only one series, say so
  rather than silently producing an unstacked chart.
- Boolean values are the literal strings `True` / `False` (capitalised).
- Only set the parameters the user actually asked for — never write a full list
  of defaults.
- Respect the user's access rights: only touch reports they can access.
