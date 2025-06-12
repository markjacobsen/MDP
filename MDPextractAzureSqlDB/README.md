# MDPextractSQLserver
Used to extract from Azure SQL DB

## Packages Used
```
dotnet add package Azure.Identity
```

## Run via the command line for testing
```
# If in MDP solution dir:
dotnet run --project MDPextractAzureSqlDB "C:\Dev\Test\" "MSSStest.sql,MSSStest2.sql" "MYDB"

# If in project dir:
dotnet run -- "C:\Dev\Test\" "MSSStest.sql,MSSStest2.sql" "MYDB"
```