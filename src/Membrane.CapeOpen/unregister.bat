@echo off
REM Unregister the ORS membrane unit operation COM server (run as Administrator).
setlocal
set DLL=%~dp0Membrane.CapeOpen.dll
set REGASM=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
echo Unregistering "%DLL%" ...
"%REGASM%" "%DLL%" /unregister
endlocal
