@echo off
rem Rebuilds the Seal Report class documentation (Sandcastle Help File Builder)
rem Output: Projects\SealDocumentation\Help
rem Usage: BuildDocumentation.bat [nopause]
setlocal
for %%i in ("%~dp0..") do set "ROOT=%%~fi"

if not defined SHFBROOT (
    echo ERROR: SHFBROOT is not defined, Sandcastle Help File Builder must be installed.
    goto :error
)

rem Locate the MSBuild of the latest Visual Studio
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo ERROR: vswhere.exe not found, Visual Studio must be installed.
    goto :error
)
set "MSBUILD="
for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -prerelease -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"
if not defined MSBUILD (
    echo ERROR: MSBuild.exe not found.
    goto :error
)

echo === 1/2 Building SealWebServer (Debug): assemblies and XML documentation files ===
rem Incremental build only: a Rebuild would delete the generated swi-*.js files
dotnet build "%ROOT%\SealWebServer\SealWebServer.csproj" -c Debug -nologo -v:minimal
if errorlevel 1 goto :error

echo.
echo === 2/2 Building the class documentation ===
rem AlwaysLoadProject=True: workaround for 'Invalid or missing SHFBSchemaVersion' with MSBuild 18
"%MSBUILD%" "%~dp0seal.shfbproj" /p:AlwaysLoadProject=True /nologo /v:minimal /clp:ErrorsOnly;Summary
if errorlevel 1 goto :error

echo.
echo Documentation generated in %ROOT%\SealDocumentation\Help
echo Build log: %ROOT%\SealDocumentation\Help\LastBuild.log
if /i not "%~1"=="nopause" pause
exit /b 0

:error
echo.
echo BUILD FAILED
if /i not "%~1"=="nopause" pause
exit /b 1
