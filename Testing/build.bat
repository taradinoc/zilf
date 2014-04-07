@echo off
xcopy /d /y ..\zilf\bin\debug\zilf.exe .
xcopy /d /y ..\zilf\bin\debug\Zilf.Emit.dll .
xcopy /d /y ..\zilf\bin\debug\Antlr3.Runtime.dll .
xcopy /d /y ..\zapf\bin\debug\zapf.exe .
xcopy /d /y ..\library\parser.zil .
zilf %1.zil
if errorlevel 1 exit /b %ERRORLEVEL%
zapf %1.zap
