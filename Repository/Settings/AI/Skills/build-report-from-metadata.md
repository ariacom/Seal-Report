# Build a report from metadata (default report type)

Load this skill whenever the user asks to **create, build, save, modify or generate a
report** and the query does **not** require raw SQL: lists, search reports,
totals/counts/averages by dimension, date filters, cross-tabs, charts, maps, tabs,
cards, KPIs, dashboards… Aggregation, grouping and date filtering are native
metadata capabilities — never a reason to fall back to SQL.

> Raw SQL (window functions, CTEs, UNION, a query supplied by the user):
> `build-report-from-sql`. MongoDB / NoSQL sources: `build-nosql-report`.

You can build **any** Seal report. Learn Seal like a developer: find an example,
check the classes and the view parameters, write, test, fix. The only limit is
your authorization level (SQL and scripts are refused above it: build the report
without them, or tell the user that an administrator must grant a higher level).

## Method

0. Call `report_check_model_type` with the request and follow its recommendation.
1. **Data**: `datasource_list` (MetaSourceGUID), `datasource_get_detail` (column
   GUIDs, enumerated lists, joins). `database_get_sample_values` for filter values.
2. **Find an example**: `seal_search` in `Reports\Samples` (e.g. `Tab Page`,
   `Card`, `KPI`, `Container`, `ShowTotal`), then `seal_read` the sample around the
   line. `view_template_list` lists the views; `view_template_get_parameters` gives
   the parameters of a template; `seal_read` on `Views\<Template>.cshtml` shows how
   it renders them.
3. **Check the classes**: `seal_describe_class` (`ReportElement`,
   `ReportRestriction`, `ReportModel`, `ReportView`…) gives the valid XML elements,
   their enumeration values and defaults. An omitted element keeps its default:
   write only what differs.
4. **Write**: new report → `report_create_from_xml`; change to a saved report →
   `report_patch_xml` (XPath set/insert/remove; call `report_get_xml` first). A
   change of view parameters only can also use `style-report-view`.
5. **Test and fix**: each save executes the report with its default criteria and
   renders its views. `TEST FAILED`, `VIEW RENDER FAILED` or a timeout → fix and save
   again before answering. `0 row(s)` → check the filter values, or tell the user
   nothing matches. Use `render_picture: true` once to check a layout (tabs,
   containers, charts) when the result is not obvious.
6. **Answer**: the report exists only after a successful save in this turn. Give the
   row count; explain any difference with a figure already given in the conversation
   (a report applies exactly the filters the user validated, never an extra one).
   Propose execution with the `[EXECUTE_REPORT:...]` tag, using the returned path.

## Structure

`Report` > `Sources/ReportSource` (MetaSourceGUID) > `Models/ReportModel`
(SourceGUID, Elements, Restrictions) > `Views/ReportView` tree:
`Report` view (root, = `<ViewGUID>` of the report) > `Model` view (`<ModelGUID>` =
the model; no data without it) > `Container` > result views (`Page Table`,
`Data Table`, `Chart JS`, `Map`…). Tabs, cards, KPIs, widgets: copy the tree of a
sample. A model is executed only if a view displays it.

Minimal list with one prompted restriction:

