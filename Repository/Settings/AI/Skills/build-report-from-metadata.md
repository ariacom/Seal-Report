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
   the real values to list. It returns at most 50 values: never use them to build a
   drop-down list on a column without enumerated list, see "Drop-down lists on restrictions" below.
4. Build the XML from the specification below and call `report_create_from_xml`.
   **The report exists only once this call has returned success in the current turn.**
   Never tell the user a report was created or saved, and never give a path, before that:
   running queries or loading this skill creates nothing. If the call returns an error, fix the XML
   and call it again, or report the error.
5. Propose execution with the `[EXECUTE_REPORT:...]` tag (see the system prompt), using the path
   returned by `report_create_from_xml` exactly.

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
      <MetaData />         <!-- empty, except <Enums> for a drop-down list defined in the report (see "Drop-down lists on restrictions") -->
      <MetaSourceGUID>«datasource_list GUID»</MetaSourceGUID>
    </ReportSource>
  </Sources>
  <Models>
    <ReportModel>
      <GUID>d</GUID>
      <Name>ModelName</Name>
      <SourceGUID>c</SourceGUID>   <!-- matches ReportSource GUID -->
      <Alias>Master</Alias>
      <MaxNumberOfRecords>5000</MaxNumberOfRecords>  <!-- default 5000 (applied by the tool when omitted or 0); a smaller value for a Top N;
                                                         no limit ONLY if the user explicitly asks: then pass no_record_limit=true to the tool -->
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

               Enumerated (the column is marked `enumerated list: …` by datasource_get_detail, or the restriction
               references a list of the report with <EnumGUIDRE>):
                 Call database_get_sample_values first, then list ALL returned values as <EnumValues>
                 (for a list defined in the report, <EnumValues> holds the values selected by default; omit it for no pre-selection):
                 <EnumValues>
                   <string>Argentina</string>
                   <string>France</string>
                 </EnumValues>
                 Do NOT use <Value1> for enumerated columns.
                 On a column WITHOUT enumerated list, <EnumValues> is ignored and the tool rejects it: a drop-down
                 list needs an enumerated list (see "Drop-down lists on restrictions").

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
      <!-- JoinOverrides: OPTIONAL, omit it by default. See "Join overrides" below. -->
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
                <!-- Always keep ALL six default sub-views. Never remove or omit any of them.
                     For a map, add the Map sub-view described in "Maps" below. -->
                <ReportView><GUID>j1</GUID><Name>Page Table</Name><TemplateName>Page Table</TemplateName><SortOrder>1</SortOrder></ReportView>
                <ReportView><GUID>j2</GUID><Name>Chart JS</Name><TemplateName>Chart JS</TemplateName><SortOrder>2</SortOrder></ReportView>
                <ReportView><GUID>j3</GUID><Name>Chart Scottplot</Name><TemplateName>Chart Scottplot</TemplateName><SortOrder>3</SortOrder></ReportView>
                <ReportView><GUID>j4</GUID><Name>Chart Echarts</Name><TemplateName>Chart Echarts</TemplateName><SortOrder>4</SortOrder></ReportView>
                <ReportView><GUID>j5</GUID><Name>Chart Plotly</Name><TemplateName>Chart Plotly</TemplateName><SortOrder>5</SortOrder></ReportView>
                <ReportView><GUID>j6</GUID><Name>Data Table</Name><TemplateName>Data Table</TemplateName><SortOrder>6</SortOrder></ReportView>
                <!-- When the model sets a Top N record limit (e.g. Top 10, not the default 5000), disable the
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
- Do **not** set `overwrite: true` unless the user explicitly asks to replace or to modify an existing report.
- `<EnumGUIDRE>` references the `<GUID>` of a `<MetaEnum>` defined in the report (placeholder, remapped automatically).

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
- **Enumerated restriction values** — always call `database_get_sample_values` first, then list all returned values as `<EnumValues><string>…</string></EnumValues>`. Never use `<Value1>` for enumerated columns. This applies only to columns having an enumerated list; for a column without list see "Drop-down lists on restrictions".
- **Date restrictions** — set `<Prompt>PromptTwoValues</Prompt>` so the user can adjust the range. For a concrete year/date, set literal `<Date1>`/`<Date2>`; never substitute a relative keyword.

