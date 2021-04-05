@ECHO OFF
SET stateLog=%1

IF [%1] == [] GOTO MISSINGPARAMETER

CALL :TANGIBLECLUSTER
GOTO EXIT

:TANGIBLECLUSTER
    CALL start "TestReceiver" call test\TestReceiver\bin\Debug\net5.0\TestReceiver.exe
    CALL start "TcpNode0" call TangibleNode\bin\Debug\net5.0\TangibleNode.exe TangibleNode\settings\settings_0_driver_test_0.json %stateLog% TcpNode0
    CALL start "TcpNode1" call TangibleNode\bin\Debug\net5.0\TangibleNode.exe TangibleNode\settings\settings_0_driver_test_1.json %stateLog% TcpNode1
    GOTO EXIT

:MISSINGPARAMETER
ECHO Please define whether to run with STATE_LOG enabled, i.e., "$> ...cmd <true or false>"
GOTO EXIT

:EXIT