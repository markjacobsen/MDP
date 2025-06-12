# MDPextractDB2
Used to extract DB2 data

## Packages Used
```
dotnet add package Net.IBM.Data.Db2
```

## Run via the command line for testing
```
# If in MDP solution dir:
dotnet run --project MDPextractDB2 "C:\Dev\Test\DB2test.sql" "MYDB" "jsmith" "p@s$word"

# If in project dir:
dotnet run -- "C:\Dev\Test\DB2test.sql" "MYDB" "jsmith" "p@s$word"
```