```xml
<?xml version="1.0"?>
<Report xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <GUID>a</GUID><ViewGUID>b</ViewGUID><DisplayName>Orders of France</DisplayName>
  <Sources><ReportSource><GUID>c</GUID><Name>Northwind</Name><ConnectionGUID>1</ConnectionGUID>
    <MetaSourceGUID>«datasource_list GUID»</MetaSourceGUID></ReportSource></Sources>
  <Models><ReportModel><GUID>d</GUID><Name>Orders</Name><SourceGUID>c</SourceGUID>
    <Elements>
      <ReportElement><GUID>e1</GUID><Name>Orders.OrderDate</Name><PivotPosition>Row</PivotPosition><MetaColumnGUID>«column GUID»</MetaColumnGUID></ReportElement>
      <ReportElement><GUID>e2</GUID><Name>Orders.Freight</Name><PivotPosition>Data</PivotPosition><AggregateFunction>Sum</AggregateFunction><ShowTotal>Column</ShowTotal><MetaColumnGUID>«column GUID»</MetaColumnGUID></ReportElement>
    </Elements>
    <Restriction>[f1]</Restriction>
    <Restrictions><ReportRestriction><GUID>f1</GUID><Name>Orders.ShipCountry</Name><MetaColumnGUID>«column GUID»</MetaColumnGUID>
      <Prompt>Prompt</Prompt><Operator>Contains</Operator><Value1>France</Value1></ReportRestriction></Restrictions>
  </ReportModel></Models>
  <Views><ReportView><GUID>b</GUID><Name>View</Name><TemplateName>Report</TemplateName><Views>
    <ReportView><GUID>h</GUID><Name>Orders</Name><TemplateName>Model</TemplateName><ModelGUID>d</ModelGUID><Views>
      <ReportView><GUID>i</GUID><Name>Container</Name><TemplateName>Container</TemplateName><Views>
        <ReportView><GUID>j1</GUID><Name>Page Table</Name><TemplateName>Page Table</TemplateName><SortOrder>1</SortOrder></ReportView>
        <ReportView><GUID>j2</GUID><Name>Data Table</Name><TemplateName>Data Table</TemplateName><SortOrder>2</SortOrder></ReportView>
      </Views></ReportView></Views></ReportView></Views></ReportView></Views>
</Report>
```

## GUID rules

- `<MetaSourceGUID>` from `datasource_list`, every `<MetaColumnGUID>` and
  `<JoinGUID>` from `datasource_get_detail`: copied verbatim, **never invented or
  sequenced**.
- Internal GUIDs (`GUID`, `ViewGUID`, `SourceGUID`, `ModelGUID`, `[key]` in
  `<Restriction>`, `EnumGUIDRE`): short placeholders (`a`, `d`, `f1`) kept consistent
  in a new report, regenerated by the tool. In a patch keep the real GUIDs and use
  placeholders only for the new elements. Never mix real GUIDs and placeholders for
  the same object.
- Paths: `Reports\Folder\report_name.srex` or `Personal\report_name.srex`.
  `overwrite: true` only to replace a report the user asked to replace.

## Rules the tools do not check

- **Names**: friendly display names ("Sales of 1997 per Category"); file names in
  lowercase with underscores. `<Name>` of an element = name of its column
  (`Table.Column`).
- **Elements**: `PivotPosition` Row (dimension), Column (cross-tab axis), Data
  (measure, with `AggregateFunction`), Page only if asked. A cross-tab (Row + Column)
  sets `<ShowFirstLine>false</ShowFirstLine>` on the model.
- **Totals**: every Data element has `<ShowTotal>`: `Column` for a flat table,
  `RowColumn` for a cross-tab. Sorting on the total column:
  `data_tables_sort_configuration` = `[{LAST},'desc']` on the Data Table view.
- **Sort**: no `<SortOrder>` unless asked. Top N → measure `1 Descendant` and a
  smaller `MaxNumberOfRecords`, plus `data_warning_show` = `false` on the Data
  Table view. A sort for the chart only uses `SerieSortType`/`SerieSortOrder`.
- **Restrictions**: every requested filter is prompted (`Prompt`, `PromptTwoValues`
  for a date range) and pre-filled with the requested value; `None` only for a
  fixed filter the user asked for. `Required` stays false unless the value is
  mandatory. Text → `Contains`; enumerated column (marked `enumerated list` by
  `datasource_get_detail`, never judged by its name) → `Equal`/`NotEqual` with
  `<EnumValues><string>id</string></EnumValues>`; empty/null → `IsEmpty`,
  `IsNotEmpty`, `IsNull`, `IsNotNull` with no value. A concrete year or date →
  literal `<Date1>`/`<Date2>`, never a keyword; keywords (`ThisYear`,
  `Today-1D`…) only for relative periods. More than 4 prompted restrictions →
  `restrictions_per_row` = 6 on the root Report view.
