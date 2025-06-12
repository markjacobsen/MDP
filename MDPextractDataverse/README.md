# MDPextractDataverse
Used to extract Dataverse tables

## Packages Used
```
dotnet add package Azure.Identity
```

## Run via the command line for testing
```
# If in MDP solution dir:
dotnet run --project MDPExtractDataverse "C:\Dev\Test\Dataverse" "L0_CONTACT.dv,L0_ACCOUNT.dv" "DEV"

# If in project dir:
dotnet run -- "C:\Dev\Test\Dataverse" "L0_CONTACT.dv,L0_ACCOUNT.dv" "DEV"
```