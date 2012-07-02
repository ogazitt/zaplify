@echo off

echo Allowing access to workflows
cd workflows
icacls *.* /grant Everyone:R

cd ..\nlp
icacls lex.dat /grant Everyone:R
cd ..

if "%EMULATED%"=="true" goto :EOF

cd startup

echo Getting MSIs
if %SPEECHWORKERCOUNT% lss 1 (set BLOBCONTAINER=webrole-install) else (set BLOBCONTAINER=workerrole-install)
echo Getting MSIs from %BLOBCONTAINER% >blobcontainer.log
deployblob.exe /downloadFrom %BLOBCONTAINER% /downloadTo .

echo Installing Splunk
msiexec.exe /l* splunk.log /i splunkforwarder-4.3.2-123586-x64-release.msi RECEIVING_INDEXER="%SPLUNKENDPOINT%" AGREETOLICENSE=Yes /quiet
"%ProgramFiles%"\SplunkUniversalForwarder\bin\Splunk.exe add tcp "%SPLUNKLOCALPORT%" -auth admin:changeme >splunkinit.log 2>&1

if %SPEECHWORKERCOUNT% lss 1 goto :EOF

rem don't need to grant access anymore because running elevated
echo granting HTTP.SYS port permission 
rem netsh.exe http add urlacl url=http://+:%SPEECHPORT%/api user=everyone listen=yes delegate=yes >netsh.log 2>&1

echo Installing Speech Platform
msiexec.exe /qn /l* speech.log /i SpeechPlatformRuntime.msi

echo Installing Speech en-US pack
msiexec.exe /qn /l* speech.en-US.log /i MSSpeech_SR_en-US_TELE.msi

echo Installing grammars
mkdir ..\grammars
7z.exe e grammars.zip -o..\grammars

