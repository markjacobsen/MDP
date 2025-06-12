if not exist "%SYNC_DRIVE_HOME%\Apps\" mkdir "%SYNC_DRIVE_HOME%\Apps\"
if not exist "%SYNC_DRIVE_HOME%\Apps\MDP\" mkdir "%SYNC_DRIVE_HOME%\Apps\MDP\"
if not exist "%SYNC_DRIVE_HOME%\Apps\MDP\Test\" mkdir "%SYNC_DRIVE_HOME%\Apps\MDP\Test\"

xcopy %~dp0\L0_TEST.csv "%SYNC_DRIVE_HOME%\Apps\MDP\Test\" /Y

xcopy %~dp0\LogTest.txt "%SYNC_DRIVE_HOME%\Apps\MDP\Test\" /Y

cd %~dp0

cd ..

echo %cd%

dotnet run --project MDPloadCSV "%SYNC_DRIVE_HOME%\Apps\MDP\Test\L0_TEST.csv"

dotnet run --project MDPloadLog "%SYNC_DRIVE_HOME%\Apps\MDP\Test\LogTest.txt"
