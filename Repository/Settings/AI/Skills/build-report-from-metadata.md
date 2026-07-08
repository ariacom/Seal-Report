# Build a report from metadata (default report type)

Load this skill whenever the user asks to **create, build, save, or generate a
report** and the query does **not** require raw SQL. This is the correct tool for
the vast majority of reports — simple lists, search reports, totals/sums/counts/
averages grouped by one or more dimensions, date-filtered reports, and pivot /
cross-tab reports. Aggregation, grouping, and date filtering are **native
metadata capabilities** — they are never a reason to fall back to SQL.

> For raw-SQL reports (window functions, CTEs, UNION, subqueries, or a SQL query
> the user supplied) load `build-report-from-sql` instead. For MongoDB / NoSQL
> sources load `build-nosql-report`.

## Workflow

**Step 0 — always call `report_check_model_type` first** with the user's request
before deciding which creation tool to use. Follow its recommendation.

1. Call `datasource_list` to get the `MetaSourceGUID`.
2. Call `datasource_get_detail` to get the `MetaColumnGUID` for **every** element
   and restriction. Never guess or fabricate a column GUID.
3. For enumerated restriction values, call `database_get_sample_values` to obtain
   the real values to list.
4. Build the XML from the specification below and call `report_create_from_xml`.
5. Propose execution with the `[EXECUTE_REPORT:...]` tag (see the system prompt).

## Metadata report XML specification

