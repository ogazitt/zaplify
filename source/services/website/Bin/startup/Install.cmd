@echo off
if "%EMULATED%"=="true" goto :EOF

cd startup

echo Getting MSIs
deployblob.exe /downloadFrom tsinstall /downloadTo .

echo Installing Speech Platform
msiexec.exe /qn /l* speech.log /i SpeechPlatformRuntime.msi

echo Installing MVC3
AspNetMVC3Setup.exe /q /log mvc3_install.htm

echo Installing Speech en-US pack
msiexec.exe /qn /l* speech.en-US.log /i MSSpeech_SR_en-US_TELE.msi

echo Installing grammars
mkdir ..\..\Content\grammars
7z.exe e grammars.zip -o..\..\Content\grammars

