@echo off
REM Unregister the membrane unit operation COM server (per-machine, HKLM; run as Administrator).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0unregister-user.ps1" -Machine
