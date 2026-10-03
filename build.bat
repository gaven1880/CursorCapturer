@echo off

set "_buildtag=%~1"
for %%I in (.) do set "_dirname=%%~nxI"

if "%_buildtag%"=="" (
  echo No build tag provided.
  exit /b 1
)

echo Building with tag: %_buildtag%

if exist "_publish" rmdir /s /q "_publish"

dotnet publish -c Release -r win-x64 --self-contained false -o "_publish"
if errorlevel 1 (
  echo Build failed.
  exit /b 1
)

tar -a -c -f "%_dirname%-%_buildtag%.zip" -C "_publish" .
if errorlevel 1 (
  echo ZIP process failed.
  exit /b 1
)

rmdir /s /q "_publish"

echo Done!
exit /b 0