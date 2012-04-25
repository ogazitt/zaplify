@echo off

if "%EMULATED%"=="true" goto :EOF

echo Allowing access to workflows
cd workflows
icacls *.* /grant Everyone:F

