@echo off
set version=%1

IF [%1] == [] GOTO MISSINGPARAMETER

:PUBLISH

docker tag emilkolvigraun/tangible-driver:%version% emilkolvigraun/tangible-driver:latest
docker push emilkolvigraun/tangible-driver:%version%
docker push emilkolvigraun/tangible-driver:latest 

GOTO EXIT  

:MISSINGPARAMETER
echo Please add the version number like "push.cmd 1.0"
GOTO EXIT

:EXIT
