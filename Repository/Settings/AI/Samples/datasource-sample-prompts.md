# Sample prompts — Data Source Manager
# One prompt per line. Blank lines and lines starting with # are ignored.

# --- Inspect ---
List all available data sources and their tables
Show the schema of the [source] data source
Which reports use the [column] column?

# --- Add metadata (safe) ---
For [source], add the [table] table
For [source], add a full name column to the [table] table
For [source], create an enumerated list from the [column] column
For [source], suggest joins between the tables

# --- Change metadata (impact-checked) ---
For [source], add a join between [table] and [table]
For [source], change the SQL of the [column] column
Remove the [column] column from [source]
