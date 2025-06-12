# MDPextractDataverse
Used to extract Dataverse tables

## Packages Used
```
dotnet add package Azure.Identity
```

## Run via the command line for testing
```
# If in MDP solution dir:
dotnet run --project MDPloadCSV "C:\Dev\Test\L0_TEST.csv"

# If in project dir:
dotnet run -- "C:\Dev\Test\L0_TEST.csv"
```