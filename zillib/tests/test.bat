@echo off
if "%1" == "" goto :Usage
if not exist test-%1.zil goto :TestMissing
if not exist ..\..\bin\Debug\net10.0\zilf.exe goto :ZilfMissing
if not exist .\ConsoleZLR.exe goto :CzlrMissing

..\..\bin\Debug\net10.0\zilf.exe -I .. test-%1.zil
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
echo Couldn't find zilf.exe under ..\..\Zilf\bin\Debug\net10.0.
cd
echo Build the solution first.
goto :EOF

:CzlrMissing
echo Couldn't find ConsoleZLR.exe in the current directory.
echo Copy it (and its DLLs) from the ZLR distribution first.
goto :EOF