```xml
<?xml version="1.0"?>
<Report xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <GUID>a</GUID>          <!-- placeholder; regenerated automatically -->
  <ViewGUID>b</ViewGUID>  <!-- must equal the root ReportView GUID below -->
  <DisplayName>Report Title</DisplayName>
  <Sources>
    <ReportSource>
      <GUID>c</GUID>
      <Name>Source display name</Name>
      <ConnectionGUID>1</ConnectionGUID>
      <MetaData />         <!-- always empty -->
      <MetaSourceGUID>«datasource_list GUID»</MetaSourceGUID>
    </ReportSource>
  </Sources>
  <Models>
    <ReportModel>
      <GUID>d</GUID>
      <Name>ModelName</Name>
      <SourceGUID>c</SourceGUID>   <!-- matches ReportSource GUID -->
      <Alias>Master</Alias>
      <MaxNumberOfRecords>0</MaxNumberOfRecords>  <!-- 0 = no limit; set a positive integer ONLY if the user asks to limit records -->
      <!-- ShowFirstLine ("Show first header line"): default true; OMIT it for normal tables.
           Add <ShowFirstLine>false</ShowFirstLine> here ONLY when the model is a cross-tab,
           i.e. it has at least one Row element AND at least one Column element. -->
      <Elements>
        <ReportElement>
          <GUID>e1</GUID>
          <Name>Table.Column</Name>  <!-- SQL sources keep the Table.Column form. -->
          <DisplayName />           <!-- leave empty to inherit the column display name -->
          <DisplayOrder>1</DisplayOrder> <!-- left-to-right column position; 1 = leftmost -->
          <PivotPosition>Row</PivotPosition>   <!-- Row | Column | Data | Page -->
          <!-- <SortOrder> is OPTIONAL. Omit it to keep the default "Automatic Ascendant".
               Add it with an explicit priority ONLY on the element(s) the user asks to sort by:
               Automatic Ascendant → ascending, automatic priority — the DEFAULT when the tag is omitted
               {n} Ascendant       → ascending, explicit priority n (lower n = sorted first); e.g. "1 Ascendant"
               {n} Descendant      → descending, explicit priority n; e.g. "1 Descendant"
               Not sorted          → no sort contribution from this element -->
          <AggregateFunction>Sum</AggregateFunction>  <!-- Sum|Count|Avg|Min|Max|CountDistinct; required for Data -->
          <Format>d</Format>        <!-- optional: N0 integer, N2 decimal, d date -->
          <!-- Chart fields — only needed when the report includes a chart (see "Charts" below). -->
          <MetaColumnGUID>«datasource_get_detail GUID»</MetaColumnGUID>
        </ReportElement>
        <!-- repeat for each column -->
      </Elements>
      <!-- Restriction: chain every active WHERE filter with AND. Omit the element entirely if there are no restrictions.
           Use a newline before each AND when there are more than 2 restrictions:
             2 or fewer : <Restriction>[f1] AND [f2]</Restriction>
             3 or more  : <Restriction>[f1]
AND [f2]
AND [f3]</Restriction>
           Each key inside [...] must exactly match the <GUID> of the corresponding <ReportRestriction> below. -->
      <Restriction>[f1]
	  AND [f2]</Restriction>
      <Restrictions>
        <ReportRestriction>
          <GUID>f1</GUID>           <!-- matches [f1] in <Restriction> above -->
          <Name>Table.Column</Name>
          <MetaColumnGUID>«datasource_get_detail GUID»</MetaColumnGUID>
          <Prompt>Prompt</Prompt>   <!-- Prompt | PromptOneValue | PromptTwoValues | None -->
          <Operator>Contains</Operator>  <!-- Default by column type:
                                              Text (free text)  → Contains
                                              Enumerated values → Equal (default; only Equal or NotEqual are allowed, never Contains/StartsWith/etc.)
                                              Numeric / Date    → Equal, Between, Greater, Smaller, etc.
                                              A column is "enumerated" when datasource_get_detail marks it `enumerated list: …`,
                                              regardless of its name — not when the name merely looks like a fixed set.
                                              Empty/null checks → IsEmpty, IsNotEmpty (text only), IsNull, IsNotNull (any type).
                                              These operators take NO value element. NEVER use Equal/NotEqual with an empty
                                              <Value1> to test for empty or null: a restriction whose value is empty is
                                              considered "not filled" and is SKIPPED at execution. -->
          <PlaceHolder>Type to filter</PlaceHolder>
          <Required>false</Required>  <!-- default false. "Prompted" does NOT mean "required" — keep false even when
                                            the restriction is prompted. Set true ONLY when the user explicitly says the
                                            value is mandatory (e.g. "the user must select…"). -->
          <!-- Filter values — use the correct element for the column type:

               Text (free text):
                 <Value1>search term</Value1>

               Enumerated (the column is marked `enumerated list: …` by datasource_get_detail):
                 Call database_get_sample_values first, then list ALL returned values as <EnumValues>:
                 <EnumValues>
                   <string>Argentina</string>
                   <string>France</string>
                 </EnumValues>
                 Do NOT use <Value1> for enumerated columns.

               Numeric / Between:
                 <Value1>100</Value1>  <Value2>500</Value2>

               Date keyword — use ONLY when user says "this year", "today", "current month", etc.:
                 <Date1Keyword>ThisYear</Date1Keyword>
                 (Today|ThisWeek|ThisMonth|ThisQuarter|ThisSemester|ThisYear|Now; offsets: Today-1D, ThisMonth-1M, ThisYear+1Y)

               Date literal — use when user specifies a concrete year or date range (e.g. "in 1997", "from Jan to Jun 2024"):
                 <Date1>1997-01-01T00:00:00</Date1>  <Date2>1997-12-31T00:00:00</Date2>
                 Never substitute a keyword (ThisYear, Today…) when the user gave a specific year or date.

               Omit value elements entirely when no default is needed (prompted with no pre-fill). -->
        </ReportRestriction>
        <!-- repeat for each restriction -->
      </Restrictions>
      <!-- AggregateRestriction / AggregateRestrictions: same pattern, applied as HAVING clause -->
    </ReportModel>
  </Models>
  <Views>
    <ReportView>
      <GUID>b</GUID>               <!-- matches ViewGUID above -->
      <Name>Report Title</Name>
      <Views>
        <!-- MANDATORY view nesting: Report > Model > Container > the six result sub-views.
             Never collapse or merge these levels into one. The MODEL view below MUST keep both
             <TemplateName>Model</TemplateName> AND <ModelGUID> (binding it to the <ReportModel>);
             without that Model view + ModelGUID the report has no data to render. -->
        <ReportView>                       <!-- MODEL view (TemplateName=Model, has ModelGUID) -->
          <GUID>h</GUID>
          <Name>ModelName</Name>
          <Views>
            <ReportView>                   <!-- CONTAINER view (TemplateName=Container, NO ModelGUID) -->
              <GUID>i</GUID>
              <Name>Model Container</Name>
              <Views>
                <!-- Always keep ALL six default sub-views. Never remove or omit any of them. -->
                <ReportView><GUID>j1</GUID><Name>Page Table</Name><TemplateName>Page Table</TemplateName><SortOrder>1</SortOrder></ReportView>
                <ReportView><GUID>j2</GUID><Name>Chart JS</Name><TemplateName>Chart JS</TemplateName><SortOrder>2</SortOrder></ReportView>
                <ReportView><GUID>j3</GUID><Name>Chart NVD3</Name><TemplateName>Chart NVD3</TemplateName><SortOrder>3</SortOrder></ReportView>
                <ReportView><GUID>j4</GUID><Name>Chart Scottplot</Name><TemplateName>Chart Scottplot</TemplateName><SortOrder>4</SortOrder></ReportView>
                <ReportView><GUID>j5</GUID><Name>Chart Plotly</Name><TemplateName>Chart Plotly</TemplateName><SortOrder>5</SortOrder></ReportView>
                <ReportView><GUID>j6</GUID><Name>Data Table</Name><TemplateName>Data Table</TemplateName><SortOrder>6</SortOrder></ReportView>
                <!-- When the model sets a record limit (<MaxNumberOfRecords> > 0, e.g. Top 10), disable the
                     "maximum number of records reached" warning by adding a <Parameters> block to this Data Table view:
                     <ReportView><GUID>j6</GUID><Name>Data Table</Name>
                       <Parameters><Parameter><Name>data_warning_show</Name><Value>false</Value></Parameter></Parameters>
                       <TemplateName>Data Table</TemplateName><SortOrder>6</SortOrder></ReportView> -->
              </Views>
              <TemplateName>Container</TemplateName>
              <SortOrder>1</SortOrder>
            </ReportView>
          </Views>
          <TemplateName>Model</TemplateName>  <!-- REQUIRED: the MODEL view, a level above the Container -->
          <ModelGUID>d</ModelGUID>            <!-- REQUIRED: matches the <ReportModel> GUID; this binding feeds data into the views -->
          <SortOrder>1</SortOrder>
        </ReportView>
      </Views>
      <TemplateName>Report</TemplateName>
      <Parameters>
        <!-- report_format: default output format (ReportFormat enum). Default is html, so OMIT it for html
             reports; add it only when the user asks for a different default (e.g. "generate in Excel", "as a
             PDF"). It belongs ONLY in THIS root Report view <Parameters> block — never on a sub-view.
             Values: html | print | Excel | PDF | HTML2PDF | csv | Text | XML | Json -->
        <!-- restrictions_per_row ("Restrictions: Number of restrictions per row"): default 4.
             OMIT this parameter when the model has 4 or fewer prompted restrictions.
             REQUIRED set to 6 whenever there are MORE than 4 prompted restrictions:
             <Parameter><Name>restrictions_per_row</Name><Value>6</Value></Parameter> -->
        <!-- force_execution: default false. OMIT it by default. Add it set to True ONLY when the user
             explicitly wants the report to execute immediately on first open:
             <Parameter><Name>force_execution</Name><Value>True</Value></Parameter> -->
      </Parameters>
      <SortOrder>0</SortOrder>
    </ReportView>
  </Views>
  <Cancel>false</Cancel>
</Report>
```

