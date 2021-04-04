@ECHO OFF
SET stateLog=%1

IF [%1] == [] GOTO MISSINGPARAMETER

CALL :TANGIBLECLUSTER
GOTO :EOF

:TANGIBLECLUSTER
    CALL start "TcpNode0" call bin\Debug\net5.0\TangibleNode.exe settings\demo1_settings_0.json TcpNode0 %stateLog%
    CALL start "TcpNode1" call bin\Debug\net5.0\TangibleNode.exe settings\demo1_settings_1.json TcpNode1 %stateLog%
    CALL start "TcpNode2" call bin\Debug\net5.0\TangibleNode.exe settings\demo1_settings_2.json TcpNode2 %stateLog%

:MISSINGPARAMETER
ECHO Please define whether to run with STATE_LOG enabled, i.e., "$> ...cmd <true or false>"
GOTO EXIT

:EXIT