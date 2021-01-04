
@echo off
set version=%1

IF [%1] == [] GOTO OHNO

.\build.cmd %version%
docker run emilkolvigraun/ec-dotnet-core-5.0:%version%

GOTO EXIT  

:OHNO
echo Add the version (!latest): ".\run.cmd 0.0.1"
GOTO EXIT

:EXIT