## Critical GUID rules

- `<MetaSourceGUID>` — exact GUID from `datasource_list`. Never regenerated; you must supply the correct value.
- `<MetaColumnGUID>` — exact column GUID from `datasource_get_detail`. Never regenerated; you must supply the correct value.
- **Never fabricate, sequence, or zero-fill a GUID** (e.g. `…-000000000001`). Every `<MetaColumnGUID>` must be copied verbatim from `datasource_get_detail`; if you have not called it for this source, call it before writing the XML.
- `<GUID>`, `<ViewGUID>`, `<SourceGUID>`, `<ModelGUID>` — internal cross-references regenerated automatically. Use any short placeholder (a, b, c…) and keep them consistent within the XML.
- Every `[key]` in `<Restriction>` must match the `<GUID>` of a `<ReportRestriction>` in `<Restrictions>`. Short placeholders (e.g. `f1`) are fine — the tool remaps them automatically.
- Paths use the format `Reports\FolderName\report_name.srex`. Available roots: `Reports`, `SubReports`, `Personal`.
- Do **not** set `overwrite: true` unless the user explicitly asks to replace an existing report.

## Key concepts

### PivotPosition
| Value | Meaning |
|---|---|
| `Row` | Dimension — appears as a row label in the table |
| `Column` | Cross-tab axis — pivots unique values into columns |
| `Data` | Measure — aggregated numeric value (Sum, Count, Avg, Min, Max, CountDistinct) |
| `Page` | Page-level filter — lets the user page through values |

