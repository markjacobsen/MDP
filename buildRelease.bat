cd %~dp0
cd MDPextractAzureSqlDB
CALL .\build.bat

cd %~dp0
cd MDPextractDataverse
CALL .\build.bat

cd %~dp0
cd MDPextractDB2
CALL .\build.bat

cd %~dp0
cd MDPextractSQLite
CALL .\build.bat

cd %~dp0
cd MDPloadCSV
CALL .\build.bat

cd %~dp0
cd MDPloadLog
CALL .\build.bat