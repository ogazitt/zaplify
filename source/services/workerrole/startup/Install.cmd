@echo off

if "%EMULATED%"=="true" goto :EOF

echo Allowing access to workflows
cd workflows
icacls *.* /grant Everyone:R

cd ..\nlp
icacls lex.dat /grant Everyone:R
