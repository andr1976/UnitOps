@echo off
REM Register the membrane unit operation as a CAPE-OPEN COM server (per-machine, HKLM; run as Administrator).
REM The unit is a .NET 8 assembly activated via the comhost shim (Membrane.CapeOpen.comhost.dll); this writes
REM InprocServer32 -> comhost.dll plus the CAPE-OPEN CATIDs and CapeDescription. Requires the .NET 8 Desktop runtime.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0register-user.ps1" -Machine
