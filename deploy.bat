set CODE_BASE=%~dp0
set TARGET=%SYNC_DRIVE_HOME%\Apps\CFG2\MDP

xcopy "%CODE_BASE%MDPextractAzureSqlDB\bin\Debug\net9.0\" "%TARGET%\MDPextractAzureSqlDB\" /Y /E
xcopy "%CODE_BASE%MDPextractDataverse\bin\Debug\net9.0\" "%TARGET%\MDPextractDataverse\" /Y /E
xcopy "%CODE_BASE%MDPextractDB2\bin\Debug\net9.0\" "%TARGET%\MDPextractDB2\" /Y /E
xcopy "%CODE_BASE%MDPextractSQLite\bin\Debug\net9.0\" "%TARGET%\MDPextractSQLite\" /Y /E
xcopy "%CODE_BASE%MDPloadCSV\bin\Debug\net9.0\" "%TARGET%\MDPloadCSV\" /Y /E
xcopy "%CODE_BASE%MDPloadLog\bin\Debug\net9.0\" "%TARGET%\MDPloadLog\" /Y /E
xcopy "%CODE_BASE%MDPconsole\bin\Debug\net9.0\" "%TARGET%\MDPconsole\" /Y /E