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

echo Installing Speech Platform
msiexec.exe /qn /l* speech.log /i SpeechPlatformRuntime.msi

echo Installing MVC3
AspNetMVC3Setup.exe /q /log mvc3_install.htm

echo Installing Speech en-US pack
msiexec.exe /qn /l* speech.en-US.log /i MSSpeech_SR_en-US_TELE.msi

echo Installing grammars
mkdir ..\..\Content\grammars
7z.exe e grammars.zip -o..\..\Content\grammars

