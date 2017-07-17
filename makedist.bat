@echo off
setlocal
set zip7="C:\Program Files\7-Zip\7z.exe"
set winrar="C:\Program Files\WinRAR\WinRAR.exe"

if not exist zilf\bin\release\zilf.exe goto missing
if not exist zapf\bin\release\zapf.exe goto missing

rd /s /q dist
mkdir dist

for /f %%n in ('zilf\bin\release\zilf.exe -x DistFiles\distname.mud') do (
  set distname=%%n
)
set dist=dist\%distname%
mkdir %dist%

mkdir %dist%\bin
mkdir %dist%\doc
mkdir %dist%\library
mkdir %dist%\sample
mkdir %dist%\sample\advent

copy distfiles\README.txt %dist%

copy zilf\bin\release\zilf.exe %dist%\bin
copy zilf\bin\release\zilf.emit.dll %dist%\bin
copy zilf\bin\release\zilf.common.dll %dist%\bin
copy COPYING.txt %dist%\bin
copy zapf\bin\release\zapf.exe %dist%\bin

copy zilf_manual.html %dist%\doc
copy zapf_manual.html %dist%\doc
copy zilf\quickref.txt %dist%\doc

copy library\*.zil %dist%\library
copy library\*.mud %dist%\library
copy library\LICENSE.txt %dist%\library
copy library\ZIL_ZILF_differences.txt %dist%\library

copy examples\*.zil %dist%\sample
copy examples\advent\*.zil %dist%\sample\advent

del /s %dist%\*~

if exist %zip7% goto use7z
if exist %winrar% goto usewinrar
echo Can't find 7-Zip or WinRAR. Not zipping.
exit /b 2

:use7z
pushd dist
%zip7% a %distname%.zip %distname%
exit /b 0

:usewinrar
pushd dist
%winrar% a -afzip %distname%.zip %distname%
exit /b 0

:missing
echo Can't find compiled program files.
exit /b 1