**Cross-tab (pivot) models — `ShowFirstLine`:** when a model defines **both** at least one `Row` element **and** at least one `Column` element, it is a cross-tab. Set `<ShowFirstLine>false</ShowFirstLine>` on the `<ReportModel>`. Leave it at default (omit) for non-cross-tab models. Never use `<PivotPosition>Page</PivotPosition>` unless the user explicitly asks for a page-level filter.

### Totals (`<ShowTotal>` on Data elements)
Always include a `<ShowTotal>` on every `Data` element. Choose by table shape:
| Value | Effect |
|---|---|
| `No` | No total |
| `Column` | Grand-total **row at the bottom** (one total per pivoted column) |
| `Row` | Total **column on the right** (one total per row) |
| `RowColumn` | **Both** — bottom total row and right-hand total column |
| `RowHidden` / `RowColumnHidden` | Same as `Row` / `RowColumn` but detail value columns hidden |

- **Flat table** (no `Column` element) → `<ShowTotal>Column</ShowTotal>` (grand-total row).
- **Cross-tab** (Row + Column) → `<ShowTotal>RowColumn</ShowTotal>`. `Column` alone never produces a row total; "totals for rows and columns" needs `RowColumn`.

**Sort by the total column** — set `data_tables_sort_configuration` ("Data tables: Sort configuration") on the **`Data Table`** view, using `{LAST}` (the total is the last column): `[{LAST},'desc']` (descending) or `[{LAST},'asc']` (ascending). A row total must exist (`<ShowTotal>Row</ShowTotal>` or `RowColumn`). Use **only** this parameter for total-column sorting — do not also add `<SortOrder>` on the measure element.

### Column sort order (`<SortOrder>` inside `<ReportElement>`)
| Value | Effect |
|---|---|
| `Automatic Ascendant` | Ascending, automatic priority — the default when `<SortOrder>` is omitted |
| `Not sorted` | No sort contribution from this element |
| `{n} Ascendant` | Ascending with explicit priority n (lower n = sorted first) |
| `{n} Descendant` | Descending with explicit priority n |

- **Do not force a sort the user didn't ask for.** Omit `<SortOrder>` on every element when nothing about ordering is requested. Never assign sequential `1, 2, 3…` just to fill a value.
- Top-N / "best/worst" → set the measure (Data) to `1 Descendant` (or `1 Ascendant` for bottom); leave others default.
- Sort by a dimension ("alphabetical", "by date") → set that dimension to `1 Ascendant`; leave others default.
- Use explicit `1`, `2`, `3`… only when more than one element contributes to a requested sort.

**Column display order (`<DisplayOrder>`):** consecutive integers from 1, left to right. Dimension (Row) columns typically before measure (Data) columns.

### Restrictions
- `Prompt` → user is asked for a value at execution time. `None` → static filter, no interaction.
- `Required` → **defaults to `false`; keep it `false`.** "Prompted" and "required" are independent. Set `Required=true` only when the user explicitly says the value is mandatory.
- **Default operator by column type:** Text (free text) → `Contains`; Enumerated → `Equal` (only `Equal`/`NotEqual` allowed); Numeric/Date → `Equal`, `Between`, `Greater`, `Smaller`, etc.
- **Empty / null checks** — to filter on a missing value, use the dedicated operators: `IsEmpty` / `IsNotEmpty` (text columns) or `IsNull` / `IsNotNull` (any type), with **no** value element. Never use `Equal` with an empty `<Value1>` — a restriction with no value is treated as "not filled" and is silently ignored at execution.
- **A column is enumerated when `datasource_get_detail` marks it `enumerated list: …`** — judge by that marker, never by the column name.
- **Enumerated restriction values** — always call `database_get_sample_values` first, then list all returned values as `<EnumValues><string>…</string></EnumValues>`. Never use `<Value1>` for enumerated columns.
- **Date restrictions** — set `<Prompt>PromptTwoValues</Prompt>` so the user can adjust the range. For a concrete year/date, set literal `<Date1>`/`<Date2>`; never substitute a relative keyword.

### Charts (ChartJS — default engine)
Add a chart whenever the user says "chart", "graph", "plot", "visualize", or "show as chart". Use ChartJS unless another engine is explicitly requested.

**Element-level wiring:**
| Element role | PivotPosition | What to add |
|---|---|---|
| Axis / X-axis label | `Row` | `<SerieDefinition>Axis</SerieDefinition>` |
| Measure / data series | `Data` | `<ChartJSSerie>Bar</ChartJSSerie>` (or the chosen type) |
| Series splitter (multiple lines/bars) | `Column` | `<SerieDefinition>Splitter</SerieDefinition>` |

