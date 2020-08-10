
# Seal Report Web Server configuration

  

# Session management

  

## Options

  

With Seal Report Web Server you've got to options to manage the ASP session:

  

1. In memory - stored individually in memory not shared between nodes

2. In SQL Server - stored in database, in one table, shared between the nodes.

  

The default option that is chosen in to store in memory. To store the session in SQL Server you have to just add the configuration to appsettings.json file like:

  

```json

"SealConfiguration": {
    "SessionProvider": {
        "SqlServer": {
            "ConnectionString": "",
            "SchemaName": "dbo",
            "TableName": "CacheSessions"
        }
    }
}

```

  

### SQL Server session storage

  

To use SQL Server option, besides the configuration, you have to also create the session table. You can do that with the .net core tool created for that purpose:

  

```c#

dotnet  tool  install --global  dotnet-sql-cache

```

  

Then when the tool is installed you have to run the command according to the CLI:

  

``` CLI

Usage: dotnet sql-cache create [arguments] [options]
  
Arguments:

[connectionString] The connection string to connect to the database.

[schemaName] Name of the table schema.

[tableName] Name of the table to be created.

```