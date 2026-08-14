@echo off
REM Register the membrane unit operation as a CAPE-OPEN COM server (per-machine; run as Administrator).
REM RegAsm invokes the [ComRegisterFunction] which writes the CAPE-OPEN CATIDs and CapeDescription keys.
setlocal
set DLL=%~dp0Membrane.CapeOpen.dll
set REGASM=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
if not exist "%DLL%" (
  echo ERROR: %DLL% not found. Build the project first ^(x64, Release^).
  exit /b 1
)
echo Registering "%DLL%" ...
"%REGASM%" "%DLL%" /codebase
if %ERRORLEVEL% NEQ 0 (
  echo Registration FAILED ^(are you running as Administrator?^).
  exit /b %ERRORLEVEL%
)
echo Done. The unit should now appear in COFE's unit-operation palette as "%~n0".
endlocal
