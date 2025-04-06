@echo off

set PROJECT_NAME=GameModifiers
set PROJECT_PATH=%cd%\..
set BUILD_PATH=%PROJECT_PATH%\Build\%PROJECT_NAME%

for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set "HH=%dt:~8,2%" & set "Min=%dt:~10,2%" & set "Sec=%dt:~12,2%"
set TIME_DATE=%YYYY%.%MM%.%DD%-%HH%.%Min%.%Sec%

if not exist %BUILD_PATH% (
    echo %BUILD_PATH% doesnt exists, cannot package %PROJECT_NAME%!
	echo.
	echo Try running GenerateProjectFiles.bar first :^)
	echo.
	pause
	exit
)

echo.                                              
echo Packaging %PROJECT_NAME% Project         
echo.                                               

echo Please choose a build configuration:
echo 1. Release
echo 2. Debug
echo.
set /p CONFIG_CHOICE="Choose configuration: "

if "%CONFIG_CHOICE%"=="1" (
    set CONFIG=Release
) else if "%CONFIG_CHOICE%"=="2" (
    set CONFIG=Debug
) else (
	echo.
    echo Invalid choice. Please enter 1 or 2.
    goto :EOF
)

echo.
echo Building project with dotnet in %CONFIG% configuration...

cd %BUILD_PATH%
dotnet build --configuration %CONFIG%

if %ERRORLEVEL% NEQ 0 (
	echo Errors found, cannot package %PROJECT_NAME%...
	pause
	exit
)

set BINARIES_PATH=%PROJECT_PATH%\Binaries\%CONFIG%\net8.0
if not exist %BINARIES_PATH% (
	echo Something went wrong, binary files was not generated. Cannot package %PROJECT_NAME%!
	pause
	exit
)

cd %BINARIES_PATH%

set PACKAGE_FOLDER_NAME=%PROJECT_NAME%_%CONFIG%_%TIME_DATE%
set PACKAGE_PATH=%PROJECT_PATH%\Packages\%PACKAGE_FOLDER_NAME%
if not exist %PACKAGE_PATH% (
	mkdir %PACKAGE_PATH%
) else (
	echo %PACKAGE_PATH% already exists? Cannot package %PROJECT_NAME%!
	pause
	exit
)

echo Packaging files into %PACKAGE_PATH%...

if exist %PROJECT_NAME%.deps.json (
	xcopy /y "%PROJECT_NAME%.deps.json" "%PACKAGE_PATH%\"
)

if exist GameModifiers.dll (
	xcopy /y "%PROJECT_NAME%.dll" "%PACKAGE_PATH%\"
)

if exist GameModifiers.pdb (
	xcopy /y "%PROJECT_NAME%.pdb" "%PACKAGE_PATH%\"
)

cd %PROJECT_PATH%

if exist Content (
	xcopy /s Content %PACKAGE_PATH%
)

echo Packing complete!

pause
exit