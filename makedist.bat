@echo off
if not exist zilf\bin\debug\zilf.exe goto missing
if not exist zapf\bin\debug\zapf.exe goto missing

rd /s /q dist
mkdir dist
mkdir dist\bin
mkdir dist\doc
mkdir dist\library
mkdir dist\sample

copy distfiles\README.txt dist

copy zilf\bin\release\Antlr3.Runtime.dll dist\bin
copy distfiles\AntlrLicense.txt dist\bin
copy zilf\bin\release\zilf.exe dist\bin
copy zilf\bin\release\zilf.emit.dll dist\bin
copy COPYING.txt dist\bin
copy zapf\bin\release\zapf.exe dist\bin

copy zilf_manual.html dist\doc
copy zapf_manual.html dist\doc
copy zilf\quickref.txt dist\doc

copy library\*.zil dist\library
copy library\LICENSE.txt dist\library
copy library\ZIL_ZILF_differences.txt dist\library

copy examples\*.zil dist\sample

exit /b 0

:missing
echo Can't find compiled program files.
exit /b 1
