cd ..

@echo off
set version=%1

IF [%1] == [] GOTO MISSINGPARAMETER

:PUBLISH
 
docker tag emilkolvigraun/ec-dotnet-core-5.0:%version% emilkolvigraun/ec-dotnet-core-5.0:latest
docker push emilkolvigraun/ec-dotnet-core-5.0:%version%
docker push emilkolvigraun/ec-dotnet-core-5.0:latest 
 
GOTO EXIT  

:MISSINGPARAMETER
echo Please add the version number like "push.cmd 1.0"
GOTO EXIT

:EXIT
