@ECHO OFF
SET stateLog=%1
SET node=%2

IF [%1] == [] GOTO MISSINGPARAMETER
IF [%2] == [] GOTO MISSINGPARAMETER

CALL :TANGIBLECLUSTER
GOTO EXIT

:TANGIBLECLUSTER
    TangibleNode\bin\Debug\net5.0\TangibleNode.exe TangibleNode\settings\settings_0_driver_test_%node%.json %stateLog% TcpNode%node% 
    GOTO EXIT

:MISSINGPARAMETER
    ECHO "[1]" Please define whether to run with STATE_LOG enabled, i.e., "$> ...cmd <true or false>"
    ECHO "[2]" Please define the node, e.g., 1, 2, 3 ..
    GOTO EXIT

:EXIT