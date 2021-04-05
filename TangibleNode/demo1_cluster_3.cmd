@ECHO OFF
SET stateLog=%1

IF [%1] == [] GOTO MISSINGPARAMETER

CALL :TANGIBLECLUSTER
GOTO EXIT

:TANGIBLECLUSTER
    CALL start "TcpNode0" call bin\Debug\net5.0\TangibleNode.exe settings\demo1_settings_0.json %stateLog% TcpNode0
    CALL start "TcpNode1" call bin\Debug\net5.0\TangibleNode.exe settings\demo1_settings_1.json %stateLog% TcpNode1
    CALL start "TcpNode2" call bin\Debug\net5.0\TangibleNode.exe settings\demo1_settings_2.json %stateLog% TcpNode2
    GOTO EXIT

:MISSINGPARAMETER
ECHO Please define whether to run with STATE_LOG enabled, i.e., "$> ...cmd <true or false>"
GOTO EXIT

:EXIT