### Drop-down lists on restrictions (enumerated lists)
When the user wants a restriction shown as a list ("liste", "liste déroulante", "enum", "select", "choose from a list"):

- **The column already has an enumerated list** (`datasource_get_detail` marks it `enumerated list: …`): the restriction
  inherits it automatically. Add nothing, just use `Equal` (and `<EnumValues>` only for default selections).
- **Otherwise, define the list in the report** and reference it from the restriction with `<EnumGUIDRE>`. Never put
  values in `<EnumValues>` of a column without list: they are ignored (the prompt stays a text box) and the tool
  rejects them. Never modify the data source for this (`datasource_manage_enum`) unless the user explicitly asks to
  add the list to the data source, as it changes every report using the column.

```xml
<ReportSource>
  <GUID>c</GUID>
  <Name>Source display name</Name>
  <ConnectionGUID>1</ConnectionGUID>
  <MetaData>
    <Enums>
      <MetaEnum>
        <GUID>en1</GUID>                 <!-- placeholder, regenerated; <EnumGUIDRE> references follow it -->
        <Name>Cities</Name>
        <IsDynamic>true</IsDynamic>       <!-- values loaded by the SQL -->
        <IsDbRefresh>true</IsDbRefresh>   <!-- reloaded before each execution -->
        <Sql>SELECT DISTINCT fr.v_etablissement.libelle_commune FROM fr.v_etablissement
WHERE fr.v_etablissement.libelle_commune IS NOT NULL ORDER BY 1</Sql>
      </MetaEnum>
    </Enums>
  </MetaData>
  <MetaSourceGUID>«datasource_list GUID»</MetaSourceGUID>
</ReportSource>
...
<ReportRestriction>
  <GUID>f1</GUID>
  <Name>fr.v_etablissement.libelle_commune</Name>
  <MetaColumnGUID>«column GUID»</MetaColumnGUID>
  <EnumGUIDRE>en1</EnumGUIDRE>      <!-- = the <GUID> of the <MetaEnum> above -->
  <Prompt>Prompt</Prompt>
  <Operator>Equal</Operator>          <!-- Equal or NotEqual only; no <Value1> -->
</ReportRestriction>
```

- **SQL**: first column = the value compared to the restricted column (it must return exactly the values of that
  column), optional second column = the label displayed. Use the table and column names of `datasource_get_detail`
  (without a `::type` cast suffix). The tool executes the SQL and rejects the report if it fails.
- **Large lists** (more than ~500 distinct values, e.g. cities, customers, products — check with
  `SELECT COUNT(DISTINCT …)` via `database_execute_query`): add a type-ahead filter so the list is built from the first
  characters typed by the user:
  ```xml
  <FilterChars>2</FilterChars>
  <Message>Type 2 characters</Message>   <!-- in the user's language -->
  <SqlDisplay>SELECT DISTINCT fr.v_etablissement.libelle_commune FROM fr.v_etablissement
WHERE fr.v_etablissement.libelle_commune LIKE '{EnumFilter}%' ORDER BY 1</SqlDisplay>
  ```
  (`ILIKE` instead of `LIKE` on PostgreSQL for a case-insensitive search.)
- **Static list** (a few fixed values): `<IsDynamic>` omitted and
  `<Values><MetaEV><Id>A</Id><Val>Active</Val></MetaEV><MetaEV><Id>C</Id><Val>Closed</Val></MetaEV></Values>`
  (`Id` = the value in the database, `Val` = the label).
- A request like "a list on the cities" made right after creating a report that restricts on the cities means a
  drop-down list on that restriction: **modify that report** (see "Modifying an existing report"), do not create a new report.

### Maps
A map (Leaflet `Map` view) shows one point per result row from a **latitude** and a **longitude** column (decimal
degrees, WGS84).

- Check first that the source has latitude/longitude columns (`datasource_get_detail`). If it has none, **tell the
  user that a map is not possible** with this source, and do not create or describe a map.
- Add the latitude and longitude columns as `Row` elements, no aggregation (one row per point). The first other
  column is the point label; the other columns are shown in the point popup.
