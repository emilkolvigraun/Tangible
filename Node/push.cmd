@echo off
set version=%1

IF [%1] == [] GOTO MISSINGPARAMETER

:PUBLISH

docker tag emilkolvigraun/tangible-node:%version% emilkolvigraun/tangible-node:latest
docker push emilkolvigraun/tangible-node:%version%
docker push emilkolvigraun/tangible-node:latest 

GOTO EXIT  

:MISSINGPARAMETER
echo Please add the version number like "push.cmd 1.0"
GOTO EXIT

:EXIT