The chart tag goes **inside the same `<ReportElement>`** as its dimension/measure — it is not a separate element. Worked example (horizontal stacked bar = country axis, category splitter, Amount bar serie):
```xml
<!-- Axis element (dimension on the Row) -->
<ReportElement>
  <GUID>e1</GUID>
  <Name>Customers.Country</Name>
  <PivotPosition>Row</PivotPosition>
  <SerieDefinition>Axis</SerieDefinition>   <!-- REQUIRED: makes this the chart axis -->
  <AggregateFunction>Sum</AggregateFunction>
  <MetaColumnGUID>«guid»</MetaColumnGUID>
</ReportElement>
<!-- Splitter element (stacks/splits the series) -->
<ReportElement>
  <GUID>e2</GUID>
  <Name>Products.CategoryName</Name>
  <PivotPosition>Column</PivotPosition>
  <SerieDefinition>Splitter</SerieDefinition>   <!-- REQUIRED: splits each bar by category -->
  <AggregateFunction>Sum</AggregateFunction>
  <MetaColumnGUID>«guid»</MetaColumnGUID>
</ReportElement>
<!-- Measure element (the bar serie) -->
<ReportElement>
  <GUID>e3</GUID>
  <Name>Order Details.Amount</Name>
  <PivotPosition>Data</PivotPosition>
  <ChartJSSerie>Bar</ChartJSSerie>   <!-- REQUIRED: emits an actual chart serie -->
  <AggregateFunction>Sum</AggregateFunction>
  <Format>N0</Format>
  <MetaColumnGUID>«guid»</MetaColumnGUID>
</ReportElement>
```
Without `<SerieDefinition>Axis</SerieDefinition>` on the Row dimension (and `<ChartJSSerie>` on the measure) the chart has no axis and renders nothing — always wire all three when a chart is requested.

`<ChartJSSerie>` values: `None` (table only) | `Bar` | `Line` | `Pie` | `Doughnut` | `Scatter` | `Radar` | `PolarArea`. Selection: comparison → `Bar`; trend over time → `Line`; part-of-whole → `Pie`/`Doughnut`; correlation → `Scatter`; unspecified → `Bar`.

**Chart series sorting** — set on the **`Data` (measure)** element, independent of table `<SortOrder>`:
- `<SerieSortType>` = `None` (keep query order) · `Y` (by point/value) · `AxisLabel` (by dimension)
- `<SerieSortOrder>` = `Ascending` · `Descending` (defaults: `Y` + `Ascending`)
- "biggest first" → `Y` + `Descending`; "alphabetical/date order" → `AxisLabel` + the requested direction.

**Chart view options** — add a `<Parameters>` block to the `Chart JS` sub-view (bars are vertical & clustered by default):
| User asks for | Parameter | Value |
|---|---|---|
| Horizontal bars | `chartjs_bar_horizontal` | `True` |
| Stacked bars | `chartjs_bar_stacked` | `True` |
| Hide the legend | `chartjs_show_legend` | `False` |
| Legend on the right/bottom | `chartjs_legend_position` | `right` / `bottom` |
| Chart title | `chartjs_title` | the title text |
| Doughnut instead of pie | `chartjs_doughnut` | `True` |

`chartjs_bar_horizontal`/`chartjs_bar_stacked` apply only to `Bar` series. **Stacking requires more than one series** — add a Column-position `<SerieDefinition>Splitter</SerieDefinition>` element. Boolean values are the literal strings `True`/`False`. Only emit parameters the user actually asked for.

> To change how an **already-saved** report renders, do **not** rewrite the XML —
> load the `style-report-view` skill and edit the view in place instead.

## Rules
- Friendly, human-readable **display names** ("Sales of 1997 per Category", "Top 10 Customers by Revenue"). Never technical identifiers or underscores.
- **Filenames** lowercase with underscores derived from the display name (e.g. `sales_1997_per_category.srex`).
- Always include `<MaxNumberOfRecords>` (default `0`). When it is a positive limit (e.g. Top 10), also set `data_warning_show=false` on the `Data Table` view.
- **Avoid redundant parameters** — never emit a `<Parameter>` whose value equals the template/model default; it is stripped on save and only adds noise.
- Keep all six default sub-views in every Container; never drop the Model view.
- Respect the user's access rights: only use sources and folders they can access.
