@ECHO OFF
SET size=%1
SET ipAddress=%2

IF [%1] == [] GOTO MISSINGPARAMETER
IF [%2] == [] GOTO MISSINGPARAMETER

CALL :GENERATE
GOTO EXIT

:GENERATE
    python ./settings-generator.py %size% %ipAddress%
    CALL :TANGIBLECLUSTER
    GOTO EXIT

:TANGIBLECLUSTER
    FOR /L %%X in (0, 1, %size%) DO (
        IF %%X==%size% (
            CALL :CLEAN
            GOTO EXIT
        )
        ECHO Deploying TcpNode%%X
        CALL start "DemoNode%%X" call TangibleNode\bin\Debug\net5.0\TangibleNode.exe DemoNode%%X.json DemoNode%%X
    )

:CLEAN
    TIMEOUT 5
    FOR /L %%X in (0, 1, %size%) DO (
        IF %%X==%size% (
            CALL :TESTRECEIVER
            GOTO EXIT
        )
        del /f "DemoNode%%X.json"
        ECHO Deleted settings-file "DemoNode%%X.json"
    )
    CALL :TESTRECEIVER
    GOTO EXIT

:MISSINGPARAMETER
    @REM ECHO Please define whether to run with STATE_LOG enabled, i.e., "$> ...cmd <true or false>"
    ECHO Please define cluster size, INT
    @REM ECHO Pipe sd-out to file, BOOL
    ECHO IP-Address of Nodes, e.g., 192.168.1.199, STR
    GOTO EXIT

:TESTRECEIVER
    @REM IF "%tofile%" == "true" (
    @REM     ECHO Starting receiver [piping to file]
    @REM     ECHO Tools\TestReceiver\bin\Debug\net5.0\TestReceiver.exe >> receiver_log.txt %ipAddress%
    @REM     Tools\TestReceiver\bin\Debug\net5.0\TestReceiver.exe >> receiver_log.txt %ipAddress%
    @REM ) ELSE (
        ECHO Starting receiver [in cmd]
        ECHO Tools\TestReceiver\bin\Debug\net5.0\TestReceiver.exe %ipAddress%
        CALL START "TestReceiver" CALL Tools\TestReceiver\bin\Debug\net5.0\TestReceiver.exe %ipAddress%
    @REM )
    GOTO EXIT

:EXIT