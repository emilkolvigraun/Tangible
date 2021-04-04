@ECHO OFF

CALL :TangibleCluster
GOTO :EOF

:TangibleCluster
    CALL :RunProgramAsync "bin\Debug\net5.0\TangibleNode.exe settings\demo1_settings_0.json"
    CALL :RunProgramAsync "bin\Debug\net5.0\TangibleNode.exe settings\demo1_settings_1.json"
    CALL :RunProgramAsync "bin\Debug\net5.0\TangibleNode.exe settings\demo1_settings_2.json"
GOTO :EOF

:RunProgramAsync
    REM ~sI expands the variable to contain short DOS names only
    start "TangibleNode" call %~s1