- Add a `Map` sub-view in the Container, right after `Page Table` (give it `SortOrder` 2 and shift the following ones):
  ```xml
  <ReportView><GUID>j7</GUID><Name>Map</Name>
    <Parameters>
      <!-- all optional; column names = element display names -->
      <Parameter><Name>map_label</Name><Value>Name</Value></Parameter>              <!-- label column -->
      <Parameter><Name>map_color_column</Name><Value>Activity</Value></Parameter>   <!-- one color per value -->
      <Parameter><Name>map_size_column</Name><Value>Amount</Value></Parameter>      <!-- numeric: point size -->
    </Parameters>
    <TemplateName>Map</TemplateName><SortOrder>2</SortOrder></ReportView>
  ```
  The coordinate columns are found automatically when their display names contain "latitude" / "longitude";
  otherwise set `map_latitude` / `map_longitude` to their display names. Other parameters: `map_height` (pixels,
  default 500), `map_tile_provider` (`osm`, `opentopomap`, `ign` for France).

### Modifying an existing report
To change a saved report (add/remove a column or a restriction, add a drop-down list or a map…): call `report_get_xml`,
change **only** what is asked in the returned XML, then save it with `report_create_from_xml` on the **same path** with
`overwrite: true` (the user's request to change the report is the explicit request to replace it). Only a change of view
parameters (chart style, title…) goes through `style-report-view` instead.

### Join overrides (`<JoinOverrides>` inside `<ReportModel>`)
The tables of a model are linked automatically with the joins of the data source (listed under `## Joins` by
`datasource_get_detail`, with their type and GUID). **Omit `<JoinOverrides>` by default.** Add it only when the
request needs a join to behave differently **for this report only** — the data source is never modified:

- "all customers, **even those without orders**", "include products never sold", "with or without…" → the join must
  become an outer join that keeps the rows of the table the user wants complete.
- a filter on the optional table must not remove the rows of the complete table (e.g. "all customers with their 1997
  orders, if any") → put the filter in `<AdditionalClause>` instead of a `<ReportRestriction>` (a restriction goes to
  the WHERE clause and turns the outer join back into an inner join).

```xml
<JoinOverrides>
  <JoinOverride>
    <JoinGUID>«join GUID from datasource_get_detail»</JoinGUID>
    <JoinType>LeftOuter</JoinType>   <!-- optional: Inner | LeftOuter | RightOuter -->
    <AdditionalClause>Orders.OrderDate >= '1997-01-01'</AdditionalClause>  <!-- optional: SQL added with AND to the join clause -->
    <!-- also optional: <Exclude>true</Exclude> (join not used by this model), <IsBiDirectional>false</IsBiDirectional>,
         <Weight>5</Weight> (1-1000: a higher weight makes the join avoided when another path exists) -->
  </JoinOverride>
</JoinOverrides>
```

- **Left / right are the tables of the join as listed** (`Left → Right`): for `Customers → Orders`, `LeftOuter` keeps all
  Customers, `RightOuter` keeps all Orders.
- Only set the elements that change; everything omitted keeps the data source value.
- `<JoinGUID>` must be copied verbatim from `datasource_get_detail` — never fabricate it.
- `<AdditionalClause>` is raw SQL in the dialect of the source (SQL sources only); qualify columns with their table name.
- To count rows of the optional table, use `Count` on one of **its** columns (rows without match give 0).

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
- **Record limit**: reports are limited to **5000 records** by default (`<MaxNumberOfRecords>5000</MaxNumberOfRecords>`, applied by the tool when omitted or 0). Use a smaller value for a Top N, and then also set `data_warning_show=false` on the `Data Table` view. Remove the limit only when the user explicitly asks for it: pass `no_record_limit: true` to `report_create_from_xml`. Tell the user about the limit.
- Every `<Name>` must be the name of the column whose GUID is in `<MetaColumnGUID>`: check in which table a column really is (a column of another table needs its own table, joined by the source).
- **Only describe what is really in the saved XML.** If part of the request cannot be done (no coordinates for a map, a feature not covered by this skill…), say so explicitly instead of saving a report that only looks like the request.
- **Avoid redundant parameters** — never emit a `<Parameter>` whose value equals the template/model default; it is stripped on save and only adds noise.
- Keep all six default sub-views in every Container; never drop the Model view.
- Respect the user's access rights: only use sources and folders they can access.
