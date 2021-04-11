@echo off
set version=%1

IF [%1] == [] GOTO MISSINGPARAMETER

:PUBLISH

docker tag emilkolvigraun/tangible-test-driver:%version% emilkolvigraun/tangible-test-driver:latest
docker push emilkolvigraun/tangible-test-driver:%version%
docker push emilkolvigraun/tangible-test-driver:latest 

GOTO EXIT  

:MISSINGPARAMETER
echo Please add the version number like "push.cmd 1.0"
GOTO EXIT

:EXIT
