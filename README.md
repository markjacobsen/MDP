# My Data Platform (MDP)
## aka: Mark's Data Platform

This Solution is a collection of utilities for making it easier to load data into a single SQLite database. Why would you ever need to do that? IDK about you, but on a nearly daily basis I have data that's coming in from all directions: JSON, DB2, SQLServer, CSV, etc. Trying to search across all of these things and establish relationships can be a royal pain. Something SQL and relational databases make extremely easy.

## Getting Started
First, make sure you have an environment variable named "SYNC_DIR_HOME" defined with a value of a path that exists. Personally, I use the root of my Google Drive, OneDrive, Dropbox, or whatever but that's not a requirement. Just that the env var and directory exist.

### Clone the Repository
Hopefully this is a no brainer, but from the command line you can run
```
git clone https://github.com/markjacobsen/MDP.git
```
Or, simply use your tool of choice (VS Code, Visual Studio, etc) to clone the repo.

### Build the Solution
From the base directory you cloned the repo to, run:
```
dotnet build
```
Now you're ready to start putting the pieces together!

## MDP Components

### MDP.db
This is the actual SQLite database that is used for everything. It will be created at %SYNC_DIR_HOME%\MDP.db

### Extractors
These are simple utilities that make it easy to run a query or pull a file from somewhere and create a (CSV) file which then gets fed into the Loaders.

### Loaders
More simple utilities the make it easy to load tables from CSV, txt, etc
