
docker image prune -a -f

@echo off
set version=%1

IF [%1] == [] GOTO MISSINGPARAMETER

:BUILD
cd ..
docker build --rm -f "Dockerfile" -t emilkolvigraun/ec-dotnet-core-5.0:%version% .
docker tag emilkolvigraun/ec-dotnet-core-5.0:%version% emilkolvigraun/ec-dotnet-core-5.0:latest
   
GOTO EXIT

:MISSINGPARAMETER
echo Please add the version number like "build.cmd 1.0"
GOTO EXIT

:EXIT
