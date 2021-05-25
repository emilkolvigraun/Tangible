@echo off
set version=%1

IF [%1] == [] GOTO MISSINGPARAMETER

:BUILD

docker build --rm -f "Dockerfile" -t emilkolvigraun/tangible-test-driver:%version% .
docker tag emilkolvigraun/tangible-test-driver:%version% emilkolvigraun/tangible-test-driver:latest

GOTO EXIT

:MISSINGPARAMETER
echo Please add the version number like "build.cmd 1.0"
GOTO EXIT

:EXIT