- **Drop-down list** on a column without enumerated list: define a `<MetaEnum>` in
  `<ReportSource><MetaData><Enums>` and reference it with `<EnumGUIDRE>` on the
  restriction (`Equal`/`NotEqual`). For the values of a column, give
  `<MetaEnum><GUID>en1</GUID><Name>Cities</Name><FromColumnGUID>«column GUID»</FromColumnGUID></MetaEnum>`:
  the tool generates the list SQL, allowed at every level. A `<Sql>` you write yourself
  requires the Sql level; a fixed list uses `<Values><MetaEV><Id>A</Id><Val>Active</Val></MetaEV></Values>`
  (the only choice on a NoSQL source). Never modify the data source for one report.
- **View parameters rendered as code** (colors, CSS, summaries…) accept below the
  Scripts level: colors as `['#1f77b4','green','rgba(0,0,0,0.5)']`, color mappings as
  `'ok':'green','ko':'#ff0000'`, CSS declarations `property: value;` (no `url(`), class
  names, HTML without script or event handler, and an existing report path for
  `widget_exec_report`.
- **Outer joins** for one report ("even those without orders"): `<JoinOverrides>`
  with `LeftOuter`/`RightOuter` (Left/Right as listed by `datasource_get_detail`); a
  filter on the optional table goes in `<AdditionalClause>`, not in a restriction.
- **Charts** (Chart JS by default): Row dimension `<SerieDefinition>Axis</SerieDefinition>`,
  Data measure `<ChartJSSerie>Bar</ChartJSSerie>` (Line for a trend, Pie for a
  part-of-whole), optional Column element `<SerieDefinition>Splitter</SerieDefinition>`
  for several series (needed to stack), and a `Chart JS` view in the Container.
  Without the Axis the chart is empty. Each chart view reads the serie of its own
  engine: `Chart Echarts` → `<EChartsSerie>`, `Chart Plotly` → `<PlotlySerie>`,
  `Chart Scottplot` → `<ScottPlotSerie>` (e.g. `<EChartsSerie>Pie</EChartsSerie>`).
  Set the serie of every engine displayed for the model, otherwise its view is empty.
- **Maps**: only if the source has latitude/longitude columns (as Row elements, no
  aggregation) and a `Map` view in the Container (`map_label`, `map_color_column`,
  `map_size_column`; `map_latitude`/`map_longitude` when the column names do not
  contain latitude/longitude). Without coordinates, say a map is not possible.
- **Status colors**: when a chart or a map shows a status, a level or a rating (OK/KO,
  Good/Average/Poor, Excellent/Insufficient, Error…), color each value by its meaning
  unless the user chose other colors: positive → green `#2ca02c`, intermediate or
  warning → orange `#ff7f0e`, negative or error → red `#d62728`, unknown or not
  applicable → grey `#7f7f7f`. Map every value (labels exactly as displayed: case,
  accents, spaces; read them in the test rows or with `database_get_sample_values`)
  with `chartjs_color_mappings` / `plotly_color_mappings`
  (`'Good':'#2ca02c','Average':'#ff7f0e','Poor':'#d62728'`) or `map_colors` (one
  `value=color` per line). Chart JS maps the axis labels of a pie but the series of a
  bar or a line: for bars per status, put the status in a Column element with
  `<SerieDefinition>Splitter</SerieDefinition>`. `Chart Echarts` and `Chart Scottplot`
  have no mapping usable here: use `Chart JS` for a status chart.
- **Record limit**: models are limited to 5000 records by default; pass
  `no_record_limit: true` only if the user explicitly asks. Tell the user.
- **Editable criteria** on an existing report: set `Prompt` and keep the current
  values as defaults.
- **Only describe what is saved.** If part of the request cannot be done, say so.
- Only use the sources and folders the user can access; never emit a parameter whose
  value equals its default.
