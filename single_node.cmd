@ECHO OFF
SET node=%1

IF [%1] == [] GOTO MISSINGPARAMETER

CALL :TANGIBLECLUSTER
GOTO EXIT

:TANGIBLECLUSTER
    CALL start "DemoNode0" call TangibleNode\bin\Debug\net5.0\TangibleNode.exe %node% DemoNode0
    GOTO EXIT

:MISSINGPARAMETER
    ECHO "[2]" Please define the location of the configuration file, e.g., "TangibleNode\settings\single_node.json"
    GOTO EXIT

:EXIT