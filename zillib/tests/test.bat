@echo off
if "%1" == "" goto :Usage
if not exist test-%1.zil goto :TestMissing
if not exist ..\..\Zilf\bin\Debug\zilf.exe goto :ZilfMissing
if not exist ..\..\Zapf\bin\Debug\zapf.exe goto :ZapfMissing
if not exist .\ConsoleZLR.exe goto :CzlrMissing

..\..\Zilf\bin\Debug\zilf.exe -ip .. test-%1.zil
if errorlevel 1 goto :EOF
..\..\Zapf\bin\Debug\zapf.exe test-%1.zap
if errorlevel 1 goto :EOF
.\ConsoleZLR.exe test-%1.z3
goto :EOF

:Usage
echo Usage: %0 TEST-NAME
goto :EOF

:TestMissing
echo Couldn't find test-%1.zil.
goto :EOF

:ZilfMissing
echo Couldn't find zilf.exe under ..\..\Zilf\bin\Debug.
echo Build the solution first.
goto :EOF

:ZapfMissing
echo Couldn't find zapf.exe under ..\..\Zapf\bin\Debug.
echo Build the solution first.
goto :EOF

:CzlrMissing
echo Couldn't find ConsoleZLR.exe in the current directory.
echo Copy it (and its DLLs) from the ZLR distribution first.
goto :EOF
