# MDPloadCSV
Used to load CSV files into the MDP.

## Packages Used
```
dotnet add package System.Data.SQLite
```

## Run via the command line for testing
```
# If in MDP solution dir:
dotnet run --project MDPloadCSV "C:\Dev\Test\L0_TEST.csv"

# If in project dir:
dotnet run -- "C:\Dev\Test\L0_TEST.csv"
```