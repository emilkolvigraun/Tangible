@echo off
set version=%1

IF [%1] == [] GOTO MISSINGPARAMETER

:BUILD

docker build --rm -f "Dockerfile" -t emilkolvigraun/tangible-driver:%version% .
docker tag emilkolvigraun/tangible-driver:%version% emilkolvigraun/tangible-driver:latest

GOTO EXIT

:MISSINGPARAMETER
echo Please add the version number like "build.cmd 1.0"
GOTO EXIT

:EXIT
