@echo off

cd nlp
icacls lex.dat /grant Everyone:R
cd ..

if "%EMULATED%"=="true" goto :EOF

cd startup

echo Getting MSIs
deployblob.exe /downloadFrom webrole-install /downloadTo .

echo Installing Splunk
msiexec.exe /l* splunk.log /i splunkforwarder-4.3.2-123586-x64-release.msi RECEIVING_INDEXER="%SPLUNKENDPOINT%" AGREETOLICENSE=Yes /quiet
"%ProgramFiles%"\SplunkUniversalForwarder\bin\Splunk.exe add tcp "%SPLUNKLOCALPORT%" -auth admin:changeme >splunkinit.log 2>&1

