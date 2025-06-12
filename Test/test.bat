if not exist "%SYNC_DRIVE_HOME%\Apps\" mkdir "%SYNC_DRIVE_HOME%\Apps\"
if not exist "%SYNC_DRIVE_HOME%\Apps\CFG2\" mkdir "%SYNC_DRIVE_HOME%\Apps\CFG2\"
if not exist "%SYNC_DRIVE_HOME%\Apps\CFG2\MDP\" mkdir "%SYNC_DRIVE_HOME%\Apps\CFG2\MDP\"
if not exist "%SYNC_DRIVE_HOME%\Apps\CFG2\MDP\Test\" mkdir "%SYNC_DRIVE_HOME%\Apps\CFG2\MDP\Test\"

xcopy %~dp0\L0_TEST.csv "%SYNC_DRIVE_HOME%\Apps\CFG2\MDP\Test\" /Y

xcopy %~dp0\LogTest.txt "%SYNC_DRIVE_HOME%\Apps\CFG2\MDP\Test\" /Y

cd %~dp0

cd ..

echo %cd%

dotnet run --project MDPloadCSV "%SYNC_DRIVE_HOME%\Apps\CFG2\MDP\Test\L0_TEST.csv"

dotnet run --project MDPloadLog "%SYNC_DRIVE_HOME%\Apps\CFG2\MDP\Test\LogTest.txt"
